using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Vosk;

namespace AryoVideoPlayer
{
    public partial class AudioToTextWindow : Window
    {
        public string? CurrentVideoPath { get; set; }
        public string? SavedSubtitlePath { get; private set; }
        private CancellationTokenSource? _cts;

        private static readonly string _appDir = AppDomain.CurrentDomain.BaseDirectory;
        private static readonly string _ffmpegPath = Path.Combine(_appDir, "ffmpeg.exe");
        private static readonly string _modelsDir = Path.Combine(_appDir, "vosk_models");

        private static readonly (string langCode, string modelName)[] _models = {
            ("en", "vosk-model-en-us-0.22"),
            ("fa", "vosk-model-small-fa-0.5"),
            ("fr", "vosk-model-small-fr-0.22"),
            ("de", "vosk-model-small-de-0.22"),
            ("es", "vosk-model-small-es-0.42"),
            ("it", "vosk-model-small-it-0.22"),
            ("ja", "vosk-model-small-ja-0.22"),
            ("ko", "vosk-model-small-ko-0.22"),
            ("zh", "vosk-model-small-cn-0.22"),
            ("ar", "vosk-model-ar-mgb-0.4"),
            ("tr", "vosk-model-small-tr-0.3"),
            ("ru", "vosk-model-small-ru-0.22"),
        };

        private static readonly string[] _sourceLangCodes = {
            "en", "fa", "fr", "de", "es", "it", "ja", "ko", "zh", "ar", "tr", "ru"
        };

        private static readonly string[] _targetLangCodes = {
            "fa", "en", "fr", "de", "es", "it", "ja", "ko", "zh", "ar", "tr", "ru"
        };

        public AudioToTextWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(CurrentVideoPath) && File.Exists(CurrentVideoPath))
                VideoNameText.Text = Path.GetFileName(CurrentVideoPath);
            else
                VideoNameText.Text = "هیچ ویدیویی پخش نیست";
        }

        // ═══════════════════════════════════════════════
        //  دکمه ترجمه
        // ═══════════════════════════════════════════════

        private async void TranslateBtn_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(CurrentVideoPath) || !File.Exists(CurrentVideoPath))
            {
                DarkMessageBox.Show("هیچ ویدیویی در حال پخش نیست!", "خطا");
                return;
            }

            TranslateBtn.IsEnabled = false;
            ProgressBar.Visibility = Visibility.Visible;
            ProgressBar.IsIndeterminate = true;
            _cts = new CancellationTokenSource();

            try
            {
                await Task.Run(() => RunTranslation(_cts.Token), _cts.Token);
            }
            catch (OperationCanceledException)
            {
                Log("لغو شد");
            }
            catch (Exception ex)
            {
                Log("خطا: " + ex.Message);
                Dispatcher.Invoke(() => DarkMessageBox.ShowError(ex.Message, "خطا"));
            }
            finally
            {
                TranslateBtn.IsEnabled = true;
                ProgressBar.Visibility = Visibility.Collapsed;
                ProgressBar.IsIndeterminate = false;
                _cts?.Dispose();
                _cts = null;
            }
        }

        // ═══════════════════════════════════════════════
        //  متد اصلی
        // ═══════════════════════════════════════════════

        private void RunTranslation(CancellationToken ct)
        {
            string sourceLang = "";
            string targetLang = "";
            Dispatcher.Invoke(() =>
            {
                sourceLang = _sourceLangCodes[SourceLangComboBox.SelectedIndex];
                targetLang = _targetLangCodes[TargetLangComboBox.SelectedIndex];
            });

            if (sourceLang == targetLang)
            {
                Log("زبان مبدأ و مقصد یکسانه!");
                return;
            }

            // ── ۱: ffmpeg ──
            if (!File.Exists(_ffmpegPath))
            {
                Log("ffmpeg پیدا نشد. دانلود خودکار...");
                if (!DownloadFfmpegSync(ct))
                {
                    Log("خطا: ffmpeg دانلود نشد");
                    return;
                }
            }
            Log("ffmpeg آماده");

            // ── ۲: تلاش برای استخراج زیرنویس embedded ──
            Log("بررسی زیرنویس embedded...");
            var embeddedSubs = ExtractEmbeddedSubtitles(ct);

            if (embeddedSubs.Count > 0)
            {
                Log($"{embeddedSubs.Count} خط زیرنویس embedded یافت شد!");
                // ترجمه زیرنویس‌های موجود
                Log($"ترجمه {sourceLang} → {targetLang}...");
                for (int i = 0; i < embeddedSubs.Count; i++)
                {
                    ct.ThrowIfCancellationRequested();
                    embeddedSubs[i].TranslatedText = Helpers.TranslationService.TranslateText(
                        embeddedSubs[i].Text, sourceLang, targetLang).GetAwaiter().GetResult();
                    if (i % 10 == 0)
                        Log($"[{i + 1}/{embeddedSubs.Count}] ترجمه شد");
                    Thread.Sleep(50);
                }
                SaveSrt(embeddedSubs, targetLang, ct);
                return;
            }

            Log("زیرنویس embedded یافت نشد. تبدیل صدا به متن...");

            // ── ۳: مدل Vosk ──
            Directory.CreateDirectory(_modelsDir);
            string modelName = "";
            foreach (var m in _models)
            {
                if (m.langCode == sourceLang)
                {
                    modelName = m.modelName;
                    break;
                }
            }

            if (string.IsNullOrEmpty(modelName))
            {
                Log("مدلی برای این زبان یافت نشد");
                return;
            }

            string modelPath = Path.Combine(_modelsDir, modelName);
            if (!Directory.Exists(modelPath))
            {
                Log($"مدل {modelName} دانلود نشده. دانلود...");
                if (!DownloadModelSync(modelName, ct))
                {
                    Log("خطا: مدل دانلود نشد");
                    return;
                }
            }
            Log($"مدل {modelName} آماده");

            // ── ۴: استخراج صدا ──
            Log("استخراج صدا...");
            string tempWav = Path.Combine(Path.GetTempPath(), $"ariyo_{Guid.NewGuid()}.wav");
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = _ffmpegPath,
                    Arguments = $"-i \"{CurrentVideoPath}\" -vn -acodec pcm_s16le -ar 16000 -ac 1 \"{tempWav}\" -y",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                };
                using var process = Process.Start(psi)!;
                // خواندن stderr تا هنگ نکنه
                string stderr = process.StandardError.ReadToEnd();
                process.WaitForExit(300000); // 5 دقیقه timeout

                if (!File.Exists(tempWav))
                {
                    Log("خطا: فایل صدا ساخته نشد");
                    return;
                }

                long wavSize = new FileInfo(tempWav).Length;
                Log($"صدا: {wavSize / 1024 / 1024}MB");

                if (wavSize < 1000)
                {
                    Log("خطا: فایل صدا خالیه");
                    return;
                }

                // ── ۵: بازشناسی گفتار ──
                Log("بازشناسی گفتار...");
                using var model = new Model(modelPath);
                using var recognizer = new VoskRecognizer(model, 16000.0f);

                var entries = new System.Collections.Generic.List<SrtEntry>();
                using (var fs = new FileStream(tempWav, FileMode.Open, FileAccess.Read))
                {
                    byte[] buf = new byte[8192];
                    int n;
                    while ((n = fs.Read(buf, 0, buf.Length)) > 0)
                    {
                        recognizer.AcceptWaveform(buf, n);
                    }
                    string finalJson = recognizer.FinalResult();
                    Log("JSON خروجی: " + (finalJson.Length > 200 ? finalJson.Substring(0, 200) : finalJson));
                    var words = ParseVoskWords(finalJson);
                    if (words.Count > 0)
                        entries.AddRange(SplitIntoSubtitles(words));
                }

                if (entries.Count == 0)
                {
                    Log("متنی یافت نشد. مدل با فرمت صدا سازگار نیست.");
                    Log("پیشنهاد: از زیرنویس embedded استفاده کنید.");
                    return;
                }
                Log($"{entries.Count} خط یافت شد");

                // ── ۶: ترجمه ──
                Log($"ترجمه {sourceLang} → {targetLang}...");
                for (int i = 0; i < entries.Count; i++)
                {
                    ct.ThrowIfCancellationRequested();
                    entries[i].TranslatedText = Helpers.TranslationService.TranslateText(
                        entries[i].Text, sourceLang, targetLang).GetAwaiter().GetResult();
                    if (i % 10 == 0)
                        Log($"[{i + 1}/{entries.Count}] ترجمه شد");
                    Thread.Sleep(50);
                }

                // ── ۷: ذخیره ──
                SaveSrt(entries, targetLang, ct);
            }
            finally
            {
                if (File.Exists(tempWav))
                    File.Delete(tempWav);
            }
        }

        // ═══════════════════════════════════════════════
        //  استخراج زیرنویس embedded از ویدیو
        // ═══════════════════════════════════════════════

        private System.Collections.Generic.List<SrtEntry> ExtractEmbeddedSubtitles(CancellationToken ct)
        {
            var entries = new System.Collections.Generic.List<SrtEntry>();
            string tempSrt = Path.Combine(Path.GetTempPath(), $"ariyo_sub_{Guid.NewGuid()}.srt");

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = _ffmpegPath,
                    Arguments = $"-i \"{CurrentVideoPath}\" -map 0:s? -c:s srt \"{tempSrt}\" -y",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                };
                using var process = Process.Start(psi)!;
                // خواندن stderr تا process هنگ نکنه
                string stderr = process.StandardError.ReadToEnd();
                process.WaitForExit(10000); // 10 ثانیه timeout

                if (process.HasExited && File.Exists(tempSrt) && new FileInfo(tempSrt).Length > 10)
                {
                    string content = File.ReadAllText(tempSrt, Encoding.UTF8);
                    entries = ParseSrt(content);
                    Log($"{entries.Count} خط زیرنویس embedded");
                }
                else
                {
                    Log("زیرنویس embedded یافت نشد");
                }

                File.Delete(tempSrt);
            }
            catch (Exception ex)
            {
                Log("خطا: " + ex.Message);
            }

            return entries;
        }

        // ═══════════════════════════════════════════════
        //  پارس SRT
        // ═══════════════════════════════════════════════

        private static System.Collections.Generic.List<SrtEntry> ParseSrt(string content)
        {
            var entries = new System.Collections.Generic.List<SrtEntry>();
            var blocks = content.Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var block in blocks)
            {
                var lines = block.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length < 2) continue;

                int timeIdx = -1;
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Contains("-->"))
                    {
                        timeIdx = i;
                        break;
                    }
                }
                if (timeIdx < 0) continue;

                var parts = lines[timeIdx].Split(new[] { "-->" }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2) continue;

                if (TryParseTime(parts[0].Trim(), out double start) &&
                    TryParseTime(parts[1].Trim().Split(' ')[0], out double end))
                {
                    string text = string.Join("\n", lines, timeIdx + 1, lines.Length - timeIdx - 1);
                    entries.Add(new SrtEntry { Start = start, End = end, Text = text });
                }
            }
            return entries;
        }

        private static bool TryParseTime(string s, out double seconds)
        {
            seconds = 0;
            try
            {
                s = s.Replace(',', '.');
                var p = s.Split(':');
                if (p.Length == 3)
                {
                    seconds = double.Parse(p[0]) * 3600 + double.Parse(p[1]) * 60 + double.Parse(p[2]);
                    return true;
                }
            }
            catch { }
            return false;
        }

        // ═══════════════════════════════════════════════
        //  ذخیره SRT
        // ═══════════════════════════════════════════════

        private void SaveSrt(System.Collections.Generic.List<SrtEntry> entries, string lang, CancellationToken ct)
        {
            // ذخیره خودکار کنار ویدیو
            string videoDir = Path.GetDirectoryName(CurrentVideoPath!)!;
            string videoName = Path.GetFileNameWithoutExtension(CurrentVideoPath);
            string srtPath = Path.Combine(videoDir, $"{videoName}.{lang}.srt");

            var sb = new StringBuilder();
            int idx = 1;
            foreach (var e in entries)
            {
                sb.AppendLine($"{idx++}");
                sb.AppendLine($"{Fmt(e.Start)} --> {Fmt(e.End)}");
                sb.AppendLine(!string.IsNullOrEmpty(e.TranslatedText) ? e.TranslatedText : e.Text);
                sb.AppendLine();
            }
            File.WriteAllText(srtPath, sb.ToString(), Encoding.UTF8);
            SavedSubtitlePath = srtPath;

            Log($"ذخیره شد: {srtPath}");
            Dispatcher.Invoke(() =>
                DarkMessageBox.Show($"زیرنویس ذخیره شد:\n{srtPath}", "تکمیل"));
        }

        private static string Fmt(double s)
        {
            var ts = TimeSpan.FromSeconds(s);
            return $"{ts.Hours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2},{ts.Milliseconds:D3}";
        }

        // ═══════════════════════════════════════════════
        //  Vosk helpers
        // ═══════════════════════════════════════════════

        private static System.Collections.Generic.List<VoskWord> ParseVoskWords(string json)
        {
            var words = new System.Collections.Generic.List<VoskWord>();
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("result", out var arr))
                {
                    foreach (var item in arr.EnumerateArray())
                    {
                        var w = new VoskWord
                        {
                            Word = item.TryGetProperty("word", out var ww) ? ww.GetString() ?? "" : "",
                            Start = item.TryGetProperty("start", out var s) ? s.GetDouble() : 0,
                            End = item.TryGetProperty("end", out var e) ? e.GetDouble() : 0
                        };
                        if (!string.IsNullOrWhiteSpace(w.Word))
                            words.Add(w);
                    }
                }
            }
            catch { }
            return words;
        }

        private static System.Collections.Generic.List<SrtEntry> SplitIntoSubtitles(
            System.Collections.Generic.List<VoskWord> words)
        {
            var entries = new System.Collections.Generic.List<SrtEntry>();
            var cur = new System.Collections.Generic.List<VoskWord>();

            foreach (var w in words)
            {
                cur.Add(w);
                bool split = false;
                double dur = w.End - cur[0].Start;
                if (dur > 7.0) split = true;
                if (cur.Count >= 12 && dur > 3.0) split = true;
                if (w.Word.EndsWith('.') || w.Word.EndsWith('؟') || w.Word.EndsWith('!'))
                    if (dur > 1.5 || cur.Count >= 4) split = true;

                if (split)
                {
                    entries.Add(new SrtEntry { Start = cur[0].Start, End = cur[cur.Count - 1].End,
                        Text = string.Join(" ", cur.ConvertAll(x => x.Word)) });
                    cur = new System.Collections.Generic.List<VoskWord>();
                }
            }
            if (cur.Count > 0)
                entries.Add(new SrtEntry { Start = cur[0].Start, End = cur[cur.Count - 1].End,
                    Text = string.Join(" ", cur.ConvertAll(x => x.Word)) });

            return entries;
        }

        // ═══════════════════════════════════════════════
        //  دانلود ffmpeg
        // ═══════════════════════════════════════════════

        private bool DownloadFfmpegSync(CancellationToken ct)
        {
            try
            {
                string zipPath = Path.Combine(Path.GetTempPath(), "ffmpeg_ariyo.zip");
                string url = "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip";

                Log("دانلود ffmpeg (~80MB)...");

                using (var client = new HttpClient { Timeout = TimeSpan.FromMinutes(15) })
                {
                    var response = client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct).GetAwaiter().GetResult();
                    if (!response.IsSuccessStatusCode) return false;

                    long totalBytes = response.Content.Headers.ContentLength ?? 0;
                    using var httpStream = response.Content.ReadAsStreamAsync(ct).GetAwaiter().GetResult();
                    using var fileStream = File.Create(zipPath);

                    byte[] buffer = new byte[65536];
                    long totalRead = 0;
                    int bytesRead;
                    int lastPct = -1;

                    while ((bytesRead = httpStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        ct.ThrowIfCancellationRequested();
                        fileStream.Write(buffer, 0, bytesRead);
                        totalRead += bytesRead;
                        if (totalBytes > 0)
                        {
                            int pct = (int)((double)totalRead / totalBytes * 100);
                            if (pct != lastPct && pct % 10 == 0)
                            {
                                lastPct = pct;
                                Log($"دانلود ffmpeg: {pct}%");
                            }
                        }
                    }
                    fileStream.Close();
                }

                Log("استخراج ffmpeg...");
                string extractDir = Path.Combine(Path.GetTempPath(), "ffmpeg_extract");
                if (Directory.Exists(extractDir)) Directory.Delete(extractDir, true);
                ZipFile.ExtractToDirectory(zipPath, extractDir);

                var files = Directory.GetFiles(extractDir, "ffmpeg.exe", SearchOption.AllDirectories);
                if (files.Length > 0)
                {
                    File.Copy(files[0], _ffmpegPath, true);
                    Log("ffmpeg نصب شد");
                    File.Delete(zipPath);
                    Directory.Delete(extractDir, true);
                    return true;
                }

                File.Delete(zipPath);
                return false;
            }
            catch (Exception ex)
            {
                Log("خطا: " + ex.Message);
                return false;
            }
        }

        // ═══════════════════════════════════════════════
        //  دانلود مدل Vosk
        // ═══════════════════════════════════════════════

        private bool DownloadModelSync(string modelName, CancellationToken ct)
        {
            try
            {
                string url = $"https://alphacephei.com/vosk/models/{modelName}.zip";
                string zipPath = Path.Combine(_modelsDir, $"{modelName}.zip");

                using var client = new HttpClient { Timeout = TimeSpan.FromHours(2) };
                var response = client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct).GetAwaiter().GetResult();
                if (!response.IsSuccessStatusCode) return false;

                long totalBytes = response.Content.Headers.ContentLength ?? 0;
                using var httpStream = response.Content.ReadAsStreamAsync(ct).GetAwaiter().GetResult();
                using var fileStream = File.Create(zipPath);

                byte[] buffer = new byte[65536];
                long totalRead = 0;
                int bytesRead;
                int lastPct = -1;

                while ((bytesRead = httpStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ct.ThrowIfCancellationRequested();
                    fileStream.Write(buffer, 0, bytesRead);
                    totalRead += bytesRead;
                    if (totalBytes > 0)
                    {
                        int pct = (int)((double)totalRead / totalBytes * 100);
                        if (pct != lastPct && pct % 10 == 0)
                        {
                            lastPct = pct;
                            Log($"دانلود مدل: {pct}%");
                        }
                    }
                }
                fileStream.Close();

                Log("استخراج مدل...");
                ZipFile.ExtractToDirectory(zipPath, _modelsDir);
                File.Delete(zipPath);
                return true;
            }
            catch (Exception ex)
            {
                Log("خطا: " + ex.Message);
                return false;
            }
        }

        // ═══════════════════════════════════════════════
        //  لاگ
        // ═══════════════════════════════════════════════

        private void Log(string msg)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => Log(msg));
                return;
            }
            LogText.Text += $"[{DateTime.Now:HH:mm:ss}] {msg}\n";
            LogScroll.ScrollToEnd();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) DragMove();
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            _cts?.Cancel();
            Close();
        }
    }

    public class SrtEntry
    {
        public int Index { get; set; }
        public double Start { get; set; }
        public double End { get; set; }
        public string Text { get; set; } = "";
        public string TranslatedText { get; set; } = "";
    }

    public class VoskWord
    {
        public string Word { get; set; } = "";
        public double Start { get; set; }
        public double End { get; set; }
    }
}
