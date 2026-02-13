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
    /// <summary>
    /// MainView for EasySave V2.0
    /// </summary>
    public partial class MainView : Window
    {
        // ===== PRIVATE MEMBERS =====
        private readonly WpfViewModel _viewModel;
        private readonly LanguageManager _lang;
        private readonly HashSet<BackupJob> _selectedJobs = new();

        // ===== CONSTRUCTOR =====
        public MainView()
        {
            InitializeComponent();

            // Initialize ViewModel
            _lang = new LanguageManager();
            _viewModel = new WpfViewModel(_lang);

            // Subscribe to property changes for UI updates
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
                            UpdateBusinessSoftwareWarning();
                            break;

                        case nameof(WpfViewModel.CanExecute):
                            UpdateExecuteButtons();
                            break;
                    }
                });
            };

            // --- Load initial values from ViewModel into UI ---
            TxtBusinessSoftware.Text = _viewModel.BusinessSoftwareName;
            TxtEncryptionKey.Text = _viewModel.EncryptionKey;
            TxtEncryptionExtensions.Text = _viewModel.EncryptionExtensionsText;

            // Initial UI refresh
            RefreshJobList();
            UpdateBusinessSoftwareWarning();
            UpdateExecuteButtons();
        }

        // ===== JOB LIST RENDERING =====

        private void RefreshJobList()
        {
            JobListPanel.Children.Clear();
            _selectedJobs.Clear();

            var jobs = _viewModel.Jobs;

            if (jobs.Count == 0)
            {
                var noJobs = new TextBlock
                {
                    Text = _lang.GetText("no_jobs"),
                    Foreground = new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66)),
                    FontStyle = FontStyles.Italic,
                    FontSize = 14,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 40, 0, 0)
                };
                JobListPanel.Children.Add(noJobs);
            }
            else
            {
                for (int i = 0; i < jobs.Count; i++)
                {
                    var row = CreateJobRow(jobs[i], i + 1);
                    JobListPanel.Children.Add(row);
                }
            }

            TxtJobCount.Text = $"{jobs.Count} job(s)";
        }

        private Border CreateJobRow(BackupJob job, int index)
        {
            string typeText = job.Type == BackupType.Full
                ? _lang.GetText("type_full")
                : _lang.GetText("type_differential");

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.5, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.5, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });

            // Index
            var indexText = new TextBlock
            {
                Text = index.ToString(),
                Foreground = Brushes.Gray,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 13
            };
            Grid.SetColumn(indexText, 0);
            grid.Children.Add(indexText);

            // Name
            var nameText = new TextBlock
            {
                Text = job.Name,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold
            };
            Grid.SetColumn(nameText, 1);
            grid.Children.Add(nameText);

            // Source
            var sourceText = new TextBlock
            {
                Text = job.SourceDirectory,
                Foreground = new SolidColorBrush(Color.FromRgb(0xA0, 0xA0, 0xB8)),
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 12,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            Grid.SetColumn(sourceText, 2);
            grid.Children.Add(sourceText);

            // Target
            var targetText = new TextBlock
            {
                Text = job.TargetDirectory,
                Foreground = new SolidColorBrush(Color.FromRgb(0xA0, 0xA0, 0xB8)),
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 12,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            Grid.SetColumn(targetText, 3);
            grid.Children.Add(targetText);

            // Type badge
            var typeBadge = new Border
            {
                Background = job.Type == BackupType.Full
                    ? new SolidColorBrush(Color.FromArgb(0x33, 0x7C, 0x3A, 0xED))
                    : new SolidColorBrush(Color.FromArgb(0x33, 0x05, 0x96, 0x69)),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8, 2, 8, 2),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            typeBadge.Child = new TextBlock
            {
                Text = typeText,
                Foreground = job.Type == BackupType.Full
                    ? new SolidColorBrush(Color.FromRgb(0x8B, 0x5C, 0xF6))
                    : new SolidColorBrush(Color.FromRgb(0x10, 0xB9, 0x81)),
                FontSize = 11,
                FontWeight = FontWeights.SemiBold
            };
            Grid.SetColumn(typeBadge, 4);
            grid.Children.Add(typeBadge);

            // Execute button
            var btnExec = new Button
            {
                Content = "▶",
                FontSize = 14,
                Padding = new Thickness(8, 4, 8, 4),
                Cursor = System.Windows.Input.Cursors.Hand,
                Background = new SolidColorBrush(Color.FromRgb(0x7C, 0x3A, 0xED)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                IsEnabled = _viewModel.CanExecute
            };
            btnExec.Click += (s, e) => ExecuteSingleJob(job);
            Grid.SetColumn(btnExec, 5);
            grid.Children.Add(btnExec);

            // Delete button
            var btnDel = new Button
            {
                Content = "✕",
                FontSize = 14,
                Padding = new Thickness(8, 4, 8, 4),
                Cursor = System.Windows.Input.Cursors.Hand,
                Background = new SolidColorBrush(Color.FromRgb(0xDC, 0x26, 0x26)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0)
            };
            btnDel.Click += (s, e) =>
            {
                _viewModel.DeleteJob(job);
                RefreshJobList();
            };
            Grid.SetColumn(btnDel, 6);
            grid.Children.Add(btnDel);

            // Row container
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x48)),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8, 10, 8, 10),
                Margin = new Thickness(0, 2, 0, 2),
                Child = grid,
                Cursor = System.Windows.Input.Cursors.Hand
            };

            // Click to toggle selection
            border.MouseLeftButtonDown += (s, e) =>
            {
                if (_selectedJobs.Contains(job))
                {
                    _selectedJobs.Remove(job);
                    border.Background = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x48));
                }
                else
                {
                    _selectedJobs.Add(job);
                    border.Background = new SolidColorBrush(Color.FromRgb(0x3D, 0x3D, 0x5C));
                }
            };

            return border;
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
                TxtStatus.Foreground = new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44));
                return;
            }

            bool created = _viewModel.CreateJob(name, source, target, typeInput);
            if (created)
            {
                TxtName.Clear();
                TxtSource.Clear();
                TxtTarget.Clear();
                CmbType.SelectedIndex = 0;
                TxtStatus.Foreground = new SolidColorBrush(Color.FromRgb(0x10, 0xB9, 0x81));
                RefreshJobList();
            }
            else
            {
                TxtStatus.Text = _lang.GetText("error_invalid_choice");
                TxtStatus.Foreground = new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44));
            }
        }

        private async void BtnExecuteSelected_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedJobs.Count == 0)
            {
                TxtStatus.Text = _lang.GetText("error_job_not_found");
                return;
            }

            var jobsToExecute = new List<BackupJob>(_selectedJobs);
            foreach (var job in jobsToExecute)
            {
                await Task.Run(() => _viewModel.ExecuteJob(job));
            }
            RefreshJobList();
        }

        private async void BtnExecuteAll_Click(object sender, RoutedEventArgs e)
        {
            await Task.Run(() => _viewModel.ExecuteAllJobs());
            RefreshJobList();
        }

        private void BtnDeleteSelected_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedJobs.Count == 0)
            {
                TxtStatus.Text = _lang.GetText("error_job_not_found");
                return;
            }

            var jobsToDelete = new List<BackupJob>(_selectedJobs);
            foreach (var job in jobsToDelete)
            {
                _viewModel.DeleteJob(job);
            }
            RefreshJobList();
        }

        private async void ExecuteSingleJob(BackupJob job)
        {
            await Task.Run(() => _viewModel.ExecuteJob(job));
            Dispatcher.Invoke(() => RefreshJobList());
        }

        // ===== LANGUAGE =====
        // Use FULL qualified enum to avoid conflict with System.Windows.Markup.XmlLanguage

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
            if (_viewModel != null)
            {
                _viewModel.BusinessSoftwareName = TxtBusinessSoftware.Text.Trim();
            }
        }

        // ===== CRYPTOSOFT SETTINGS =====

        private void TxtEncryptionKey_TextChanged(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.EncryptionKey = TxtEncryptionKey.Text.Trim();
            }
        }

        private void TxtEncryptionExtensions_TextChanged(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.EncryptionExtensionsText = TxtEncryptionExtensions.Text.Trim();
            }
        }

        // ===== UI UPDATES =====

        private void UpdateBusinessSoftwareWarning()
        {
            if (_viewModel.IsBusinessSoftwareDetected)
            {
                WarningBanner.Visibility = Visibility.Visible;
                WarningText.Text = _lang.GetText("error_business_software");
                LblMonitorStatus.Text = "● Detected";
                LblMonitorStatus.Foreground = new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44));
            }
            else
            {
                WarningBanner.Visibility = Visibility.Collapsed;
                LblMonitorStatus.Text = "● Not detected";
                LblMonitorStatus.Foreground = new SolidColorBrush(Color.FromRgb(0x05, 0x96, 0x69));
            }
            UpdateExecuteButtons();
        }

        private void UpdateExecuteButtons()
        {
            bool canExecute = _viewModel.CanExecute;
            BtnExecuteSelected.IsEnabled = canExecute;
            BtnExecuteAll.IsEnabled = canExecute;
        }

        // ===== CLEANUP =====
        protected override void OnClosed(EventArgs e)
        {
            _viewModel.Dispose();
            base.OnClosed(e);
        }
    }
}
