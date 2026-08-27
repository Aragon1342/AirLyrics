using System;
using System.Diagnostics;
using System.Windows;
using AirLyrics.App.Services.Spotify;

namespace AirLyrics.App.Views
{
    public partial class SpotifyLoginDialog : Window
    {
        private readonly SpotifyAuthService _authService;

        public SpotifyLoginDialog(SpotifyAuthService authService)
        {
            InitializeComponent();
            _authService = authService;
            ClientIdInput.Text = _authService.Config.ClientId;
        }

        private void OpenDashboard_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://developer.spotify.com/dashboard",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir el navegador: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void StartLogin_Click(object sender, RoutedEventArgs e)
        {
            var clientId = ClientIdInput.Text.Trim();
            if (string.IsNullOrEmpty(clientId))
            {
                StatusMessage.Text = "Por favor ingresa un Spotify Client ID válido.";
                return;
            }

            LoginButton.IsEnabled = false;
            StatusMessage.Foreground = System.Windows.Media.Brushes.DeepSkyBlue;
            StatusMessage.Text = "Esperando autorización en el navegador...";

            try
            {
                var success = await _authService.StartLoginAsync(clientId);
                if (success)
                {
                    DialogResult = true;
                    Close();
                }
                else
                {
                    StatusMessage.Foreground = System.Windows.Media.Brushes.Salmon;
                    StatusMessage.Text = "La autorización fue cancelada o no se pudo completar.";
                    LoginButton.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                StatusMessage.Foreground = System.Windows.Media.Brushes.Salmon;
                StatusMessage.Text = $"Error: {ex.Message}";
                LoginButton.IsEnabled = true;
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
