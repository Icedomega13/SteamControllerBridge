using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;

namespace SteamControllerBridge.Bridge;

internal sealed class XboxVirtualController : IDisposable
{
    private readonly ViGEmClient _client = new();
    private readonly IXbox360Controller _controller;
    private const int GyroStickMax = 32767;
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

    public void Update(SteamControllerInput input, BridgeOptions options, bool gyroAllowed)
    {
        if (!input.IsValid)
        {
            return;
        }

        var buttons = BuildButtons(input, options);

        _controller.SetButtonsFull(buttons);
        _controller.SetSliderValue(Xbox360Slider.LeftTrigger, BuildTriggerValue(input.LeftTrigger, ControllerInput.LeftTrigger, options.LeftTriggerTurbo, options));
        _controller.SetSliderValue(Xbox360Slider.RightTrigger, BuildTriggerValue(input.RightTrigger, ControllerInput.RightTrigger, options.RightTriggerTurbo, options));
        var leftX = options.LeftStickWasdEnabled ? (short)0 : ReadInt16(input.Report, 10);
        var leftY = options.LeftStickWasdEnabled ? (short)0 : ReadInt16(input.Report, 12);
        var rightX = ReadInt16(input.Report, 14);
        var rightY = ReadInt16(input.Report, 16);
        if (options.RightStickMouseEnabled)
        {
            rightX = 0;
            rightY = 0;
        }

        ApplyTrackpadStick(input, options, ref leftX, ref leftY, ref rightX, ref rightY);
        ApplyGyroRightStick(input, options, gyroAllowed, ref rightX, ref rightY);
        _controller.SetAxisValue(Xbox360Axis.LeftThumbX, leftX);
        _controller.SetAxisValue(Xbox360Axis.LeftThumbY, leftY);
        _controller.SetAxisValue(Xbox360Axis.RightThumbX, rightX);
        _controller.SetAxisValue(Xbox360Axis.RightThumbY, rightY);
        _controller.SubmitReport();
    }

    private static void ApplyTrackpadStick(
        SteamControllerInput input,
        BridgeOptions options,
        ref short leftX,
        ref short leftY,
        ref short rightX,
        ref short rightY)
    {
        if (!options.TrackpadStickEnabled ||
            !TryGetTrackpad(input, options.TrackpadStickSource, out var padX, out var padY))
        {
            return;
        }

        var x = ScaleTrackpadAxis(padX, options);
        var y = ScaleTrackpadAxis(padY, options);
        if (options.InvertTrackpadStickY)
        {
            y = (short)-y;
        }

        if (options.TrackpadStickOutput == TrackpadStickOutput.LeftStick)
        {
            leftX = x;
            leftY = y;
            return;
        }

        rightX = x;
        rightY = y;
    }

    private static bool TryGetTrackpad(SteamControllerInput input, TrackpadMouseSource source, out short x, out short y)
    {
        if ((source == TrackpadMouseSource.Left || source == TrackpadMouseSource.Both) && input.LeftPadTouched)
        {
            x = input.LeftPadX;
            y = input.LeftPadY;
            return true;
        }

        if ((source == TrackpadMouseSource.Right || source == TrackpadMouseSource.Both) && input.RightPadTouched)
        {
            x = input.RightPadX;
            y = input.RightPadY;
            return true;
        }

        x = 0;
        y = 0;
        return false;
    }

    private static short ScaleTrackpadAxis(short value, BridgeOptions options)
    {
        if (Math.Abs(value) < options.TrackpadStickDeadZone)
        {
            return 0;
        }

        var scaled = value * (options.TrackpadStickSensitivity / 100.0);
        return (short)Math.Clamp((int)Math.Round(scaled), short.MinValue, short.MaxValue);
    }

    private void ApplyGyroRightStick(SteamControllerInput input, BridgeOptions options, bool gyroAllowed, ref short rightX, ref short rightY)
    {
        if (!gyroAllowed || !options.GyroMouseEnabled || options.GyroOutputMode != GyroOutputMode.RightStick || !input.HasGyro)
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
        var gyroX = ApplyGyroStickDeadZone(-yaw, options);
        var gyroY = ApplyGyroStickDeadZone(-pitch, options);

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

    private static int ApplyGyroStickDeadZone(double value, BridgeOptions options)
    {
        if (Math.Abs(value) < options.GyroStickDeadZone)
        {
            return 0;
        }

        return (int)Math.Clamp(value * options.GyroStickSensitivity, -GyroStickMax, GyroStickMax);
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
        AddMappedButton(ref buttons, input.A, ControllerInput.A, options.GetBinding(PhysicalButton.A), options);
        AddMappedButton(ref buttons, input.B, ControllerInput.B, options.GetBinding(PhysicalButton.B), options);
        AddMappedButton(ref buttons, input.X, ControllerInput.X, options.GetBinding(PhysicalButton.X), options);
        AddMappedButton(ref buttons, input.Y, ControllerInput.Y, options.GetBinding(PhysicalButton.Y), options);
        AddMappedButton(ref buttons, input.LeftShoulder, ControllerInput.LeftShoulder, options.GetBinding(PhysicalButton.LeftShoulder), options);
        AddMappedButton(ref buttons, input.RightShoulder, ControllerInput.RightShoulder, options.GetBinding(PhysicalButton.RightShoulder), options);
        AddMappedButton(ref buttons, input.LeftThumb, ControllerInput.LeftThumb, options.GetBinding(PhysicalButton.LeftThumb), options);
        AddMappedButton(ref buttons, input.RightThumb, ControllerInput.RightThumb, options.GetBinding(PhysicalButton.RightThumb), options);
        AddMappedButton(ref buttons, input.Back, ControllerInput.Back, options.GetBinding(PhysicalButton.Back), options);
        AddMappedButton(ref buttons, input.Start, ControllerInput.Start, options.GetBinding(PhysicalButton.Start), options);
        AddMappedButton(ref buttons, input.Guide, ControllerInput.Guide, options.GetBinding(PhysicalButton.Guide), options);
        AddMappedButton(ref buttons, input.DPadUp, ControllerInput.DPadUp, options.GetBinding(PhysicalButton.DPadUp), options);
        AddMappedButton(ref buttons, input.DPadDown, ControllerInput.DPadDown, options.GetBinding(PhysicalButton.DPadDown), options);
        AddMappedButton(ref buttons, input.DPadLeft, ControllerInput.DPadLeft, options.GetBinding(PhysicalButton.DPadLeft), options);
        AddMappedButton(ref buttons, input.DPadRight, ControllerInput.DPadRight, options.GetBinding(PhysicalButton.DPadRight), options);
        AddMappedButton(ref buttons, input.L4, ControllerInput.L4, options.GetBinding(PhysicalButton.L4), options);
        AddMappedButton(ref buttons, input.L5, ControllerInput.L5, options.GetBinding(PhysicalButton.L5), options);
        AddMappedButton(ref buttons, input.R4, ControllerInput.R4, options.GetBinding(PhysicalButton.R4), options);
        AddMappedButton(ref buttons, input.R5, ControllerInput.R5, options.GetBinding(PhysicalButton.R5), options);
        return buttons;
    }

    private static void AddMappedButton(ref ushort buttons, bool pressed, ControllerInput input, ButtonBinding binding, BridgeOptions options)
    {
        if (options.HasKeyboardKey(input) || !pressed || binding.Turbo && !IsTurboPulseOn(options) || TryMapButton(binding.Output) is not { } button)
        {
            return;
        }

        buttons |= button.Value;
    }

    private static byte BuildTriggerValue(byte value, ControllerInput input, bool turbo, BridgeOptions options)
    {
        if (options.HasKeyboardKey(input) || value == 0)
        {
            return 0;
        }

        if (!turbo)
        {
            return value;
        }

        return IsTurboPulseOn(options) ? byte.MaxValue : (byte)0;
    }

    private static bool IsTurboPulseOn(BridgeOptions options)
    {
        return Environment.TickCount64 / options.TurboIntervalMs % 2 == 0;
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
