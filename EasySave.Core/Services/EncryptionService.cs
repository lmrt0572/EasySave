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
            _key = key;
            _targetedExtensions = extensions ?? new List<string>();
        }

        // ===== INTERFACE IMPLEMENTATION =====

        public bool IsExtensionTargeted(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return false;
            string ext = Path.GetExtension(filePath);
            return _targetedExtensions.Any(e => string.Equals((e ?? "").Trim(), ext, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<int> EncryptAsync(string targetFilePath)
        {
            if (!File.Exists(_cryptoSoftPath))
            {
                return -99;
            }

            await _semaphore.WaitAsync();
            try
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = _cryptoSoftPath,
                        Arguments = $"\"{targetFilePath}\" \"{_key}\"",
                        CreateNoWindow = true,
                        UseShellExecute = false
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