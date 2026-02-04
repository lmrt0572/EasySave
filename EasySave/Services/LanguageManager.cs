using System;
using System.Collections.Generic;
using System.Text;

namespace EasySave.Localization
{
    public enum Language { English, French }

    public class LanguageManager
    {
        private Language _currentLanguage = Language.English;
        private readonly Dictionary<string, Dictionary<Language, string>> _translations;

        public LanguageManager()
        {
            _translations = new Dictionary<string, Dictionary<Language, string>>
            {
                // Menu principal
                ["menu_title"] = new() { [Language.English] = "=== EasySave ===", [Language.French] = "=== EasySave ===" },
                ["menu_create"] = new() { [Language.English] = "1. Create backup job", [Language.French] = "1. Créer un travail de sauvegarde" },
                ["menu_list"] = new() { [Language.English] = "2. List backup jobs", [Language.French] = "2. Lister les travaux" },
                ["menu_execute"] = new() { [Language.English] = "3. Execute backup job", [Language.French] = "3. Exécuter une sauvegarde" },
                ["menu_delete"] = new() { [Language.English] = "4. Delete backup job", [Language.French] = "4. Supprimer un travail" },
                ["menu_language"] = new() { [Language.English] = "5. Change language", [Language.French] = "5. Changer la langue" },
                ["menu_quit"] = new() { [Language.English] = "6. Quit", [Language.French] = "6. Quitter" },
                ["menu_choice"] = new() { [Language.English] = "Your choice: ", [Language.French] = "Votre choix : " },

                // Messages confirmation
                ["job_created"] = new() { [Language.English] = "Backup job created successfully", [Language.French] = "Travail de sauvegarde créé avec succès" },
                ["job_deleted"] = new() { [Language.English] = "Backup job deleted", [Language.French] = "Travail supprimé" },
                ["job_executed"] = new() { [Language.English] = "Backup completed", [Language.French] = "Sauvegarde terminée" },
                ["language_changed"] = new() { [Language.English] = "Language changed", [Language.French] = "Langue changée" },

                // Messages erreur
                ["error_max_jobs"] = new() { [Language.English] = "Error: Maximum 5 jobs allowed", [Language.French] = "Erreur : Maximum 5 travaux autorisés" },
                ["error_job_not_found"] = new() { [Language.English] = "Error: Job not found", [Language.French] = "Erreur : Travail non trouvé" },
                ["error_invalid_choice"] = new() { [Language.English] = "Error: Invalid choice", [Language.French] = "Erreur : Choix invalide" },
                ["error_invalid_path"] = new() { [Language.English] = "Error: Invalid path", [Language.French] = "Erreur : Chemin invalide" },
                ["error_execution"] = new() { [Language.English] = "Error during execution", [Language.French] = "Erreur lors de l'exécution" },
                ["error_name_exists"] = new() { [Language.English] = "Error: Name already exists", [Language.French] = "Erreur : Ce nom existe déjà" },

                // Prompts
                ["prompt_name"] = new() { [Language.English] = "Job name: ", [Language.French] = "Nom du travail : " },
                ["prompt_source"] = new() { [Language.English] = "Source directory: ", [Language.French] = "Répertoire source : " },
                ["prompt_target"] = new() { [Language.English] = "Target directory: ", [Language.French] = "Répertoire cible : " },
                ["prompt_type"] = new() { [Language.English] = "Type (1=Full, 2=Differential): ", [Language.French] = "Type (1=Complète, 2=Différentielle) : " },
                ["prompt_job_index"] = new() { [Language.English] = "Job number: ", [Language.French] = "Numéro du travail : " },

                // Status
                ["status_active"] = new() { [Language.English] = "Active", [Language.French] = "Actif" },
                ["status_inactive"] = new() { [Language.English] = "Inactive", [Language.French] = "Inactif" },
                ["status_completed"] = new() { [Language.English] = "Completed", [Language.French] = "Terminé" },
                ["status_error"] = new() { [Language.English] = "Error", [Language.French] = "Erreur" },

                // Progress
                ["progress_files"] = new() { [Language.English] = "Files: {0}/{1}", [Language.French] = "Fichiers : {0}/{1}" },
                ["progress_current"] = new() { [Language.English] = "Current: {0}", [Language.French] = "En cours : {0}" },
                ["no_jobs"] = new() { [Language.English] = "No backup jobs configured", [Language.French] = "Aucun travail configuré" },

                // Types
                ["type_full"] = new() { [Language.English] = "Full", [Language.French] = "Complète" },
                ["type_differential"] = new() { [Language.English] = "Differential", [Language.French] = "Différentielle" }
            };
        }

        public void SetLanguage(Language lang) => _currentLanguage = lang;

        public Language GetCurrentLanguage() => _currentLanguage;

        public string GetText(string key)
        {
            if (_translations.TryGetValue(key, out var langDict))
                if (langDict.TryGetValue(_currentLanguage, out var text))
                    return text;
            return $"[{key}]";
        }

        public string GetText(string key, params object[] args)
        {
            return string.Format(GetText(key), args);
        }
    }
}