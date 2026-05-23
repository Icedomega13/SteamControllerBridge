namespace SteamControllerBridge.Bridge;

internal static class BridgeLog
{
    private static readonly object Gate = new();

    public static string LogPath
    {
        get
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SteamControllerBridge");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "bridge.log");
        }
    }

    public static void Write(string message)
    {
        lock (Gate)
        {
            File.AppendAllText(LogPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}  {message}{Environment.NewLine}");
        }
    }
}
