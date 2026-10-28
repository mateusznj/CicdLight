# Progress — 02-migrate-tray-and-menu

## Zmiany
- Nowy `TrayController.cs` (zastępuje `TrayApplicationContext`): `TaskbarIcon` z H.NotifyIcon.WinUI, dynamiczna ikona (zielony/niebieski/żółty/czerwony/szary), tooltip, `MenuFlyout` w trybie PopupMenu (lewy i prawy klik), sekcje Pipelines/Releases/Review requested/My pull requests, Refresh/Open Azure DevOps/Choose pipelines/Settings/Exit, `DispatcherQueueTimer`, powiadomienia (build failed/fixed, review requested) z kliknięciem otwierającym URL.
- `App.xaml.cs` tworzy `TrayController` w `OnLaunched`.
- `ShowSettings()` i `ChoosePipelines()` to puste metody — implementacja w zadaniu 03.

## Walidacja
- Build: sukces. Kod bez System.Windows.Forms.
- Uruchomienie (kolor ikony, popup, toasty) wymaga ręcznej weryfikacji.

## Problemy
- Terminal IDE zawiesił się przy zapisie `App.xaml.cs` (plik został obcięty); odtworzony narzędziami plikowymi.
