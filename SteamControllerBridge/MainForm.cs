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
    private readonly ComboBox _gyroToggleCombo = new();
    private readonly ComboBox _gyroOutputCombo = new();
    private readonly TrackBar _gyroStickSensitivitySlider = new();
    private readonly Label _gyroStickSensitivityValue = new();
    private readonly NumericUpDown _gyroStickDeadZoneInput = new();
    private readonly TrackBar _turboSpeedSlider = new();
    private readonly Label _turboSpeedValue = new();
    private readonly CheckBox _leftTriggerTurboCheck = new();
    private readonly CheckBox _rightTriggerTurboCheck = new();
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
        MinimumSize = new Size(680, 460);
        Size = new Size(780, 560);
        Font = new Font("Segoe UI", 9F);
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
            Padding = new Padding(22),
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
            Font = new Font(Font.FontFamily, 18, FontStyle.Bold),
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 14)
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
        _enableSwitch.Height = 52;
        _enableSwitch.Dock = DockStyle.Top;
        _enableSwitch.FlatStyle = FlatStyle.Flat;
        _enableSwitch.FlatAppearance.BorderSize = 0;
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
        _advancedButton.FlatStyle = FlatStyle.Flat;
        _advancedButton.FlatAppearance.BorderSize = 0;
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
        _gyroActivationCombo.Width = 130;
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
        _advancedPanel.AutoScroll = false;

        ConfigureCombo(_trackpadSourceCombo, Enum.GetValues<TrackpadMouseSource>());
        ConfigureCombo(_presetCombo, Enum.GetValues<RemapPreset>());
        ConfigureCombo(_gyroOutputCombo, Enum.GetValues<GyroOutputMode>());
        ConfigureCombo(_gyroToggleCombo, Enum.GetValues<GyroToggleButton>());

        var tabs = new TabControl
        {
            Dock = DockStyle.Fill,
            HotTrack = true,
            Padding = new Point(12, 5),
            Margin = new Padding(0)
        };

        tabs.TabPages.Add(BuildPresetTab());
        tabs.TabPages.Add(BuildButtonsTab());
        tabs.TabPages.Add(BuildMotionTab());
        tabs.TabPages.Add(BuildLogsTab());
        _advancedPanel.Controls.Add(tabs);
    }

    private TabPage BuildPresetTab()
    {
        var tab = CreateTab("Presets");
        var layout = CreateFormLayout(3);
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        AddOptionRow(layout, 0, "Preset", _presetCombo);
        _applyPresetButton.Text = "Apply";
        _applyPresetButton.Height = 32;
        _applyPresetButton.Dock = DockStyle.Fill;
        _applyPresetButton.Click += (_, _) => ApplySelectedPreset();
        layout.Controls.Add(_applyPresetButton, 2, 0);
        tab.Controls.Add(layout);
        return tab;
    }

    private TabPage BuildButtonsTab()
    {
        var tab = CreateTab("Buttons");
        var scroller = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        var layout = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Top,
            ColumnCount = 6,
            Padding = new Padding(12)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 74));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));

        var row = 0;
        AddTurboSpeedRow(layout, row++);
        AddTriggerTurboRow(layout, row++);
        AddBindingPairRow(layout, row++, "A", PhysicalButton.A, "View", PhysicalButton.Back);
        AddBindingPairRow(layout, row++, "B", PhysicalButton.B, "Menu", PhysicalButton.Start);
        AddBindingPairRow(layout, row++, "X", PhysicalButton.X, "Steam", PhysicalButton.Guide);
        AddBindingPairRow(layout, row++, "Y", PhysicalButton.Y, "D-pad up", PhysicalButton.DPadUp);
        AddBindingPairRow(layout, row++, "LB", PhysicalButton.LeftShoulder, "D-pad down", PhysicalButton.DPadDown);
        AddBindingPairRow(layout, row++, "RB", PhysicalButton.RightShoulder, "D-pad left", PhysicalButton.DPadLeft);
        AddBindingPairRow(layout, row++, "L3", PhysicalButton.LeftThumb, "D-pad right", PhysicalButton.DPadRight);
        AddBindingPairRow(layout, row++, "R3", PhysicalButton.RightThumb, "L4", PhysicalButton.L4);
        AddBindingPairRow(layout, row++, "L5", PhysicalButton.L5, "R4", PhysicalButton.R4);
        AddBindingPairRow(layout, row, "R5", PhysicalButton.R5, string.Empty, null);

        scroller.Controls.Add(layout);
        tab.Controls.Add(scroller);
        return tab;
    }

    private TabPage BuildMotionTab()
    {
        var tab = CreateTab("Motion");
        var layout = CreateFormLayout(2);
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var row = 0; row < 10; row++)
        {
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }

        AddOptionRow(layout, 0, "Gyro output", _gyroOutputCombo);
        AddOptionRow(layout, 1, "Gyro toggle", _gyroToggleCombo);
        AddGyroSensitivityRow(layout, 2);
        AddNumericRow(layout, 3, "Gyro deadzone", _gyroStickDeadZoneInput, 0, 300);
        AddOptionRow(layout, 4, "Mouse pad", _trackpadSourceCombo);
        AddCheckRow(layout, 5, _trackpadMouseCheck, "Use trackpad as mouse");
        AddCheckRow(layout, 6, _trackpadClickCheck, "Trackpad click is left click");
        AddCheckRow(layout, 7, _startWithWindowsCheck, "Start with Windows");
        AddCheckRow(layout, 8, _autoDisableForSteamCheck, "Back off when Steam opens");
        tab.Controls.Add(layout);
        return tab;
    }

    private TabPage BuildLogsTab()
    {
        var tab = CreateTab("Logs");
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, Padding = new Padding(10) };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var actions = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Top };
        _openLogButton.Text = "Open log";
        _openLogButton.AutoSize = true;
        _openLogButton.Click += (_, _) => OpenLog();
        _copyDiagnosticsButton.Text = "Copy diagnostics";
        _copyDiagnosticsButton.AutoSize = true;
        _copyDiagnosticsButton.Click += (_, _) => CopyDiagnostics();
        actions.Controls.Add(_openLogButton);
        actions.Controls.Add(_copyDiagnosticsButton);

        _logBox.Multiline = true;
        _logBox.ReadOnly = true;
        _logBox.ScrollBars = ScrollBars.Vertical;
        _logBox.Dock = DockStyle.Fill;
        _logBox.BorderStyle = BorderStyle.None;

        layout.Controls.Add(actions, 0, 0);
        layout.Controls.Add(_logBox, 0, 1);
        tab.Controls.Add(layout);
        return tab;
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

    private void AddBindingPairRow(TableLayoutPanel layout, int row, string leftLabel, PhysicalButton leftButton, string rightLabel, PhysicalButton? rightButton)
    {
        AddBindingCells(layout, row, 0, leftLabel, leftButton);
        if (rightButton is not null)
        {
            AddBindingCells(layout, row, 3, rightLabel, rightButton.Value);
        }
    }

    private void AddBindingCells(TableLayoutPanel layout, int row, int column, string label, PhysicalButton button)
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
            Margin = new Padding(8, 6, 0, 0)
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

        layout.Controls.Add(text, column, row);
        layout.Controls.Add(combo, column + 1, row);
        layout.Controls.Add(turbo, column + 2, row);
    }

    private void AddTurboSpeedRow(TableLayoutPanel layout, int row)
    {
        var label = new Label
        {
            Text = "Turbo speed",
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 9, 8, 0)
        };

        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, AutoSize = true };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));

        _turboSpeedSlider.Minimum = 25;
        _turboSpeedSlider.Maximum = 500;
        _turboSpeedSlider.TickFrequency = 50;
        _turboSpeedSlider.SmallChange = 5;
        _turboSpeedSlider.LargeChange = 25;
        _turboSpeedSlider.Dock = DockStyle.Fill;
        _turboSpeedSlider.ValueChanged += (_, _) =>
        {
            _turboSpeedValue.Text = $"{_turboSpeedSlider.Value} ms";
            SaveOptionsFromUi();
        };

        _turboSpeedValue.AutoSize = true;
        _turboSpeedValue.Anchor = AnchorStyles.Left;

        panel.Controls.Add(_turboSpeedSlider, 0, 0);
        panel.Controls.Add(_turboSpeedValue, 1, 0);
        layout.Controls.Add(label, 0, row);
        layout.Controls.Add(panel, 1, row);
        layout.SetColumnSpan(panel, 5);
    }

    private void AddTriggerTurboRow(TableLayoutPanel layout, int row)
    {
        var label = new Label
        {
            Text = "Triggers",
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 6, 8, 0)
        };

        var panel = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, WrapContents = true };
        _leftTriggerTurboCheck.Text = "LT Turbo";
        _leftTriggerTurboCheck.AutoSize = true;
        _leftTriggerTurboCheck.Margin = new Padding(0, 4, 16, 0);
        _leftTriggerTurboCheck.CheckedChanged += (_, _) => SaveOptionsFromUi();

        _rightTriggerTurboCheck.Text = "RT Turbo";
        _rightTriggerTurboCheck.AutoSize = true;
        _rightTriggerTurboCheck.Margin = new Padding(0, 4, 16, 0);
        _rightTriggerTurboCheck.CheckedChanged += (_, _) => SaveOptionsFromUi();

        panel.Controls.Add(_leftTriggerTurboCheck);
        panel.Controls.Add(_rightTriggerTurboCheck);

        layout.Controls.Add(label, 0, row);
        layout.Controls.Add(panel, 1, row);
        layout.SetColumnSpan(panel, 5);
    }

    private static TabPage CreateTab(string title)
    {
        return new TabPage(title) { Padding = new Padding(4) };
    }

    private static TableLayoutPanel CreateFormLayout(int columns)
    {
        return new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = columns,
            Padding = new Padding(10)
        };
    }

    private void AddCheckRow(TableLayoutPanel layout, int row, CheckBox checkBox, string text)
    {
        checkBox.Text = text;
        checkBox.AutoSize = true;
        checkBox.Margin = new Padding(0, 8, 0, 0);
        checkBox.CheckedChanged += (_, _) => SaveOptionsFromUi();
        layout.Controls.Add(checkBox, 1, row);
    }

    private void AddGyroSensitivityRow(TableLayoutPanel layout, int row)
    {
        var label = new Label
        {
            Text = "Gyro stick speed",
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 9, 8, 0)
        };

        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, AutoSize = true };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 46));

        _gyroStickSensitivitySlider.Minimum = 1;
        _gyroStickSensitivitySlider.Maximum = 80;
        _gyroStickSensitivitySlider.TickFrequency = 10;
        _gyroStickSensitivitySlider.SmallChange = 1;
        _gyroStickSensitivitySlider.LargeChange = 5;
        _gyroStickSensitivitySlider.Dock = DockStyle.Fill;
        _gyroStickSensitivitySlider.ValueChanged += (_, _) =>
        {
            _gyroStickSensitivityValue.Text = _gyroStickSensitivitySlider.Value.ToString();
            SaveOptionsFromUi();
        };

        _gyroStickSensitivityValue.AutoSize = true;
        _gyroStickSensitivityValue.Anchor = AnchorStyles.Left;

        panel.Controls.Add(_gyroStickSensitivitySlider, 0, 0);
        panel.Controls.Add(_gyroStickSensitivityValue, 1, 0);
        layout.Controls.Add(label, 0, row);
        layout.Controls.Add(panel, 1, row);
    }

    private void AddNumericRow(TableLayoutPanel layout, int row, string labelText, NumericUpDown input, int minimum, int maximum)
    {
        var label = new Label
        {
            Text = labelText,
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 9, 8, 0)
        };

        input.Minimum = minimum;
        input.Maximum = maximum;
        input.Width = 90;
        input.ValueChanged += (_, _) => SaveOptionsFromUi();

        layout.Controls.Add(label, 0, row);
        layout.Controls.Add(input, 1, row);
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
        _gyroToggleCombo.SelectedItem = _bridge.Options.GyroToggleButton;
        _gyroStickSensitivitySlider.Value = _bridge.Options.GyroStickSensitivity;
        _gyroStickSensitivityValue.Text = _bridge.Options.GyroStickSensitivity.ToString();
        _gyroStickDeadZoneInput.Value = _bridge.Options.GyroStickDeadZone;
        _turboSpeedSlider.Value = _bridge.Options.TurboIntervalMs;
        _turboSpeedValue.Text = $"{_bridge.Options.TurboIntervalMs} ms";
        _leftTriggerTurboCheck.Checked = _bridge.Options.LeftTriggerTurbo;
        _rightTriggerTurboCheck.Checked = _bridge.Options.RightTriggerTurbo;
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
        _bridge.Options.GyroToggleButton = (GyroToggleButton)(_gyroToggleCombo.SelectedItem ?? _bridge.Options.GyroToggleButton);
        _bridge.Options.GyroStickSensitivity = _gyroStickSensitivitySlider.Value;
        _bridge.Options.GyroStickDeadZone = (int)_gyroStickDeadZoneInput.Value;
        _bridge.Options.TurboIntervalMs = _turboSpeedSlider.Value;
        _bridge.Options.LeftTriggerTurbo = _leftTriggerTurboCheck.Checked;
        _bridge.Options.RightTriggerTurbo = _rightTriggerTurboCheck.Checked;
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
        ApplyTheme();
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
        var back = dark ? Color.FromArgb(18, 22, 28) : Color.FromArgb(244, 247, 250);
        var panel = dark ? Color.FromArgb(28, 34, 43) : Color.White;
        var fore = dark ? Color.FromArgb(238, 238, 238) : SystemColors.ControlText;
        var muted = dark ? Color.FromArgb(170, 178, 188) : Color.FromArgb(92, 101, 112);
        var accent = dark ? Color.FromArgb(75, 195, 255) : Color.FromArgb(0, 116, 217);
        var button = dark ? Color.FromArgb(43, 52, 64) : Color.FromArgb(226, 236, 246);
        var success = dark ? Color.FromArgb(24, 135, 94) : Color.FromArgb(0, 153, 102);

        BackColor = back;
        ForeColor = fore;
        ApplyThemeToControls(Controls, back, panel, fore, button, accent);
        _detailLabel.ForeColor = muted;
        _logBox.BackColor = panel;
        _logBox.ForeColor = fore;
        _logBox.BorderStyle = BorderStyle.None;
        _enableSwitch.BackColor = _bridge.Status.IsEnabled ? success : button;
        _enableSwitch.ForeColor = _bridge.Status.IsEnabled ? Color.White : fore;
        _advancedButton.BackColor = button;
        _advancedButton.ForeColor = fore;
    }

    private static void ApplyThemeToControls(
        Control.ControlCollection controls,
        Color back,
        Color panel,
        Color fore,
        Color button,
        Color accent)
    {
        foreach (Control control in controls)
        {
            control.ForeColor = fore;
            control.BackColor = control switch
            {
                TextBox or ComboBox or TabPage => panel,
                Button => button,
                _ => back
            };

            if (control is Button buttonControl)
            {
                buttonControl.FlatStyle = FlatStyle.Flat;
                buttonControl.FlatAppearance.BorderSize = 0;
            }

            if (control is TabControl tabControl)
            {
                tabControl.BackColor = panel;
            }

            if (control.HasChildren)
            {
                ApplyThemeToControls(control.Controls, back, panel, fore, button, accent);
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
