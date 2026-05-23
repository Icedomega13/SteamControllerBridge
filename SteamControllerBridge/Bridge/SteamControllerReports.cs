namespace SteamControllerBridge.Bridge;

internal static class SteamControllerReports
{
    public const ushort ValveVendorId = 0x28DE;
    public const ushort WiredProductId = 0x1302;
    public const ushort PuckProductId = 0x1304;
    public const ushort VendorUsagePage = 0xFF00;

    public const byte StateReportId = 0x42;
    public const byte StateBleReportId = 0x45;
    public const byte HapticRumbleReportId = 0x80;
    public const byte FeatureCommandReportId = 0x01;
    public const byte CommandClearDigitalMappings = 0x81;
    public const byte CommandSetDefaultMappings = 0x85;
    public const byte CommandSetSettings = 0x87;
    public const byte SettingRightTrackpadMode = 0x07;
    public const byte SettingLeftTrackpadMode = 0x08;
    public const byte SettingImuMode = 0x30;
    public const ushort GyroModeSendRawAccel = 0x0008;
    public const ushort GyroModeSendRawGyro = 0x0010;

    public const byte ButtonA = 0x01;
    public const byte ButtonB = 0x02;
    public const byte ButtonX = 0x04;
    public const byte ButtonY = 0x08;
    public const byte ButtonRightStick = 0x20;
    public const byte ButtonMenu = 0x40;
    public const byte ButtonR4 = 0x80;

    public const byte ButtonR5 = 0x01;
    public const byte ButtonRightBumper = 0x02;
    public const byte ButtonDPadDown = 0x04;
    public const byte ButtonDPadRight = 0x08;
    public const byte ButtonDPadLeft = 0x10;
    public const byte ButtonDPadUp = 0x20;
    public const byte ButtonView = 0x40;
    public const byte ButtonLeftStick = 0x80;

    public const byte ButtonSteam = 0x01;
    public const byte ButtonL4 = 0x02;
    public const byte ButtonL5 = 0x04;
    public const byte ButtonLeftBumper = 0x08;
    public const byte ButtonRightPadTouch = 0x20;
    public const byte ButtonRightPadClick = 0x40;

    public const byte ButtonLeftPadTouch = 0x02;
    public const byte ButtonLeftPadClick = 0x04;

    public static bool IsStateReport(byte reportId)
    {
        return reportId is StateReportId or StateBleReportId;
    }
}
