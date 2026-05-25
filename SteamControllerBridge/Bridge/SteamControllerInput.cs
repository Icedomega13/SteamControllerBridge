namespace SteamControllerBridge.Bridge;

internal readonly ref struct SteamControllerInput
{
    private readonly ReadOnlySpan<byte> _report;

    public SteamControllerInput(ReadOnlySpan<byte> report)
    {
        _report = report;
    }

    public bool IsValid => _report.Length >= 30 && SteamControllerReports.IsStateReport(_report[0]);
    public ReadOnlySpan<byte> Report => _report;
    public byte B0 => _report[2];
    public byte B1 => _report[3];
    public byte B2 => _report[4];
    public byte B3 => _report[5];
    public bool L4 => (B2 & SteamControllerReports.ButtonL4) != 0;
    public bool L5 => (B2 & SteamControllerReports.ButtonL5) != 0;
    public bool R4 => (B0 & SteamControllerReports.ButtonR4) != 0;
    public bool R5 => (B1 & SteamControllerReports.ButtonR5) != 0;
    public bool A => (B0 & SteamControllerReports.ButtonA) != 0;
    public bool B => (B0 & SteamControllerReports.ButtonB) != 0;
    public bool X => (B0 & SteamControllerReports.ButtonX) != 0;
    public bool Y => (B0 & SteamControllerReports.ButtonY) != 0;
    public bool LeftShoulder => (B2 & SteamControllerReports.ButtonLeftBumper) != 0;
    public bool RightShoulder => (B1 & SteamControllerReports.ButtonRightBumper) != 0;
    public bool LeftThumb => (B1 & SteamControllerReports.ButtonLeftStick) != 0;
    public bool RightThumb => (B0 & SteamControllerReports.ButtonRightStick) != 0;
    public bool Back => (B1 & SteamControllerReports.ButtonView) != 0;
    public bool Start => (B0 & SteamControllerReports.ButtonMenu) != 0;
    public bool Guide => (B2 & SteamControllerReports.ButtonSteam) != 0;
    public bool DPadUp => (B1 & SteamControllerReports.ButtonDPadUp) != 0;
    public bool DPadDown => (B1 & SteamControllerReports.ButtonDPadDown) != 0;
    public bool DPadLeft => (B1 & SteamControllerReports.ButtonDPadLeft) != 0;
    public bool DPadRight => (B1 & SteamControllerReports.ButtonDPadRight) != 0;
    public bool LeftPadTouched => (B3 & SteamControllerReports.ButtonLeftPadTouch) != 0;
    public bool LeftPadClicked => (B3 & SteamControllerReports.ButtonLeftPadClick) != 0;
    public bool RightPadTouched => (B2 & SteamControllerReports.ButtonRightPadTouch) != 0;
    public bool RightPadClicked => (B2 & SteamControllerReports.ButtonRightPadClick) != 0;
    public bool LeftTriggerActive => LeftTrigger > 32;
    public bool RightTriggerActive => RightTrigger > 32;
    public byte LeftTrigger => TriggerToByte(ReadInt16(6));
    public byte RightTrigger => TriggerToByte(ReadInt16(8));
    public short LeftStickX => ReadInt16(10);
    public short LeftStickY => ReadInt16(12);
    public short RightStickX => ReadInt16(14);
    public short RightStickY => ReadInt16(16);
    public short LeftPadX => ReadInt16(18);
    public short LeftPadY => ReadInt16(20);
    public short RightPadX => ReadInt16(24);
    public short RightPadY => ReadInt16(26);
    public bool HasGyro => _report.Length >= 46;
    public short AccelX => HasGyro ? ReadInt16(34) : (short)0;
    public short AccelY => HasGyro ? ReadInt16(36) : (short)0;
    public short AccelZ => HasGyro ? ReadInt16(38) : (short)0;
    public short GyroX => HasGyro ? ReadInt16(40) : (short)0;
    public short GyroY => HasGyro ? ReadInt16(42) : (short)0;
    public short GyroZ => HasGyro ? ReadInt16(44) : (short)0;

    private short ReadInt16(int offset)
    {
        return (short)(_report[offset] | (_report[offset + 1] << 8));
    }

    private static byte TriggerToByte(short value)
    {
        return (byte)Math.Clamp(Math.Max(0, (int)value) >> 7, 0, 255);
    }
}
