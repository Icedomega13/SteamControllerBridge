using System.Diagnostics;
using Nefarius.ViGEm.Client.Exceptions;

namespace SteamControllerBridge.Bridge;

internal sealed class BridgeService : IDisposable
{
    private const int MaxSteamControllerRumbleByte = 150;
    private readonly object _gate = new();
    private CancellationTokenSource? _readLoopCts;
    private Task? _readLoopTask;
    private SteamControllerDevice? _controller;
    private XboxVirtualController? _virtualController;
    private readonly MouseEmulator _mouse = new();
    private readonly GyroMouseEmulator _gyroMouse = new();
    private readonly KeyboardEmulator _keyboard = new();
    private readonly FpsInputEmulator _fpsInput = new();
    private readonly DsuMotionServer _dsuMotionServer;
    private readonly object _rumbleGate = new();
    private byte _rumbleSmall;
    private byte _rumbleLarge;
    private byte _lastSentRumbleSmall;
    private byte _lastSentRumbleLarge;
    private DateTime _nextRumbleAt = DateTime.MinValue;
    private DateTime _nextReconnectAt = DateTime.MinValue;
    private bool _userWantsEnabled;
    private bool _steamBackoffActive;
    private bool _gyroAllowed = true;
    private bool _lastGyroTogglePressed;
    private DateTime? _emergencyShortcutStartedAt;
    private bool _emergencyShortcutWarned;
    private bool _emergencyShortcutTriggered;
    private CancellationTokenSource? _midiChimeCts;
    private DateTime _nextProfileHookScanAt = DateTime.MinValue;
    private string _activeProfileHookKey = string.Empty;

    public event EventHandler<BridgeStatus>? StatusChanged;
    public event EventHandler<string>? LogWritten;
    public event EventHandler<string>? ProfileHookApplied;

    public BridgeStatus Status { get; private set; } = BridgeStatus.Idle("Off");
    public BridgeOptions Options { get; private set; } = BridgeOptionsStore.Load();
    public InputSnapshot LatestInputSnapshot { get; private set; } = InputSnapshot.Empty;

    public BridgeService()
    {
        _dsuMotionServer = new DsuMotionServer(Log);
        _keyboard.LogWritten += (_, message) => Log(message);
        _fpsInput.LogWritten += (_, message) => Log(message);
        LoadStartupProfileIfConfigured();
        SyncDsuMotionServer();
    }

    public void SaveOptions()
    {
        BridgeOptionsStore.Save(Options);
        StartupManager.SetEnabled(Options.StartWithWindows);
        if (!Options.RumbleEnabled)
        {
            StopRumble();
        }

        SyncDsuMotionServer();
    }

    public void TestRumble()
    {
        Task.Run(() =>
        {
            SteamControllerDevice? controller;
            lock (_gate)
            {
                controller = Status.IsEnabled ? _controller : null;
            }

            if (controller is null)
            {
                Log("Test rumble skipped: bridge is not connected.");
                return;
            }

            var small = ScaleRumble(120);
            var large = ScaleRumble(180);
            if (!Options.RumbleEnabled || small == 0 && large == 0)
            {
                Log("Test rumble skipped: rumble is disabled or intensity is 0%.");
                return;
            }

            controller.SendRumble(small, large);
            Thread.Sleep(350);
            controller.SendRumble(0, 0);
            Log($"Test rumble played at {Options.RumbleIntensityPercent}% intensity.");
        });
    }

    public void TestPowerChime()
    {
        Task.Run(() =>
        {
            SteamControllerDevice? controller;
            lock (_gate)
            {
                controller = Status.IsEnabled ? _controller : null;
            }

            if (controller is null)
            {
                Log("Test chime skipped: bridge is not connected.");
                return;
            }

            PlayToneChime(controller, activate: true);
            Thread.Sleep(80);
            PlayToneChime(controller, activate: false);
            Log("Test haptic chime played.");
        });
    }

    public void PlayMidiHapticFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            Log("MIDI haptic skipped: choose a MIDI file first.");
            return;
        }

        if (!File.Exists(path))
        {
            Log("MIDI haptic skipped: file was not found.");
            return;
        }

        CancellationTokenSource cts;
        lock (_gate)
        {
            _midiChimeCts?.Cancel();
            _midiChimeCts?.Dispose();
            _midiChimeCts = new CancellationTokenSource();
            cts = _midiChimeCts;
        }

        Task.Run(() => PlayMidiHapticFile(path, cts.Token));
    }

    public void StopMidiHaptic()
    {
        CancellationTokenSource? cts;
        SteamControllerDevice? controller;
        lock (_gate)
        {
            cts = _midiChimeCts;
            _midiChimeCts = null;
            controller = _controller;
        }

        cts?.Cancel();
        cts?.Dispose();
        StopAllHapticTones(controller);
        controller?.SendRumble(0, 0);
        Log("MIDI haptic stopped.");
    }

    public void LoadOptions(BridgeOptions options)
    {
        var appSettings = CaptureAppSettings(Options);
        options.Normalize();
        RestoreAppSettings(options, appSettings);
        Options = options;
        SaveOptions();
    }

    private void LoadStartupProfileIfConfigured()
    {
        Options.Normalize();
        if (string.IsNullOrWhiteSpace(Options.StartupProfileName) || !BridgeProfileStore.Exists(Options.StartupProfileName))
        {
            return;
        }

        var appSettings = CaptureAppSettings(Options);
        try
        {
            var profile = BridgeProfileStore.Load(Options.StartupProfileName);
            RestoreAppSettings(profile, appSettings);
            Options = profile;
            Log($"Loaded startup profile: {Options.StartupProfileName}");
        }
        catch (Exception ex)
        {
            Log($"Startup profile failed to load: {ex.Message}");
        }
    }

    private static AppSettings CaptureAppSettings(BridgeOptions options)
    {
        return new AppSettings(
            options.StartWithWindows,
            options.StartMinimizedToTray,
            options.AutoStartBridge,
            options.AutoDisableForSteam,
            options.DsuMotionServerEnabled,
            options.StartupProfileName,
            CloneProfileHooks(options.ProfileHooks));
    }

    private static void RestoreAppSettings(BridgeOptions options, AppSettings appSettings)
    {
        options.StartWithWindows = appSettings.StartWithWindows;
        options.StartMinimizedToTray = appSettings.StartMinimizedToTray;
        options.AutoStartBridge = appSettings.AutoStartBridge;
        options.AutoDisableForSteam = appSettings.AutoDisableForSteam;
        options.DsuMotionServerEnabled = appSettings.DsuMotionServerEnabled;
        options.StartupProfileName = appSettings.StartupProfileName;
        options.ProfileHooks = CloneProfileHooks(appSettings.ProfileHooks);
    }

    private static List<ProfileHook> CloneProfileHooks(IEnumerable<ProfileHook> hooks)
    {
        return hooks
            .Where(hook => hook is not null)
            .Select(hook => new ProfileHook
            {
                ExePath = hook.ExePath,
                ProfileName = hook.ProfileName,
                Enabled = hook.Enabled
            })
            .ToList();
    }

    private readonly record struct AppSettings(
        bool StartWithWindows,
        bool StartMinimizedToTray,
        bool AutoStartBridge,
        bool AutoDisableForSteam,
        bool DsuMotionServerEnabled,
        string StartupProfileName,
        IReadOnlyList<ProfileHook> ProfileHooks);

    public void TickLifecycle()
    {
        UpdateProfileHooking();

        lock (_gate)
        {
            if (Options.AutoDisableForSteam && Status.IsEnabled && SystemDiagnostics.IsSteamRunning())
            {
                Log("Steam started. Backing off so Steam Input can take over.");
                _steamBackoffActive = true;
                StopInternal(userRequested: false, restoreLizard: true);
                SetStatus(BridgeStatus.Failed("Paused while Steam is running"));
                return;
            }

            if (!_userWantsEnabled || Status.IsEnabled || Status.IsWorking || DateTime.UtcNow < _nextReconnectAt)
            {
                return;
            }

            if (Options.AutoDisableForSteam && SystemDiagnostics.IsSteamRunning())
            {
                if (!_steamBackoffActive)
                {
                    Log("Waiting to reconnect until Steam closes.");
                    _steamBackoffActive = true;
                    SetStatus(BridgeStatus.Failed("Paused while Steam is running"));
                }

                _nextReconnectAt = DateTime.UtcNow.AddSeconds(3);
                return;
            }

            _steamBackoffActive = false;
            _nextReconnectAt = DateTime.UtcNow.AddSeconds(3);
        }

        Start();
    }

    private void UpdateProfileHooking()
    {
        if (DateTime.UtcNow < _nextProfileHookScanAt)
        {
            return;
        }

        _nextProfileHookScanAt = DateTime.UtcNow.AddSeconds(2);

        ProfileHook[] hooks;
        lock (_gate)
        {
            Options.Normalize();
            hooks = Options.ProfileHooks
                .Where(hook => hook.Enabled && BridgeProfileStore.Exists(hook.ProfileName))
                .ToArray();
        }

        if (hooks.Length == 0)
        {
            _activeProfileHookKey = string.Empty;
            return;
        }

        var match = FindActiveProfileHook(hooks);
        if (match is null)
        {
            _activeProfileHookKey = string.Empty;
            return;
        }

        var key = $"{match.ExePath}|{match.ProfileName}";
        if (string.Equals(key, _activeProfileHookKey, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        try
        {
            LoadOptions(BridgeProfileStore.Load(match.ProfileName));
            _activeProfileHookKey = key;
            Log($"Profile hook applied: {match.ProfileName} for {Path.GetFileName(match.ExePath)}");
            ProfileHookApplied?.Invoke(this, match.ProfileName);
        }
        catch (Exception ex)
        {
            Log($"Profile hook failed for {Path.GetFileName(match.ExePath)}: {ex.Message}");
        }
    }

    private static ProfileHook? FindActiveProfileHook(IEnumerable<ProfileHook> hooks)
    {
        var hookList = hooks
            .Where(hook => !string.IsNullOrWhiteSpace(hook.ExePath))
            .ToArray();
        if (hookList.Length == 0)
        {
            return null;
        }

        var hooksByName = hookList
            .GroupBy(hook => Path.GetFileNameWithoutExtension(hook.ExePath), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.OrdinalIgnoreCase);

        foreach (var process in Process.GetProcesses())
        {
            using (process)
            {
                if (!hooksByName.TryGetValue(process.ProcessName, out var candidates))
                {
                    continue;
                }

                var processPath = TryGetProcessPath(process);
                if (!string.IsNullOrWhiteSpace(processPath))
                {
                    var exact = candidates.FirstOrDefault(hook => string.Equals(hook.ExePath, processPath, StringComparison.OrdinalIgnoreCase));
                    if (exact is not null)
                    {
                        return exact;
                    }
                }

                if (candidates.Length == 1)
                {
                    return candidates[0];
                }
            }
        }

        return null;
    }

    private static string? TryGetProcessPath(Process process)
    {
        try
        {
            return process.MainModule?.FileName;
        }
        catch
        {
            return null;
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

            _userWantsEnabled = true;
            _steamBackoffActive = false;
            SetStatus(BridgeStatus.Working("Connecting to Steam Controller..."));
            Log($"Starting bridge. Log file: {BridgeLog.LogPath}");
            Log($"App version: {typeof(BridgeService).Assembly.GetName().Version?.ToString(3) ?? "unknown"}");

            try
            {
                if (!SystemDiagnostics.IsVigemBusInstalled())
                {
                    SetStatus(BridgeStatus.Failed("ViGEmBus is not installed"));
                    Log("Startup blocked: ViGEmBus service was not found.");
                    return;
                }

                if (SystemDiagnostics.IsSteamRunning())
                {
                    SetStatus(BridgeStatus.Failed("Close Steam and try again"));
                    Log("Startup blocked: Steam is running and may claim the controller.");
                    return;
                }

                _controller = SteamControllerDevice.OpenFirst(Log);
                if (_controller is null)
                {
                    SetStatus(BridgeStatus.Failed("Steam Controller not found"));
                    Log("Startup failed: no live Steam Controller HID interface was found.");
                    return;
                }

                Log("Steam Controller connected.");
                _gyroAllowed = true;
                _lastGyroTogglePressed = false;
                ResetEmergencyShortcut();
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
                PlayPowerChime(activate: true);
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
            StopInternal(userRequested: true, restoreLizard: true);
        }
    }

    private void StopInternal(bool userRequested, bool restoreLizard)
    {
        if (userRequested)
        {
            _userWantsEnabled = false;
            _steamBackoffActive = false;
        }

        if (!Status.IsEnabled && _controller is null && _virtualController is null)
        {
            SetStatus(userRequested ? BridgeStatus.Idle("Off") : Status);
            return;
        }

        SetStatus(BridgeStatus.Working("Turning off..."));
        PlayPowerChime(activate: false);
        StopReadLoop();
        _mouse.Reset();
        _gyroMouse.Reset();
        _keyboard.Reset();
        _fpsInput.Reset();
        ResetEmergencyShortcut();
        _dsuMotionServer.SetControllerConnected(false);
        _controller?.SendRumble(0, 0);
        _virtualController?.Dispose();
        _virtualController = null;
        CleanupController(restoreLizard);
        SetStatus(userRequested ? BridgeStatus.Idle("Off") : BridgeStatus.Failed("Paused"));
        Log(userRequested ? "Bridge stopped." : "Bridge paused.");
    }

    private void PlayPowerChime(bool activate)
    {
        if (!Options.PowerHapticChimeEnabled || _controller is null)
        {
            return;
        }

        PlayToneChime(_controller, activate);
    }

    private static void PlayToneChime(SteamControllerDevice controller, bool activate)
    {
        var notes = activate
            ? new ushort[] { 440, 660 }
            : new ushort[] { 660, 330 };
        foreach (var note in notes)
        {
            controller.PlayHapticTone(0, note, 120);
            controller.PlayHapticTone(1, note, 120);
            Thread.Sleep(55);
        }

        controller.StopHapticTone(0);
        controller.StopHapticTone(1);
    }

    private void PlayMidiHapticFile(string path, CancellationToken token)
    {
        SteamControllerDevice? controller;
        lock (_gate)
        {
            controller = Status.IsEnabled ? _controller : null;
        }

        if (controller is null)
        {
            Log("MIDI haptic skipped: bridge is not connected.");
            return;
        }

        try
        {
            var sequence = MidiHapticSequence.Load(path);
            if (sequence.Events.Count == 0)
            {
                Log("MIDI haptic skipped: no notes found.");
                return;
            }

            var mode = Options.MidiHapticPlaybackMode;
            Log($"Playing MIDI haptic: {Path.GetFileName(path)} ({mode})");
            if (mode == MidiHapticPlaybackMode.Smart)
            {
                PlaySmartMidiHapticSequence(controller, sequence.Events, token);
                return;
            }

            DateTime? bassStopAt = null;
            foreach (var ev in sequence.Events)
            {
                if (token.IsCancellationRequested)
                {
                    break;
                }

                if (ev.DelayMs > 0)
                {
                    bassStopAt = SleepMidiDelay(controller, Math.Min(ev.DelayMs, 4000), bassStopAt, token);
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }
                }

                bassStopAt = PlayMidiHapticEvent(controller, ev, mode);
            }
        }
        catch (Exception ex)
        {
            Log($"MIDI haptic failed: {ex.Message}");
        }
        finally
        {
            StopAllHapticTones(controller);
            controller.SendRumble(0, 0);
        }
    }

    private void PlaySmartMidiHapticSequence(
        SteamControllerDevice controller,
        IReadOnlyList<MidiHapticEvent> events,
        CancellationToken token)
    {
        DateTime? leftStopAt = null;
        DateTime? rightStopAt = null;
        DateTime? bassStopAt = null;
        var alternateSingleNotes = false;

        for (var i = 0; i < events.Count && !token.IsCancellationRequested; i++)
        {
            var ev = events[i];
            if (ev.DelayMs > 0)
            {
                SleepSmartMidiDelay(controller, Math.Min(ev.DelayMs, 4000), ref leftStopAt, ref rightStopAt, ref bassStopAt, token);
                if (token.IsCancellationRequested)
                {
                    break;
                }
            }

            var group = new List<MidiHapticEvent> { ev };
            while (i + 1 < events.Count && events[i + 1].DelayMs == 0)
            {
                group.Add(events[++i]);
            }

            var (left, right) = SelectSmartMidiVoices(group, ref alternateSingleNotes);
            if (left is { } leftNote)
            {
                leftStopAt = PlaySmartMidiTone(controller, channel: 1, leftNote);
                bassStopAt = PlayBassPulse(controller, leftNote, force: false) ?? bassStopAt;
            }

            if (right is { } rightNote)
            {
                rightStopAt = PlaySmartMidiTone(controller, channel: 0, rightNote);
                bassStopAt = PlayBassPulse(controller, rightNote, force: false) ?? bassStopAt;
            }
        }

        StopAllHapticTones(controller);
        controller.SendRumble(0, 0);
    }

    private static (MidiHapticEvent? Left, MidiHapticEvent? Right) SelectSmartMidiVoices(
        IReadOnlyList<MidiHapticEvent> group,
        ref bool alternateSingleNotes)
    {
        if (group.Count == 1)
        {
            alternateSingleNotes = !alternateSingleNotes;
            return alternateSingleNotes ? (group[0], null) : (null, group[0]);
        }

        var left = BestMidiVoice(group.Where(e => e.Channel == 1), preferLow: true);
        var right = BestMidiVoice(group.Where(e => e.Channel == 0), preferLow: false);
        if (left is not null || right is not null)
        {
            var remaining = group.Where(e => e.Channel != 0 && e.Channel != 1).ToList();
            left ??= BestMidiVoice(remaining, preferLow: true);
            right ??= BestMidiVoice(remaining.Where(e => !ReferenceEquals(e, left)), preferLow: false);
            return (left, right);
        }

        left = BestMidiVoice(group, preferLow: true);
        right = BestMidiVoice(group.Where(e => !ReferenceEquals(e, left)), preferLow: false);
        return (left, right);
    }

    private static MidiHapticEvent? BestMidiVoice(IEnumerable<MidiHapticEvent> notes, bool preferLow)
    {
        return preferLow
            ? notes.OrderBy(e => e.Note).ThenByDescending(e => e.Velocity).FirstOrDefault()
            : notes.OrderByDescending(e => e.Note).ThenByDescending(e => e.Velocity).FirstOrDefault();
    }

    private static DateTime PlaySmartMidiTone(SteamControllerDevice controller, byte channel, MidiHapticEvent ev)
    {
        controller.StopHapticTone(channel);
        Thread.Sleep(2);
        controller.PlayHapticTone(channel, ev.Frequency, CalculateSmartPadVelocity(ev));
        return DateTime.UtcNow.AddMilliseconds(Math.Clamp((int)(ev.DurationMs * 1.15), 45, 720));
    }

    private DateTime? PlayMidiHapticEvent(SteamControllerDevice controller, MidiHapticEvent ev, MidiHapticPlaybackMode mode)
    {
        if (mode == MidiHapticPlaybackMode.RumbleOnly)
        {
            return PlayBassPulse(controller, ev, force: true);
        }

        var velocity = CalculateMidiVelocity(ev);
        if (mode == MidiHapticPlaybackMode.Simple)
        {
            controller.StopHapticTone(0);
            controller.StopHapticTone(1);
            Thread.Sleep(2);
            controller.PlayHapticTone(0, ev.Frequency, velocity);
            controller.PlayHapticTone(1, ev.Frequency, velocity);
            return null;
        }

        var channel = mode == MidiHapticPlaybackMode.PadsOnly
            ? (byte)(ev.Channel % 2)
            : (byte)(ev.Channel % 4);

        controller.StopHapticTone(channel);
        Thread.Sleep(2);
        controller.PlayHapticTone(channel, ev.Frequency, velocity);
        return mode == MidiHapticPlaybackMode.Full ? PlayBassPulse(controller, ev, force: false) : null;
    }

    private DateTime? PlayBassPulse(SteamControllerDevice controller, MidiHapticEvent ev, bool force)
    {
        if (!force && ev.Frequency > 155)
        {
            return null;
        }

        var intensity = (byte)Math.Clamp(80 + ev.Velocity + Math.Max(0, 155 - ev.Frequency) / 2, 60, 255);
        intensity = ScaleRumble(intensity);
        if (intensity == 0)
        {
            return null;
        }

        controller.SendRumble(intensity, intensity);
        return DateTime.UtcNow.AddMilliseconds(force ? 70 : 45);
    }

    private static byte CalculateMidiVelocity(MidiHapticEvent ev)
    {
        var bassBoost = ev.Frequency < 180 ? 28 : ev.Frequency < 260 ? 12 : 0;
        return (byte)Math.Clamp(185 + (ev.Velocity * 55 / 127) + bassBoost, 170, 255);
    }

    private static byte CalculateSmartPadVelocity(MidiHapticEvent ev)
    {
        var boosted = CalculateMidiVelocity(ev) * 1.25;
        var melodyBoost = ev.Frequency >= 220 ? 10 : 0;
        return (byte)Math.Clamp((int)Math.Round(boosted) + melodyBoost, 210, 255);
    }

    private static void StopAllHapticTones(SteamControllerDevice? controller)
    {
        if (controller is null)
        {
            return;
        }

        for (byte channel = 0; channel < 4; channel++)
        {
            controller.StopHapticTone(channel);
        }
    }

    private static DateTime? SleepMidiDelay(SteamControllerDevice controller, int delayMs, DateTime? bassStopAt, CancellationToken token)
    {
        var remaining = Math.Max(0, delayMs);
        while (remaining > 0 && !token.IsCancellationRequested)
        {
            var chunk = Math.Min(remaining, 25);
            if (bassStopAt is { } stopAt)
            {
                var untilStop = (int)Math.Ceiling((stopAt - DateTime.UtcNow).TotalMilliseconds);
                if (untilStop <= 0)
                {
                    controller.SendRumble(0, 0);
                    bassStopAt = null;
                }
                else
                {
                    chunk = Math.Min(chunk, untilStop);
                }
            }

            Thread.Sleep(Math.Max(1, chunk));
            remaining -= chunk;
        }

        if (bassStopAt is { } finalStop && DateTime.UtcNow >= finalStop)
        {
            controller.SendRumble(0, 0);
            return null;
        }

        return bassStopAt;
    }

    private static void SleepSmartMidiDelay(
        SteamControllerDevice controller,
        int delayMs,
        ref DateTime? leftStopAt,
        ref DateTime? rightStopAt,
        ref DateTime? bassStopAt,
        CancellationToken token)
    {
        var remaining = Math.Max(0, delayMs);
        while (remaining > 0 && !token.IsCancellationRequested)
        {
            var chunk = Math.Min(remaining, 20);
            chunk = LimitDelayToStopAt(chunk, leftStopAt);
            chunk = LimitDelayToStopAt(chunk, rightStopAt);
            chunk = LimitDelayToStopAt(chunk, bassStopAt);
            Thread.Sleep(Math.Max(1, chunk));
            remaining -= chunk;

            if (leftStopAt is { } left && DateTime.UtcNow >= left)
            {
                controller.StopHapticTone(1);
                leftStopAt = null;
            }

            if (rightStopAt is { } right && DateTime.UtcNow >= right)
            {
                controller.StopHapticTone(0);
                rightStopAt = null;
            }

            if (bassStopAt is { } bass && DateTime.UtcNow >= bass)
            {
                controller.SendRumble(0, 0);
                bassStopAt = null;
            }
        }
    }

    private static int LimitDelayToStopAt(int chunk, DateTime? stopAt)
    {
        if (stopAt is not { } target)
        {
            return chunk;
        }

        var untilStop = (int)Math.Ceiling((target - DateTime.UtcNow).TotalMilliseconds);
        return untilStop <= 0 ? 1 : Math.Min(chunk, untilStop);
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
                LatestInputSnapshot = InputSnapshot.FromInput(input);
                _dsuMotionServer.Update(input);
                UpdateEmergencyShortcut(input);
                if (token.IsCancellationRequested)
                {
                    break;
                }

                UpdateGyroToggle(input);
                _virtualController?.Update(input, Options, _gyroAllowed);
                _keyboard.Update(input, Options);
                _fpsInput.Update(input, Options);
                _mouse.Update(input, Options);
                if (Options.GyroOutputMode == GyroOutputMode.Mouse)
                {
                    _gyroMouse.Update(input, Options, _gyroAllowed);
                }
                else
                {
                    _gyroMouse.Reset();
                }
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

    private void UpdateGyroToggle(SteamControllerInput input)
    {
        if (Options.GyroToggleButton == GyroToggleButton.Disabled)
        {
            _gyroAllowed = true;
            _lastGyroTogglePressed = false;
            return;
        }

        var pressed = IsPressed(input, Options.GyroToggleButton);
        if (pressed && !_lastGyroTogglePressed)
        {
            _gyroAllowed = !_gyroAllowed;
            Log(_gyroAllowed ? "Gyro aim toggle enabled." : "Gyro aim toggle disabled.");
        }

        _lastGyroTogglePressed = pressed;
    }

    private void UpdateEmergencyShortcut(SteamControllerInput input)
    {
        if (!input.Back || !input.Start)
        {
            ResetEmergencyShortcut();
            return;
        }

        var now = DateTime.UtcNow;
        _emergencyShortcutStartedAt ??= now;
        var held = now - _emergencyShortcutStartedAt.Value;

        if (!_emergencyShortcutWarned && held >= TimeSpan.FromSeconds(3))
        {
            _emergencyShortcutWarned = true;
            Log("Controller shortcut warning: release View + Menu to keep Bridge on.");
            PlayEmergencyWarningChime();
        }

        if (_emergencyShortcutTriggered || held < TimeSpan.FromSeconds(5))
        {
            return;
        }

        _emergencyShortcutTriggered = true;
        Log("Controller shortcut held for 5 seconds. Turning Bridge off and returning controller to Steam.");
        Task.Run(() =>
        {
            lock (_gate)
            {
                StopInternal(userRequested: true, restoreLizard: true);
                SetStatus(BridgeStatus.Idle("Disabled from controller shortcut"));
            }
        });
    }

    private void PlayEmergencyWarningChime()
    {
        SteamControllerDevice? controller;
        lock (_gate)
        {
            controller = _controller;
        }

        if (controller is null)
        {
            return;
        }

        Task.Run(() =>
        {
            try
            {
                controller.PlayHapticTone(0, 220, 230);
                controller.PlayHapticTone(1, 220, 230);
                Thread.Sleep(90);
                controller.StopHapticTone(0);
                controller.StopHapticTone(1);
                Thread.Sleep(55);
                controller.PlayHapticTone(0, 165, 230);
                controller.PlayHapticTone(1, 165, 230);
                Thread.Sleep(100);
                controller.StopHapticTone(0);
                controller.StopHapticTone(1);
            }
            catch
            {
                // Best-effort warning only.
            }
        });
    }

    private void ResetEmergencyShortcut()
    {
        _emergencyShortcutStartedAt = null;
        _emergencyShortcutWarned = false;
        _emergencyShortcutTriggered = false;
    }

    private static bool IsPressed(SteamControllerInput input, GyroToggleButton button)
    {
        return button switch
        {
            GyroToggleButton.A => input.A,
            GyroToggleButton.B => input.B,
            GyroToggleButton.X => input.X,
            GyroToggleButton.Y => input.Y,
            GyroToggleButton.LeftShoulder => input.LeftShoulder,
            GyroToggleButton.RightShoulder => input.RightShoulder,
            GyroToggleButton.LeftThumb => input.LeftThumb,
            GyroToggleButton.RightThumb => input.RightThumb,
            GyroToggleButton.Back => input.Back,
            GyroToggleButton.Start => input.Start,
            GyroToggleButton.Guide => input.Guide,
            GyroToggleButton.DPadUp => input.DPadUp,
            GyroToggleButton.DPadDown => input.DPadDown,
            GyroToggleButton.DPadLeft => input.DPadLeft,
            GyroToggleButton.DPadRight => input.DPadRight,
            GyroToggleButton.L4 => input.L4,
            GyroToggleButton.L5 => input.L5,
            GyroToggleButton.R4 => input.R4,
            GyroToggleButton.R5 => input.R5,
            GyroToggleButton.LeftTrigger => input.LeftTriggerActive,
            GyroToggleButton.RightTrigger => input.RightTriggerActive,
            GyroToggleButton.LeftPadTouch => input.LeftPadTouched,
            GyroToggleButton.RightPadTouch => input.RightPadTouched,
            _ => false
        };
    }

    private void BeginStopAfterFailure(string message)
    {
        Task.Run(() =>
        {
            lock (_gate)
            {
                StopReadLoop();
                _mouse.Reset();
                _gyroMouse.Reset();
                _keyboard.Reset();
                _fpsInput.Reset();
                _dsuMotionServer.SetControllerConnected(false);
                _virtualController?.Dispose();
                _virtualController = null;
                CleanupController(restoreLizard: false);
                SetStatus(BridgeStatus.Failed(message));
                _nextReconnectAt = DateTime.UtcNow.AddSeconds(2);
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

        if (!Options.RumbleEnabled || Options.RumbleIntensityPercent <= 0 || e.SmallMotor == 0 && e.LargeMotor == 0)
        {
            StopRumble();
        }
    }

    private void MaybeSendRumble()
    {
        if (!Options.RumbleEnabled || Options.RumbleIntensityPercent <= 0)
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

        var scaledSmall = ScaleRumble(small);
        var scaledLarge = ScaleRumble(large);
        if (scaledSmall == 0 && scaledLarge == 0)
        {
            StopRumble();
            return;
        }

        _controller?.SendRumble(scaledSmall, scaledLarge);
        _lastSentRumbleSmall = scaledSmall;
        _lastSentRumbleLarge = scaledLarge;
        _nextRumbleAt = DateTime.UtcNow.AddMilliseconds(40);
    }

    private byte ScaleRumble(byte value)
    {
        if (!Options.RumbleEnabled || Options.RumbleIntensityPercent <= 0 || value == 0)
        {
            return 0;
        }

        var input = value / 255.0;
        var response = Math.Pow(input, 1.35);
        var intensity = Options.RumbleIntensityPercent / 100.0;
        var scaled = (int)Math.Round(response * MaxSteamControllerRumbleByte * intensity);
        return (byte)Math.Clamp(scaled, 0, byte.MaxValue);
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

    private void SyncDsuMotionServer()
    {
        _dsuMotionServer.SetEnabled(Options.DsuMotionServerEnabled);
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
        _keyboard.Reset();
        _fpsInput.Reset();
        StopMidiHaptic();
        _dsuMotionServer.Dispose();
    }
}
