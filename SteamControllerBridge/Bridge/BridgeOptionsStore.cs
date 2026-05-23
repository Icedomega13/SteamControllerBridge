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

            return JsonSerializer.Deserialize<BridgeOptions>(File.ReadAllText(OptionsPath)) ?? new BridgeOptions();
        }
        catch
        {
            return new BridgeOptions();
        }
    }

    public static void Save(BridgeOptions options)
    {
        File.WriteAllText(OptionsPath, JsonSerializer.Serialize(options, JsonOptions));
    }
}
