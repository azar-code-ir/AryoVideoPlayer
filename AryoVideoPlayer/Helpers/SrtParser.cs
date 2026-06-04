using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using AryoVideoPlayer.Models;

namespace AryoVideoPlayer.Helpers
{
    // کلاس خواندن فایل زیرنویس SRT
    public static class SrtParser
    {
        // خواندن فایل SRT و برگرداندن لیست زیرنویس‌ها
        public static List<SubtitleEntry> Parse(string filePath)
        {
            var entries = new List<SubtitleEntry>();

            // بررسی وجود فایل
            if (!File.Exists(filePath))
                return entries;

            // خواندن تمام خطوط فایل
            string content = File.ReadAllText(filePath);

            // جداسازی بلوک‌های زیرنویس با دو خط خالی
            string[] blocks = Regex.Split(content, @"\r?\n\r?\n");

            foreach (string block in blocks)
            {
                string[] lines = block.Trim().Split('\n');

                // هر بلوک حداقل ۳ خط دارد: شماره، زمان، متن
                if (lines.Length < 2) continue;

                // پیدا کردن خط زمان (شامل -->)
                int timeLineIndex = -1;
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Contains("-->"))
                    {
                        timeLineIndex = i;
                        break;
                    }
                }

                if (timeLineIndex == -1) continue;

                // پارس کردن زمان شروع و پایان
                string timeLine = lines[timeLineIndex].Trim();
                string[] times = timeLine.Split(new[] { "-->" }, StringSplitOptions.None);

                if (times.Length != 2) continue;

                SubtitleEntry entry = new SubtitleEntry
                {
                    Index = entries.Count + 1,
                    StartTime = ParseTime(times[0].Trim()),
                    EndTime = ParseTime(times[1].Trim()),
                    Text = string.Join("\n", lines, timeLineIndex + 1, lines.Length - timeLineIndex - 1).Trim()
                };

                entries.Add(entry);
            }

            return entries;
        }

        // تبدیل متن زمان به TimeSpan
        // فرمت: 00:01:23,456 یا 00:01:23.456
        private static TimeSpan ParseTime(string timeStr)
        {
            // جایگزینی نقطه با کاما
            timeStr = timeStr.Replace('.', ',');

            // حذف کاراکترهای اضافی
            timeStr = timeStr.Trim();

            if (TimeSpan.TryParse(timeStr, out TimeSpan result))
                return result;

            // پارس دستی اگر Parse موفق نبود
            try
            {
                string[] parts = timeStr.Split(',', ':');
                int hours = int.Parse(parts[0]);
                int minutes = int.Parse(parts[1]);
                int seconds = int.Parse(parts[2]);
                int milliseconds = parts.Length > 3 ? int.Parse(parts[3].PadRight(3, '0').Substring(0, 3)) : 0;

                return new TimeSpan(0, hours, minutes, seconds, milliseconds);
            }
            catch
            {
                return TimeSpan.Zero;
            }
        }

        // ذخیره فایل SRT
        public static void Save(string filePath, List<SubtitleEntry> entries, bool saveTranslated = false)
        {
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    writer.WriteLine(entries[i].Index);
                    writer.WriteLine($"{FormatTime(entries[i].StartTime)} --> {FormatTime(entries[i].EndTime)}");
                    // اگر ترجمه موجود باشد و درخواست شده باشد، متن ترجمه ذخیره شود
                    if (saveTranslated && !string.IsNullOrEmpty(entries[i].TranslatedText))
                        writer.WriteLine(entries[i].TranslatedText);
                    else
                        writer.WriteLine(entries[i].Text);
                    writer.WriteLine();
                }
            }
        }

        // تبدیل TimeSpan به فرمت SRT
        private static string FormatTime(TimeSpan time)
        {
            return $"{time.Hours:D2}:{time.Minutes:D2}:{time.Seconds:D2},{time.Milliseconds:D3}";
        }
    }
}
