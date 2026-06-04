using System;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace AryoVideoPlayer;

public partial class App : Application
{
    public static string[] StartupArgs { get; internal set; } = Array.Empty<string>();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        StartupArgs = e.Args;
        RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.Default;
    }
}
