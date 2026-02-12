using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
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
        private readonly string _configPath;
        private readonly LanguageManager _languageManager;
        private readonly ServiceCommandLineParser _parser;
        private readonly IStateService _stateService;
        private LogFormat _currentLogFormat = LogFormat.Json;

        // ===== CONSTRUCTOR =====
        public MainViewModel(LanguageManager languageManager)
        {
            _languageManager = languageManager ?? throw new ArgumentNullException(nameof(languageManager));
            _parser = new ServiceCommandLineParser();
            _stateService = new StateService();
            _jobs = new List<BackupJob>();

            // Setup config path
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string configDir = Path.Combine(appDataPath, "EasySave");
            Directory.CreateDirectory(configDir);
            _configPath = Path.Combine(configDir, "config.json");

            // Load existing jobs
            LoadJobs();
        }

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
            // Validate max jobs
            if (_jobs.Count >= MaxJobs)
                return false;

            // Validate name not empty
            if (string.IsNullOrWhiteSpace(name))
                return false;

            // Validate unique name
            if (_jobs.Any(j => j.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                return false;

            // Validate source directory exists
            if (!FileUtils.DirectoryExists(source))
                return false;

            // Determine backup type
            BackupType type = typeInput == 2 ? BackupType.Differential : BackupType.Full;

            // Create and add job
            var job = new BackupJob(name, source, target, type);
            _jobs.Add(job);
            SaveJobs();

            return true;
        }

        public bool DeleteJob(int index)
        {
            // Convert 1-based to 0-based index
            int zeroBasedIndex = index - 1;

            if (zeroBasedIndex < 0 || zeroBasedIndex >= _jobs.Count)
                return false;

            _jobs.RemoveAt(zeroBasedIndex);
            SaveJobs();
            return true;
        }

        public bool DeleteJobByName(string name)
        {
            var job = _jobs.FirstOrDefault(j => j.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (job == null)
                return false;

            _jobs.Remove(job);
            SaveJobs();
            return true;
        }

        // ===== EXECUTION ===== 

        public void ExecuteJob(int index)
        {
            // Convert 1-based to 0-based index
            int zeroBasedIndex = index - 1;

            var job = GetJob(zeroBasedIndex);
            if (job == null)
                return;

            ExecuteSingleJob(job);
        }

        public void ExecuteAllJobs()
        {
            foreach (var job in _jobs)
            {
                ExecuteSingleJob(job);
            }
        }

        public void ExecuteSelectedJobs(IEnumerable<int> indices)
        {
            foreach (int index in indices)
            {
                if (index >= 0 && index < _jobs.Count)
                {
                    ExecuteSingleJob(_jobs[index]);
                }
            }
        }

        private void ExecuteSingleJob(BackupJob job)
        {
            // Select strategy based on job type
            IBackupStrategy strategy = job.Type == BackupType.Full
                ? new FullBackupStrategy()
                : new DifferentialBackupStrategy();

            // Create execution service
            var logService = EasyLog.Services.LogService.Instance;
            var execution = new ServiceBackupExecution(strategy, logService, _stateService);

            // Execute
            execution.Execute(job);
        }

            
        // ===== CLI MODE ===== 

        public void RunCli(string[] args)
        {
            if (args == null || args.Length == 0)
                return;

            var indices = _parser.Parse(args);

            // Check for parsing errors
            if (_parser.HasError)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  Error: {_parser.ErrorMessage}");
                Console.ResetColor();

                if (!indices.Any())
                    return;
            }

            // Check if any jobs exist
            if (_jobs.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("  No backup jobs configured.");
                Console.ResetColor();
                return;
            }

            // Filter to only existing jobs and warn about missing ones
            var validIndices = new List<int>();
            var invalidIndices = new List<int>();

            foreach (int index in indices)
            {
                if (index >= 0 && index < _jobs.Count)
                {
                    validIndices.Add(index);
                }
                else
                {
                    invalidIndices.Add(index + 1); // Convert back to 1-based for display
                }
            }

            // Warn about non-existent jobs
            if (invalidIndices.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"  Warning: Job(s) {string.Join(", ", invalidIndices)} do not exist. (You have {_jobs.Count} job(s))");
                Console.ResetColor();
            }

            // Check if any valid jobs remain
            if (validIndices.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("  No valid jobs to execute.");
                Console.ResetColor();
                return;
            }

            // Execute only valid jobs
            Console.WriteLine($"  Executing job(s): {string.Join(", ", validIndices.Select(i => i + 1))}");
            Console.WriteLine();

            ExecuteSelectedJobs(validIndices);
        }

        // ===== PERSISTENCE ===== 

        private void LoadJobs()
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

                    _currentLogFormat = LogFormat.Json;
                }
                else
                {
                    var config = JsonSerializer.Deserialize<ConfigDto>(json);
                    var jobDtos = config?.Jobs ?? new List<BackupJobDto>();

                    _jobs = jobDtos.Select(dto => new BackupJob(
                        dto.Name ?? string.Empty,
                        dto.SourceDirectory ?? string.Empty,
                        dto.TargetDirectory ?? string.Empty,
                        dto.Type
                    )).ToList();

                    _currentLogFormat = config?.LogFormat ?? LogFormat.Json;
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

        private void SaveJobs()
        {
            var jobDtos = _jobs.Select(j => new BackupJobDto
            {
                Name = j.Name,
                SourceDirectory = j.SourceDirectory,
                TargetDirectory = j.TargetDirectory,
                Type = j.Type
            }).ToList();

            var options = new JsonSerializerOptions { WriteIndented = true };
            var config = new ConfigDto
            {
                Jobs = jobDtos,
                LogFormat = _currentLogFormat
            };

            try
            {
                string json = JsonSerializer.Serialize(config, options);
                File.WriteAllText(_configPath, json);
            }
            catch
            {
                // Ignore config save errors
            }
        }

        // ===== DTO ===== 
        private class BackupJobDto
        {
            public string? Name { get; set; }
            public string? SourceDirectory { get; set; }
            public string? TargetDirectory { get; set; }
            public BackupType Type { get; set; }
        }

        // ===== LOG MANAGEMENT ===== 
        public void SetLogFormat(LogFormat format)
        {
            _currentLogFormat = format;
            EasyLog.Services.LogService.Instance.SetLogFormat(format);
            SaveLogFormatPreference(format);
        }

        public LogFormat GetCurrentLogFormat() => _currentLogFormat;

        private void SaveLogFormatPreference(LogFormat format)
        {
            // Persist preference alongside jobs
            _currentLogFormat = format;
            SaveJobs();
        }

        // ===== CONFIG DTO =====
        private class ConfigDto
        {
            public List<BackupJobDto> Jobs { get; set; } = new List<BackupJobDto>();
            public LogFormat LogFormat { get; set; } = LogFormat.Json;
        }
    }
}