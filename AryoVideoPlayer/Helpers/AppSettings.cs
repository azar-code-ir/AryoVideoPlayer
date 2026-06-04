using System;
using System.IO;
using Newtonsoft.Json;

namespace AryoVideoPlayer.Helpers
{
    // کلاس ذخیره‌سازی تنظیمات
    public class AppSettings
    {
        public double SubtitleFontSize { get; set; } = 28;
        public bool DualSubtitle { get; set; } = false;
        public double Volume { get; set; } = 80;
        public string LastVideoPath { get; set; } = "";

        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AryoVideoPlayer", "settings.json");

        // بارگذاری تنظیمات
        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    string json = File.ReadAllText(SettingsPath);
                    return JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch { }

            return new AppSettings();
        }

        // ذخیره تنظیمات
        public void Save()
        {
            try
            {
                string json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(SettingsPath, json);
            }
            catch { }
        }
    }
}
