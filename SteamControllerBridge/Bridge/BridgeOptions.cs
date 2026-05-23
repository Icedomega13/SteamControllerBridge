namespace SteamControllerBridge.Bridge;

internal sealed class BridgeOptions
{
    public PaddleMapping L4 { get; set; } = PaddleMapping.Y;
    public PaddleMapping L5 { get; set; } = PaddleMapping.X;
    public PaddleMapping R4 { get; set; } = PaddleMapping.B;
    public PaddleMapping R5 { get; set; } = PaddleMapping.A;
    public ButtonBinding MapA { get; set; } = new(GamepadButton.A);
    public ButtonBinding MapB { get; set; } = new(GamepadButton.B);
    public ButtonBinding MapX { get; set; } = new(GamepadButton.X);
    public ButtonBinding MapY { get; set; } = new(GamepadButton.Y);
    public ButtonBinding MapLeftShoulder { get; set; } = new(GamepadButton.LeftShoulder);
    public ButtonBinding MapRightShoulder { get; set; } = new(GamepadButton.RightShoulder);
    public ButtonBinding MapLeftThumb { get; set; } = new(GamepadButton.LeftThumb);
    public ButtonBinding MapRightThumb { get; set; } = new(GamepadButton.RightThumb);
    public ButtonBinding MapBack { get; set; } = new(GamepadButton.Back);
    public ButtonBinding MapStart { get; set; } = new(GamepadButton.Start);
    public ButtonBinding MapGuide { get; set; } = new(GamepadButton.Guide);
    public ButtonBinding MapDPadUp { get; set; } = new(GamepadButton.DPadUp);
    public ButtonBinding MapDPadDown { get; set; } = new(GamepadButton.DPadDown);
    public ButtonBinding MapDPadLeft { get; set; } = new(GamepadButton.DPadLeft);
    public ButtonBinding MapDPadRight { get; set; } = new(GamepadButton.DPadRight);
    public ButtonBinding MapL4 { get; set; } = new(GamepadButton.Y);
    public ButtonBinding MapL5 { get; set; } = new(GamepadButton.X);
    public ButtonBinding MapR4 { get; set; } = new(GamepadButton.B);
    public ButtonBinding MapR5 { get; set; } = new(GamepadButton.A);
    public bool TrackpadMouseEnabled { get; set; }
    public TrackpadMouseSource TrackpadMouseSource { get; set; } = TrackpadMouseSource.Right;
    public bool TrackpadClickEnabled { get; set; } = true;
    public bool GyroMouseEnabled { get; set; }
    public GyroMouseActivation GyroMouseActivation { get; set; } = GyroMouseActivation.LeftTrigger;
    public GyroToggleButton GyroToggleButton { get; set; } = GyroToggleButton.Disabled;
    public GyroOutputMode GyroOutputMode { get; set; } = GyroOutputMode.Mouse;
    public int GyroStickSensitivity { get; set; } = 26;
    public int GyroStickDeadZone { get; set; } = 45;
    public int TurboIntervalMs { get; set; } = 80;
    public bool LeftTriggerTurbo { get; set; }
    public bool RightTriggerTurbo { get; set; }
    public bool RumbleEnabled { get; set; } = true;
    public bool DarkModeEnabled { get; set; }
    public bool StartWithWindows { get; set; }
    public bool AutoDisableForSteam { get; set; } = true;
    public Dictionary<string, int> KeyboardKeys { get; set; } = new();

    public void Normalize()
    {
        MapA ??= new(GamepadButton.A);
        MapB ??= new(GamepadButton.B);
        MapX ??= new(GamepadButton.X);
        MapY ??= new(GamepadButton.Y);
        MapLeftShoulder ??= new(GamepadButton.LeftShoulder);
        MapRightShoulder ??= new(GamepadButton.RightShoulder);
        MapLeftThumb ??= new(GamepadButton.LeftThumb);
        MapRightThumb ??= new(GamepadButton.RightThumb);
        MapBack ??= new(GamepadButton.Back);
        MapStart ??= new(GamepadButton.Start);
        MapGuide ??= new(GamepadButton.Guide);
        MapDPadUp ??= new(GamepadButton.DPadUp);
        MapDPadDown ??= new(GamepadButton.DPadDown);
        MapDPadLeft ??= new(GamepadButton.DPadLeft);
        MapDPadRight ??= new(GamepadButton.DPadRight);
        MapL4 ??= new(ToGamepadButton(L4));
        MapL5 ??= new(ToGamepadButton(L5));
        MapR4 ??= new(ToGamepadButton(R4));
        MapR5 ??= new(ToGamepadButton(R5));
        KeyboardKeys ??= new Dictionary<string, int>();
        GyroStickSensitivity = Math.Clamp(GyroStickSensitivity, 1, 80);
        GyroStickDeadZone = Math.Clamp(GyroStickDeadZone, 0, 300);
        TurboIntervalMs = Math.Clamp(TurboIntervalMs, 25, 500);
    }

    public int GetKeyboardKey(ControllerInput input)
    {
        return KeyboardKeys.TryGetValue(input.ToString(), out var key) ? key : 0;
    }

    public bool HasKeyboardKey(ControllerInput input)
    {
        return GetKeyboardKey(input) > 0;
    }

    public void SetKeyboardKey(ControllerInput input, int key)
    {
        if (key <= 0)
        {
            KeyboardKeys.Remove(input.ToString());
            return;
        }

        KeyboardKeys[input.ToString()] = key;
    }

    public ButtonBinding GetBinding(PhysicalButton button)
    {
        return button switch
        {
            PhysicalButton.A => MapA,
            PhysicalButton.B => MapB,
            PhysicalButton.X => MapX,
            PhysicalButton.Y => MapY,
            PhysicalButton.LeftShoulder => MapLeftShoulder,
            PhysicalButton.RightShoulder => MapRightShoulder,
            PhysicalButton.LeftThumb => MapLeftThumb,
            PhysicalButton.RightThumb => MapRightThumb,
            PhysicalButton.Back => MapBack,
            PhysicalButton.Start => MapStart,
            PhysicalButton.Guide => MapGuide,
            PhysicalButton.DPadUp => MapDPadUp,
            PhysicalButton.DPadDown => MapDPadDown,
            PhysicalButton.DPadLeft => MapDPadLeft,
            PhysicalButton.DPadRight => MapDPadRight,
            PhysicalButton.L4 => MapL4,
            PhysicalButton.L5 => MapL5,
            PhysicalButton.R4 => MapR4,
            PhysicalButton.R5 => MapR5,
            _ => new ButtonBinding(GamepadButton.Disabled)
        };
    }

    public static GamepadButton ToGamepadButton(PaddleMapping mapping)
    {
        return mapping switch
        {
            PaddleMapping.A => GamepadButton.A,
            PaddleMapping.B => GamepadButton.B,
            PaddleMapping.X => GamepadButton.X,
            PaddleMapping.Y => GamepadButton.Y,
            PaddleMapping.LeftShoulder => GamepadButton.LeftShoulder,
            PaddleMapping.RightShoulder => GamepadButton.RightShoulder,
            PaddleMapping.LeftThumb => GamepadButton.LeftThumb,
            PaddleMapping.RightThumb => GamepadButton.RightThumb,
            PaddleMapping.Back => GamepadButton.Back,
            PaddleMapping.Start => GamepadButton.Start,
            PaddleMapping.DPadUp => GamepadButton.DPadUp,
            PaddleMapping.DPadDown => GamepadButton.DPadDown,
            PaddleMapping.DPadLeft => GamepadButton.DPadLeft,
            PaddleMapping.DPadRight => GamepadButton.DPadRight,
            _ => GamepadButton.Disabled
        };
    }

    public void ApplyPreset(RemapPreset preset)
    {
        ResetButtonBindings();
        TrackpadMouseEnabled = false;
        GyroMouseEnabled = false;
        GyroOutputMode = GyroOutputMode.Mouse;
        GyroMouseActivation = GyroMouseActivation.LeftTrigger;
        GyroToggleButton = GyroToggleButton.Disabled;

        switch (preset)
        {
            case RemapPreset.NintendoSwap:
                MapA.Output = GamepadButton.B;
                MapB.Output = GamepadButton.A;
                MapX.Output = GamepadButton.Y;
                MapY.Output = GamepadButton.X;
                break;
            case RemapPreset.FpsGyroMouse:
                GyroMouseEnabled = true;
                GyroOutputMode = GyroOutputMode.Mouse;
                GyroMouseActivation = GyroMouseActivation.LeftTrigger;
                break;
            case RemapPreset.FpsGyroStick:
                GyroMouseEnabled = true;
                GyroOutputMode = GyroOutputMode.RightStick;
                GyroMouseActivation = GyroMouseActivation.LeftTrigger;
                break;
            case RemapPreset.DesktopMouse:
                TrackpadMouseEnabled = true;
                TrackpadMouseSource = TrackpadMouseSource.Right;
                GyroMouseEnabled = true;
                GyroOutputMode = GyroOutputMode.Mouse;
                GyroMouseActivation = GyroMouseActivation.RightPadTouch;
                break;
        }
    }

    private void ResetButtonBindings()
    {
        SetBinding(MapA, GamepadButton.A);
        SetBinding(MapB, GamepadButton.B);
        SetBinding(MapX, GamepadButton.X);
        SetBinding(MapY, GamepadButton.Y);
        SetBinding(MapLeftShoulder, GamepadButton.LeftShoulder);
        SetBinding(MapRightShoulder, GamepadButton.RightShoulder);
        SetBinding(MapLeftThumb, GamepadButton.LeftThumb);
        SetBinding(MapRightThumb, GamepadButton.RightThumb);
        SetBinding(MapBack, GamepadButton.Back);
        SetBinding(MapStart, GamepadButton.Start);
        SetBinding(MapGuide, GamepadButton.Guide);
        SetBinding(MapDPadUp, GamepadButton.DPadUp);
        SetBinding(MapDPadDown, GamepadButton.DPadDown);
        SetBinding(MapDPadLeft, GamepadButton.DPadLeft);
        SetBinding(MapDPadRight, GamepadButton.DPadRight);
        SetBinding(MapL4, GamepadButton.Y);
        SetBinding(MapL5, GamepadButton.X);
        SetBinding(MapR4, GamepadButton.B);
        SetBinding(MapR5, GamepadButton.A);
        LeftTriggerTurbo = false;
        RightTriggerTurbo = false;
    }

    private static void SetBinding(ButtonBinding binding, GamepadButton output)
    {
        binding.Output = output;
        binding.Turbo = false;
    }
}

internal sealed class ButtonBinding
{
    public ButtonBinding()
    {
    }

    public ButtonBinding(GamepadButton output)
    {
        Output = output;
    }

    public GamepadButton Output { get; set; }
    public bool Turbo { get; set; }
}

internal enum PhysicalButton
{
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
    Guide,
    DPadUp,
    DPadDown,
    DPadLeft,
    DPadRight,
    L4,
    L5,
    R4,
    R5
}

internal enum ControllerInput
{
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
    Guide,
    DPadUp,
    DPadDown,
    DPadLeft,
    DPadRight,
    L4,
    L5,
    R4,
    R5,
    LeftTrigger,
    RightTrigger,
    LeftPadClick,
    RightPadClick
}

internal enum GamepadButton
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
    Guide,
    DPadUp,
    DPadDown,
    DPadLeft,
    DPadRight
}

internal enum RemapPreset
{
    DefaultXbox,
    NintendoSwap,
    FpsGyroMouse,
    FpsGyroStick,
    DesktopMouse
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
    L4,
    L5,
    R4,
    R5,
    LeftPadTouch,
    RightPadTouch,
    Always
}

internal enum GyroToggleButton
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
    Guide,
    DPadUp,
    DPadDown,
    DPadLeft,
    DPadRight,
    L4,
    L5,
    R4,
    R5,
    LeftTrigger,
    RightTrigger,
    LeftPadTouch,
    RightPadTouch
}

internal enum GyroOutputMode
{
    Mouse,
    RightStick
}
