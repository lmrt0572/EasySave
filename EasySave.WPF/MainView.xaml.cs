using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using EasySave.Core.Models;
using EasySave.Core.Models.Enums;
using EasySave.Core.ViewModels;

namespace EasySave.WPF
{
    public partial class MainView : Window
    {
        private readonly WpfViewModel _viewModel;
        private readonly LanguageManager _lang;
        private readonly HashSet<BackupJob> _selectedJobs = new();

        // ═══ ACTIVE THEME (set by ApplyTheme) ═══
        private ThemeDef _theme;

        // ═══ THEME DEFINITIONS ═══
        private static readonly ThemeDef[] Themes = new[] {

    
    // 4. SOFT WARM (Votre thème beige amélioré pour plus de douceur)
    new ThemeDef(
        Name: "Latte",
        Bg: "#FDFBF7",
        Sidebar: "#FFFFFF",
        Text: "#4A4036",
        Soft: "#75685B",
        Muted: "#B8B0A6",
        Accent: "#D4A373",      // Caramel
        AccentHover: "#B0855A",
        BorderSoft: "#EAE5DF",
        Border: "#DED6CC",
        Hover: "#FAF7F2",
        Select: "#F0EBE4",
        Success: "#8A9A5B",     // Vert sauge
        Danger: "#C87566",      // Terracotta
        GradEnd: "#E6C9A8"
    ),

    // 5. SOFT STONE (Mode sombre gris & beige, sans tons marrons)
    new ThemeDef(
        Name: "Soft Stone",
        Bg: "#1A1B1E",          // Gris anthracite très sombre
        Sidebar: "#25262B",     // Gris ardoise (panneaux)
        Text: "#E9E5D9",        // Beige très clair (texte)
        Soft: "#A6A69F",        // Gris chaud (chemins de fichiers)
        Muted: "#70716B",       // Gris moyen (labels)
        Accent: "#D1C7B7",      // Beige sable (accentuation)
        AccentHover: "#F0EBE3", // Beige éclatant au survol
        BorderSoft: "#2C2D33",  // Bordures sombres
        Border: "#373940",      // Séparateurs
        Hover: "#2C2E33",       // Survol de ligne
        Select: "#3F414A",      // Sélection
        Success: "#9DBEBB",     // Gris-bleu/vert d'eau (discret)
        Danger: "#E29595",      // Rose poudré (pour les erreurs)
        GradEnd: "#5C5E66"      // Dégradé vers gris neutre
    ),

};

        // ═══ CONSTRUCTOR ═══
        public MainView()
        {
            InitializeComponent();

            _lang = new LanguageManager();
            _viewModel = new WpfViewModel(_lang);
            _theme = Themes[0]; // default beige

            _viewModel.PropertyChanged += (s, e) =>
            {
                Dispatcher.Invoke(() =>
                {
                    switch (e.PropertyName)
                    {
                        case nameof(WpfViewModel.StatusMessage): TxtStatus.Text = _viewModel.StatusMessage; break;
                        case nameof(WpfViewModel.IsBusinessSoftwareDetected): UpdateWarning(); break;
                        case nameof(WpfViewModel.CanExecute): UpdateButtons(); break;
                        case nameof(WpfViewModel.IsExecuting):
                            ProgressPanel.Visibility = _viewModel.IsExecuting ? Visibility.Visible : Visibility.Collapsed;
                            break;
                        case nameof(WpfViewModel.ProgressPercent): UpdateProgress(); break;
                        case nameof(WpfViewModel.ProgressText): TxtProgressFiles.Text = _viewModel.ProgressText; break;
                        case nameof(WpfViewModel.CurrentJobName): TxtProgressJob.Text = _viewModel.CurrentJobName; break;
                        case nameof(WpfViewModel.CurrentFileName): TxtProgressFile.Text = _viewModel.CurrentFileName; break;
                    }
                });
            };

            TxtBusinessSoftware.Text = _viewModel.BusinessSoftwareName;
            TxtEncryptionKey.Text = _viewModel.EncryptionKey;
            TxtEncryptionExtensions.Text = _viewModel.EncryptionExtensionsText;

            BuildThemeSwatches();
            ApplyTheme(Themes[0]);
            ApplyTranslations();
            RefreshJobList();
            UpdateWarning();
            UpdateButtons();
        }

        // ═══════════════════════════════════════════
        // ═══  THEME SYSTEM
        // ═══════════════════════════════════════════

        private void BuildThemeSwatches()
        {
            ThemePanel.Children.Clear();
            foreach (var t in Themes)
            {
                var swatch = new Border
                {
                    Width = 24,
                    Height = 24,
                    CornerRadius = new CornerRadius(12),
                    Background = new SolidColorBrush(C(t.Accent)),
                    Margin = new Thickness(0, 0, 6, 6),
                    Cursor = Cursors.Hand,
                    BorderThickness = new Thickness(2),
                    BorderBrush = new SolidColorBrush(Colors.Transparent),
                    ToolTip = t.Name
                };
                var theme = t;
                swatch.MouseLeftButtonDown += (s, e) => ApplyTheme(theme);
                ThemePanel.Children.Add(swatch);
            }
        }

        private void ApplyTheme(ThemeDef t)
        {
            _theme = t;

            // Window
            RootWindow.Background = new SolidColorBrush(C(t.Bg));

            // Sidebar
            SidebarBorder.Background = new SolidColorBrush(C(t.Sidebar));
            SidebarBorder.BorderBrush = new SolidColorBrush(C(t.Border));

            // Logo
            TxtLogo.Foreground = new SolidColorBrush(C(t.Text));
            BadgeVersion.Background = new SolidColorBrush(C(t.Accent));
            TxtSubtitle.Foreground = new SolidColorBrush(C(t.Muted));

            // Section labels
            foreach (var lbl in new[] { LblActions, LblSettings, LblTheme })
                lbl.Foreground = new SolidColorBrush(C(t.Muted));

            // Setting labels
            foreach (var lbl in new[] { LblBusinessSoftware, LblEncryptionKey, LblEncryptionExtensions })
                lbl.Foreground = new SolidColorBrush(C(t.Soft));

            // Inputs sidebar
            foreach (var tb in new[] { TxtBusinessSoftware, TxtEncryptionKey, TxtEncryptionExtensions })
            {
                tb.Foreground = new SolidColorBrush(C(t.Text));
                tb.BorderBrush = new SolidColorBrush(C(t.BorderSoft));
                tb.Background = new SolidColorBrush(C(t.Sidebar));
                tb.CaretBrush = new SolidColorBrush(C(t.Accent));
            }

            // Separators
            Sep1.Background = new SolidColorBrush(C(t.Border));

            // Footer
            TxtJobCount.Foreground = new SolidColorBrush(C(t.Muted));

            // Main area
            TxtNewJob.Foreground = new SolidColorBrush(C(t.Text));
            TxtStatus.Foreground = new SolidColorBrush(C(t.Muted));

            // Form labels
            foreach (var lbl in new[] { LblFormName, LblFormSource, LblFormTarget, LblFormType })
                lbl.Foreground = new SolidColorBrush(C(t.Muted));

            // Form inputs
            foreach (var tb in new[] { TxtName, TxtSource, TxtTarget })
            {
                tb.Foreground = new SolidColorBrush(C(t.Text));
                tb.BorderBrush = new SolidColorBrush(C(t.BorderSoft));
                tb.Background = new SolidColorBrush(C(t.Sidebar));
                tb.CaretBrush = new SolidColorBrush(C(t.Accent));
            }

            // Progress
            TxtProgressJob.Foreground = new SolidColorBrush(C(t.Text));
            TxtProgressFiles.Foreground = new SolidColorBrush(C(t.Soft));
            TxtProgressPct.Foreground = new SolidColorBrush(C(t.Accent));
            ProgressTrack.Background = new SolidColorBrush(C(t.Border));
            GradStart.Color = C(t.Accent);
            GradEnd.Color = C(t.GradEnd);
            TxtProgressFile.Foreground = new SolidColorBrush(C(t.Muted));

            // Column headers
            foreach (var col in new[] { ColName, ColSource, ColTarget, ColType })
                col.Foreground = new SolidColorBrush(C(t.Muted));

            // Swatch highlights
            for (int i = 0; i < ThemePanel.Children.Count; i++)
            {
                if (ThemePanel.Children[i] is Border b)
                    b.BorderBrush = new SolidColorBrush(Themes[i] == t ? C(t.Text) : Colors.Transparent);
            }

            // Re-render job list with new theme colors
            RefreshJobList();
        }

        // ═══════════════════════════════════════════
        // ═══  TRANSLATIONS
        // ═══════════════════════════════════════════

        private void ApplyTranslations()
        {
            // Sidebar
            TxtSubtitle.Text = _lang.GetText("wpf_subtitle");
            LblActions.Text = _lang.GetText("wpf_actions");
            BtnExecuteAll.Content = _lang.GetText("wpf_execute_all");
            BtnExecuteSelected.Content = _lang.GetText("wpf_execute_selected");
            BtnDeleteSelected.Content = _lang.GetText("wpf_delete_selected");
            LblSettings.Text = _lang.GetText("wpf_settings");
            LblBusinessSoftware.Text = _lang.GetText("wpf_business_software");
            LblEncryptionKey.Text = _lang.GetText("wpf_encryption_key");
            LblEncryptionExtensions.Text = _lang.GetText("wpf_encryption_extensions");
            LblTheme.Text = _lang.GetText("wpf_theme");

            // Monitor
            if (!_viewModel.IsBusinessSoftwareDetected)
                LblMonitorStatus.Text = _lang.GetText("wpf_monitor_not_detected");
            else
                LblMonitorStatus.Text = _lang.GetText("wpf_monitor_detected");

            // Main area
            TxtNewJob.Text = _lang.GetText("wpf_new_job");
            LblFormName.Text = _lang.GetText("wpf_label_name");
            LblFormSource.Text = _lang.GetText("wpf_label_source");
            LblFormTarget.Text = _lang.GetText("wpf_label_target");
            LblFormType.Text = _lang.GetText("wpf_label_type");
            BtnCreate.Content = _lang.GetText("wpf_btn_add");

            // Column headers
            ColName.Text = _lang.GetText("wpf_col_name");
            ColSource.Text = _lang.GetText("wpf_col_source");
            ColTarget.Text = _lang.GetText("wpf_col_target");
            ColType.Text = _lang.GetText("wpf_col_type");

            // Status
            TxtStatus.Text = _lang.GetText("wpf_ready");
            TxtJobCount.Text = _lang.GetText("wpf_jobs_count", _viewModel.GetJobCount());
        }

        // ═══════════════════════════════════════════
        // ═══  PROGRESS
        // ═══════════════════════════════════════════

        private void UpdateProgress()
        {
            int pct = _viewModel.ProgressPercent;
            TxtProgressPct.Text = $"{pct}%";
            double parentWidth = ProgressFill.Parent is Border parent ? parent.ActualWidth : 500;
            ProgressFill.Width = Math.Max(0, parentWidth * pct / 100.0);
        }

        // ═══════════════════════════════════════════
        // ═══  JOB LIST
        // ═══════════════════════════════════════════

        private void RefreshJobList()
        {
            JobListPanel.Children.Clear();
            _selectedJobs.Clear();
            var jobs = _viewModel.Jobs;

            if (jobs.Count == 0)
            {
                JobListPanel.Children.Add(new TextBlock
                {
                    Text = _lang.GetText("no_jobs"),
                    Foreground = new SolidColorBrush(C(_theme.Muted)),
                    FontSize = 13,
                    FontFamily = new FontFamily("Segoe UI"),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 48, 0, 0)
                });
            }
            else
            {
                for (int i = 0; i < jobs.Count; i++)
                    JobListPanel.Children.Add(CreateJobRow(jobs[i], i + 1, i == jobs.Count - 1));
            }

            TxtJobCount.Text = _lang.GetText("wpf_jobs_count", jobs.Count);
        }

        private UIElement CreateJobRow(BackupJob job, int index, bool isLast)
        {
            var container = new StackPanel();
            bool isFull = job.Type == BackupType.Full;
            string typeText = isFull ? _lang.GetText("type_full") : _lang.GetText("type_differential");
            Color badgeColor = isFull ? C(_theme.Accent) : C(_theme.Success);

            var grid = new Grid { Margin = new Thickness(12, 0, 12, 0) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(32) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.4, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.4, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });

            AddCell(grid, 0, index.ToString(), C(_theme.Muted), 11.5);
            AddCell(grid, 1, job.Name, C(_theme.Text), 13, true);
            AddCell(grid, 2, job.SourceDirectory, C(_theme.Soft), 11.5, false, true);
            AddCell(grid, 3, job.TargetDirectory, C(_theme.Soft), 11.5, false, true);

            // Type pill
            var pill = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(0x18, badgeColor.R, badgeColor.G, badgeColor.B)),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(8, 2, 8, 2),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left,
                Child = new TextBlock
                {
                    Text = typeText,
                    Foreground = new SolidColorBrush(badgeColor),
                    FontSize = 10.5,
                    FontWeight = FontWeights.SemiBold,
                    FontFamily = new FontFamily("Segoe UI")
                }
            };
            Grid.SetColumn(pill, 4);
            grid.Children.Add(pill);

            // Actions
            var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };

            var btnRun = new Button { Content = "▶", Style = (Style)FindResource("BtnIcon"), Foreground = new SolidColorBrush(C(_theme.Accent)), FontSize = 11, IsEnabled = _viewModel.CanExecute };
            btnRun.Click += (s, e) => { e.Handled = true; ExecuteSingleJob(job); };
            actions.Children.Add(btnRun);

            var btnDel = new Button { Content = "✕", Style = (Style)FindResource("BtnIcon"), Foreground = new SolidColorBrush(C(_theme.Danger)), FontSize = 11 };
            btnDel.Click += (s, e) => { e.Handled = true; _viewModel.DeleteJob(job); RefreshJobList(); };
            actions.Children.Add(btnDel);

            Grid.SetColumn(actions, 5);
            grid.Children.Add(actions);

            var row = new Border
            {
                Background = Brushes.Transparent,
                Padding = new Thickness(0, 10, 0, 10),
                Child = grid,
                Cursor = Cursors.Hand
            };

            var hoverBrush = new SolidColorBrush(C(_theme.Hover));
            var selectBrush = new SolidColorBrush(C(_theme.Select));

            row.MouseEnter += (s, e) => { if (!_selectedJobs.Contains(job)) row.Background = hoverBrush; };
            row.MouseLeave += (s, e) => { if (!_selectedJobs.Contains(job)) row.Background = Brushes.Transparent; };
            row.MouseLeftButtonDown += (s, e) =>
            {
                if (_selectedJobs.Contains(job)) { _selectedJobs.Remove(job); row.Background = Brushes.Transparent; }
                else { _selectedJobs.Add(job); row.Background = selectBrush; }
            };

            container.Children.Add(row);

            if (!isLast)
                container.Children.Add(new Border { Height = 1, Background = new SolidColorBrush(C(_theme.Border)), Margin = new Thickness(12, 0, 12, 0) });

            return container;
        }

        private void AddCell(Grid grid, int col, string text, Color color, double size, bool bold = false, bool trim = false)
        {
            var tb = new TextBlock { Text = text, Foreground = new SolidColorBrush(color), FontSize = size, FontFamily = new FontFamily("Segoe UI"), VerticalAlignment = VerticalAlignment.Center };
            if (bold) tb.FontWeight = FontWeights.SemiBold;
            if (trim) tb.TextTrimming = TextTrimming.CharacterEllipsis;
            Grid.SetColumn(tb, col);
            grid.Children.Add(tb);
        }

        // ═══════════════════════════════════════════
        // ═══  HANDLERS
        // ═══════════════════════════════════════════

        private void BtnCreate_Click(object sender, RoutedEventArgs e)
        {
            string name = TxtName.Text.Trim(), source = TxtSource.Text.Trim(), target = TxtTarget.Text.Trim();
            int typeInput = CmbType.SelectedIndex == 1 ? 2 : 1;
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(target))
            { TxtStatus.Text = _lang.GetText("error_invalid_choice"); return; }
            if (_viewModel.CreateJob(name, source, target, typeInput))
            { TxtName.Clear(); TxtSource.Clear(); TxtTarget.Clear(); CmbType.SelectedIndex = 0; RefreshJobList(); }
        }

        private async void BtnExecuteSelected_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedJobs.Count == 0) { TxtStatus.Text = _lang.GetText("error_job_not_found"); return; }
            foreach (var job in new List<BackupJob>(_selectedJobs))
                await Task.Run(() => _viewModel.ExecuteJob(job));
            RefreshJobList();
        }

        private async void BtnExecuteAll_Click(object sender, RoutedEventArgs e)
        {
            await Task.Run(() => _viewModel.ExecuteAllJobs());
            RefreshJobList();
        }

        private void BtnDeleteSelected_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedJobs.Count == 0) { TxtStatus.Text = _lang.GetText("error_job_not_found"); return; }
            foreach (var job in new List<BackupJob>(_selectedJobs)) _viewModel.DeleteJob(job);
            RefreshJobList();
        }

        private async void ExecuteSingleJob(BackupJob job)
        {
            await Task.Run(() => _viewModel.ExecuteJob(job));
            Dispatcher.Invoke(RefreshJobList);
        }

        // ═══ LANGUAGE ═══

        private void BtnLangEn_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.SetLanguage(EasySave.Core.Models.Enums.Language.English);
            ApplyTranslations();
            RefreshJobList();
        }

        private void BtnLangFr_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.SetLanguage(EasySave.Core.Models.Enums.Language.French);
            ApplyTranslations();
            RefreshJobList();
        }

        // ═══ SETTINGS ═══

        private void TxtBusinessSoftware_TextChanged(object sender, TextChangedEventArgs e)
        { if (_viewModel != null) _viewModel.BusinessSoftwareName = TxtBusinessSoftware.Text.Trim(); }

        private void TxtEncryptionKey_LostFocus(object sender, RoutedEventArgs e)
        { if (_viewModel != null && !string.IsNullOrWhiteSpace(TxtEncryptionKey.Text)) _viewModel.EncryptionKey = TxtEncryptionKey.Text.Trim(); }

        private void TxtEncryptionExtensions_LostFocus(object sender, RoutedEventArgs e)
        { if (_viewModel != null && !string.IsNullOrWhiteSpace(TxtEncryptionExtensions.Text)) _viewModel.EncryptionExtensionsText = TxtEncryptionExtensions.Text.Trim(); }

        // ═══ UI STATE ═══

        private void UpdateWarning()
        {
            if (_viewModel.IsBusinessSoftwareDetected)
            {
                WarningBanner.Visibility = Visibility.Visible;
                WarningText.Text = "⚠ " + _lang.GetText("error_business_software");
                MonitorDot.Fill = new SolidColorBrush(C(_theme.Danger));
                LblMonitorStatus.Text = _lang.GetText("wpf_monitor_detected");
                LblMonitorStatus.Foreground = new SolidColorBrush(C(_theme.Danger));
            }
            else
            {
                WarningBanner.Visibility = Visibility.Collapsed;
                MonitorDot.Fill = new SolidColorBrush(C(_theme.Success));
                LblMonitorStatus.Text = _lang.GetText("wpf_monitor_not_detected");
                LblMonitorStatus.Foreground = new SolidColorBrush(C(_theme.Success));
            }
            UpdateButtons();
        }

        private void UpdateButtons()
        {
            bool can = _viewModel.CanExecute;
            BtnExecuteSelected.IsEnabled = can;
            BtnExecuteAll.IsEnabled = can;
        }

        protected override void OnClosed(EventArgs e) { _viewModel.Dispose(); base.OnClosed(e); }

        // ═══ HELPER ═══
        private static Color C(string hex) => (Color)ColorConverter.ConvertFromString(hex);

        // ═══ THEME DEFINITION ═══
        private record ThemeDef(
            string Name,
            string Bg,         // Window background
            string Sidebar,    // Sidebar / cards / inputs background
            string Text,       // Primary text
            string Soft,       // Secondary text (paths, descriptions)
            string Muted,      // Tertiary text (labels, headers)
            string Accent,     // Primary accent (buttons, badges, active)
            string AccentHover,// Accent hover state
            string BorderSoft, // Input borders
            string Border,     // Separators, card borders
            string Hover,      // Row hover bg
            string Select,     // Row selected bg
            string Success,    // Green indicators
            string Danger,     // Red indicators
            string GradEnd     // Progress bar gradient end
        );
    }
}
