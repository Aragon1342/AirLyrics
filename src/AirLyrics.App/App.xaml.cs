using System;
using System.Windows;

namespace AirLyrics.App
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                if (args.ExceptionObject is Exception ex)
                {
                    MessageBox.Show($"Error fatal en la aplicación:\n\n{ex.Message}\n\n{ex.StackTrace}", "Error - AirLyrics", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };

            DispatcherUnhandledException += (s, args) =>
            {
                MessageBox.Show($"Error en interfaz:\n\n{args.Exception.Message}\n\n{args.Exception.StackTrace}", "Error - AirLyrics", MessageBoxButton.OK, MessageBoxImage.Error);
                args.Handled = true;
            };

            base.OnStartup(e);
        }
    }
}
