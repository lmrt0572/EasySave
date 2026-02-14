using System;
using System.Collections.Generic;

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
                // ===== CONSOLE MENU =====
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

                // ===== CONFIRMATION MESSAGES =====
                ["job_created"] = new() { [Language.English] = "Backup job created successfully", [Language.French] = "Travail de sauvegarde créé avec succès" },
                ["job_deleted"] = new() { [Language.English] = "Backup job deleted", [Language.French] = "Travail supprimé" },
                ["job_executed"] = new() { [Language.English] = "Backup completed", [Language.French] = "Sauvegarde terminée" },
                ["language_changed"] = new() { [Language.English] = "Language changed", [Language.French] = "Langue changée" },

                // ===== ERROR MESSAGES =====
                ["error_max_jobs"] = new() { [Language.English] = "Error: Maximum 5 jobs allowed", [Language.French] = "Erreur : Maximum 5 travaux autorisés" },
                ["error_job_not_found"] = new() { [Language.English] = "Error: Job not found", [Language.French] = "Erreur : Travail non trouvé" },
                ["error_invalid_choice"] = new() { [Language.English] = "Error: Invalid choice", [Language.French] = "Erreur : Choix invalide" },
                ["error_invalid_path"] = new() { [Language.English] = "Error: Invalid path", [Language.French] = "Erreur : Chemin invalide" },
                ["error_execution"] = new() { [Language.English] = "Error during execution", [Language.French] = "Erreur lors de l'exécution" },
                ["error_name_exists"] = new() { [Language.English] = "Error: Name already exists", [Language.French] = "Erreur : Ce nom existe déjà" },
                ["error_generic"] = new() { [Language.English] = "An unexpected error occurred", [Language.French] = "Une erreur inattendue est survenue" },
                ["not_implemented"] = new() { [Language.English] = "Feature not implemented yet", [Language.French] = "Fonctionnalité non implémentée" },

                // ===== CONSOLE PROMPTS =====
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

                // ===== STATUS =====
                ["status_active"] = new() { [Language.English] = "Active", [Language.French] = "Actif" },
                ["status_inactive"] = new() { [Language.English] = "Inactive", [Language.French] = "Inactif" },
                ["status_completed"] = new() { [Language.English] = "Completed", [Language.French] = "Terminé" },
                ["status_error"] = new() { [Language.English] = "Error", [Language.French] = "Erreur" },

                // ===== PROGRESS =====
                ["progress_files"] = new() { [Language.English] = "Files: {0}/{1}", [Language.French] = "Fichiers : {0}/{1}" },
                ["progress_current"] = new() { [Language.English] = "Current: {0}", [Language.French] = "En cours : {0}" },
                ["no_jobs"] = new() { [Language.English] = "No backup jobs yet", [Language.French] = "Aucun travail configuré" },

                // ===== TYPES =====
                ["type_full"] = new() { [Language.English] = "Full", [Language.French] = "Complète" },
                ["type_differential"] = new() { [Language.English] = "Differential", [Language.French] = "Différentielle" },

                // ===== CONSOLE JOB DETAILS =====
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
                ["wpf_actions"] = new() { [Language.English] = "ACTIONS", [Language.French] = "ACTIONS" },
                ["wpf_execute_all"] = new() { [Language.English] = "▶  Execute All", [Language.French] = "▶  Tout exécuter" },
                ["wpf_execute_selected"] = new() { [Language.English] = "▷  Execute Selected", [Language.French] = "▷  Exécuter la sélection" },
                ["wpf_delete_selected"] = new() { [Language.English] = "✕  Delete Selected", [Language.French] = "✕  Supprimer la sélection" },
                ["wpf_settings"] = new() { [Language.English] = "SETTINGS", [Language.French] = "PARAMÈTRES" },
                ["wpf_business_software"] = new() { [Language.English] = "Business software", [Language.French] = "Logiciel métier" },
                ["wpf_encryption_key"] = new() { [Language.English] = "Encryption key", [Language.French] = "Clé de chiffrement" },
                ["wpf_encryption_extensions"] = new() { [Language.English] = "Encrypted extensions", [Language.French] = "Extensions chiffrées" },
                ["wpf_theme"] = new() { [Language.English] = "THEME", [Language.French] = "THÈME" },

                // ===== WPF MONITOR =====
                ["wpf_monitor_detected"] = new() { [Language.English] = "Detected — backups blocked", [Language.French] = "Détecté — sauvegardes bloquées" },
                ["wpf_monitor_not_detected"] = new() { [Language.English] = "Not detected", [Language.French] = "Non détecté" },
                ["error_business_software"] = new() { [Language.English] = "Business software detected — backups blocked", [Language.French] = "Logiciel métier détecté — sauvegardes bloquées" },
                ["business_software_cleared"] = new() { [Language.English] = "Business software closed — ready", [Language.French] = "Logiciel métier fermé — prêt" },
                ["job_stopped_business_software"] = new() { [Language.English] = "Backup stopped: business software detected", [Language.French] = "Sauvegarde arrêtée : logiciel métier détecté" },

                // ===== WPF CREATE FORM =====
                ["wpf_new_job"] = new() { [Language.English] = "New backup job", [Language.French] = "Nouveau travail de sauvegarde" },
                ["wpf_label_name"] = new() { [Language.English] = "Name", [Language.French] = "Nom" },
                ["wpf_label_source"] = new() { [Language.English] = "Source", [Language.French] = "Source" },
                ["wpf_label_target"] = new() { [Language.English] = "Target", [Language.French] = "Cible" },
                ["wpf_label_type"] = new() { [Language.English] = "Type", [Language.French] = "Type" },
                ["wpf_btn_add"] = new() { [Language.English] = "＋  Add", [Language.French] = "＋  Ajouter" },
                ["wpf_btn_update"] = new() { [Language.English] = "✓  Update", [Language.French] = "✓  Modifier" },

                // ===== WPF TABLE HEADERS =====
                ["wpf_col_name"] = new() { [Language.English] = "NAME", [Language.French] = "NOM" },
                ["wpf_col_source"] = new() { [Language.English] = "SOURCE", [Language.French] = "SOURCE" },
                ["wpf_col_target"] = new() { [Language.English] = "TARGET", [Language.French] = "CIBLE" },
                ["wpf_col_type"] = new() { [Language.English] = "TYPE", [Language.French] = "TYPE" },

                // ===== WPF STATUS BAR =====
                ["wpf_ready"] = new() { [Language.English] = "Ready", [Language.French] = "Prêt" },
                ["wpf_jobs_count"] = new() { [Language.English] = "{0} job(s)", [Language.French] = "{0} travail/travaux" },

                // ===== NAVIGATION =====
                ["nav_jobs"] = new() { [Language.English] = "Jobs", [Language.French] = "Travaux" },
                ["nav_dashboard"] = new() { [Language.English] = "Dashboard", [Language.French] = "Tableau de bord" },
                ["nav_settings"] = new() { [Language.English] = "Settings", [Language.French] = "Paramètres" },

                // ===== DASHBOARD =====
                ["dashboard_title"] = new() { [Language.English] = "Dashboard", [Language.French] = "Tableau de bord" },
                ["dashboard_total_jobs"] = new() { [Language.English] = "Total Jobs", [Language.French] = "Total travaux" },
                ["dashboard_status"] = new() { [Language.English] = "System Status", [Language.French] = "État du système" },
                ["dashboard_ready"] = new() { [Language.English] = "Ready to backup", [Language.French] = "Prêt pour la sauvegarde" },
                ["dashboard_blocked"] = new() { [Language.English] = "Backups blocked", [Language.French] = "Sauvegardes bloquées" },
                ["dashboard_log_format"] = new() { [Language.English] = "Log Format", [Language.French] = "Format des logs" },
                ["dashboard_encryption"] = new() { [Language.English] = "Encryption", [Language.French] = "Chiffrement" },
                ["dashboard_active"] = new() { [Language.English] = "Active", [Language.French] = "Actif" },

                // ===== JOBS PAGE =====
                ["jobs_title"] = new() { [Language.English] = "Backup Jobs", [Language.French] = "Travaux de sauvegarde" },
                ["jobs_create_title"] = new() { [Language.English] = "Create New Job", [Language.French] = "Créer un nouveau travail" },
                ["jobs_edit_title"] = new() { [Language.English] = "Edit Job", [Language.French] = "Modifier le travail" },
                ["jobs_empty"] = new() { [Language.English] = "No backup jobs configured", [Language.French] = "Aucun travail de sauvegarde configuré" },
                ["jobs_empty_desc"] = new() { [Language.English] = "Create your first backup job to get started", [Language.French] = "Créez votre premier travail pour commencer" },

                // ===== SETTINGS TABS =====
                ["settings_tab_general"] = new() { [Language.English] = "General", [Language.French] = "Général" },
                ["settings_tab_logs"] = new() { [Language.English] = "Logs", [Language.French] = "Journaux" },
                ["settings_tab_language"] = new() { [Language.English] = "Language", [Language.French] = "Langue" },

                // ===== SETTINGS GENERAL =====
                ["settings_business_software"] = new() { [Language.English] = "Business Software Detection", [Language.French] = "Détection logiciel métier" },
                ["settings_business_desc"] = new() { [Language.English] = "Process name to monitor (e.g. calc.exe)", [Language.French] = "Nom du processus à surveiller (ex: calc.exe)" },
                ["settings_encryption"] = new() { [Language.English] = "Encryption", [Language.French] = "Chiffrement" },
                ["settings_encryption_key"] = new() { [Language.English] = "Encryption Key", [Language.French] = "Clé de chiffrement" },
                ["settings_encryption_ext"] = new() { [Language.English] = "Target Extensions", [Language.French] = "Extensions cibles" },

                // ===== SETTINGS LOGS =====
                ["settings_log_format"] = new() { [Language.English] = "Log Format", [Language.French] = "Format des journaux" },
                ["settings_log_desc"] = new() { [Language.English] = "Choose output format for backup logs", [Language.French] = "Choisissez le format de sortie des journaux" },

                // ===== SETTINGS LANGUAGE =====
                ["settings_language_title"] = new() { [Language.English] = "Application Language", [Language.French] = "Langue de l'application" },
                ["settings_language_desc"] = new() { [Language.English] = "Select your preferred language", [Language.French] = "Sélectionnez la langue de l'application" },

                // ===== SETTINGS THEME =====
                ["settings_tab_theme"] = new() { [Language.English] = "Theme", [Language.French] = "Thème" },
                ["settings_theme_title"] = new() { [Language.English] = "Color Theme", [Language.French] = "Thème de couleurs" },
                ["settings_theme_desc"] = new() { [Language.English] = "Select your preferred color palette", [Language.French] = "Sélectionnez votre palette de couleurs préférée" },

                // ===== NOTIFICATIONS =====
                ["notif_job_created"] = new() { [Language.English] = "Job created successfully", [Language.French] = "Travail créé avec succès" },
                ["notif_job_updated"] = new() { [Language.English] = "Job updated successfully", [Language.French] = "Travail modifié avec succès" },
                ["notif_job_deleted"] = new() { [Language.English] = "Job deleted", [Language.French] = "Travail supprimé" },
                ["notif_execution_complete"] = new() { [Language.English] = "Backup completed successfully", [Language.French] = "Sauvegarde terminée avec succès" },
                ["notif_execution_error"] = new() { [Language.English] = "Backup failed", [Language.French] = "Échec de la sauvegarde" },
                ["notif_settings_saved"] = new() { [Language.English] = "Settings saved", [Language.French] = "Paramètres enregistrés" },
                ["notif_language_changed"] = new() { [Language.English] = "Language changed to English", [Language.French] = "Langue changée en Français" },
                ["notif_fields_required"] = new() { [Language.English] = "Please fill all required fields", [Language.French] = "Veuillez remplir tous les champs requis" },
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
