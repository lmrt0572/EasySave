using System;
using EasySave.Localization;
using EasySave.ViewModels;
using EasySave.Views;

namespace EasySave
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                // ===== INITIALIZATION =====
                var languageManager = new LanguageManager();
                var viewModel = new MainViewModel(languageManager);
                var view = new ConsoleView(viewModel);

                // ===== MODE SELECTION =====
                if (args.Length > 0)
                {
                    // CLI Mode: EasySave.exe 1-3 or EasySave.exe 1;3;5
                    RunCliMode(viewModel, args);
                }
                else
                {
                    // Interactive Mode: Menu
                    view.Run();
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Fatal error: {ex.Message}");
                Console.ResetColor();
                Environment.Exit(1);
            }
        }

        static void RunCliMode(MainViewModel viewModel, string[] args)
        {
            var lang = viewModel.GetLanguageManager();

            Console.WriteLine();
            Console.WriteLine($"Executing jobs: {args[0]}");
            Console.WriteLine();

            viewModel.RunCli(args);

            Console.WriteLine();
            Console.WriteLine(lang.GetText("job_executed"));
        }
    }
}