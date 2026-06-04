using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using LibVLCSharp.Shared;
using AryoVideoPlayer.Helpers;
using AryoVideoPlayer.Models;
using MediaPlayer = LibVLCSharp.Shared.MediaPlayer;

namespace AryoVideoPlayer
{
    [ComImport, Guid("56FDF342-FD6D-11d0-958A-006097C9A090")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface ITaskbarList2
    {
        void HrInit();
        void AddTab(IntPtr hwnd);
        void DeleteTab(IntPtr hwnd);
        void ActivateTab(IntPtr hwnd);
        void SetActiveAlt(IntPtr hwnd);
        void MarkFullscreenWindow(IntPtr hwnd, [MarshalAs(UnmanagedType.Bool)] bool fFullscreen);
    }

    [ComImport, Guid("56FDF344-FD6D-11d0-958A-006097C9A090")]
    [ClassInterface(ClassInterfaceType.None)]
    class CTaskbarList { }

    public partial class MainWindow : Window
    {
        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("shell32.dll")]
        private static extern uint SHAppBarMessage(uint dwMessage, ref APPBARDATA pData);

        [StructLayout(LayoutKind.Sequential)]
        private struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left, Top, Right, Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct APPBARDATA
        {
            public int cbSize;
            public IntPtr hWnd;
            public uint uCallbackMessage;
            public uint uEdge;
            public RECT rc;
            public int lParam;
        }

        private const int GWL_STYLE = -16;
        private const int GWL_EXSTYLE = -20;
        private const int WS_SYSMENU = 0x00080000;
        private const int WS_CAPTION = 0x00C00000;
        private const int WS_THICKFRAME = 0x00040000;
        private const int WS_MINIMIZEBOX = 0x00020000;
        private const int WS_MAXIMIZEBOX = 0x00010000;
        private const int WS_POPUP = unchecked((int)0x80000000);
        private const int WS_BORDER = 0x00800000;
        private const int WS_DLGFRAME = 0x00400000;
        private const int WS_CLIPSIBLINGS = 0x04000000;
        private const int WS_CLIPCHILDREN = 0x02000000;
        private const int WS_EX_APPWINDOW = 0x00040000;
        private const int WS_EX_WINDOWEDGE = 0x00000100;
        private const int WS_EX_CLIENTEDGE = 0x00000200;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_TOPMOST = 0x00000008;
        private const int WS_EX_ACCEPTFILES = 0x00000010;

        private const uint SWP_SHOWWINDOW = 0x0040;
        private const uint SWP_FRAMECHANGED = 0x0020;
        private const uint SWP_NOOWNERZORDER = 0x0200;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint MONITOR_DEFAULTTONEAREST = 2;
        private const uint MONITOR_DEFAULTTOPRIMARY = 1;

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
        private static readonly IntPtr HWND_BOTTOM = new IntPtr(1);

        private const int SW_MINIMIZE = 6;
        private const int SW_RESTORE = 9;
        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;
        private const int ABM_NEW = 0;
        private const int ABM_REMOVE = 1;
        private const int ABM_QUERYPOS = 2;
        private const int ABM_SETPOS = 3;
        private const int ABM_GETSTATE = 4;
        private const int ABM_GETTASKBARPOS = 5;
        private const int ABM_AUTOHIDE = 21;  // ABM_SETAUTOHIDE
        private const int ABM_SETSTATE = 10;
        private const int ABS_AUTOHIDE = 1;
        private const int ABS_ALWAYSONTOP = 2;

        private LibVLC _libVLC;
        private MediaPlayer _mediaPlayer;
        private DispatcherTimer _timer;
        private DispatcherTimer _hideCursorTimer;
        private bool _disposed;

        // وضعیت
        private bool _isDraggingSlider = false;
        private bool _isFullscreen = false;
        private ITaskbarList2 _taskbarList;
        private int _prevWindowStyle;
        private int _prevExStyle;
        private WindowState _prevWindowState;
        private double _prevLeft, _prevTop, _prevWidth, _prevHeight;
        private bool _isCinemaMode = false;
        private bool _isNightMode = false;
        private bool _isLooping = false;
        private double _currentSpeed = 1.0;
        private string _currentAspectRatio = "default";
        private int _hideCursorCountdown = 0;

        // لیست پخش
        private List<string> _playlist = new();
        private List<SubtitleEntry> _subtitles = new();
        private List<SubtitleEntry> _translatedSubtitles = new();
        private int _currentPlaylistIndex = -1;
        private Random _random = new();

        // تنظیمات
        private AppSettings _settings;
        private string _currentSubtitlePath = "";

        // قابلیت‌های پیشرفته VLC
        private float _videoBrightness = 1.0f;
        private float _videoContrast = 1.0f;
        private float _videoSaturation = 1.0f;
        private double _prevVolumeBeforeMute = 80;

        // لیست فرمت‌های ویدیویی پشتیبانی شده
        private static readonly string SupportedVideoFilter = "فایل ویدیو|*.mp4;*.avi;*.mkv;*.mov;*.wmv;*.flv;*.webm|همه فایل‌ها|*.*";
        private static readonly string[] SupportedVideoExtensions = { ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".webm" };

        // وضعیت لیست پخش قبل از تمام صفحه
        private Visibility _playlistVisibilityBeforeFullscreen = Visibility.Collapsed;
        private Visibility _playlistVisibilityBeforeCinema = Visibility.Collapsed;

        public MainWindow()
        {
            InitializeComponent();
            try
            {
                _taskbarList = (ITaskbarList2)new CTaskbarList();
                _taskbarList.HrInit();
            }
            catch { }
            _settings = AppSettings.Load();
            InitializeVLC();
            VideoView.MediaPlayer = _mediaPlayer;
            InitializeTimer();
            InitializeHideCursorTimer();
            ApplySettings();
            Closed += MainWindow_Closed;

            Application.Current.DispatcherUnhandledException += (s, e) =>
            {
                DarkMessageBox.ShowError($"خطای غیرمنتظره:\n{e.Exception.Message}\n\n{e.Exception.StackTrace}", "خطا", this);
                e.Handled = true;
            };

            // ── کلیک راست روی ویدیو = باز شدن منوی راست‌کلیک ──
            VideoContainer.MouseRightButtonDown += (s, e) =>
            {
                ContextMenuPopup.IsOpen = !ContextMenuPopup.IsOpen;
                e.Handled = true;
            };

            // ── دوبار کلیک روی ویدیو = تمام صفحه ──
            VideoContainer.PreviewMouseLeftButtonDown += (s, e) =>
            {
                if (e.ClickCount == 2)
                {
                    ToggleFullscreen();
                    e.Handled = true;
                }
            };

            // ── چرخش موس روی ویدیو = تنظیم صدا ──
            VideoContainer.PreviewMouseWheel += (s, e) =>
            {
                if (_mediaPlayer != null)
                {
                    VolumeSlider.Value = Math.Clamp(VolumeSlider.Value + (e.Delta > 0 ? 5 : -5), 0, 100);
                    e.Handled = true;
                }
            };

            // بارگذاری آخرین ویدیو
            if (!string.IsNullOrEmpty(_settings.LastVideoPath) && File.Exists(_settings.LastVideoPath))
            {
                _playlist.Add(_settings.LastVideoPath);
                RefreshPlaylistDisplay();
            }

            // ── پخش فایل از Open With (آرگومان‌های خط فرمان) ──
            if (App.StartupArgs.Length > 0)
            {
                foreach (var arg in App.StartupArgs)
                {
                    string fileArg = arg.Trim('"', ' ');
                    try
                    {
                        if (File.Exists(fileArg) && SupportedVideoExtensions.Contains(Path.GetExtension(fileArg).ToLower()))
                        {
                            _playlist.Clear();
                            PlaylistListBox.Items.Clear();
                            PlayVideo(fileArg);
                            break;
                        }
                    }
                    catch { }
                }

                // پاک کردن آرگومان‌ها بعد از پردازش
                App.StartupArgs = Array.Empty<string>();
            }

            // بررسی خودکار بروزرسانی
            _ = Task.Run(async () =>
            {
                await Task.Delay(5000);
                await Dispatcher.InvokeAsync(async () => await UpdateChecker.CheckForUpdateAsync());
            });
        }

        // ==================== مقداردهی اولیه ====================

        private void InitializeVLC()
        {
            _libVLC = new LibVLC("--no-video-title-show");
            _mediaPlayer = new MediaPlayer(_libVLC);
            _mediaPlayer.EndReached += MediaPlayer_EndReached;
            _mediaPlayer.Playing += MediaPlayer_Playing;
        }

        private void InitializeTimer()
        {
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void InitializeHideCursorTimer()
        {
            _hideCursorTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _hideCursorTimer.Tick += (s, e) =>
            {
                if (_isFullscreen)
                {
                    _hideCursorCountdown--;
                    if (_hideCursorCountdown <= 0)
                    {
                        Cursor = Cursors.None;
                        FullscreenControls.Visibility = Visibility.Collapsed;
                    }
                }
            };
            _hideCursorTimer.Start();
        }

        // ==================== تایمر ====================

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (_mediaPlayer == null || !_mediaPlayer.IsPlaying) return;

            if (!_isDraggingSlider && _mediaPlayer.Length > 0)
            {
                double progress = _mediaPlayer.Position * 100.0;
                PositionSlider.Value = progress;
                FsPositionSlider.Value = progress;
            }

            UpdateTimeDisplay();
            UpdateSubtitle();
        }

        private void UpdateTimeDisplay()
        {
            long time = Math.Max(0, _mediaPlayer.Time);
            long len = Math.Max(0, _mediaPlayer.Length);
            var current = TimeSpan.FromMilliseconds(time);
            var total = TimeSpan.FromMilliseconds(len);
            string cur = current.ToString(@"hh\:mm\:ss");
            string tot = total.ToString(@"hh\:mm\:ss");

            CurrentTimeText.Text = cur;
            TotalTimeText.Text = tot;
            FsCurrentTime.Text = cur;
            FsTotalTime.Text = tot;
        }

        private void UpdateSubtitle()
        {
            var currentTime = TimeSpan.FromMilliseconds(Math.Max(0, _mediaPlayer.Time));
            var currentSub = FindSubtitleAtTime(_subtitles, currentTime);
            var translatedSub = FindSubtitleAtTime(_translatedSubtitles, currentTime);

            string mainText = currentSub?.Text ?? "";
            string transText = (DualSubCheck.IsChecked == true && translatedSub != null) ? translatedSub.TranslatedText : "";

            MainSubtitleText.Text = mainText;
            TranslatedSubtitleText.Text = transText;
            TranslatedSubtitleText.Visibility = string.IsNullOrEmpty(transText) ? Visibility.Collapsed : Visibility.Visible;

            FsMainSubtitle.Text = mainText;
            FsTranslatedSubtitle.Text = transText;
            FsTranslatedSubtitle.Visibility = string.IsNullOrEmpty(transText) ? Visibility.Collapsed : Visibility.Visible;
        }

        private SubtitleEntry FindSubtitleAtTime(List<SubtitleEntry> entries, TimeSpan time)
        {
            if (entries == null || entries.Count == 0) return null;
            return entries.FirstOrDefault(e => time >= e.StartTime && time <= e.EndTime);
        }

        private void MediaPlayer_Playing(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() => { if (!_disposed) PlayPauseButton.Content = "⏸"; });
        }

        // ==================== پخش ویدیو ====================

        private void PlayVideo(string filePath)
        {
            if (!File.Exists(filePath)) return;

            EmptyState.Visibility = Visibility.Collapsed;
            VideoView.Visibility = Visibility.Visible;

            var media = new Media(_libVLC, new Uri(filePath));
            _mediaPlayer.Play(media);
            _mediaPlayer.Volume = (int)VolumeSlider.Value;

            if (_currentSpeed != 1.0)
                _mediaPlayer.SetRate((float)_currentSpeed);

            if (_currentAspectRatio != "default")
                _mediaPlayer.AspectRatio = _currentAspectRatio;

            PlayPauseButton.Content = "⏸";
            FsPlayPause.Content = "⏸";
            FileNameText.Text = Path.GetFileName(filePath);

            if (!_playlist.Contains(filePath))
            {
                _playlist.Add(filePath);
                RefreshPlaylistDisplay();
            }

            _currentPlaylistIndex = _playlist.IndexOf(filePath);
            PlaylistListBox.SelectedIndex = _currentPlaylistIndex;

            _settings.LastVideoPath = filePath;
            _settings.Save();
        }

        private void MediaPlayer_EndReached(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (_disposed) return;
                if (_isLooping)
                {
                    _mediaPlayer.Stop();
                    if (_mediaPlayer.Media != null)
                        _mediaPlayer.Play(_mediaPlayer.Media);
                }
                else if (_currentPlaylistIndex < _playlist.Count - 1)
                {
                    PlayVideo(_playlist[_currentPlaylistIndex + 1]);
                }
                else
                {
                    PlayPauseButton.Content = "▶";
                    FsPlayPause.Content = "▶";
                    VideoView.Visibility = Visibility.Collapsed;
                    EmptyState.Visibility = Visibility.Visible;
                }
            });
        }

        // ==================== دکمه‌ها ====================

        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            CloseAllPopups();
            ContextMenuPopup.IsOpen = false;

            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = SupportedVideoFilter,
                Title = "انتخاب فایل ویدیو"
            };
            if (dialog.ShowDialog() == true) PlayVideo(dialog.FileName);
        }

        private void EmptyState_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            OpenButton_Click(sender, e);
        }

        private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (_mediaPlayer == null) return;

            if (_mediaPlayer.IsPlaying)
            {
                _mediaPlayer.Pause();
                PlayPauseButton.Content = "▶";
                FsPlayPause.Content = "▶";
            }
            else if (_mediaPlayer.Media != null)
            {
                _mediaPlayer.Play();
                PlayPauseButton.Content = "⏸";
                FsPlayPause.Content = "⏸";
            }
        }

        private void PrevButton_Click(object sender, RoutedEventArgs e)
        {
            if (_mediaPlayer.Time > 3000)
                _mediaPlayer.Time = 0;
            else if (_currentPlaylistIndex > 0)
                PlayVideo(_playlist[_currentPlaylistIndex - 1]);
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPlaylistIndex < _playlist.Count - 1)
                PlayVideo(_playlist[_currentPlaylistIndex + 1]);
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_mediaPlayer != null)
            {
                int vol = (int)e.NewValue;
                _mediaPlayer.Volume = vol;

                string icon = vol == 0 ? "🔇" : vol < 30 ? "🔈" : vol < 70 ? "🔉" : "🔊";
                VolumeIcon.Text = icon;
                FsVolumeIcon.Text = icon;

                if (sender == VolumeSlider && Math.Abs(FsVolumeSlider.Value - e.NewValue) > 1)
                    FsVolumeSlider.Value = e.NewValue;
                else if (sender == FsVolumeSlider && Math.Abs(VolumeSlider.Value - e.NewValue) > 1)
                    VolumeSlider.Value = e.NewValue;
            }
        }

        // ==================== Seek Bar ====================

        private bool _isSeeking = false;

        private void PositionSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isSeeking && _mediaPlayer != null && _mediaPlayer.Length > 0)
            {
                float pos = (float)(e.NewValue / 100.0);
                _mediaPlayer.Position = pos;
            }
        }

        private void PositionSlider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isSeeking = true;
            _isDraggingSlider = true;
        }

        private void PositionSlider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isSeeking = false;
            _isDraggingSlider = false;
        }

        // ==================== سرعت ====================

        private void SpeedBtn_Click(object sender, RoutedEventArgs e)
        {
            CloseAllPopups();
            double[] speeds = { 0.25, 0.5, 0.75, 1.0, 1.25, 1.5, 2.0, 3.0 };
            var items = new List<(string, Action)>();
            foreach (var sp in speeds)
            {
                string label = (sp == _currentSpeed ? "✓ " : "  ") + $"{sp}x" + (sp == 1.0 ? " (پیش‌فرض)" : "");
                double speed = sp;
                items.Add((label, () => { _currentSpeed = speed; _mediaPlayer?.SetRate((float)speed); StatusText.Text = $"⚡ سرعت: {speed}x"; }));
            }
            ShowDarkMenu(sender as UIElement, items.ToArray());
        }

        // ==================== Loop & Shuffle ====================

        private void LoopBtn_Click(object sender, RoutedEventArgs e)
        {
            CloseAllPopups();
            _isLooping = !_isLooping;
            StatusText.Text = _isLooping ? "🔁 تکرار فعال" : "تکرار غیرفعال";
        }

        private void ShuffleBtn_Click(object sender, RoutedEventArgs e)
        {
            CloseAllPopups();
            if (_playlist.Count < 2) return;
            int nextIndex;
            do { nextIndex = _random.Next(_playlist.Count); }
            while (nextIndex == _currentPlaylistIndex);
            PlayVideo(_playlist[nextIndex]);
        }

        // ==================== عکس ====================

        private void ScreenshotButton_Click(object sender, RoutedEventArgs e)
        {
            CloseAllPopups();
            ContextMenuPopup.IsOpen = false;

            if (_mediaPlayer == null || _mediaPlayer.Media == null)
            {
                DarkMessageBox.ShowWarning("ابتدا یک ویدیو پخش کنید.", "خطا", this);
                return;
            }

            try
            {
                string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Screenshots");
                Directory.CreateDirectory(dir);
                string file = Path.Combine(dir, $"Screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png");

                _mediaPlayer.TakeSnapshot(0, file, 0, 0);
                StatusText.Text = $"📷 ذخیره شد: {Path.GetFileName(file)}";
            }
            catch (Exception ex)
            {
                DarkMessageBox.ShowError($"خطا: {ex.Message}", "خطا", this);
            }
        }

        // ==================== اطلاعات ====================

        private void InfoButton_Click(object sender, RoutedEventArgs e)
        {
            CloseAllPopups();
            ContextMenuPopup.IsOpen = false;

            if (_mediaPlayer == null || _mediaPlayer.Media == null)
            {
                DarkMessageBox.Show("ابتدا یک فایل ویدیو باز کنید.", "اطلاعات", this);
                return;
            }

            string info = $"📁 فایل: {FileNameText.Text}\n\n" +
                          $"⏱ مدت: {TimeSpan.FromMilliseconds(_mediaPlayer.Length):hh\\:mm\\:ss}\n" +
                          $"🔊 صدا: {_mediaPlayer.Volume}%\n" +
                          $"⚡ سرعت: {_currentSpeed}x\n" +
                          $"📐 نسبت: {_currentAspectRatio}\n" +
                          $"🔄 تکرار: {(_isLooping ? "فعال" : "غیرفعال")}\n" +
                          $"📝 زیرنویس: {(_subtitles.Count > 0 ? $"{_subtitles.Count} خط" : "ندارد")}\n" +
                          $"📋 لیست: {_playlist.Count} فایل\n" +
                          $"☀ روشنایی: {_videoBrightness}x\n" +
                          $"◐ کنتراست: {_videoContrast}x\n" +
                          $"🎨 اشباع: {_videoSaturation}x\n" +
                           $"✂ نسبت تصویر: {_currentAspectRatio}";

            DarkMessageBox.Show(info, "اطلاعات", this);
        }

        private void ShortcutsHelp_Click(object sender, RoutedEventArgs e)
        {
            CloseAllPopups();

            string shortcuts = @"⌨ کلیدهای میانبر:

Space       پخش / توقف
← →         ۵ ثانیه عقب / جلو
↑ ↓         کم / زیاد صدا
F           تمام صفحه
M           بی‌صدا
Esc         خروج از تمام صفحه
C           حالت سینمایی
N           حالت شب
I           اطلاعات ویدیو
Ctrl+O      باز کردن فایل
Ctrl+L      لیست پخش
F11         تمام صفحه";

            DarkMessageBox.Show(shortcuts, "کلیدهای میانبر", this);
        }

        private void AboutUs_Click(object sender, RoutedEventArgs e)
        {
            CloseAllPopups();
            string about = @"🎬 Aryo Video Player | نسخه 1.0.0

ساخته شده توسط تیم آذر کد (AzarCoder)
به یاد سردار بزرگ ایرانی، آریو برزن (Ariobarzanes) ✨

───── داستان آریو برزن ─────
آریو برزن، سردار دلیر پارسی، در نبرد دربند پارس (دروازه پارس)
در برابر اسکندر مقدونی ایستاد. او با تنها ۲۵۰۰ سرباز،
راه سپاه ۴۰٬۰۰۰ نفری اسکندر را سد کرد و حماسه‌ای
جاودان از شجاعت و میهن‌پرستی ایرانیان رقم زد.
اگرچه در نهایت شکست خورد، اما نامش برای همیشه
در تاریخ ایران جاودانه ماند.

این پلیر به افتخار اون سردار بزرگ نامگذاری شده تا
هنر و فرهنگ ایرانی همیشه زنده بمونه. 🦁🔥

───── ویژگی‌ها ─────
• پخش تمام فرمت‌های ویدیویی (MP4, MKV, AVI, MOV, FLV, ...)
• زیرنویس دو زبانه و ترجمه آنلاین
• حالت تمام صفحه و سینمایی
• کنترل کامل سرعت، صدا و نسبت تصویر
• لیست پخش با ذخیره/بارگذاری M3U
• اسکرین‌شات از ویدیو
• گفتار به متن با هوش مصنوعی

📌 ساخته شده با ❤️ و WPF + .NET 9";

            DarkMessageBox.Show(about, "📖 درباره ما", this);
        }

        private void ContactUs_Click(object sender, RoutedEventArgs e)
        {
            CloseAllPopups();
            string contact = @"📩 ارتباط با ما

تلگرام: @Thesurenax

برای پیشنهادات، گزارش باگ، یا هر چیز دیگه‌ای
می‌تونید از طریق تلگرام با ما در ارتباط باشید.

⭐ اگه از پلیر خوشتون اومده، حتماً استار بدید!";

            if (DarkMessageBox.ShowAlert(contact, "📩 ارتباط با ما", this, "📨 پیام در تلگرام", "بستن"))
            {
                try { Process.Start(new ProcessStartInfo("https://t.me/Thesurenax") { UseShellExecute = true }); }
                catch { }
            }
        }

        // ==================== زیرنویس ====================

        private void AddSubtitleButton_Click(object sender, RoutedEventArgs e)
        {
            CloseAllPopups();
            ContextMenuPopup.IsOpen = false;

            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "زیرنویس|*.srt;*.ass;*.sub|همه|*.*",
                Title = "انتخاب زیرنویس"
            };
            if (dialog.ShowDialog() == true) LoadSubtitle(dialog.FileName);
        }

        private void LoadSubtitle(string filePath)
        {
            _subtitles = SrtParser.Parse(filePath);
            _currentSubtitlePath = filePath;
            StatusText.Text = $"📝 {Path.GetFileName(filePath)} ({_subtitles.Count} خط)";
            SubtitleStatusText.Text = $"{_subtitles.Count} خط";
        }

        private async void TranslateButton_Click(object sender, RoutedEventArgs e)
        {
            if (_subtitles.Count == 0)
            {
                DarkMessageBox.ShowWarning("اول زیرنویس لود کنید!", "خطا", this);
                return;
            }

            var win = new LanguageSelectionWindow { Owner = this };
            if (win.ShowDialog() == true)
            {
                StatusText.Text = "🌐 در حال ترجمه...";
                TranslateBtn.IsEnabled = false;
                try
                {
                    _translatedSubtitles = await TranslationService.TranslateSubtitles(
                        _subtitles, win.SourceLanguage, win.TargetLanguage,
                        (c, t) => Dispatcher.Invoke(() => StatusText.Text = $"🌐 {c}/{t}"));

                    string path = Path.ChangeExtension(_currentSubtitlePath, $"_{win.TargetLanguage}.srt");
                    SrtParser.Save(path, _translatedSubtitles, saveTranslated: true);
                    StatusText.Text = "✅ ترجمه کامل شد!";
                }
                catch (Exception ex) { DarkMessageBox.ShowError(ex.Message, "خطا", this); }
                finally { TranslateBtn.IsEnabled = true; }
            }
        }

        private void DualSubCheck_Click(object sender, RoutedEventArgs e)
        {
            _settings.DualSubtitle = DualSubCheck.IsChecked == true;
            _settings.Save();
        }

        // ==================== تنظیم زیرنویس ====================

        private void SubtitleSync_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_mediaPlayer == null || _mediaPlayer.Media == null) return;

                CloseAllPopups();
                int[] delays = { -5000, -2000, -1000, -500, 0, 500, 1000, 2000, 5000 };
                var items = new List<(string, Action)>();
                foreach (var d in delays)
                {
                    string label = d == 0 ? "پیش‌فرض" : d > 0 ? $"+{d}ms" : $"{d}ms";
                    string display = (d == 0 ? "✓ " : "  ") + label;
                    int delay = d;
                    items.Add((display, () => { _mediaPlayer.SetSpuDelay(delay * 1000); StatusText.Text = $"⏱ تاخیر زیرنویس: {label}"; }));
                }
                ShowDarkMenu(sender as UIElement, items.ToArray());
            }
            catch (Exception ex)
            {
                DarkMessageBox.ShowError($"خطا: {ex.Message}", "خطا", this);
            }
        }

        // ==================== تبدیل صدا به متن ====================

        private void AudioToText_Click(object sender, RoutedEventArgs e)
        {
            CloseAllPopups();
            var win = new AudioToTextWindow { Owner = this };

            if (_currentPlaylistIndex >= 0 && _currentPlaylistIndex < _playlist.Count)
                win.CurrentVideoPath = _playlist[_currentPlaylistIndex];

            win.ShowDialog();

            // خودکار لود کردن زیرنویس ذخیره شده
            if (!string.IsNullOrEmpty(win.SavedSubtitlePath) && File.Exists(win.SavedSubtitlePath))
                LoadSubtitle(win.SavedSubtitlePath);
        }

        // ==================== نسبت تصویر ====================

        private void AspectRatio_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_mediaPlayer == null || _mediaPlayer.Media == null) return;

                CloseAllPopups();
                string[][] ratios = {
                    new[] { "default", "اصلی (Auto)" },
                    new[] { "16:9", "16:9" },
                    new[] { "4:3", "4:3" },
                    new[] { "1:1", "1:1" },
                    new[] { "2.35:1", "سینمایی (2.35:1)" },
                    new[] { "fill", "پر کردن (Fill)" }
                };
                var items = new List<(string, Action)>();
                foreach (var r in ratios)
                {
                    string label = (_currentAspectRatio == r[0] ? "✓ " : "  ") + r[1];
                    string val = r[0];
                    items.Add((label, () => { _currentAspectRatio = val; _mediaPlayer.AspectRatio = val; StatusText.Text = $"📐 {r[1]}"; }));
                }
                ShowDarkMenu(sender as UIElement, items.ToArray());
            }
            catch (Exception ex)
            {
                DarkMessageBox.ShowError($"خطا: {ex.Message}", "خطا", this);
            }
        }

        // ==================== حالت‌ها ====================

        private void CinemaMode_Click(object sender, RoutedEventArgs e)
        {
            CloseAllPopups();
            _isCinemaMode = !_isCinemaMode;
            if (_isCinemaMode)
            {
                if (_isNightMode)
                {
                    _isNightMode = false;
                    var ov = VideoContainer.Children.OfType<System.Windows.Controls.Border>()
                        .FirstOrDefault(b => b.Name == "NightOverlay");
                    if (ov != null) VideoContainer.Children.Remove(ov);
                }
                Background = new SolidColorBrush(Color.FromRgb(0x03, 0x03, 0x03));
                _playlistVisibilityBeforeCinema = PlaylistPanel.Visibility;
                PlaylistPanel.Visibility = Visibility.Collapsed;
                TitleBar.Visibility = Visibility.Collapsed;
                ControlBar.Visibility = Visibility.Collapsed;

                var hwnd = new WindowInteropHelper(this).Handle;
                _prevWindowState = WindowState;
                _prevLeft = Left;
                _prevTop = Top;
                _prevWidth = Width;
                _prevHeight = Height;
                _prevExStyle = GetWindowLong(hwnd, GWL_EXSTYLE);

                int newExStyle = _prevExStyle & ~WS_EX_APPWINDOW;
                SetWindowLong(hwnd, GWL_EXSTYLE, newExStyle);

                WindowState = WindowState.Normal;

                var monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
                if (monitor == IntPtr.Zero) monitor = MonitorFromWindow(IntPtr.Zero, MONITOR_DEFAULTTOPRIMARY);
                var mi = new MONITORINFO { cbSize = Marshal.SizeOf(typeof(MONITORINFO)) };
                if (GetMonitorInfo(monitor, ref mi))
                {
                    var r = mi.rcMonitor;
                    Topmost = true;
                    SetWindowPos(hwnd, HWND_TOPMOST, r.Left, r.Top, r.Right - r.Left, r.Bottom - r.Top,
                        SWP_SHOWWINDOW | SWP_FRAMECHANGED);
                }
                SetForegroundWindow(hwnd);
                try { _taskbarList?.MarkFullscreenWindow(hwnd, true); } catch { }

                StatusText.Text = "🎬 حالت سینمایی (Esc خروج)";
            }
            else
            {
                ExitCinemaMode();
            }
        }

        private void ExitCinemaMode()
        {
            _isCinemaMode = false;

            var hwnd = new WindowInteropHelper(this).Handle;

            // بازیابی WS_EX_APPWINDOW
            SetWindowLong(hwnd, GWL_EXSTYLE, _prevExStyle);

            try { _taskbarList?.MarkFullscreenWindow(hwnd, false); } catch { }

            Topmost = false;
            Background = new SolidColorBrush(Color.FromRgb(0x0D, 0x0D, 0x0D));
            PlaylistPanel.Visibility = _playlistVisibilityBeforeCinema;
            TitleBar.Visibility = Visibility.Visible;
            ControlBar.Visibility = Visibility.Visible;
            WindowState = _prevWindowState;
            Left = _prevLeft;
            Top = _prevTop;
            Width = _prevWidth;
            Height = _prevHeight;
            StatusText.Text = "حالت عادی";
        }
        private void NightMode_Click(object sender, RoutedEventArgs e)
        {
            CloseAllPopups();
            _isNightMode = !_isNightMode;
            if (_isNightMode)
            {
                if (_isCinemaMode) CinemaMode_Click(sender, e);

                // حذف overlay قبلی اگر وجود داشته باشه
                var existing = VideoContainer.Children.OfType<System.Windows.Controls.Border>()
                    .FirstOrDefault(b => b.Name == "NightOverlay");
                if (existing != null) VideoContainer.Children.Remove(existing);

                var overlay = new System.Windows.Controls.Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(25, 255, 180, 80)),
                    IsHitTestVisible = false,
                    Name = "NightOverlay"
                };
                VideoContainer.Children.Add(overlay);
                System.Windows.Controls.Canvas.SetZIndex(overlay, 100);
                StatusText.Text = "🌙 حالت شب";
            }
            else
            {
                var ov = VideoContainer.Children.OfType<System.Windows.Controls.Border>()
                    .FirstOrDefault(b => b.Name == "NightOverlay");
                if (ov != null) VideoContainer.Children.Remove(ov);
                StatusText.Text = "حالت عادی";
            }
        }

        // ==================== تمام صفحه ====================

        private void FullscreenButton_Click(object sender, RoutedEventArgs e)
        {
            CloseAllPopups();
            ContextMenuPopup.IsOpen = false;
            ToggleFullscreen();
        }

        private void ExitFullscreen_Click(object sender, RoutedEventArgs e) => ToggleFullscreen();

        private void ToggleFullscreen()
        {
            if (_isFullscreen)
                ExitFullscreen();
            else
                EnterFullscreen();
        }

        private void EnterFullscreen()
        {
            _isFullscreen = true;

            _prevWindowState = WindowState;
            _prevLeft = Left;
            _prevTop = Top;
            _prevWidth = Width;
            _prevHeight = Height;

            var hwnd = new WindowInteropHelper(this).Handle;

            _prevWindowStyle = GetWindowLong(hwnd, GWL_STYLE);
            _prevExStyle = GetWindowLong(hwnd, GWL_EXSTYLE);

            // جلوگیری از فعال شدن ExitFullscreen در Window_StateChanged
            _isFullscreen = false;
            WindowState = WindowState.Normal;
            _isFullscreen = true;

            // حذف WS_EX_APPWINDOW تا پنجره از تسک‌بار مخفی بشه
            int newExStyle = _prevExStyle & ~WS_EX_APPWINDOW;
            SetWindowLong(hwnd, GWL_EXSTYLE, newExStyle);

            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            Topmost = true;

            // اطلاع به ویندوز که برنامه در حالت تمام صفحه قرار گرفته
            try { _taskbarList?.MarkFullscreenWindow(hwnd, true); } catch { }

            // دریافت ابعاد کامل صفحه (شامل تسک‌بار) و پوشاندن کل صفحه
            var monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
            if (monitor == IntPtr.Zero) monitor = MonitorFromWindow(IntPtr.Zero, MONITOR_DEFAULTTOPRIMARY);
            var mi = new MONITORINFO { cbSize = Marshal.SizeOf(typeof(MONITORINFO)) };
            RECT r = default;
            if (GetMonitorInfo(monitor, ref mi))
                r = mi.rcMonitor;

            SetWindowPos(hwnd, HWND_TOPMOST, r.Left, r.Top, r.Right - r.Left, r.Bottom - r.Top,
                SWP_SHOWWINDOW | SWP_FRAMECHANGED);

            SetForegroundWindow(hwnd);

            TitleBar.Visibility = Visibility.Collapsed;
            ControlBar.Visibility = Visibility.Collapsed;

            var mainBorder = Content as System.Windows.Controls.Border;
            if (mainBorder != null) mainBorder.BorderThickness = new Thickness(0);

            _playlistVisibilityBeforeFullscreen = PlaylistPanel.Visibility;
            PlaylistPanel.Visibility = Visibility.Collapsed;

            FullscreenOverlay.Visibility = Visibility.Visible;
            FullscreenControls.Visibility = Visibility.Collapsed;

            _hideCursorCountdown = 3;
        }

        private void ExitFullscreen()
        {
            _isFullscreen = false;

            var hwnd = new WindowInteropHelper(this).Handle;

            // اطلاع به ویندوز که حالت تمام صفحه تمام شد
            try { _taskbarList?.MarkFullscreenWindow(hwnd, false); } catch { }

            // بازیابی استایل اصلی پنجره
            SetWindowLong(hwnd, GWL_EXSTYLE, _prevExStyle);
            SetWindowLong(hwnd, GWL_STYLE, _prevWindowStyle);

            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.CanResizeWithGrip;
            Topmost = false;

            WindowState = _prevWindowState;
            Left = _prevLeft;
            Top = _prevTop;
            Width = _prevWidth;
            Height = _prevHeight;

            SetWindowPos(hwnd, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_FRAMECHANGED);

            TitleBar.Visibility = Visibility.Visible;
            ControlBar.Visibility = Visibility.Visible;

            var mainBorder = Content as System.Windows.Controls.Border;
            if (mainBorder != null) mainBorder.BorderThickness = new Thickness(1);

            PlaylistPanel.Visibility = _playlistVisibilityBeforeFullscreen;

            FullscreenOverlay.Visibility = Visibility.Collapsed;
            FullscreenControls.Visibility = Visibility.Collapsed;

            Cursor = Cursors.Arrow;
        }

        private void SetTaskbarVisible(bool visible)
        {
            try
            {
                var trayHwnd = FindWindow("Shell_TrayWnd", null);
                if (trayHwnd != IntPtr.Zero)
                {
                    ShowWindow(trayHwnd, visible ? SW_SHOW : SW_HIDE);
                    SetWindowPos(trayHwnd, visible ? HWND_TOPMOST : HWND_BOTTOM, 0, 0, 0, 0,
                        SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
                }

                var secTrayHwnd = FindWindow("Shell_SecondaryTrayWnd", null);
                if (secTrayHwnd != IntPtr.Zero)
                {
                    ShowWindow(secTrayHwnd, visible ? SW_SHOW : SW_HIDE);
                    SetWindowPos(secTrayHwnd, visible ? HWND_TOPMOST : HWND_BOTTOM, 0, 0, 0, 0,
                        SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
                }

                if (!visible)
                {
                    SetTaskbarAutoHide(true);
                }
                else
                {
                    SetTaskbarAutoHide(false);
                }
            }
            catch { }
        }

        private void SetTaskbarAutoHide(bool enable)
        {
            try
            {
                var trayHwnd = FindWindow("Shell_TrayWnd", null);
                if (trayHwnd != IntPtr.Zero)
                {
                    var abd = new APPBARDATA { cbSize = Marshal.SizeOf(typeof(APPBARDATA)), hWnd = trayHwnd, lParam = enable ? ABS_AUTOHIDE : ABS_ALWAYSONTOP };
                    SHAppBarMessage(ABM_SETSTATE, ref abd);
                }
            }
            catch { }
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (WindowState != WindowState.Maximized && _isFullscreen)
                ExitFullscreen();

            if (MaximizeBtn != null)
                MaximizeBtn.Content = WindowState == WindowState.Maximized ? "❐" : "☐";
        }

        // ==================== کنترل ماوس تمام صفحه ====================

        private void VideoArea_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isFullscreen)
            {
                Cursor = Cursors.Arrow;
                FullscreenControls.Visibility = Visibility.Visible;
                _hideCursorCountdown = 3;
            }
        }

        private void VideoArea_MouseEnter(object sender, MouseEventArgs e)
        {
            if (_isFullscreen)
            {
                FullscreenControls.Visibility = Visibility.Visible;
                _hideCursorCountdown = 3;
            }
        }

        private void VideoArea_MouseLeave(object sender, MouseEventArgs e)
        {
            if (_isFullscreen)
            {
                FullscreenControls.Visibility = Visibility.Collapsed;
            }
        }

        // ==================== لیست پخش ====================

        private void AddToPlaylist_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = SupportedVideoFilter,
                Multiselect = true
            };
            if (dialog.ShowDialog() == true)
            {
                foreach (string file in dialog.FileNames)
                {
                    if (!_playlist.Contains(file))
                        _playlist.Add(file);
                }
                RefreshPlaylistDisplay();
            }
        }

        private void RemoveFromPlaylist_Click(object sender, RoutedEventArgs e)
        {
            int idx = PlaylistListBox.SelectedIndex;
            if (idx >= 0) RemoveFromPlaylistAt(idx);
        }

        private void ClearPlaylist_Click(object sender, RoutedEventArgs e)
        {
            _mediaPlayer?.Stop();
            _playlist.Clear();
            _currentPlaylistIndex = -1;
            PlayPauseButton.Content = "▶";
            FsPlayPause.Content = "▶";
            VideoView.Visibility = Visibility.Collapsed;
            EmptyState.Visibility = Visibility.Visible;
            RefreshPlaylistDisplay();
        }

        private void RefreshPlaylistDisplay()
        {
            PlaylistListBox.Items.Clear();
            for (int i = 0; i < _playlist.Count; i++)
                PlaylistListBox.Items.Add($"{i + 1}. {Path.GetFileName(_playlist[i])}");
            if (_currentPlaylistIndex >= 0 && _currentPlaylistIndex < _playlist.Count)
                PlaylistListBox.SelectedIndex = _currentPlaylistIndex;
        }

        private void PlaylistListBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                int idx = PlaylistListBox.SelectedIndex;
                if (idx >= 0) RemoveFromPlaylistAt(idx);
            }
        }

        private void RemoveFromPlaylistAt(int idx)
        {
            if (idx < 0 || idx >= _playlist.Count) return;
            bool wasPlaying = _currentPlaylistIndex == idx;
            _playlist.RemoveAt(idx);
            if (wasPlaying)
            {
                if (idx < _playlist.Count)
                    PlayVideo(_playlist[idx]);
                else if (_playlist.Count > 0)
                    PlayVideo(_playlist[^1]);
                else
                {
                    _mediaPlayer?.Stop();
                    _currentPlaylistIndex = -1;
                    PlayPauseButton.Content = "▶";
                    FsPlayPause.Content = "▶";
                    VideoView.Visibility = Visibility.Collapsed;
                    EmptyState.Visibility = Visibility.Visible;
                }
            }
            else if (_currentPlaylistIndex > idx)
            {
                _currentPlaylistIndex--;
            }
            RefreshPlaylistDisplay();
        }

        private void MoveUp_Click(object sender, RoutedEventArgs e)
        {
            int idx = PlaylistListBox.SelectedIndex;
            if (idx > 0 && idx < _playlist.Count)
            {
                (_playlist[idx], _playlist[idx - 1]) = (_playlist[idx - 1], _playlist[idx]);
                if (_currentPlaylistIndex == idx)
                    _currentPlaylistIndex = idx - 1;
                else if (_currentPlaylistIndex == idx - 1)
                    _currentPlaylistIndex = idx;
                RefreshPlaylistDisplay();
                PlaylistListBox.SelectedIndex = idx - 1;
            }
        }

        private void MoveDown_Click(object sender, RoutedEventArgs e)
        {
            int idx = PlaylistListBox.SelectedIndex;
            if (idx >= 0 && idx < _playlist.Count - 1)
            {
                (_playlist[idx], _playlist[idx + 1]) = (_playlist[idx + 1], _playlist[idx]);
                if (_currentPlaylistIndex == idx)
                    _currentPlaylistIndex = idx + 1;
                else if (_currentPlaylistIndex == idx + 1)
                    _currentPlaylistIndex = idx;
                RefreshPlaylistDisplay();
                PlaylistListBox.SelectedIndex = idx + 1;
            }
        }

        private void PlaylistContextMenu_Play(object sender, RoutedEventArgs e)
        {
            int idx = PlaylistListBox.SelectedIndex;
            if (idx >= 0 && idx < _playlist.Count) PlayVideo(_playlist[idx]);
        }

        private void PlaylistContextMenu_Remove(object sender, RoutedEventArgs e)
        {
            int idx = PlaylistListBox.SelectedIndex;
            if (idx >= 0) RemoveFromPlaylistAt(idx);
        }

        private void PlaylistListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            int idx = PlaylistListBox.SelectedIndex;
            if (idx >= 0 && idx < _playlist.Count) PlayVideo(_playlist[idx]);
        }

        private void TogglePlaylist_Click(object sender, RoutedEventArgs e)
        {
            PlaylistPanel.Visibility = PlaylistPanel.Visibility == Visibility.Visible
                ? Visibility.Collapsed : Visibility.Visible;
        }

        private void PlaylistToggle_Click(object sender, RoutedEventArgs e)
        {
            CloseAllPopups();
            PlaylistPanel.Visibility = PlaylistPanel.Visibility == Visibility.Visible
                ? Visibility.Collapsed : Visibility.Visible;
        }

        // ==================== ذخیره/بارگذاری لیست پخش ====================

        private void SavePlaylist_Click(object sender, RoutedEventArgs e)
        {
            if (_playlist.Count == 0)
            {
                DarkMessageBox.ShowWarning("لیست پخش خالی است!", "هشدار", this);
                return;
            }
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "M3U Playlist|*.m3u|All Files|*.*",
                FileName = "playlist.m3u"
            };
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var lines = new List<string>
                    {
                        "#EXTM3U",
                        $"#PLAYLIST: AryoVideoPlayer"
                    };
                    foreach (var file in _playlist)
                    {
                        if (File.Exists(file))
                            lines.Add($"#EXTINF:-1,{Path.GetFileName(file)}\n{file}");
                    }
                    File.WriteAllLines(dialog.FileName, lines);
                    StatusText.Text = $"✅ لیست پخش در {Path.GetFileName(dialog.FileName)} ذخیره شد";
                }
                catch (Exception ex)
                {
                    DarkMessageBox.ShowError($"خطا در ذخیره: {ex.Message}", "خطا", this);
                }
            }
        }

        private void LoadPlaylist_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "M3U Playlist|*.m3u|All Files|*.*",
                Title = "بارگذاری لیست پخش"
            };
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    string[] lines = File.ReadAllLines(dialog.FileName);
                    int added = 0;
                    foreach (string line in lines)
                    {
                        string trimmed = line.Trim();
                        if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#")) continue;
                        if (File.Exists(trimmed) && SupportedVideoExtensions.Contains(Path.GetExtension(trimmed).ToLower()))
                        {
                            if (!_playlist.Contains(trimmed))
                            {
                                _playlist.Add(trimmed);
                                added++;
                            }
                        }
                    }
                    if (added > 0)
                    {
                        RefreshPlaylistDisplay();
                        StatusText.Text = $"✅ {added} فایل به لیست پخش اضافه شد";
                    }
                    else
                    {
                        DarkMessageBox.ShowWarning("هیچ فایل ویدیویی معتبری در فایل پیدا نشد!", "هشدار", this);
                    }
                }
                catch (Exception ex)
                {
                    DarkMessageBox.ShowError($"خطا در بارگذاری: {ex.Message}", "خطا", this);
                }
            }
        }

        // ==================== پرش به زمان ====================

        private void JumpToTime_Click(object sender, RoutedEventArgs e)
        {
            if (_mediaPlayer == null || _mediaPlayer.Media == null) return;
            string input = DarkMessageBox.ShowInput("زمان مورد نظر را وارد کنید (مثال: 01:23:45)", "پرش به زمان", this);
            if (string.IsNullOrEmpty(input)) return;
            try
            {
                var ts = TimeSpan.ParseExact(input, @"hh\:mm\:ss", null);
                long ms = (long)ts.TotalMilliseconds;
                _mediaPlayer.Time = Math.Clamp(ms, 0, _mediaPlayer.Length);
            }
            catch
            {
                DarkMessageBox.ShowError("فرمت زمان نامعتبر! از hh:mm:ss استفاده کنید.", "خطا", this);
            }
        }

        // ==================== Drag & Drop ====================

        private void Window_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (files == null || files.Length == 0) return;
            bool firstPlayed = false;
            foreach (string file in files)
            {
                string ext = Path.GetExtension(file).ToLower();
                if (SupportedVideoExtensions.Contains(ext))
                {
                    if (!_playlist.Contains(file))
                        _playlist.Add(file);
                    if (!firstPlayed) { PlayVideo(file); firstPlayed = true; }
                }
                else if (new[] { ".srt", ".ass", ".sub" }.Contains(ext))
                    LoadSubtitle(file);
            }
            RefreshPlaylistDisplay();
        }

        // ==================== کنترل پنجره ====================

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                Maximize_Click(sender, e);
            }
            else
            {
                DragMove();
            }
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Maximize_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
                MaximizeBtn.Content = "☐";
            }
            else
            {
                WindowState = WindowState.Maximized;
                MaximizeBtn.Content = "❐";
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();

        // ==================== قابلیت‌های پیشرفته VLC ====================

        private void AudioTrack_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_mediaPlayer == null || _mediaPlayer.Media == null) return;

                CloseAllPopups();
                var tracks = _mediaPlayer.AudioTrackDescription;
                if (tracks == null || tracks.Length == 0)
                {
                    ShowDarkMenu(sender as UIElement, ("  بدون صدا", (Action)null));
                    return;
                }
                int currentTrack = _mediaPlayer.AudioTrack;
                var items = new List<(string, Action)>();
                for (int i = 0; i < tracks.Length; i++)
                {
                    var track = tracks[i];
                    string label = (track.Id == currentTrack ? "✓ " : "  ") + $"🔊 {track.Name ?? $"صدا {i + 1}"}";
                    int trackId = track.Id;
                    string trackName = track.Name ?? $"ترک {i + 1}";
                    items.Add((label, () => { _mediaPlayer.SetAudioTrack(trackId); StatusText.Text = $"🔊 صدا: {trackName}"; }));
                }
                ShowDarkMenu(sender as UIElement, items.ToArray());
            }
            catch (Exception ex)
            {
                DarkMessageBox.ShowError($"خطا: {ex.Message}", "خطا", this);
            }
        }

        private void VideoTrack_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_mediaPlayer == null || _mediaPlayer.Media == null) return;

                CloseAllPopups();
                var tracks = _mediaPlayer.VideoTrackDescription;
                if (tracks == null || tracks.Length == 0)
                {
                    ShowDarkMenu(sender as UIElement, ("  بدون ویدیو", (Action)null));
                    return;
                }
                int currentTrack = _mediaPlayer.VideoTrack;
                var items = new List<(string, Action)>();
                for (int i = 0; i < tracks.Length; i++)
                {
                    var track = tracks[i];
                    string label = (track.Id == currentTrack ? "✓ " : "  ") + $"🎬 {track.Name ?? $"ویدیو {i + 1}"}";
                    int trackId = track.Id;
                    string trackName = track.Name ?? $"ترک {i + 1}";
                    items.Add((label, () => { _mediaPlayer.SetVideoTrack(trackId); StatusText.Text = $"🎬 ویدیو: {trackName}"; }));
                }
                ShowDarkMenu(sender as UIElement, items.ToArray());
            }
            catch (Exception ex)
            {
                DarkMessageBox.ShowError($"خطا: {ex.Message}", "خطا", this);
            }
        }

        private void VideoFilter_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_mediaPlayer == null || _mediaPlayer.Media == null) return;

                CloseAllPopups();
                var items = new List<(string header, Action onClick)>();

                items.Add(("── ☀ روشنایی ──", (Action)null));
                foreach (var level in new[] { 0.5f, 0.75f, 1.0f, 1.25f, 1.5f, 2.0f })
                {
                    float l = level;
                    string label = $"{level}x {(level == 1.0f ? "(پیش‌فرض)" : "")}";
                    items.Add((_videoBrightness == level ? $"✓ {label}" : $"  {label}",
                        () => { _videoBrightness = l; _mediaPlayer.SetAdjustFloat(VideoAdjustOption.Enable, 1f); _mediaPlayer.SetAdjustFloat(VideoAdjustOption.Brightness, l); StatusText.Text = $"☀ روشنایی: {l}x"; }));
                }

                items.Add(("── ◐ کنتراست ──", (Action)null));
                foreach (var level in new[] { 0.5f, 0.75f, 1.0f, 1.25f, 1.5f, 2.0f })
                {
                    float l = level;
                    string label = $"{level}x {(level == 1.0f ? "(پیش‌فرض)" : "")}";
                    items.Add((_videoContrast == level ? $"✓ {label}" : $"  {label}",
                        () => { _videoContrast = l; _mediaPlayer.SetAdjustFloat(VideoAdjustOption.Enable, 1f); _mediaPlayer.SetAdjustFloat(VideoAdjustOption.Contrast, l); StatusText.Text = $"◐ کنتراست: {l}x"; }));
                }

                items.Add(("── 🎨 اشباع رنگ ──", (Action)null));
                foreach (var level in new[] { 0.0f, 0.5f, 0.75f, 1.0f, 1.5f, 2.0f })
                {
                    float l = level;
                    string label = $"{level}x {(level == 1.0f ? "(پیش‌فرض)" : "")}";
                    items.Add((_videoSaturation == level ? $"✓ {label}" : $"  {label}",
                        () => { _videoSaturation = l; _mediaPlayer.SetAdjustFloat(VideoAdjustOption.Enable, 1f); _mediaPlayer.SetAdjustFloat(VideoAdjustOption.Saturation, l); StatusText.Text = $"🎨 اشباع: {l}x"; }));
                }

                items.Add(("── 🔄 Deinterlace ──", (Action)null));
                foreach (var mode in new[] { "off", "auto", "on" })
                {
                    string m = mode;
                    string label = mode == "off" ? "غیرفعال" : mode == "auto" ? "خودکار" : "فعال";
                    items.Add(("  " + label,
                        () => { _mediaPlayer.SetDeinterlace(m == "off" ? null : m); StatusText.Text = $"🔄 Deinterlace: {m}"; }));
                }

                ShowDarkMenu(sender as UIElement, items.ToArray());
            }
            catch (Exception ex)
            {
                DarkMessageBox.ShowError($"خطا: {ex.Message}", "خطا", this);
            }
        }

        // ==================== کلیدها ====================

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            bool ctrl = Keyboard.Modifiers == ModifierKeys.Control;

            if (e.Key == Key.Escape)
            {
                if (ContextMenuPopup.IsOpen)
                    ContextMenuPopup.IsOpen = false;
                else if (PopupFile.IsOpen || PopupView.IsOpen || PopupTools.IsOpen || PopupHelp.IsOpen)
                    CloseAllPopups();
                else if (_isFullscreen) ExitFullscreen();
                else if (_isCinemaMode) ExitCinemaMode();
            }
            else if (e.Key == Key.Space)
            {
                PlayPauseButton_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
            else if (e.Key == Key.Right && _mediaPlayer != null)
            {
                _mediaPlayer.Time = Math.Min(_mediaPlayer.Length, _mediaPlayer.Time + 5000);
            }
            else if (e.Key == Key.Left && _mediaPlayer != null)
            {
                _mediaPlayer.Time = Math.Max(0, _mediaPlayer.Time - 5000);
            }
            else if (e.Key == Key.Up)
            {
                VolumeSlider.Value = Math.Min(100, VolumeSlider.Value + 5);
            }
            else if (e.Key == Key.Down)
            {
                VolumeSlider.Value = Math.Max(0, VolumeSlider.Value - 5);
            }
            else if (e.Key == Key.F && !ctrl)
            {
                ToggleFullscreen();
            }
            else if (e.Key == Key.M && !ctrl)
            {
                if (VolumeSlider.Value > 0)
                {
                    _prevVolumeBeforeMute = VolumeSlider.Value;
                    VolumeSlider.Value = 0;
                }
                else
                {
                    VolumeSlider.Value = _prevVolumeBeforeMute;
                }
            }
            else if (e.Key == Key.F11)
            {
                ToggleFullscreen();
            }
            else if (e.Key == Key.I && !ctrl)
            {
                InfoButton_Click(this, new RoutedEventArgs());
            }
            else if (e.Key == Key.Enter && !ctrl)
            {
                ToggleFullscreen();
            }
            else if (e.Key == Key.R && !ctrl)
            {
                CloseAllPopups();
                AspectRatio_Click(this, new RoutedEventArgs());
            }
            else if (e.Key == Key.OemOpenBrackets && !ctrl && _mediaPlayer != null)
            {
                _currentSpeed = Math.Max(0.25, _currentSpeed - 0.25);
                _mediaPlayer.SetRate((float)_currentSpeed);
                StatusText.Text = $"⚡ سرعت: {_currentSpeed}x";
            }
            else if (e.Key == Key.OemCloseBrackets && !ctrl && _mediaPlayer != null)
            {
                _currentSpeed = Math.Min(3.0, _currentSpeed + 0.25);
                _mediaPlayer.SetRate((float)_currentSpeed);
                StatusText.Text = $"⚡ سرعت: {_currentSpeed}x";
            }
            else if (e.Key == Key.PageUp && _playlist.Count > 0)
            {
                CloseAllPopups();
                PrevButton_Click(this, new RoutedEventArgs());
            }
            else if (e.Key == Key.PageDown && _playlist.Count > 0)
            {
                CloseAllPopups();
                NextButton_Click(this, new RoutedEventArgs());
            }
            else if (e.Key == Key.T && !ctrl)
            {
                CloseAllPopups();
                SubtitleSync_Click(this, new RoutedEventArgs());
            }
            else if (e.Key == Key.G && ctrl && _mediaPlayer?.Media != null)
            {
                JumpToTime_Click(this, new RoutedEventArgs());
            }
            else if (e.Key == Key.E && !ctrl && _mediaPlayer != null)
            {
                _mediaPlayer.NextFrame();
                _mediaPlayer.Pause();
                PlayPauseButton.Content = "▶";
                FsPlayPause.Content = "▶";
            }
            else if (e.Key == Key.S && ctrl && e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                SavePlaylist_Click(this, new RoutedEventArgs());
            }
            else if (ctrl && e.Key == Key.Right && _mediaPlayer != null)
            {
                _mediaPlayer.Time = Math.Min(_mediaPlayer.Length, _mediaPlayer.Time + 60000);
            }
            else if (ctrl && e.Key == Key.Left && _mediaPlayer != null)
            {
                _mediaPlayer.Time = Math.Max(0, _mediaPlayer.Time - 60000);
            }
            else if (ctrl && e.Key == Key.Up)
            {
                VolumeSlider.Value = Math.Min(100, VolumeSlider.Value + 10);
            }
            else if (ctrl && e.Key == Key.Down)
            {
                VolumeSlider.Value = Math.Max(0, VolumeSlider.Value - 10);
            }
            else if (e.Key == Key.N && !ctrl)
            {
                NightMode_Click(this, new RoutedEventArgs());
            }
            else if (e.Key == Key.C && !ctrl)
            {
                CinemaMode_Click(this, new RoutedEventArgs());
            }
            else if (ctrl && e.Key == Key.O)
            {
                OpenButton_Click(this, new RoutedEventArgs());
            }
            else if (ctrl && e.Key == Key.L)
            {
                PlaylistToggle_Click(this, new RoutedEventArgs());
            }
            else if (ctrl && e.Key == Key.S)
            {
                ScreenshotButton_Click(this, new RoutedEventArgs());
            }
            else if (e.Key == Key.Home && _mediaPlayer != null)
            {
                _mediaPlayer.Time = 0;
            }
            else if (e.Key == Key.End && _mediaPlayer != null)
            {
                _mediaPlayer.Time = _mediaPlayer.Length;
            }
            else if (e.Key == Key.D && !ctrl)
            {
                DualSubCheck.IsChecked = DualSubCheck.IsChecked != true;
                StatusText.Text = DualSubCheck.IsChecked == true ? "📝 زیرنویس دوگانه: فعال" : "📝 زیرنویس دوگانه: غیرفعال";
            }
            else if (e.Key == Key.L && !ctrl)
            {
                LoopBtn_Click(this, new RoutedEventArgs());
            }
            else if (e.Key == Key.A && !ctrl && _mediaPlayer?.Media != null)
            {
                AudioTrack_Click(this, new RoutedEventArgs());
            }
            else if (e.Key == Key.V && !ctrl && _mediaPlayer?.Media != null)
            {
                VideoTrack_Click(this, new RoutedEventArgs());
            }
            else if (e.Key == Key.B && !ctrl)
            {
                NightMode_Click(this, new RoutedEventArgs());
            }
            else if (e.Key == Key.Z && !ctrl)
            {
                AspectRatio_Click(this, new RoutedEventArgs());
            }
            else if (e.Key == Key.X && !ctrl)
            {
                CinemaMode_Click(this, new RoutedEventArgs());
            }
            else if (ctrl && e.Key == Key.I)
            {
                InfoButton_Click(this, new RoutedEventArgs());
            }
            else if (ctrl && e.Key == Key.M)
            {
                TogglePlaylist_Click(this, new RoutedEventArgs());
            }
        }

        // ==================== ذخیره ====================

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            _disposed = true;
            try
            {
                var hwnd = new WindowInteropHelper(this).Handle;
                SetWindowLong(hwnd, GWL_EXSTYLE, _prevExStyle);
                try { _taskbarList?.MarkFullscreenWindow(hwnd, false); } catch { }
            }
            catch { }
            _settings.Volume = VolumeSlider.Value;
            _settings.Save();
            if (_mediaPlayer != null)
            {
                _mediaPlayer.EndReached -= MediaPlayer_EndReached;
                _mediaPlayer.Playing -= MediaPlayer_Playing;
            }
            _mediaPlayer?.Stop();
            _mediaPlayer?.Dispose();
            _libVLC?.Dispose();
            _timer?.Stop();
            _hideCursorTimer?.Stop();
            _timer.Tick -= Timer_Tick;
        }

        private void ApplySettings()
        {
            VolumeSlider.Value = _settings.Volume;
            FsVolumeSlider.Value = _settings.Volume;
            DualSubCheck.IsChecked = _settings.DualSubtitle;
            MainSubtitleText.FontSize = _settings.SubtitleFontSize;
        }

        // ==================== منوی بالا ====================

        // ── پاپ‌آپ فرعی فعال ──
        private System.Windows.Controls.Primitives.Popup _activeSubMenu;

        private void CloseAllPopups()
        {
            PopupFile.IsOpen = false;
            PopupView.IsOpen = false;
            PopupTools.IsOpen = false;
            PopupHelp.IsOpen = false;
            CloseDarkMenu();
        }

        private void CloseDarkMenu()
        {
            if (_activeSubMenu != null)
            {
                _activeSubMenu.IsOpen = false;
                _activeSubMenu = null;
            }
        }

        private void ShowDarkMenu(UIElement placementTarget, params (string header, Action onClick)[] items)
        {
            try
            {
                CloseDarkMenu();

                var stack = new StackPanel();
                foreach (var item in items)
                {
                    var btn = new Button
                    {
                        Content = item.header,
                        Style = (Style)FindResource("ContextMenuItem")
                    };
                    btn.Click += (s, e) =>
                    {
                        CloseDarkMenu();
                        item.onClick?.Invoke();
                    };
                    stack.Children.Add(btn);
                }

                var border = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x1E)),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(4),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33)),
                    BorderThickness = new Thickness(1),
                    MinWidth = 180,
                    Effect = FindResource("MenuShadow") as DropShadowEffect,
                    Margin = new Thickness(0, 4, 0, 0)
                };
                border.Child = stack;

                var popup = new System.Windows.Controls.Primitives.Popup
                {
                    AllowsTransparency = true,
                    PopupAnimation = System.Windows.Controls.Primitives.PopupAnimation.Fade,
                    Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint,
                    StaysOpen = true,
                    Child = border
                };

                popup.Opened += (s, e) =>
                {
                    _activeSubMenu = popup;
                    void handler(object sender2, MouseButtonEventArgs e2)
                    {
                        if (!popup.IsMouseOver)
                        {
                            CloseDarkMenu();
                            PreviewMouseLeftButtonDown -= handler;
                        }
                    }
                    PreviewMouseLeftButtonDown += handler;
                };

                popup.IsOpen = true;
            }
            catch (Exception ex)
            {
                DarkMessageBox.ShowError($"خطا: {ex.Message}", "خطا در منو", this);
            }
        }

        private void Popup_Opened(object sender, EventArgs e)
        {
            var popup = sender as System.Windows.Controls.Primitives.Popup;
            if (popup == PopupFile) { PopupView.IsOpen = false; PopupTools.IsOpen = false; PopupHelp.IsOpen = false; }
            else if (popup == PopupView) { PopupFile.IsOpen = false; PopupTools.IsOpen = false; PopupHelp.IsOpen = false; }
            else if (popup == PopupTools) { PopupFile.IsOpen = false; PopupView.IsOpen = false; PopupHelp.IsOpen = false; }
            else if (popup == PopupHelp) { PopupFile.IsOpen = false; PopupView.IsOpen = false; PopupTools.IsOpen = false; }
        }

        private void MenuFile_Click(object sender, RoutedEventArgs e)
        {
            PopupFile.IsOpen = !PopupFile.IsOpen;
            PopupView.IsOpen = false;
            PopupTools.IsOpen = false;
            PopupHelp.IsOpen = false;
        }

        private void MenuView_Click(object sender, RoutedEventArgs e)
        {
            PopupView.IsOpen = !PopupView.IsOpen;
            PopupFile.IsOpen = false;
            PopupTools.IsOpen = false;
            PopupHelp.IsOpen = false;
        }

        private void MenuTools_Click(object sender, RoutedEventArgs e)
        {
            PopupTools.IsOpen = !PopupTools.IsOpen;
            PopupFile.IsOpen = false;
            PopupView.IsOpen = false;
            PopupHelp.IsOpen = false;
        }

        private void MenuHelp_Click(object sender, RoutedEventArgs e)
        {
            PopupHelp.IsOpen = !PopupHelp.IsOpen;
            PopupFile.IsOpen = false;
            PopupView.IsOpen = false;
            PopupTools.IsOpen = false;
        }

        private async void CheckUpdate_Click(object sender, RoutedEventArgs e)
        {
            CloseAllPopups();
            StatusText.Text = "🔄 در حال بررسی بروزرسانی...";
            await UpdateChecker.CheckForUpdateAsync();
            StatusText.Text = "✅ بررسی بروزرسانی انجام شد";
        }
    }
}
