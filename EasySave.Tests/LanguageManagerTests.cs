using Xunit;
using EasySave.Core.Models.Enums;

namespace EasySave.Tests.Services
{
    // ===== LANGUAGE MANAGER TESTS =====

    // Coverage : default language, SetLanguage, GetText (known key, unknown key,
    //            format args), consistency across both languages.
    public class LanguageManagerTests
    {
        private readonly LanguageManager _manager = new();

        // ==========================================
        // ===== DEFAULT LANGUAGE =====
        // ==========================================

        [Fact]
        public void Default_Language_IsEnglish()
        {
            // The manager must start in English without any configuration
            Assert.Equal(Language.English, _manager.GetCurrentLanguage());
        }

        // ==========================================
        // ===== SET LANGUAGE =====
        // ==========================================

        [Fact]
        public void SetLanguage_French_ChangesCurrent()
        {
            // SetLanguage must update the current language
            _manager.SetLanguage(Language.French);

            Assert.Equal(Language.French, _manager.GetCurrentLanguage());
        }

        // ==========================================
        // ===== GET TEXT =====
        // ==========================================

        [Fact]
        public void GetText_English_ReturnsEnglishTranslation()
        {
            // A known key must return the correct English value
            _manager.SetLanguage(Language.English);

            string text = _manager.GetText("jobs_title");

            Assert.Equal("Backup Jobs", text);
        }

        [Fact]
        public void GetText_French_ReturnsFrenchTranslation()
        {
            // The same key must return the correct French value when language is French
            _manager.SetLanguage(Language.French);

            string text = _manager.GetText("jobs_title");

            Assert.Equal("Travaux de sauvegarde", text);
        }

        [Fact]
        public void GetText_UnknownKey_ReturnsBracketedKey()
        {
            // A missing key must return the key wrapped in brackets, never throw
            string text = _manager.GetText("this_key_does_not_exist");

            Assert.Equal("[this_key_does_not_exist]", text);
        }

        // ==========================================
        // ===== GET TEXT WITH FORMAT ARGS =====
        // ==========================================

        [Fact]
        public void GetText_WithArgs_FormatsCorrectly()
        {
            // "wpf_jobs_count" = "{0} job(s)" — format argument must be substituted
            _manager.SetLanguage(Language.English);

            string text = _manager.GetText("wpf_jobs_count", 5);

            Assert.Equal("5 job(s)", text);
        }

        [Fact]
        public void GetText_ProgressFiles_FormatsWithTwoArgs()
        {
            // "progress_files" = "Files: {0}/{1}" — two format arguments
            _manager.SetLanguage(Language.English);

            string text = _manager.GetText("progress_files", 3, 10);

            Assert.Equal("Files: 3/10", text);
        }

        // ==========================================
        // ===== LANGUAGE SWITCH CONSISTENCY =====
        // ==========================================

        [Fact]
        public void LanguageSwitch_AllKeysExistInBothLanguages()
        {
            // All listed keys must resolve in both languages without returning a bracketed fallback.
            // These are real keys present in LanguageManager (WPF v3 dictionary).
            var keys = new[]
            {
                "jobs_title",
                "job_created",
                "wpf_subtitle",
                "wpf_ready",
                "job_paused",
                "job_resumed",
                "job_stopped",
                "nav_jobs",
                "nav_settings",
                "settings_log_format"
            };

            foreach (var key in keys)
            {
                _manager.SetLanguage(Language.English);
                string en = _manager.GetText(key);
                Assert.DoesNotContain("[", en); // Missing key returns "[key]"

                _manager.SetLanguage(Language.French);
                string fr = _manager.GetText(key);
                Assert.DoesNotContain("[", fr);
            }
        }

        [Fact]
        public void LanguageSwitch_EnglishAndFrenchDiffer_ForTranslatedKeys()
        {
            // For keys that have distinct translations, EN and FR must differ
            var translatedKeys = new[]
            {
                "jobs_title",
                "job_paused",
                "nav_settings",
                "settings_log_format"
            };

            foreach (var key in translatedKeys)
            {
                _manager.SetLanguage(Language.English);
                string en = _manager.GetText(key);

                _manager.SetLanguage(Language.French);
                string fr = _manager.GetText(key);

                Assert.NotEqual(en, fr);
            }
        }
    }
}