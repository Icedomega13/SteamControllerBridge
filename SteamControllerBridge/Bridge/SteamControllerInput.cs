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
    public bool LeftPadTouched => (B3 & SteamControllerReports.ButtonLeftPadTouch) != 0;
    public bool LeftPadClicked => (B3 & SteamControllerReports.ButtonLeftPadClick) != 0;
    public bool RightPadTouched => (B2 & SteamControllerReports.ButtonRightPadTouch) != 0;
    public bool RightPadClicked => (B2 & SteamControllerReports.ButtonRightPadClick) != 0;
    public short LeftPadX => ReadInt16(18);
    public short LeftPadY => ReadInt16(20);
    public short RightPadX => ReadInt16(24);
    public short RightPadY => ReadInt16(26);

    private short ReadInt16(int offset)
    {
        return (short)(_report[offset] | (_report[offset + 1] << 8));
    }
}
