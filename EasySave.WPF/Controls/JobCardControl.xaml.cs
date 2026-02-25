using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using EasySave.Core.Models;
using EasySave.Core.Models.Enums;
using EasySave.WPF.Helpers;

namespace EasySave.WPF.Controls
{
    // ===== JOB CARD =====
    // One card per backup job: name, type badge, source/target, and play/edit/delete actions.
    public partial class JobCardControl : UserControl
    {
        // ===== DEPENDENCY PROPERTIES =====
        public static readonly System.Windows.DependencyProperty JobProperty =
            System.Windows.DependencyProperty.Register(nameof(Job), typeof(BackupJob), typeof(JobCardControl),
                new PropertyMetadata(null, (d, e) => ((JobCardControl)d).OnJobChanged()));

        public static readonly System.Windows.DependencyProperty IsRunningProperty =
            System.Windows.DependencyProperty.Register(nameof(IsRunning), typeof(bool), typeof(JobCardControl),
                new PropertyMetadata(false, (d, _) => ((JobCardControl)d).UpdateBackground()));

        public static readonly System.Windows.DependencyProperty CanExecuteProperty =
            System.Windows.DependencyProperty.Register(nameof(CanExecute), typeof(bool), typeof(JobCardControl),
                new PropertyMetadata(true, (d, _) => ((JobCardControl)d).UpdatePlayButtonEnabled()));

        public static readonly System.Windows.DependencyProperty CanEditProperty =
            System.Windows.DependencyProperty.Register(nameof(CanEdit), typeof(bool), typeof(JobCardControl),
                new PropertyMetadata(true, (d, _) => ((JobCardControl)d).UpdateEditButtonEnabled()));

        public static readonly System.Windows.DependencyProperty CanDeleteProperty =
            System.Windows.DependencyProperty.Register(nameof(CanDelete), typeof(bool), typeof(JobCardControl),
                new PropertyMetadata(true, (d, _) => ((JobCardControl)d).UpdateDeleteButtonEnabled()));

        public BackupJob? Job
        {
            get => (BackupJob?)GetValue(JobProperty);
            set => SetValue(JobProperty, value);
        }

        public bool IsRunning
        {
            get => (bool)GetValue(IsRunningProperty);
            set => SetValue(IsRunningProperty, value);
        }

        public bool CanExecute
        {
            get => (bool)GetValue(CanExecuteProperty);
            set => SetValue(CanExecuteProperty, value);
        }

        public bool CanEdit
        {
            get => (bool)GetValue(CanEditProperty);
            set => SetValue(CanEditProperty, value);
        }

        public bool CanDelete
        {
            get => (bool)GetValue(CanDeleteProperty);
            set => SetValue(CanDeleteProperty, value);
        }

        public event EventHandler? PlayClick;
        public event EventHandler? EditClick;
        public event EventHandler? DeleteClick;

        private Button? _playButton;
        private Button? _editButton;
        private Button? _deleteButton;

        // ===== CONSTRUCTOR =====
        public JobCardControl()
        {
            InitializeComponent();
            Loaded += (_, _) => UpdateBackground();
        }

        private void OnJobChanged()
        {
            var job = Job;
            if (job == null) return;

            TxtName.Text = job.Name;
            bool isFull = job.Type == BackupType.Full;
            string typeText = isFull ? "Full" : "Differential";
            TxtType.Text = typeText;
            TxtSource.Text = job.SourceDirectory;
            TxtTarget.Text = job.TargetDirectory;

            var accent = ThemeColorsHelper.GetBrush(ThemeColorsHelper.AccentPrimary);
            var textSec = ThemeColorsHelper.GetBrush(ThemeColorsHelper.TextSecondary);
            var danger = ThemeColorsHelper.GetBrush(ThemeColorsHelper.StatusDanger);

            TypeBadge.Background = new SolidColorBrush(Color.FromArgb(0x20, accent.Color.R, accent.Color.G, accent.Color.B));
            TxtType.Foreground = isFull ? accent : ThemeColorsHelper.GetBrush(ThemeColorsHelper.StatusSuccess);

            ActionsPanel.Children.Clear();
            _playButton = MkBtn("\u25B6", accent, CanExecute);
            _playButton.Click += (s, e) => { e.Handled = true; PlayClick?.Invoke(this, EventArgs.Empty); };
            ActionsPanel.Children.Add(_playButton);

            _editButton = MkBtn("\u270F", textSec, CanEdit);
            _editButton.Click += (s, e) => { e.Handled = true; EditClick?.Invoke(this, EventArgs.Empty); };
            ActionsPanel.Children.Add(_editButton);

            _deleteButton = MkBtn("\u2715", danger, CanDelete);
            _deleteButton.Click += (s, e) => { e.Handled = true; DeleteClick?.Invoke(this, EventArgs.Empty); };
            ActionsPanel.Children.Add(_deleteButton);

            CardBorder.MouseEnter += Card_MouseEnter;
            CardBorder.MouseLeave += Card_MouseLeave;
            UpdateBackground();
        }

        // ===== HOVER AND THEME =====
        private void Card_MouseEnter(object sender, MouseEventArgs e)
        {
            if (!IsRunning)
                CardBorder.Background = ThemeColorsHelper.GetBrush(ThemeColorsHelper.BgInput);
        }

        private void Card_MouseLeave(object sender, MouseEventArgs e)
        {
            UpdateBackground();
        }

        public void UpdateBackground()
        {
            CardBorder.Background = ThemeColorsHelper.GetBrush(IsRunning ? ThemeColorsHelper.BgCardRunning : ThemeColorsHelper.BgCard);
        }

        public void RefreshTheme()
        {
            UpdateBackground();
        }

        private void UpdatePlayButtonEnabled()
        {
            if (_playButton != null)
                _playButton.IsEnabled = CanExecute;
        }

        private void UpdateEditButtonEnabled()
        {
            if (_editButton != null)
                _editButton.IsEnabled = CanEdit;
        }

        private void UpdateDeleteButtonEnabled()
        {
            if (_deleteButton != null)
                _deleteButton.IsEnabled = CanDelete;
        }

        public void SetTypeLabel(string fullText, string differentialText)
        {
            if (Job == null) return;
            TxtType.Text = Job.Type == BackupType.Full ? fullText : differentialText;
        }

        // ===== HELPERS =====
        private static Button MkBtn(string icon, SolidColorBrush foreground, bool enabled) => new()
        {
            Content = icon,
            Style = (Style)Application.Current.FindResource("BtnIcon"),
            Foreground = foreground,
            FontSize = 12,
            IsEnabled = enabled,
            Margin = new Thickness(2, 0, 2, 0)
        };
    }
}
