using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

public interface IEncryptionService
{
    Task<int> EncryptAsync(string targetFilePath);
    bool IsExtensionTargeted(string filePath);
}

public class EncryptionService : IEncryptionService
{
    private readonly string _cryptoSoftPath;
    private readonly string _key;
    private readonly List<string> _targetedExtensions;
    private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(2);

    public EncryptionService(string exePath, string key, List<string> extensions)
    {
        _cryptoSoftPath = exePath;
        _key = key;
        _targetedExtensions = extensions;
    }

    public bool IsExtensionTargeted(string filePath) =>
        _targetedExtensions.Contains(Path.GetExtension(filePath).ToLower());

    public async Task<int> EncryptAsync(string targetFilePath)
    {
        Console.WriteLine($"\n[DEBUG] Recherche de CryptoSoft ici : {_cryptoSoftPath}");

        if (!File.Exists(_cryptoSoftPath)) return -99;

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
        catch
        {
            return -1;
        }
        finally
        {
            _semaphore.Release();
        }
    }
}