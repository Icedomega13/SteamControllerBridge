using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;

namespace SteamControllerBridge.Bridge;

internal sealed class XboxVirtualController : IDisposable
{
    private readonly ViGEmClient _client = new();
    private readonly IXbox360Controller _controller;
    private const int TurboIntervalMs = 80;
    private const int GyroStickDeadZone = 120;
    private const double GyroStickSensitivity = 1.15;
    private const int GyroStickMax = 22000;
    private bool _gyroStickActive;
    private double _gyroStickBiasX;
    private double _gyroStickBiasZ;

    public event EventHandler<XboxRumbleEventArgs>? RumbleReceived;

    public XboxVirtualController()
    {
        _controller = _client.CreateXbox360Controller();
        ((IVirtualGamepad)_controller).AutoSubmitReport = false;
        _controller.FeedbackReceived += OnFeedbackReceived;
        _controller.Connect();
    }

    public void Update(SteamControllerInput input, BridgeOptions options)
    {
        if (!input.IsValid)
        {
            return;
        }

        var buttons = BuildButtons(input, options);

        _controller.SetButtonsFull(buttons);
        _controller.SetSliderValue(Xbox360Slider.LeftTrigger, TriggerToByte(input.Report[6], input.Report[7]));
        _controller.SetSliderValue(Xbox360Slider.RightTrigger, TriggerToByte(input.Report[8], input.Report[9]));
        _controller.SetAxisValue(Xbox360Axis.LeftThumbX, ReadInt16(input.Report, 10));
        _controller.SetAxisValue(Xbox360Axis.LeftThumbY, ReadInt16(input.Report, 12));
        var rightX = ReadInt16(input.Report, 14);
        var rightY = ReadInt16(input.Report, 16);
        ApplyGyroRightStick(input, options, ref rightX, ref rightY);
        _controller.SetAxisValue(Xbox360Axis.RightThumbX, rightX);
        _controller.SetAxisValue(Xbox360Axis.RightThumbY, rightY);
        _controller.SubmitReport();
    }

    private void ApplyGyroRightStick(SteamControllerInput input, BridgeOptions options, ref short rightX, ref short rightY)
    {
        if (!options.GyroMouseEnabled || options.GyroOutputMode != GyroOutputMode.RightStick || !input.HasGyro)
        {
            _gyroStickActive = false;
            return;
        }

        if (!IsGyroActive(input, options.GyroMouseActivation))
        {
            LearnGyroStickBias(input, fast: false);
            _gyroStickActive = false;
            return;
        }

        if (!_gyroStickActive)
        {
            LearnGyroStickBias(input, fast: true);
            _gyroStickActive = true;
            return;
        }

        var yaw = input.GyroZ - _gyroStickBiasZ;
        var pitch = input.GyroX - _gyroStickBiasX;
        var gyroX = ApplyGyroStickDeadZone(-yaw);
        var gyroY = ApplyGyroStickDeadZone(-pitch);

        rightX = AddAxis(rightX, gyroX);
        rightY = AddAxis(rightY, gyroY);
    }

    private void LearnGyroStickBias(SteamControllerInput input, bool fast)
    {
        var weight = fast ? 0.65 : 0.04;
        _gyroStickBiasX = Lerp(_gyroStickBiasX, input.GyroX, weight);
        _gyroStickBiasZ = Lerp(_gyroStickBiasZ, input.GyroZ, weight);
    }

    private static short AddAxis(short current, int delta)
    {
        return (short)Math.Clamp(current + delta, short.MinValue, short.MaxValue);
    }

    private static int ApplyGyroStickDeadZone(double value)
    {
        if (Math.Abs(value) < GyroStickDeadZone)
        {
            return 0;
        }

        return (int)Math.Clamp(value * GyroStickSensitivity, -GyroStickMax, GyroStickMax);
    }

    private static bool IsGyroActive(SteamControllerInput input, GyroMouseActivation activation)
    {
        return activation switch
        {
            GyroMouseActivation.LeftTrigger => input.LeftTriggerActive,
            GyroMouseActivation.RightTrigger => input.RightTriggerActive,
            GyroMouseActivation.L4 => input.L4,
            GyroMouseActivation.L5 => input.L5,
            GyroMouseActivation.R4 => input.R4,
            GyroMouseActivation.R5 => input.R5,
            GyroMouseActivation.LeftPadTouch => input.LeftPadTouched,
            GyroMouseActivation.RightPadTouch => input.RightPadTouched,
            GyroMouseActivation.Always => true,
            _ => false
        };
    }

    private static ushort BuildButtons(SteamControllerInput input, BridgeOptions options)
    {
        ushort buttons = 0;
        AddMappedButton(ref buttons, input.A, options.GetBinding(PhysicalButton.A));
        AddMappedButton(ref buttons, input.B, options.GetBinding(PhysicalButton.B));
        AddMappedButton(ref buttons, input.X, options.GetBinding(PhysicalButton.X));
        AddMappedButton(ref buttons, input.Y, options.GetBinding(PhysicalButton.Y));
        AddMappedButton(ref buttons, input.LeftShoulder, options.GetBinding(PhysicalButton.LeftShoulder));
        AddMappedButton(ref buttons, input.RightShoulder, options.GetBinding(PhysicalButton.RightShoulder));
        AddMappedButton(ref buttons, input.LeftThumb, options.GetBinding(PhysicalButton.LeftThumb));
        AddMappedButton(ref buttons, input.RightThumb, options.GetBinding(PhysicalButton.RightThumb));
        AddMappedButton(ref buttons, input.Back, options.GetBinding(PhysicalButton.Back));
        AddMappedButton(ref buttons, input.Start, options.GetBinding(PhysicalButton.Start));
        AddMappedButton(ref buttons, input.Guide, options.GetBinding(PhysicalButton.Guide));
        AddMappedButton(ref buttons, input.DPadUp, options.GetBinding(PhysicalButton.DPadUp));
        AddMappedButton(ref buttons, input.DPadDown, options.GetBinding(PhysicalButton.DPadDown));
        AddMappedButton(ref buttons, input.DPadLeft, options.GetBinding(PhysicalButton.DPadLeft));
        AddMappedButton(ref buttons, input.DPadRight, options.GetBinding(PhysicalButton.DPadRight));
        AddMappedButton(ref buttons, input.L4, options.GetBinding(PhysicalButton.L4));
        AddMappedButton(ref buttons, input.L5, options.GetBinding(PhysicalButton.L5));
        AddMappedButton(ref buttons, input.R4, options.GetBinding(PhysicalButton.R4));
        AddMappedButton(ref buttons, input.R5, options.GetBinding(PhysicalButton.R5));
        return buttons;
    }

    private static void AddMappedButton(ref ushort buttons, bool pressed, ButtonBinding binding)
    {
        if (!pressed || binding.Turbo && !IsTurboPulseOn() || TryMapButton(binding.Output) is not { } button)
        {
            return;
        }

        buttons |= button.Value;
    }

    private static bool IsTurboPulseOn()
    {
        return Environment.TickCount64 / TurboIntervalMs % 2 == 0;
    }

    private static double Lerp(double current, double target, double weight)
    {
        return current + ((target - current) * weight);
    }

    private static Xbox360Button? TryMapButton(GamepadButton mapping)
    {
        return mapping switch
        {
            GamepadButton.A => Xbox360Button.A,
            GamepadButton.B => Xbox360Button.B,
            GamepadButton.X => Xbox360Button.X,
            GamepadButton.Y => Xbox360Button.Y,
            GamepadButton.LeftShoulder => Xbox360Button.LeftShoulder,
            GamepadButton.RightShoulder => Xbox360Button.RightShoulder,
            GamepadButton.LeftThumb => Xbox360Button.LeftThumb,
            GamepadButton.RightThumb => Xbox360Button.RightThumb,
            GamepadButton.Back => Xbox360Button.Back,
            GamepadButton.Start => Xbox360Button.Start,
            GamepadButton.Guide => Xbox360Button.Guide,
            GamepadButton.DPadUp => Xbox360Button.Up,
            GamepadButton.DPadDown => Xbox360Button.Down,
            GamepadButton.DPadLeft => Xbox360Button.Left,
            GamepadButton.DPadRight => Xbox360Button.Right,
            _ => null
        };
    }

    private void OnFeedbackReceived(object? sender, Xbox360FeedbackReceivedEventArgs e)
    {
        RumbleReceived?.Invoke(this, new XboxRumbleEventArgs(e.SmallMotor, e.LargeMotor));
    }

    private static byte TriggerToByte(byte lo, byte hi)
    {
        var value = Math.Max(0, (int)(short)(lo | (hi << 8)));
        return (byte)Math.Clamp(value >> 7, 0, 255);
    }

    private static short ReadInt16(ReadOnlySpan<byte> report, int offset)
    {
        return (short)(report[offset] | (report[offset + 1] << 8));
    }

    public void Dispose()
    {
        _controller.FeedbackReceived -= OnFeedbackReceived;
        _controller.Disconnect();
        _client.Dispose();
    }
}

internal sealed record XboxRumbleEventArgs(byte SmallMotor, byte LargeMotor);
