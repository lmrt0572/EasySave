using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EasyLog.Models;
using EasySave.Core.Models;
using EasySave.Core.Models.Enums;
using EasySave.Core.ViewModels;

namespace EasySave.Cmd.Views
{
    // ===== CONSOLE VIEW =====
    public class CmdView
    {
        // ===== CONSTANTS =====
        private const int MenuOptionCount = 8;
        private const int BoxWidth = 50;

        // ===== PRIVATE MEMBERS =====
        private readonly MainViewModel _viewModel;
        private readonly LanguageManager _lang;

        // ===== CONSTRUCTOR =====
        public CmdView(MainViewModel viewModel)
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

                if (key.Key == ConsoleKey.UpArrow)
                {
                    selected = (selected - 1 + MenuOptionCount) % MenuOptionCount;
                    continue;
                }
                if (key.Key == ConsoleKey.DownArrow)
                {
                    selected = (selected + 1) % MenuOptionCount;
                    continue;
                }

                if (key.Key == ConsoleKey.Enter)
                {
                    if (ExecuteMenuAction(selected))
                        return;
                    continue;
                }

                if (key.KeyChar >= '1' && key.KeyChar <= '8')
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
                    case 4: RunSettings(); break;
                    case 5: RunChangeLanguage(); break;
                    case 6: RunChangeLogFormat(); break;
                    case 7: return true; // Quit
                }
            }
            catch (Exception ex)
            {
                DisplayError("error_generic");
                Console.WriteLine("  " + ex.Message);
            }

            if (index != 7)
                WaitForKey();
            return index == 7;
        }

        // ===== MENU DRAWING =====
        private void DrawMenu(int selectedIndex)
        {
            Console.Clear();
            Console.WriteLine();
            DrawCenteredText("menu_title");
            Console.WriteLine();
            DrawSeparator('=');

            string[] menuKeys = new string[]
            {
                "menu_create",
                "menu_list",
                "menu_execute",
                "menu_delete",
                "menu_settings",
                "menu_language",
                "menu_log_format",
                "menu_quit"
            };

            for (int i = 0; i < menuKeys.Length; i++)
            {
                DrawMenuItem(menuKeys[i], i == selectedIndex);
            }

            DrawSeparator('=');
            Console.WriteLine();
        }

        private void DrawMenuItem(string textKey, bool isSelected)
        {
            string label = _lang.GetText(textKey) ?? textKey;

            if (isSelected)
            {
                Console.BackgroundColor = ConsoleColor.DarkBlue;
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("  >> " + label);
                Console.ResetColor();
            }
            else
            {
                Console.WriteLine("     " + label);
            }
        }

        private void DrawCenteredText(string textKey)
        {
            string text = _lang.GetText(textKey) ?? textKey;
            int pad = Math.Max(0, (BoxWidth - text.Length) / 2);
            Console.WriteLine(new string(' ', pad) + text);
        }

        private void DrawSeparator(char character = '-', int width = BoxWidth - 2)
        {
            Console.WriteLine("  " + new string(character, width));
        }

        // ===== SETTINGS (feature/GUI) =====
        private void RunSettings()
        {
            Console.WriteLine();
            DisplayMessage("settings_title");
            Console.WriteLine();

            // 1. Key
            string currentKey = _viewModel.GetEncryptionKey();
            Console.WriteLine("  " + _lang.GetText("settings_current_key", currentKey));
            string newKey = PromptUser("settings_prompt_key").Trim();
            if (string.IsNullOrEmpty(newKey)) newKey = currentKey;

            // 2. Extensions
            string currentExts = string.Join(", ", _viewModel.GetEncryptionExtensions());
            Console.WriteLine("  " + _lang.GetText("settings_current_extensions", currentExts));
            string newExtsInput = PromptUser("settings_prompt_extensions").Trim();
            List<string> newExts = string.IsNullOrEmpty(newExtsInput)
                ? _viewModel.GetEncryptionExtensions()
                : newExtsInput.Split(',').Select(e => e.Trim()).ToList();

            // 3. Business Software
            string currentSoft = _viewModel.GetBusinessSoftware();
            Console.WriteLine("  " + _lang.GetText("settings_current_software", currentSoft));
            string newSoft = PromptUser("settings_prompt_software").Trim();
            if (string.IsNullOrEmpty(newSoft)) newSoft = currentSoft;

            _viewModel.UpdateSettings(newKey, newExts, newSoft);

            Console.WriteLine();
            DisplaySuccess("settings_success");
        }

        // ===== LOG FORMAT (dev) =====
        private void RunChangeLogFormat()
        {
            var currentFormat = _viewModel.GetCurrentLogFormat();
            string currentLabel = currentFormat == LogFormat.Json ? "JSON" : "XML";

            DisplayMessage($"Current format: {currentLabel}", useTranslation: false);

            string choice = PromptUser("prompt_log_format").Trim();

            if (choice == "1")
            {
                _viewModel.SetLogFormat(LogFormat.Json);
                string msg = _lang.GetText("log_format_changed", "JSON");
                DisplayMessage(msg, useTranslation: false);
            }
            else if (choice == "2")
            {
                _viewModel.SetLogFormat(LogFormat.Xml);
                string msg = _lang.GetText("log_format_changed", "XML");
                DisplayMessage(msg, useTranslation: false);
            }
            else
            {
                DisplayError("error_invalid_choice");
            }
        }

        // ===== JOB LISTS =====
        public void DisplayJobList()
        {
            Console.WriteLine();
            var jobs = _viewModel.GetAllJobs();

            if (jobs.Count == 0)
            {
                DisplayMessage("no_jobs");
                return;
            }

            DrawSeparator('-', 48);
            for (int i = 0; i < jobs.Count; i++)
            {
                DisplayJobSummary(jobs[i], i + 1);
            }
            DrawSeparator('-', 48);
        }

        private void DisplayJobSummary(BackupJob job, int index)
        {
            string typeKey = job.Type == BackupType.Full ? "type_full" : "type_differential";
            string typeText = _lang.GetText(typeKey) ?? typeKey;
            Console.WriteLine($"  {index}. {job.Name} [{typeText}]");
            Console.WriteLine($"     {job.SourceDirectory} → {job.TargetDirectory}");
        }

        // ===== USER INPUTS =====

        private void RunCreateJob()
        {
            string name = PromptUser("prompt_name").Trim();
            if (string.IsNullOrEmpty(name)) return;

            string source = PromptUser("prompt_source").Trim();
            string target = PromptUser("prompt_target").Trim();
            string typeInput = PromptUser("prompt_type").Trim();

            if (typeInput != "1" && typeInput != "2") return;

            if (_viewModel.CreateJob(name, source, target, int.Parse(typeInput)))
            {
                DisplaySuccess("job_created");
            }
        }

        private async void RunExecuteJob()
        {
            if (TryGetJobIndex(out int jobIndex))
            {
                await _viewModel.ExecuteJob(jobIndex);
                DisplaySuccess("job_executed");
            }
        }

        private void RunDeleteJob()
        {
            if (TryGetJobIndex(out int jobIndex))
            {
                _viewModel.DeleteJob(jobIndex);
                DisplaySuccess("job_deleted");
            }
        }

        private bool TryGetJobIndex(out int jobIndex)
        {
            jobIndex = 0;
            string input = PromptUser("prompt_job_index").Trim();
            return int.TryParse(input, out jobIndex) && jobIndex >= 1 && jobIndex <= 5;
        }

        private void RunChangeLanguage()
        {
            var current = _lang.GetCurrentLanguage();
            _lang.SetLanguage(current == Language.English ? Language.French : Language.English);
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

        private string PromptUser(string promptKey)
        {
            string label = _lang.GetText(promptKey) ?? promptKey;
            Console.Write("  " + label);
            return Console.ReadLine() ?? string.Empty;
        }

        public void WaitForKey()
        {
            Console.WriteLine();
            Console.WriteLine("  " + _lang.GetText("press_any_key"));
            Console.ReadKey(intercept: true);
        }
    }
}
