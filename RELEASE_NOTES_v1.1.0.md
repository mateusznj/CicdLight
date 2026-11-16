# CicdLight v1.1.0

## 🇵🇱 Polski

Wersja 1.1.0 usprawnia monitorowanie pull requestów w Azure DevOps i GitHub oraz obsługę limitów GitHub API.

### Najważniejsze zmiany

- Wyświetlanie pull requestów ze wszystkich skonfigurowanych projektów; widoczność PR-ów można włączać i wyłączać osobno dla każdego projektu.
- Liczba nierozwiązanych wątków komentarzy jest wyświetlana również przy pozostałych pull requestach.
- Aplikacja respektuje limity GitHub API: wstrzymuje zapytania do czasu resetu limitu, a następnie automatycznie wznawia odświeżanie.
- Czytelniejsze informacje o zapisanym tokenie GitHub oraz błędach autoryzacji i dostępu.
- README zawiera legendę kolorów ikon zasobnika i odnośnik do planu rozwoju.

### Instalacja

Pobierz instalator Windows x64 z sekcji **Assets** na stronie [CicdLight v1.1.0](https://github.com/mateusznj/CicdLight/releases/tag/v1.1.0). Pliki instalacyjne i publikacyjne nie są już przechowywane w repozytorium źródłowym.

## 🇬🇧 English

Version 1.1.0 improves pull request monitoring across Azure DevOps and GitHub, and handles GitHub API rate limits more reliably.

### Highlights

- Pull requests from all configured projects are shown; PR visibility can be toggled separately for each project.
- Unresolved comment thread counts are also shown for other pull requests.
- The app respects GitHub API rate limits: it pauses requests until the limit resets, then automatically resumes refreshing.
- Clearer information about stored GitHub tokens and authentication or access errors.
- The README now includes a system tray icon color legend and a link to the project roadmap.

### Installation

Download the Windows x64 installer from the **Assets** section on the [CicdLight v1.1.0 release page](https://github.com/mateusznj/CicdLight/releases/tag/v1.1.0). Installer and publish artifacts are no longer stored in the source repository.
