using System.Diagnostics;
using SteamControllerBridge.Bridge;

namespace SteamControllerBridge;

internal sealed class MainForm : Form
{
    private readonly BridgeService _bridge = new();
    private readonly NotifyIcon _trayIcon;
    private readonly Label _statusLabel = new();
    private readonly Label _detailLabel = new();
    private readonly CheckBox _enableSwitch = new();
    private readonly FlowLayoutPanel _quickOptionsPanel = new();
    private readonly TextBox _logBox = new();
    private readonly Button _advancedButton = new();
    private readonly Panel _advancedPanel = new();
    private readonly Dictionary<PhysicalButton, ComboBox> _buttonMapCombos = new();
    private readonly Dictionary<PhysicalButton, CheckBox> _turboChecks = new();
    private readonly ComboBox _presetCombo = new();
    private readonly Button _applyPresetButton = new();
    private readonly ComboBox _trackpadSourceCombo = new();
    private readonly ComboBox _gyroActivationCombo = new();
    private readonly ComboBox _gyroOutputCombo = new();
    private readonly CheckBox _trackpadMouseCheck = new();
    private readonly CheckBox _trackpadClickCheck = new();
    private readonly CheckBox _gyroMouseCheck = new();
    private readonly CheckBox _rumbleCheck = new();
    private readonly CheckBox _darkModeCheck = new();
    private readonly CheckBox _startWithWindowsCheck = new();
    private readonly CheckBox _autoDisableForSteamCheck = new();
    private readonly Button _openLogButton = new();
    private readonly Button _copyDiagnosticsButton = new();
    private readonly System.Windows.Forms.Timer _lifecycleTimer = new();
    private ToolStripMenuItem? _rumbleTrayItem;
    private ToolStripMenuItem? _darkModeTrayItem;
    private readonly Icon _appIcon;
    private bool _updatingSwitch;
    private bool _updatingOptions;
    private bool _advancedVisible;

    public MainForm()
    {
        Text = "Steam Controller Bridge";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(420, 260);
        Size = new Size(520, 520);
        _appIcon = LoadAppIcon();
        Icon = _appIcon;

        _trayIcon = new NotifyIcon
        {
            Icon = _appIcon,
            Text = "Steam Controller Bridge",
            Visible = true,
            ContextMenuStrip = BuildTrayMenu()
        };
        _trayIcon.DoubleClick += (_, _) => ShowMainWindow();
        _lifecycleTimer.Interval = 2500;
        _lifecycleTimer.Tick += (_, _) => Task.Run(() => _bridge.TickLifecycle());
        _lifecycleTimer.Start();

        BuildUi();
        LoadOptionsIntoUi();

        _bridge.StatusChanged += (_, status) => OnUi(() => ApplyStatus(status));
        _bridge.LogWritten += (_, line) => OnUi(() => AppendLog(line));
        FormClosing += (_, _) =>
        {
            _bridge.Dispose();
            _lifecycleTimer.Stop();
            _trayIcon.Dispose();
            _appIcon.Dispose();
        };

        ApplyStatus(_bridge.Status);
        ApplyTheme();
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(24),
            RowCount = 7,
            ColumnCount = 1
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var title = new Label
        {
            Text = "Steam Controller Bridge",
            Font = new Font(Font.FontFamily, 16, FontStyle.Bold),
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 16)
        };

        _statusLabel.Font = new Font(Font.FontFamily, 11, FontStyle.Bold);
        _statusLabel.AutoSize = true;
        _statusLabel.Margin = new Padding(0, 0, 0, 4);

        _detailLabel.AutoSize = true;
        _detailLabel.Margin = new Padding(0, 0, 0, 20);

        _enableSwitch.Text = "Off";
        _enableSwitch.Appearance = Appearance.Button;
        _enableSwitch.TextAlign = ContentAlignment.MiddleCenter;
        _enableSwitch.Font = new Font(Font.FontFamily, 12, FontStyle.Bold);
        _enableSwitch.Height = 48;
        _enableSwitch.Dock = DockStyle.Top;
        _enableSwitch.CheckedChanged += (_, _) =>
        {
            if (_updatingSwitch)
            {
                return;
            }

            if (_enableSwitch.Checked)
            {
                StartBridgeAsync();
            }
            else
            {
                StopBridgeAsync();
            }
        };

        BuildQuickOptions();

        _advancedButton.Text = "Advanced";
        _advancedButton.AutoSize = true;
        _advancedButton.Margin = new Padding(0, 10, 0, 8);
        _advancedButton.Click += (_, _) => ToggleAdvanced();

        BuildAdvancedPanel();

        root.Controls.Add(title, 0, 0);
        root.Controls.Add(_statusLabel, 0, 1);
        root.Controls.Add(_detailLabel, 0, 2);
        root.Controls.Add(_enableSwitch, 0, 3);
        root.Controls.Add(_quickOptionsPanel, 0, 4);
        root.Controls.Add(_advancedButton, 0, 5);
        root.Controls.Add(_advancedPanel, 0, 6);

        Controls.Add(root);
    }

    private void BuildQuickOptions()
    {
        _quickOptionsPanel.AutoSize = true;
        _quickOptionsPanel.Dock = DockStyle.Top;
        _quickOptionsPanel.Margin = new Padding(0, 12, 0, 0);
        _quickOptionsPanel.WrapContents = true;

        _rumbleCheck.Text = "Enable rumble";
        _rumbleCheck.AutoSize = true;
        _rumbleCheck.Margin = new Padding(0, 0, 18, 0);
        _rumbleCheck.CheckedChanged += (_, _) => SaveOptionsFromUi();

        _gyroMouseCheck.Text = "Gyro aim";
        _gyroMouseCheck.AutoSize = true;
        _gyroMouseCheck.Margin = new Padding(0, 0, 18, 0);
        _gyroMouseCheck.CheckedChanged += (_, _) => SaveOptionsFromUi();

        ConfigureCombo(_gyroActivationCombo, Enum.GetValues<GyroMouseActivation>());
        _gyroActivationCombo.Width = 115;
        _gyroActivationCombo.Margin = new Padding(0, 0, 18, 0);
        _gyroActivationCombo.SelectedIndexChanged += (_, _) => SaveOptionsFromUi();

        _darkModeCheck.Text = "Dark mode";
        _darkModeCheck.AutoSize = true;
        _darkModeCheck.Margin = new Padding(0, 0, 0, 0);
        _darkModeCheck.CheckedChanged += (_, _) =>
        {
            SaveOptionsFromUi();
            ApplyTheme();
        };

        _quickOptionsPanel.Controls.Add(_rumbleCheck);
        _quickOptionsPanel.Controls.Add(_gyroMouseCheck);
        _quickOptionsPanel.Controls.Add(_gyroActivationCombo);
        _quickOptionsPanel.Controls.Add(_darkModeCheck);
    }

    private void BuildAdvancedPanel()
    {
        _advancedPanel.Dock = DockStyle.Fill;
        _advancedPanel.Visible = false;
        _advancedPanel.AutoScroll = true;

        var advancedLayout = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Top,
            RowCount = 2,
            ColumnCount = 1
        };
        advancedLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        advancedLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 118));

        var layout = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Top,
            RowCount = 33,
            ColumnCount = 3
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));
        for (var row = 0; row < layout.RowCount; row++)
        {
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }

        ConfigureCombo(_trackpadSourceCombo, Enum.GetValues<TrackpadMouseSource>());
        ConfigureCombo(_presetCombo, Enum.GetValues<RemapPreset>());
        ConfigureCombo(_gyroOutputCombo, Enum.GetValues<GyroOutputMode>());

        var optionRow = 0;
        AddSectionLabel(layout, optionRow++, "Presets");
        AddOptionRow(layout, optionRow, "Preset", _presetCombo);
        _applyPresetButton.Text = "Apply";
        _applyPresetButton.AutoSize = true;
        _applyPresetButton.Click += (_, _) => ApplySelectedPreset();
        layout.Controls.Add(_applyPresetButton, 2, optionRow++);
        AddSectionLabel(layout, optionRow++, "Button remapping");
        AddBindingRow(layout, optionRow++, "A", PhysicalButton.A);
        AddBindingRow(layout, optionRow++, "B", PhysicalButton.B);
        AddBindingRow(layout, optionRow++, "X", PhysicalButton.X);
        AddBindingRow(layout, optionRow++, "Y", PhysicalButton.Y);
        AddBindingRow(layout, optionRow++, "LB", PhysicalButton.LeftShoulder);
        AddBindingRow(layout, optionRow++, "RB", PhysicalButton.RightShoulder);
        AddBindingRow(layout, optionRow++, "L3", PhysicalButton.LeftThumb);
        AddBindingRow(layout, optionRow++, "R3", PhysicalButton.RightThumb);
        AddBindingRow(layout, optionRow++, "View", PhysicalButton.Back);
        AddBindingRow(layout, optionRow++, "Menu", PhysicalButton.Start);
        AddBindingRow(layout, optionRow++, "Steam", PhysicalButton.Guide);
        AddBindingRow(layout, optionRow++, "D-pad up", PhysicalButton.DPadUp);
        AddBindingRow(layout, optionRow++, "D-pad down", PhysicalButton.DPadDown);
        AddBindingRow(layout, optionRow++, "D-pad left", PhysicalButton.DPadLeft);
        AddBindingRow(layout, optionRow++, "D-pad right", PhysicalButton.DPadRight);
        AddBindingRow(layout, optionRow++, "L4", PhysicalButton.L4);
        AddBindingRow(layout, optionRow++, "L5", PhysicalButton.L5);
        AddBindingRow(layout, optionRow++, "R4", PhysicalButton.R4);
        AddBindingRow(layout, optionRow++, "R5", PhysicalButton.R5);
        AddSectionLabel(layout, optionRow++, "Gyro, mouse, and app");
        AddOptionRow(layout, optionRow++, "Gyro output", _gyroOutputCombo);
        AddOptionRow(layout, optionRow++, "Mouse pad", _trackpadSourceCombo);

        _trackpadMouseCheck.Text = "Use trackpad as mouse";
        _trackpadMouseCheck.AutoSize = true;
        _trackpadMouseCheck.CheckedChanged += (_, _) => SaveOptionsFromUi();
        layout.Controls.Add(_trackpadMouseCheck, 1, optionRow++);

        _trackpadClickCheck.Text = "Trackpad click is left click";
        _trackpadClickCheck.AutoSize = true;
        _trackpadClickCheck.CheckedChanged += (_, _) => SaveOptionsFromUi();
        layout.Controls.Add(_trackpadClickCheck, 1, optionRow++);

        _startWithWindowsCheck.Text = "Start with Windows";
        _startWithWindowsCheck.AutoSize = true;
        _startWithWindowsCheck.CheckedChanged += (_, _) => SaveOptionsFromUi();
        layout.Controls.Add(_startWithWindowsCheck, 1, optionRow++);

        _autoDisableForSteamCheck.Text = "Back off when Steam opens";
        _autoDisableForSteamCheck.AutoSize = true;
        _autoDisableForSteamCheck.CheckedChanged += (_, _) => SaveOptionsFromUi();
        layout.Controls.Add(_autoDisableForSteamCheck, 1, optionRow++);

        _openLogButton.Text = "Open log";
        _openLogButton.AutoSize = true;
        _openLogButton.Click += (_, _) => OpenLog();
        layout.Controls.Add(_openLogButton, 1, optionRow++);

        _copyDiagnosticsButton.Text = "Copy diagnostics";
        _copyDiagnosticsButton.AutoSize = true;
        _copyDiagnosticsButton.Click += (_, _) => CopyDiagnostics();
        layout.Controls.Add(_copyDiagnosticsButton, 1, optionRow);

        _logBox.Multiline = true;
        _logBox.ReadOnly = true;
        _logBox.ScrollBars = ScrollBars.Vertical;
        _logBox.Dock = DockStyle.Fill;
        _logBox.Margin = new Padding(0, 10, 0, 0);

        advancedLayout.Controls.Add(layout, 0, 0);
        advancedLayout.Controls.Add(_logBox, 0, 1);
        _advancedPanel.Controls.Add(advancedLayout);
    }

    private static void ConfigureCombo<T>(ComboBox combo, IEnumerable<T> values)
    {
        combo.DropDownStyle = ComboBoxStyle.DropDownList;
        combo.Dock = DockStyle.Fill;
        foreach (var value in values)
        {
            combo.Items.Add(value!);
        }
    }

    private void AddOptionRow(TableLayoutPanel layout, int row, string label, ComboBox combo)
    {
        var text = new Label
        {
            Text = label,
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 6, 8, 0)
        };
        combo.SelectedIndexChanged += (_, _) => SaveOptionsFromUi();
        layout.Controls.Add(text, 0, row);
        layout.Controls.Add(combo, 1, row);
    }

    private void AddSectionLabel(TableLayoutPanel layout, int row, string text)
    {
        var label = new Label
        {
            Text = text,
            AutoSize = true,
            Font = new Font(Font.FontFamily, 9, FontStyle.Bold),
            Margin = new Padding(0, 12, 0, 4)
        };
        layout.Controls.Add(label, 0, row);
        layout.SetColumnSpan(label, 3);
    }

    private void AddBindingRow(TableLayoutPanel layout, int row, string label, PhysicalButton button)
    {
        var combo = new ComboBox();
        ConfigureCombo(combo, Enum.GetValues<GamepadButton>());
        combo.SelectedIndexChanged += (_, _) => SaveOptionsFromUi();
        _buttonMapCombos[button] = combo;

        var turbo = new CheckBox
        {
            Text = "Turbo",
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(8, 4, 0, 0)
        };
        turbo.CheckedChanged += (_, _) => SaveOptionsFromUi();
        _turboChecks[button] = turbo;

        var text = new Label
        {
            Text = label,
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 6, 8, 0)
        };

        layout.Controls.Add(text, 0, row);
        layout.Controls.Add(combo, 1, row);
        layout.Controls.Add(turbo, 2, row);
    }

    private ContextMenuStrip BuildTrayMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Show", null, (_, _) => ShowMainWindow());
        menu.Items.Add("Enable", null, (_, _) => StartBridgeAsync());
        menu.Items.Add("Disable", null, (_, _) => StopBridgeAsync());
        menu.Items.Add(new ToolStripSeparator());
        _rumbleTrayItem = new ToolStripMenuItem("Enable rumble") { CheckOnClick = true };
        _rumbleTrayItem.Click += (_, _) => _rumbleCheck.Checked = _rumbleTrayItem.Checked;
        _darkModeTrayItem = new ToolStripMenuItem("Dark mode") { CheckOnClick = true };
        _darkModeTrayItem.Click += (_, _) => _darkModeCheck.Checked = _darkModeTrayItem.Checked;
        menu.Items.Add(_rumbleTrayItem);
        menu.Items.Add(_darkModeTrayItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) =>
        {
            _trayIcon.Visible = false;
            Application.Exit();
        });
        return menu;
    }

    private void LoadOptionsIntoUi()
    {
        _updatingOptions = true;
        foreach (var (button, combo) in _buttonMapCombos)
        {
            var binding = _bridge.Options.GetBinding(button);
            combo.SelectedItem = binding.Output;
            _turboChecks[button].Checked = binding.Turbo;
        }

        _trackpadSourceCombo.SelectedItem = _bridge.Options.TrackpadMouseSource;
        _presetCombo.SelectedItem = RemapPreset.DefaultXbox;
        _gyroOutputCombo.SelectedItem = _bridge.Options.GyroOutputMode;
        _gyroActivationCombo.SelectedItem = _bridge.Options.GyroMouseActivation;
        _trackpadMouseCheck.Checked = _bridge.Options.TrackpadMouseEnabled;
        _trackpadClickCheck.Checked = _bridge.Options.TrackpadClickEnabled;
        _bridge.Options.StartWithWindows = StartupManager.IsEnabled();
        _startWithWindowsCheck.Checked = _bridge.Options.StartWithWindows;
        _autoDisableForSteamCheck.Checked = _bridge.Options.AutoDisableForSteam;
        _gyroMouseCheck.Checked = _bridge.Options.GyroMouseEnabled;
        _rumbleCheck.Checked = _bridge.Options.RumbleEnabled;
        _darkModeCheck.Checked = _bridge.Options.DarkModeEnabled;
        _updatingOptions = false;
        SyncTrayOptions();
    }

    private void SaveOptionsFromUi()
    {
        if (_updatingOptions)
        {
            return;
        }

        foreach (var (button, combo) in _buttonMapCombos)
        {
            var binding = _bridge.Options.GetBinding(button);
            binding.Output = (GamepadButton)(combo.SelectedItem ?? binding.Output);
            binding.Turbo = _turboChecks[button].Checked;
        }

        _bridge.Options.TrackpadMouseSource = (TrackpadMouseSource)(_trackpadSourceCombo.SelectedItem ?? _bridge.Options.TrackpadMouseSource);
        _bridge.Options.GyroOutputMode = (GyroOutputMode)(_gyroOutputCombo.SelectedItem ?? _bridge.Options.GyroOutputMode);
        _bridge.Options.GyroMouseActivation = (GyroMouseActivation)(_gyroActivationCombo.SelectedItem ?? _bridge.Options.GyroMouseActivation);
        _bridge.Options.TrackpadMouseEnabled = _trackpadMouseCheck.Checked;
        _bridge.Options.TrackpadClickEnabled = _trackpadClickCheck.Checked;
        _bridge.Options.StartWithWindows = _startWithWindowsCheck.Checked;
        _bridge.Options.AutoDisableForSteam = _autoDisableForSteamCheck.Checked;
        _bridge.Options.GyroMouseEnabled = _gyroMouseCheck.Checked;
        _bridge.Options.RumbleEnabled = _rumbleCheck.Checked;
        _bridge.Options.DarkModeEnabled = _darkModeCheck.Checked;
        _bridge.SaveOptions();
        SyncTrayOptions();
    }

    private void ApplySelectedPreset()
    {
        var preset = (RemapPreset)(_presetCombo.SelectedItem ?? RemapPreset.DefaultXbox);
        _bridge.Options.ApplyPreset(preset);
        _bridge.SaveOptions();
        LoadOptionsIntoUi();
        _presetCombo.SelectedItem = preset;
        AppendLog($"{DateTime.Now:HH:mm:ss}  Applied preset: {preset}");
    }

    private void SyncTrayOptions()
    {
        if (_rumbleTrayItem is not null)
        {
            _rumbleTrayItem.Checked = _rumbleCheck.Checked;
        }

        if (_darkModeTrayItem is not null)
        {
            _darkModeTrayItem.Checked = _darkModeCheck.Checked;
        }
    }

    private void ApplyStatus(BridgeStatus status)
    {
        _updatingSwitch = true;
        _enableSwitch.Checked = status.IsEnabled || status.IsWorking;
        _enableSwitch.Enabled = !status.IsWorking;
        _enableSwitch.Text = status.IsEnabled ? "On" : status.IsWorking ? "Working..." : "Off";
        _updatingSwitch = false;

        _statusLabel.Text = status.HasError ? "Needs attention" : status.IsEnabled ? "Ready" : "Idle";
        _detailLabel.Text = status.Message;
        _trayIcon.Text = ClampTrayText($"Steam Controller Bridge - {status.Message}");
    }

    private void ToggleAdvanced()
    {
        _advancedVisible = !_advancedVisible;
        _advancedPanel.Visible = _advancedVisible;
        _advancedButton.Text = _advancedVisible ? "Hide advanced" : "Advanced";
        Height = _advancedVisible ? 640 : 360;
    }

    private void ApplyTheme()
    {
        var dark = _darkModeCheck.Checked;
        var back = dark ? Color.FromArgb(22, 22, 24) : SystemColors.Control;
        var panel = dark ? Color.FromArgb(32, 32, 36) : SystemColors.Window;
        var fore = dark ? Color.FromArgb(238, 238, 238) : SystemColors.ControlText;
        var muted = dark ? Color.FromArgb(170, 170, 170) : SystemColors.GrayText;

        BackColor = back;
        ForeColor = fore;
        ApplyThemeToControls(Controls, back, panel, fore);
        _detailLabel.ForeColor = muted;
        _logBox.BackColor = panel;
        _logBox.ForeColor = fore;
        _logBox.BorderStyle = dark ? BorderStyle.FixedSingle : BorderStyle.Fixed3D;
    }

    private static void ApplyThemeToControls(Control.ControlCollection controls, Color back, Color panel, Color fore)
    {
        foreach (Control control in controls)
        {
            control.ForeColor = fore;
            control.BackColor = control is TextBox or ComboBox ? panel : back;
            if (control.HasChildren)
            {
                ApplyThemeToControls(control.Controls, back, panel, fore);
            }
        }
    }

    private void StartBridgeAsync()
    {
        _ = Task.Run(() => _bridge.Start());
    }

    private void StopBridgeAsync()
    {
        _ = Task.Run(() => _bridge.Stop());
    }

    private void OpenLog()
    {
        try
        {
            if (!File.Exists(BridgeLog.LogPath))
            {
                File.WriteAllText(BridgeLog.LogPath, string.Empty);
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = BridgeLog.LogPath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Could not open log", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void CopyDiagnostics()
    {
        try
        {
            Clipboard.SetText(SystemDiagnostics.BuildReport());
            AppendLog($"{DateTime.Now:HH:mm:ss}  Diagnostics copied to clipboard");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Could not copy diagnostics", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void AppendLog(string line)
    {
        _logBox.AppendText(line + Environment.NewLine);
    }

    private void ShowMainWindow()
    {
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (WindowState == FormWindowState.Minimized)
        {
            Hide();
        }
    }

    private void OnUi(Action action)
    {
        if (IsDisposed)
        {
            return;
        }

        if (InvokeRequired)
        {
            BeginInvoke(action);
        }
        else
        {
            action();
        }
    }

    private static string ClampTrayText(string text)
    {
        return text.Length <= 63 ? text : text[..60] + "...";
    }

    private static Icon LoadAppIcon()
    {
        var icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        return icon ?? SystemIcons.Application;
    }
}
