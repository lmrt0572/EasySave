using System;
using System.Collections.Generic;
using EasySave.Localization;
using EasySave.Models;
using EasySave.Models.Enums;
using EasySave.ViewModels;

namespace EasySave.Views
{
    // ===== CONSOLE VIEW =====
    public class ConsoleView
    {
        // ===== CONSTANTS =====
        private const int MenuOptionCount = 6;
        private const int BoxWidth = 50;

        // ===== PRIVATE MEMBERS =====
        private readonly MainViewModel _viewModel;
        private readonly LanguageManager _lang;

        // ===== CONSTRUCTOR =====
        public ConsoleView(MainViewModel viewModel)
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            _lang = viewModel.GetLanguageManager();
        }

        // ===== MAIN MENU LOOP =====

        public void Run()
        {
            int selected = 0;

            while (true)
            {
                DrawMenu(selected);

                var key = Console.ReadKey(intercept: true);

                // ----- Arrow keys: move selection
                if (key.Key == ConsoleKey.UpArrow || IsEscapeSequenceUp(key))
                {
                    selected = (selected - 1 + MenuOptionCount) % MenuOptionCount;
                    continue;
                }
                if (key.Key == ConsoleKey.DownArrow || IsEscapeSequenceDown(key))
                {
                    selected = (selected + 1) % MenuOptionCount;
                    continue;
                }

                // ----- Enter: validate current selection
                if (key.Key == ConsoleKey.Enter)
                {
                    if (ExecuteMenuAction(selected))
                        return; // Quit chosen
                    continue;
                }

                // ----- Number keys 1-6: direct selection
                if (key.KeyChar >= '1' && key.KeyChar <= '6')
                {
                    int index = key.KeyChar - '1';
                    if (ExecuteMenuAction(index))
                        return;
                }
            }
        }

        private bool ExecuteMenuAction(int index)
        {
            Console.Clear();
            try
            {
                switch (index)
                {
                    case 0: RunCreateJob(); break;
                    case 1: DisplayJobList(); break;
                    case 2: RunExecuteJob(); break;
                    case 3: RunDeleteJob(); break;
                    case 4: RunChangeLanguage(); break;
                    case 5: return true; // Quit
                }
            }
            catch (NotImplementedException)
            {
                DisplayMessage("not_implemented");
            }
            catch (Exception ex)
            {
                DisplayError("error_generic");
                DisplayMessage(ex.Message, useTranslation: false);
            }

            if (index != 5)
                WaitForKey();
            return index == 5;
        }

        // ===== MENU DRAWING =====

        private void DrawMenu(int selectedIndex)
        {
            Console.Clear();

            // ----- Title
            Console.WriteLine();
            DrawCenteredText("menu_title");
            Console.WriteLine();

            // ----- Top border of box
            DrawSeparator('=');

            // ----- Menu options (numbered 1-6)
            string[] menuKeys = new string[]
            {
                "menu_create",
                "menu_list",
                "menu_execute",
                "menu_delete",
                "menu_language",
                "menu_quit"
            };

            for (int i = 0; i < menuKeys.Length; i++)
            {
                DrawMenuItem(menuKeys[i], i == selectedIndex);
            }

            // ----- Bottom border and hint
            DrawSeparator('=');
            Console.WriteLine();
        }

        private void DrawMenuItem(string textKey, bool isSelected)
        {
            string label = _lang.GetText(textKey);
            
            if (isSelected)
            {
                var bg = Console.BackgroundColor;
                var fg = Console.ForegroundColor;
                Console.BackgroundColor = ConsoleColor.DarkBlue;
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("  >> " + label);
                Console.BackgroundColor = bg;
                Console.ForegroundColor = fg;
            }
            else
            {
                Console.WriteLine("     " + label);
            }
        }

        private void DrawCenteredText(string textKey)
        {
            string text = _lang.GetText(textKey);
            int pad = Math.Max(0, (BoxWidth - text.Length) / 2);
            Console.WriteLine(new string(' ', pad) + text);
        }

        private void DrawSeparator(char character = '-', int width = BoxWidth - 2)
        {
            Console.WriteLine("  " + new string(character, width));
        }

        // ===== JOB LISTS =====

        public void DisplayJobList()
        {
            Console.WriteLine();
            var jobs = _viewModel.GetAllJobs();

            if (jobs.Count == 0)
            {
                DisplayMessage("no_jobs");
                Console.WriteLine();
                return;
            }

            DrawSeparator('-', 48);
            
            for (int i = 0; i < jobs.Count; i++)
            {
                DisplayJobSummary(jobs[i], i + 1);
            }
            
            DrawSeparator('-', 48);
            Console.WriteLine();
        }

        private void DisplayJobSummary(BackupJob job, int index)
        {
            string typeKey = job.Type == BackupType.Full ? "type_full" : "type_differential";
            string typeText = _lang.GetText(typeKey);
            
            Console.WriteLine($"  {index}. {job.Name} [{typeText}]");
            Console.WriteLine($"     {job.SourceDirectory} → {job.TargetDirectory}");
        }

        public void DisplayJobDetails(BackupJob? job)
        {
            if (job == null)
            {
                DisplayError("error_job_not_found");
                return;
            }

            string typeKey = job.Type == BackupType.Full ? "type_full" : "type_differential";
            string typeText = _lang.GetText(typeKey);

            Console.WriteLine();
            DisplayMessage("job_details_header");
            DrawSeparator('-', 28);
            
            DisplayLabelValue("job_name", job.Name);
            DisplayLabelValue("job_source", job.SourceDirectory);
            DisplayLabelValue("job_target", job.TargetDirectory);
            DisplayLabelValue("job_type", typeText);
            
            DrawSeparator('-', 28);
            Console.WriteLine();
        }

        // ===== USER INPUTS =====

        private void RunCreateJob()
        {
            string name = PromptUser("prompt_name").Trim();
            if (string.IsNullOrEmpty(name))
            {
                DisplayError("error_invalid_choice");
                return;
            }

            string source = PromptUser("prompt_source").Trim();
            string target = PromptUser("prompt_target").Trim();
            string typeInput = PromptUser("prompt_type").Trim();

            if (typeInput != "1" && typeInput != "2")
            {
                DisplayError("error_invalid_choice");
                return;
            }

            bool created = _viewModel.CreateJob(name, source, target, int.Parse(typeInput));
            if (!created)
            {
                // Specific feedback requested: max jobs reached
                if (_viewModel.GetJobCount() >= 5)
                {
                    DisplayError("error_max_jobs");
                    return;
                }

                DisplayError("error_invalid_choice");
                return;
            }

            DisplaySuccess("job_created");
        }

        private void RunExecuteJob()
        {
            if (!TryGetJobIndex(out int jobIndex))
                return;

            _viewModel.ExecuteJob(jobIndex);
            DisplaySuccess("job_executed");
        }

        private void RunDeleteJob()
        {
            if (!TryGetJobIndex(out int jobIndex))
                return;

            _viewModel.DeleteJob(jobIndex);
            DisplaySuccess("job_deleted");
        }

        private bool TryGetJobIndex(out int jobIndex)
        {
            jobIndex = 0;
            string input = PromptUser("prompt_job_index").Trim();

            if (!int.TryParse(input, out jobIndex) || jobIndex < 1 || jobIndex > 5)
            {
                DisplayError("error_invalid_choice");
                return false;
            }

            return true;
        }

        private void RunChangeLanguage()
        {
            var current = _lang.GetCurrentLanguage();
            var newLanguage = current == EasySave.Localization.Language.English
                ? EasySave.Localization.Language.French
                : EasySave.Localization.Language.English;
            
            _lang.SetLanguage(newLanguage);
            DisplaySuccess("language_changed");
        }

        // ===== DISPLAY HELPERS =====

        private void DisplayMessage(string textKey, bool useTranslation = true)
        {
            string message = useTranslation ? _lang.GetText(textKey) : textKey;
            Console.WriteLine("  " + message);
        }

        private void DisplayLabelValue(string labelKey, string value)
        {
            Console.WriteLine("  " + _lang.GetText(labelKey) + value);
        }

        private void DisplaySuccess(string textKey)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            DisplayMessage(textKey);
            Console.ResetColor();
        }

        public void DisplayError(string textKey)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            DisplayMessage(textKey);
            Console.ResetColor();
        }

        // ===== INPUT HELPERS =====

        private string PromptUser(string promptKey)
        {
            Console.Write("  " + _lang.GetText(promptKey));
            return Console.ReadLine() ?? string.Empty;
        }

        public void WaitForKey()
        {
            Console.WriteLine();
            Console.ReadKey(intercept: true);
        }

        // ===== ESCAPE SEQUENCE DETECTION =====

        private static bool IsEscapeSequenceUp(ConsoleKeyInfo key)
        {
            if (key.Key != ConsoleKey.Escape || !Console.KeyAvailable)
                return false;

            var c1 = Console.ReadKey(true);
            if (!Console.KeyAvailable)
                return false;

            var c2 = Console.ReadKey(true);
            return c1.KeyChar == '[' && c2.KeyChar == 'A';
        }

        private static bool IsEscapeSequenceDown(ConsoleKeyInfo key)
        {
            if (key.Key != ConsoleKey.Escape || !Console.KeyAvailable)
                return false;

            var c1 = Console.ReadKey(true);
            if (!Console.KeyAvailable)
                return false;

            var c2 = Console.ReadKey(true);
            return c1.KeyChar == '[' && c2.KeyChar == 'B';
        }
    }
}