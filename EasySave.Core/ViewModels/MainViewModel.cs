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
        private List<BackupJob> _jobs;
        private string _encryptionKey;
        private List<string> _encryptionExtensions;
        private string _businessSoftwareName;
        private readonly string _configPath;
        private readonly LanguageManager _languageManager;
        private readonly ServiceCommandLineParser _parser;
        private readonly IStateService _stateService;
        private IEncryptionService _encryptionService = null!;
        private LogFormat _currentLogFormat = LogFormat.Json;

        // ===== CONSTRUCTOR =====
        public MainViewModel(LanguageManager languageManager)
        {
            _languageManager = languageManager ?? throw new ArgumentNullException(nameof(languageManager));
            _parser = new ServiceCommandLineParser();
            _stateService = new StateService();
            _jobs = new List<BackupJob>();

            // Initial default values
            _encryptionKey = "Prosoft123";
            _encryptionExtensions = new List<string> { ".txt", ".md", ".pdf" };
            _businessSoftwareName = "CalculatorApp";

            // Setup config path
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string configDir = Path.Combine(appDataPath, "EasySave");
            Directory.CreateDirectory(configDir);
            _configPath = Path.Combine(configDir, "config.json");

            // Load existing config (jobs + settings + log format)
            LoadConfig();

            // Initialize encryption service with loaded settings
            UpdateEncryptionService();
        }

        // ===== SETTINGS MANAGEMENT (feature/GUI) =====
        public string GetEncryptionKey() => _encryptionKey;
        public List<string> GetEncryptionExtensions() => _encryptionExtensions;
        public string GetBusinessSoftware() => _businessSoftwareName;

        public void UpdateSettings(string key, List<string> extensions, string businessSoftware)
        {
            _encryptionKey = key;
            _encryptionExtensions = extensions;
            _businessSoftwareName = businessSoftware;
            UpdateEncryptionService();
            SaveConfig();
        }

        private void UpdateEncryptionService()
        {
            _encryptionService = new EncryptionService(
                exePath: Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CryptoSoft.exe"),
                key: _encryptionKey,
                extensions: _encryptionExtensions
            );
        }

        // ===== LOG FORMAT MANAGEMENT (dev) =====
        public void SetLogFormat(LogFormat format)
        {
            _currentLogFormat = format;
            EasyLog.Services.LogService.Instance.SetLogFormat(format);
            SaveConfig();
        }

        public LogFormat GetCurrentLogFormat() => _currentLogFormat;

        // ===== LANGUAGE =====
        public LanguageManager GetLanguageManager() => _languageManager;

        // ===== JOB MANAGEMENT =====
        public List<BackupJob> GetAllJobs() => _jobs.ToList();

        public BackupJob? GetJob(int index)
        {
            if (index < 0 || index >= _jobs.Count)
                return null;
            return _jobs[index];
        }

        public int GetJobCount() => _jobs.Count;

        public bool CreateJob(string name, string source, string target, int typeInput)
        {
            if (_jobs.Count >= MaxJobs)
                return false;

            if (string.IsNullOrWhiteSpace(name))
                return false;

            if (_jobs.Any(j => j.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                return false;

            if (!FileUtils.DirectoryExists(source))
                return false;

            BackupType type = typeInput == 2 ? BackupType.Differential : BackupType.Full;

            var job = new BackupJob(name, source, target, type);
            _jobs.Add(job);
            SaveConfig();

            return true;
        }

        public bool DeleteJob(int index)
        {
            int zeroBasedIndex = index - 1;

            if (zeroBasedIndex < 0 || zeroBasedIndex >= _jobs.Count)
                return false;

            _jobs.RemoveAt(zeroBasedIndex);
            SaveConfig();
            return true;
        }

        public bool DeleteJobByName(string name)
        {
            var job = _jobs.FirstOrDefault(j => j.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (job == null)
                return false;

            _jobs.Remove(job);
            SaveConfig();
            return true;
        }

        // ===== EXECUTION =====

        public async Task ExecuteJob(int index)
        {
            int zeroBasedIndex = index - 1;

            var job = GetJob(zeroBasedIndex);
            if (job == null)
                return;

            await ExecuteSingleJob(job);
        }

        public async Task ExecuteAllJobs()
        {
            foreach (var job in _jobs)
            {
                await ExecuteSingleJob(job);
            }
        }

        public async Task ExecuteSelectedJobs(IEnumerable<int> indices)
        {
            foreach (int index in indices)
            {
                if (index >= 0 && index < _jobs.Count)
                {
                    await ExecuteSingleJob(_jobs[index]);
                }
            }
        }

        private async Task ExecuteSingleJob(BackupJob job)
        {
            IBackupStrategy strategy = job.Type == BackupType.Full
                ? new FullBackupStrategy()
                : new DifferentialBackupStrategy();

            var logService = EasyLog.Services.LogService.Instance;
            var execution = new ServiceBackupExecution(strategy, logService, _stateService, _encryptionService);

            await execution.Execute(job, _businessSoftwareName);
        }

        // ===== CLI MODE =====

        public async Task RunCli(string[] args)
        {
            if (args == null || args.Length == 0)
                return;

            var indices = _parser.Parse(args);

            if (_parser.HasError)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  Error: {_parser.ErrorMessage}");
                Console.ResetColor();

                if (!indices.Any())
                    return;
            }

            if (_jobs.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("  No backup jobs configured.");
                Console.ResetColor();
                return;
            }

            var validIndices = new List<int>();
            var invalidIndices = new List<int>();

            foreach (int index in indices)
            {
                if (index >= 0 && index < _jobs.Count)
                    validIndices.Add(index);
                else
                    invalidIndices.Add(index + 1);
            }

            if (invalidIndices.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"  Warning: Job(s) {string.Join(", ", invalidIndices)} do not exist. (You have {_jobs.Count} job(s))");
                Console.ResetColor();
            }

            if (validIndices.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("  No valid jobs to execute.");
                Console.ResetColor();
                return;
            }

            Console.WriteLine($"  Executing job(s): {string.Join(", ", validIndices.Select(i => i + 1))}");
            Console.WriteLine();

            await ExecuteSelectedJobs(validIndices);
        }

        // ===== PERSISTENCE =====
        // Unified format: AppConfigDto contains encryption settings (GUI) + LogFormat (dev) + Jobs

        private void LoadConfig()
        {
            if (!File.Exists(_configPath))
            {
                _jobs = new List<BackupJob>();
                _currentLogFormat = LogFormat.Json;
                EasyLog.Services.LogService.Instance.SetLogFormat(_currentLogFormat);
                return;
            }

            try
            {
                string json = File.ReadAllText(_configPath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    _jobs = new List<BackupJob>();
                    _currentLogFormat = LogFormat.Json;
                    EasyLog.Services.LogService.Instance.SetLogFormat(_currentLogFormat);
                    return;
                }

                json = json.TrimStart();

                // Backward compatibility: old format was a plain array of jobs
                if (json.StartsWith("["))
                {
                    var jobDtos = JsonSerializer.Deserialize<List<BackupJobDto>>(json);

                    _jobs = jobDtos?.Select(dto => new BackupJob(
                        dto.Name ?? string.Empty,
                        dto.SourceDirectory ?? string.Empty,
                        dto.TargetDirectory ?? string.Empty,
                        dto.Type
                    )).ToList() ?? new List<BackupJob>();

                    // Old format had no settings — use defaults
                    _encryptionKey = "Prosoft123";
                    _encryptionExtensions = new List<string> { ".txt", ".md", ".pdf" };
                    _businessSoftwareName = "CalculatorApp";
                    _currentLogFormat = LogFormat.Json;
                }
                else
                {
                    // New unified format (AppConfigDto)
                    var configDto = JsonSerializer.Deserialize<AppConfigDto>(json);

                    if (configDto != null)
                    {
                        _encryptionKey = configDto.EncryptionKey ?? "Prosoft123";
                        _encryptionExtensions = configDto.EncryptionExtensions ?? new List<string> { ".txt", ".md", ".pdf" };
                        _businessSoftwareName = configDto.BusinessSoftwareName ?? "CalculatorApp";
                        _currentLogFormat = configDto.LogFormat;

                        _jobs = configDto.Jobs?.Select(dto => new BackupJob(
                            dto.Name ?? string.Empty,
                            dto.SourceDirectory ?? string.Empty,
                            dto.TargetDirectory ?? string.Empty,
                            dto.Type
                        )).ToList() ?? new List<BackupJob>();
                    }
                    else
                    {
                        _jobs = new List<BackupJob>();
                    }
                }

                EasyLog.Services.LogService.Instance.SetLogFormat(_currentLogFormat);
            }
            catch
            {
                _jobs = new List<BackupJob>();
                _currentLogFormat = LogFormat.Json;
                EasyLog.Services.LogService.Instance.SetLogFormat(_currentLogFormat);
            }
        }

        private void SaveConfig()
        {
            var configDto = new AppConfigDto
            {
                EncryptionKey = _encryptionKey,
                EncryptionExtensions = _encryptionExtensions,
                BusinessSoftwareName = _businessSoftwareName,
                LogFormat = _currentLogFormat,
                Jobs = _jobs.Select(j => new BackupJobDto
                {
                    Name = j.Name,
                    SourceDirectory = j.SourceDirectory,
                    TargetDirectory = j.TargetDirectory,
                    Type = j.Type
                }).ToList()
            };

            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(configDto, options);
                File.WriteAllText(_configPath, json);
            }
            catch
            {
                // Ignore config save errors
            }
        }

        // ===== DTO (unified: GUI encryption + dev LogFormat) =====
        private class AppConfigDto
        {
            public string? EncryptionKey { get; set; }
            public List<string>? EncryptionExtensions { get; set; }
            public string? BusinessSoftwareName { get; set; }
            public LogFormat LogFormat { get; set; } = LogFormat.Json;
            public List<BackupJobDto>? Jobs { get; set; }
        }

        private class BackupJobDto
        {
            public string? Name { get; set; }
            public string? SourceDirectory { get; set; }
            public string? TargetDirectory { get; set; }
            public BackupType Type { get; set; }
        }
    }
}
