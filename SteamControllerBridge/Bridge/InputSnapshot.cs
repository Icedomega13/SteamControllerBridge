namespace SteamControllerBridge.Bridge;

internal sealed class InputSnapshot
{
    public static readonly InputSnapshot Empty = new();

    public DateTime UpdatedAtUtc { get; init; } = DateTime.MinValue;
    public bool A { get; init; }
    public bool B { get; init; }
    public bool X { get; init; }
    public bool Y { get; init; }
    public bool LeftShoulder { get; init; }
    public bool RightShoulder { get; init; }
    public bool LeftThumb { get; init; }
    public bool RightThumb { get; init; }
    public bool Back { get; init; }
    public bool Start { get; init; }
    public bool Guide { get; init; }
    public bool DPadUp { get; init; }
    public bool DPadDown { get; init; }
    public bool DPadLeft { get; init; }
    public bool DPadRight { get; init; }
    public bool L4 { get; init; }
    public bool L5 { get; init; }
    public bool R4 { get; init; }
    public bool R5 { get; init; }
    public bool LeftPadTouched { get; init; }
    public bool LeftPadClicked { get; init; }
    public bool RightPadTouched { get; init; }
    public bool RightPadClicked { get; init; }
    public byte LeftTrigger { get; init; }
    public byte RightTrigger { get; init; }
    public short LeftStickX { get; init; }
    public short LeftStickY { get; init; }
    public short RightStickX { get; init; }
    public short RightStickY { get; init; }
    public short LeftPadX { get; init; }
    public short LeftPadY { get; init; }
    public short RightPadX { get; init; }
    public short RightPadY { get; init; }
    public short AccelX { get; init; }
    public short AccelY { get; init; }
    public short AccelZ { get; init; }
    public short GyroX { get; init; }
    public short GyroY { get; init; }
    public short GyroZ { get; init; }

    public bool IsFresh => UpdatedAtUtc != DateTime.MinValue && DateTime.UtcNow - UpdatedAtUtc < TimeSpan.FromSeconds(1);

    public static InputSnapshot FromInput(SteamControllerInput input)
    {
        return new InputSnapshot
        {
            UpdatedAtUtc = DateTime.UtcNow,
            A = input.A,
            B = input.B,
            X = input.X,
            Y = input.Y,
            LeftShoulder = input.LeftShoulder,
            RightShoulder = input.RightShoulder,
            LeftThumb = input.LeftThumb,
            RightThumb = input.RightThumb,
            Back = input.Back,
            Start = input.Start,
            Guide = input.Guide,
            DPadUp = input.DPadUp,
            DPadDown = input.DPadDown,
            DPadLeft = input.DPadLeft,
            DPadRight = input.DPadRight,
            L4 = input.L4,
            L5 = input.L5,
            R4 = input.R4,
            R5 = input.R5,
            LeftPadTouched = input.LeftPadTouched,
            LeftPadClicked = input.LeftPadClicked,
            RightPadTouched = input.RightPadTouched,
            RightPadClicked = input.RightPadClicked,
            LeftTrigger = input.LeftTrigger,
            RightTrigger = input.RightTrigger,
            LeftStickX = input.LeftStickX,
            LeftStickY = input.LeftStickY,
            RightStickX = input.RightStickX,
            RightStickY = input.RightStickY,
            LeftPadX = input.LeftPadX,
            LeftPadY = input.LeftPadY,
            RightPadX = input.RightPadX,
            RightPadY = input.RightPadY,
            AccelX = input.AccelX,
            AccelY = input.AccelY,
            AccelZ = input.AccelZ,
            GyroX = input.GyroX,
            GyroY = input.GyroY,
            GyroZ = input.GyroZ
        };
    }
}
