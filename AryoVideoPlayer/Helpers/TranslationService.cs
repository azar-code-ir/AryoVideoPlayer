using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using AryoVideoPlayer.Models;

namespace AryoVideoPlayer.Helpers
{
    // سرویس ترجمه با استفاده از Google Translate رایگان
    public static class TranslationService
    {
        private static readonly HttpClient _client = new HttpClient();

        // ترجمه متن از یک زبان به زبان دیگر
        public static async Task<string> TranslateText(string text, string sourceLang, string targetLang)
        {
            try
            {
                string url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl={sourceLang}&tl={targetLang}&dt=t&q={HttpUtility.UrlEncode(text)}";
                HttpResponseMessage response = await _client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                string result = await response.Content.ReadAsStringAsync();
                return ParseTranslationResponse(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Translation error: {ex.Message}");
                return text;
            }
        }

        // ترجمه تمام زیرنویس‌ها با پیشرفت خط‌به‌خط
        public static async Task<List<SubtitleEntry>> TranslateSubtitles(
            List<SubtitleEntry> entries,
            string sourceLang,
            string targetLang,
            Action<int, int> progressCallback = null)
        {
            List<SubtitleEntry> translatedEntries = new List<SubtitleEntry>();

            for (int i = 0; i < entries.Count; i++)
            {
                string translatedText = await TranslateText(entries[i].Text, sourceLang, targetLang);

                SubtitleEntry translatedEntry = new SubtitleEntry
                {
                    Index = entries[i].Index,
                    StartTime = entries[i].StartTime,
                    EndTime = entries[i].EndTime,
                    Text = entries[i].Text,
                    TranslatedText = translatedText
                };

                translatedEntries.Add(translatedEntry);
                progressCallback?.Invoke(i + 1, entries.Count);

                // تأخیر برای جلوگیری از محدودیت سرور
                if (i < entries.Count - 1)
                    await Task.Delay(150);
            }

            return translatedEntries;
        }

        // پارس کردن پاسخ JSON با System.Text.Json
        private static string ParseTranslationResponse(string response)
        {
            try
            {
                using var doc = JsonDocument.Parse(response);
                var root = doc.RootElement;

                // پاسخ: [[["ترجمه","متن اصلی",...],...],...]
                if (root.GetArrayLength() > 0)
                {
                    var firstBlock = root[0];
                    if (firstBlock.GetArrayLength() > 0)
                    {
                        var firstSentence = firstBlock[0];
                        if (firstSentence.GetArrayLength() > 0)
                        {
                            return firstSentence[0].GetString() ?? "";
                        }
                    }
                }

                return response;
            }
            catch
            {
                // fallback: جستجوی رشته
                return FallbackParse(response);
            }
        }

        // پارس جایگزین در صورت خطا
        private static string FallbackParse(string response)
        {
            try
            {
                int startIndex = response.IndexOf("[[\"");
                if (startIndex == -1) return response;

                int endIndex = response.IndexOf("\"", startIndex + 4);
                if (endIndex == -1) return response;

                return response.Substring(startIndex + 4, endIndex - startIndex - 4);
            }
            catch
            {
                return response;
            }
        }
    }
}
