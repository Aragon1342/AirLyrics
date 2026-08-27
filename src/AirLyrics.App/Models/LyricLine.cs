using System;

namespace AirLyrics.App.Models
{
    public class LyricLine
    {
        public TimeSpan Timestamp { get; set; }
        public string Text { get; set; } = string.Empty;
        public bool IsActive { get; set; }

        public LyricLine() { }

        public LyricLine(TimeSpan timestamp, string text)
        {
            Timestamp = timestamp;
            Text = text;
        }
    }
}
