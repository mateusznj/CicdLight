using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Graphics;

namespace CicdLight;

public sealed class SettingsWindow : Window
{
    private readonly TextBox _org = new() { MinWidth = 300 };
    private readonly TextBox _projects = new() { MinWidth = 300 };
    private readonly PasswordBox _pat = new() { MinWidth = 300 };
    private readonly NumberBox _poll = new() { Minimum = 15, Maximum = 3600, SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact, SmallChange = 15, MinWidth = 120, HorizontalAlignment = HorizontalAlignment.Left };
    private readonly CheckBox _notify = new() { Content = Strings.Get("Settings_Notify") };
    private readonly CheckBox _mine = new() { Content = Strings.Get("Settings_Mine") };
    private readonly CheckBox _startup = new() { Content = Strings.Get("Settings_Startup") };
    private readonly ComboBox _language = new() { MinWidth = 200, Items = { Strings.Get("Language_System"), "English", "Polski" } };
    private readonly AppSettings _s;
    private readonly Action _onSaved;

    public SettingsWindow(AppSettings settings, Action onSaved)
    {
        _s = settings;
        _onSaved = onSaved;
        Title = Strings.Get("Settings_Title");
        WindowIcon.Apply(this);
        AppWindow.Resize(new SizeInt32(520, 620));

        _org.Text = settings.Organization;
        _projects.Text = settings.Projects;
        _pat.Password = settings.Pat;
        _poll.Value = Math.Clamp(settings.PollSeconds, 15, 3600);
        _notify.IsChecked = settings.Notifications;
        _mine.IsChecked = settings.OnlyMyBuilds;
        _startup.IsChecked = settings.StartWithWindows;
        _language.SelectedIndex = settings.Language switch { "en" => 1, "pl" => 2, _ => 0 };
        var save = new Button { Content = Strings.Get("Button_Save"), Style = (Style)Application.Current.Resources["AccentButtonStyle"] };
        var cancel = new Button { Content = Strings.Get("Button_Cancel") };
        save.Click += (_, _) => { Apply(); Close(); _onSaved(); };
        cancel.Click += (_, _) => Close();

        var panel = new StackPanel { Spacing = 10, Padding = new Thickness(20) };
        panel.Children.Add(Labeled(Strings.Get("Settings_Org"), _org));
        panel.Children.Add(Labeled(Strings.Get("Settings_Projects"), _projects));
        panel.Children.Add(Labeled(Strings.Get("Settings_Pat"), _pat));
        panel.Children.Add(Labeled(Strings.Get("Settings_Poll"), _poll));
        panel.Children.Add(Labeled(Strings.Get("Settings_Language"), _language));
        panel.Children.Add(_notify);
        panel.Children.Add(_mine);
        panel.Children.Add(_startup);
        panel.Children.Add(new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { save, cancel }
        });
        Content = new ScrollViewer { Content = panel };
        panel.Loaded += (_, _) => FitHeight(panel);
    }

    /// <summary>Dopasowuje wysokość okna do zawartości (ogranicza do obszaru roboczego ekranu).</summary>
    private void FitHeight(FrameworkElement content)
    {
        var scale = content.XamlRoot?.RasterizationScale ?? 1.0;
        var chrome = AppWindow.Size.Height - AppWindow.ClientSize.Height;
        var wanted = (int)Math.Ceiling(content.ActualHeight * scale) + chrome;
        var area = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Nearest).WorkArea;
        AppWindow.Resize(new SizeInt32(AppWindow.Size.Width, Math.Min(wanted, area.Height)));
    }

    private static StackPanel Labeled(string label, FrameworkElement control) => new()
    {
        Spacing = 4,
        Children = { new TextBlock { Text = label }, control }
    };

    private void Apply()
    {
        _s.Organization = _org.Text.Trim();
        _s.Projects = _projects.Text.Trim();
        _s.Pat = _pat.Password.Trim();
        _s.PollSeconds = double.IsNaN(_poll.Value) ? 60 : (int)Math.Clamp(_poll.Value, 15, 3600);
        _s.Notifications = _notify.IsChecked == true;
        _s.OnlyMyBuilds = _mine.IsChecked == true;
        _s.StartWithWindows = _startup.IsChecked == true;
        _s.Language = _language.SelectedIndex switch { 1 => "en", 2 => "pl", _ => "" };
        _s.Save();
        Autostart.Set(_s.StartWithWindows);
    }

}
