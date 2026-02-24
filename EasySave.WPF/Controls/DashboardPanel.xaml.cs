using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using EasySave.WPF.Helpers;

namespace EasySave.WPF.Controls
{
    // ===== DASHBOARD PANEL =====
    // Four summary cards: total jobs, system status, log format, and encryption state.
    public partial class DashboardPanel : UserControl
    {
        private TextBlock? _totalLabel, _totalValue;
        private TextBlock? _statusLabel, _statusValue;
        private TextBlock? _logLabel, _logValue;
        private TextBlock? _encLabel, _encValue;
        private Ellipse? _statusDot, _encDot;

        public DashboardPanel()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            BuildCards();
        }

        private void BuildCards()
        {
            if (CardsPanel.Children.Count > 0) return;

            var shadow = new DropShadowEffect { BlurRadius = 12, ShadowDepth = 2, Opacity = 0.06, Color = ThemeColorsHelper.GetColorValue(ThemeColorsHelper.TextPrimary) };
            Border MakeCard(UIElement content) => new()
            {
                Background = ThemeColorsHelper.GetBrush(ThemeColorsHelper.BgCard),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(24, 20, 24, 20),
                Margin = new Thickness(0, 0, 16, 16),
                MinWidth = 200,
                Effect = shadow,
                Child = content
            };

            var sp1 = new StackPanel();
            _totalLabel = new TextBlock { Text = "Total Jobs", Foreground = ThemeColorsHelper.GetBrush(ThemeColorsHelper.TextMuted), FontSize = 11, FontWeight = FontWeights.SemiBold };
            _totalValue = new TextBlock { Text = "0", FontSize = 32, FontWeight = FontWeights.Bold, Foreground = ThemeColorsHelper.GetBrush(ThemeColorsHelper.TextPrimary), Margin = new Thickness(0, 4, 0, 0) };
            sp1.Children.Add(_totalLabel!);
            sp1.Children.Add(_totalValue!);
            CardsPanel.Children.Add(MakeCard(sp1));

            var sp2 = new StackPanel();
            _statusLabel = new TextBlock { Text = "System Status", Foreground = ThemeColorsHelper.GetBrush(ThemeColorsHelper.TextMuted), FontSize = 11, FontWeight = FontWeights.SemiBold };
            var statusRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) };
            _statusDot = new Ellipse { Width = 10, Height = 10, Fill = ThemeColorsHelper.GetBrush(ThemeColorsHelper.StatusSuccess), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
            _statusValue = new TextBlock { Text = "Ready", FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = ThemeColorsHelper.GetBrush(ThemeColorsHelper.TextPrimary) };
            statusRow.Children.Add(_statusDot!);
            statusRow.Children.Add(_statusValue!);
            sp2.Children.Add(_statusLabel!);
            sp2.Children.Add(statusRow);
            CardsPanel.Children.Add(MakeCard(sp2));

            var sp3 = new StackPanel();
            _logLabel = new TextBlock { Text = "Log Format", Foreground = ThemeColorsHelper.GetBrush(ThemeColorsHelper.TextMuted), FontSize = 11, FontWeight = FontWeights.SemiBold };
            _logValue = new TextBlock { Text = "JSON", FontSize = 24, FontWeight = FontWeights.Bold, Foreground = ThemeColorsHelper.GetBrush(ThemeColorsHelper.AccentPrimary), Margin = new Thickness(0, 4, 0, 0) };
            sp3.Children.Add(_logLabel!);
            sp3.Children.Add(_logValue!);
            CardsPanel.Children.Add(MakeCard(sp3));

            var sp4 = new StackPanel();
            _encLabel = new TextBlock { Text = "Encryption", Foreground = ThemeColorsHelper.GetBrush(ThemeColorsHelper.TextMuted), FontSize = 11, FontWeight = FontWeights.SemiBold };
            var encRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) };
            _encDot = new Ellipse { Width = 10, Height = 10, Fill = ThemeColorsHelper.GetBrush(ThemeColorsHelper.StatusSuccess), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
            _encValue = new TextBlock { Text = "Active", FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = ThemeColorsHelper.GetBrush(ThemeColorsHelper.StatusSuccess) };
            encRow.Children.Add(_encDot!);
            encRow.Children.Add(_encValue!);
            sp4.Children.Add(_encLabel!);
            sp4.Children.Add(encRow);
            CardsPanel.Children.Add(MakeCard(sp4));
        }

        // ===== PUBLIC API =====
        public void SetTitle(string title) => TxtTitle.Text = title;

        public void SetLabels(string totalJobs, string systemStatus, string logFormat, string encryption)
        {
            BuildCards();
            if (_totalLabel != null) _totalLabel.Text = totalJobs;
            if (_statusLabel != null) _statusLabel.Text = systemStatus;
            if (_logLabel != null) _logLabel.Text = logFormat;
            if (_encLabel != null) _encLabel.Text = encryption;
        }

        // Status and encryption cards use theme colors for success, danger, and muted states.
        public void UpdateDashboard(int totalJobs, string logFormat, bool isBlocked, string statusText, bool isEncryptionActive, string encActiveText, string encInactiveText)
        {
            BuildCards();
            if (_totalValue != null) _totalValue.Text = totalJobs.ToString();
            if (_logValue != null) _logValue.Text = logFormat.ToUpper();

            var sd = ThemeColorsHelper.GetBrush(ThemeColorsHelper.StatusDanger);
            var ss = ThemeColorsHelper.GetBrush(ThemeColorsHelper.StatusSuccess);
            var tm = ThemeColorsHelper.GetBrush(ThemeColorsHelper.TextMuted);

            if (isBlocked)
            {
                if (_statusDot != null) _statusDot.Fill = sd;
                if (_statusValue != null) { _statusValue.Text = statusText; _statusValue.Foreground = sd; }
            }
            else
            {
                if (_statusDot != null) _statusDot.Fill = ss;
                if (_statusValue != null) { _statusValue.Text = statusText; _statusValue.Foreground = ss; }
            }

            if (isEncryptionActive)
            {
                if (_encDot != null) _encDot.Fill = ss;
                if (_encValue != null) { _encValue.Text = encActiveText; _encValue.Foreground = ss; }
            }
            else
            {
                if (_encDot != null) _encDot.Fill = tm;
                if (_encValue != null) { _encValue.Text = encInactiveText; _encValue.Foreground = tm; }
            }
        }
    }
}
