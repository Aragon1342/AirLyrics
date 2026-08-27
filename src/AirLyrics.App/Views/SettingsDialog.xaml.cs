using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using AirLyrics.App.Models;
using AirLyrics.App.Native;

namespace AirLyrics.App.Views
{
    public partial class SettingsDialog : Window
    {
        private readonly AppSettings _settings;
        private uint _selectedVirtualKey = 0x47; // 'G' por defecto
        private string _selectedKeyName = "G";
        private bool _isListeningForKey = false;
        private bool _isLoaded = false;

        public SettingsDialog(AppSettings settings)
        {
            InitializeComponent();
            _settings = settings;

            Loaded += SettingsDialog_Loaded;
        }

        private void SettingsDialog_Loaded(object sender, RoutedEventArgs e)
        {
            LoadCurrentSettings();
            _isLoaded = true;
            UpdateShortcutPreview();
        }

        private void LoadCurrentSettings()
        {
            if (ChkCtrl == null || ChkAlt == null || ChkShift == null || ChkWin == null) return;

            ChkCtrl.IsChecked = _settings.GhostModifier.HasFlag(KeyModifiers.Control);
            ChkAlt.IsChecked = _settings.GhostModifier.HasFlag(KeyModifiers.Alt);
            ChkShift.IsChecked = _settings.GhostModifier.HasFlag(KeyModifiers.Shift);
            ChkWin.IsChecked = _settings.GhostModifier.HasFlag(KeyModifiers.Windows);

            _selectedVirtualKey = _settings.GhostVirtualKey != 0 ? _settings.GhostVirtualKey : 0x47;
            _selectedKeyName = FormatVirtualKeyName(_selectedVirtualKey);

            if (KeyBindingText != null)
            {
                KeyBindingText.Text = $"[ {_selectedKeyName} ]";
            }
        }

        private void ShortcutChanged(object sender, RoutedEventArgs e)
        {
            if (_isLoaded)
            {
                UpdateShortcutPreview();
            }
        }

        private void KeyBindingButton_Click(object sender, RoutedEventArgs e)
        {
            StartListeningForKey();
        }

        private void StartListeningForKey()
        {
            _isListeningForKey = true;
            if (KeyBindingText != null)
            {
                KeyBindingText.Text = "🎮 Presiona cualquier tecla...";
            }
            if (KeyBindingButton != null)
            {
                KeyBindingButton.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#38BDF8"));
                KeyBindingButton.Focus();
            }
        }

        private void StopListeningForKey()
        {
            _isListeningForKey = false;
            if (KeyBindingText != null)
            {
                KeyBindingText.Text = $"[ {_selectedKeyName} ]";
            }
            if (KeyBindingButton != null)
            {
                KeyBindingButton.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155"));
            }
            UpdateShortcutPreview();
        }

        private void KeyBindingButton_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!_isListeningForKey) return;

            e.Handled = true;

            var key = e.Key == Key.System ? e.SystemKey : e.Key;

            // Ignorar pulsaciones de solo teclas modificadoras
            if (key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin)
            {
                return;
            }

            if (key == Key.Escape)
            {
                StopListeningForKey();
                return;
            }

            var virtualKey = (uint)KeyInterop.VirtualKeyFromKey(key);
            if (virtualKey > 0)
            {
                _selectedVirtualKey = virtualKey;
                _selectedKeyName = FormatKeyName(key, virtualKey);
                StopListeningForKey();
            }
        }

        private void KeyBindingButton_LostFocus(object sender, RoutedEventArgs e)
        {
            if (_isListeningForKey)
            {
                StopListeningForKey();
            }
        }

        private static string FormatKeyName(Key key, uint virtualKey)
        {
            if (key >= Key.A && key <= Key.Z)
            {
                return key.ToString();
            }

            if (key >= Key.F1 && key <= Key.F24)
            {
                return key.ToString();
            }

            if (key >= Key.D0 && key <= Key.D9)
            {
                return key.ToString().Substring(1);
            }

            if (key >= Key.NumPad0 && key <= Key.NumPad9)
            {
                return $"Num {key.ToString().Substring(6)}";
            }

            return key switch
            {
                Key.Space => "Espacio",
                Key.Tab => "Tab",
                Key.Insert => "Insert",
                Key.Delete => "Supr",
                Key.Home => "Inicio",
                Key.End => "Fin",
                Key.PageUp => "RePág",
                Key.PageDown => "AvPág",
                Key.Back => "Retroceso",
                _ => key.ToString()
            };
        }

        private static string FormatVirtualKeyName(uint virtualKey)
        {
            var key = KeyInterop.KeyFromVirtualKey((int)virtualKey);
            return FormatKeyName(key, virtualKey);
        }

        private void UpdateShortcutPreview()
        {
            if (ShortcutPreviewText == null) return;

            var parts = new List<string>();
            if (ChkCtrl?.IsChecked == true) parts.Add("Ctrl");
            if (ChkAlt?.IsChecked == true) parts.Add("Alt");
            if (ChkShift?.IsChecked == true) parts.Add("Shift");
            if (ChkWin?.IsChecked == true) parts.Add("Win");

            if (!string.IsNullOrEmpty(_selectedKeyName))
            {
                parts.Add(_selectedKeyName);
            }

            ShortcutPreviewText.Text = parts.Count > 0 ? string.Join(" + ", parts) : "(Ninguno seleccionado)";
        }

        private void ResetDefaults_Click(object sender, RoutedEventArgs e)
        {
            var result = ModernMessageBox.Show(
                this,
                "¿Estás seguro de que deseas restablecer todos los ajustes (atajos, color y tamaño de fuente) a los valores de fábrica?",
                "Restablecer Valores de Fábrica",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                _settings.ResetToDefaults();
                LoadCurrentSettings();
                UpdateShortcutPreview();

                ModernMessageBox.Show(
                    this,
                    "Los ajustes se han restablecido correctamente a los valores por defecto.",
                    "Ajustes Restablecidos",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var modifier = KeyModifiers.None;
            if (ChkCtrl?.IsChecked == true) modifier |= KeyModifiers.Control;
            if (ChkAlt?.IsChecked == true) modifier |= KeyModifiers.Alt;
            if (ChkShift?.IsChecked == true) modifier |= KeyModifiers.Shift;
            if (ChkWin?.IsChecked == true) modifier |= KeyModifiers.Windows;

            if (_selectedVirtualKey != 0)
            {
                _settings.GhostModifier = modifier;
                _settings.GhostVirtualKey = _selectedVirtualKey;
                _settings.GhostShortcutText = ShortcutPreviewText?.Text ?? "Ctrl + Alt + G";
                _settings.Save();

                DialogResult = true;
                Close();
            }
            else
            {
                ModernMessageBox.Show(this, "Por favor presiona una tecla para asignar el atajo.", "Atención", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
