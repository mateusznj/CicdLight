using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Graphics;

namespace CicdLight;

public sealed class SettingsWindow : Window
{
    private readonly TextBox _org = new() { MinWidth = 300 };
    private readonly TextBox _azureProjectInput = new() { MinWidth = 300, PlaceholderText = Strings.Get("Settings_AzureProjectPlaceholder") };
    private readonly PasswordBox _pat = new() { MinWidth = 300 };
    private readonly TextBox _githubRepositoryInput = new() { MinWidth = 300, PlaceholderText = Strings.Get("Settings_GitHubRepositoryPlaceholder") };
    private readonly PasswordBox _githubToken = new() { MinWidth = 300 };
    private readonly List<string> _azureProjects = [];
    private readonly List<string> _githubRepositories = [];
    private readonly StackPanel _azureProjectRows = new() { Spacing = 2 };
    private readonly StackPanel _githubRepositoryRows = new() { Spacing = 2 };
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
        AppWindow.Resize(new SizeInt32(560, 720));

        _org.Text = settings.Organization;
        _pat.Password = settings.Pat;
        _azureProjects.AddRange(ParseEntries(settings.Projects));
        _githubRepositories.AddRange(ParseEntries(settings.GitHubRepositories));
        _githubToken.Password = settings.GitHubToken;
        RenderEntries(_azureProjects, _azureProjectRows);
        RenderEntries(_githubRepositories, _githubRepositoryRows);
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
        panel.Children.Add(Labeled(Strings.Get("Settings_Pat"), _pat));
        panel.Children.Add(BuildListSection(Strings.Get("Settings_AzureProjects"), _azureProjectInput, _azureProjects, _azureProjectRows));
        panel.Children.Add(BuildListSection(Strings.Get("Settings_GitHubRepositories"), _githubRepositoryInput, _githubRepositories, _githubRepositoryRows, validateGitHubRepository: true));
        panel.Children.Add(Labeled(Strings.Get("Settings_GitHubToken"), _githubToken));
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

    private StackPanel BuildListSection(string title, TextBox input, List<string> entries, StackPanel rows, bool validateGitHubRepository = false)
    {
        var section = new StackPanel { Spacing = 4 };
        section.Children.Add(new TextBlock { Text = title });
        var error = new TextBlock { TextWrapping = TextWrapping.Wrap, Opacity = 0.8 };
        var add = new Button { Content = Strings.Get("Button_Add"), MinWidth = 84 };
        var inputRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Children = { input, add }
        };
        section.Children.Add(inputRow);
        section.Children.Add(rows);
        section.Children.Add(error);

        void AddEntry()
        {
            var value = input.Text.Trim();
            if (value.Length == 0) return;
            try
            {
                if (validateGitHubRepository)
                    _ = GitHubActionsClient.GetRepositoryUrl(value);
                if (!entries.Contains(value, StringComparer.OrdinalIgnoreCase))
                    entries.Add(value);
                input.Text = "";
                error.Text = "";
                RenderEntries(entries, rows);
            }
            catch (FormatException)
            {
                error.Text = Strings.Get("Settings_InvalidGitHubRepository");
            }
        }
        add.Click += (_, _) => AddEntry();

        input.KeyDown += (_, e) =>
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                AddEntry();
                e.Handled = true;
                add.Focus(FocusState.Programmatic);
            }
        };
        return section;
    }

    private void RenderEntries(List<string> entries, StackPanel rows)
    {
        rows.Children.Clear();
        foreach (var entry in entries)
        {
            var remove = new Button
            {
                Content = new FontIcon { Glyph = "\uE74D", FontSize = 14 },
                MinWidth = 32,
                Padding = new Thickness(6, 2, 6, 2),
                VerticalAlignment = VerticalAlignment.Center
            };
            ToolTipService.SetToolTip(remove, Strings.Get("Button_Remove"));
            remove.Click += (_, _) =>
            {
                entries.Remove(entry);
                RenderEntries(entries, rows);
            };
            var row = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                Children =
                {
                    new TextBlock { Text = entry, MaxWidth = 390, VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis },
                    remove
                }
            };
            rows.Children.Add(row);
        }
    }

    private static IEnumerable<string> ParseEntries(string value) =>
        value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase);

    private void Apply()
    {
        _s.Organization = _org.Text.Trim();
        _s.Projects = string.Join(",", _azureProjects);
        _s.Pat = _pat.Password.Trim();
        _s.GitHubRepositories = string.Join(",", _githubRepositories);
        _s.GitHubToken = _githubToken.Password.Trim();
        _s.PollSeconds = double.IsNaN(_poll.Value) ? 60 : (int)Math.Clamp(_poll.Value, 15, 3600);
        _s.Notifications = _notify.IsChecked == true;
        _s.OnlyMyBuilds = _mine.IsChecked == true;
        _s.StartWithWindows = _startup.IsChecked == true;
        _s.Language = _language.SelectedIndex switch { 1 => "en", 2 => "pl", _ => "" };
        _s.Save();
        Autostart.Set(_s.StartWithWindows);
    }

}
