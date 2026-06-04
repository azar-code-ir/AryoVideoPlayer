using System;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace AryoVideoPlayer.Helpers;

public static class UpdateChecker
{
    private const string Owner = "azar-code-ir";
    private const string Repo = "AryoVideoPlayer";
    private const string ApiUrl = $"https://api.github.com/repos/{Owner}/{Repo}/releases/latest";

    public static string CurrentVersion => Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";

    public static async Task CheckForUpdateAsync()
    {
        try
        {
            using var client = new WebClient();
            client.Headers.Add("User-Agent", "AryoVideoPlayer");
            var json = await client.DownloadStringTaskAsync(ApiUrl);

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            string latestTag = root.GetProperty("tag_name").GetString() ?? "";
            string latestVersion = latestTag.TrimStart('v');
            string body = root.GetProperty("body").GetString() ?? "";
            string downloadUrl = "";

            if (root.TryGetProperty("assets", out var assets) && assets.GetArrayLength() > 0)
            {
                downloadUrl = assets[0].GetProperty("browser_download_url").GetString() ?? "";
            }

            if (new Version(latestVersion) > new Version(CurrentVersion))
            {
                var result = System.Windows.MessageBox.Show(
                    $"نسخه جدید {latestTag} موجود است!\n\n" +
                    $"نسخه فعلی: {CurrentVersion}\n" +
                    $"نسخه جدید: {latestVersion}\n\n" +
                    $"تغییرات:\n{body}\n\n" +
                    $"آیا میخواهید دانلود کنید؟",
                    "بروزرسانی Aryo Video Player",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (result == MessageBoxResult.Yes)
                {
                    if (!string.IsNullOrEmpty(downloadUrl))
                        Process.Start(new ProcessStartInfo(downloadUrl) { UseShellExecute = true });
                    else
                        Process.Start(new ProcessStartInfo($"https://github.com/{Owner}/{Repo}/releases/latest") { UseShellExecute = true });
                }
            }
        }
        catch
        {
            //无声失败 - 不打扰用户
        }
    }
}
