using System;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace SteamControllerBridge.GameBarWidget
{
    public sealed partial class WidgetPage : Page
    {
        private static readonly string[] Outputs = new[]
        {
            "Disabled",
            "A",
            "B",
            "X",
            "Y",
            "LeftShoulder",
            "RightShoulder",
            "LeftThumb",
            "RightThumb",
            "Back",
            "Start",
            "Guide",
            "DPadUp",
            "DPadDown",
            "DPadLeft",
            "DPadRight"
        };

        private readonly LocalControlClient _client = new LocalControlClient();
        private readonly DispatcherTimer _pollTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        private bool _updatingUi;
        private int _rumblePercent = 50;

        public WidgetPage()
        {
            InitializeComponent();
            PopulateCombo(L4Combo);
            PopulateCombo(L5Combo);
            PopulateCombo(R4Combo);
            PopulateCombo(R5Combo);

            _pollTimer.Tick += async (sender, args) => await RefreshStatusAsync();
            Loaded += async (sender, args) =>
            {
                _pollTimer.Start();
                await RefreshStatusAsync();
            };
            Unloaded += (sender, args) => _pollTimer.Stop();
        }

        private static void PopulateCombo(ComboBox combo)
        {
            foreach (var output in Outputs)
            {
                combo.Items.Add(output);
            }
        }

        private async void ToggleButton_Click(object sender, RoutedEventArgs e)
        {
            await SendAndRefreshAsync(() => _client.ToggleAsync());
        }

        private async void RumbleDownButton_Click(object sender, RoutedEventArgs e)
        {
            if (_updatingUi)
            {
                return;
            }

            await SetRumblePercentAsync(_rumblePercent - 5);
        }

        private async void RumbleUpButton_Click(object sender, RoutedEventArgs e)
        {
            if (_updatingUi)
            {
                return;
            }

            await SetRumblePercentAsync(_rumblePercent + 5);
        }

        private async void PaddleCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var combo = sender as ComboBox;
            var output = combo == null ? null : combo.SelectedItem as string;
            if (_updatingUi || combo == null || output == null)
            {
                return;
            }

            var paddle = combo == L4Combo ? "L4" :
                combo == L5Combo ? "L5" :
                combo == R4Combo ? "R4" : "R5";
            await SendAndRefreshAsync(() => _client.SetPaddleAsync(paddle, output));
        }

        private async Task RefreshStatusAsync()
        {
            await SendAndRefreshAsync(() => _client.GetStatusAsync(), false);
        }

        private async Task SendAndRefreshAsync(Func<Task<LocalControlResponse>> action, bool showErrors = true)
        {
            try
            {
                var response = await action();
                if (!response.Ok || response.Status == null)
                {
                    SetNoLink(showErrors ? response.Error : "Bridge not available");
                    return;
                }

                ApplyStatus(response.Status);
            }
            catch (Exception ex)
            {
                SetNoLink(showErrors ? ex.Message : "Bridge not available");
            }
        }

        private void ApplyStatus(LocalControlStatus status)
        {
            _updatingUi = true;
            try
            {
                StatusDot.Fill = new SolidColorBrush(status.IsEnabled ? Color.FromArgb(255, 91, 244, 183) : Color.FromArgb(255, 132, 151, 172));
                StatusText.Text = status.IsEnabled ? "Bridge On" : status.IsWorking ? "Working" : "Bridge Off";
                MessageText.Text = status.Message;
                SetToggleImage(status.IsEnabled || status.IsWorking);
                SetRumbleUi(SnapPercent(status.RumbleIntensityPercent));
                SetCombo(L4Combo, status.L4);
                SetCombo(L5Combo, status.L5);
                SetCombo(R4Combo, status.R4);
                SetCombo(R5Combo, status.R5);
            }
            finally
            {
                _updatingUi = false;
            }
        }

        private void SetNoLink(string message)
        {
            _updatingUi = true;
            try
            {
                StatusDot.Fill = new SolidColorBrush(Color.FromArgb(255, 255, 91, 91));
                StatusText.Text = "No Link";
                MessageText.Text = message;
                SetToggleImage(false);
            }
            finally
            {
                _updatingUi = false;
            }
        }

        private void SetToggleImage(bool isOn)
        {
            var fileName = isOn ? "ControllerON.png" : "ControllerOFF.png";
            ToggleImage.Source = new BitmapImage(new Uri("ms-appx:///Assets/" + fileName));
        }

        private async Task SetRumblePercentAsync(int value)
        {
            value = SnapPercent(value);
            SetRumbleUi(value);
            await SendAndRefreshAsync(() => _client.SetRumbleIntensityAsync(value));
        }

        private void SetRumbleUi(int value)
        {
            _rumblePercent = SnapPercent(value);
            RumbleBar.Value = _rumblePercent;
            RumbleValueText.Text = _rumblePercent + "%";
        }

        private static void SetCombo(ComboBox combo, string value)
        {
            var index = combo.Items.IndexOf(value);
            combo.SelectedIndex = index >= 0 ? index : 0;
        }

        private static int SnapPercent(int value)
        {
            return (int)Math.Round(Math.Max(0, Math.Min(100, value)) / 5.0) * 5;
        }
    }
}
