# 03-migrate-settings-windows: Migracja okien Settings i Choose pipelines

Zastąpić `SettingsForm` oknem WinUI 3 (`SettingsWindow` XAML): organizacja, projekty, PAT (`PasswordBox`), interwał (`NumberBox`, 15–3600), powiadomienia, tylko moje buildy, autostart (zachować klasę `Autostart` z rejestrem). Zastąpić dialog „Choose pipelines" oknem z listą pól wyboru. Zapis przez `AppSettings.Save()` z szyfrowaniem DPAPI bez zmian formatu `settings.json`. Okno otwiera się przy pierwszym starcie, gdy brak konfiguracji.

**Done when**: Oba okna działają, ustawienia zapisują się zgodnie z dotychczasowym formatem, autostart włącza/wyłącza wpis w rejestrze.
