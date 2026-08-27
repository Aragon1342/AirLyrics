using System.Windows;
using System.Windows.Media;

namespace AirLyrics.App.Views
{
    public partial class ModernMessageBox : Window
    {
        public MessageBoxResult Result { get; private set; } = MessageBoxResult.None;

        public ModernMessageBox()
        {
            InitializeComponent();
        }

        public static MessageBoxResult Show(
            Window? owner, 
            string message, 
            string title = "AirLyrics", 
            MessageBoxButton buttons = MessageBoxButton.OK, 
            MessageBoxImage image = MessageBoxImage.Information)
        {
            var dialog = new ModernMessageBox
            {
                Owner = owner
            };

            dialog.TitleText.Text = title;
            dialog.MessageText.Text = message;

            // Configurar Icono y Colores Vibrantes
            switch (image)
            {
                case MessageBoxImage.Warning:
                    dialog.IconText.Text = "⚠";
                    dialog.IconText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));
                    dialog.IconBadge.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#33EF4444"));
                    dialog.TitleText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));
                    break;

                case MessageBoxImage.Error:
                    dialog.IconText.Text = "✕";
                    dialog.IconText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));
                    dialog.IconBadge.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#33EF4444"));
                    dialog.TitleText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));
                    break;

                case MessageBoxImage.Question:
                    dialog.IconText.Text = "?";
                    dialog.IconText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C084FC"));
                    dialog.IconBadge.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#33C084FC"));
                    dialog.TitleText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C084FC"));
                    break;

                case MessageBoxImage.Information:
                default:
                    dialog.IconText.Text = "★";
                    dialog.IconText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FACC15"));
                    dialog.IconBadge.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#33FACC15"));
                    dialog.TitleText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#38BDF8"));
                    break;
            }

            // Configurar Botones
            switch (buttons)
            {
                case MessageBoxButton.YesNo:
                    dialog.PrimaryButton.Content = "Sí";
                    dialog.PrimaryButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));
                    dialog.PrimaryButton.Foreground = Brushes.White;
                    dialog.SecondaryButton.Content = "No";
                    dialog.SecondaryButton.Visibility = Visibility.Visible;
                    break;

                case MessageBoxButton.OKCancel:
                    dialog.PrimaryButton.Content = "Aceptar";
                    dialog.SecondaryButton.Content = "Cancelar";
                    dialog.SecondaryButton.Visibility = Visibility.Visible;
                    break;

                case MessageBoxButton.OK:
                default:
                    dialog.PrimaryButton.Content = "Entendido";
                    dialog.SecondaryButton.Visibility = Visibility.Collapsed;
                    break;
            }

            dialog.ShowDialog();
            return dialog.Result;
        }

        private void PrimaryButton_Click(object sender, RoutedEventArgs e)
        {
            Result = SecondaryButton.Visibility == Visibility.Visible ? MessageBoxResult.Yes : MessageBoxResult.OK;
            DialogResult = true;
            Close();
        }

        private void SecondaryButton_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.No;
            DialogResult = false;
            Close();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.Cancel;
            DialogResult = false;
            Close();
        }
    }
}
