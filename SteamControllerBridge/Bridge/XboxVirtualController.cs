using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;

namespace SteamControllerBridge.Bridge;

internal sealed class XboxVirtualController : IDisposable
{
    private readonly ViGEmClient _client = new();
    private readonly IXbox360Controller _controller;

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

        ushort buttons = 0;

        AddButton(ref buttons, input.B0, SteamControllerReports.ButtonA, Xbox360Button.A);
        AddButton(ref buttons, input.B0, SteamControllerReports.ButtonB, Xbox360Button.B);
        AddButton(ref buttons, input.B0, SteamControllerReports.ButtonX, Xbox360Button.X);
        AddButton(ref buttons, input.B0, SteamControllerReports.ButtonY, Xbox360Button.Y);
        AddButton(ref buttons, input.B0, SteamControllerReports.ButtonMenu, Xbox360Button.Start);
        AddButton(ref buttons, input.B0, SteamControllerReports.ButtonRightStick, Xbox360Button.RightThumb);

        AddButton(ref buttons, input.B1, SteamControllerReports.ButtonView, Xbox360Button.Back);
        AddButton(ref buttons, input.B1, SteamControllerReports.ButtonLeftStick, Xbox360Button.LeftThumb);
        AddButton(ref buttons, input.B1, SteamControllerReports.ButtonRightBumper, Xbox360Button.RightShoulder);
        AddButton(ref buttons, input.B1, SteamControllerReports.ButtonDPadUp, Xbox360Button.Up);
        AddButton(ref buttons, input.B1, SteamControllerReports.ButtonDPadDown, Xbox360Button.Down);
        AddButton(ref buttons, input.B1, SteamControllerReports.ButtonDPadLeft, Xbox360Button.Left);
        AddButton(ref buttons, input.B1, SteamControllerReports.ButtonDPadRight, Xbox360Button.Right);

        AddButton(ref buttons, input.B2, SteamControllerReports.ButtonSteam, Xbox360Button.Guide);
        AddButton(ref buttons, input.B2, SteamControllerReports.ButtonLeftBumper, Xbox360Button.LeftShoulder);
        AddMappedButton(ref buttons, input.L4, options.L4);
        AddMappedButton(ref buttons, input.L5, options.L5);
        AddMappedButton(ref buttons, input.R4, options.R4);
        AddMappedButton(ref buttons, input.R5, options.R5);

        _controller.SetButtonsFull(buttons);
        _controller.SetSliderValue(Xbox360Slider.LeftTrigger, TriggerToByte(input.Report[6], input.Report[7]));
        _controller.SetSliderValue(Xbox360Slider.RightTrigger, TriggerToByte(input.Report[8], input.Report[9]));
        _controller.SetAxisValue(Xbox360Axis.LeftThumbX, ReadInt16(input.Report, 10));
        _controller.SetAxisValue(Xbox360Axis.LeftThumbY, ReadInt16(input.Report, 12));
        _controller.SetAxisValue(Xbox360Axis.RightThumbX, ReadInt16(input.Report, 14));
        _controller.SetAxisValue(Xbox360Axis.RightThumbY, ReadInt16(input.Report, 16));
        _controller.SubmitReport();
    }

    private static void AddButton(ref ushort buttons, byte source, byte mask, Xbox360Button button)
    {
        if ((source & mask) != 0)
        {
            buttons |= button.Value;
        }
    }

    private static void AddMappedButton(ref ushort buttons, bool pressed, PaddleMapping mapping)
    {
        if (!pressed || TryMapButton(mapping) is not { } button)
        {
            return;
        }

        buttons |= button.Value;
    }

    private static Xbox360Button? TryMapButton(PaddleMapping mapping)
    {
        return mapping switch
        {
            PaddleMapping.A => Xbox360Button.A,
            PaddleMapping.B => Xbox360Button.B,
            PaddleMapping.X => Xbox360Button.X,
            PaddleMapping.Y => Xbox360Button.Y,
            PaddleMapping.LeftShoulder => Xbox360Button.LeftShoulder,
            PaddleMapping.RightShoulder => Xbox360Button.RightShoulder,
            PaddleMapping.LeftThumb => Xbox360Button.LeftThumb,
            PaddleMapping.RightThumb => Xbox360Button.RightThumb,
            PaddleMapping.Back => Xbox360Button.Back,
            PaddleMapping.Start => Xbox360Button.Start,
            PaddleMapping.DPadUp => Xbox360Button.Up,
            PaddleMapping.DPadDown => Xbox360Button.Down,
            PaddleMapping.DPadLeft => Xbox360Button.Left,
            PaddleMapping.DPadRight => Xbox360Button.Right,
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
