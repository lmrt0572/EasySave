using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using EasySave.Core.Models;
using EasySave.Core.Models.Enums;
using EasySave.WPF.Helpers;

namespace EasySave.WPF.Controls
{
    // ===== PROGRESS CARD =====
    // One card per running job: name, status badge, progress bar, current file, and pause/stop buttons.
    public partial class ProgressCardControl : UserControl
    {
        private JobProgressInfo? _info;
        private Button? _btnPause;
        private Button? _btnStop;
        private string _labelRunning = "Running", _labelPaused = "Paused", _labelStopped = "Stopped", _labelCompleted = "Completed";

        public ProgressCardControl()
        {
            InitializeComponent();
        }

        public void Bind(JobProgressInfo info, Action<string> onPauseResume, Action<string> onStop)
        {
            _info = info;
            TxtJobName.Text = info.JobName;
            CardBorder.Tag = info.JobName;

            var accent = ThemeColorsHelper.GetBrush(ThemeColorsHelper.AccentPrimary);
            _btnPause = MkBtn(info.IsPaused ? "\u25B6" : "\u2502\u2502", accent, "Pause");
            _btnPause.Tag = info.JobName;
            _btnPause.Click += (s, e) =>
            {
                e.Handled = true;
                onPauseResume(info.JobName);
            };
            ControlsPanel.Children.Add(_btnPause);

            _btnStop = MkBtn("\u25A0", accent, "Stop");
            _btnStop.Tag = info.JobName;
            _btnStop.Click += (s, e) =>
            {
                e.Handled = true;
                onStop(info.JobName);
            };
            ControlsPanel.Children.Add(_btnStop);

            ProgressFill.Background = new LinearGradientBrush(
                ThemeColorsHelper.GetColorValue(ThemeColorsHelper.AccentPrimary),
                ThemeColorsHelper.GetColorValue(ThemeColorsHelper.AccentLight),
                0);

            info.PropertyChanged += (s, e) => Dispatcher.Invoke(() => UpdateFromInfo());
            UpdateFromInfo();
        }

        // Updates all visible elements from the bound progress info; progress bar uses star-based column widths.
        private void UpdateFromInfo()
        {
            if (_info == null) return;

            double pct = Math.Max(0, Math.Min(100, _info.Progression));
            ProgressInner.ColumnDefinitions[0].Width = new GridLength(pct, GridUnitType.Star);
            ProgressInner.ColumnDefinitions[1].Width = new GridLength(100 - pct, GridUnitType.Star);
            TxtPercent.Text = $"{_info.Progression}%";
            TxtCurrentFile.Text = _info.CurrentFile;
            TxtFilesCount.Text = $"{_info.FilesDone} / {_info.TotalFiles}";

            var ss = ThemeColorsHelper.GetBrush(ThemeColorsHelper.StatusSuccess);
            var sd = ThemeColorsHelper.GetBrush(ThemeColorsHelper.StatusDanger);
            var sw = ThemeColorsHelper.GetBrush(ThemeColorsHelper.StatusWarning);

            switch (_info.Status)
            {
                case BackupStatus.Running:
                    StatusBadge.Background = new SolidColorBrush(Color.FromArgb(0x20, ss.Color.R, ss.Color.G, ss.Color.B));
                    TxtStatus.Text = _labelRunning;
                    TxtStatus.Foreground = ss;
                    break;
                case BackupStatus.Paused:
                    StatusBadge.Background = new SolidColorBrush(Color.FromArgb(0x20, sw.Color.R, sw.Color.G, sw.Color.B));
                    TxtStatus.Text = _labelPaused;
                    TxtStatus.Foreground = sw;
                    break;
                case BackupStatus.Stopped:
                    StatusBadge.Background = new SolidColorBrush(Color.FromArgb(0x20, sd.Color.R, sd.Color.G, sd.Color.B));
                    TxtStatus.Text = _labelStopped;
                    TxtStatus.Foreground = sd;
                    break;
                case BackupStatus.Completed:
                    StatusBadge.Background = new SolidColorBrush(Color.FromArgb(0x20, ss.Color.R, ss.Color.G, ss.Color.B));
                    TxtStatus.Text = _labelCompleted;
                    TxtStatus.Foreground = ss;
                    break;
            }

            if (_btnPause != null)
            {
                _btnPause.Content = _info.IsPaused ? "\u25B6" : "\u2502\u2502";
                _btnPause.ToolTip = _info.IsPaused ? "Resume" : "Pause";
            }
        }

        public void SetActionButtonsEnabled(bool enabled)
        {
            if (_btnPause != null) _btnPause.IsEnabled = enabled;
            if (_btnStop != null) _btnStop.IsEnabled = enabled;
        }

        // ===== LABELS AND THEME =====
        public void SetStatusLabels(string running, string paused, string stopped, string completed)
        {
            _labelRunning = running;
            _labelPaused = paused;
            _labelStopped = stopped;
            _labelCompleted = completed;
            UpdateFromInfo();
        }

        public void RefreshTheme()
        {
            CardBorder.Background = ThemeColorsHelper.GetBrush(ThemeColorsHelper.BgCard);
            ProgressFill.Background = new LinearGradientBrush(
                ThemeColorsHelper.GetColorValue(ThemeColorsHelper.AccentPrimary),
                ThemeColorsHelper.GetColorValue(ThemeColorsHelper.AccentLight),
                0);
            TxtPercent.Foreground = ThemeColorsHelper.GetBrush(ThemeColorsHelper.AccentPrimary);
            UpdateFromInfo();
        }

        private static Button MkBtn(string icon, SolidColorBrush foreground, string tooltip) => new()
        {
            Content = icon,
            Style = (Style)Application.Current.FindResource("BtnIcon"),
            Foreground = foreground,
            FontSize = 12,
            Margin = new Thickness(2, 0, 2, 0),
            ToolTip = tooltip
        };
    }
}
