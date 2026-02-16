using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using EasyLog.Models;
using EasySave.Core.Models;
using EasySave.Core.Models.Enums;
using EasySave.Core.Services;
using EasySave.Core.Strategies;
using EasySave.Core.Utils;

namespace EasySave.Core.ViewModels
{
    public class MainViewModel
    {
        // ===== CONSTANTS =====
        private const int MaxJobs = 5;

        // ===== PRIVATE MEMBERS =====
        private AppConfig _config;
        private readonly string _configPath;
        private readonly LanguageManager _languageManager;
        private readonly ServiceCommandLineParser _parser;
        private readonly IStateService _stateService;
        private IEncryptionService _encryptionService = null!;

        // ===== CONSTRUCTOR =====
        public MainViewModel(LanguageManager languageManager)
        {
            _languageManager = languageManager ?? throw new ArgumentNullException(nameof(languageManager));
            _parser = new ServiceCommandLineParser();
            _stateService = new StateService();
            _config = new AppConfig();

            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string configDir = Path.Combine(appDataPath, "EasySave");
            Directory.CreateDirectory(configDir);
            _configPath = Path.Combine(configDir, "config.json");

            LoadConfig();
            UpdateEncryptionService();
        }

        // ===== SETTINGS =====
        public string GetEncryptionKey() => _config.EncryptionKey;
        public List<string> GetEncryptionExtensions() => _config.EncryptionExtensions;
        public string GetBusinessSoftware() => _config.BusinessSoftwareName;

        public void UpdateSettings(string key, List<string> extensions, string businessSoftware)
        {
            _config.EncryptionKey = key;
            _config.EncryptionExtensions = extensions;
            _config.BusinessSoftwareName = businessSoftware;
            UpdateEncryptionService();
            SaveConfig();
        }

        private void UpdateEncryptionService()
        {
            _encryptionService = new EncryptionService(
                exePath: Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CryptoSoft.exe"),
                key: _config.EncryptionKey,
                extensions: _config.EncryptionExtensions);
        }

        // ===== LOG FORMAT =====
        public void SetLogFormat(LogFormat format)
        {
            _config.LogFormat = format;
            EasyLog.Services.LogService.Instance.SetLogFormat(format);
            SaveConfig();
        }

        public LogFormat GetCurrentLogFormat() => _config.LogFormat;

        // ===== LANGUAGE =====
        public LanguageManager GetLanguageManager() => _languageManager;

        // ===== JOB MANAGEMENT =====
        public List<BackupJob> GetAllJobs() => _config.Jobs.ToList();

        public BackupJob? GetJob(int index)
        {
            if (index < 0 || index >= _config.Jobs.Count) return null;
            return _config.Jobs[index];
        }

        public int GetJobCount() => _config.Jobs.Count;

        public bool CreateJob(string name, string source, string target, int typeInput)
        {
            if (_config.Jobs.Count >= MaxJobs) return false;
            if (string.IsNullOrWhiteSpace(name)) return false;
            if (_config.Jobs.Any(j => j.Name.Equals(name, StringComparison.OrdinalIgnoreCase))) return false;
            if (!FileUtils.DirectoryExists(source)) return false;

            _config.Jobs.Add(new BackupJob(name, source, target, typeInput == 2 ? BackupType.Differential : BackupType.Full));
            SaveConfig();
            return true;
        }

        public bool DeleteJob(int index)
        {
            int i = index - 1;
            if (i < 0 || i >= _config.Jobs.Count) return false;
            _config.Jobs.RemoveAt(i);
            SaveConfig();
            return true;
        }

        public bool DeleteJobByName(string name)
        {
            var job = _config.Jobs.FirstOrDefault(j => j.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (job == null) return false;
            _config.Jobs.Remove(job);
            SaveConfig();
            return true;
        }

        // ===== EXECUTION =====
        public async Task ExecuteJob(int index)
        {
            var job = GetJob(index - 1);
            if (job == null) return;
            await ExecuteSingleJob(job);
        }

        public async Task ExecuteAllJobs()
        {
            foreach (var job in _config.Jobs) await ExecuteSingleJob(job);
        }

        public async Task ExecuteSelectedJobs(IEnumerable<int> indices)
        {
            foreach (int i in indices)
                if (i >= 0 && i < _config.Jobs.Count)
                    await ExecuteSingleJob(_config.Jobs[i]);
        }

        private async Task ExecuteSingleJob(BackupJob job)
        {
            IBackupStrategy strategy = job.Type == BackupType.Full ? new FullBackupStrategy() : new DifferentialBackupStrategy();
            var execution = new ServiceBackupExecution(strategy, EasyLog.Services.LogService.Instance, _stateService, _encryptionService);
            await execution.Execute(job, _config.BusinessSoftwareName);
        }

        // ===== CLI =====
        public async Task RunCli(string[] args)
        {
            if (args == null || args.Length == 0) return;
            var indices = _parser.Parse(args);

            if (_parser.HasError)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  Error: {_parser.ErrorMessage}");
                Console.ResetColor();
                if (!indices.Any()) return;
            }

            if (_config.Jobs.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("  No backup jobs configured.");
                Console.ResetColor();
                return;
            }

            var valid = new List<int>();
            var invalid = new List<int>();
            foreach (int i in indices)
            {
                if (i >= 0 && i < _config.Jobs.Count) valid.Add(i);
                else invalid.Add(i + 1);
            }

            if (invalid.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"  Warning: Job(s) {string.Join(", ", invalid)} do not exist. (You have {_config.Jobs.Count} job(s))");
                Console.ResetColor();
            }

            if (valid.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("  No valid jobs to execute.");
                Console.ResetColor();
                return;
            }

            Console.WriteLine($"  Executing job(s): {string.Join(", ", valid.Select(i => i + 1))}");
            Console.WriteLine();
            await ExecuteSelectedJobs(valid);
        }

        // ===== PERSISTENCE =====
        private static readonly JsonSerializerOptions _jsonOpts = new() { WriteIndented = true };

        private void LoadConfig()
        {
            if (!File.Exists(_configPath)) { _config = new AppConfig(); ApplyLogFormat(); return; }

            try
            {
                string json = File.ReadAllText(_configPath);
                if (string.IsNullOrWhiteSpace(json)) { _config = new AppConfig(); ApplyLogFormat(); return; }

                json = json.TrimStart();

                if (json.StartsWith("["))
                {
                    var jobs = JsonSerializer.Deserialize<List<BackupJob>>(json);
                    _config = new AppConfig { Jobs = jobs ?? new() };
                }
                else
                {
                    _config = JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
                }
            }
            catch { _config = new AppConfig(); }

            ApplyLogFormat();
        }

        private void ApplyLogFormat() => EasyLog.Services.LogService.Instance.SetLogFormat(_config.LogFormat);

        private void SaveConfig()
        {
            try { File.WriteAllText(_configPath, JsonSerializer.Serialize(_config, _jsonOpts)); }
            catch { }
        }
    }
}
