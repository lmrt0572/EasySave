using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EasySave.Core.Services
{
    public class EncryptionService : IEncryptionService
    {
        // ===== PRIVATE MEMBERS =====
        private readonly string _cryptoSoftPath;
        private readonly string _key;
        private readonly List<string> _targetedExtensions;

        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(2);

        // ===== CONSTRUCTOR =====
        public EncryptionService(string exePath, string key, List<string> extensions)
        {
            _cryptoSoftPath = exePath;
            _key = key ?? string.Empty;
            _targetedExtensions = (extensions ?? new List<string>())
                .Select(e =>
                {
                    var x = e.Trim().ToLowerInvariant();
                    if (string.IsNullOrEmpty(x)) return null;
                    return x.StartsWith('.') ? x : "." + x;
                })
                .Where(e => e != null)
                .Cast<string>()
                .Distinct()
                .ToList();
        }

        // ===== INTERFACE IMPLEMENTATION =====

        public bool IsExtensionTargeted(string filePath)
        {
            if (string.IsNullOrWhiteSpace(_key)) return false;
            if (string.IsNullOrEmpty(filePath)) return false;
            string ext = Path.GetExtension(filePath).ToLowerInvariant();
            return _targetedExtensions.Contains(ext);
        }

        public async Task<int> EncryptAsync(string targetFilePath)
        {
            if (!File.Exists(_cryptoSoftPath))
            {
                return -99;
            }

            if (string.IsNullOrWhiteSpace(_key))
            {
                return 0;
            }

            await _semaphore.WaitAsync();
            try
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = _cryptoSoftPath,
                        ArgumentList = { targetFilePath, _key },
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        WorkingDirectory = Path.GetDirectoryName(_cryptoSoftPath) ?? AppDomain.CurrentDomain.BaseDirectory
                    }
                };

                process.Start();
                await process.WaitForExitAsync();

                return process.ExitCode;
            }
            catch (Exception)
            {
                return -1;
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}