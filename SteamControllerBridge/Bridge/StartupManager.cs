using Microsoft.Win32;

namespace SteamControllerBridge.Bridge;

internal static class StartupManager
{
    private const string AppName = "SteamControllerBridge";
    private const string LegacyShortcutName = "Steam Controller Bridge.lnk";
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        return key?.GetValue(AppName) is string || File.Exists(LegacyStartupShortcutPath);
    }

    public static void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
            ?? Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);

        if (enabled)
        {
            key.SetValue(AppName, $"\"{Application.ExecutablePath}\"");
        }
        else
        {
            key.DeleteValue(AppName, throwOnMissingValue: false);
        }

        DeleteLegacyStartupShortcut();
    }

    public static void EnsureSingleStartupEntry()
    {
        if (!File.Exists(LegacyStartupShortcutPath))
        {
            return;
        }

        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
            ?? Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);
        key.SetValue(AppName, $"\"{Application.ExecutablePath}\"");
        DeleteLegacyStartupShortcut();
    }

    private static string LegacyStartupShortcutPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), LegacyShortcutName);

    private static void DeleteLegacyStartupShortcut()
    {
        try
        {
            if (File.Exists(LegacyStartupShortcutPath))
            {
                File.Delete(LegacyStartupShortcutPath);
            }
        }
        catch
        {
            // Startup cleanup should never prevent the app from opening.
        }
    }
}
