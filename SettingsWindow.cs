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
    private readonly CheckBox _notify = new() { Content = "Show notifications" };
    private readonly CheckBox _mine = new() { Content = "Only builds requested by me" };
    private readonly CheckBox _startup = new() { Content = "Start with Windows" };
    private readonly AppSettings _s;
    private readonly Action _onSaved;

    public SettingsWindow(AppSettings settings, Action onSaved)
    {
        _s = settings;
        _onSaved = onSaved;
        Title = "CicdLight - Settings";
        AppWindow.Resize(new SizeInt32(520, 520));

        _org.Text = settings.Organization;
        _projects.Text = settings.Projects;
        _pat.Password = settings.Pat;
        _poll.Value = Math.Clamp(settings.PollSeconds, 15, 3600);
        _notify.IsChecked = settings.Notifications;
        _mine.IsChecked = settings.OnlyMyBuilds;
        _startup.IsChecked = settings.StartWithWindows;

        var save = new Button { Content = "Save", Style = (Style)Application.Current.Resources["AccentButtonStyle"] };
        var cancel = new Button { Content = "Cancel" };
        save.Click += (_, _) => { Apply(); Close(); _onSaved(); };
        cancel.Click += (_, _) => Close();

        var panel = new StackPanel { Spacing = 10, Padding = new Thickness(20) };
        panel.Children.Add(Labeled("Organization (name or URL)", _org));
        panel.Children.Add(Labeled("Projects (comma separated, empty = all)", _projects));
        panel.Children.Add(Labeled("Personal Access Token", _pat));
        panel.Children.Add(Labeled("Refresh every (seconds)", _poll));
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
        _s.Save();
        Autostart.Set(_s.StartWithWindows);
    }
}
