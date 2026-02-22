using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EasySave.Core.Utils
{
    // ===== FILE UTILS =====
    public static class FileUtils
    {
        // ===== FILE OPERATIONS =====

        private const int CopyBufferSize = 262144; // 256 KB – balance between copy speed and Stop responsiveness

        public static void CopyFile(string source, string dest)
        {
            string? directory = Path.GetDirectoryName(dest);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            File.Copy(source, dest, true);
        }

        public static async Task CopyFileAsync(string source, string dest, CancellationToken cancellationToken)
        {
            string? directory = Path.GetDirectoryName(dest);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            var buffer = new byte[CopyBufferSize];
            await using var src = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read, CopyBufferSize, true);
            await using var dst = new FileStream(dest, FileMode.Create, FileAccess.Write, FileShare.None, CopyBufferSize, true);

            int read;
            while ((read = await src.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await dst.WriteAsync(buffer, 0, read, cancellationToken);
            }
        }

        public static string GetUNCPath(string path) => Path.GetFullPath(path);

        public static long GetFileSize(string path) => new FileInfo(path).Length;

        public static IEnumerable<string> GetAllFilesRecursive(string directory)
            => Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories);

        public static bool DirectoryExists(string path) => Directory.Exists(path);

        public static bool IsProcessRunning(string processName)
        {
            if (string.IsNullOrWhiteSpace(processName)) return false;

            string cleanName = processName.Replace(".exe", "", StringComparison.OrdinalIgnoreCase);

            return Process.GetProcessesByName(cleanName).Length > 0;
        }
    }
}

