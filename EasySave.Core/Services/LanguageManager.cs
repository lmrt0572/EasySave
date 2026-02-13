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
                // ══════════════════════════════════════════════
                // ══  Console V1.1 — Menu & Prompts           ══
                // ══════════════════════════════════════════════

                ["menu_title"] = new() { [Language.English] = "=== EasySave ===", [Language.French] = "=== EasySave ===" },
                ["menu_create"] = new() { [Language.English] = "1. Create backup job", [Language.French] = "1. Créer un travail de sauvegarde" },
                ["menu_list"] = new() { [Language.English] = "2. List backup jobs", [Language.French] = "2. Lister les travaux" },
                ["menu_execute"] = new() { [Language.English] = "3. Execute backup job", [Language.French] = "3. Exécuter une sauvegarde" },
                ["menu_delete"] = new() { [Language.English] = "4. Delete backup job", [Language.French] = "4. Supprimer un travail" },
                ["menu_settings"] = new() { [Language.English] = "5. Settings", [Language.French] = "5. Paramètres" },
                ["menu_language"] = new() { [Language.English] = "6. Change language", [Language.French] = "6. Changer la langue" },
                ["menu_log_format"] = new() { [Language.English] = "7. Change log format", [Language.French] = "7. Changer le format des logs" },
                ["menu_quit"] = new() { [Language.English] = "8. Quit", [Language.French] = "8. Quitter" },
                ["menu_choice"] = new() { [Language.English] = "Your choice: ", [Language.French] = "Votre choix : " },

                // ── Messages confirmation ──
                ["job_created"] = new() { [Language.English] = "Backup job created successfully", [Language.French] = "Travail de sauvegarde créé avec succès" },
                ["job_deleted"] = new() { [Language.English] = "Backup job deleted", [Language.French] = "Travail supprimé" },
                ["job_executed"] = new() { [Language.English] = "Backup completed", [Language.French] = "Sauvegarde terminée" },
                ["language_changed"] = new() { [Language.English] = "Language changed", [Language.French] = "Langue changée" },

                // ── Messages erreur ──
                ["error_max_jobs"] = new() { [Language.English] = "Error: Maximum 5 jobs allowed", [Language.French] = "Erreur : Maximum 5 travaux autorisés" },
                ["error_job_not_found"] = new() { [Language.English] = "Error: Job not found", [Language.French] = "Erreur : Travail non trouvé" },
                ["error_invalid_choice"] = new() { [Language.English] = "Error: Invalid choice", [Language.French] = "Erreur : Choix invalide" },
                ["error_invalid_path"] = new() { [Language.English] = "Error: Invalid path", [Language.French] = "Erreur : Chemin invalide" },
                ["error_execution"] = new() { [Language.English] = "Error during execution", [Language.French] = "Erreur lors de l'exécution" },
                ["error_name_exists"] = new() { [Language.English] = "Error: Name already exists", [Language.French] = "Erreur : Ce nom existe déjà" },
                ["error_generic"] = new() { [Language.English] = "An unexpected error occurred", [Language.French] = "Une erreur inattendue est survenue" },
                ["not_implemented"] = new() { [Language.English] = "Feature not implemented yet", [Language.French] = "Fonctionnalité non implémentée" },

                // ── Prompts (Console) ──
                ["prompt_name"] = new() { [Language.English] = "Job name: ", [Language.French] = "Nom du travail : " },
                ["prompt_source"] = new() { [Language.English] = "Source directory: ", [Language.French] = "Répertoire source : " },
                ["prompt_target"] = new() { [Language.English] = "Target directory: ", [Language.French] = "Répertoire cible : " },
                ["prompt_type"] = new() { [Language.English] = "Type (1=Full, 2=Differential): ", [Language.French] = "Type (1=Complète, 2=Différentielle) : " },
                ["prompt_job_index"] = new() { [Language.English] = "Job number: ", [Language.French] = "Numéro du travail : " },
                ["prompt_log_format"] = new() { [Language.English] = "Choose format (1=JSON, 2=XML): ", [Language.French] = "Choisir le format (1=JSON, 2=XML) : " },

                // ── Settings (Console) ──
                ["settings_title"] = new() { [Language.English] = "=== Settings ===", [Language.French] = "=== Paramètres ===" },
                ["settings_current_key"] = new() { [Language.English] = "Current encryption key: {0}", [Language.French] = "Clé de chiffrement actuelle : {0}" },
                ["settings_prompt_key"] = new() { [Language.English] = "New key (Enter to keep): ", [Language.French] = "Nouvelle clé (Entrée pour garder) : " },
                ["settings_current_extensions"] = new() { [Language.English] = "Current extensions: {0}", [Language.French] = "Extensions actuelles : {0}" },
                ["settings_prompt_extensions"] = new() { [Language.English] = "New extensions (comma-separated, Enter to keep): ", [Language.French] = "Nouvelles extensions (séparées par virgules, Entrée pour garder) : " },
                ["settings_current_software"] = new() { [Language.English] = "Current business software: {0}", [Language.French] = "Logiciel métier actuel : {0}" },
                ["settings_prompt_software"] = new() { [Language.English] = "New software name (Enter to keep): ", [Language.French] = "Nouveau nom de logiciel (Entrée pour garder) : " },
                ["settings_success"] = new() { [Language.English] = "Settings updated successfully", [Language.French] = "Paramètres mis à jour avec succès" },

                // ── Status ──
                ["status_active"] = new() { [Language.English] = "Active", [Language.French] = "Actif" },
                ["status_inactive"] = new() { [Language.English] = "Inactive", [Language.French] = "Inactif" },
                ["status_completed"] = new() { [Language.English] = "Completed", [Language.French] = "Terminé" },
                ["status_error"] = new() { [Language.English] = "Error", [Language.French] = "Erreur" },

                // ── Progress ──
                ["progress_files"] = new() { [Language.English] = "Files: {0}/{1}", [Language.French] = "Fichiers : {0}/{1}" },
                ["progress_current"] = new() { [Language.English] = "Current: {0}", [Language.French] = "En cours : {0}" },
                ["no_jobs"] = new() { [Language.English] = "No backup jobs yet", [Language.French] = "Aucun travail configuré" },

                // ── Types ──
                ["type_full"] = new() { [Language.English] = "Full", [Language.French] = "Complète" },
                ["type_differential"] = new() { [Language.English] = "Differential", [Language.French] = "Différentielle" },

                // ── Job details (Console) ──
                ["job_details_header"] = new() { [Language.English] = "Backup job details", [Language.French] = "Détails du travail de sauvegarde" },
                ["job_name"] = new() { [Language.English] = "Name: ", [Language.French] = "Nom : " },
                ["job_source"] = new() { [Language.English] = "Source: ", [Language.French] = "Source : " },
                ["job_target"] = new() { [Language.English] = "Target: ", [Language.French] = "Cible : " },
                ["job_type"] = new() { [Language.English] = "Type: ", [Language.French] = "Type : " },

                // ── Log format (dev) ──
                ["log_format_changed"] = new() { [Language.English] = "Log format changed to {0}", [Language.French] = "Format de log changé en {0}" },

                // ── Misc ──
                ["press_any_key"] = new() { [Language.English] = "Press any key to continue...", [Language.French] = "Appuyez sur une touche pour continuer..." },

                // ══════════════════════════════════════════════
                // ══  WPF V2.0 — GUI-specific translations   ══
                // ══════════════════════════════════════════════

                // ── Header ──
                ["wpf_subtitle"] = new() { [Language.English] = "Backup management", [Language.French] = "Gestion des sauvegardes" },

                // ── Sidebar Actions ──
                ["wpf_actions"] = new() { [Language.English] = "ACTIONS", [Language.French] = "ACTIONS" },
                ["wpf_execute_all"] = new() { [Language.English] = "▶  Execute All", [Language.French] = "▶  Tout exécuter" },
                ["wpf_execute_selected"] = new() { [Language.English] = "▷  Execute Selected", [Language.French] = "▷  Exécuter la sélection" },
                ["wpf_delete_selected"] = new() { [Language.English] = "✕  Delete Selected", [Language.French] = "✕  Supprimer la sélection" },

                // ── Sidebar Settings ──
                ["wpf_settings"] = new() { [Language.English] = "SETTINGS", [Language.French] = "PARAMÈTRES" },
                ["wpf_business_software"] = new() { [Language.English] = "Business software", [Language.French] = "Logiciel métier" },
                ["wpf_encryption_key"] = new() { [Language.English] = "Encryption key", [Language.French] = "Clé de chiffrement" },
                ["wpf_encryption_extensions"] = new() { [Language.English] = "Encrypted extensions", [Language.French] = "Extensions chiffrées" },
                ["wpf_theme"] = new() { [Language.English] = "THEME", [Language.French] = "THÈME" },

                // ── Monitor status ──
                ["wpf_monitor_detected"] = new() { [Language.English] = "Detected", [Language.French] = "Détecté" },
                ["wpf_monitor_not_detected"] = new() { [Language.English] = "Not detected", [Language.French] = "Non détecté" },

                // ── Business software events ──
                ["error_business_software"] = new() { [Language.English] = "Business software detected — backups blocked", [Language.French] = "Logiciel métier détecté — sauvegardes bloquées" },
                ["business_software_cleared"] = new() { [Language.English] = "Business software closed — ready", [Language.French] = "Logiciel métier fermé — prêt" },
                ["job_stopped_business_software"] = new() { [Language.English] = "Backup stopped: business software detected", [Language.French] = "Sauvegarde arrêtée : logiciel métier détecté" },

                // ── Create form ──
                ["wpf_new_job"] = new() { [Language.English] = "New backup job", [Language.French] = "Nouveau travail de sauvegarde" },
                ["wpf_label_name"] = new() { [Language.English] = "Name", [Language.French] = "Nom" },
                ["wpf_label_source"] = new() { [Language.English] = "Source", [Language.French] = "Source" },
                ["wpf_label_target"] = new() { [Language.English] = "Target", [Language.French] = "Cible" },
                ["wpf_label_type"] = new() { [Language.English] = "Type", [Language.French] = "Type" },
                ["wpf_btn_add"] = new() { [Language.English] = "＋  Add", [Language.French] = "＋  Ajouter" },

                // ── Table headers ──
                ["wpf_col_name"] = new() { [Language.English] = "NAME", [Language.French] = "NOM" },
                ["wpf_col_source"] = new() { [Language.English] = "SOURCE", [Language.French] = "SOURCE" },
                ["wpf_col_target"] = new() { [Language.English] = "TARGET", [Language.French] = "CIBLE" },
                ["wpf_col_type"] = new() { [Language.English] = "TYPE", [Language.French] = "TYPE" },

                // ── Status bar ──
                ["wpf_ready"] = new() { [Language.English] = "Ready", [Language.French] = "Prêt" },
                ["wpf_jobs_count"] = new() { [Language.English] = "{0} job(s)", [Language.French] = "{0} travail/travaux" },
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
