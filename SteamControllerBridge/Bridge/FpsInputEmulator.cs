namespace SteamControllerBridge.Bridge;

internal sealed class FpsInputEmulator
{
    private const int StickDeadZone = 9000;
    private const ushort VK_A = 0x41;
    private const ushort VK_D = 0x44;
    private const ushort VK_S = 0x53;
    private const ushort VK_W = 0x57;

    private readonly HashSet<ushort> _downKeys = [];

    public event EventHandler<string>? LogWritten;

    public void Update(SteamControllerInput input, BridgeOptions options)
    {
        if (!input.IsValid)
        {
            return;
        }

        UpdateWasd(input, options);
        UpdateRightStickMouse(input, options);
    }

    public void Reset()
    {
        foreach (var key in _downKeys)
        {
            KeyboardInput.SetKey(key, down: false, out _);
        }

        _downKeys.Clear();
    }

    private void UpdateWasd(SteamControllerInput input, BridgeOptions options)
    {
        var desiredKeys = new HashSet<ushort>();
        if (options.LeftStickWasdEnabled)
        {
            var leftX = ReadInt16(input.Report, 10);
            var leftY = ReadInt16(input.Report, 12);

            if (leftX < -StickDeadZone)
            {
                desiredKeys.Add(VK_A);
            }
            else if (leftX > StickDeadZone)
            {
                desiredKeys.Add(VK_D);
            }

            if (leftY > StickDeadZone)
            {
                desiredKeys.Add(VK_W);
            }
            else if (leftY < -StickDeadZone)
            {
                desiredKeys.Add(VK_S);
            }
        }

        foreach (var key in _downKeys.Except(desiredKeys).ToArray())
        {
            if (!KeyboardInput.SetKey(key, down: false, out var errorCode))
            {
                LogWritten?.Invoke(this, $"WASD key up failed for 0x{key:X2}. Win32 error: {errorCode}");
            }

            _downKeys.Remove(key);
        }

        foreach (var key in desiredKeys.Except(_downKeys))
        {
            if (KeyboardInput.SetKey(key, down: true, out var errorCode))
            {
                _downKeys.Add(key);
            }
            else
            {
                LogWritten?.Invoke(this, $"WASD key down failed for 0x{key:X2}. Win32 error: {errorCode}");
            }
        }
    }

    private static void UpdateRightStickMouse(SteamControllerInput input, BridgeOptions options)
    {
        if (!options.RightStickMouseEnabled)
        {
            return;
        }

        var rightX = ReadInt16(input.Report, 14);
        var rightY = ReadInt16(input.Report, 16);
        var dx = ScaleStick(rightX, options.RightStickMouseSensitivity);
        var dy = ScaleStick(rightY, options.RightStickMouseSensitivity);
        if (!options.InvertRightStickY)
        {
            dy = -dy;
        }

        if (dx != 0 || dy != 0)
        {
            MouseInput.Move(dx, dy);
        }
    }

    private static int ScaleStick(short value, int sensitivity)
    {
        if (Math.Abs(value) < StickDeadZone)
        {
            return 0;
        }

        var normalized = (Math.Abs(value) - StickDeadZone) / (double)(short.MaxValue - StickDeadZone);
        var signed = Math.Sign(value) * normalized;
        return (int)Math.Clamp(signed * sensitivity, -80, 80);
    }

    private static short ReadInt16(ReadOnlySpan<byte> report, int offset)
    {
        return (short)(report[offset] | (report[offset + 1] << 8));
    }
}
