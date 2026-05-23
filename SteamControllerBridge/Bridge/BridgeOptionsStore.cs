using System.Text.Json;

namespace SteamControllerBridge.Bridge;

internal static class BridgeOptionsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static string OptionsPath
    {
        get
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SteamControllerBridge");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "settings.json");
        }
    }

    public static BridgeOptions Load()
    {
        try
        {
            if (!File.Exists(OptionsPath))
            {
                return new BridgeOptions();
            }

            var options = JsonSerializer.Deserialize<BridgeOptions>(File.ReadAllText(OptionsPath)) ?? new BridgeOptions();
            options.Normalize();
            return options;
        }
        catch
        {
            var options = new BridgeOptions();
            options.Normalize();
            return options;
        }
    }

    public static void Save(BridgeOptions options)
    {
        options.Normalize();
        File.WriteAllText(OptionsPath, JsonSerializer.Serialize(options, JsonOptions));
    }
}
