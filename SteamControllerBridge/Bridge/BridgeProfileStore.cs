using System.Text.Json;

namespace SteamControllerBridge.Bridge;

internal static class BridgeProfileStore
{
    private const string Extension = ".scbprofile";
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static string ProfilesDirectory
    {
        get
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SteamControllerBridge",
                "Profiles");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    public static IReadOnlyList<string> ListProfiles()
    {
        return Directory.EnumerateFiles(ProfilesDirectory, $"*{Extension}")
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Cast<string>()
            .OrderBy(name => name, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }

    public static BridgeOptions Load(string profileName)
    {
        return LoadFromPath(GetProfilePath(profileName));
    }

    public static void Save(string profileName, BridgeOptions options)
    {
        var profile = ToProfileOptions(options);
        File.WriteAllText(GetProfilePath(profileName), JsonSerializer.Serialize(profile, JsonOptions));
    }

    public static string Import(string sourcePath)
    {
        var name = SanitizeProfileName(Path.GetFileNameWithoutExtension(sourcePath));
        var imported = LoadFromPath(sourcePath);
        Save(name, imported);
        return name;
    }

    public static void Export(string profileName, string destinationPath)
    {
        File.Copy(GetProfilePath(profileName), destinationPath, overwrite: true);
    }

    public static void Delete(string profileName)
    {
        var path = GetProfilePath(profileName);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    public static string GetProfilePath(string profileName)
    {
        var name = SanitizeProfileName(profileName);
        return Path.Combine(ProfilesDirectory, $"{name}{Extension}");
    }

    public static string SanitizeProfileName(string profileName)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(profileName.Trim()
            .Select(ch => invalid.Contains(ch) ? '_' : ch)
            .ToArray());
        return string.IsNullOrWhiteSpace(cleaned) ? "Profile" : cleaned;
    }

    public static bool Exists(string profileName)
    {
        return File.Exists(GetProfilePath(profileName));
    }

    public static string GetProfileSignature(BridgeOptions options)
    {
        return JsonSerializer.Serialize(ToProfileOptions(options), JsonOptions);
    }

    private static BridgeOptions LoadFromPath(string path)
    {
        var options = JsonSerializer.Deserialize<BridgeOptions>(File.ReadAllText(path)) ?? new BridgeOptions();
        options.Normalize();
        return options;
    }

    private static BridgeOptions ToProfileOptions(BridgeOptions options)
    {
        options.Normalize();
        var profile = JsonSerializer.Deserialize<BridgeOptions>(JsonSerializer.Serialize(options, JsonOptions)) ?? new BridgeOptions();
        profile.StartWithWindows = false;
        profile.StartMinimizedToTray = false;
        profile.AutoStartBridge = false;
        profile.AutoDisableForSteam = true;
        profile.DsuMotionServerEnabled = false;
        profile.StartupProfileName = string.Empty;
        profile.MidiHapticFilePath = string.Empty;
        profile.MidiHapticPlaybackMode = MidiHapticPlaybackMode.Simple;
        profile.Normalize();
        return profile;
    }
}
