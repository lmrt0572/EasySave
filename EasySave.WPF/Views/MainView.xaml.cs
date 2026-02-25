using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using EasySave.Core.Models;
using EasySave.Core.Models.Enums;
using EasySave.Core.ViewModels;
using EasySave.WPF.Controls;
using EasySave.WPF.Helpers;
using Microsoft.Win32;
using Lang = EasySave.Core.Models.Enums.Language;

namespace EasySave.WPF.Views
{
    public partial class MainView : Window
    {
        // ===== FIELDS =====
        private readonly WpfViewModel _viewModel;
        private readonly LanguageManager _lang;
        private string _currentPage = "Jobs";
        private int _currentSettingsTab;
        private BackupJob? _editingJob;
        private int _currentThemeIndex;

        private static readonly (string File, string Name, string Bg, string Sidebar, string Accent, string Border, string Text)[] Themes =
        {
            ("Styles/Themes/Theme_CaramelProfond.xaml", "Caramel Profond", "#DFC4A8", "#3E2415", "#B5651D", "#C4A07A", "#3E2415"),
            ("Styles/Themes/Theme_ModeNuit.xaml",       "Mode Nuit",       "#1E1E2E", "#14101E", "#C99B6D", "#3E3E52", "#E0D8CC"),
        };

        private readonly Dictionary<string, ProgressCardControl> _progressCards = new();

        // ===== CONSTRUCTOR =====
        public MainView()
        {
            InitializeComponent();
            _lang = new LanguageManager();
            _viewModel = new WpfViewModel(_lang);

            // View model property changes are marshalled to the UI thread and drive status, buttons, and notifications.
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

            // Running jobs collection changes drive the progress cards panel and the active jobs count.
            _viewModel.RunningJobsProgress.CollectionChanged += (s, e) => Dispatcher.Invoke(() =>
            {
                RefreshProgressCards();
                UpdateJobCardsRunningState();
                ActiveJobsEncart.Visibility = _viewModel.RunningJobsProgress.Count > 0
                    ? Visibility.Visible
                    : Visibility.Collapsed;
                TxtActiveJobsCount.Text = $"{_viewModel.RunningJobsProgress.Count} active job(s)";
            });

            SettingsPanel.BusinessSoftwareText = _viewModel.BusinessSoftwareName;
            SettingsPanel.EncryptionKeyText = _viewModel.EncryptionKey;
            SettingsPanel.PwdEncryptionPassword = _viewModel.EncryptionKey;
            SettingsPanel.EncryptionKeyVisibility = Visibility.Collapsed;
            SettingsPanel.PwdEncryptionVisibility = Visibility.Visible;
            SettingsPanel.ToggleKeyContent = "\U0001F512";
            SettingsPanel.EncryptionExtensionsText = _viewModel.EncryptionExtensionsText;
            SettingsPanel.LargeFileThresholdText = _viewModel.LargeFileThresholdKo.ToString();
            SettingsPanel.PriorityExtensionsText = _viewModel.PriorityExtensionsText;

            WireSettingsPanelEvents();
            BuildThemeSwatches();
            SetActiveNav("Jobs");
            SettingsPanel.SetActiveTab(0);
            UpdateLogFormatButtons();
            UpdateLanguageButtons();
            UpdateThemeSelection();
            ApplyTranslations();
            RefreshJobList();
            UpdateWarning();
            UpdateDashboard();
            BtnExecuteAll.IsEnabled = _viewModel.CanExecute;

            foreach (ComboBoxItem item in SettingsPanel.LogModeCombo.Items)
            {
                if (item.Tag?.ToString() == _viewModel.LogMode)
                {
                    SettingsPanel.LogModeCombo.SelectedItem = item;
                    break;
                }
            }
        }

        // ===== SETTINGS PANEL WIRING =====
        private void WireSettingsPanelEvents()
        {
            SettingsPanel.TabClicked += (_, idx) => SetActiveSettingsTab(idx);
            SettingsPanel.BusinessSoftwareTextChanged += (_, _) => _viewModel.BusinessSoftwareName = SettingsPanel.BusinessSoftwareText.Trim();
            SettingsPanel.EncryptionKeyLostFocus += (_, _) =>
            {
                _viewModel.EncryptionKey = SettingsPanel.EffectiveEncryptionKey.Trim();
                SettingsPanel.EncryptionKeyText = _viewModel.EncryptionKey;
            };
            SettingsPanel.EncryptionExtensionsLostFocus += (_, _) => _viewModel.EncryptionExtensionsText = SettingsPanel.EncryptionExtensionsText.Trim();
            SettingsPanel.LargeFileThresholdLostFocus += (_, _) =>
            {
                if (int.TryParse(SettingsPanel.LargeFileThresholdText.Trim(), out int val))
                {
                    _viewModel.LargeFileThresholdKo = Math.Max(0, val);
                    SettingsPanel.LargeFileThresholdText = _viewModel.LargeFileThresholdKo.ToString();
                }
            };
            SettingsPanel.PriorityExtensionsLostFocus += (_, _) =>
            {
                _viewModel.PriorityExtensionsText = SettingsPanel.PriorityExtensionsText.Trim();
                SettingsPanel.PriorityExtensionsText = _viewModel.PriorityExtensionsText;
            };
            SettingsPanel.LogJsonClicked += (_, _) => SetLogFormatAndNotify("json");
            SettingsPanel.LogXmlClicked += (_, _) => SetLogFormatAndNotify("xml");
            SettingsPanel.LogModeSelectionChanged += (_, _) =>
            {
                if (SettingsPanel.LogModeCombo.SelectedItem is ComboBoxItem item)
                    _viewModel.LogMode = item.Tag?.ToString() ?? "Local";
            };
            SettingsPanel.SettingsLangEnClicked += (_, _) => SwitchLang(Lang.English);
            SettingsPanel.SettingsLangFrClicked += (_, _) => SwitchLang(Lang.French);
            SettingsPanel.ThemeSwatchClicked += (_, idx) => ApplyTheme(idx);
        }

        // Persists the chosen log format and shows a confirmation notification.
        private void SetLogFormatAndNotify(string format)
        {
            _viewModel.LogFormat = format;
            UpdateLogFormatButtons();
            UpdateDashboard();
            _viewModel.ShowNotification(_lang.GetText("notif_settings_saved"), "success");
        }

        // ===== FOLDER BROWSER =====
        // Source and target path buttons open a folder dialog and assign the result to the corresponding text box.
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

        // ===== NAVIGATION =====
        // Sidebar and settings tabs update visibility and highlight the active item using theme colors.
        private void BtnNavJobs_Click(object s, RoutedEventArgs e) => SetActiveNav("Jobs");
        private void BtnNavDashboard_Click(object s, RoutedEventArgs e) { UpdateDashboard(); SetActiveNav("Dashboard"); }
        private void BtnNavSettings_Click(object s, RoutedEventArgs e) => SetActiveNav("Settings");

        private void SetActiveNav(string page)
        {
            _currentPage = page;
            JobsPage.Visibility = page == "Jobs" ? Visibility.Visible : Visibility.Collapsed;
            DashboardPage.Visibility = page == "Dashboard" ? Visibility.Visible : Visibility.Collapsed;
            SettingsPage.Visibility = page == "Settings" ? Visibility.Visible : Visibility.Collapsed;
            var accent = ThemeColorsHelper.GetBrush(ThemeColorsHelper.AccentPrimary);
            var textOnAccent = ThemeColorsHelper.GetBrush(ThemeColorsHelper.TextOnAccent);
            var muted = ThemeColorsHelper.GetBrush(ThemeColorsHelper.TextOnDarkMuted);
            foreach (var (btn, p) in new[] { (BtnNavJobs, "Jobs"), (BtnNavDashboard, "Dashboard"), (BtnNavSettings, "Settings") })
            { btn.Background = p == page ? accent : Brushes.Transparent; btn.Foreground = p == page ? textOnAccent : muted; }
        }

        private void SetActiveSettingsTab(int idx)
        {
            _currentSettingsTab = idx;
            SettingsPanel.SetActiveTab(idx);
        }

        // ===== THEME =====
        // Replaces the app-level theme dictionary and refreshes all UI that depends on it.
        private void ApplyTheme(int idx)
        {
            if (idx < 0 || idx >= Themes.Length) return;
            _currentThemeIndex = idx;
            var dicts = Application.Current.Resources.MergedDictionaries;
            var theme = new ResourceDictionary { Source = new Uri(Themes[idx].File, UriKind.Relative) };
            if (dicts.Count > 0) dicts[0] = theme; else dicts.Insert(0, theme);
            RefreshAllThemeDependentUI();
            SettingsPanel.CurrentThemeText = $"Active: {Themes[idx].Name}";
            _viewModel.ShowNotification($"Theme: {Themes[idx].Name}", "success");
        }

        // Refreshes navigation, tabs, buttons, job list, progress cards, warning, and dashboard after a theme change.
        private void RefreshAllThemeDependentUI()
        {
            UpdateThemeSelection();
            SetActiveNav(_currentPage);
            SetActiveSettingsTab(_currentSettingsTab);
            UpdateLogFormatButtons();
            UpdateLanguageButtons();
            RefreshJobList();
            UpdateJobCardsRunningState();
            foreach (var card in _progressCards.Values)
                card.RefreshTheme();
            UpdateWarning();
            UpdateDashboard();
        }

        // Builds one clickable swatch per theme and adds it to the settings panel.
        private void BuildThemeSwatches()
        {
            var panel = SettingsPanel.ThemeSwatches;
            panel.Children.Clear();
            for (int i = 0; i < Themes.Length; i++)
            {
                var t = Themes[i];
                var dots = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 6) };
                dots.Children.Add(new Ellipse { Width = 20, Height = 20, Fill = ThemeColorsHelper.GetBrushFromHex(t.Bg), Stroke = ThemeColorsHelper.GetBrushFromHex(t.Border), StrokeThickness = 1 });
                dots.Children.Add(new Ellipse { Width = 20, Height = 20, Fill = ThemeColorsHelper.GetBrushFromHex(t.Sidebar), Margin = new Thickness(3, 0, 0, 0) });
                dots.Children.Add(new Ellipse { Width = 20, Height = 20, Fill = ThemeColorsHelper.GetBrushFromHex(t.Accent), Margin = new Thickness(3, 0, 0, 0) });
                var label = new TextBlock { Text = t.Name, FontSize = 10, FontWeight = FontWeights.SemiBold, Foreground = ThemeColorsHelper.GetBrushFromHex(t.Text), HorizontalAlignment = HorizontalAlignment.Center };
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
                    Background = ThemeColorsHelper.GetBrushFromHex(t.Bg),
                    BorderThickness = new Thickness(2),
                    BorderBrush = Brushes.Transparent,
                    Child = inner
                };
                swatch.MouseLeftButtonDown += SettingsPanel.OnThemeSwatchClicked;
                panel.Children.Add(swatch);
            }
        }

        // Updates the selected theme border and the current theme label in the settings panel.
        private void UpdateThemeSelection()
        {
            SettingsPanel.SetThemeSwatchBorder(_currentThemeIndex);
            SettingsPanel.CurrentThemeText = $"Active: {Themes[_currentThemeIndex].Name}";
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

        // ===== PROGRESS CARDS =====
        // Keeps the progress cards list in sync with the running jobs collection; adds or removes cards and updates labels.
        private void RefreshProgressCards()
        {
            var activeNames = new HashSet<string>(_viewModel.RunningJobsProgress.Select(p => p.JobName));
            var toRemove = _progressCards.Keys.Where(k => !activeNames.Contains(k)).ToList();
            foreach (var name in toRemove)
            {
                if (_progressCards.TryGetValue(name, out var oldCard))
                    ProgressItemsControl.Items.Remove(oldCard);
                _progressCards.Remove(name);
            }

            var (run, paused, stopped, completed) = GetProgressLabels();
            foreach (var info in _viewModel.RunningJobsProgress)
            {
                if (!_progressCards.ContainsKey(info.JobName))
                {
                    var card = new ProgressCardControl();
                    card.Bind(info, jobName =>
                    {
                        if (_viewModel.IsJobPaused(jobName)) _viewModel.ResumeJob(jobName);
                        else _viewModel.PauseJob(jobName);
                    }, _viewModel.StopJob);
                    card.SetStatusLabels(run, paused, stopped, completed);
                    _progressCards[info.JobName] = card;
                    ProgressItemsControl.Items.Add(card);
                }
                else
                    _progressCards[info.JobName].SetStatusLabels(run, paused, stopped, completed);
            }
        }

        private (string run, string paused, string stopped, string completed) GetProgressLabels() => (
            _lang.GetText("progress_running"), _lang.GetText("progress_paused"), _lang.GetText("progress_stopped"), _lang.GetText("progress_completed"));

        // ===== JOB LIST =====
        private void RefreshJobList()
        {
            JobListPanel.Children.Clear();
            var jobs = _viewModel.Jobs;
            var tm = ThemeColorsHelper.GetBrush(ThemeColorsHelper.TextMuted);
            var tml = ThemeColorsHelper.GetBrush(ThemeColorsHelper.TextOnDarkMuted);
            if (jobs.Count == 0)
            {
                var ep = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 60, 0, 0) };
                ep.Children.Add(new TextBlock { Text = _lang.GetText("jobs_empty"), Foreground = tm, FontSize = 15, FontWeight = FontWeights.SemiBold, HorizontalAlignment = HorizontalAlignment.Center });
                ep.Children.Add(new TextBlock { Text = _lang.GetText("jobs_empty_desc"), Foreground = tml, FontSize = 12, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 4, 0, 0) });
                JobListPanel.Children.Add(ep);
            }
            else
            {
                foreach (var job in jobs)
                {
                    var card = new JobCardControl
                    {
                        Job = job,
                        IsRunning = _viewModel.IsJobRunning(job.Name),
                        CanExecute = _viewModel.CanExecute
                    };
                    card.SetTypeLabel(_lang.GetText("type_full"), _lang.GetText("type_differential"));
                    card.PlayClick += (s, _) =>
                    {
                        if (_viewModel.IsJobRunning(job.Name))
                        {
                            if (_viewModel.IsJobPaused(job.Name)) _viewModel.ResumeJob(job.Name);
                            else _viewModel.PauseJob(job.Name);
                        }
                        else ExecuteSingleJob(job);
                    };
                    card.EditClick += (s, _) => StartEditJob(job);
                    card.DeleteClick += (s, _) => DeleteJob(job);
                    JobListPanel.Children.Add(card);
                }
            }
            TxtJobCount.Text = _lang.GetText("wpf_jobs_count", jobs.Count);
            UpdateDashboard();
        }

        // Updates each job card's running state and refreshes its theme-dependent background.
        private void UpdateJobCardsRunningState()
        {
            foreach (var child in JobListPanel.Children)
            {
                if (child is JobCardControl card)
                {
                    var jobName = card.Job?.Name;
                    if (jobName != null)
                    {
                        card.IsRunning = _viewModel.IsJobRunning(jobName);
                        card.RefreshTheme();
                    }
                }
            }
        }

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

        // ===== JOB FORM AND ACTIONS =====
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
            var td = ThemeColorsHelper.GetBrush(ThemeColorsHelper.TextOnDark);
            var tm = ThemeColorsHelper.GetBrush(ThemeColorsHelper.TextOnDarkMuted);
            BtnLangEn.Foreground = en ? td : tm;
            BtnLangEn.FontWeight = en ? FontWeights.Bold : FontWeights.SemiBold;
            BtnLangFr.Foreground = !en ? td : tm;
            BtnLangFr.FontWeight = !en ? FontWeights.Bold : FontWeights.SemiBold;
            SettingsPanel.SetLanguageButtons(en, FontWeights.Bold, FontWeights.SemiBold);
        }

        private void UpdateLogFormatButtons()
        {
            bool j = _viewModel.LogFormat == "json";
            SettingsPanel.SetLogFormatButtons(j);
        }

        // ===== TRANSLATIONS =====
        // Applies the current language to all visible text, including dashboard, settings, job cards, and progress cards.
        private void ApplyTranslations()
        {
            TxtSubtitle.Text = _lang.GetText("wpf_subtitle");
            BtnNavJobs.Content = _lang.GetText("nav_jobs"); BtnNavDashboard.Content = _lang.GetText("nav_dashboard"); BtnNavSettings.Content = _lang.GetText("nav_settings");
            TxtJobsTitle.Text = _lang.GetText("jobs_title"); BtnExecuteAll.Content = _lang.GetText("wpf_execute_all");
            if (_editingJob == null) { TxtCreateTitle.Text = _lang.GetText("jobs_create_title"); BtnCreate.Content = _lang.GetText("wpf_btn_add"); }
            LblFormName.Text = _lang.GetText("wpf_label_name"); LblFormSource.Text = _lang.GetText("wpf_label_source");
            LblFormTarget.Text = _lang.GetText("wpf_label_target"); LblFormType.Text = _lang.GetText("wpf_label_type");
            TxtStatus.Text = _lang.GetText("wpf_ready"); TxtJobCount.Text = _lang.GetText("wpf_jobs_count", _viewModel.GetJobCount());
            DashboardPanel.SetTitle(_lang.GetText("dashboard_title"));
            DashboardPanel.SetLabels(_lang.GetText("dashboard_total_jobs"), _lang.GetText("dashboard_status"), _lang.GetText("dashboard_log_format"), _lang.GetText("dashboard_encryption"));
            TxtSettingsTitle.Text = _lang.GetText("nav_settings");
            SettingsPanel.SetLabels(_lang.GetText);

            if (CmbType != null && CmbType.Items.Count >= 2)
            {
                ((ComboBoxItem)CmbType.Items[0]).Content = _lang.GetText("type_full");
                ((ComboBoxItem)CmbType.Items[1]).Content = _lang.GetText("type_differential");
            }
            var (typeFull, typeDiff) = (_lang.GetText("type_full"), _lang.GetText("type_differential"));
            foreach (var child in JobListPanel.Children)
                if (child is JobCardControl card)
                    card.SetTypeLabel(typeFull, typeDiff);
            var (run, paused, stopped, completed) = GetProgressLabels();
            foreach (var card in _progressCards.Values)
                card.SetStatusLabels(run, paused, stopped, completed);
            UpdateWarning(); UpdateDashboard();
        }

        private void UpdateDashboard()
        {
            DashboardPanel.UpdateDashboard(
                _viewModel.GetJobCount(),
                _viewModel.LogFormat,
                _viewModel.IsBusinessSoftwareDetected,
                _viewModel.IsBusinessSoftwareDetected ? _lang.GetText("dashboard_blocked") : _lang.GetText("dashboard_ready"),
                _viewModel.IsEncryptionActive,
                _lang.GetText("dashboard_active"),
                _lang.GetText("dashboard_inactive"));
        }

        // Shows or hides the business-software warning and updates the monitor status in the settings panel.
        private void UpdateWarning()
        {
            var sd = ThemeColorsHelper.GetBrush(ThemeColorsHelper.StatusDanger);
            var ss = ThemeColorsHelper.GetBrush(ThemeColorsHelper.StatusSuccess);
            if (_viewModel.IsBusinessSoftwareDetected)
            {
                WarningBanner.Visibility = Visibility.Visible;
                WarningText.Text = _lang.GetText("error_business_software");
                SettingsPanel.SetMonitorStatus(_lang.GetText("wpf_monitor_detected"), sd);
            }
            else
            {
                WarningBanner.Visibility = Visibility.Collapsed;
                SettingsPanel.SetMonitorStatus(_lang.GetText("wpf_monitor_not_detected"), ss);
            }
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
            var (dotKey, bgKey) = _viewModel.NotificationType switch
            {
                "success" => (ThemeColorsHelper.StatusSuccess, ThemeColorsHelper.StatusSuccessBg),
                "error" => (ThemeColorsHelper.StatusDanger, ThemeColorsHelper.StatusDangerBg),
                "warning" => (ThemeColorsHelper.StatusWarning, ThemeColorsHelper.StatusWarningBg),
                _ => (ThemeColorsHelper.AccentPrimary, ThemeColorsHelper.BgCard)
            };
            NotifDot.Fill = ThemeColorsHelper.GetBrush(dotKey);
            NotifBg.Color = ThemeColorsHelper.GetColorValue(bgKey);
        }

        // Enables or disables the play button on each job card according to the view model.
        private void SyncPlayButtons()
        {
            bool can = _viewModel.CanExecute;
            foreach (var child in JobListPanel.Children)
                if (child is JobCardControl card)
                    card.CanExecute = can;
        }

        // ===== LIFECYCLE =====
        protected override void OnClosed(EventArgs e) { _viewModel.Dispose(); base.OnClosed(e); }
    }
}