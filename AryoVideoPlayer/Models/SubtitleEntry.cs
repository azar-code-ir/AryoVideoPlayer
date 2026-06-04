using System;

namespace AryoVideoPlayer.Models
{
    // مدل هر خط زیرنویس
    public class SubtitleEntry
    {
        public int Index { get; set; }           // شماره ردیف زیرنویس
        public TimeSpan StartTime { get; set; }   // زمان شروع
        public TimeSpan EndTime { get; set; }     // زمان پایان
        public string Text { get; set; } = "";    // متن زیرنویس
        public string TranslatedText { get; set; } = ""; // متن ترجمه شده
    }
}
