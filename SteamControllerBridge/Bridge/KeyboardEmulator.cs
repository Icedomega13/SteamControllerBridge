namespace SteamControllerBridge.Bridge;

internal sealed class KeyboardEmulator
{
    private readonly HashSet<ushort> _downKeys = [];

    public void Update(SteamControllerInput input, BridgeOptions options)
    {
        var desiredKeys = new HashSet<ushort>();
        foreach (var control in Enum.GetValues<ControllerInput>())
        {
            var key = (ushort)Math.Clamp(options.GetKeyboardKey(control), 0, ushort.MaxValue);
            var pressed = key != 0 && IsPressed(input, control) && IsTurboOn(control, options);
            if (pressed)
            {
                desiredKeys.Add(key);
            }
        }

        foreach (var key in _downKeys.Except(desiredKeys).ToArray())
        {
            KeyboardInput.SetKey(key, down: false);
            _downKeys.Remove(key);
        }

        foreach (var key in desiredKeys.Except(_downKeys))
        {
            KeyboardInput.SetKey(key, down: true);
            _downKeys.Add(key);
        }
    }

    public void Reset()
    {
        foreach (var key in _downKeys)
        {
            KeyboardInput.SetKey(key, down: false);
        }

        _downKeys.Clear();
    }

    private static bool IsTurboOn(ControllerInput control, BridgeOptions options)
    {
        return control switch
        {
            ControllerInput.A => !options.MapA.Turbo || IsTurboPulseOn(options),
            ControllerInput.B => !options.MapB.Turbo || IsTurboPulseOn(options),
            ControllerInput.X => !options.MapX.Turbo || IsTurboPulseOn(options),
            ControllerInput.Y => !options.MapY.Turbo || IsTurboPulseOn(options),
            ControllerInput.LeftShoulder => !options.MapLeftShoulder.Turbo || IsTurboPulseOn(options),
            ControllerInput.RightShoulder => !options.MapRightShoulder.Turbo || IsTurboPulseOn(options),
            ControllerInput.LeftThumb => !options.MapLeftThumb.Turbo || IsTurboPulseOn(options),
            ControllerInput.RightThumb => !options.MapRightThumb.Turbo || IsTurboPulseOn(options),
            ControllerInput.Back => !options.MapBack.Turbo || IsTurboPulseOn(options),
            ControllerInput.Start => !options.MapStart.Turbo || IsTurboPulseOn(options),
            ControllerInput.Guide => !options.MapGuide.Turbo || IsTurboPulseOn(options),
            ControllerInput.DPadUp => !options.MapDPadUp.Turbo || IsTurboPulseOn(options),
            ControllerInput.DPadDown => !options.MapDPadDown.Turbo || IsTurboPulseOn(options),
            ControllerInput.DPadLeft => !options.MapDPadLeft.Turbo || IsTurboPulseOn(options),
            ControllerInput.DPadRight => !options.MapDPadRight.Turbo || IsTurboPulseOn(options),
            ControllerInput.L4 => !options.MapL4.Turbo || IsTurboPulseOn(options),
            ControllerInput.L5 => !options.MapL5.Turbo || IsTurboPulseOn(options),
            ControllerInput.R4 => !options.MapR4.Turbo || IsTurboPulseOn(options),
            ControllerInput.R5 => !options.MapR5.Turbo || IsTurboPulseOn(options),
            ControllerInput.LeftTrigger => !options.LeftTriggerTurbo || IsTurboPulseOn(options),
            ControllerInput.RightTrigger => !options.RightTriggerTurbo || IsTurboPulseOn(options),
            _ => true
        };
    }

    private static bool IsTurboPulseOn(BridgeOptions options)
    {
        return Environment.TickCount64 / options.TurboIntervalMs % 2 == 0;
    }

    private static bool IsPressed(SteamControllerInput input, ControllerInput control)
    {
        return control switch
        {
            ControllerInput.A => input.A,
            ControllerInput.B => input.B,
            ControllerInput.X => input.X,
            ControllerInput.Y => input.Y,
            ControllerInput.LeftShoulder => input.LeftShoulder,
            ControllerInput.RightShoulder => input.RightShoulder,
            ControllerInput.LeftThumb => input.LeftThumb,
            ControllerInput.RightThumb => input.RightThumb,
            ControllerInput.Back => input.Back,
            ControllerInput.Start => input.Start,
            ControllerInput.Guide => input.Guide,
            ControllerInput.DPadUp => input.DPadUp,
            ControllerInput.DPadDown => input.DPadDown,
            ControllerInput.DPadLeft => input.DPadLeft,
            ControllerInput.DPadRight => input.DPadRight,
            ControllerInput.L4 => input.L4,
            ControllerInput.L5 => input.L5,
            ControllerInput.R4 => input.R4,
            ControllerInput.R5 => input.R5,
            ControllerInput.LeftTrigger => input.LeftTriggerActive,
            ControllerInput.RightTrigger => input.RightTriggerActive,
            ControllerInput.LeftPadClick => input.LeftPadClicked,
            ControllerInput.RightPadClick => input.RightPadClicked,
            _ => false
        };
    }
}
