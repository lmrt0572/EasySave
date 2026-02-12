using System.Threading.Tasks;

namespace EasySave.Core.Services
{
    public interface IEncryptionService
    {
        Task<int> EncryptAsync(string targetFilePath);
        bool IsExtensionTargeted(string filePath);
    }
}