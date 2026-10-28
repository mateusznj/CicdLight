using Microsoft.UI.Xaml;

namespace CicdLight;

public partial class App : Application
{
    private TrayController? _tray;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        // Aplikacja zyje w zasobniku systemowym - brak okna glownego.
        _tray = new TrayController();
    }
}
