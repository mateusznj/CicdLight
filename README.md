<div align="center">
  <img src="Assets/app.ico" alt="CicdLight app icon" width="64" height="64" />

  # CicdLight

  **Lekki monitor Azure DevOps CI/CD w zasobniku systemowym Windows.**

  .NET 10 · WinUI 3 · Windows App SDK · H.NotifyIcon
</div>

CicdLight to aplikacja inspirowana CatLight, umożliwiająca szybkie śledzenie buildów, release'ów i pull requestów bez otwierania przeglądarki.

## Podgląd

<p align="center">
  <img src="Assets/cicdlight-screenshot.png" alt="CicdLight – lista pipeline'ów i release'ów (dane demonstracyjne)" width="385" />
</p>

> Screenshot zawiera wyłącznie przykładowe nazwy projektów, użytkowników i wersji.

## Funkcje

- **Status w trayu:** zielony (OK), niebieski (build w toku), żółty (PR czeka na Twój review), czerwony (nieudany build), szary (błąd lub brak konfiguracji).
- **Szybki podgląd:** ostatnie buildy dla pipeline'ów i branchy, release'y, PR-y do review oraz Twoje PR-y. Kliknięcie elementu otwiera go w przeglądarce.
- **Powiadomienia:** nieudany build, naprawiony build i nowa prośba o review.
- **Bezpieczne przechowywanie PAT:** szyfrowanie DPAPI (`CurrentUser`); ustawienia w `%APPDATA%\CicdLight\settings.json`.
- **Integracja z Windows:** opcjonalny autostart i uruchamianie tylko jednej instancji.

## Wymagania

- Windows 10 1809 (build 17763) lub nowszy.
- .NET 10 SDK do budowania aplikacji.
- Dostęp do Azure DevOps i token PAT z odpowiednimi uprawnieniami.

## Uruchomienie

```powershell
dotnet run
```

Wersja samodzielna (unpackaged, self-contained):

```powershell
dotnet publish -c Release -r win-x64 --self-contained -o publish
```

Przy pierwszym uruchomieniu otworzy się okno **Settings**. Skonfiguruj organizację Azure DevOps, opcjonalnie projekty (oddzielone przecinkami) oraz token PAT.

## Wymagane uprawnienia PAT

- `Build (Read)`
- `Code (Read)`
- `Project and Team (Read)`

## Pomysły na rozwój

Release pipelines, work items, Azure Resource Health (ARM API), logowanie Entra ID (MSAL) zamiast PAT.
