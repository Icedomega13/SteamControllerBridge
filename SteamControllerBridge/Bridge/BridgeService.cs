using Nefarius.ViGEm.Client.Exceptions;

namespace SteamControllerBridge.Bridge;

internal sealed class BridgeService : IDisposable
{
    private readonly object _gate = new();
    private CancellationTokenSource? _readLoopCts;
    private Task? _readLoopTask;
    private SteamControllerDevice? _controller;
    private XboxVirtualController? _virtualController;
    private readonly MouseEmulator _mouse = new();
    private readonly object _rumbleGate = new();
    private byte _rumbleSmall;
    private byte _rumbleLarge;
    private byte _lastSentRumbleSmall;
    private byte _lastSentRumbleLarge;
    private DateTime _nextRumbleAt = DateTime.MinValue;

    public event EventHandler<BridgeStatus>? StatusChanged;
    public event EventHandler<string>? LogWritten;

    public BridgeStatus Status { get; private set; } = BridgeStatus.Idle("Off");
    public BridgeOptions Options { get; } = BridgeOptionsStore.Load();

    public void SaveOptions()
    {
        BridgeOptionsStore.Save(Options);
        if (!Options.RumbleEnabled)
        {
            StopRumble();
        }
    }

    public void Start()
    {
        lock (_gate)
        {
            if (Status.IsEnabled || Status.IsWorking)
            {
                return;
            }

            SetStatus(BridgeStatus.Working("Connecting to Steam Controller..."));
            Log($"Starting bridge. Log file: {BridgeLog.LogPath}");

            try
            {
                _controller = SteamControllerDevice.OpenFirst(Log);
                if (_controller is null)
                {
                    SetStatus(BridgeStatus.Failed("Steam Controller not found"));
                    Log("Startup failed: no live Steam Controller HID interface was found.");
                    return;
                }

                Log("Steam Controller connected.");
                SetStatus(BridgeStatus.Working("Disabling lizard mode..."));

                if (!_controller.DisableLizardMode())
                {
                    CleanupController(restoreLizard: true);
                    SetStatus(BridgeStatus.Failed("Could not disable lizard mode"));
                    Log("Startup failed: disable lizard mode command sequence failed.");
                    return;
                }

                SetStatus(BridgeStatus.Working("Creating virtual Xbox controller..."));
                Log("Creating ViGEm virtual Xbox 360 controller.");
                _virtualController = new XboxVirtualController();
                _virtualController.RumbleReceived += OnRumbleReceived;

                _readLoopCts = new CancellationTokenSource();
                _readLoopTask = Task.Run(() => ReadLoop(_readLoopCts.Token));

                SetStatus(BridgeStatus.Enabled("Virtual Xbox controller on"));
                Log("Virtual Xbox 360 controller is active.");
            }
            catch (VigemBusNotFoundException)
            {
                CleanupController(restoreLizard: true);
                SetStatus(BridgeStatus.Failed("ViGEmBus is not installed"));
                Log("Startup failed: ViGEmBus was not found.");
            }
            catch (Exception ex)
            {
                CleanupController(restoreLizard: true);
                SetStatus(BridgeStatus.Failed(ex.Message));
                Log(ex.ToString());
            }
        }
    }

    public void Stop()
    {
        lock (_gate)
        {
            if (!Status.IsEnabled && _controller is null && _virtualController is null)
            {
                SetStatus(BridgeStatus.Idle("Off"));
                return;
            }

            SetStatus(BridgeStatus.Working("Turning off..."));
            StopReadLoop();
            _mouse.Reset();
            _controller?.SendRumble(0, 0);
            _virtualController?.Dispose();
            _virtualController = null;
            CleanupController(restoreLizard: true);
            SetStatus(BridgeStatus.Idle("Off"));
            Log("Bridge stopped.");
        }
    }

    private async Task ReadLoop(CancellationToken token)
    {
        var buffer = new byte[64];

        while (!token.IsCancellationRequested)
        {
            try
            {
                var count = await _controller!.ReadReportAsync(buffer, TimeSpan.FromMilliseconds(50), token)
                    .ConfigureAwait(false);

                if (count == 0 || !SteamControllerReports.IsStateReport(buffer[0]))
                {
                    continue;
                }

                var input = new SteamControllerInput(buffer.AsSpan(0, count));
                _virtualController?.Update(input, Options);
                _mouse.Update(input, Options);
                MaybeSendRumble();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Log($"Read loop stopped: {ex.Message}");
                BeginStopAfterFailure("Controller disconnected");
                break;
            }
        }
    }

    private void BeginStopAfterFailure(string message)
    {
        Task.Run(() =>
        {
            lock (_gate)
            {
                StopReadLoop();
                _mouse.Reset();
                _virtualController?.Dispose();
                _virtualController = null;
                CleanupController(restoreLizard: false);
                SetStatus(BridgeStatus.Failed(message));
            }
        });
    }

    private void StopReadLoop()
    {
        _readLoopCts?.Cancel();
        try
        {
            _readLoopTask?.Wait(TimeSpan.FromSeconds(1));
        }
        catch
        {
            // Best effort shutdown; the device handle is closed next.
        }

        _readLoopTask = null;
        _readLoopCts?.Dispose();
        _readLoopCts = null;
    }

    private void CleanupController(bool restoreLizard)
    {
        if (_controller is null)
        {
            return;
        }

        if (restoreLizard)
        {
            try
            {
                _controller.EnableLizardMode();
            }
            catch (Exception ex)
            {
                Log($"Could not restore lizard mode: {ex.Message}");
            }
        }

        _controller.Dispose();
        _controller = null;
    }

    private void OnRumbleReceived(object? sender, XboxRumbleEventArgs e)
    {
        lock (_rumbleGate)
        {
            _rumbleSmall = Options.RumbleEnabled ? e.SmallMotor : (byte)0;
            _rumbleLarge = Options.RumbleEnabled ? e.LargeMotor : (byte)0;
        }

        if (!Options.RumbleEnabled || e.SmallMotor == 0 && e.LargeMotor == 0)
        {
            StopRumble();
        }
    }

    private void MaybeSendRumble()
    {
        if (!Options.RumbleEnabled)
        {
            StopRumble();
            return;
        }

        byte small;
        byte large;
        lock (_rumbleGate)
        {
            small = _rumbleSmall;
            large = _rumbleLarge;
        }

        if (small == 0 && large == 0)
        {
            return;
        }

        if (DateTime.UtcNow < _nextRumbleAt)
        {
            return;
        }

        _controller?.SendRumble(small, large);
        _lastSentRumbleSmall = small;
        _lastSentRumbleLarge = large;
        _nextRumbleAt = DateTime.UtcNow.AddMilliseconds(40);
    }

    private void StopRumble()
    {
        lock (_rumbleGate)
        {
            _rumbleSmall = 0;
            _rumbleLarge = 0;
        }

        if (_lastSentRumbleSmall == 0 && _lastSentRumbleLarge == 0)
        {
            return;
        }

        _controller?.SendRumble(0, 0);
        _lastSentRumbleSmall = 0;
        _lastSentRumbleLarge = 0;
        _nextRumbleAt = DateTime.MinValue;
    }

    private void SetStatus(BridgeStatus status)
    {
        Status = status;
        StatusChanged?.Invoke(this, status);
    }

    private void Log(string message)
    {
        BridgeLog.Write(message);
        LogWritten?.Invoke(this, $"{DateTime.Now:HH:mm:ss}  {message}");
    }

    public void Dispose()
    {
        Stop();
    }
}
