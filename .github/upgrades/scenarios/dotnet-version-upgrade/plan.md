# .NET Version Upgrade Plan

## Overview

**Target**: CicdLight — upgrade z .NET 8 do .NET 10 (LTS) oraz migracja UI z WinForms do WinUI 3 (Windows App SDK) z zachowaniem wszystkich funkcji.
**Scope**: 1 projekt (CicdLight.csproj), mały (~6 plików kodu), bez zależności projektowych.

**Strategy**: All-at-Once — jeden projekt, atomowa zmiana.

## Upgrade Options

| Option | Selected | Why |
|--------|----------|-----|
| Upgrade Strategy | All-at-Once | Pojedynczy projekt bez referencji projektowych |
| UI Framework | WinUI 3 (unpackaged) + H.NotifyIcon.WinUI | Wybór użytkownika; WinUI 3 nie ma natywnego tray, więc ikona i menu przez H.NotifyIcon |

## Tasks

### 01-upgrade-project-and-winui-shell: Upgrade do net10.0 i szkielet WinUI 3

Zmienić `CicdLight.csproj` na `net10.0-windows10.0.19041.0`, usunąć `UseWindowsForms`/`ApplicationHighDpiMode`, dodać `Microsoft.WindowsAppSDK`, `H.NotifyIcon.WinUI`, `System.Drawing.Common` i zaktualizować `System.Security.Cryptography.ProtectedData` do wersji 10.x. Projekt ma być unpackaged (`WindowsPackageType=None`, `UseWinUI=true`, `EnableMsixTooling`). Dodać `App.xaml`/`App.xaml.cs` oraz własny `Program.Main` z mutexem pojedynczej instancji (bez głównego okna; aplikacja żyje w trayu). Ryzyka z assessmentu: Api.0001–0003 dotyczą WinForms API, które zostaną usunięte przy migracji.

**Done when**: Projekt targetuje net10.0, restore przechodzi, aplikacja startuje bez okna głównego i z jedną instancją.

---

### 02-migrate-tray-and-menu: Migracja ikony tray, menu i powiadomień

Przepisać `TrayApplicationContext` na kontroler oparty o `TaskbarIcon` (H.NotifyIcon): dynamiczna ikona kolorowa (zielony/niebieski/żółty/czerwony/szary) generowana przez System.Drawing, tooltip, menu z sekcjami Pipelines/Releases/Review requested/My pull requests otwierającymi przeglądarkę, pozycje Refresh/Open Azure DevOps/Choose pipelines/Settings/Exit, timer odpytywania (`DispatcherQueueTimer`), powiadomienia (nieudany build, naprawiony build, nowa prośba o review) z kliknięciem otwierającym URL. Zachować logikę `Notify`/baseline i `AzureDevOpsClient`.

**Done when**: Ikona zmienia kolor wg `Health`, menu działa lewym i prawym kliknięciem, powiadomienia pokazują się i otwierają URL, kod nie używa System.Windows.Forms.

---

### 03-migrate-settings-windows: Migracja okien Settings i Choose pipelines

Zastąpić `SettingsForm` oknem WinUI 3 (`SettingsWindow` XAML): organizacja, projekty, PAT (`PasswordBox`), interwał (`NumberBox`, 15–3600), powiadomienia, tylko moje buildy, autostart (zachować klasę `Autostart` z rejestrem). Zastąpić dialog „Choose pipelines" oknem z listą pól wyboru. Zapis przez `AppSettings.Save()` z szyfrowaniem DPAPI bez zmian formatu `settings.json`. Okno otwiera się przy pierwszym starcie, gdy brak konfiguracji.

**Done when**: Oba okna działają, ustawienia zapisują się zgodnie z dotychczasowym formatem, autostart włącza/wyłącza wpis w rejestrze.

---

### 04-readme-and-validation: Aktualizacja README i walidacja końcowa

Zaktualizować `README.md` (.NET 10, WinUI 3, H.NotifyIcon, wymagania, uruchomienie/publikacja unpackaged). Zbudować projekt bez błędów i ostrzeżeń, usunąć stare pliki WinForms, sprawdzić brak pozostałych referencji do WinForms.

**Done when**: Build bez błędów i ostrzeżeń, README zgodny z aktualnym stanem, brak odwołań do WinForms.
