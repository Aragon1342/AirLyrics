using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SpotifyAPI.Web;

namespace AirLyrics.App.Services.Spotify
{
    public class SpotifyAuthService
    {
        public const string RedirectUri = "http://127.0.0.1:5543/callback/";
        private const int Port = 5543;

        private readonly HttpClient _httpClient = new();
        private SpotifyConfig _config;

        public SpotifyConfig Config => _config;

        public event EventHandler<SpotifyClient>? Authenticated;
        public event EventHandler? LoggedOut;

        public SpotifyAuthService()
        {
            _config = SpotifyConfig.Load();
        }

        public async Task<SpotifyClient?> GetClientAsync()
        {
            if (!_config.IsAuthenticated)
            {
                return null;
            }

            // Si el token expiró o está por expirar en los próximos 60 segundos, refrescarlo
            if (DateTime.UtcNow >= _config.ExpiresAt.AddSeconds(-60))
            {
                var refreshed = await RefreshTokenAsync();
                if (!refreshed)
                {
                    return null;
                }
            }

            return new SpotifyClient(_config.AccessToken);
        }

        public async Task<bool> StartLoginAsync(string clientId, CancellationToken cancellationToken = default)
        {
            _config.ClientId = clientId;
            _config.Save();

            // 1. Generar PKCE verifier y challenge
            var verifier = GenerateCodeVerifier();
            var challenge = GenerateCodeChallenge(verifier);

            // 2. Iniciar servidor local Loopback (HttpListener)
            using var listener = new HttpListener();
            listener.Prefixes.Add(RedirectUri);
            
            try
            {
                listener.Start();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"No se pudo iniciar el listener local en el puerto {Port}. {ex.Message}", ex);
            }

            // 3. Construir URL de autorización oficial de Spotify
            var scopes = "user-read-playback-state user-read-currently-playing user-read-private user-read-email";
            var authUrl = $"https://accounts.spotify.com/authorize?" +
                          $"client_id={Uri.EscapeDataString(clientId)}" +
                          $"&response_type=code" +
                          $"&redirect_uri={Uri.EscapeDataString(RedirectUri)}" +
                          $"&code_challenge_method=S256" +
                          $"&code_challenge={Uri.EscapeDataString(challenge)}" +
                          $"&scope={Uri.EscapeDataString(scopes)}";

            // 4. Abrir en el navegador predeterminado del usuario
            Process.Start(new ProcessStartInfo
            {
                FileName = authUrl,
                UseShellExecute = true
            });

            // 5. Esperar la respuesta de Spotify en el loopback
            var contextTask = listener.GetContextAsync();
            
            using (cancellationToken.Register(() => listener.Stop()))
            {
                try
                {
                    var context = await contextTask;
                    var request = context.Request;
                    var response = context.Response;

                    var code = request.QueryString.Get("code");
                    var error = request.QueryString.Get("error");

                    string responseString;
                    if (!string.IsNullOrEmpty(code))
                    {
                        responseString = @"<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'/>
    <title>AirLyrics - Conectado</title>
    <style>
        body { font-family: 'Segoe UI', sans-serif; background-color: #0f172a; color: #f8fafc; text-align: center; padding-top: 80px; }
        .box { background: #1e293b; max-width: 450px; margin: auto; padding: 40px; border-radius: 16px; box-shadow: 0 10px 25px rgba(0,0,0,0.5); }
        h1 { color: #1db954; font-size: 24px; margin-bottom: 10px; }
        p { color: #94a3b8; font-size: 14px; }
    </style>
</head>
<body>
    <div class='box'>
        <h1>✅ ¡Conectado con éxito a AirLyrics!</h1>
        <p>Tu cuenta de Spotify se ha vinculado correctamente. Ya puedes cerrar esta ventana y regresar a la aplicación.</p>
    </div>
</body>
</html>";
                    }
                    else
                    {
                        responseString = $"<html><body style='background:#0f172a;color:#ef4444;text-align:center;padding:50px;'><h1>Error al autenticar: {error}</h1></body></html>";
                    }

                    var buffer = Encoding.UTF8.GetBytes(responseString);
                    response.ContentLength64 = buffer.Length;
                    response.ContentType = "text/html; charset=utf-8";
                    await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                    response.OutputStream.Close();

                    if (!string.IsNullOrEmpty(code))
                    {
                        return await ExchangeCodeForTokenAsync(clientId, code, verifier);
                    }

                    return false;
                }
                catch (Exception)
                {
                    return false;
                }
                finally
                {
                    if (listener.IsListening)
                    {
                        listener.Stop();
                    }
                }
            }
        }

        private async Task<bool> ExchangeCodeForTokenAsync(string clientId, string code, string codeVerifier)
        {
            var tokenParams = new System.Collections.Generic.Dictionary<string, string>
            {
                { "grant_type", "authorization_code" },
                { "code", code },
                { "redirect_uri", RedirectUri },
                { "client_id", clientId },
                { "code_verifier", codeVerifier }
            };

            var request = new HttpRequestMessage(HttpMethod.Post, "https://accounts.spotify.com/api/token")
            {
                Content = new FormUrlEncodedContent(tokenParams)
            };

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var errContent = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"Error al canjear token: {errContent}");
                return false;
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            _config.AccessToken = root.GetProperty("access_token").GetString() ?? "";
            _config.RefreshToken = root.GetProperty("refresh_token").GetString() ?? "";
            var expiresIn = root.GetProperty("expires_in").GetInt32();
            _config.ExpiresAt = DateTime.UtcNow.AddSeconds(expiresIn);
            _config.Save();

            Authenticated?.Invoke(this, new SpotifyClient(_config.AccessToken));
            return true;
        }

        public async Task<bool> RefreshTokenAsync()
        {
            if (string.IsNullOrEmpty(_config.RefreshToken) || string.IsNullOrEmpty(_config.ClientId))
            {
                return false;
            }

            var tokenParams = new System.Collections.Generic.Dictionary<string, string>
            {
                { "grant_type", "refresh_token" },
                { "refresh_token", _config.RefreshToken },
                { "client_id", _config.ClientId }
            };

            var request = new HttpRequestMessage(HttpMethod.Post, "https://accounts.spotify.com/api/token")
            {
                Content = new FormUrlEncodedContent(tokenParams)
            };

            try
            {
                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    _config.Clear();
                    LoggedOut?.Invoke(this, EventArgs.Empty);
                    return false;
                }

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                _config.AccessToken = root.GetProperty("access_token").GetString() ?? "";
                if (root.TryGetProperty("refresh_token", out var newRefresh))
                {
                    _config.RefreshToken = newRefresh.GetString() ?? _config.RefreshToken;
                }
                var expiresIn = root.GetProperty("expires_in").GetInt32();
                _config.ExpiresAt = DateTime.UtcNow.AddSeconds(expiresIn);
                _config.Save();

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al refrescar token: {ex.Message}");
                return false;
            }
        }

        public void Logout()
        {
            _config.Clear();
            LoggedOut?.Invoke(this, EventArgs.Empty);
        }

        private static string GenerateCodeVerifier()
        {
            var bytes = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Base64UrlEncode(bytes);
        }

        private static string GenerateCodeChallenge(string codeVerifier)
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
            return Base64UrlEncode(hash);
        }

        private static string Base64UrlEncode(byte[] bytes)
        {
            return Convert.ToBase64String(bytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
        }
    }
}
