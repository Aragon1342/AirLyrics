using System;
using System.IO;
using System.Text.Json;
using AirLyrics.App.Native;

namespace AirLyrics.App.Models
{
    public class AppSettings
    {
        public double FontSize { get; set; } = 22.0;
        public string ActiveColorHex { get; set; } = "#38BDF8";
        
        // Atajo Global para Modo Fantasma
        public KeyModifiers GhostModifier { get; set; } = KeyModifiers.Control | KeyModifiers.Alt;
        public uint GhostVirtualKey { get; set; } = 0x47; // 0x47 = 'G'
        public string GhostShortcutText { get; set; } = "Ctrl + Alt + G";

        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AirLyrics",
            "app_settings.json");

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var json = File.ReadAllText(SettingsPath);
                    return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cargar configuración: {ex.Message}");
            }
            return new AppSettings();
        }

        public void Save()
        {
            try
            {
                var dir = Path.GetDirectoryName(SettingsPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al guardar configuración: {ex.Message}");
            }
        }

        public void ResetToDefaults()
        {
            FontSize = 22.0;
            ActiveColorHex = "#38BDF8";
            GhostModifier = KeyModifiers.Control | KeyModifiers.Alt;
            GhostVirtualKey = 0x47;
            GhostShortcutText = "Ctrl + Alt + G";
            Save();
        }
    }
}
