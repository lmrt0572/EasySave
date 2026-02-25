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

            if (e.Args.Length > 0)
            {
                // ===== CLI MODE =====
                AttachOrAllocConsole();
                RunCli(e.Args);
                Shutdown();
                return;
            }

            // ===== GUI MODE =====
            new WelcomeView().Show();
        }

        // ===== CLI =====
        private static void RunCli(string[] args)
        {
            Console.WriteLine();
            Console.WriteLine($"Executing jobs: {string.Join(" ", args)}");
            Console.WriteLine();

            // 1. Charger la config (même chemin + options que WpfViewModel)
            var config = LoadConfig();

            if (config.Jobs.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("  No backup jobs configured.");
                Console.ResetColor();
                return;
            }

            // 2. Parser les arguments (1-3 | 1;3;5 | 2)
            var parser = new ServiceCommandLineParser();
            var indices = parser.Parse(args).ToList();

            if (parser.HasError)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  Error: {parser.ErrorMessage}");
                Console.ResetColor();
                if (!indices.Any()) return;
            }

            // 3. Valider les indices par rapport aux jobs chargés
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

            // 4. Appliquer les paramètres de log
            LogService.Instance.SetLogFormat(config.LogFormat);
            LogService.Instance.SetLogMode(config.LogMode);
            LogService.Instance.UpdateDockerUrl(config.DockerUrl);

            // 5. Instancier les services (identiques à WpfViewModel)
            var stateService = new StateService();
            var encryptionService = new EncryptionService(
                exePath: Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CryptoSoft.exe"),
                key: config.EncryptionKey,
                extensions: config.EncryptionExtensions);

            // 6. Exécuter chaque job valide séquentiellement
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

                    // Affichage progression dans la console
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
        // Même logique que WpfViewModel.LoadConfig() :
        // - même chemin (%AppData%\EasySave\config.json)
        // - PropertyNameCaseInsensitive pour lire les champs LogMode, DockerUrl, etc.
        // - JsonStringEnumConverter pour désérialiser LogMode/LogFormat en enum
        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
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

        // ===== CONSOLE ATTACHMENT =====
        // EasySave.WPF est compilé en WinExe (pas de console par défaut).
        // AttachConsole(-1) se rattache à la console parente (le cmd qui a lancé l'exe).
        // AllocConsole() en fallback si lancé sans console parente.
        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern bool AttachConsole(int dwProcessId);

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern bool AllocConsole();

        private static void AttachOrAllocConsole()
        {
            if (!AttachConsole(-1)) // -1 = ATTACH_PARENT_PROCESS
                AllocConsole();
        }
    }
}