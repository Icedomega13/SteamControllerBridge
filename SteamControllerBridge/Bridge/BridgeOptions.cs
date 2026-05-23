namespace SteamControllerBridge.Bridge;

internal sealed class BridgeOptions
{
    public PaddleMapping L4 { get; set; } = PaddleMapping.LeftShoulder;
    public PaddleMapping L5 { get; set; } = PaddleMapping.X;
    public PaddleMapping R4 { get; set; } = PaddleMapping.RightShoulder;
    public PaddleMapping R5 { get; set; } = PaddleMapping.B;
    public bool TrackpadMouseEnabled { get; set; }
    public TrackpadMouseSource TrackpadMouseSource { get; set; } = TrackpadMouseSource.Right;
    public bool TrackpadClickEnabled { get; set; } = true;
    public bool GyroMouseEnabled { get; set; }
    public GyroMouseActivation GyroMouseActivation { get; set; } = GyroMouseActivation.LeftTrigger;
    public bool RumbleEnabled { get; set; } = true;
    public bool DarkModeEnabled { get; set; }
}

internal enum PaddleMapping
{
    Disabled,
    A,
    B,
    X,
    Y,
    LeftShoulder,
    RightShoulder,
    LeftThumb,
    RightThumb,
    Back,
    Start,
    DPadUp,
    DPadDown,
    DPadLeft,
    DPadRight
}

internal enum TrackpadMouseSource
{
    Right,
    Left,
    Both
}

internal enum GyroMouseActivation
{
    LeftTrigger,
    RightTrigger,
    LeftPadTouch,
    RightPadTouch,
    Always
}
