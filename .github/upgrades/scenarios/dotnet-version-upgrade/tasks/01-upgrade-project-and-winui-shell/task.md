# 01-upgrade-project-and-winui-shell: Upgrade do net10.0 i szkielet WinUI 3

Zmienić `CicdLight.csproj` na `net10.0-windows10.0.19041.0`, usunąć `UseWindowsForms`/`ApplicationHighDpiMode`, dodać `Microsoft.WindowsAppSDK`, `H.NotifyIcon.WinUI`, `System.Drawing.Common` i zaktualizować `System.Security.Cryptography.ProtectedData` do wersji 10.x. Projekt ma być unpackaged (`WindowsPackageType=None`, `UseWinUI=true`, `EnableMsixTooling`). Dodać `App.xaml`/`App.xaml.cs` oraz własny `Program.Main` z mutexem pojedynczej instancji (bez głównego okna; aplikacja żyje w trayu). Ryzyka z assessmentu: Api.0001–0003 dotyczą WinForms API, które zostaną usunięte przy migracji.

**Done when**: Projekt targetuje net10.0, restore przechodzi, aplikacja startuje bez okna głównego i z jedną instancją.

## Research
- Jeden projekt CicdLight.csproj (net8.0-windows, WinForms); pliki: AppSettings, AzureDevOpsClient, Models, Program, SettingsForm, TrayApplicationContext.
- Assessment: Api.0001-0003 (WinForms), NuGet.0002 (ProtectedData 8.0.0 -> 10.0.14), Project.0002 (TFM).
- Wersje: WindowsAppSDK 2.6.4, H.NotifyIcon.WinUI 2.4.1, ProtectedData 10.0.14, System.Drawing.Common 10.0.14.
- Decyzje: unpackaged + self-contained, wlasny Main (DISABLE_XAML_GENERATED_MAIN), mutex zachowany, Autostart wydzielony do Autostart.cs.
- SettingsForm.cs i TrayApplicationContext.cs usuwane; logika wraca w zadaniach 02/03.
