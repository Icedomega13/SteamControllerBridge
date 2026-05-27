namespace SteamControllerBridge;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        using var singleInstance = new Mutex(true, "SteamControllerBridge.SingleInstance", out var created);
        if (!created)
        {
            return;
        }

        Bridge.StartupManager.EnsureSingleStartupEntry();
        Application.Run(new MainForm());
    }
}
