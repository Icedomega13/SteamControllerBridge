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
            "Valve HID interfaces:",
            BuildValveHidReport(),
            string.Empty,
            "Recent log:",
            ReadRecentLog()
        });
    }

    private static string BuildValveHidReport()
    {
        try
        {
            var interfaces = HidDevice.EnumerateInterfaces()
                .Where(device => device.VendorId == SteamControllerReports.ValveVendorId ||
                                 device.Path.Contains("vid_28de", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (interfaces.Count == 0)
            {
                return "No Valve HID interfaces found. Expected VID_28DE with PID_1302 or PID_1304.";
            }

            return string.Join(Environment.NewLine, interfaces.Select(FormatHidInterface));
        }
        catch (Exception ex)
        {
            return $"Could not enumerate HID interfaces: {ex.Message}";
        }
    }

    private static string FormatHidInterface(HidInterfaceInfo device)
    {
        var supported = device.VendorId == SteamControllerReports.ValveVendorId &&
                        (device.ProductId == SteamControllerReports.WiredProductId ||
                         device.ProductId == SteamControllerReports.PuckProductId);
        var metadata = device.CanOpenMetadata ? "metadata=yes" : $"metadata=no ({device.OpenError ?? "unknown error"})";

        return string.Join(Environment.NewLine, new[]
        {
            $"- VID=0x{device.VendorId:X4} PID=0x{device.ProductId:X4} REV=0x{device.VersionNumber:X4} UsagePage=0x{device.UsagePage:X4} Usage=0x{device.Usage:X4} Input={device.InputReportLength} Output={device.OutputReportLength} Feature={device.FeatureReportLength} SupportedPid={(supported ? "yes" : "no")} {metadata}",
            $"  Path={device.Path}"
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
