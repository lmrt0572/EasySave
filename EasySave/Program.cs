using System;
using EasySave.Localization;
using EasySave.ViewModels;
using EasySave.Views;

namespace EasySave
{
    // ===== PROGRAM =====
    class Program
    {
        // ===== MAIN =====
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
                    RunCliMode(viewModel, args);
                }
                else
                {
                    view.Run();
                }
            }
            // ===== ERROR HANDLING =====
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Fatal error: {ex.Message}");
                Console.ResetColor();
                Environment.Exit(1);
            }
        }

        // ===== CLI MODE =====
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