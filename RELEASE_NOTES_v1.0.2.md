# CicdLight v1.0.2

## Polski

### Nowości
- Dodano obsługę pull requestów GitHub: podgląd PR-ów oczekujących na Twoje review oraz Twoich własnych PR-ów.
- Przy PR-ach wyświetlana jest liczba nierozwiązanych wątków komentarzy.
- Ikona aplikacji w zasobniku ostrzega na żółto o nierozwiązanych komentarzach lub oczekującym review.
- Dodano powiadomienia o nowych nierozwiązanych komentarzach, konfigurowane osobno dla każdego projektu lub repozytorium.
- Dodano przycisk testowego powiadomienia w **Ustawienia → Ogólne**.
- Menu pozwala otworzyć osobno każde skonfigurowane repozytorium GitHub lub projekt Azure DevOps.

### Ulepszenia i poprawki
- Błąd pobierania pull requestów nie ukrywa już wyników buildów GitHub Actions.
- Usunięto zduplikowane wpisy GitHub w ustawieniach powiadomień.
- Poprawiono rozmiar przycisku usuwania połączenia.
- Naprawiono wyświetlanie czasu od ostatniego builda i uporządkowano obsługę ikon.

### Instalator
- Instalator MSI x64 dla Windows.

## English

### New features
- Added GitHub pull request monitoring: view PRs awaiting your review and your own PRs.
- Pull requests now show the number of unresolved comment threads.
- The system tray icon turns yellow when a PR needs your review or has unresolved comments.
- Added notifications for newly unresolved PR comments, configurable per project or repository.
- Added a test notification button in **Settings → General**.
- The menu can open each configured GitHub repository or Azure DevOps project separately.

### Improvements and fixes
- A pull request fetch error no longer hides GitHub Actions build results.
- Removed duplicate GitHub entries from notification settings.
- Fixed the connection delete button sizing.
- Fixed relative build time display and organized icon handling.

### Installer
- Windows x64 MSI installer.
