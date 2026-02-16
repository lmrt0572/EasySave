using System;
using System.Collections.Generic;
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
        private bool _isKeyVisible = true;

        private static readonly (string File, string Name, string Bg, string Sidebar, string Accent, string Border, string Text)[] Themes =
        {
            ("Styles/Themes/Theme_BeigeClassique.xaml", "Beige Classique", "#E7D3C1", "#553f2a", "#a67847", "#C9A882", "#553f2a"),
            ("Styles/Themes/Theme_CaramelProfond.xaml", "Caramel Profond", "#DFC4A8", "#3E2415", "#B5651D", "#C4A07A", "#3E2415"),
            ("Styles/Themes/Theme_IvoireCreme.xaml",    "Ivoire Cr\u00e8me",    "#F2ECE0", "#5C4033", "#8B6F4E", "#D4C8B4", "#4A3828"),
            ("Styles/Themes/Theme_TerreCuite.xaml",     "Terre Cuite",     "#E0C0A0", "#6B3A20", "#C06030", "#C8A078", "#5A3018"),
            ("Styles/Themes/Theme_ModeNuit.xaml",       "Mode Nuit",       "#1E1E2E", "#14101E", "#C99B6D", "#3E3E52", "#E0D8CC"),
        };

        private readonly List<Button> _playButtons = new();

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
                        if (_viewModel.IsBusinessSoftwareDetected) ProgressPanel.Visibility = Visibility.Collapsed;
                        break;
                    case nameof(WpfViewModel.CanExecute): BtnExecuteAll.IsEnabled = _viewModel.CanExecute; SyncPlayButtons(); break;
                    case nameof(WpfViewModel.IsExecuting):
                        ProgressPanel.Visibility = _viewModel.IsExecuting ? Visibility.Visible : Visibility.Collapsed;
                        SyncPlayButtons(); break;
                    case nameof(WpfViewModel.ProgressPercent): UpdateProgress(); break;
                    case nameof(WpfViewModel.ProgressText): TxtProgressFiles.Text = _viewModel.ProgressText; break;
                    case nameof(WpfViewModel.CurrentJobName): TxtProgressJob.Text = _viewModel.CurrentJobName; break;
                    case nameof(WpfViewModel.CurrentFileName): TxtProgressFile.Text = _viewModel.CurrentFileName; break;
                    case nameof(WpfViewModel.IsNotificationVisible):
                        if (_viewModel.IsNotificationVisible) ShowNotificationToast(); else HideNotificationToast(); break;
                    case nameof(WpfViewModel.NotificationMessage): NotifText.Text = _viewModel.NotificationMessage; break;
                    case nameof(WpfViewModel.NotificationType): UpdateNotificationStyle(); break;
                    case nameof(WpfViewModel.IsEncryptionActive): UpdateDashboard(); break;
                }
            });

            TxtBusinessSoftware.Text = _viewModel.BusinessSoftwareName;
            TxtEncryptionKey.Text = _viewModel.EncryptionKey;
            PwdEncryptionKey.Password = _viewModel.EncryptionKey;
            TxtEncryptionExtensions.Text = _viewModel.EncryptionExtensionsText;

            BuildThemeSwatches(); BuildDashboardCards();
            SetActiveNav("Jobs"); SetActiveSettingsTab(0);
            UpdateLogFormatButtons(); UpdateLanguageButtons(); UpdateThemeSelection();
            ApplyTranslations(); RefreshJobList(); UpdateWarning(); UpdateDashboard();
            BtnExecuteAll.IsEnabled = _viewModel.CanExecute;
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
            UpdateWarning(); UpdateDashboard(); UpdateProgressGradient();
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

        private void UpdateProgressGradient()
        {
            GradStart.Color = Cl(TC("AccentPrimary", "#a67847"));
            GradEnd.Color = Cl(TC("AccentLight", "#C99B6D"));
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

        // ===== PROGRESS =====
        private void UpdateProgress()
        {
            int pct = _viewModel.ProgressPercent;
            TxtProgressPct.Text = $"{pct}%";
            double pw = ProgressFill.Parent is Border p ? p.ActualWidth : 500;
            ProgressFill.Width = Math.Max(0, pw * pct / 100.0);
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
            var (accent, bgCard, bgHover, tp, ts, tm) = (TC("AccentPrimary", "#a67847"), TC("BgCard", "#F2E0CE"), TC("BgInput", "#EBCFB8"),
                TC("TextPrimary", "#553f2a"), TC("TextSecondary", "#7A6147"), TC("TextMuted", "#9C8468"));
            bool isFull = job.Type == BackupType.Full;
            string typeText = isFull ? _lang.GetText("type_full") : _lang.GetText("type_differential");
            Color bc = isFull ? Cl(accent) : Cl(TC("StatusSuccess", "#5A7247"));
            var card = new Border
            {
                Background = B(bgCard),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(20, 16, 20, 16),
                Margin = new Thickness(0, 0, 0, 12),
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
            var name = new TextBlock { Text = job.Name, FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = B(tp), VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(name, 0); top.Children.Add(name);
            var badge = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(0x20, bc.R, bc.G, bc.B)),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10, 4, 10, 4),
                Margin = new Thickness(0, 0, 12, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Child = new TextBlock { Text = typeText, FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(bc) }
            };
            Grid.SetColumn(badge, 1); top.Children.Add(badge);
            var acts = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            var r = MkBtn("\u25B6", accent, _viewModel.CanExecute); r.Click += (s, e) => { e.Handled = true; ExecuteSingleJob(job); }; _playButtons.Add(r); acts.Children.Add(r);
            var ed = MkBtn("\u270F", ts, true); ed.Click += (s, e) => { e.Handled = true; StartEditJob(job); }; acts.Children.Add(ed);
            var dl = MkBtn("\u2715", TC("StatusDanger", "#9B4D4D"), true); dl.Click += (s, e) => { e.Handled = true; DeleteJob(job); }; acts.Children.Add(dl);
            Grid.SetColumn(acts, 2); top.Children.Add(acts);
            Grid.SetRow(top, 0); cg.Children.Add(top);
            var info = new Grid { Margin = new Thickness(0, 10, 0, 0) };
            info.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(55) });
            info.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            info.RowDefinitions.Add(new RowDefinition()); info.RowDefinitions.Add(new RowDefinition());
            AddCell(info, 0, 0, "Source", tm); AddCell(info, 0, 1, job.SourceDirectory, ts, true);
            AddCell(info, 1, 0, "Target", tm); AddCell(info, 1, 1, job.TargetDirectory, ts, true);
            Grid.SetRow(info, 1); cg.Children.Add(info);
            card.Child = cg;
            card.MouseEnter += (s, e) => card.Background = B(bgHover);
            card.MouseLeave += (s, e) => card.Background = B(bgCard);
            return card;
        }

        private void AddCell(Grid g, int r, int c, string txt, string col, bool trim = false)
        {
            var tb = new TextBlock { Text = txt, FontSize = 11, Foreground = B(col), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 1, 0, 1) };
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

        private async void BtnExecuteAll_Click(object s, RoutedEventArgs e) { await Task.Run(() => _viewModel.ExecuteAllJobs()); Dispatcher.Invoke(RefreshJobList); }
        private async void ExecuteSingleJob(BackupJob job) { await Task.Run(() => _viewModel.ExecuteJob(job)); Dispatcher.Invoke(RefreshJobList); }
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
            LblSettingsEncExt.Text = _lang.GetText("settings_encryption_ext"); LblSettingsLogFormat.Text = _lang.GetText("settings_log_format");
            LblSettingsLogDesc.Text = _lang.GetText("settings_log_desc"); LblSettingsLangTitle.Text = _lang.GetText("settings_language_title");
            LblSettingsLangDesc.Text = _lang.GetText("settings_language_desc"); LblSettingsThemeTitle.Text = _lang.GetText("settings_theme_title");
            LblSettingsThemeDesc.Text = _lang.GetText("settings_theme_desc");
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