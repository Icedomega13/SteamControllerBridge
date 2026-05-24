using System.Diagnostics;
using SteamControllerBridge.Bridge;

namespace SteamControllerBridge;

internal sealed class MainForm : Form
{
    private readonly BridgeService _bridge = new();
    private readonly NotifyIcon _trayIcon;
    private readonly Label _statusLabel = new();
    private readonly Label _detailLabel = new();
    private readonly Panel _quickOptionsPanel = new();
    private readonly FlowLayoutPanel _quickOptionsContent = new();
    private readonly TextBox _logBox = new();
    private readonly Panel _advancedPanel = new();
    private readonly Panel _sidebarPanel = new();
    private readonly Panel _controllerPowerPanel = new();
    private readonly FlowLayoutPanel _sidebarNavPanel = new();
    private readonly Panel _sidebarStatusCard = new();
    private readonly Label _connectionLabel = new();
    private readonly Label _sidebarStatusLabel = new();
    private readonly Panel _statusDot = new();
    private readonly PictureBox _controllerPowerImage = new();
    private readonly ToolTip _toolTip = new();
    private readonly Dictionary<string, Button> _navButtons = new();
    private readonly Dictionary<int, Image> _navActiveImages = new();
    private readonly Dictionary<int, Image> _navInactiveImages = new();
    private readonly Panel _contentHost = new();
    private readonly List<Control> _contentPages = new();
    private int _selectedPageIndex = 1;
    private readonly Dictionary<PhysicalButton, ComboBox> _buttonMapCombos = new();
    private readonly Dictionary<PhysicalButton, CheckBox> _turboChecks = new();
    private readonly Dictionary<ControllerInput, TextBox> _keyboardKeyBoxes = new();
    private readonly ComboBox _presetCombo = new();
    private readonly Button _applyPresetButton = new();
    private readonly ComboBox _profileCombo = new();
    private readonly TextBox _profileNameBox = new();
    private readonly Button _loadProfileButton = new();
    private readonly Button _saveProfileButton = new();
    private readonly Button _importProfileButton = new();
    private readonly Button _exportProfileButton = new();
    private readonly Button _deleteProfileButton = new();
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
    private readonly CheckBox _leftStickWasdCheck = new();
    private readonly CheckBox _rightStickMouseCheck = new();
    private readonly CheckBox _invertRightStickYCheck = new();
    private readonly TrackBar _rightStickMouseSensitivitySlider = new();
    private readonly Label _rightStickMouseSensitivityValue = new();
    private readonly CheckBox _gyroMouseCheck = new();
    private readonly CheckBox _rumbleCheck = new();
    private readonly CheckBox _startWithWindowsCheck = new();
    private readonly CheckBox _autoDisableForSteamCheck = new();
    private readonly Button _openLogButton = new();
    private readonly Button _copyDiagnosticsButton = new();
    private readonly System.Windows.Forms.Timer _lifecycleTimer = new();
    private ToolStripMenuItem? _rumbleTrayItem;
    private readonly Icon _appIcon;
    private readonly Image? _controllerOnImage;
    private readonly Image? _controllerOffImage;
    private bool _updatingOptions;
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
        _controllerOnImage = LoadSoftImageAsset("ControllerON.png");
        _controllerOffImage = LoadSoftImageAsset("ControllerOFF.png");
        LoadNavigationImages();

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
            _controllerOnImage?.Dispose();
            _controllerOffImage?.Dispose();
            foreach (var image in _navActiveImages.Values.Concat(_navInactiveImages.Values))
            {
                image.Dispose();
            }

            _toolTip.Dispose();
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
            ColumnCount = 1,
            Margin = new Padding(0, 0, 0, 24)
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

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

        header.Controls.Add(titleStack, 0, 0);

        BuildQuickOptions();
        _quickOptionsPanel.Margin = new Padding(0, 0, 0, 22);

        BuildAdvancedPanel();
        _advancedPanel.Visible = true;

        main.Controls.Add(header, 0, 0);
        main.Controls.Add(_quickOptionsPanel, 0, 1);
        main.Controls.Add(_advancedPanel, 0, 2);

        root.Controls.Add(sidebar, 0, 0);
        root.Controls.Add(main, 1, 0);
        Controls.Add(root);
    }

    private Panel BuildSidebar()
    {
        _sidebarPanel.Dock = DockStyle.Fill;
        _sidebarPanel.Padding = new Padding(18, 24, 16, 24);
        _sidebarPanel.BackColor = Color.FromArgb(7, 18, 33);

        _controllerPowerPanel.Width = 120;
        _controllerPowerPanel.Height = 120;
        _controllerPowerPanel.Left = 37;
        _controllerPowerPanel.Top = 18;
        _controllerPowerPanel.BackColor = Color.FromArgb(7, 18, 33);

        _controllerPowerImage.Dock = DockStyle.Fill;
        _controllerPowerImage.Margin = new Padding(0);
        _controllerPowerImage.Padding = new Padding(4);
        _controllerPowerImage.SizeMode = PictureBoxSizeMode.Zoom;
        _controllerPowerImage.BackColor = Color.Transparent;
        _controllerPowerImage.Cursor = Cursors.Hand;
        _controllerPowerImage.Click += (_, _) => ToggleBridgeFromIcon();
        _toolTip.SetToolTip(_controllerPowerImage, "Click to connect or disconnect");
        _controllerPowerPanel.Controls.Add(_controllerPowerImage);
        _sidebarPanel.Controls.Add(_controllerPowerPanel);

        _sidebarNavPanel.FlowDirection = FlowDirection.TopDown;
        _sidebarNavPanel.WrapContents = false;
        _sidebarNavPanel.Width = 196;
        _sidebarNavPanel.Height = 430;
        _sidebarNavPanel.Left = 16;
        _sidebarNavPanel.Top = 168;
        _sidebarNavPanel.BackColor = Color.FromArgb(7, 18, 33);

        AddNavButton(_sidebarNavPanel, "Presets", 0);
        AddNavButton(_sidebarNavPanel, "Buttons", 1);
        AddNavButton(_sidebarNavPanel, "Keyboard", 2);
        AddNavButton(_sidebarNavPanel, "Motion", 3);
        AddNavButton(_sidebarNavPanel, "Logs", 4);
        _sidebarPanel.Controls.Add(_sidebarNavPanel);

        _sidebarStatusCard.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
        _sidebarStatusCard.Width = 196;
        _sidebarStatusCard.Height = 92;
        _sidebarStatusCard.Left = 16;
        _sidebarStatusCard.Top = 560;
        _sidebarStatusCard.BackColor = Color.FromArgb(7, 18, 33);
        _sidebarStatusCard.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var fill = new SolidBrush(Color.FromArgb(7, 18, 33));
            using var pen = new Pen(Color.FromArgb(33, 55, 82));
            e.Graphics.FillRoundedRectangle(fill, new Rectangle(0, 0, _sidebarStatusCard.Width - 1, _sidebarStatusCard.Height - 1), 8);
            e.Graphics.DrawRoundedRectangle(pen, new Rectangle(0, 0, _sidebarStatusCard.Width - 1, _sidebarStatusCard.Height - 1), 8);
        };
        _sidebarPanel.Resize += (_, _) => _sidebarStatusCard.Top = _sidebarPanel.ClientSize.Height - 116;

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
        _sidebarStatusCard.Controls.Add(_connectionLabel);
        _sidebarStatusCard.Controls.Add(_sidebarStatusLabel);
        _sidebarStatusCard.Controls.Add(_statusDot);
        _sidebarPanel.Controls.Add(_sidebarStatusCard);

        return _sidebarPanel;
    }

    private void AddNavButton(FlowLayoutPanel nav, string text, int tabIndex)
    {
        var button = new Button
        {
            Text = text,
            Name = text,
            Width = 196,
            Height = 72,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(0),
            Margin = new Padding(0, 0, 0, 12),
            FlatStyle = FlatStyle.Flat,
            Font = new Font(Font.FontFamily, 10, FontStyle.Bold),
            BackgroundImageLayout = ImageLayout.Stretch,
            Tag = tabIndex
        };
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseDownBackColor = Color.FromArgb(24, 48, 68);
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(18, 31, 49);
        _toolTip.SetToolTip(button, text);
        button.Click += (_, _) =>
        {
            SelectPage(tabIndex);
        };
        _navButtons[text] = button;
        nav.Controls.Add(button);
    }

    private void BuildQuickOptions()
    {
        _quickOptionsPanel.Height = 48;
        _quickOptionsPanel.Dock = DockStyle.Top;
        _quickOptionsPanel.Margin = new Padding(0, 12, 0, 0);
        _quickOptionsPanel.Padding = new Padding(18, 12, 18, 10);
        _quickOptionsPanel.Resize += (_, _) => _quickOptionsPanel.Invalidate();
        _quickOptionsPanel.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var fill = new SolidBrush(Color.FromArgb(18, 31, 49));
            using var pen = new Pen(Color.FromArgb(41, 63, 91));
            e.Graphics.FillRoundedRectangle(fill, new Rectangle(0, 0, _quickOptionsPanel.Width - 1, _quickOptionsPanel.Height - 1), 10);
            e.Graphics.DrawRoundedRectangle(pen, new Rectangle(0, 0, _quickOptionsPanel.Width - 1, _quickOptionsPanel.Height - 1), 10);
        };

        _quickOptionsContent.Dock = DockStyle.Fill;
        _quickOptionsContent.FlowDirection = FlowDirection.LeftToRight;
        _quickOptionsContent.WrapContents = false;
        _quickOptionsContent.AutoScroll = false;
        _quickOptionsContent.Margin = new Padding(0);
        _quickOptionsContent.Padding = new Padding(0);

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

        _startWithWindowsCheck.Text = "Start with Windows";
        _startWithWindowsCheck.AutoSize = true;
        _startWithWindowsCheck.Margin = new Padding(0, 0, 18, 0);
        _startWithWindowsCheck.CheckedChanged += (_, _) => SaveOptionsFromUi();

        _autoDisableForSteamCheck.Text = "Back off when Steam opens";
        _autoDisableForSteamCheck.AutoSize = true;
        _autoDisableForSteamCheck.Margin = new Padding(0, 0, 0, 0);
        _autoDisableForSteamCheck.CheckedChanged += (_, _) => SaveOptionsFromUi();

        _quickOptionsContent.Controls.Add(_rumbleCheck);
        _quickOptionsContent.Controls.Add(_gyroMouseCheck);
        _quickOptionsContent.Controls.Add(_gyroActivationCombo);
        _quickOptionsContent.Controls.Add(_startWithWindowsCheck);
        _quickOptionsContent.Controls.Add(_autoDisableForSteamCheck);
        _quickOptionsPanel.Controls.Add(_quickOptionsContent);
    }

    private void BuildAdvancedPanel()
    {
        _advancedPanel.Dock = DockStyle.Fill;
        _advancedPanel.Visible = false;
        _advancedPanel.AutoScroll = false;

        ConfigureCombo(_trackpadSourceCombo, Enum.GetValues<TrackpadMouseSource>());
        ConfigureCombo(_presetCombo, Enum.GetValues<RemapPreset>());
        ConfigureCombo(_profileCombo, Array.Empty<string>());
        ConfigureCombo(_gyroOutputCombo, Enum.GetValues<GyroOutputMode>());
        ConfigureCombo(_gyroToggleCombo, Enum.GetValues<GyroToggleButton>());

        _contentHost.Dock = DockStyle.Fill;
        _contentHost.Margin = new Padding(0);
        _contentHost.Padding = new Padding(0);
        _contentHost.BackColor = Color.FromArgb(7, 18, 33);
        _contentPages.Clear();

        AddContentPage(BuildPresetTab());
        AddContentPage(BuildButtonsTab());
        AddContentPage(BuildKeyboardTab());
        AddContentPage(BuildMotionTab());
        AddContentPage(BuildLogsTab());

        _advancedPanel.Controls.Add(_contentHost);
        SelectPage(1);
    }

    private void AddContentPage(Control page)
    {
        page.Dock = DockStyle.Fill;
        page.Margin = new Padding(0);
        page.Visible = false;
        _contentPages.Add(page);
        _contentHost.Controls.Add(page);
    }

    private void SelectPage(int selectedIndex)
    {
        if (selectedIndex < 0 || selectedIndex >= _contentPages.Count)
        {
            return;
        }

        for (var index = 0; index < _contentPages.Count; index++)
        {
            _contentPages[index].Visible = index == selectedIndex;
        }

        _selectedPageIndex = selectedIndex;
        HighlightNav(selectedIndex);
    }

    private Control BuildPresetTab()
    {
        var tab = CreateTab("Presets");
        var layout = CreateFormLayout(3);
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        for (var row = 0; row < 5; row++)
        {
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }

        AddOptionRow(layout, 0, "Preset", _presetCombo);
        _applyPresetButton.Text = "Apply";
        _applyPresetButton.Height = 32;
        _applyPresetButton.Dock = DockStyle.Fill;
        _applyPresetButton.Click += (_, _) => ApplySelectedPreset();
        layout.Controls.Add(_applyPresetButton, 2, 0);

        var profileHeader = new Label
        {
            Text = "Profiles",
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Font = new Font(Font, FontStyle.Bold),
            Margin = new Padding(0, 18, 8, 4)
        };
        layout.Controls.Add(profileHeader, 0, 1);

        AddOptionRow(layout, 2, "Profile", _profileCombo);
        ConfigureSmallButton(_loadProfileButton, "Load");
        _loadProfileButton.Click += (_, _) => LoadSelectedProfile();
        layout.Controls.Add(_loadProfileButton, 2, 2);

        _profileNameBox.Dock = DockStyle.Fill;
        _profileNameBox.Margin = new Padding(0, 7, 8, 0);
        var saveLabel = new Label
        {
            Text = "Save as",
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 9, 8, 0)
        };
        layout.Controls.Add(saveLabel, 0, 3);
        layout.Controls.Add(_profileNameBox, 1, 3);
        ConfigureSmallButton(_saveProfileButton, "Save");
        _saveProfileButton.Click += (_, _) => SaveProfile();
        layout.Controls.Add(_saveProfileButton, 2, 3);

        var profileActions = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Top, Margin = new Padding(0, 8, 0, 0) };
        ConfigureSmallButton(_importProfileButton, "Import");
        ConfigureSmallButton(_exportProfileButton, "Export");
        ConfigureSmallButton(_deleteProfileButton, "Delete");
        _importProfileButton.Click += (_, _) => ImportProfile();
        _exportProfileButton.Click += (_, _) => ExportSelectedProfile();
        _deleteProfileButton.Click += (_, _) => DeleteSelectedProfile();
        profileActions.Controls.Add(_importProfileButton);
        profileActions.Controls.Add(_exportProfileButton);
        profileActions.Controls.Add(_deleteProfileButton);
        layout.Controls.Add(profileActions, 1, 4);
        layout.SetColumnSpan(profileActions, 2);
        tab.Controls.Add(layout);
        return tab;
    }

    private Control BuildButtonsTab()
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

    private Control BuildKeyboardTab()
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

    private Control BuildMotionTab()
    {
        var tab = CreateTab("Motion");
        var layout = CreateFormLayout(2);
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var row = 0; row < 12; row++)
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
        AddCheckRow(layout, 7, _leftStickWasdCheck, "Left stick sends WASD");
        AddCheckRow(layout, 8, _rightStickMouseCheck, "Right stick controls mouse");
        AddRightStickSensitivityRow(layout, 9);
        AddCheckRow(layout, 10, _invertRightStickYCheck, "Invert right-stick vertical mouse");
        tab.Controls.Add(layout);
        return tab;
    }

    private Control BuildLogsTab()
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

    private static void ConfigureSmallButton(Button button, string text)
    {
        button.Text = text;
        button.Height = 32;
        button.AutoSize = false;
        button.Width = 88;
        button.Margin = new Padding(0, 6, 8, 0);
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

    private static Panel CreateTab(string title)
    {
        return new Panel
        {
            Padding = new Padding(0),
            Margin = new Padding(0)
        };
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

    private void AddRightStickSensitivityRow(TableLayoutPanel layout, int row)
    {
        var label = new Label
        {
            Text = "Right stick mouse speed",
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 9, 8, 0)
        };

        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, AutoSize = true };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 46));

        _rightStickMouseSensitivitySlider.Minimum = 1;
        _rightStickMouseSensitivitySlider.Maximum = 80;
        _rightStickMouseSensitivitySlider.TickFrequency = 10;
        _rightStickMouseSensitivitySlider.SmallChange = 1;
        _rightStickMouseSensitivitySlider.LargeChange = 5;
        _rightStickMouseSensitivitySlider.Dock = DockStyle.Fill;
        _rightStickMouseSensitivitySlider.ValueChanged += (_, _) =>
        {
            _rightStickMouseSensitivityValue.Text = _rightStickMouseSensitivitySlider.Value.ToString();
            SaveOptionsFromUi();
        };

        _rightStickMouseSensitivityValue.AutoSize = true;
        _rightStickMouseSensitivityValue.Anchor = AnchorStyles.Left;

        panel.Controls.Add(_rightStickMouseSensitivitySlider, 0, 0);
        panel.Controls.Add(_rightStickMouseSensitivityValue, 1, 0);
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
        menu.Items.Add(_rumbleTrayItem);
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
        _leftStickWasdCheck.Checked = _bridge.Options.LeftStickWasdEnabled;
        _rightStickMouseCheck.Checked = _bridge.Options.RightStickMouseEnabled;
        _invertRightStickYCheck.Checked = _bridge.Options.InvertRightStickY;
        _rightStickMouseSensitivitySlider.Value = _bridge.Options.RightStickMouseSensitivity;
        _rightStickMouseSensitivityValue.Text = _bridge.Options.RightStickMouseSensitivity.ToString();
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
        RefreshProfiles();
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
        _bridge.Options.LeftStickWasdEnabled = _leftStickWasdCheck.Checked;
        _bridge.Options.RightStickMouseEnabled = _rightStickMouseCheck.Checked;
        _bridge.Options.InvertRightStickY = _invertRightStickYCheck.Checked;
        _bridge.Options.RightStickMouseSensitivity = _rightStickMouseSensitivitySlider.Value;
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

    private void RefreshProfiles(string? selectedProfile = null)
    {
        var current = selectedProfile ?? _profileCombo.SelectedItem as string;
        _profileCombo.Items.Clear();
        foreach (var profile in BridgeProfileStore.ListProfiles())
        {
            _profileCombo.Items.Add(profile);
        }

        if (current is not null && _profileCombo.Items.Contains(current))
        {
            _profileCombo.SelectedItem = current;
        }
        else if (_profileCombo.Items.Count > 0)
        {
            _profileCombo.SelectedIndex = 0;
        }

        var hasSelection = _profileCombo.SelectedItem is not null;
        _loadProfileButton.Enabled = hasSelection;
        _exportProfileButton.Enabled = hasSelection;
        _deleteProfileButton.Enabled = hasSelection;
    }

    private void SaveProfile()
    {
        var name = BridgeProfileStore.SanitizeProfileName(_profileNameBox.Text);
        if (string.IsNullOrWhiteSpace(_profileNameBox.Text))
        {
            MessageBox.Show(this, "Enter a profile name first.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        SaveOptionsFromUi();
        BridgeProfileStore.Save(name, _bridge.Options);
        RefreshProfiles(name);
        _profileNameBox.Text = name;
        AppendLog($"{DateTime.Now:HH:mm:ss}  Saved profile: {name}");
    }

    private void LoadSelectedProfile()
    {
        if (_profileCombo.SelectedItem is not string name)
        {
            return;
        }

        _bridge.LoadOptions(BridgeProfileStore.Load(name));
        LoadOptionsIntoUi();
        RefreshProfiles(name);
        AppendLog($"{DateTime.Now:HH:mm:ss}  Loaded profile: {name}");
    }

    private void ImportProfile()
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "Steam Controller Bridge profile (*.scbprofile)|*.scbprofile|JSON files (*.json)|*.json|All files (*.*)|*.*",
            Title = "Import profile"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            var name = BridgeProfileStore.Import(dialog.FileName);
            RefreshProfiles(name);
            AppendLog($"{DateTime.Now:HH:mm:ss}  Imported profile: {name}");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Could not import profile:\n{ex.Message}", Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ExportSelectedProfile()
    {
        if (_profileCombo.SelectedItem is not string name)
        {
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Filter = "Steam Controller Bridge profile (*.scbprofile)|*.scbprofile",
            FileName = $"{name}.scbprofile",
            Title = "Export profile"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        BridgeProfileStore.Export(name, dialog.FileName);
        AppendLog($"{DateTime.Now:HH:mm:ss}  Exported profile: {name}");
    }

    private void DeleteSelectedProfile()
    {
        if (_profileCombo.SelectedItem is not string name)
        {
            return;
        }

        var confirm = MessageBox.Show(this, $"Delete profile '{name}'?", Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (confirm != DialogResult.Yes)
        {
            return;
        }

        BridgeProfileStore.Delete(name);
        RefreshProfiles();
        AppendLog($"{DateTime.Now:HH:mm:ss}  Deleted profile: {name}");
    }

    private void SyncTrayOptions()
    {
        if (_rumbleTrayItem is not null)
        {
            _rumbleTrayItem.Checked = _rumbleCheck.Checked;
        }

    }

    private void ApplyStatus(BridgeStatus status)
    {
        _statusLabel.Text = status.HasError ? "Needs attention" : status.IsEnabled ? "Ready" : "Idle";
        _detailLabel.Text = status.Message;
        _sidebarStatusLabel.Text = status.IsEnabled ? "Connected" : status.IsWorking ? "Working" : "Disconnected";
        _controllerPowerImage.Enabled = !status.IsWorking;
        _controllerPowerImage.Cursor = status.IsWorking ? Cursors.WaitCursor : Cursors.Hand;
        _controllerPowerImage.Image = status.IsEnabled ? _controllerOnImage : _controllerOffImage;
        _toolTip.SetToolTip(_controllerPowerImage, status.IsEnabled ? "Click to disconnect" : "Click to connect");
        _trayIcon.Text = ClampTrayText($"Steam Controller Bridge - {status.Message}");
        ApplyTheme();
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
        _quickOptionsContent.BackColor = Color.Transparent;
        _sidebarPanel.BackColor = back;
        _controllerPowerPanel.BackColor = back;
        _sidebarNavPanel.BackColor = back;
        _sidebarStatusCard.BackColor = back;
        _sidebarStatusCard.Invalidate();
        _quickOptionsPanel.Invalidate();
        HighlightNav(_selectedPageIndex);
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
            var tabIndex = button.Tag is int indexValue ? indexValue : -1;
            var hasImage = active
                ? _navActiveImages.TryGetValue(tabIndex, out var navImage)
                : _navInactiveImages.TryGetValue(tabIndex, out navImage);

            button.BackgroundImage = hasImage ? navImage : null;
            button.Text = hasImage ? string.Empty : button.Name;
            button.BackColor = Color.FromArgb(7, 18, 33);
            button.ForeColor = active ? Color.FromArgb(114, 255, 202) : Color.FromArgb(226, 234, 246);
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(13, 28, 47);
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

    private void ToggleBridgeFromIcon()
    {
        if (_bridge.Status.IsWorking)
        {
            return;
        }

        if (_bridge.Status.IsEnabled)
        {
            StopBridgeAsync();
        }
        else
        {
            StartBridgeAsync();
        }
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

    private void LoadNavigationImages()
    {
        AddNavigationImages(0, "Presets.png", "presentsUNclicked.png");
        AddNavigationImages(1, "Buttons.png", "buttonsUnClicked.png");
        AddNavigationImages(2, "keyboard.png", "keyboardUNclicked.png");
        AddNavigationImages(3, "motion.png", "MotionUNclicked.png");
        AddNavigationImages(4, "logs.png", "LogsUNclicked.png");
    }

    private void AddNavigationImages(int index, string activeFile, string inactiveFile)
    {
        if (LoadImageAsset(activeFile) is { } active)
        {
            _navActiveImages[index] = CreateNavigationImage(active);
            active.Dispose();
        }

        if (LoadImageAsset(inactiveFile) is { } inactive)
        {
            _navInactiveImages[index] = CreateNavigationImage(inactive);
            inactive.Dispose();
        }
    }

    private static Image? LoadImageAsset(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", fileName);
        if (!File.Exists(path))
        {
            return null;
        }

        using var stream = File.OpenRead(path);
        using var image = Image.FromStream(stream);
        return new Bitmap(image);
    }

    private static Image? LoadSoftImageAsset(string fileName)
    {
        using var image = LoadImageAsset(fileName);
        return image is null ? null : CreateSoftEdgeImage(image, 0.20f);
    }

    private static Bitmap CreateNavigationImage(Image source)
    {
        const int width = 392;
        const int height = 144;
        var cropAspect = width / (float)height;
        var cropWidth = source.Width;
        var cropHeight = Math.Min(source.Height, (int)(cropWidth / cropAspect));
        var cropY = Math.Max(0, (source.Height - cropHeight) / 2);
        var crop = new Rectangle(0, cropY, cropWidth, cropHeight);

        var output = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
        using var graphics = Graphics.FromImage(output);
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        graphics.Clear(Color.Transparent);

        using var clip = GraphicsExtensions.CreateRoundedRectangle(new Rectangle(0, 0, width, height), 12);
        graphics.SetClip(clip);
        graphics.DrawImage(source, new Rectangle(0, 0, width, height), crop, GraphicsUnit.Pixel);

        using var veil = new SolidBrush(Color.FromArgb(58, 7, 18, 33));
        graphics.FillRectangle(veil, 0, 0, width, height);
        return output;
    }

    private static Bitmap CreateSoftEdgeImage(Image source, float fadeFraction)
    {
        var output = new Bitmap(source.Width, source.Height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
        using (var graphics = Graphics.FromImage(output))
        {
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            graphics.DrawImage(source, 0, 0, source.Width, source.Height);
        }

        var fade = Math.Max(1, (int)(Math.Min(output.Width, output.Height) * fadeFraction));
        for (var y = 0; y < output.Height; y++)
        {
            for (var x = 0; x < output.Width; x++)
            {
                var edgeDistance = Math.Min(Math.Min(x, output.Width - 1 - x), Math.Min(y, output.Height - 1 - y));
                var alphaScale = Math.Clamp(edgeDistance / (float)fade, 0f, 1f);
                alphaScale *= alphaScale;

                var pixel = output.GetPixel(x, y);
                output.SetPixel(x, y, Color.FromArgb((int)(pixel.A * alphaScale), pixel.R, pixel.G, pixel.B));
            }
        }

        return output;
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

    public static System.Drawing.Drawing2D.GraphicsPath CreateRoundedRectangle(Rectangle bounds, int radius)
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
