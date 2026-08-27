using System;
using System.IO;
using System.Text.Json;

namespace AirLyrics.App.Services.Spotify
{
    public class SpotifyConfig
    {
        public string ClientId { get; set; } = string.Empty;
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }

        public bool IsAuthenticated => !string.IsNullOrEmpty(AccessToken) && !string.IsNullOrEmpty(RefreshToken);

        private static readonly string ConfigPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AirLyrics",
            "spotify_auth.json");

        public static SpotifyConfig Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    var json = File.ReadAllText(ConfigPath);
                    return JsonSerializer.Deserialize<SpotifyConfig>(json) ?? new SpotifyConfig();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cargar auth de Spotify: {ex.Message}");
            }
            return new SpotifyConfig();
        }

        public void Save()
        {
            try
            {
                var dir = Path.GetDirectoryName(ConfigPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al guardar auth de Spotify: {ex.Message}");
            }
        }

        public void Clear()
        {
            AccessToken = string.Empty;
            RefreshToken = string.Empty;
            ExpiresAt = DateTime.MinValue;
            Save();
        }
    }
}
