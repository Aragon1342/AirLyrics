using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using AirLyrics.App.Models;

namespace AirLyrics.App.Services.Lyrics
{
    public static class LrcParser
    {
        // Regex para capturar timestamps en formato [mm:ss.xx] o [mm:ss.xxx]
        private static readonly Regex LrcLineRegex = new(
            @"^\[(?<min>\d{1,2}):(?<sec>\d{1,2})(?:\.(?<ms>\d{1,3}))?\](?<text>.*)$", 
            RegexOptions.Compiled);

        public static List<LyricLine> Parse(string lrcContent)
        {
            var result = new List<LyricLine>();
            if (string.IsNullOrWhiteSpace(lrcContent))
            {
                return result;
            }

            var lines = lrcContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();
                var match = LrcLineRegex.Match(line);
                if (match.Success)
                {
                    var min = int.Parse(match.Groups["min"].Value, CultureInfo.InvariantCulture);
                    var sec = int.Parse(match.Groups["sec"].Value, CultureInfo.InvariantCulture);
                    var msStr = match.Groups["ms"].Value;

                    var ms = 0;
                    if (!string.IsNullOrEmpty(msStr))
                    {
                        if (msStr.Length == 2) ms = int.Parse(msStr, CultureInfo.InvariantCulture) * 10;
                        else if (msStr.Length == 3) ms = int.Parse(msStr, CultureInfo.InvariantCulture);
                        else if (msStr.Length == 1) ms = int.Parse(msStr, CultureInfo.InvariantCulture) * 100;
                    }

                    var timestamp = new TimeSpan(0, 0, min, sec, ms);
                    var text = match.Groups["text"].Value.Trim();

                    result.Add(new LyricLine(timestamp, text));
                }
            }

            // Ordenar por tiempo
            result.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));
            return result;
        }
    }
}
