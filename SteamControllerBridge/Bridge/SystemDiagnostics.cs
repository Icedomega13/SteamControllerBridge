using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace SteamControllerBridge.Bridge;

internal static class SystemDiagnostics
{
    public static bool IsSteamRunning()
    {
        return Process.GetProcessesByName("steam").Length > 0;
    }

    public static bool IsVigemBusInstalled()
    {
        using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\ViGEmBus");
        return key is not null;
    }

    public static string BuildReport()
    {
        var appVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";
        var steamRunning = IsSteamRunning() ? "yes" : "no";
        var vigemInstalled = IsVigemBusInstalled() ? "yes" : "no";
        var startup = StartupManager.IsEnabled() ? "yes" : "no";

        return string.Join(Environment.NewLine, new[]
        {
            "Steam Controller Bridge diagnostics",
            $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
            $"App version: {appVersion}",
            $"OS: {RuntimeInformation.OSDescription}",
            $".NET: {RuntimeInformation.FrameworkDescription}",
            $"Process architecture: {RuntimeInformation.ProcessArchitecture}",
            $"Steam running: {steamRunning}",
            $"ViGEmBus installed: {vigemInstalled}",
            $"Start with Windows: {startup}",
            $"Settings: {BridgeOptionsStore.OptionsPath}",
            $"Log: {BridgeLog.LogPath}",
            "Known state reports: 0x42, 0x45",
            string.Empty,
            "Recent log:",
            ReadRecentLog()
        });
    }

    private static string ReadRecentLog()
    {
        try
        {
            if (!File.Exists(BridgeLog.LogPath))
            {
                return "(no log yet)";
            }

            var lines = File.ReadLines(BridgeLog.LogPath).TakeLast(80);
            return string.Join(Environment.NewLine, lines);
        }
        catch (Exception ex)
        {
            return $"Could not read log: {ex.Message}";
        }
    }
}
