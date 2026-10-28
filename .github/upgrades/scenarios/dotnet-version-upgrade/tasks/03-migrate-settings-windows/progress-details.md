# Progress — 03-migrate-settings-windows

## Zmiany
- `SettingsWindow.cs` (WinUI 3 `Window`, UI budowany w kodzie): organizacja, projekty, PAT (`PasswordBox`), interwał (`NumberBox` 15–3600), powiadomienia, tylko moje buildy, autostart. Zapis przez `AppSettings.Save()` (format settings.json i DPAPI bez zmian) oraz `Autostart.Set`.
- `ChoosePipelinesWindow.cs`: lista pól wyboru pipelines/releases; zapis jak wcześniej (wszystkie zaznaczone = pusta lista).
- `TrayController.cs`: `ShowSettings()` i `ChoosePipelines()` otwierają okna (jedna instancja każdego), Settings otwiera się przy pierwszym starcie bez konfiguracji. Brak danych do wyboru → powiadomienie zamiast MessageBox.

## Walidacja
- Build: sukces. Działanie okien wymaga ręcznej weryfikacji po uruchomieniu.
- Commit wykonuje użytkownik ręcznie (terminal IDE niestabilny).
