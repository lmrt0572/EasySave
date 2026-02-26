using System;
using System.Windows;

namespace EasySave.WPF
{
    // ===== ENTRY POINT =====
    // Single Main() avoids CS0017 (multiple entry points) with the WPF SDK-generated Main.
    // Receives command-line args (e.g. EasySave.exe 1, EasySave.exe 1-3, EasySave.exe 1 ;3) and runs CLI or GUI.
    public static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            if (args != null && args.Length > 0)
            {
                App.RunCli(args);
                return;
            }
            var app = new App();
            app.InitializeComponent();
            app.Run();
        }
    }
}
