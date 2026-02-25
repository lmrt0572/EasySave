using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using EasyLog.Services;
using EasySave.Core.Models;
using EasySave.Core.Models.Enums;
using EasySave.Core.Services;
using EasySave.Core.Strategies;
using EasySave.WPF.Views;

[assembly: ThemeInfo(
    ResourceDictionaryLocation.None,
    ResourceDictionaryLocation.SourceAssembly)]

namespace EasySave.WPF
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            // Only reached in GUI mode (Program.Main calls Run only when args is empty).
            FreeConsole();
            new WelcomeView().Show();
        }

        // ===== CLI ARGS (fallback when not started from Program.Main) =====
        // Tries e.Args, then Environment.GetCommandLineArgs(), then parsing Environment.CommandLine.
        private static string[]? GetCliArgs(StartupEventArgs e)
        {
            if (e.Args != null && e.Args.Length > 0)
                return e.Args;
            string[] all = Environment.GetCommandLineArgs();
            if (all.Length > 1)
            {
                var args = new string[all.Length - 1];
                Array.Copy(all, 1, args, 0, args.Length);
                return args;
            }
            string raw = Environment.CommandLine?.Trim() ?? "";
            if (raw.Length == 0) return null;
            List<string> parsed = ParseCommandLine(raw);
            if (parsed.Count > 1)
            {
                parsed.RemoveAt(0); // drop exe path
                return parsed.ToArray();
            }
            return null;
        }

        // ===== COMMAND LINE PARSING =====
        // Splits a Windows command line (quotes, spaces) without using GetCommandLineArgs.
        private static List<string> ParseCommandLine(string commandLine)
        {
            var list = new List<string>();
            bool inQuotes = false;
            var current = new System.Text.StringBuilder();
            for (int i = 0; i < commandLine.Length; i++)
            {
                char c = commandLine[i];
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                    continue;
                }
                if (!inQuotes && (c == ' ' || c == '\t'))
                {
                    if (current.Length > 0) { list.Add(current.ToString()); current.Clear(); }
                    continue;
                }
                current.Append(c);
            }
            if (current.Length > 0)
                list.Add(current.ToString());
            return list;
        }

        // ===== CLI =====
        // Called by Program.Main when command-line args are provided.
        internal static void RunCli(string[] args)
        {
            Console.WriteLine();
            Console.WriteLine($"Executing jobs: {string.Join(" ", args)}");
            Console.WriteLine();

            // Load config (same path and options as WpfViewModel)
            var config = LoadConfig();

            if (config.Jobs.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("  No backup jobs configured.");
                Console.ResetColor();
                return;
            }

            // Parse arguments (1-3 | 1;3;5 | 2)
            var parser = new ServiceCommandLineParser();
            var indices = parser.Parse(args).ToList();

            if (parser.HasError)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  Error: {parser.ErrorMessage}");
                Console.ResetColor();
                if (!indices.Any()) return;
            }

            // Validate indices against loaded jobs
            var valid = new List<int>();
            var invalid = new List<int>();

            foreach (int i in indices)
            {
                if (i >= 0 && i < config.Jobs.Count) valid.Add(i);
                else invalid.Add(i + 1);
            }

            if (invalid.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"  Warning: Job(s) {string.Join(", ", invalid)} do not exist. (You have {config.Jobs.Count} job(s))");
                Console.ResetColor();
            }

            if (valid.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("  No valid jobs to execute.");
                Console.ResetColor();
                return;
            }

            // Apply log settings
            LogService.Instance.SetLogFormat(config.LogFormat);
            LogService.Instance.SetLogMode(config.LogMode);
            LogService.Instance.UpdateDockerUrl(config.DockerUrl);

            // Instantiate services (same as WpfViewModel)
            var stateService = new StateService();
            var encryptionService = new EncryptionService(
                exePath: Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CryptoSoft.exe"),
                key: config.EncryptionKey,
                extensions: config.EncryptionExtensions);

            // Run each valid job sequentially
            foreach (int i in valid)
            {
                var job = config.Jobs[i];

                try
                {
                    IBackupStrategy strategy = job.Type == BackupType.Full
                        ? new FullBackupStrategy()
                        : (IBackupStrategy)new DifferentialBackupStrategy();

                    using var context = new JobExecutionContext(job.Name)
                    {
                        LargeFileThresholdKo = config.LargeFileThresholdKo,
                        PriorityExtensions = config.PriorityExtensions
                    };

                    var execution = new ServiceBackupExecution(
                        strategy, LogService.Instance, stateService, encryptionService);

                    // Progress output in console
                    execution.StateUpdated += state =>
                        Console.Write($"\r  [{i + 1}] {job.Name} ... {state.Progression}%   ");

                    execution.Execute(job, context).GetAwaiter().GetResult();

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"\r  [{i + 1}] {job.Name} ... done        ");
                    Console.ResetColor();
                }
                catch (OperationCanceledException)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"\r  [{i + 1}] {job.Name} ... stopped");
                    Console.ResetColor();
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"\r  [{i + 1}] {job.Name} ... error: {ex.Message}");
                    Console.ResetColor();
                }
            }

            Console.WriteLine();
            Console.WriteLine("Backup completed");
        }

        // ===== CONFIG LOADING =====
        // Same path (%AppData%\EasySave\config.json) and serialization options as WpfViewModel.
        // WpfViewModel saves enums as numbers (no JsonStringEnumConverter); match that so
        // deserialization succeeds and config.Jobs is populated in CLI mode.
        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        private static AppConfig LoadConfig()
        {
            string configPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "EasySave", "config.json");

            if (!File.Exists(configPath))
                return new AppConfig();

            try
            {
                string json = File.ReadAllText(configPath).TrimStart();
                if (string.IsNullOrWhiteSpace(json))
                    return new AppConfig();

                return json.StartsWith("[")
                    ? new AppConfig { Jobs = JsonSerializer.Deserialize<List<BackupJob>>(json, _jsonOpts) ?? new() }
                    : JsonSerializer.Deserialize<AppConfig>(json, _jsonOpts) ?? new AppConfig();
            }
            catch
            {
                return new AppConfig();
            }
        }

        // ===== CONSOLE (GUI) =====
        // In GUI mode, hide console window (process is Exe so console is visible by default).
        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern bool AttachConsole(int dwProcessId);

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern bool AllocConsole();

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern bool FreeConsole();

        private static void AttachOrAllocConsole()
        {
            if (!AttachConsole(-1)) // -1 = ATTACH_PARENT_PROCESS
                AllocConsole();
        }
    }
}