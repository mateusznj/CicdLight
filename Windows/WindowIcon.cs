using Microsoft.UI.Xaml;

namespace CicdLight;

internal static class WindowIcon
{
    private static readonly string Path = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico");

    /// <summary>Ustawia ikonę aplikacji na pasku tytułu i w pasku zadań.</summary>
    public static void Apply(Window window)
    {
        if (File.Exists(Path)) window.AppWindow.SetIcon(Path);
    }
}
