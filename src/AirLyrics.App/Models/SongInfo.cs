using System;

namespace AirLyrics.App.Models
{
    public class SongInfo
    {
        public string Title { get; set; } = string.Empty;
        public string Artist { get; set; } = string.Empty;
        public string Album { get; set; } = string.Empty;
        public string? AlbumArtUrl { get; set; }
        public TimeSpan Duration { get; set; }
        public TimeSpan Progress { get; set; }
        public bool IsPlaying { get; set; }
    }
}
