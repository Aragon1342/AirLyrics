using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AirLyrics.App.Models;

namespace AirLyrics.App.Services.Lyrics
{
    public class LrcLibResponse
    {
        public int id { get; set; }
        public string? trackName { get; set; }
        public string? artistName { get; set; }
        public string? albumName { get; set; }
        public double duration { get; set; }
        public bool instrumental { get; set; }
        public string? plainLyrics { get; set; }
        public string? syncedLyrics { get; set; }
    }

    public class LrcLibService
    {
        private static readonly HttpClient _httpClient = new()
        {
            BaseAddress = new Uri("https://lrclib.net/api/")
        };

        private readonly Dictionary<string, List<LyricLine>> _cache = new();

        static LrcLibService()
        {
            _httpClient.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue("AirLyricsApp", "1.0.0"));
        }

        public async Task<List<LyricLine>> GetLyricsAsync(string trackName, string artistName, string albumName, TimeSpan duration)
        {
            var cacheKey = $"{trackName.ToLowerInvariant()} - {artistName.ToLowerInvariant()}";
            if (_cache.TryGetValue(cacheKey, out var cached))
            {
                return cached;
            }

            try
            {
                // Limpiar nombre de canciones (quitar " - Remastered", " (feat. ...)", etc.) para mejorar búsqueda
                var cleanTrack = CleanTitle(trackName);
                var durationSec = (int)Math.Round(duration.TotalSeconds);

                // 1. Intentar endpoint exacto /get
                var getUrl = $"get?track_name={Uri.EscapeDataString(cleanTrack)}&artist_name={Uri.EscapeDataString(artistName)}&duration={durationSec}";
                if (!string.IsNullOrEmpty(albumName))
                {
                    getUrl += $"&album_name={Uri.EscapeDataString(albumName)}";
                }

                var response = await _httpClient.GetAsync(getUrl);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var data = JsonSerializer.Deserialize<LrcLibResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (data != null)
                    {
                        var parsed = ExtractLyrics(data);
                        _cache[cacheKey] = parsed;
                        return parsed;
                    }
                }

                // 2. Fallback: búsqueda difusa con /search
                var searchUrl = $"search?q={Uri.EscapeDataString($"{cleanTrack} {artistName}")}";
                var searchResponse = await _httpClient.GetAsync(searchUrl);
                if (searchResponse.IsSuccessStatusCode)
                {
                    var searchJson = await searchResponse.Content.ReadAsStringAsync();
                    var searchResults = JsonSerializer.Deserialize<List<LrcLibResponse>>(searchJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (searchResults != null && searchResults.Count > 0)
                    {
                        // Priorizar resultado con syncedLyrics
                        var bestMatch = searchResults.Find(r => !string.IsNullOrEmpty(r.syncedLyrics)) ?? searchResults[0];
                        var parsed = ExtractLyrics(bestMatch);
                        _cache[cacheKey] = parsed;
                        return parsed;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al consultar LRCLIB: {ex.Message}");
            }

            return new List<LyricLine>();
        }

        private static List<LyricLine> ExtractLyrics(LrcLibResponse response)
        {
            if (response.instrumental)
            {
                return new List<LyricLine>
                {
                    new(TimeSpan.Zero, "♪ [Canción Instrumental] ♪")
                };
            }

            if (!string.IsNullOrEmpty(response.syncedLyrics))
            {
                return LrcParser.Parse(response.syncedLyrics);
            }

            if (!string.IsNullOrEmpty(response.plainLyrics))
            {
                var lines = response.plainLyrics.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                var result = new List<LyricLine>();
                foreach (var line in lines)
                {
                    result.Add(new LyricLine(TimeSpan.Zero, line.Trim()));
                }
                return result;
            }

            return new List<LyricLine>();
        }

        private static string CleanTitle(string title)
        {
            if (string.IsNullOrEmpty(title)) return title;
            
            // Quitar tags como (feat. ...), [Remastered], - Live, etc.
            var clean = Regex.Replace(title, @"\s*[\(\[](feat\.|with|remastered|live|deluxe|version).*?[\)\]]", "", RegexOptions.IgnoreCase);
            var dashIndex = clean.IndexOf(" - ");
            if (dashIndex > 0)
            {
                clean = clean.Substring(0, dashIndex);
            }
            return clean.Trim();
        }
    }
}
