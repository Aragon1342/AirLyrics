using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using AirLyrics.App.Models;
using AirLyrics.App.Native;
using AirLyrics.App.Services.Lyrics;
using AirLyrics.App.Services.Spotify;

namespace AirLyrics.App.Views
{
    public partial class OverlayWindow : Window
    {
        private bool _isGhostMode = false;
        private HotKeyManager? _hotKeyManager;
        private int _ghostHotKeyId = -1;

        private readonly SpotifyAuthService _spotifyAuthService;
        private readonly SpotifyPlaybackService _spotifyPlaybackService;
        private readonly LrcLibService _lyricsService = new();
        private AppSettings _settings;

        private List<LyricLine> _currentLyrics = new();
        private int _activeLyricIndex = -1;
        private double _lyricFontSize = 22.0;
        private string _activeColorHex = "#38BDF8";
        private bool _isLoaded = false;

        public OverlayWindow()
        {
            InitializeComponent();
            _settings = AppSettings.Load();
            _lyricFontSize = _settings.FontSize;
            _activeColorHex = _settings.ActiveColorHex;

            _spotifyAuthService = new SpotifyAuthService();
            _spotifyPlaybackService = new SpotifyPlaybackService(_spotifyAuthService);

            _spotifyPlaybackService.TrackChanged += OnTrackChanged;
            _spotifyPlaybackService.ProgressUpdated += OnProgressUpdated;

            Loaded += OverlayWindow_Loaded;
            Closed += OverlayWindow_Closed;
            KeyDown += OverlayWindow_KeyDown;
        }

        private void OverlayWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _isLoaded = true;

            ColorSliderPopup.PlacementTarget = ColorPickerButton;
            HueSlider.ValueChanged += ColorSlider_ValueChanged;
            LightnessSlider.ValueChanged += ColorSlider_ValueChanged;

            UpdateFontSizeLabel();
            UpdateColorPreview(_activeColorHex);

            RegisterGhostHotKey();

            if (_spotifyAuthService.Config.IsAuthenticated)
            {
                SpotifyConnectButton.ToolTip = "Spotify Conectado (Haz clic para gestionar)";
                _spotifyPlaybackService.StartPolling();
            }
            else
            {
                ShowEmptyState("AirLyrics", "Haz clic en '🟢' para vincular Spotify");
            }
        }

        private void RegisterGhostHotKey()
        {
            try
            {
                if (_hotKeyManager != null && _ghostHotKeyId != -1)
                {
                    _hotKeyManager.Unregister(_ghostHotKeyId);
                    _ghostHotKeyId = -1;
                }

                _hotKeyManager ??= new HotKeyManager(this);
                _ghostHotKeyId = _hotKeyManager.Register(
                    _settings.GhostModifier, 
                    _settings.GhostVirtualKey, 
                    ToggleGhostMode);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"No se pudo registrar HotKey global: {ex.Message}");
            }
        }

        private void OverlayWindow_Closed(object? sender, EventArgs e)
        {
            _spotifyPlaybackService.Dispose();
            _hotKeyManager?.Dispose();
            _hotKeyManager = null;
        }

        private async void OnTrackChanged(object? sender, SongInfo? song)
        {
            await Dispatcher.InvokeAsync(async () =>
            {
                if (song != null)
                {
                    TrackTitleBanner.Text = song.Title;
                    TrackArtistAlbumBanner.Text = string.IsNullOrEmpty(song.Album)
                        ? song.Artist 
                        : $"{song.Artist} • {song.Album}";

                    ShowEmptyState($"♪ {song.Title}", $"Buscando letra para {song.Artist}...");

                    _currentLyrics = await _lyricsService.GetLyricsAsync(
                        song.Title, 
                        song.Artist, 
                        song.Album, 
                        song.Duration);

                    _activeLyricIndex = -1;

                    if (_currentLyrics.Count > 0)
                    {
                        RenderLyricsList();
                    }
                    else
                    {
                        ShowEmptyState($"♪ {song.Title}", "Letra no disponible para esta canción");
                    }
                }
                else
                {
                    _currentLyrics.Clear();
                    _activeLyricIndex = -1;
                    LyricsContainer.Children.Clear();
                    TrackTitleBanner.Text = "Spotify en Pausa";
                    TrackArtistAlbumBanner.Text = "Reproduce una canción en Spotify";
                    ShowEmptyState("Spotify en Pausa", "Reproduce una canción en Spotify");
                }
            });
        }

        private void RenderLyricsList()
        {
            EmptyStatePanel.Visibility = Visibility.Collapsed;
            LyricsScrollViewer.Visibility = Visibility.Visible;
            LyricsContainer.Children.Clear();

            for (int i = 0; i < _currentLyrics.Count; i++)
            {
                var line = _currentLyrics[i];
                var textBlock = new TextBlock
                {
                    Text = string.IsNullOrWhiteSpace(line.Text) ? "• • •" : line.Text,
                    FontSize = _lyricFontSize,
                    FontWeight = FontWeights.Medium,
                    Foreground = Brushes.White,
                    Opacity = 0.4,
                    TextAlignment = TextAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 10, 0, 10),
                    Effect = CreateDynamicShadow(false),
                    Tag = i
                };

                LyricsContainer.Children.Add(textBlock);
            }
        }

        private void OnProgressUpdated(object? sender, TimeSpan progress)
        {
            Dispatcher.Invoke(() =>
            {
                if (_currentLyrics.Count == 0 || LyricsContainer.Children.Count == 0) return;

                int newIndex = -1;
                for (int i = 0; i < _currentLyrics.Count; i++)
                {
                    if (_currentLyrics[i].Timestamp <= progress)
                    {
                        newIndex = i;
                    }
                    else
                    {
                        break;
                    }
                }

                if (newIndex != -1 && newIndex != _activeLyricIndex)
                {
                    UpdateActiveLyric(newIndex);
                }
            });
        }

        private void UpdateActiveLyric(int newIndex)
        {
            if (_activeLyricIndex >= 0 && _activeLyricIndex < LyricsContainer.Children.Count)
            {
                if (LyricsContainer.Children[_activeLyricIndex] is TextBlock previousBlock)
                {
                    previousBlock.Foreground = Brushes.White;
                    previousBlock.FontWeight = FontWeights.Normal;
                    previousBlock.Opacity = 0.45;
                    previousBlock.FontSize = _lyricFontSize;
                    previousBlock.Effect = CreateDynamicShadow(false);
                }
            }

            _activeLyricIndex = newIndex;

            if (_activeLyricIndex >= 0 && _activeLyricIndex < LyricsContainer.Children.Count)
            {
                if (LyricsContainer.Children[_activeLyricIndex] is TextBlock activeBlock)
                {
                    activeBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(_activeColorHex));
                    activeBlock.FontWeight = FontWeights.Bold;
                    activeBlock.Opacity = 1.0;
                    activeBlock.FontSize = _lyricFontSize + 3.0;
                    activeBlock.Effect = CreateDynamicShadow(true);

                    ScrollToCenter(activeBlock);
                }
            }
        }

        private DropShadowEffect CreateDynamicShadow(bool isActive)
        {
            var isBlack = _activeColorHex.Equals("#000000", StringComparison.OrdinalIgnoreCase);
            if (isActive && isBlack)
            {
                return new DropShadowEffect
                {
                    BlurRadius = 12,
                    ShadowDepth = 0,
                    Color = Colors.White,
                    Opacity = 0.95
                };
            }

            return new DropShadowEffect
            {
                BlurRadius = 10,
                ShadowDepth = 2,
                Color = Colors.Black,
                Opacity = 0.95
            };
        }

        private void ScrollToCenter(FrameworkElement element)
        {
            var transform = element.TransformToVisual(LyricsContainer);
            var elementLocation = transform.Transform(new Point(0, 0));
            var targetOffset = elementLocation.Y - (LyricsScrollViewer.ActualHeight / 2) + (element.ActualHeight / 2);

            if (targetOffset < 0) targetOffset = 0;
            LyricsScrollViewer.ScrollToVerticalOffset(targetOffset);
        }

        private void ShowEmptyState(string title, string subtitle)
        {
            LyricsScrollViewer.Visibility = Visibility.Collapsed;
            EmptyStatePanel.Visibility = Visibility.Visible;
            EmptyStateTitle.Text = title;
            EmptyStateSubtitle.Text = subtitle;
        }

        // --- Selección de Color (Botones Rápidos dentro del Popup) ---
        private void ColorSwatch_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string hex)
            {
                ApplyActiveColor(hex);
            }
        }

        // --- Toggle Popup con Slider de Colores ---
        private void ToggleColorSliderPopup_Click(object sender, RoutedEventArgs e)
        {
            ColorSliderPopup.IsOpen = !ColorSliderPopup.IsOpen;
        }

        private void ColorSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_isLoaded || HueSlider == null || LightnessSlider == null) return;

            var hue = HueSlider.Value;
            var lightness = LightnessSlider.Value;
            var color = ColorHelper.FromHsl(hue, 1.0, lightness);
            var hex = ColorHelper.ColorToHex(color);

            ApplyActiveColor(hex);
        }

        private void ApplyActiveColor(string hex)
        {
            _activeColorHex = hex;
            _settings.ActiveColorHex = hex;
            _settings.Save();

            UpdateColorPreview(hex);

            if (_activeLyricIndex >= 0 && _activeLyricIndex < LyricsContainer.Children.Count)
            {
                if (LyricsContainer.Children[_activeLyricIndex] is TextBlock activeBlock)
                {
                    activeBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(_activeColorHex));
                    activeBlock.Effect = CreateDynamicShadow(true);
                }
            }
        }

        private void UpdateColorPreview(string hex)
        {
            try
            {
                var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
                if (CurrentColorIndicator != null)
                {
                    CurrentColorIndicator.Background = brush;
                }
                if (ColorPreviewBox != null && HexCodeText != null)
                {
                    ColorPreviewBox.Background = brush;
                    HexCodeText.Text = hex;
                }
            }
            catch { }
        }

        // --- Manejo de Tamaño de Letra (A- / A+) ---
        private void IncreaseFontSize_Click(object sender, RoutedEventArgs e)
        {
            if (_lyricFontSize < 44.0)
            {
                _lyricFontSize += 2.0;
                _settings.FontSize = _lyricFontSize;
                _settings.Save();
                ApplyFontSizeToAll();
            }
        }

        private void DecreaseFontSize_Click(object sender, RoutedEventArgs e)
        {
            if (_lyricFontSize > 14.0)
            {
                _lyricFontSize -= 2.0;
                _settings.FontSize = _lyricFontSize;
                _settings.Save();
                ApplyFontSizeToAll();
            }
        }

        private void ApplyFontSizeToAll()
        {
            UpdateFontSizeLabel();
            for (int i = 0; i < LyricsContainer.Children.Count; i++)
            {
                if (LyricsContainer.Children[i] is TextBlock tb)
                {
                    tb.FontSize = (i == _activeLyricIndex) ? _lyricFontSize + 3.0 : _lyricFontSize;
                }
            }
        }

        private void UpdateFontSizeLabel()
        {
            FontSizeLabel.Text = $"{_lyricFontSize:0}px";
        }

        // --- Ventana de Ajustes ---
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SettingsDialog(_settings)
            {
                Owner = this
            };

            if (dialog.ShowDialog() == true)
            {
                // Re-cargar ajustes actualizados
                _settings = AppSettings.Load();
                _lyricFontSize = _settings.FontSize;
                _activeColorHex = _settings.ActiveColorHex;

                UpdateFontSizeLabel();
                ApplyFontSizeToAll();
                UpdateColorPreview(_activeColorHex);
                RegisterGhostHotKey();
            }
        }

        // --- Redimensionamiento de Ventana en Modo Edición ---
        private void Resize_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_isGhostMode) return;

            if (sender is FrameworkElement element && int.TryParse(element.Tag?.ToString(), out int dir))
            {
                WindowResizeHelper.ResizeWindow(this, (ResizeDirection)dir);
            }
        }

        private void SpotifyConnect_Click(object sender, RoutedEventArgs e)
        {
            if (_spotifyAuthService.Config.IsAuthenticated)
            {
                var result = ModernMessageBox.Show(
                    this,
                    "¿Deseas cerrar sesión de tu cuenta de Spotify en AirLyrics?", 
                    "Spotify Conectado", 
                    MessageBoxButton.YesNo, 
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _spotifyAuthService.Logout();
                    SpotifyConnectButton.ToolTip = "Conectar con Spotify";
                    ShowEmptyState("AirLyrics", "Haz clic en '🟢' para vincular Spotify");
                    TrackTitleBanner.Text = "AirLyrics";
                    TrackArtistAlbumBanner.Text = "Conecta Spotify para comenzar";
                }
                return;
            }

            var dialog = new SpotifyLoginDialog(_spotifyAuthService)
            {
                Owner = this
            };

            if (dialog.ShowDialog() == true)
            {
                SpotifyConnectButton.ToolTip = "Spotify Conectado (Haz clic para gestionar)";
                _spotifyPlaybackService.StartPolling();
            }
        }

        private void OverlayWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F2)
            {
                ToggleGhostMode();
            }
        }

        private void ToggleGhost_Click(object sender, RoutedEventArgs e)
        {
            ToggleGhostMode();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        public void ToggleGhostMode()
        {
            _isGhostMode = !_isGhostMode;
            ApplyGhostState();
        }

        private void ApplyGhostState()
        {
            if (_isGhostMode)
            {
                if (ColorSliderPopup != null) ColorSliderPopup.IsOpen = false;

                ControlHeader.Visibility = Visibility.Collapsed;
                ResizeGripIcon.Visibility = Visibility.Collapsed;
                
                RootBorder.Background = Brushes.Transparent;
                RootBorder.BorderThickness = new Thickness(0);

                SetResizeHandlesActive(false);

                WindowGhostHelper.SetGhostMode(this, true);
            }
            else
            {
                ControlHeader.Visibility = Visibility.Visible;
                ResizeGripIcon.Visibility = Visibility.Visible;

                RootBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F172A"));
                RootBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155"));
                RootBorder.BorderThickness = new Thickness(1);

                SetResizeHandlesActive(true);

                WindowGhostHelper.SetGhostMode(this, false);
            }
        }

        private void SetResizeHandlesActive(bool active)
        {
            var visibility = active ? Visibility.Visible : Visibility.Collapsed;
            ResizeTop.Visibility = visibility;
            ResizeBottom.Visibility = visibility;
            ResizeLeft.Visibility = visibility;
            ResizeRight.Visibility = visibility;
            ResizeTopLeft.Visibility = visibility;
            ResizeTopRight.Visibility = visibility;
            ResizeBottomLeft.Visibility = visibility;
            ResizeBottomRight.Visibility = visibility;
        }

        private void RootBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!_isGhostMode && e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }
    }
}
