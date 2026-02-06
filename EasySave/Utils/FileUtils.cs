using System;
using System.Collections.Generic;
using System.Text;

namespace EasySave.Utils
{
    // ===== FILE UTILS =====
    public static class FileUtils
    {
        // ===== FILE OPERATIONS =====
        public static void CopyFile(string source, string dest)
        {
            string directory = Path.GetDirectoryName(dest);
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
            File.Copy(source, dest, true);
        }

        public static string GetUNCPath(string path) => Path.GetFullPath(path);

        public static long GetFileSize(string path) => new FileInfo(path).Length;

        public static IEnumerable<string> GetAllFilesRecursive(string directory)
            => Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories);

        public static bool DirectoryExists(string path) => Directory.Exists(path);
    }
}

