using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;

namespace SteamControllerBridge.Bridge;

internal sealed class XboxVirtualController : IDisposable
{
    private readonly ViGEmClient _client = new();
    private readonly IXbox360Controller _controller;
    private const int TurboIntervalMs = 80;

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
        _controller.SetAxisValue(Xbox360Axis.RightThumbX, ReadInt16(input.Report, 14));
        _controller.SetAxisValue(Xbox360Axis.RightThumbY, ReadInt16(input.Report, 16));
        _controller.SubmitReport();
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
