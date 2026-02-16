using System;
using System.Threading.Tasks;
using EasySave.Core.Models.Enums;
using EasySave.Core.ViewModels;
using EasySave.Cmd.Views;

namespace EasySave.Cmd
{
    // ===== PROGRAM =====
    class Program
    {
        // ===== MAIN =====
        static async Task Main(string[] args)
        {
            try
            {
                // ===== INITIALIZATION =====
                var languageManager = new LanguageManager();
                var viewModel = new MainViewModel(languageManager);
                var view = new CmdView(viewModel);

                // ===== MODE SELECTION =====
                if (args.Length > 0)
                {
                    await RunCliMode(viewModel, args);
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
        static async Task RunCliMode(MainViewModel viewModel, string[] args)
        {
            var lang = viewModel.GetLanguageManager();

            Console.WriteLine();
            Console.WriteLine($"Executing jobs: {string.Join(" ", args)}");
            Console.WriteLine();

            await viewModel.RunCli(args);

            Console.WriteLine();
            Console.WriteLine(lang.GetText("job_executed") ?? "Execution finished.");
        }
    }
}