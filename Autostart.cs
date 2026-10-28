namespace CicdLight;

internal static class Autostart
{
    private const string Key = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public static void Set(bool enabled)
    {
        using var k = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(Key, writable: true);
        if (k == null) return;
        if (enabled) k.SetValue("CicdLight", $"\"{Environment.ProcessPath}\"");
        else k.DeleteValue("CicdLight", throwOnMissingValue: false);
    }
}
