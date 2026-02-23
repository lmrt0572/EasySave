using Xunit;
using EasySave.Core.Models.Enums;

namespace EasySave.Tests.Services
{
    // ===== LANGUAGE MANAGER TESTS =====
    public class LanguageManagerTests
    {
        private readonly LanguageManager _manager = new();

        // ==========================================
        // ===== DEFAULT LANGUAGE =====
        // ==========================================

        [Fact]
        public void Default_Language_IsEnglish()
        {
            Assert.Equal(Language.English, _manager.GetCurrentLanguage());
        }

        // ==========================================
        // ===== SET LANGUAGE =====
        // ==========================================

        [Fact]
        public void SetLanguage_French_ChangesCurrent()
        {
            _manager.SetLanguage(Language.French);

            Assert.Equal(Language.French, _manager.GetCurrentLanguage());
        }

        // ==========================================
        // ===== GET TEXT =====
        // ==========================================

        [Fact]
        public void GetText_English_ReturnsEnglishTranslation()
        {
            _manager.SetLanguage(Language.English);

            string text = _manager.GetText("menu_create");

            Assert.Equal("1. Create backup job", text);
        }

        [Fact]
        public void GetText_French_ReturnsFrenchTranslation()
        {
            _manager.SetLanguage(Language.French);

            string text = _manager.GetText("menu_create");

            Assert.Equal("1. Créer un travail de sauvegarde", text);
        }

        [Fact]
        public void GetText_UnknownKey_ReturnsBracketedKey()
        {
            string text = _manager.GetText("this_key_does_not_exist");

            Assert.Equal("[this_key_does_not_exist]", text);
        }

        // ==========================================
        // ===== GET TEXT WITH FORMAT ARGS =====
        // ==========================================

        [Fact]
        public void GetText_WithArgs_FormatsCorrectly()
        {
            _manager.SetLanguage(Language.English);

            // "wpf_jobs_count" = "{0} job(s)"
            string text = _manager.GetText("wpf_jobs_count", 5);

            Assert.Equal("5 job(s)", text);
        }

        [Fact]
        public void GetText_SettingsCurrentKey_FormatsWithValue()
        {
            _manager.SetLanguage(Language.English);

            // "settings_current_key" = "Current encryption key: {0}"
            string text = _manager.GetText("settings_current_key", "MyKey123");

            Assert.Equal("Current encryption key: MyKey123", text);
        }

        // ==========================================
        // ===== LANGUAGE SWITCH CONSISTENCY =====
        // ==========================================

        [Fact]
        public void LanguageSwitch_AllKeysExistInBothLanguages()
        {
            // Verify that switching languages still returns valid text for known keys
            var keys = new[]
            {
                "menu_title", "menu_create", "menu_quit",
                "job_created", "error_max_jobs",
                "wpf_subtitle", "wpf_ready",
                "job_paused", "job_resumed", "job_stopped"
            };

            foreach (var key in keys)
            {
                _manager.SetLanguage(Language.English);
                string en = _manager.GetText(key);
                Assert.DoesNotContain("[", en); // Not a missing key

                _manager.SetLanguage(Language.French);
                string fr = _manager.GetText(key);
                Assert.DoesNotContain("[", fr); // Not a missing key

                // English and French should be different (except "=== EasySave ===")
                if (key != "menu_title" && key != "wpf_actions")
                {
                    Assert.NotEqual(en, fr);
                }
            }
        }
    }
}