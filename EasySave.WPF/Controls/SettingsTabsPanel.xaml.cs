using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using EasySave.WPF.Helpers;

namespace EasySave.WPF.Controls
{
    // ===== SETTINGS TABS PANEL =====
    // Tab bar and content for General, Logs, Language, and Theme; raises events for the parent to handle.
    public partial class SettingsTabsPanel : UserControl
    {
        private int _activeTabIndex;
        private bool _isKeyVisible;

        public event EventHandler<int>? TabClicked;
        public event EventHandler? BusinessSoftwareTextChanged;
        public event EventHandler? EncryptionKeyLostFocus;
        public event EventHandler? EncryptionExtensionsLostFocus;
        public event EventHandler? LargeFileThresholdLostFocus;
        public event EventHandler? PriorityExtensionsLostFocus;
        public event EventHandler? LogJsonClicked;
        public event EventHandler? LogXmlClicked;
        public event EventHandler? LogModeSelectionChanged;
        public event EventHandler? SettingsLangEnClicked;
        public event EventHandler? SettingsLangFrClicked;
        public event EventHandler<int>? ThemeSwatchClicked;

        public SettingsTabsPanel()
        {
            InitializeComponent();
        }

        public void SetActiveTab(int index)
        {
            _activeTabIndex = index;
            TabGeneralContent.Visibility = index == 0 ? Visibility.Visible : Visibility.Collapsed;
            TabLogsContent.Visibility = index == 1 ? Visibility.Visible : Visibility.Collapsed;
            TabLanguageContent.Visibility = index == 2 ? Visibility.Visible : Visibility.Collapsed;
            TabThemeContent.Visibility = index == 3 ? Visibility.Visible : Visibility.Collapsed;

            var accent = ThemeColorsHelper.GetBrush(ThemeColorsHelper.AccentPrimary);
            var textOnAccent = ThemeColorsHelper.GetBrush(ThemeColorsHelper.TextOnAccent);
            var muted = ThemeColorsHelper.GetBrush(ThemeColorsHelper.TextMuted);

            void StyleTab(Button btn, int i)
            {
                btn.Background = i == index ? accent : System.Windows.Media.Brushes.Transparent;
                btn.Foreground = i == index ? textOnAccent : muted;
            }
            StyleTab(BtnTabGeneral, 0);
            StyleTab(BtnTabLogs, 1);
            StyleTab(BtnTabLanguage, 2);
            StyleTab(BtnTabTheme, 3);
        }

        // ===== PROPERTIES (EXPOSED TO PARENT) =====
        public string BusinessSoftwareText { get => TxtBusinessSoftware.Text; set => TxtBusinessSoftware.Text = value; }
        public string EncryptionKeyText { get => TxtEncryptionKey.Text; set { TxtEncryptionKey.Text = value; PwdEncryptionKey.Password = value; } }
        public string PwdEncryptionPassword { get => PwdEncryptionKey.Password; set => PwdEncryptionKey.Password = value; }
        public string EffectiveEncryptionKey => _isKeyVisible ? TxtEncryptionKey.Text : PwdEncryptionKey.Password;
        public Visibility EncryptionKeyVisibility { get => TxtEncryptionKey.Visibility; set => TxtEncryptionKey.Visibility = value; }
        public Visibility PwdEncryptionVisibility { get => PwdEncryptionKey.Visibility; set => PwdEncryptionKey.Visibility = value; }
        public object? ToggleKeyContent { set => BtnToggleKeyVisibility.Content = value; }
        public string EncryptionExtensionsText { get => TxtEncryptionExtensions.Text; set => TxtEncryptionExtensions.Text = value; }
        public string LargeFileThresholdText { get => TxtLargeFileThreshold.Text; set => TxtLargeFileThreshold.Text = value; }
        public string PriorityExtensionsText { get => TxtPriorityExtensions.Text; set => TxtPriorityExtensions.Text = value; }
        public ComboBox LogModeCombo => CmbLogMode;
        public WrapPanel ThemeSwatches => ThemeSwatchPanel;
        public string CurrentThemeText { get => TxtCurrentTheme.Text; set => TxtCurrentTheme.Text = value; }

        public void SetMonitorStatus(string text, Brush brush)
        {
            LblMonitorStatus.Text = text;
            LblMonitorStatus.Foreground = brush;
            MonitorDot.Fill = brush;
        }

        public void SetLogFormatButtons(bool isJson)
        {
            var accent = ThemeColorsHelper.GetBrush(ThemeColorsHelper.AccentPrimary);
            var textOnAccent = ThemeColorsHelper.GetBrush(ThemeColorsHelper.TextOnAccent);
            var muted = ThemeColorsHelper.GetBrush(ThemeColorsHelper.TextMuted);
            BtnLogJson.Background = isJson ? accent : Brushes.Transparent;
            BtnLogJson.Foreground = isJson ? textOnAccent : muted;
            BtnLogXml.Background = !isJson ? accent : Brushes.Transparent;
            BtnLogXml.Foreground = !isJson ? textOnAccent : muted;
        }

        public void SetLanguageButtons(bool isEn, FontWeight activeWeight, FontWeight inactiveWeight)
        {
            var accent = ThemeColorsHelper.GetBrush(ThemeColorsHelper.AccentPrimary);
            var textOnAccent = ThemeColorsHelper.GetBrush(ThemeColorsHelper.TextOnAccent);
            var muted = ThemeColorsHelper.GetBrush(ThemeColorsHelper.TextMuted);
            BtnSettingsLangEn.Background = isEn ? accent : Brushes.Transparent;
            BtnSettingsLangEn.Foreground = isEn ? textOnAccent : muted;
            BtnSettingsLangEn.FontWeight = isEn ? activeWeight : inactiveWeight;
            BtnSettingsLangFr.Background = !isEn ? accent : Brushes.Transparent;
            BtnSettingsLangFr.Foreground = !isEn ? textOnAccent : muted;
            BtnSettingsLangFr.FontWeight = !isEn ? activeWeight : inactiveWeight;
        }

        public void SetThemeSwatchBorder(int selectedIndex)
        {
            var accent = ThemeColorsHelper.GetBrush(ThemeColorsHelper.AccentPrimary);
            for (int i = 0; i < ThemeSwatchPanel.Children.Count; i++)
            {
                if (ThemeSwatchPanel.Children[i] is Border bd)
                    bd.BorderBrush = i == selectedIndex ? accent : Brushes.Transparent;
            }
        }

        /// <summary>Applies translations via getText(key).</summary>
        public void SetLabels(Func<string, string> getText)
        {
            BtnTabGeneral.Content = getText("settings_tab_general");
            BtnTabLogs.Content = getText("settings_tab_logs");
            BtnTabLanguage.Content = getText("settings_tab_language");
            BtnTabTheme.Content = getText("settings_tab_theme");
            LblSettingsBusiness.Text = getText("settings_business_software");
            LblSettingsBusinessDesc.Text = getText("settings_business_desc");
            LblSettingsEncryption.Text = getText("settings_encryption");
            LblSettingsEncKey.Text = getText("settings_encryption_key");
            LblSettingsEncExt.Text = getText("settings_encryption_ext");
            LblSettingsLargeFile.Text = getText("settings_large_file");
            LblSettingsLargeFileDesc.Text = getText("settings_large_file_desc");
            LblSettingsPriorityExt.Text = getText("settings_priority_ext");
            LblSettingsPriorityExtDesc.Text = getText("settings_priority_ext_desc");
            LblSettingsLogFormat.Text = getText("settings_log_format");
            LblSettingsLogDesc.Text = getText("settings_log_desc");
            LblSettingsLogMode.Text = getText("settings_log_mode_title");
            LblSettingsLogModeDesc.Text = getText("settings_log_mode_desc");
            LblSettingsLangTitle.Text = getText("settings_language_title");
            LblSettingsLangDesc.Text = getText("settings_language_desc");
            LblSettingsThemeTitle.Text = getText("settings_theme_title");
            LblSettingsThemeDesc.Text = getText("settings_theme_desc");
            if (CmbLogMode.Items.Count >= 3)
            {
                ((ComboBoxItem)CmbLogMode.Items[0]).Content = getText("log_mode_local");
                ((ComboBoxItem)CmbLogMode.Items[1]).Content = getText("log_mode_centralized");
                ((ComboBoxItem)CmbLogMode.Items[2]).Content = getText("log_mode_both");
            }
        }

        // ===== EVENT HANDLERS (FORWARD TO PARENT) =====
        private void BtnTabGeneral_Click(object sender, RoutedEventArgs e) => TabClicked?.Invoke(this, 0);
        private void BtnTabLogs_Click(object sender, RoutedEventArgs e) => TabClicked?.Invoke(this, 1);
        private void BtnTabLanguage_Click(object sender, RoutedEventArgs e) => TabClicked?.Invoke(this, 2);
        private void BtnTabTheme_Click(object sender, RoutedEventArgs e) => TabClicked?.Invoke(this, 3);

        private void TxtBusinessSoftware_TextChanged(object sender, TextChangedEventArgs e) => BusinessSoftwareTextChanged?.Invoke(this, EventArgs.Empty);
        private void TxtEncryptionKey_LostFocus(object sender, RoutedEventArgs e) => EncryptionKeyLostFocus?.Invoke(this, EventArgs.Empty);
        private void PwdEncryptionKey_LostFocus(object sender, RoutedEventArgs e) => EncryptionKeyLostFocus?.Invoke(this, EventArgs.Empty);
        private void TxtEncryptionExtensions_LostFocus(object sender, RoutedEventArgs e) => EncryptionExtensionsLostFocus?.Invoke(this, EventArgs.Empty);
        private void TxtLargeFileThreshold_LostFocus(object sender, RoutedEventArgs e) => LargeFileThresholdLostFocus?.Invoke(this, EventArgs.Empty);
        private void TxtPriorityExtensions_LostFocus(object sender, RoutedEventArgs e) => PriorityExtensionsLostFocus?.Invoke(this, EventArgs.Empty);
        // Toggles between password box and plain text box for the encryption key.
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
        private void BtnLogJson_Click(object sender, RoutedEventArgs e) => LogJsonClicked?.Invoke(this, EventArgs.Empty);
        private void BtnLogXml_Click(object sender, RoutedEventArgs e) => LogXmlClicked?.Invoke(this, EventArgs.Empty);
        private void CmbLogMode_SelectionChanged(object sender, SelectionChangedEventArgs e) => LogModeSelectionChanged?.Invoke(this, EventArgs.Empty);
        private void BtnSettingsLangEn_Click(object sender, RoutedEventArgs e) => SettingsLangEnClicked?.Invoke(this, EventArgs.Empty);
        private void BtnSettingsLangFr_Click(object sender, RoutedEventArgs e) => SettingsLangFrClicked?.Invoke(this, EventArgs.Empty);

        // Extracts the theme index from the swatch border tag and raises ThemeSwatchClicked with that index.
        public void OnThemeSwatchClicked(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border bd && bd.Tag is int idx)
                ThemeSwatchClicked?.Invoke(this, idx);
        }
    }
}
