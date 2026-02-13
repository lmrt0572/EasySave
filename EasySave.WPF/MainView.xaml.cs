using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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

        // ===== THEME COLORS (change these to test other themes) =====
        private static readonly Color ClrText = (Color)ColorConverter.ConvertFromString("#3D2B1F");
        private static readonly Color ClrTextSoft = (Color)ColorConverter.ConvertFromString("#8B7355");
        private static readonly Color ClrAccent = (Color)ColorConverter.ConvertFromString("#6B4C3B");
        private static readonly Color ClrBg = (Color)ColorConverter.ConvertFromString("#FAF6F1");
        private static readonly Color ClrRowBg = (Color)ColorConverter.ConvertFromString("#FDFCFA");
        private static readonly Color ClrRowHover = (Color)ColorConverter.ConvertFromString("#F5EDE4");
        private static readonly Color ClrRowSelect = (Color)ColorConverter.ConvertFromString("#EDE3D6");
        private static readonly Color ClrBorder = (Color)ColorConverter.ConvertFromString("#E8DFD4");
        private static readonly Color ClrSuccess = (Color)ColorConverter.ConvertFromString("#5A7247");
        private static readonly Color ClrDanger = (Color)ColorConverter.ConvertFromString("#9B2C2C");
        private static readonly Color ClrBadgeFull = (Color)ColorConverter.ConvertFromString("#6B4C3B");
        private static readonly Color ClrBadgeDiff = (Color)ColorConverter.ConvertFromString("#5A7247");

        public MainView()
        {
            InitializeComponent();

            _lang = new LanguageManager();
            _viewModel = new WpfViewModel(_lang);

            _viewModel.PropertyChanged += (s, e) =>
            {
                Dispatcher.Invoke(() =>
                {
                    switch (e.PropertyName)
                    {
                        case nameof(WpfViewModel.StatusMessage):
                            TxtStatus.Text = _viewModel.StatusMessage;
                            break;
                        case nameof(WpfViewModel.IsBusinessSoftwareDetected):
                            UpdateWarning();
                            break;
                        case nameof(WpfViewModel.CanExecute):
                            UpdateButtons();
                            break;
                        case nameof(WpfViewModel.IsExecuting):
                            ProgressPanel.Visibility = _viewModel.IsExecuting ? Visibility.Visible : Visibility.Collapsed;
                            break;
                        case nameof(WpfViewModel.ProgressPercent):
                            UpdateProgress();
                            break;
                        case nameof(WpfViewModel.ProgressText):
                            TxtProgressFiles.Text = _viewModel.ProgressText;
                            break;
                        case nameof(WpfViewModel.CurrentJobName):
                            TxtProgressJob.Text = _viewModel.CurrentJobName;
                            break;
                        case nameof(WpfViewModel.CurrentFileName):
                            TxtProgressFile.Text = _viewModel.CurrentFileName;
                            break;
                    }
                });
            };

            TxtBusinessSoftware.Text = _viewModel.BusinessSoftwareName;
            TxtEncryptionKey.Text = _viewModel.EncryptionKey;
            TxtEncryptionExtensions.Text = _viewModel.EncryptionExtensionsText;

            RefreshJobList();
            UpdateWarning();
            UpdateButtons();
        }

        // ===== PROGRESS =====

        private void UpdateProgress()
        {
            int pct = _viewModel.ProgressPercent;
            TxtProgressPct.Text = $"{pct}%";

            // Animate the fill bar width relative to parent
            double parentWidth = ProgressFill.Parent is Border parent ? parent.ActualWidth : 400;
            ProgressFill.Width = Math.Max(0, parentWidth * pct / 100.0);
        }

        // ===== JOB LIST =====

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
                    Foreground = new SolidColorBrush(ClrTextSoft),
                    FontStyle = FontStyles.Italic,
                    FontSize = 13,
                    FontFamily = new FontFamily("Segoe UI"),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 36, 0, 0)
                });
            }
            else
            {
                for (int i = 0; i < jobs.Count; i++)
                    JobListPanel.Children.Add(CreateJobRow(jobs[i], i + 1));
            }

            TxtJobCount.Text = $"{jobs.Count} job(s)";
        }

        private Border CreateJobRow(BackupJob job, int index)
        {
            bool isFull = job.Type == BackupType.Full;
            string typeText = isFull ? _lang.GetText("type_full") : _lang.GetText("type_differential");
            Color badgeColor = isFull ? ClrBadgeFull : ClrBadgeDiff;

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(36) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.4, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.4, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });

            // #
            AddText(grid, 0, index.ToString(), ClrTextSoft, 12);
            // Name
            AddText(grid, 1, job.Name, ClrText, 13, true);
            // Source
            AddText(grid, 2, job.SourceDirectory, ClrTextSoft, 11.5, false, true);
            // Target
            AddText(grid, 3, job.TargetDirectory, ClrTextSoft, 11.5, false, true);

            // Type badge
            var badge = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(0x18, badgeColor.R, badgeColor.G, badgeColor.B)),
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(8, 3, 8, 3),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left,
                Child = new TextBlock
                {
                    Text = typeText,
                    Foreground = new SolidColorBrush(badgeColor),
                    FontSize = 11,
                    FontWeight = FontWeights.SemiBold,
                    FontFamily = new FontFamily("Segoe UI")
                }
            };
            Grid.SetColumn(badge, 4);
            grid.Children.Add(badge);

            // Action buttons
            var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };

            var btnExec = new Button
            {
                Content = "▶",
                Style = (Style)FindResource("BtnIcon"),
                Foreground = new SolidColorBrush(ClrAccent),
                IsEnabled = _viewModel.CanExecute
            };
            btnExec.Click += (s, e) => ExecuteSingleJob(job);
            actions.Children.Add(btnExec);

            var btnDel = new Button
            {
                Content = "✕",
                Style = (Style)FindResource("BtnIcon"),
                Foreground = new SolidColorBrush(ClrDanger)
            };
            btnDel.Click += (s, e) => { _viewModel.DeleteJob(job); RefreshJobList(); };
            actions.Children.Add(btnDel);

            Grid.SetColumn(actions, 5);
            grid.Children.Add(actions);

            // Row border
            var border = new Border
            {
                Background = new SolidColorBrush(ClrRowBg),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 10, 12, 10),
                Margin = new Thickness(0, 2, 0, 2),
                BorderBrush = new SolidColorBrush(Colors.Transparent),
                BorderThickness = new Thickness(1),
                Child = grid,
                Cursor = System.Windows.Input.Cursors.Hand
            };

            border.MouseEnter += (s, e) =>
            {
                if (!_selectedJobs.Contains(job))
                    border.Background = new SolidColorBrush(ClrRowHover);
            };
            border.MouseLeave += (s, e) =>
            {
                if (!_selectedJobs.Contains(job))
                    border.Background = new SolidColorBrush(ClrRowBg);
            };
            border.MouseLeftButtonDown += (s, e) =>
            {
                if (_selectedJobs.Contains(job))
                {
                    _selectedJobs.Remove(job);
                    border.Background = new SolidColorBrush(ClrRowBg);
                    border.BorderBrush = new SolidColorBrush(Colors.Transparent);
                }
                else
                {
                    _selectedJobs.Add(job);
                    border.Background = new SolidColorBrush(ClrRowSelect);
                    border.BorderBrush = new SolidColorBrush(ClrBorder);
                }
            };

            return border;
        }

        private void AddText(Grid grid, int col, string text, Color color, double size,
            bool bold = false, bool trim = false)
        {
            var tb = new TextBlock
            {
                Text = text,
                Foreground = new SolidColorBrush(color),
                FontSize = size,
                FontFamily = new FontFamily("Segoe UI"),
                VerticalAlignment = VerticalAlignment.Center
            };
            if (bold) tb.FontWeight = FontWeights.SemiBold;
            if (trim) tb.TextTrimming = TextTrimming.CharacterEllipsis;
            Grid.SetColumn(tb, col);
            grid.Children.Add(tb);
        }

        // ===== BUTTON HANDLERS =====

        private void BtnCreate_Click(object sender, RoutedEventArgs e)
        {
            string name = TxtName.Text.Trim();
            string source = TxtSource.Text.Trim();
            string target = TxtTarget.Text.Trim();
            int typeInput = CmbType.SelectedIndex == 1 ? 2 : 1;

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(target))
            {
                TxtStatus.Text = _lang.GetText("error_invalid_choice");
                return;
            }

            if (_viewModel.CreateJob(name, source, target, typeInput))
            {
                TxtName.Clear(); TxtSource.Clear(); TxtTarget.Clear(); CmbType.SelectedIndex = 0;
                RefreshJobList();
            }
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
            foreach (var job in new List<BackupJob>(_selectedJobs))
                _viewModel.DeleteJob(job);
            RefreshJobList();
        }

        private async void ExecuteSingleJob(BackupJob job)
        {
            await Task.Run(() => _viewModel.ExecuteJob(job));
            Dispatcher.Invoke(RefreshJobList);
        }

        // ===== LANGUAGE =====

        private void BtnLangEn_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.SetLanguage(EasySave.Core.Models.Enums.Language.English);
            RefreshJobList();
        }

        private void BtnLangFr_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.SetLanguage(EasySave.Core.Models.Enums.Language.French);
            RefreshJobList();
        }

        // ===== SETTINGS =====

        private void TxtBusinessSoftware_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_viewModel != null) _viewModel.BusinessSoftwareName = TxtBusinessSoftware.Text.Trim();
        }

        private void TxtEncryptionKey_LostFocus(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null && !string.IsNullOrWhiteSpace(TxtEncryptionKey.Text))
                _viewModel.EncryptionKey = TxtEncryptionKey.Text.Trim();
        }

        private void TxtEncryptionExtensions_LostFocus(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null && !string.IsNullOrWhiteSpace(TxtEncryptionExtensions.Text))
                _viewModel.EncryptionExtensionsText = TxtEncryptionExtensions.Text.Trim();
        }

        // ===== UI STATE =====

        private void UpdateWarning()
        {
            if (_viewModel.IsBusinessSoftwareDetected)
            {
                WarningBanner.Visibility = Visibility.Visible;
                WarningText.Text = _lang.GetText("error_business_software");
                MonitorDot.Background = new SolidColorBrush(ClrDanger);
                LblMonitorStatus.Text = "Detected";
                LblMonitorStatus.Foreground = new SolidColorBrush(ClrDanger);
            }
            else
            {
                WarningBanner.Visibility = Visibility.Collapsed;
                MonitorDot.Background = new SolidColorBrush(ClrSuccess);
                LblMonitorStatus.Text = "Not detected";
                LblMonitorStatus.Foreground = new SolidColorBrush(ClrSuccess);
            }
            UpdateButtons();
        }

        private void UpdateButtons()
        {
            bool can = _viewModel.CanExecute;
            BtnExecuteSelected.IsEnabled = can;
            BtnExecuteAll.IsEnabled = can;
        }

        protected override void OnClosed(EventArgs e)
        {
            _viewModel.Dispose();
            base.OnClosed(e);
        }
    }
}
