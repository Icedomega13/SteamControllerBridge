namespace SteamControllerBridge.Bridge;

internal sealed record BridgeStatus(bool IsEnabled, bool IsWorking, bool HasError, string Message)
{
    public static BridgeStatus Idle(string message) => new(false, false, false, message);
    public static BridgeStatus Working(string message) => new(false, true, false, message);
    public static BridgeStatus Enabled(string message) => new(true, false, false, message);
    public static BridgeStatus Failed(string message) => new(false, false, true, message);
}
