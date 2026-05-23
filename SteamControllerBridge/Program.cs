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
            MessageBox.Show("Steam Controller Bridge is already running.", "Steam Controller Bridge",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        Application.Run(new MainForm());
    }
}
