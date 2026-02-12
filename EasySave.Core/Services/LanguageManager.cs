using System;
using System.Collections.Generic;
using System.Text;

namespace EasySave.Core.Models.Enums
{

    // ===== LANGUAGE MANAGER =====
    public class LanguageManager
    {
        // ===== PRIVATE MEMBERS =====
        private Language _currentLanguage = Language.English;
        private readonly Dictionary<string, Dictionary<Language, string>> _translations;

        // ===== CONSTRUCTOR =====
        public LanguageManager()
        {
            // ===== TRANSLATIONS DATA =====
            _translations = new Dictionary<string, Dictionary<Language, string>>
            {
                // Menu principal
                ["menu_title"] = new() { [Language.English] = "=== EasySave ===", [Language.French] = "=== EasySave ===" },
                ["menu_create"] = new() { [Language.English] = "1. Create backup job", [Language.French] = "1. Créer un travail de sauvegarde" },
                ["menu_list"] = new() { [Language.English] = "2. List backup jobs", [Language.French] = "2. Lister les travaux" },
                ["menu_execute"] = new() { [Language.English] = "3. Execute backup job", [Language.French] = "3. Exécuter une sauvegarde" },
                ["menu_delete"] = new() { [Language.English] = "4. Delete backup job", [Language.French] = "4. Supprimer un travail" },
                ["menu_settings"] = new() { [Language.English] = "5. Settings", [Language.French] = "5. Paramètres" },
                ["menu_language"] = new() { [Language.English] = "6. Change language", [Language.French] = "6. Changer la langue" },
                ["menu_quit"] = new() { [Language.English] = "7. Quit", [Language.French] = "7. Quitter" },
                ["menu_choice"] = new() { [Language.English] = "Your choice: ", [Language.French] = "Votre choix : " },

                // Settings
                ["settings_title"] = new() { [Language.English] = "--- CONFIGURATION ---", [Language.French] = "--- CONFIGURATION ---" },
                ["settings_current_key"] = new() { [Language.English] = "Current Encryption Key: {0}", [Language.French] = "Clé de chiffrement actuelle : {0}" },
                ["settings_prompt_key"] = new() { [Language.English] = "Enter new key (leave empty to keep current): ", [Language.French] = "Entrez la nouvelle clé (vide pour garder l'actuelle) : " },
                ["settings_current_extensions"] = new() { [Language.English] = "Current Extensions: {0}", [Language.French] = "Extensions actuelles : {0}" },
                ["settings_prompt_extensions"] = new() { [Language.English] = "Enter extensions (comma separated, ex: .txt,.pdf): ", [Language.French] = "Entrez les extensions (séparées par virgule, ex: .txt,.pdf) : " },
                ["settings_current_software"] = new() { [Language.English] = "Current Business Software: {0}", [Language.French] = "Logiciel métier actuel : {0}" },
                ["settings_prompt_software"] = new() { [Language.English] = "Enter software process name: ", [Language.French] = "Entrez le nom du processus logiciel : " },
                ["settings_success"] = new() { [Language.English] = "Settings updated and saved!", [Language.French] = "Paramètres mis à jour et sauvegardés !" },

                // Messages confirmation
                ["job_created"] = new() { [Language.English] = "Backup job created successfully", [Language.French] = "Travail de sauvegarde créé avec succès" },
                ["job_deleted"] = new() { [Language.English] = "Backup job deleted", [Language.French] = "Travail supprimé" },
                ["job_executed"] = new() { [Language.English] = "Backup completed", [Language.French] = "Sauvegarde terminée" },
                ["language_changed"] = new() { [Language.English] = "Language changed", [Language.French] = "Langue changée" },
                ["press_any_key"] = new() { [Language.English] = "Press any key...", [Language.French] = "Appuyez sur une touche..." },

                // Messages erreur
                ["error_max_jobs"] = new() { [Language.English] = "Error: Maximum 5 jobs allowed", [Language.French] = "Erreur : Maximum 5 travaux autorisés" },
                ["error_job_not_found"] = new() { [Language.English] = "Error: Job not found", [Language.French] = "Erreur : Travail non trouvé" },
                ["error_invalid_choice"] = new() { [Language.English] = "Error: Invalid choice", [Language.French] = "Erreur : Choix invalide" },
                ["error_invalid_path"] = new() { [Language.English] = "Error: Invalid path", [Language.French] = "Erreur : Chemin invalide" },
                ["error_execution"] = new() { [Language.English] = "Error during execution", [Language.French] = "Erreur lors de l'exécution" },
                ["error_name_exists"] = new() { [Language.English] = "Error: Name already exists", [Language.French] = "Erreur : Ce nom existe déjà" },
                ["error_generic"] = new() { [Language.English] = "An unexpected error occurred", [Language.French] = "Une erreur inattendue est survenue" },
                ["not_implemented"] = new() { [Language.English] = "Feature not implemented yet", [Language.French] = "Fonctionnalité non implémentée" },

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
                ["type_differential"] = new() { [Language.English] = "Differential", [Language.French] = "Différentielle" },

                // Job details (used in ConsoleView)
                ["job_details_header"] = new() { [Language.English] = "Backup job details", [Language.French] = "Détails du travail de sauvegarde" },
                ["job_name"] = new() { [Language.English] = "Name: ", [Language.French] = "Nom : " },
                ["job_source"] = new() { [Language.English] = "Source: ", [Language.French] = "Source : " },
                ["job_target"] = new() { [Language.English] = "Target: ", [Language.French] = "Cible : " },
                ["job_type"] = new() { [Language.English] = "Type: ", [Language.French] = "Type : " }
            };
        }

        // ===== LANGUAGE API =====
        public void SetLanguage(Language lang) => _currentLanguage = lang;

        public Language GetCurrentLanguage() => _currentLanguage;

        // ===== TRANSLATION =====
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