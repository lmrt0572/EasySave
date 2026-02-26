using System.Windows;

namespace EasySave.WPF.Views
{
    // ===== WELCOME SCREEN =====
    public partial class WelcomeView : Window
    {
        // ===== CONSTRUCTOR =====
        public WelcomeView()
        {
            InitializeComponent();
        }

        // ===== LAUNCH =====
        // Opens the main window and closes the welcome screen.
        private void BtnLaunch_Click(object sender, RoutedEventArgs e)
        {
            var mainView = new MainView();
            mainView.Show();
            Close();
        }
    }
}
