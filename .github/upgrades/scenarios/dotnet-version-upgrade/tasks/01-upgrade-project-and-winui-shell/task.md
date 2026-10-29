# 01-upgrade-project-and-winui-shell: Upgrade do net10.0 i szkielet WinUI 3

Zmienić `CicdLight.csproj` na `net10.0-windows10.0.19041.0`, usunąć `UseWindowsForms`/`ApplicationHighDpiMode`, dodać `Microsoft.WindowsAppSDK`, `H.NotifyIcon.WinUI`, `System.Drawing.Common` i zaktualizować `System.Security.Cryptography.ProtectedData` do wersji 10.x. Projekt ma być unpackaged (`WindowsPackageType=None`, `UseWinUI=true`, `EnableMsixTooling`). Dodać `App.xaml`/`App.xaml.cs` oraz własny `Program.Main` z mutexem pojedynczej instancji (bez głównego okna; aplikacja żyje w trayu). Ryzyka z assessmentu: Api.0001–0003 dotyczą WinForms API, które zostaną usunięte przy migracji.

**Done when**: Projekt targetuje net10.0, restore przechodzi, aplikacja startuje bez okna głównego i z jedną instancją.
