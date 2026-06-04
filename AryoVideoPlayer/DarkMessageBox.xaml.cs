using System.Windows;
using System.Windows.Media;

namespace AryoVideoPlayer
{
    public partial class DarkMessageBox : Window
    {
        public bool Result { get; private set; } = false;

        public DarkMessageBox()
        {
            InitializeComponent();
        }

        public static void Show(string message, string title = "Aryo", Window owner = null)
        {
            var msg = new DarkMessageBox
            {
                TitleText = { Text = title },
                MessageText = { Text = message },
                IconText = { Text = "i" }
            };
            msg.IconBorder.Background = new SolidColorBrush(Color.FromRgb(0x7C, 0x6A, 0xFF));
            if (owner != null) msg.Owner = owner;
            msg.ShowDialog();
        }

        public static void ShowError(string message, string title = "خطا", Window owner = null)
        {
            var msg = new DarkMessageBox
            {
                TitleText = { Text = title },
                MessageText = { Text = message },
                IconText = { Text = "✕" }
            };
            msg.IconBorder.Background = new SolidColorBrush(Color.FromRgb(0xE8, 0x11, 0x23));
            if (owner != null) msg.Owner = owner;
            msg.ShowDialog();
        }

        public static void ShowWarning(string message, string title = "هشدار", Window owner = null)
        {
            var msg = new DarkMessageBox
            {
                TitleText = { Text = title },
                MessageText = { Text = message },
                IconText = { Text = "!" }
            };
            msg.IconBorder.Background = new SolidColorBrush(Color.FromRgb(0xFF, 0xAA, 0x00));
            if (owner != null) msg.Owner = owner;
            msg.ShowDialog();
        }

        public static void ShowSuccess(string message, string title = "موفقیت", Window owner = null)
        {
            var msg = new DarkMessageBox
            {
                TitleText = { Text = title },
                MessageText = { Text = message },
                IconText = { Text = "✓" }
            };
            msg.IconBorder.Background = new SolidColorBrush(Color.FromRgb(0x00, 0xC8, 0x53));
            if (owner != null) msg.Owner = owner;
            msg.ShowDialog();
        }

        public static bool ShowAlert(string message, string title, Window owner, string okText, string cancelText = null)
        {
            var msg = new DarkMessageBox
            {
                TitleText = { Text = title },
                MessageText = { Text = message },
                IconText = { Text = "i" }
            };
            msg.IconBorder.Background = new SolidColorBrush(Color.FromRgb(0x7C, 0x6A, 0xFF));
            msg.OkButton.Visibility = Visibility.Visible;
            if (!string.IsNullOrEmpty(okText)) msg.OkButton.Content = okText;
            if (!string.IsNullOrEmpty(cancelText))
            {
                msg.CancelButton.Content = cancelText;
                msg.CancelButton.Visibility = Visibility.Visible;
            }
            else
            {
                msg.CancelButton.Visibility = Visibility.Collapsed;
                msg.OkButton.Margin = new Thickness(0);
            }
            if (owner != null) msg.Owner = owner;
            msg.ShowDialog();
            return msg.Result;
        }

        public static string ShowInput(string message, string title, Window owner, string defaultValue = "")
        {
            var msg = new DarkMessageBox
            {
                TitleText = { Text = title },
                MessageText = { Text = message },
                IconText = { Text = "⌨" },
                InputBox = { Visibility = Visibility.Visible, Text = defaultValue }
            };
            msg.IconBorder.Background = new SolidColorBrush(Color.FromRgb(0x7C, 0x6A, 0xFF));
            msg.OkButton.Visibility = Visibility.Visible;
            msg.CancelButton.Visibility = Visibility.Visible;
            msg.InputBox.Focus();
            msg.InputBox.SelectAll();
            if (owner != null) msg.Owner = owner;
            msg.ShowDialog();
            return msg.Result ? msg.InputBox.Text : null;
        }

        private void OkBtn_Click(object sender, RoutedEventArgs e) { Result = true; Close(); }
        private void CancelBtn_Click(object sender, RoutedEventArgs e) { Result = false; Close(); }
        private void YesBtn_Click(object sender, RoutedEventArgs e) { Result = true; Close(); }
        private void NoBtn_Click(object sender, RoutedEventArgs e) { Result = false; Close(); }
        private void CloseBtn_Click(object sender, RoutedEventArgs e) { Result = false; Close(); }
    }
}
