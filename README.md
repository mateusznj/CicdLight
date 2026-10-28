# CicdLight

Aplikacja w zasobniku systemowym Windows (C# / .NET 8, WinForms `NotifyIcon`) monitorująca Azure DevOps — w stylu CatLight.

## Funkcje
- Ikona w trayu zmienia kolor: zielony (OK), niebieski (build w toku), żółty (PR czeka na Twój review), czerwony (nieudany build), szary (błąd/brak konfiguracji).
- Menu (lewy/prawy klik): ostatnie buildy per pipeline+branch, PR do review, Twoje PR-y; klik otwiera stronę w przeglądarce.
- Powiadomienia: nieudany build, naprawiony build, nowa prośba o review.
- PAT szyfrowany DPAPI (CurrentUser), ustawienia w `%APPDATA%\CicdLight\settings.json`.
- Opcjonalny autostart z Windows, pojedyncza instancja.

## Uruchomienie
```powershell
dotnet run
# lub wersja samodzielna:
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o publish
```
Przy pierwszym starcie otworzy się okno Settings: organizacja, (opcjonalnie) projekty oddzielone przecinkami oraz PAT.

## Wymagane uprawnienia PAT
`Build (Read)`, `Code (Read)`, `Project and Team (Read)`.

## Pomysły na rozwój
Release pipelines, work items, Azure Resource Health (ARM API), logowanie Entra ID (MSAL) zamiast PAT.
