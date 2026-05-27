namespace SteamControllerBridge.Bridge;

internal sealed class SteamControllerDevice : IDisposable
{
    private readonly HidDevice _device;
    private readonly CancellationTokenSource _heartbeatCts = new();
    private readonly Action<string>? _log;
    private Task? _heartbeatTask;

    private SteamControllerDevice(HidDevice device, Action<string>? log)
    {
        _device = device;
        _log = log;
    }

    public static SteamControllerDevice? OpenFirst(Action<string>? log = null)
    {
        foreach (var strictUsage in new[] { true, false })
        {
            foreach (var productId in SteamControllerReports.SupportedProductIds)
            {
                var usagePage = strictUsage ? SteamControllerReports.VendorUsagePage : (ushort)0;
                log?.Invoke($"Scanning Valve HID interfaces. Product=0x{productId:X4}, UsagePage={(usagePage == 0 ? "any" : $"0x{usagePage:X4}")}.");
                foreach (var path in HidDevice.Enumerate(
                             SteamControllerReports.ValveVendorId,
                             productId,
                             usagePage).Distinct())
                {
                    log?.Invoke($"Found candidate HID interface: {path}");
                    var device = HidDevice.Open(path);
                    if (device is null)
                    {
                        log?.Invoke("Could not open candidate interface for read/write access.");
                        continue;
                    }

                    log?.Invoke($"Opened candidate. FeatureReportLength={device.FeatureReportLength}, OutputReportLength={device.OutputReportLength}. Waiting for controller input reports.");
                    var probe = new byte[64];
                    var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(2);
                    while (DateTime.UtcNow < deadline)
                    {
                        var read = device.ReadReportAsync(probe, TimeSpan.FromMilliseconds(100), CancellationToken.None)
                            .GetAwaiter()
                            .GetResult();

                        if (read > 0 && SteamControllerReports.IsStateReport(probe[0]))
                        {
                            log?.Invoke($"Candidate is live. First report length={read}.");
                            return new SteamControllerDevice(device, log);
                        }

                        if (read > 0)
                        {
                            log?.Invoke($"Candidate emitted non-controller report 0x{probe[0]:X2}, length={read}.");
                        }
                    }

                    log?.Invoke("Candidate did not emit a live controller report before timeout.");
                    device.Dispose();
                }
            }
        }

        return null;
    }

    public bool DisableLizardMode()
    {
        if (!SendCommand(SteamControllerReports.CommandClearDigitalMappings))
        {
            _log?.Invoke("Command failed: clear digital mappings.");
            return false;
        }

        var settings = new byte[]
        {
            SteamControllerReports.SettingLeftTrackpadMode, 0x00, 0x00,
            SteamControllerReports.SettingRightTrackpadMode, 0x00, 0x00,
            SteamControllerReports.SettingImuMode,
            (byte)(SteamControllerReports.GyroModeSendRawAccel | SteamControllerReports.GyroModeSendRawGyro),
            0x00
        };

        if (!SendCommand(SteamControllerReports.CommandSetSettings, settings))
        {
            _log?.Invoke("Command failed: set trackpad/lizard settings.");
            return false;
        }

        _log?.Invoke("Lizard mode disabled.");
        _heartbeatTask ??= Task.Run(HeartbeatLoop);
        return true;
    }

    public bool EnableLizardMode()
    {
        _heartbeatCts.Cancel();
        SendRumble(0, 0);
        try
        {
            _heartbeatTask?.Wait(TimeSpan.FromSeconds(1));
        }
        catch
        {
            // Best effort only.
        }

        var restored = SendCommand(SteamControllerReports.CommandSetDefaultMappings);
        _log?.Invoke(restored ? "Lizard mode restored." : "Could not restore lizard mode.");
        return restored;
    }

    public Task<int> ReadReportAsync(byte[] buffer, TimeSpan timeout, CancellationToken token)
    {
        return _device.ReadReportAsync(buffer, timeout, token);
    }

    public bool SendRumble(byte smallMotor, byte largeMotor)
    {
        Span<byte> report = stackalloc byte[10];
        report.Clear();
        report[0] = SteamControllerReports.HapticRumbleReportId;
        report[1] = 0x00;

        // Best-effort Steam Controller 2026 haptic output report layout, matching the
        // public SDL driver shape: intensity, left speed/gain, right speed/gain.
        WriteUInt16(report, 2, (ushort)Math.Clamp(Math.Max(smallMotor, largeMotor) * 3, 0, ushort.MaxValue));
        WriteUInt16(report, 4, 160);
        report[6] = ToSignedGain(smallMotor);
        WriteUInt16(report, 7, 320);
        report[9] = ToSignedGain(largeMotor);
        return _device.SendOutputReport(report);
    }

    public bool PlayHapticTone(byte channel, ushort frequency, byte velocity = 120)
    {
        Span<byte> report = stackalloc byte[10];
        report.Clear();
        report[0] = SteamControllerReports.HapticPlayToneReportId;
        report[1] = channel;
        report[2] = velocity;
        WriteUInt16(report, 3, frequency);
        report[5] = 0xFF;
        report[6] = 0x18;
        return _device.SendOutputReport(report);
    }

    public bool StopHapticTone(byte channel)
    {
        Span<byte> report = stackalloc byte[10];
        report.Clear();
        report[0] = SteamControllerReports.HapticStopToneReportId;
        report[1] = channel;
        return _device.SendOutputReport(report);
    }

    private async Task HeartbeatLoop()
    {
        while (!_heartbeatCts.IsCancellationRequested)
        {
            SendCommand(SteamControllerReports.CommandClearDigitalMappings);
            try
            {
                await Task.Delay(800, _heartbeatCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private bool SendCommand(byte command, ReadOnlySpan<byte> payload = default)
    {
        Span<byte> report = stackalloc byte[64];
        report.Clear();
        report[0] = SteamControllerReports.FeatureCommandReportId;
        report[1] = command;
        report[2] = (byte)payload.Length;
        payload.CopyTo(report[3..]);
        return _device.SendFeatureReport(report);
    }

    private static void WriteUInt16(Span<byte> report, int offset, ushort value)
    {
        report[offset] = (byte)value;
        report[offset + 1] = (byte)(value >> 8);
    }

    private static byte ToSignedGain(byte motor)
    {
        return (byte)Math.Clamp(motor / 2, 0, 127);
    }

    public void Dispose()
    {
        _heartbeatCts.Cancel();
        SendRumble(0, 0);
        _heartbeatCts.Dispose();
        _device.Dispose();
    }
}
