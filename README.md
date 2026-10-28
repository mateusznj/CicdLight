# CicdLight

Aplikacja w zasobniku systemowym Windows (.NET 10, WinUI 3 / Windows App SDK + H.NotifyIcon) monitorująca Azure DevOps — w stylu CatLight.

## Funkcje
- Ikona w trayu zmienia kolor: zielony (OK), niebieski (build w toku), żółty (PR czeka na Twój review), czerwony (nieudany build), szary (błąd/brak konfiguracji).
- Menu (lewy/prawy klik): ostatnie buildy per pipeline+branch, PR do review, Twoje PR-y; klik otwiera stronę w przeglądarce.
- Powiadomienia: nieudany build, naprawiony build, nowa prośba o review.
- PAT szyfrowany DPAPI (CurrentUser), ustawienia w `%APPDATA%\CicdLight\settings.json`.
- Opcjonalny autostart z Windows, pojedyncza instancja.

## Wymagania
- Windows 10 1809 (build 17763) lub nowszy, .NET 10 SDK (do budowania).

## Uruchomienie
```powershell
dotnet run
# lub wersja samodzielna (unpackaged, self-contained):
dotnet publish -c Release -r win-x64 --self-contained -o publish
```
Przy pierwszym starcie otworzy się okno Settings: organizacja, (opcjonalnie) projekty oddzielone przecinkami oraz PAT.

## Wymagane uprawnienia PAT
`Build (Read)`, `Code (Read)`, `Project and Team (Read)`.

## Pomysły na rozwój
Release pipelines, work items, Azure Resource Health (ARM API), logowanie Entra ID (MSAL) zamiast PAT.
