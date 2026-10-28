# Progress — 01-upgrade-project-and-winui-shell

## Zmiany
- `CicdLight.csproj`: net8.0-windows → `net10.0-windows10.0.19041.0`, usunięto WinForms, dodano UseWinUI, unpackaged, self-contained WinAppSDK, Platform x64 (domyślnie), RID win-x64/win-arm64.
- Pakiety: Microsoft.WindowsAppSDK 2.6.4, H.NotifyIcon.WinUI 2.4.1, System.Drawing.Common 10.0.14, ProtectedData 8.0.0 → 10.0.14.
- Nowe: `App.xaml`, `App.xaml.cs` (bez okna głównego), `Program.cs` (własny Main, mutex pojedynczej instancji), `Autostart.cs`, `NuGet.Config` (tylko nuget.org — źródło Telerik z konfiguracji użytkownika zwracało 401).
- Usunięto: `SettingsForm.cs`, `TrayApplicationContext.cs` (logika wraca w zadaniach 02/03).

## Walidacja
- `dotnet build`: 0 błędów, 0 ostrzeżeń.
- Uruchomienie z trayem do weryfikacji po zadaniu 02.
