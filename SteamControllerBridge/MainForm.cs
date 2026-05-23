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
    private readonly ComboBox _l4Combo = new();
    private readonly ComboBox _l5Combo = new();
    private readonly ComboBox _r4Combo = new();
    private readonly ComboBox _r5Combo = new();
    private readonly ComboBox _trackpadSourceCombo = new();
    private readonly ComboBox _gyroActivationCombo = new();
    private readonly CheckBox _trackpadMouseCheck = new();
    private readonly CheckBox _trackpadClickCheck = new();
    private readonly CheckBox _gyroMouseCheck = new();
    private readonly CheckBox _rumbleCheck = new();
    private readonly CheckBox _darkModeCheck = new();
    private readonly Button _openLogButton = new();
    private readonly Button _copyDiagnosticsButton = new();
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
        Size = new Size(460, 340);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
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

        BuildUi();
        LoadOptionsIntoUi();

        _bridge.StatusChanged += (_, status) => OnUi(() => ApplyStatus(status));
        _bridge.LogWritten += (_, line) => OnUi(() => AppendLog(line));
        FormClosing += (_, _) =>
        {
            _bridge.Dispose();
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

        _darkModeCheck.Text = "Dark mode";
        _darkModeCheck.AutoSize = true;
        _darkModeCheck.Margin = new Padding(0, 0, 0, 0);
        _darkModeCheck.CheckedChanged += (_, _) =>
        {
            SaveOptionsFromUi();
            ApplyTheme();
        };

        _quickOptionsPanel.Controls.Add(_rumbleCheck);
        _quickOptionsPanel.Controls.Add(_darkModeCheck);
    }

    private void BuildAdvancedPanel()
    {
        _advancedPanel.Dock = DockStyle.Fill;
        _advancedPanel.Visible = false;

        var advancedLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1
        };
        advancedLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        advancedLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 106));

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 11,
            ColumnCount = 2
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var row = 0; row < layout.RowCount; row++)
        {
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }

        ConfigureCombo(_l4Combo, Enum.GetValues<PaddleMapping>());
        ConfigureCombo(_l5Combo, Enum.GetValues<PaddleMapping>());
        ConfigureCombo(_r4Combo, Enum.GetValues<PaddleMapping>());
        ConfigureCombo(_r5Combo, Enum.GetValues<PaddleMapping>());
        ConfigureCombo(_trackpadSourceCombo, Enum.GetValues<TrackpadMouseSource>());
        ConfigureCombo(_gyroActivationCombo, Enum.GetValues<GyroMouseActivation>());

        AddOptionRow(layout, 0, "L4", _l4Combo);
        AddOptionRow(layout, 1, "L5", _l5Combo);
        AddOptionRow(layout, 2, "R4", _r4Combo);
        AddOptionRow(layout, 3, "R5", _r5Combo);
        AddOptionRow(layout, 4, "Mouse pad", _trackpadSourceCombo);

        _trackpadMouseCheck.Text = "Use trackpad as mouse";
        _trackpadMouseCheck.AutoSize = true;
        _trackpadMouseCheck.CheckedChanged += (_, _) => SaveOptionsFromUi();
        layout.Controls.Add(_trackpadMouseCheck, 1, 5);

        _trackpadClickCheck.Text = "Trackpad click is left click";
        _trackpadClickCheck.AutoSize = true;
        _trackpadClickCheck.CheckedChanged += (_, _) => SaveOptionsFromUi();
        layout.Controls.Add(_trackpadClickCheck, 1, 6);

        _gyroMouseCheck.Text = "Use gyro as mouse";
        _gyroMouseCheck.AutoSize = true;
        _gyroMouseCheck.CheckedChanged += (_, _) => SaveOptionsFromUi();
        layout.Controls.Add(_gyroMouseCheck, 1, 7);

        AddOptionRow(layout, 8, "Gyro activation", _gyroActivationCombo);

        _openLogButton.Text = "Open log";
        _openLogButton.AutoSize = true;
        _openLogButton.Click += (_, _) => OpenLog();
        layout.Controls.Add(_openLogButton, 1, 9);

        _copyDiagnosticsButton.Text = "Copy diagnostics";
        _copyDiagnosticsButton.AutoSize = true;
        _copyDiagnosticsButton.Click += (_, _) => CopyDiagnostics();
        layout.Controls.Add(_copyDiagnosticsButton, 1, 10);

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
        _l4Combo.SelectedItem = _bridge.Options.L4;
        _l5Combo.SelectedItem = _bridge.Options.L5;
        _r4Combo.SelectedItem = _bridge.Options.R4;
        _r5Combo.SelectedItem = _bridge.Options.R5;
        _trackpadSourceCombo.SelectedItem = _bridge.Options.TrackpadMouseSource;
        _gyroActivationCombo.SelectedItem = _bridge.Options.GyroMouseActivation;
        _trackpadMouseCheck.Checked = _bridge.Options.TrackpadMouseEnabled;
        _trackpadClickCheck.Checked = _bridge.Options.TrackpadClickEnabled;
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

        _bridge.Options.L4 = (PaddleMapping)(_l4Combo.SelectedItem ?? _bridge.Options.L4);
        _bridge.Options.L5 = (PaddleMapping)(_l5Combo.SelectedItem ?? _bridge.Options.L5);
        _bridge.Options.R4 = (PaddleMapping)(_r4Combo.SelectedItem ?? _bridge.Options.R4);
        _bridge.Options.R5 = (PaddleMapping)(_r5Combo.SelectedItem ?? _bridge.Options.R5);
        _bridge.Options.TrackpadMouseSource = (TrackpadMouseSource)(_trackpadSourceCombo.SelectedItem ?? _bridge.Options.TrackpadMouseSource);
        _bridge.Options.GyroMouseActivation = (GyroMouseActivation)(_gyroActivationCombo.SelectedItem ?? _bridge.Options.GyroMouseActivation);
        _bridge.Options.TrackpadMouseEnabled = _trackpadMouseCheck.Checked;
        _bridge.Options.TrackpadClickEnabled = _trackpadClickCheck.Checked;
        _bridge.Options.GyroMouseEnabled = _gyroMouseCheck.Checked;
        _bridge.Options.RumbleEnabled = _rumbleCheck.Checked;
        _bridge.Options.DarkModeEnabled = _darkModeCheck.Checked;
        _bridge.SaveOptions();
        SyncTrayOptions();
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
        Height = _advancedVisible ? 610 : 340;
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
