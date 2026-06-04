using System.Windows;

namespace AryoVideoPlayer
{
    public partial class LanguageSelectionWindow : Window
    {
        // زبان مبدأ و مقصد
        public string SourceLanguage { get; private set; } = "en";
        public string TargetLanguage { get; private set; } = "fa";

        // نقشه کد زبان‌ها
        private readonly string[] _langCodes = {
            "fa", "en", "fr", "de", "es", "it", "ja", "ko", "zh", "ar", "tr", "ru"
        };

        public LanguageSelectionWindow()
        {
            InitializeComponent();
        }

        // دکمه ترجمه
        private void Translate_Click(object sender, RoutedEventArgs e)
        {
            // دریافت کد زبان از انتخاب کاربر
            SourceLanguage = _langCodes[SourceLangComboBox.SelectedIndex];
            TargetLanguage = _langCodes[TargetLangComboBox.SelectedIndex];

            // بررسی اینکه زبان مبدأ و مقصد متفاوت باشند
            if (SourceLanguage == TargetLanguage)
            {
                DarkMessageBox.ShowWarning("زبان مبدأ و مقصد نباید یکسان باشند!", "خطا", this);
                return;
            }

            DialogResult = true;
        }

        // دکمه لغو
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
