using Xunit;
using EasySave.Core.Services;

namespace EasySave.Tests.Services
{
    // ===== ENCRYPTION SERVICE TESTS =====
    public class EncryptionServiceTests
    {
        // ==========================================
        // ===== IsExtensionTargeted TESTS =====
        // ==========================================

        [Fact]
        public void IsExtensionTargeted_MatchingExtension_ReturnsTrue()
        {
            var service = new EncryptionService("dummy.exe", "key123", new List<string> { ".txt", ".pdf" });

            Assert.True(service.IsExtensionTargeted(@"C:\docs\file.txt"));
            Assert.True(service.IsExtensionTargeted(@"C:\docs\report.pdf"));
        }

        [Fact]
        public void IsExtensionTargeted_NonMatchingExtension_ReturnsFalse()
        {
            var service = new EncryptionService("dummy.exe", "key123", new List<string> { ".txt" });

            Assert.False(service.IsExtensionTargeted(@"C:\docs\image.png"));
        }

        [Fact]
        public void IsExtensionTargeted_CaseInsensitive_ReturnsTrue()
        {
            var service = new EncryptionService("dummy.exe", "key123", new List<string> { ".txt" });

            Assert.True(service.IsExtensionTargeted(@"C:\docs\FILE.TXT"));
            Assert.True(service.IsExtensionTargeted(@"C:\docs\file.Txt"));
        }

        [Fact]
        public void IsExtensionTargeted_ExtensionWithoutDot_NormalizesAndMatches()
        {
            // Extensions provided without leading dot should still work
            var service = new EncryptionService("dummy.exe", "key123", new List<string> { "txt", "pdf" });

            Assert.True(service.IsExtensionTargeted(@"C:\docs\file.txt"));
            Assert.True(service.IsExtensionTargeted(@"C:\docs\file.pdf"));
        }

        [Fact]
        public void IsExtensionTargeted_EmptyKey_ReturnsFalse()
        {
            // No encryption key → no file should be targeted
            var service = new EncryptionService("dummy.exe", "", new List<string> { ".txt" });

            Assert.False(service.IsExtensionTargeted(@"C:\docs\file.txt"));
        }

        [Fact]
        public void IsExtensionTargeted_NullKey_ReturnsFalse()
        {
            var service = new EncryptionService("dummy.exe", null!, new List<string> { ".txt" });

            Assert.False(service.IsExtensionTargeted(@"C:\docs\file.txt"));
        }

        [Fact]
        public void IsExtensionTargeted_EmptyFilePath_ReturnsFalse()
        {
            var service = new EncryptionService("dummy.exe", "key123", new List<string> { ".txt" });

            Assert.False(service.IsExtensionTargeted(""));
            Assert.False(service.IsExtensionTargeted(null!));
        }

        [Fact]
        public void IsExtensionTargeted_NullExtensionsList_DoesNotThrow()
        {
            var service = new EncryptionService("dummy.exe", "key123", null!);

            Assert.False(service.IsExtensionTargeted(@"C:\file.txt"));
        }

        [Fact]
        public void IsExtensionTargeted_EmptyExtensionInList_Ignored()
        {
            // Empty strings in extensions list should be filtered out
            var service = new EncryptionService("dummy.exe", "key123", new List<string> { "", " ", ".txt" });

            Assert.True(service.IsExtensionTargeted(@"C:\file.txt"));
        }

        [Fact]
        public void IsExtensionTargeted_DuplicateExtensions_HandledCorrectly()
        {
            var service = new EncryptionService("dummy.exe", "key123", new List<string> { ".txt", ".txt", "txt" });

            Assert.True(service.IsExtensionTargeted(@"C:\file.txt"));
        }
    }
}