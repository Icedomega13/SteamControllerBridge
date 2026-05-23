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
    private readonly Label _connectionLabel = new();
    private readonly Label _sidebarStatusLabel = new();
    private readonly Panel _statusDot = new();
    private readonly Dictionary<string, Button> _navButtons = new();
    private TabControl? _contentTabs;
    private readonly Dictionary<PhysicalButton, ComboBox> _buttonMapCombos = new();
    private readonly Dictionary<PhysicalButton, CheckBox> _turboChecks = new();
    private readonly Dictionary<ControllerInput, TextBox> _keyboardKeyBoxes = new();
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
    private ControllerInput? _capturingKeyboardInput;

    public MainForm()
    {
        Text = "Steam Controller Bridge";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1120, 720);
        Size = new Size(1240, 780);
        Font = new Font("Segoe UI", 9F);
        KeyPreview = true;
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
            Padding = new Padding(0),
            RowCount = 1,
            ColumnCount = 2
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 230));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var sidebar = BuildSidebar();
        var main = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(28, 26, 28, 24),
            RowCount = 3,
            ColumnCount = 1
        };
        main.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        main.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        main.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            Margin = new Padding(0, 0, 0, 24)
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 250));

        var titleStack = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false
        };
        var title = new Label
        {
            Text = "Steam Controller Bridge",
            Font = new Font(Font.FontFamily, 22, FontStyle.Bold),
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 8)
        };
        _statusLabel.Font = new Font(Font.FontFamily, 12, FontStyle.Bold);
        _statusLabel.AutoSize = true;
        _statusLabel.Margin = new Padding(0, 0, 0, 6);

        _detailLabel.AutoSize = true;
        _detailLabel.Margin = new Padding(0);
        titleStack.Controls.Add(title);
        titleStack.Controls.Add(_statusLabel);
        titleStack.Controls.Add(_detailLabel);

        _enableSwitch.Text = "Off";
        _enableSwitch.Appearance = Appearance.Button;
        _enableSwitch.TextAlign = ContentAlignment.MiddleCenter;
        _enableSwitch.Font = new Font(Font.FontFamily, 18, FontStyle.Bold);
        _enableSwitch.Width = 220;
        _enableSwitch.Height = 82;
        _enableSwitch.Dock = DockStyle.Right;
        _enableSwitch.Margin = new Padding(0);
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
        header.Controls.Add(titleStack, 0, 0);
        header.Controls.Add(_enableSwitch, 1, 0);

        BuildQuickOptions();
        _quickOptionsPanel.Padding = new Padding(18, 14, 18, 14);
        _quickOptionsPanel.Margin = new Padding(0, 0, 0, 22);

        _advancedButton.Text = "Hide advanced";
        _advancedButton.AutoSize = false;
        _advancedButton.Width = 140;
        _advancedButton.Height = 34;
        _advancedButton.Margin = new Padding(14, 0, 0, 0);
        _advancedButton.FlatStyle = FlatStyle.Flat;
        _advancedButton.FlatAppearance.BorderSize = 0;
        _advancedButton.Click += (_, _) => ToggleAdvanced();
        _quickOptionsPanel.Controls.Add(_advancedButton);

        BuildAdvancedPanel();
        _advancedPanel.Visible = true;
        _advancedVisible = true;

        main.Controls.Add(header, 0, 0);
        main.Controls.Add(_quickOptionsPanel, 0, 1);
        main.Controls.Add(_advancedPanel, 0, 2);

        root.Controls.Add(sidebar, 0, 0);
        root.Controls.Add(main, 1, 0);
        Controls.Add(root);
    }

    private Panel BuildSidebar()
    {
        var sidebar = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18, 24, 16, 24)
        };

        var iconPanel = new Panel
        {
            Width = 104,
            Height = 104,
            Left = 45,
            Top = 24
        };
        iconPanel.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var fill = new SolidBrush(Color.FromArgb(36, 119, 96));
            using var pen = new Pen(Color.FromArgb(95, 242, 186), 2);
            e.Graphics.FillRoundedRectangle(fill, new Rectangle(10, 10, 84, 84), 18);
            e.Graphics.DrawRoundedRectangle(pen, new Rectangle(10, 10, 84, 84), 18);
            using var font = new Font("Segoe UI Symbol", 34, FontStyle.Regular);
            using var brush = new SolidBrush(Color.FromArgb(103, 255, 196));
            e.Graphics.DrawString("◇", font, brush, 34, 28);
        };
        sidebar.Controls.Add(iconPanel);

        var nav = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Width = 196,
            Height = 360,
            Left = 16,
            Top = 168
        };

        AddNavButton(nav, "Presets", 0);
        AddNavButton(nav, "Buttons", 1);
        AddNavButton(nav, "Keyboard", 2);
        AddNavButton(nav, "Motion", 3);
        AddNavButton(nav, "Logs", 4);
        sidebar.Controls.Add(nav);

        var statusCard = new Panel
        {
            Anchor = AnchorStyles.Left | AnchorStyles.Bottom,
            Width = 196,
            Height = 92,
            Left = 16,
            Top = 560
        };
        statusCard.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var fill = new SolidBrush(Color.FromArgb(23, 35, 53));
            using var pen = new Pen(Color.FromArgb(45, 68, 96));
            e.Graphics.FillRoundedRectangle(fill, new Rectangle(0, 0, statusCard.Width - 1, statusCard.Height - 1), 8);
            e.Graphics.DrawRoundedRectangle(pen, new Rectangle(0, 0, statusCard.Width - 1, statusCard.Height - 1), 8);
        };
        sidebar.Resize += (_, _) => statusCard.Top = sidebar.ClientSize.Height - 116;

        _connectionLabel.Text = "Virtual Xbox controller";
        _connectionLabel.AutoSize = false;
        _connectionLabel.Width = 140;
        _connectionLabel.Height = 38;
        _connectionLabel.Left = 14;
        _connectionLabel.Top = 14;
        _connectionLabel.Font = new Font(Font.FontFamily, 9, FontStyle.Regular);
        _sidebarStatusLabel.Text = "Disconnected";
        _sidebarStatusLabel.AutoSize = true;
        _sidebarStatusLabel.Left = 14;
        _sidebarStatusLabel.Top = 58;
        _sidebarStatusLabel.Font = new Font(Font.FontFamily, 9, FontStyle.Bold);
        _statusDot.Width = 9;
        _statusDot.Height = 9;
        _statusDot.Left = 170;
        _statusDot.Top = 22;
        _statusDot.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var brush = new SolidBrush(_bridge.Status.IsEnabled ? Color.FromArgb(90, 250, 178) : Color.FromArgb(110, 126, 148));
            e.Graphics.FillEllipse(brush, 0, 0, 8, 8);
        };
        statusCard.Controls.Add(_connectionLabel);
        statusCard.Controls.Add(_sidebarStatusLabel);
        statusCard.Controls.Add(_statusDot);
        sidebar.Controls.Add(statusCard);

        return sidebar;
    }

    private void AddNavButton(FlowLayoutPanel nav, string text, int tabIndex)
    {
        var button = new Button
        {
            Text = text,
            Width = 196,
            Height = 48,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(18, 0, 0, 0),
            Margin = new Padding(0, 0, 0, 10),
            FlatStyle = FlatStyle.Flat,
            Font = new Font(Font.FontFamily, 10, FontStyle.Regular),
            Tag = tabIndex
        };
        button.FlatAppearance.BorderSize = 0;
        button.Click += (_, _) =>
        {
            if (_contentTabs is not null)
            {
                _contentTabs.SelectedIndex = tabIndex;
                HighlightNav(tabIndex);
            }
        };
        _navButtons[text] = button;
        nav.Controls.Add(button);
    }

    private void BuildQuickOptions()
    {
        _quickOptionsPanel.AutoSize = true;
        _quickOptionsPanel.Dock = DockStyle.Top;
        _quickOptionsPanel.Margin = new Padding(0, 12, 0, 0);
        _quickOptionsPanel.WrapContents = true;
        _quickOptionsPanel.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var fill = new SolidBrush(Color.FromArgb(18, 31, 49));
            using var pen = new Pen(Color.FromArgb(41, 63, 91));
            e.Graphics.FillRoundedRectangle(fill, new Rectangle(0, 0, _quickOptionsPanel.Width - 1, _quickOptionsPanel.Height - 1), 10);
            e.Graphics.DrawRoundedRectangle(pen, new Rectangle(0, 0, _quickOptionsPanel.Width - 1, _quickOptionsPanel.Height - 1), 10);
        };

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
        _contentTabs = tabs;

        tabs.TabPages.Add(BuildPresetTab());
        tabs.TabPages.Add(BuildButtonsTab());
        tabs.TabPages.Add(BuildKeyboardTab());
        tabs.TabPages.Add(BuildMotionTab());
        tabs.TabPages.Add(BuildLogsTab());
        tabs.SelectedIndexChanged += (_, _) => HighlightNav(tabs.SelectedIndex);
        tabs.SelectedIndex = 1;
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

    private TabPage BuildKeyboardTab()
    {
        var tab = CreateTab("Keyboard");
        var scroller = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        var layout = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Top,
            ColumnCount = 6,
            Padding = new Padding(12)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 86));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 64));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 86));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 64));

        var row = 0;
        AddKeyboardPairRow(layout, row++, "A", ControllerInput.A, "View", ControllerInput.Back);
        AddKeyboardPairRow(layout, row++, "B", ControllerInput.B, "Menu", ControllerInput.Start);
        AddKeyboardPairRow(layout, row++, "X", ControllerInput.X, "Steam", ControllerInput.Guide);
        AddKeyboardPairRow(layout, row++, "Y", ControllerInput.Y, "D-pad up", ControllerInput.DPadUp);
        AddKeyboardPairRow(layout, row++, "LB", ControllerInput.LeftShoulder, "D-pad down", ControllerInput.DPadDown);
        AddKeyboardPairRow(layout, row++, "RB", ControllerInput.RightShoulder, "D-pad left", ControllerInput.DPadLeft);
        AddKeyboardPairRow(layout, row++, "LT", ControllerInput.LeftTrigger, "D-pad right", ControllerInput.DPadRight);
        AddKeyboardPairRow(layout, row++, "RT", ControllerInput.RightTrigger, "L3", ControllerInput.LeftThumb);
        AddKeyboardPairRow(layout, row++, "R3", ControllerInput.RightThumb, "L4", ControllerInput.L4);
        AddKeyboardPairRow(layout, row++, "L5", ControllerInput.L5, "R4", ControllerInput.R4);
        AddKeyboardPairRow(layout, row++, "R5", ControllerInput.R5, "Left pad", ControllerInput.LeftPadClick);
        AddKeyboardPairRow(layout, row, "Right pad", ControllerInput.RightPadClick, string.Empty, null);

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

    private void AddKeyboardPairRow(TableLayoutPanel layout, int row, string leftLabel, ControllerInput leftInput, string rightLabel, ControllerInput? rightInput)
    {
        AddKeyboardCells(layout, row, 0, leftLabel, leftInput);
        if (rightInput is not null)
        {
            AddKeyboardCells(layout, row, 3, rightLabel, rightInput.Value);
        }
    }

    private void AddKeyboardCells(TableLayoutPanel layout, int row, int column, string label, ControllerInput input)
    {
        var text = new Label
        {
            Text = label,
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 6, 8, 0)
        };

        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, AutoSize = true };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 56));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 58));

        var keyBox = new TextBox
        {
            ReadOnly = true,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 1, 6, 1),
            TabStop = false
        };
        _keyboardKeyBoxes[input] = keyBox;

        var setButton = new Button
        {
            Text = "Set",
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 6, 0)
        };
        setButton.Click += (_, _) => BeginKeyboardCapture(input, keyBox);

        var clearButton = new Button
        {
            Text = "Clear",
            Dock = DockStyle.Fill,
            Margin = new Padding(0)
        };
        clearButton.Click += (_, _) =>
        {
            _bridge.Options.SetKeyboardKey(input, 0);
            keyBox.Text = string.Empty;
            _bridge.SaveOptions();
        };

        panel.Controls.Add(keyBox, 0, 0);
        panel.Controls.Add(setButton, 1, 0);
        panel.Controls.Add(clearButton, 2, 0);

        layout.Controls.Add(text, column, row);
        layout.Controls.Add(panel, column + 1, row);
        layout.SetColumnSpan(panel, 2);
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

        foreach (var (input, keyBox) in _keyboardKeyBoxes)
        {
            keyBox.Text = FormatKeyName(_bridge.Options.GetKeyboardKey(input));
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
    }

    private void ApplyTheme()
    {
        var back = Color.FromArgb(7, 18, 33);
        var panel = Color.FromArgb(15, 28, 46);
        var input = Color.FromArgb(20, 35, 55);
        var fore = Color.FromArgb(239, 246, 255);
        var muted = Color.FromArgb(151, 167, 190);
        var accent = Color.FromArgb(91, 244, 183);
        var button = Color.FromArgb(24, 43, 66);
        var success = Color.FromArgb(30, 158, 106);
        var disabled = Color.FromArgb(43, 54, 72);

        BackColor = back;
        ForeColor = fore;
        ApplyThemeToControls(Controls, back, panel, input, fore, button, accent);
        _detailLabel.ForeColor = muted;
        _statusLabel.ForeColor = _bridge.Status.HasError ? Color.FromArgb(255, 178, 111) : _bridge.Status.IsEnabled ? accent : muted;
        _connectionLabel.ForeColor = fore;
        _sidebarStatusLabel.ForeColor = _bridge.Status.IsEnabled ? accent : muted;
        _statusDot.Invalidate();
        _logBox.BackColor = panel;
        _logBox.ForeColor = fore;
        _logBox.BorderStyle = BorderStyle.None;
        _enableSwitch.BackColor = _bridge.Status.IsEnabled ? success : disabled;
        _enableSwitch.ForeColor = _bridge.Status.IsEnabled ? Color.White : fore;
        _advancedButton.BackColor = button;
        _advancedButton.ForeColor = fore;
        _quickOptionsPanel.Invalidate();
        HighlightNav(_contentTabs?.SelectedIndex ?? 1);
    }

    private static void ApplyThemeToControls(
        Control.ControlCollection controls,
        Color back,
        Color panel,
        Color input,
        Color fore,
        Color button,
        Color accent)
    {
        foreach (Control control in controls)
        {
            control.ForeColor = fore;
            control.BackColor = control switch
            {
                TextBox or ComboBox or NumericUpDown => input,
                TabPage => back,
                Button => button,
                FlowLayoutPanel or TableLayoutPanel => back,
                Panel => panel,
                TabControl => back,
                _ => back
            };

            if (control is Button buttonControl)
            {
                buttonControl.FlatStyle = FlatStyle.Flat;
                buttonControl.FlatAppearance.BorderSize = 0;
            }

            if (control is ComboBox comboBox)
            {
                comboBox.FlatStyle = FlatStyle.Flat;
            }

            if (control is CheckBox checkBox)
            {
                checkBox.FlatStyle = FlatStyle.Flat;
            }

            if (control.HasChildren)
            {
                ApplyThemeToControls(control.Controls, back, panel, input, fore, button, accent);
            }
        }
    }

    private void HighlightNav(int selectedIndex)
    {
        foreach (var button in _navButtons.Values)
        {
            var active = button.Tag is int index && index == selectedIndex;
            button.BackColor = active ? Color.FromArgb(31, 112, 89) : Color.FromArgb(7, 18, 33);
            button.ForeColor = active ? Color.FromArgb(114, 255, 202) : Color.FromArgb(203, 214, 231);
            button.FlatAppearance.MouseOverBackColor = active ? Color.FromArgb(35, 126, 100) : Color.FromArgb(18, 31, 49);
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

    private void BeginKeyboardCapture(ControllerInput input, TextBox keyBox)
    {
        _capturingKeyboardInput = input;
        keyBox.Text = "Press a key...";
        keyBox.Focus();
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (_capturingKeyboardInput is not { } input)
        {
            return base.ProcessCmdKey(ref msg, keyData);
        }

        var keyCode = keyData & Keys.KeyCode;
        if (keyCode == Keys.Escape)
        {
            _capturingKeyboardInput = null;
            if (_keyboardKeyBoxes.TryGetValue(input, out var cancelBox))
            {
                cancelBox.Text = FormatKeyName(_bridge.Options.GetKeyboardKey(input));
            }

            return true;
        }

        if (keyCode != Keys.None)
        {
            _bridge.Options.SetKeyboardKey(input, (int)keyCode);
            _bridge.SaveOptions();
            if (_keyboardKeyBoxes.TryGetValue(input, out var keyBox))
            {
                keyBox.Text = FormatKeyName((int)keyCode);
            }
        }

        _capturingKeyboardInput = null;
        return true;
    }

    private static string FormatKeyName(int key)
    {
        return key <= 0 ? string.Empty : ((Keys)key).ToString();
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

internal static class GraphicsExtensions
{
    public static void FillRoundedRectangle(this Graphics graphics, Brush brush, Rectangle bounds, int radius)
    {
        using var path = CreateRoundedRectangle(bounds, radius);
        graphics.FillPath(brush, path);
    }

    public static void DrawRoundedRectangle(this Graphics graphics, Pen pen, Rectangle bounds, int radius)
    {
        using var path = CreateRoundedRectangle(bounds, radius);
        graphics.DrawPath(pen, path);
    }

    private static System.Drawing.Drawing2D.GraphicsPath CreateRoundedRectangle(Rectangle bounds, int radius)
    {
        var diameter = radius * 2;
        var path = new System.Drawing.Drawing2D.GraphicsPath();
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}
