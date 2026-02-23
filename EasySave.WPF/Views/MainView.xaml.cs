using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using EasySave.Core.Models;
using EasySave.Core.Models.Enums;
using EasySave.Core.ViewModels;
using Microsoft.Win32;
using Lang = EasySave.Core.Models.Enums.Language;

namespace EasySave.WPF.Views
{
    public partial class MainView : Window
    {
        private readonly WpfViewModel _viewModel;
        private readonly LanguageManager _lang;
        private string _currentPage = "Jobs";
        private int _currentSettingsTab;
        private BackupJob? _editingJob;
        private int _currentThemeIndex;
        private bool _isKeyVisible = false;

        private static readonly (string File, string Name, string Bg, string Sidebar, string Accent, string Border, string Text)[] Themes =
        {
            ("Styles/Themes/Theme_CaramelProfond.xaml", "Caramel Profond", "#DFC4A8", "#3E2415", "#B5651D", "#C4A07A", "#3E2415"),
            ("Styles/Themes/Theme_ModeNuit.xaml",       "Mode Nuit",       "#1E1E2E", "#14101E", "#C99B6D", "#3E3E52", "#E0D8CC"),
        };

        private readonly List<Button> _playButtons = new();
        // V3 - Track progress cards by job name
        private readonly Dictionary<string, Border> _progressCards = new();

        private TextBlock _dashTotalLabel = null!, _dashTotalValue = null!;
        private TextBlock _dashStatusLabel = null!, _dashStatusValue = null!;
        private TextBlock _dashLogLabel = null!, _dashLogValue = null!;
        private TextBlock _dashEncLabel = null!, _dashEncValue = null!;
        private Ellipse _dashStatusDot = null!, _dashEncDot = null!;

        public MainView()
        {
            InitializeComponent();
            _lang = new LanguageManager();
            _viewModel = new WpfViewModel(_lang);

            _viewModel.PropertyChanged += (s, e) => Dispatcher.Invoke(() =>
            {
                switch (e.PropertyName)
                {
                    case nameof(WpfViewModel.StatusMessage): TxtStatus.Text = _viewModel.StatusMessage; break;
                    case nameof(WpfViewModel.IsBusinessSoftwareDetected):
                        UpdateWarning(); UpdateDashboard();
                        break;
                    case nameof(WpfViewModel.CanExecute): BtnExecuteAll.IsEnabled = _viewModel.CanExecute; SyncPlayButtons(); break;
                    case nameof(WpfViewModel.IsExecuting): SyncPlayButtons(); break;
                    case nameof(WpfViewModel.IsNotificationVisible):
                        if (_viewModel.IsNotificationVisible) ShowNotificationToast(); else HideNotificationToast(); break;
                    case nameof(WpfViewModel.NotificationMessage): NotifText.Text = _viewModel.NotificationMessage; break;
                    case nameof(WpfViewModel.NotificationType): UpdateNotificationStyle(); break;
                    case nameof(WpfViewModel.IsEncryptionActive): UpdateDashboard(); break;
                }
            });

            // V3 - Subscribe to RunningJobsProgress collection changes
            _viewModel.RunningJobsProgress.CollectionChanged += (s, e) => Dispatcher.Invoke(() =>
            {
                RefreshProgressCards();
                UpdateJobCardsRunningState();
                ActiveJobsEncart.Visibility = _viewModel.RunningJobsProgress.Count > 0
                    ? Visibility.Visible
                    : Visibility.Collapsed;
                TxtActiveJobsCount.Text = $"{_viewModel.RunningJobsProgress.Count} active job(s)";
            });

            TxtBusinessSoftware.Text = _viewModel.BusinessSoftwareName;
            TxtEncryptionKey.Text = _viewModel.EncryptionKey;
            PwdEncryptionKey.Password = _viewModel.EncryptionKey;
            TxtEncryptionKey.Visibility = Visibility.Collapsed;
            PwdEncryptionKey.Visibility = Visibility.Visible;
            BtnToggleKeyVisibility.Content = "\U0001F512";
            TxtEncryptionExtensions.Text = _viewModel.EncryptionExtensionsText;
            TxtLargeFileThreshold.Text = _viewModel.LargeFileThresholdKo.ToString();
            TxtPriorityExtensions.Text = _viewModel.PriorityExtensionsText;

            BuildThemeSwatches(); BuildDashboardCards();
            SetActiveNav("Jobs"); SetActiveSettingsTab(0);
            UpdateLogFormatButtons(); UpdateLanguageButtons(); UpdateThemeSelection();
            ApplyTranslations(); RefreshJobList(); UpdateWarning(); UpdateDashboard();
            BtnExecuteAll.IsEnabled = _viewModel.CanExecute;

            foreach (ComboBoxItem item in CmbLogMode.Items)
            {
                if (item.Tag?.ToString() == _viewModel.LogMode)
                {
                    CmbLogMode.SelectedItem = item;
                    break;
                }
            }
        }

        // ===== FOLDER BROWSER =====
        private void BtnBrowseSource_Click(object sender, RoutedEventArgs e)
        {
            var path = BrowseForFolder();
            if (!string.IsNullOrEmpty(path)) TxtSource.Text = path;
        }

        private void BtnBrowseTarget_Click(object sender, RoutedEventArgs e)
        {
            var path = BrowseForFolder();
            if (!string.IsNullOrEmpty(path)) TxtTarget.Text = path;
        }

        private string? BrowseForFolder()
        {
            var dialog = new OpenFolderDialog { Title = "Select folder", Multiselect = false };
            if (dialog.ShowDialog(this) == true) return dialog.FolderName;
            return null;
        }

        // ===== PASSWORD TOGGLE =====
        private void BtnToggleKeyVisibility_Click(object sender, RoutedEventArgs e)
        {
            _isKeyVisible = !_isKeyVisible;
            if (_isKeyVisible)
            {
                TxtEncryptionKey.Text = PwdEncryptionKey.Password;
                TxtEncryptionKey.Visibility = Visibility.Visible;
                PwdEncryptionKey.Visibility = Visibility.Collapsed;
                BtnToggleKeyVisibility.Content = "\U0001F441";
            }
            else
            {
                PwdEncryptionKey.Password = TxtEncryptionKey.Text;
                TxtEncryptionKey.Visibility = Visibility.Collapsed;
                PwdEncryptionKey.Visibility = Visibility.Visible;
                BtnToggleKeyVisibility.Content = "\U0001F512";
            }
        }

        private void PwdEncryptionKey_LostFocus(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.EncryptionKey = PwdEncryptionKey.Password.Trim();
                TxtEncryptionKey.Text = PwdEncryptionKey.Password;
            }
        }

        // ===== NAVIGATION =====
        private void BtnNavJobs_Click(object s, RoutedEventArgs e) => SetActiveNav("Jobs");
        private void BtnNavDashboard_Click(object s, RoutedEventArgs e) { UpdateDashboard(); SetActiveNav("Dashboard"); }
        private void BtnNavSettings_Click(object s, RoutedEventArgs e) => SetActiveNav("Settings");

        private void SetActiveNav(string page)
        {
            _currentPage = page;
            JobsPage.Visibility = page == "Jobs" ? Visibility.Visible : Visibility.Collapsed;
            DashboardPage.Visibility = page == "Dashboard" ? Visibility.Visible : Visibility.Collapsed;
            SettingsPage.Visibility = page == "Settings" ? Visibility.Visible : Visibility.Collapsed;
            var (a, t, m) = (TC("AccentPrimary", "#a67847"), TC("TextOnAccent", "#F5E6D3"), TC("TextOnDarkMuted", "#B8A08A"));
            foreach (var (btn, p) in new[] { (BtnNavJobs, "Jobs"), (BtnNavDashboard, "Dashboard"), (BtnNavSettings, "Settings") })
            { btn.Background = p == page ? B(a) : Brushes.Transparent; btn.Foreground = B(p == page ? t : m); }
        }

        // ===== SETTINGS TABS =====
        private void BtnTabGeneral_Click(object s, RoutedEventArgs e) => SetActiveSettingsTab(0);
        private void BtnTabLogs_Click(object s, RoutedEventArgs e) => SetActiveSettingsTab(1);
        private void BtnTabLanguage_Click(object s, RoutedEventArgs e) => SetActiveSettingsTab(2);
        private void BtnTabTheme_Click(object s, RoutedEventArgs e) => SetActiveSettingsTab(3);

        private void SetActiveSettingsTab(int idx)
        {
            _currentSettingsTab = idx;
            TabGeneralContent.Visibility = idx == 0 ? Visibility.Visible : Visibility.Collapsed;
            TabLogsContent.Visibility = idx == 1 ? Visibility.Visible : Visibility.Collapsed;
            TabLanguageContent.Visibility = idx == 2 ? Visibility.Visible : Visibility.Collapsed;
            TabThemeContent.Visibility = idx == 3 ? Visibility.Visible : Visibility.Collapsed;
            var (a, t, m) = (TC("AccentPrimary", "#a67847"), TC("TextOnAccent", "#F5E6D3"), TC("TextMuted", "#9C8468"));
            foreach (var (btn, i) in new[] { (BtnTabGeneral, 0), (BtnTabLogs, 1), (BtnTabLanguage, 2), (BtnTabTheme, 3) })
            { btn.Background = i == idx ? B(a) : Brushes.Transparent; btn.Foreground = B(i == idx ? t : m); }
        }

        // ===== THEME SWITCHING =====
        private void ThemeSwatch_Click(object sender, MouseButtonEventArgs e)
        { if (sender is Border bd && bd.Tag is int idx) ApplyTheme(idx); }

        private void ApplyTheme(int idx)
        {
            if (idx < 0 || idx >= Themes.Length) return;
            _currentThemeIndex = idx;
            var dicts = Application.Current.Resources.MergedDictionaries;
            var theme = new ResourceDictionary { Source = new Uri(Themes[idx].File, UriKind.Relative) };
            if (dicts.Count > 0) dicts[0] = theme; else dicts.Insert(0, theme);
            UpdateThemeSelection(); SetActiveNav(_currentPage); SetActiveSettingsTab(_currentSettingsTab);
            UpdateLogFormatButtons(); UpdateLanguageButtons(); RefreshJobList();
            UpdateWarning(); UpdateDashboard();
            TxtCurrentTheme.Text = $"Active: {Themes[idx].Name}";
            _viewModel.ShowNotification($"Theme: {Themes[idx].Name}", "success");
        }

        private void BuildThemeSwatches()
        {
            for (int i = 0; i < Themes.Length; i++)
            {
                var t = Themes[i];
                var dots = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 6) };
                dots.Children.Add(new Ellipse { Width = 20, Height = 20, Fill = B(t.Bg), Stroke = B(t.Border), StrokeThickness = 1 });
                dots.Children.Add(new Ellipse { Width = 20, Height = 20, Fill = B(t.Sidebar), Margin = new Thickness(3, 0, 0, 0) });
                dots.Children.Add(new Ellipse { Width = 20, Height = 20, Fill = B(t.Accent), Margin = new Thickness(3, 0, 0, 0) });
                var label = new TextBlock { Text = t.Name, FontSize = 10, FontWeight = FontWeights.SemiBold, Foreground = B(t.Text), HorizontalAlignment = HorizontalAlignment.Center };
                var inner = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
                inner.Children.Add(dots); inner.Children.Add(label);
                var swatch = new Border
                {
                    Tag = i,
                    Margin = new Thickness(0, 0, 10, 14),
                    CornerRadius = new CornerRadius(12),
                    Padding = new Thickness(12, 10, 12, 10),
                    Cursor = Cursors.Hand,
                    Width = 120,
                    Background = B(t.Bg),
                    BorderThickness = new Thickness(2),
                    BorderBrush = Brushes.Transparent,
                    Child = inner
                };
                swatch.MouseLeftButtonDown += ThemeSwatch_Click;
                ThemeSwatchPanel.Children.Add(swatch);
            }
        }

        private void UpdateThemeSelection()
        {
            var accent = TC("AccentPrimary", "#a67847");
            for (int i = 0; i < ThemeSwatchPanel.Children.Count; i++)
                if (ThemeSwatchPanel.Children[i] is Border bd)
                    bd.BorderBrush = i == _currentThemeIndex ? B(accent) : Brushes.Transparent;
            TxtCurrentTheme.Text = $"Active: {Themes[_currentThemeIndex].Name}";
        }

        // ===== DASHBOARD =====
        private void BuildDashboardCards()
        {
            var shadow = new Func<DropShadowEffect>(() => new DropShadowEffect { BlurRadius = 12, ShadowDepth = 2, Opacity = 0.06, Color = Cl("#553f2a") });
            Border MakeCard(UIElement content) => new()
            {
                Background = B(TC("BgCard", "#F2E0CE")),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(24, 20, 24, 20),
                Margin = new Thickness(0, 0, 16, 16),
                MinWidth = 200,
                Effect = shadow(),
                Child = content
            };

            var sp1 = new StackPanel();
            _dashTotalLabel = new TextBlock { Text = "Total Jobs", Foreground = B(TC("TextMuted", "#9C8468")), FontSize = 11, FontWeight = FontWeights.SemiBold };
            _dashTotalValue = new TextBlock { Text = "0", FontSize = 32, FontWeight = FontWeights.Bold, Foreground = B(TC("TextPrimary", "#553f2a")), Margin = new Thickness(0, 4, 0, 0) };
            sp1.Children.Add(_dashTotalLabel); sp1.Children.Add(_dashTotalValue);
            DashboardCards.Children.Add(MakeCard(sp1));

            var sp2 = new StackPanel();
            _dashStatusLabel = new TextBlock { Text = "System Status", Foreground = B(TC("TextMuted", "#9C8468")), FontSize = 11, FontWeight = FontWeights.SemiBold };
            var statusRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) };
            _dashStatusDot = new Ellipse { Width = 10, Height = 10, Fill = B("#5A7247"), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
            _dashStatusValue = new TextBlock { Text = "Ready", FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = B(TC("TextPrimary", "#553f2a")) };
            statusRow.Children.Add(_dashStatusDot); statusRow.Children.Add(_dashStatusValue);
            sp2.Children.Add(_dashStatusLabel); sp2.Children.Add(statusRow);
            DashboardCards.Children.Add(MakeCard(sp2));

            var sp3 = new StackPanel();
            _dashLogLabel = new TextBlock { Text = "Log Format", Foreground = B(TC("TextMuted", "#9C8468")), FontSize = 11, FontWeight = FontWeights.SemiBold };
            _dashLogValue = new TextBlock { Text = "JSON", FontSize = 24, FontWeight = FontWeights.Bold, Foreground = B(TC("AccentPrimary", "#a67847")), Margin = new Thickness(0, 4, 0, 0) };
            sp3.Children.Add(_dashLogLabel); sp3.Children.Add(_dashLogValue);
            DashboardCards.Children.Add(MakeCard(sp3));

            var sp4 = new StackPanel();
            _dashEncLabel = new TextBlock { Text = "Encryption", Foreground = B(TC("TextMuted", "#9C8468")), FontSize = 11, FontWeight = FontWeights.SemiBold };
            var encRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) };
            _dashEncDot = new Ellipse { Width = 10, Height = 10, Fill = B("#5A7247"), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
            _dashEncValue = new TextBlock { Text = "Active", FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = B("#5A7247") };
            encRow.Children.Add(_dashEncDot); encRow.Children.Add(_dashEncValue);
            sp4.Children.Add(_dashEncLabel); sp4.Children.Add(encRow);
            DashboardCards.Children.Add(MakeCard(sp4));
        }

        // ===== V3 - GLOBAL CONTROL HANDLERS =====
        private void BtnPauseAll_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.PauseAllJobs();
        }

        private void BtnResumeAll_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.ResumeAllJobs();
        }

        private void BtnStopAll_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.StopAllJobs();
        }

        // ===== V3 - PER-JOB PROGRESS CARDS =====
        private void RefreshProgressCards()
        {
            // Remove cards for jobs no longer in RunningJobsProgress
            var activeNames = new HashSet<string>(_viewModel.RunningJobsProgress.Select(p => p.JobName));
            var toRemove = _progressCards.Keys.Where(k => !activeNames.Contains(k)).ToList();
            foreach (var name in toRemove)
            {
                if (_progressCards.TryGetValue(name, out var oldCard))
                    ProgressItemsControl.Items.Remove(oldCard);
                _progressCards.Remove(name);
            }

            // Add cards for new jobs
            foreach (var info in _viewModel.RunningJobsProgress)
            {
                if (!_progressCards.ContainsKey(info.JobName))
                {
                    var card = CreateProgressCard(info);
                    _progressCards[info.JobName] = card;
                    ProgressItemsControl.Items.Add(card);

                    // Subscribe to per-property updates
                    info.PropertyChanged += (s, e) => Dispatcher.Invoke(() => UpdateProgressCard(info));
                }
            }
        }

        private Border CreateProgressCard(JobProgressInfo info)
        {
            var (accent, bgCard, tp, ts, tm) = (
                TC("AccentPrimary", "#a67847"),
                TC("BgCard", "#F2E0CE"),
                TC("TextPrimary", "#553f2a"),
                TC("TextSecondary", "#7A6147"),
                TC("TextMuted", "#9C8468"));

            var card = new Border
            {
                Background = B(bgCard),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10, 5, 10, 5),
                Margin = new Thickness(0, 0, 0, 4),
                Tag = info.JobName,
                Effect = new DropShadowEffect { BlurRadius = 4, ShadowDepth = 1, Opacity = 0.03, Color = Cl("#553f2a") }
            };

            var stack = new StackPanel();

            // Row 1: Job name + status badge + per-job controls
            var topRow = new Grid();
            topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var nameBlock = new TextBlock { Text = info.JobName, FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = B(tp), VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(nameBlock, 0);
            topRow.Children.Add(nameBlock);

            var statusBadge = new Border { CornerRadius = new CornerRadius(4), Padding = new Thickness(5, 1, 5, 1), Margin = new Thickness(0, 0, 6, 0), VerticalAlignment = VerticalAlignment.Center };
            var statusText = new TextBlock { FontSize = 8, FontWeight = FontWeights.SemiBold };
            statusBadge.Child = statusText;
            Grid.SetColumn(statusBadge, 1);
            topRow.Children.Add(statusBadge);

            var controls = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            var btnPause = MkBtn("\u2502\u2502", accent, true); // ││ 2 thin short bars (discreet pause)
            btnPause.Tag = info.JobName;
            btnPause.ToolTip = "Pause";
            btnPause.Click += (s, e) =>
            {
                e.Handled = true;
                string jobName = (string)((Button)s).Tag;
                if (_viewModel.IsJobPaused(jobName)) _viewModel.ResumeJob(jobName);
                else _viewModel.PauseJob(jobName);
            };
            controls.Children.Add(btnPause);

            var btnStop = MkBtn("\u25A0", accent, true); // ■ full square, same style as resume
            btnStop.Tag = info.JobName;
            btnStop.ToolTip = "Stop";
            btnStop.Click += (s, e) =>
            {
                e.Handled = true;
                string jobName = (string)((Button)s).Tag;
                _viewModel.StopJob(jobName);
            };
            controls.Children.Add(btnStop);
            Grid.SetColumn(controls, 2);
            topRow.Children.Add(controls);
            stack.Children.Add(topRow);

            // Row 2: Progress bar + percentage (AccentPrimary = same as main buttons)
            var progressRow = new Grid { Margin = new Thickness(0, 2, 0, 0) };
            progressRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            progressRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var progressTrack = new Border { Background = B(TC("BorderLight", "#DBBFA0")), CornerRadius = new CornerRadius(2), Height = 3 };
            var innerGrid = new Grid();
            innerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0, GridUnitType.Star) });
            innerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100, GridUnitType.Star) });
            var progressFill = new Border
            {
                CornerRadius = new CornerRadius(2),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Background = new LinearGradientBrush(Cl(accent), Cl(TC("AccentLight", "#C99B6D")), 0)
            };
            Grid.SetColumn(progressFill, 0);
            innerGrid.Children.Add(progressFill);
            progressTrack.Child = innerGrid;
            Grid.SetColumn(progressTrack, 0);
            progressRow.Children.Add(progressTrack);

            var pctText = new TextBlock { Text = "0%", Foreground = B(accent), FontSize = 10, FontWeight = FontWeights.Bold, Margin = new Thickness(6, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(pctText, 1);
            progressRow.Children.Add(pctText);
            stack.Children.Add(progressRow);

            // Row 3: File name + count
            var fileRow = new Grid { Margin = new Thickness(0, 2, 0, 0) };
            fileRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            fileRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var fileText = new TextBlock { Foreground = B(tm), FontSize = 9, TextTrimming = TextTrimming.CharacterEllipsis };
            Grid.SetColumn(fileText, 0);
            fileRow.Children.Add(fileText);

            var filesCountText = new TextBlock { Foreground = B(ts), FontSize = 9, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(filesCountText, 1);
            fileRow.Children.Add(filesCountText);
            stack.Children.Add(fileRow);

            card.Child = stack;
            UpdateProgressCardVisuals(card, info);
            return card;
        }

        private void UpdateProgressCard(JobProgressInfo info)
        {
            if (!_progressCards.TryGetValue(info.JobName, out var card)) return;
            UpdateProgressCardVisuals(card, info);
        }

        private void UpdateProgressCardVisuals(Border card, JobProgressInfo info)
        {
            var (accent, ss, sd, sw, tm, ts) = (
                TC("AccentPrimary", "#a67847"),
                TC("StatusSuccess", "#5A7247"),
                TC("StatusDanger", "#9B4D4D"),
                TC("StatusWarning", "#B8860B"),
                TC("TextMuted", "#9C8468"),
                TC("TextSecondary", "#7A6147"));

            if (card.Child is not StackPanel stack) return;

            // Update status badge + pause button
            if (stack.Children[0] is Grid topRow && topRow.Children.Count >= 2)
            {
                if (topRow.Children[1] is Border badge && badge.Child is TextBlock statusTb)
                {
                    switch (info.Status)
                    {
                        case BackupStatus.Running:
                        case BackupStatus.Active:
                            badge.Background = new SolidColorBrush(Color.FromArgb(0x20, 0x5A, 0x72, 0x47));
                            statusTb.Text = _lang.GetText("progress_running"); statusTb.Foreground = B(ss);
                            break;
                        case BackupStatus.Paused:
                            badge.Background = new SolidColorBrush(Color.FromArgb(0x20, 0xB8, 0x86, 0x0B));
                            statusTb.Text = _lang.GetText("progress_paused"); statusTb.Foreground = B(sw);
                            break;
                        case BackupStatus.Stopped:
                            badge.Background = new SolidColorBrush(Color.FromArgb(0x20, 0x9B, 0x4D, 0x4D));
                            statusTb.Text = _lang.GetText("progress_stopped"); statusTb.Foreground = B(sd);
                            break;
                        case BackupStatus.Completed:
                            badge.Background = new SolidColorBrush(Color.FromArgb(0x20, 0x5A, 0x72, 0x47));
                            statusTb.Text = _lang.GetText("progress_completed"); statusTb.Foreground = B(ss);
                            break;
                    }
                }
                if (topRow.Children.Count >= 3 && topRow.Children[2] is StackPanel controls && controls.Children.Count > 0)
                {
                    if (controls.Children[0] is Button btnPause)
                    { btnPause.Content = info.IsPaused ? "\u25B6" : "\u2502\u2502"; btnPause.ToolTip = info.IsPaused ? "Resume" : "Pause"; }
                }
            }

            // Update progress bar — use star-ratio columns (no ActualWidth dependency)
            if (stack.Children.Count >= 2 && stack.Children[1] is Grid progressRow)
            {
                if (progressRow.Children[0] is Border track && track.Child is Grid innerGrid && innerGrid.ColumnDefinitions.Count == 2)
                {
                    double pct = Math.Max(0, Math.Min(100, info.Progression));
                    innerGrid.ColumnDefinitions[0].Width = new GridLength(pct, GridUnitType.Star);
                    innerGrid.ColumnDefinitions[1].Width = new GridLength(100 - pct, GridUnitType.Star);
                }
                if (progressRow.Children.Count >= 2 && progressRow.Children[1] is TextBlock pctTb)
                    pctTb.Text = $"{info.Progression}%";
            }

            // Update file info
            if (stack.Children.Count >= 3 && stack.Children[2] is Grid fileRow)
            {
                if (fileRow.Children[0] is TextBlock fileTb) fileTb.Text = info.CurrentFile;
                if (fileRow.Children.Count >= 2 && fileRow.Children[1] is TextBlock countTb) countTb.Text = $"{info.FilesDone} / {info.TotalFiles}";
            }
        }

        // ===== JOB LIST =====
        private void RefreshJobList()
        {
            JobListPanel.Children.Clear(); _playButtons.Clear();
            var jobs = _viewModel.Jobs;
            var (tm, tml) = (TC("TextMuted", "#9C8468"), TC("TextOnDarkMuted", "#B8A08A"));
            if (jobs.Count == 0)
            {
                var ep = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 60, 0, 0) };
                ep.Children.Add(new TextBlock { Text = _lang.GetText("jobs_empty"), Foreground = B(tm), FontSize = 15, FontWeight = FontWeights.SemiBold, HorizontalAlignment = HorizontalAlignment.Center });
                ep.Children.Add(new TextBlock { Text = _lang.GetText("jobs_empty_desc"), Foreground = B(tml), FontSize = 12, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 4, 0, 0) });
                JobListPanel.Children.Add(ep);
            }
            else
                for (int i = 0; i < jobs.Count; i++) JobListPanel.Children.Add(CreateJobCard(jobs[i]));
            TxtJobCount.Text = _lang.GetText("wpf_jobs_count", jobs.Count);
            UpdateDashboard();
        }

        private UIElement CreateJobCard(BackupJob job)
        {
            var (accent, bgCard, bgHover, bgRunning, tp, ts, tm) = (TC("AccentPrimary", "#a67847"), TC("BgCard", "#F2E0CE"), TC("BgInput", "#EBCFB8"),
                "#DCC4A8", TC("TextPrimary", "#553f2a"), TC("TextSecondary", "#7A6147"), TC("TextMuted", "#9C8468")); // bgRunning = marron foncé #a67847
            bool isFull = job.Type == BackupType.Full;
            string typeText = isFull ? _lang.GetText("type_full") : _lang.GetText("type_differential");
            Color bc = isFull ? Cl(accent) : Cl(TC("StatusSuccess", "#5A7247"));
            var card = new Border
            {
                Background = B(_viewModel.IsJobRunning(job.Name) ? bgRunning : bgCard),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(10, 6, 10, 6),
                Margin = new Thickness(0, 0, 0, 5),
                Cursor = Cursors.Hand,
                Effect = new DropShadowEffect { BlurRadius = 12, ShadowDepth = 2, Opacity = 0.06, Color = Cl(tp) }
            };
            var cg = new Grid();
            cg.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            cg.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var top = new Grid();
            top.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            top.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            top.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var name = new TextBlock { Text = job.Name, FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = B(tp), VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(name, 0); top.Children.Add(name);
            var badge = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(0x20, bc.R, bc.G, bc.B)),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6, 2, 6, 2),
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Child = new TextBlock { Text = typeText, FontSize = 9, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(bc) }
            };
            Grid.SetColumn(badge, 1); top.Children.Add(badge);
            var acts = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            // V3 - Play button: start job or toggle pause
            var r = MkBtn("\u25B6", accent, _viewModel.CanExecute);
            r.Click += (s, e) =>
            {
                e.Handled = true;
                if (_viewModel.IsJobRunning(job.Name))
                {
                    if (_viewModel.IsJobPaused(job.Name)) _viewModel.ResumeJob(job.Name);
                    else _viewModel.PauseJob(job.Name);
                }
                else ExecuteSingleJob(job);
            };
            _playButtons.Add(r); acts.Children.Add(r);
            var ed = MkBtn("\u270F", ts, true); ed.Click += (s, e) => { e.Handled = true; StartEditJob(job); }; acts.Children.Add(ed);
            var dl = MkBtn("\u2715", TC("StatusDanger", "#9B4D4D"), true); dl.Click += (s, e) => { e.Handled = true; DeleteJob(job); }; acts.Children.Add(dl);
            Grid.SetColumn(acts, 2); top.Children.Add(acts);
            Grid.SetRow(top, 0); cg.Children.Add(top);
            card.Tag = job.Name;
            var info = new Grid { Margin = new Thickness(0, 4, 0, 0) };
            info.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(55) });
            info.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            info.RowDefinitions.Add(new RowDefinition()); info.RowDefinitions.Add(new RowDefinition());
            AddCell(info, 0, 0, "Source", tm); AddCell(info, 0, 1, job.SourceDirectory, ts, true);
            AddCell(info, 1, 0, "Target", tm); AddCell(info, 1, 1, job.TargetDirectory, ts, true);
            Grid.SetRow(info, 1); cg.Children.Add(info);
            card.Child = cg;
            card.MouseEnter += (s, e) =>
            {
                if (!_viewModel.IsJobRunning(job.Name)) card.Background = B(bgHover);
            };
            card.MouseLeave += (s, e) =>
            {
                card.Background = B(_viewModel.IsJobRunning(job.Name) ? bgRunning : bgCard);
            };
            return card;
        }

        private void UpdateJobCardsRunningState()
        {
            var (bgCard, bgRunning) = (TC("BgCard", "#F2E0CE"), "#DCC4A8");
            foreach (var child in JobListPanel.Children)
            {
                if (child is Border card && card.Tag is string jobName)
                {
                    card.Background = B(_viewModel.IsJobRunning(jobName) ? bgRunning : bgCard);
                }
            }
        }

        private void AddCell(Grid g, int r, int c, string txt, string col, bool trim = false)
        {
            var tb = new TextBlock { Text = txt, FontSize = 10, Foreground = B(col), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 0, 0) };
            if (trim) tb.TextTrimming = TextTrimming.CharacterEllipsis;
            Grid.SetRow(tb, r); Grid.SetColumn(tb, c); g.Children.Add(tb);
        }

        private Button MkBtn(string icon, string color, bool enabled) => new()
        { Content = icon, Style = (Style)FindResource("BtnIcon"), Foreground = B(color), FontSize = 12, IsEnabled = enabled, Margin = new Thickness(2, 0, 2, 0) };

        // ===== JOB EDIT =====
        private void StartEditJob(BackupJob job)
        {
            _editingJob = job; TxtName.Text = job.Name; TxtSource.Text = job.SourceDirectory; TxtTarget.Text = job.TargetDirectory;
            CmbType.SelectedIndex = job.Type == BackupType.Differential ? 1 : 0;
            TxtCreateTitle.Text = _lang.GetText("jobs_edit_title"); BtnCreate.Content = _lang.GetText("wpf_btn_update"); SetActiveNav("Jobs");
        }

        private void ClearCreateForm()
        {
            TxtName.Clear(); TxtSource.Clear(); TxtTarget.Clear(); CmbType.SelectedIndex = 0; _editingJob = null;
            TxtCreateTitle.Text = _lang.GetText("jobs_create_title"); BtnCreate.Content = _lang.GetText("wpf_btn_add");
        }

        // ===== EVENT HANDLERS =====
        private void BtnCreate_Click(object sender, RoutedEventArgs e)
        {
            string name = TxtName.Text.Trim(), source = TxtSource.Text.Trim(), target = TxtTarget.Text.Trim();
            int typeInput = CmbType.SelectedIndex == 1 ? 2 : 1;
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(target))
            { _viewModel.ShowNotification(_lang.GetText("notif_fields_required"), "warning"); return; }
            if (_editingJob != null) { _viewModel.DeleteJob(_editingJob); _editingJob = null; }
            if (_viewModel.CreateJob(name, source, target, typeInput))
            { ClearCreateForm(); RefreshJobList(); _viewModel.ShowNotification(_lang.GetText("notif_job_created"), "success"); }
            else _viewModel.ShowNotification(_lang.GetText("error_invalid_choice"), "error");
        }

        private async void BtnExecuteAll_Click(object s, RoutedEventArgs e)
        { await Task.Run(() => _viewModel.ExecuteAllJobs()); Dispatcher.Invoke(RefreshJobList); }

        private async void ExecuteSingleJob(BackupJob job)
        { await Task.Run(() => _viewModel.ExecuteJob(job)); Dispatcher.Invoke(RefreshJobList); }

        private void DeleteJob(BackupJob job) { _viewModel.DeleteJob(job); RefreshJobList(); _viewModel.ShowNotification(_lang.GetText("notif_job_deleted"), "info"); }

        // ===== LANGUAGE =====
        private void BtnLangEn_Click(object s, RoutedEventArgs e) => SwitchLang(Lang.English);
        private void BtnLangFr_Click(object s, RoutedEventArgs e) => SwitchLang(Lang.French);
        private void BtnSettingsLangEn_Click(object s, RoutedEventArgs e) => SwitchLang(Lang.English);
        private void BtnSettingsLangFr_Click(object s, RoutedEventArgs e) => SwitchLang(Lang.French);

        private void SwitchLang(Lang lang)
        {
            _viewModel.SetLanguage(lang); ApplyTranslations(); RefreshJobList(); UpdateLanguageButtons(); UpdateDashboard();
            _viewModel.ShowNotification(_lang.GetText("notif_language_changed"), "success");
        }

        private void UpdateLanguageButtons()
        {
            bool en = _lang.GetCurrentLanguage() == Lang.English;
            var (td, tm, a, ta, tmu) = (TC("TextOnDark", "#E7D3C1"), TC("TextOnDarkMuted", "#B8A08A"), TC("AccentPrimary", "#a67847"), TC("TextOnAccent", "#F5E6D3"), TC("TextMuted", "#9C8468"));
            BtnLangEn.Foreground = B(en ? td : tm); BtnLangEn.FontWeight = en ? FontWeights.Bold : FontWeights.SemiBold;
            BtnLangFr.Foreground = B(!en ? td : tm); BtnLangFr.FontWeight = !en ? FontWeights.Bold : FontWeights.SemiBold;
            BtnSettingsLangEn.Background = B(en ? a : "Transparent"); BtnSettingsLangEn.Foreground = B(en ? ta : tmu);
            BtnSettingsLangFr.Background = B(!en ? a : "Transparent"); BtnSettingsLangFr.Foreground = B(!en ? ta : tmu);
        }

        // ===== LOG FORMAT =====
        private void BtnLogJson_Click(object s, RoutedEventArgs e) { _viewModel.LogFormat = "json"; UpdateLogFormatButtons(); UpdateDashboard(); _viewModel.ShowNotification(_lang.GetText("notif_settings_saved"), "success"); }
        private void BtnLogXml_Click(object s, RoutedEventArgs e) { _viewModel.LogFormat = "xml"; UpdateLogFormatButtons(); UpdateDashboard(); _viewModel.ShowNotification(_lang.GetText("notif_settings_saved"), "success"); }

        private void UpdateLogFormatButtons()
        {
            bool j = _viewModel.LogFormat == "json";
            var (a, ta, tm) = (TC("AccentPrimary", "#a67847"), TC("TextOnAccent", "#F5E6D3"), TC("TextMuted", "#9C8468"));
            BtnLogJson.Background = B(j ? a : "Transparent"); BtnLogJson.Foreground = B(j ? ta : tm);
            BtnLogXml.Background = B(!j ? a : "Transparent"); BtnLogXml.Foreground = B(!j ? ta : tm);
        }

        // ===== SETTINGS HANDLERS =====
        private void TxtBusinessSoftware_TextChanged(object s, TextChangedEventArgs e) { if (_viewModel != null) _viewModel.BusinessSoftwareName = TxtBusinessSoftware.Text.Trim(); }
        private void TxtEncryptionKey_LostFocus(object s, RoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.EncryptionKey = TxtEncryptionKey.Text.Trim();
                PwdEncryptionKey.Password = TxtEncryptionKey.Text;
            }
        }
        private void TxtEncryptionExtensions_LostFocus(object s, RoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.EncryptionExtensionsText = TxtEncryptionExtensions.Text.Trim();
            }
        }
        private void TxtLargeFileThreshold_LostFocus(object s, RoutedEventArgs e)
        {
            if (_viewModel != null && int.TryParse(TxtLargeFileThreshold.Text.Trim(), out int val))
            {
                _viewModel.LargeFileThresholdKo = Math.Max(0, val);
                TxtLargeFileThreshold.Text = _viewModel.LargeFileThresholdKo.ToString();
            }
        }

        private void TxtPriorityExtensions_LostFocus(object s, RoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.PriorityExtensionsText = TxtPriorityExtensions.Text.Trim();
                TxtPriorityExtensions.Text = _viewModel.PriorityExtensionsText;
        private void CmbLogMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_viewModel != null && CmbLogMode.SelectedItem is ComboBoxItem item)
            {
                _viewModel.LogMode = item.Tag.ToString()!;
            }
        }

        // ===== TRANSLATIONS =====
        private void ApplyTranslations()
        {
            TxtSubtitle.Text = _lang.GetText("wpf_subtitle");
            BtnNavJobs.Content = _lang.GetText("nav_jobs"); BtnNavDashboard.Content = _lang.GetText("nav_dashboard"); BtnNavSettings.Content = _lang.GetText("nav_settings");
            TxtJobsTitle.Text = _lang.GetText("jobs_title"); BtnExecuteAll.Content = _lang.GetText("wpf_execute_all");
            if (_editingJob == null) { TxtCreateTitle.Text = _lang.GetText("jobs_create_title"); BtnCreate.Content = _lang.GetText("wpf_btn_add"); }
            LblFormName.Text = _lang.GetText("wpf_label_name"); LblFormSource.Text = _lang.GetText("wpf_label_source");
            LblFormTarget.Text = _lang.GetText("wpf_label_target"); LblFormType.Text = _lang.GetText("wpf_label_type");
            TxtStatus.Text = _lang.GetText("wpf_ready"); TxtJobCount.Text = _lang.GetText("wpf_jobs_count", _viewModel.GetJobCount());
            TxtDashboardTitle.Text = _lang.GetText("dashboard_title");
            _dashTotalLabel.Text = _lang.GetText("dashboard_total_jobs"); _dashStatusLabel.Text = _lang.GetText("dashboard_status");
            _dashLogLabel.Text = _lang.GetText("dashboard_log_format"); _dashEncLabel.Text = _lang.GetText("dashboard_encryption");
            TxtSettingsTitle.Text = _lang.GetText("nav_settings");
            BtnTabGeneral.Content = _lang.GetText("settings_tab_general"); BtnTabLogs.Content = _lang.GetText("settings_tab_logs");
            BtnTabLanguage.Content = _lang.GetText("settings_tab_language"); BtnTabTheme.Content = _lang.GetText("settings_tab_theme");
            LblSettingsBusiness.Text = _lang.GetText("settings_business_software"); LblSettingsBusinessDesc.Text = _lang.GetText("settings_business_desc");
            LblSettingsEncryption.Text = _lang.GetText("settings_encryption"); LblSettingsEncKey.Text = _lang.GetText("settings_encryption_key");
            LblSettingsEncExt.Text = _lang.GetText("settings_encryption_ext");
            LblSettingsLargeFile.Text = _lang.GetText("settings_large_file"); LblSettingsLargeFileDesc.Text = _lang.GetText("settings_large_file_desc");
            LblSettingsPriorityExt.Text = _lang.GetText("settings_priority_ext"); LblSettingsPriorityExtDesc.Text = _lang.GetText("settings_priority_ext_desc");
            LblSettingsLogFormat.Text = _lang.GetText("settings_log_format");
            LblSettingsLogDesc.Text = _lang.GetText("settings_log_desc"); LblSettingsLangTitle.Text = _lang.GetText("settings_language_title");
            LblSettingsLangDesc.Text = _lang.GetText("settings_language_desc"); LblSettingsThemeTitle.Text = _lang.GetText("settings_theme_title");
            LblSettingsThemeDesc.Text = _lang.GetText("settings_theme_desc");
            LblSettingsLogMode.Text = _lang.GetText("settings_log_mode_title");
            LblSettingsLogModeDesc.Text = _lang.GetText("settings_log_mode_desc");

            if (CmbType != null && CmbType.Items.Count >= 2)
            {
                ((ComboBoxItem)CmbType.Items[0]).Content = _lang.GetText("type_full");
                ((ComboBoxItem)CmbType.Items[1]).Content = _lang.GetText("type_differential");
            }

            if (CmbLogMode != null && CmbLogMode.Items.Count >= 3)
            {
                ((ComboBoxItem)CmbLogMode.Items[0]).Content = _lang.GetText("log_mode_local");
                ((ComboBoxItem)CmbLogMode.Items[1]).Content = _lang.GetText("log_mode_centralized");
                ((ComboBoxItem)CmbLogMode.Items[2]).Content = _lang.GetText("log_mode_both");
            }
            UpdateWarning(); UpdateDashboard();
        }

        // ===== DASHBOARD UPDATE =====
        private void UpdateDashboard()
        {
            _dashTotalValue.Text = _viewModel.GetJobCount().ToString();
            _dashLogValue.Text = _viewModel.LogFormat.ToUpper();
            var (sd, ss, tp) = (TC("StatusDanger", "#9B4D4D"), TC("StatusSuccess", "#5A7247"), TC("TextPrimary", "#553f2a"));
            if (_viewModel.IsBusinessSoftwareDetected)
            { _dashStatusDot.Fill = B(sd); _dashStatusValue.Text = _lang.GetText("dashboard_blocked"); _dashStatusValue.Foreground = B(sd); }
            else
            { _dashStatusDot.Fill = B(ss); _dashStatusValue.Text = _lang.GetText("dashboard_ready"); _dashStatusValue.Foreground = B(tp); }

            if (_viewModel.IsEncryptionActive)
            {
                _dashEncDot.Fill = B(ss);
                _dashEncValue.Text = _lang.GetText("dashboard_active");
                _dashEncValue.Foreground = B(ss);
            }
            else
            {
                _dashEncDot.Fill = B(TC("TextMuted", "#9C8468"));
                _dashEncValue.Text = _lang.GetText("dashboard_inactive");
                _dashEncValue.Foreground = B(TC("TextMuted", "#9C8468"));
            }
        }

        // ===== WARNING =====
        private void UpdateWarning()
        {
            var (sd, ss) = (TC("StatusDanger", "#9B4D4D"), TC("StatusSuccess", "#5A7247"));
            if (_viewModel.IsBusinessSoftwareDetected)
            { WarningBanner.Visibility = Visibility.Visible; WarningText.Text = _lang.GetText("error_business_software"); MonitorDot.Fill = B(sd); LblMonitorStatus.Text = _lang.GetText("wpf_monitor_detected"); LblMonitorStatus.Foreground = B(sd); }
            else
            { WarningBanner.Visibility = Visibility.Collapsed; MonitorDot.Fill = B(ss); LblMonitorStatus.Text = _lang.GetText("wpf_monitor_not_detected"); LblMonitorStatus.Foreground = B(ss); }
        }

        // ===== NOTIFICATION =====
        private void ShowNotificationToast()
        {
            UpdateNotificationStyle(); NotifText.Text = _viewModel.NotificationMessage;
            NotificationToast.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(250)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
        }
        private void HideNotificationToast() =>
            NotificationToast.BeginAnimation(OpacityProperty, new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(300)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn } });

        private void UpdateNotificationStyle()
        {
            var (d, bg) = _viewModel.NotificationType switch
            {
                "success" => (TC("StatusSuccess", "#5A7247"), TC("StatusSuccessBg", "#EFF5EB")),
                "error" => (TC("StatusDanger", "#9B4D4D"), TC("StatusDangerBg", "#F5EDED")),
                "warning" => (TC("StatusWarning", "#B8860B"), TC("StatusWarningBg", "#F5F0E0")),
                _ => (TC("AccentPrimary", "#a67847"), TC("BgCard", "#F2E0CE"))
            };
            NotifDot.Fill = B(d); NotifBg.Color = Cl(bg);
        }

        private void SyncPlayButtons() { bool can = _viewModel.CanExecute; foreach (var btn in _playButtons) btn.IsEnabled = can; }

        protected override void OnClosed(EventArgs e) { _viewModel.Dispose(); base.OnClosed(e); }

        // ===== HELPERS =====
        private static SolidColorBrush B(string hex) => new((Color)ColorConverter.ConvertFromString(hex));
        private static Color Cl(string hex) => (Color)ColorConverter.ConvertFromString(hex);
        private static string TC(string key, string fb) => Application.Current.Resources[key] is SolidColorBrush b ? b.Color.ToString() : fb;
    }
}