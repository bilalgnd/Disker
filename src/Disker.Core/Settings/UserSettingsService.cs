using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Disker.Core.Models;

namespace Disker.Core.Settings
{
    public class UserSettings
    {
        public string Language { get; set; } = "tr"; // "tr" veya "en"
        public bool ShowSystemPartitions { get; set; } = true;
        public List<string> DiskOrder { get; set; } = new();
        public Dictionary<string, string> DiskColors { get; set; } = new();
        public List<string> ProtectedDiskIds { get; set; } = new();
    }

    public class UserSettingsService
    {
        private readonly string _settingsFilePath;

        public UserSettingsService()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string diskerDir = Path.Combine(appData, "Disker");
            Directory.CreateDirectory(diskerDir);
            _settingsFilePath = Path.Combine(diskerDir, "disker_settings.json");
        }

        public UserSettings LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsFilePath))
                {
                    string json = File.ReadAllText(_settingsFilePath);
                    return JsonSerializer.Deserialize<UserSettings>(json) ?? new UserSettings();
                }
            }
            catch { }

            return new UserSettings();
        }

        public void SaveSettings(UserSettings settings)
        {
            try
            {
                string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_settingsFilePath, json);
            }
            catch { }
        }

        public static string GetDiskId(PhysicalDiskInfo disk)
        {
            if (!string.IsNullOrWhiteSpace(disk.SerialNumber))
                return $"{disk.FriendlyName}_{disk.SerialNumber}";
            return $"{disk.FriendlyName}_Disk{disk.DiskNumber}";
        }
    }
}
