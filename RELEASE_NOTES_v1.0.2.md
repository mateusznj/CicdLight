# CicdLight v1.0.2

## 🇵🇱 Polski

CicdLight 1.0.2 dodaje obsługę pull requestów GitHub oraz powiadomienia o nierozwiązanych komentarzach. Ułatwia też sprawdzanie działania powiadomień i otwieranie skonfigurowanych projektów.

### Najważniejsze zmiany

- Podgląd pull requestów GitHub oczekujących na Twoje review oraz Twoich własnych PR-ów.
- Licznik nierozwiązanych wątków komentarzy przy pull requestach.
- Żółta ikona aplikacji w zasobniku, gdy PR oczekuje na review lub ma nierozwiązane komentarze.
- Powiadomienia o nowych nierozwiązanych komentarzach, konfigurowane osobno dla każdego projektu lub repozytorium.
- Przycisk testowego powiadomienia w **Ustawienia → Ogólne**.
- Osobne pozycje menu do otwierania skonfigurowanych projektów Azure DevOps i repozytoriów GitHub.
- Poprawki wyświetlania czasu od ostatniego builda, przycisku usuwania połączenia i zduplikowanych wpisów GitHub.

### Instalacja

1. Pobierz `CicdLight-1.0.2-x64.msi` z sekcji Assets poniżej.
2. Uruchom instalator, a następnie CicdLight.
3. W **Ustawieniach** skonfiguruj połączenia Azure DevOps lub GitHub. Powiadomienia o komentarzach możesz ustawić osobno dla każdego projektu lub repozytorium.

Wymagania: Windows 10 1809 lub nowszy. Azure DevOps wymaga PAT z uprawnieniami `Build (Read)`, `Code (Read)` i `Release (Read)`; `Project and Team (Read)` jest potrzebne, gdy lista projektów jest pusta. Prywatne repozytoria GitHub wymagają tokenu z uprawnieniami `Actions: Read` i `Pull requests: Read`.

[Repozytorium / Repository](https://github.com/mateusznj/CicdLight) · [Zgłoś problem / Report an issue](https://github.com/mateusznj/CicdLight/issues)

## 🇬🇧 English

CicdLight 1.0.2 adds GitHub pull request monitoring and notifications for unresolved comments. It also makes it easier to test notifications and open configured projects.

### Highlights

- View GitHub pull requests awaiting your review and your own PRs.
- See the number of unresolved comment threads on pull requests.
- The system tray icon turns yellow when a PR needs your review or has unresolved comments.
- Notifications for newly unresolved comments, configurable separately for each project or repository.
- A test notification button in **Settings → General**.
- Separate menu entries for opening configured Azure DevOps projects and GitHub repositories.
- Fixes for relative build time display, connection delete button sizing, and duplicate GitHub entries.

### Installation

1. Download `CicdLight-1.0.2-x64.msi` from the Assets section below.
2. Run the installer, then launch CicdLight.
3. In **Settings**, configure your Azure DevOps or GitHub connections. Comment notifications can be configured separately for each project or repository.

Requirements: Windows 10 1809 or later. Azure DevOps requires a PAT with `Build (Read)`, `Code (Read)` and `Release (Read)` permissions; `Project and Team (Read)` is required when the project list is empty. Private GitHub repositories require a token with `Actions: Read` and `Pull requests: Read` permissions.

[Repository](https://github.com/mateusznj/CicdLight) · [Report an issue](https://github.com/mateusznj/CicdLight/issues)
