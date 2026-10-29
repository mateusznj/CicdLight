<div align="center">
  <img src="Assets/app.ico" alt="CicdLight icon" width="64" height="64" />

  # CicdLight

  **Lekki monitor Azure DevOps CI/CD w zasobniku systemowym Windows.**  
  **A lightweight Azure DevOps CI/CD monitor for the Windows system tray.**

  .NET 10 · WinUI 3 · Windows App SDK · H.NotifyIcon

  [🇵🇱 Polski](#-polski) · [🇬🇧 English](#-english) · [Pobierz / Download](https://github.com/mateusznj/CicdLight/releases/latest)
</div>

<p align="center">
  <img src="Assets/cicdlight-screenshot.png" alt="CicdLight app with Windows system tray icon — demo data" width="385" />
</p>

<p align="center"><sub>Podgląd zawiera dane demonstracyjne. / The screenshot uses demo data.</sub></p>

---

## 🇵🇱 Polski

CicdLight to niewielka aplikacja inspirowana CatLight, która pozwala śledzić status pipeline'ów, wydań i pull requestów w Azure DevOps bez ciągłego otwierania przeglądarki.

### Funkcje

- **Ikona w zasobniku systemowym (tray):** zielony — OK, niebieski — build w toku, żółty — PR oczekuje na Twoje review, czerwony — nieudany build, szary — błąd lub brak konfiguracji.
- **Szybki podgląd:** ostatnie buildy (pipeline + branch), wydania, PR-y oczekujące na review oraz Twoje PR-y. Kliknięcie elementu otwiera go w przeglądarce.
- **Powiadomienia:** nieudany build, naprawiony build i nowe prośby o review.
- **Ochrona tokenu PAT:** szyfrowanie DPAPI (`CurrentUser`); ustawienia zapisane w `%APPDATA%\CicdLight\settings.json`.
- **Integracja z Windows:** opcjonalny autostart oraz uruchamianie tylko jednej instancji.

### Pobieranie i instalacja

1. Przejdź do [najnowszego wydania (GitHub Releases)](https://github.com/mateusznj/CicdLight/releases/latest).
2. Pobierz odpowiedni instalator z sekcji **Assets** i uruchom go.
3. Uruchom CicdLight i w oknie **Settings** skonfiguruj organizację Azure DevOps, opcjonalną listę projektów (oddzielonych przecinkami) oraz token PAT.

> Pliki instalacyjne znajdują się również w katalogu [`artifacts`](artifacts), ale zalecanym miejscem pobierania opublikowanych wersji jest zakładka **Releases**.

### Wymagania

- Windows 10 w wersji 1809 (build 17763) lub nowszy.
- Organizacja Azure DevOps i token PAT z odpowiednimi uprawnieniami.
- .NET 10 SDK **tylko do budowania aplikacji ze źródeł**.

### Uruchomienie ze źródeł

```powershell
dotnet run
```

Publikacja samodzielnej wersji (unpackaged, self-contained):

```powershell
dotnet publish -c Release -r win-x64 --self-contained -o publish
```

### Wymagane uprawnienia PAT

- `Build (Read)`
- `Code (Read)`
- `Project and Team (Read)`

### Planowane możliwości

Obsługa kolejnych scenariuszy release pipelines, work items, Azure Resource Health (ARM API) oraz logowanie Entra ID (MSAL) zamiast PAT.

---

## 🇬🇧 English

CicdLight is a small Windows system tray application inspired by CatLight. It keeps Azure DevOps pipelines, releases and pull requests in view without requiring you to constantly check the browser.

### Features

- **System tray status:** green — OK, blue — build in progress, yellow — PR awaiting your review, red — failed build, gray — error or missing configuration.
- **At-a-glance overview:** recent builds (pipeline + branch), releases, PRs awaiting review and your own PRs. Click an item to open it in the browser.
- **Notifications:** failed builds, recovered builds and new review requests.
- **PAT protection:** Windows DPAPI encryption (`CurrentUser`); settings stored in `%APPDATA%\CicdLight\settings.json`.
- **Windows integration:** optional startup with Windows and single-instance execution.

### Download and installation

1. Open the [latest GitHub Release](https://github.com/mateusznj/CicdLight/releases/latest).
2. Download the appropriate installer from **Assets** and run it.
3. Start CicdLight. In **Settings**, configure your Azure DevOps organization, an optional comma-separated project list, and your PAT.

> Installer files are also available in [`artifacts`](artifacts), but **Releases** is the recommended location for downloading published versions.

### Requirements

- Windows 10 version 1809 (build 17763) or later.
- An Azure DevOps organization and a PAT with the required permissions.
- .NET 10 SDK **only if building from source**.

### Run from source

```powershell
dotnet run
```

Publish a self-contained unpackaged build:

```powershell
dotnet publish -c Release -r win-x64 --self-contained -o publish
```

### Required PAT permissions

- `Build (Read)`
- `Code (Read)`
- `Project and Team (Read)`

### Roadmap ideas

Additional release pipeline scenarios, work items, Azure Resource Health (ARM API), and Entra ID (MSAL) sign-in instead of PAT.
