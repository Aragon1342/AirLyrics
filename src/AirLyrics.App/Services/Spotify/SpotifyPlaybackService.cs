using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using AirLyrics.App.Models;
using SpotifyAPI.Web;

namespace AirLyrics.App.Services.Spotify
{
    public class SpotifyPlaybackService : IDisposable
    {
        private readonly SpotifyAuthService _authService;
        private CancellationTokenSource? _pollingCts;
        private SongInfo? _currentSong;
        private Stopwatch _progressStopwatch = new();
        private TimeSpan _lastReportedProgress = TimeSpan.Zero;

        public event EventHandler<SongInfo?>? TrackChanged;
        public event EventHandler<TimeSpan>? ProgressUpdated;

        public SongInfo? CurrentSong => _currentSong;

        public SpotifyPlaybackService(SpotifyAuthService authService)
        {
            _authService = authService;
            _authService.Authenticated += OnAuthenticated;
            _authService.LoggedOut += OnLoggedOut;
        }

        private void OnAuthenticated(object? sender, SpotifyClient client)
        {
            StartPolling();
        }

        private void OnLoggedOut(object? sender, EventArgs e)
        {
            StopPolling();
            _currentSong = null;
            TrackChanged?.Invoke(this, null);
        }

        public void StartPolling()
        {
            StopPolling();
            _pollingCts = new CancellationTokenSource();
            _ = PollPlaybackLoopAsync(_pollingCts.Token);
            _ = SmoothProgressLoopAsync(_pollingCts.Token);
        }

        public void StopPolling()
        {
            _pollingCts?.Cancel();
            _pollingCts?.Dispose();
            _pollingCts = null;
            _progressStopwatch.Stop();
        }

        private async Task PollPlaybackLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var client = await _authService.GetClientAsync();
                    if (client != null)
                    {
                        var currentlyPlaying = await client.Player.GetCurrentlyPlaying(new PlayerCurrentlyPlayingRequest());

                        if (currentlyPlaying?.Item is FullTrack track)
                        {
                            var artists = string.Join(", ", track.Artists.ConvertAll(a => a.Name));
                            var isNewTrack = _currentSong == null || 
                                             _currentSong.Title != track.Name || 
                                             _currentSong.Artist != artists;

                            var duration = TimeSpan.FromMilliseconds(track.DurationMs);
                            var progress = TimeSpan.FromMilliseconds(currentlyPlaying.ProgressMs ?? 0);
                            var isPlaying = currentlyPlaying.IsPlaying;

                            _lastReportedProgress = progress;
                            _progressStopwatch.Restart();

                            if (isNewTrack)
                            {
                                _currentSong = new SongInfo
                                {
                                    Title = track.Name,
                                    Artist = artists,
                                    Album = track.Album?.Name ?? string.Empty,
                                    AlbumArtUrl = track.Album?.Images?.Count > 0 ? track.Album.Images[0].Url : null,
                                    Duration = duration,
                                    Progress = progress,
                                    IsPlaying = isPlaying
                                };

                                TrackChanged?.Invoke(this, _currentSong);
                            }
                            else if (_currentSong != null)
                            {
                                _currentSong.IsPlaying = isPlaying;
                                _currentSong.Progress = progress;
                            }
                        }
                        else
                        {
                            if (_currentSong != null)
                            {
                                _currentSong = null;
                                TrackChanged?.Invoke(this, null);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error en polling de Spotify: {ex.Message}");
                }

                // Polling cada 1.5 segundos para no saturar rate limit
                await Task.Delay(1500, token).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Emite actualizaciones de progreso a 50ms para un scroll de letras ultra fluido
        /// </summary>
        private async Task SmoothProgressLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (_currentSong != null && _currentSong.IsPlaying)
                {
                    var estimatedProgress = _lastReportedProgress + _progressStopwatch.Elapsed;
                    if (estimatedProgress <= _currentSong.Duration)
                    {
                        ProgressUpdated?.Invoke(this, estimatedProgress);
                    }
                }

                await Task.Delay(50, token).ConfigureAwait(false);
            }
        }

        public void Dispose()
        {
            StopPolling();
        }
    }
}
