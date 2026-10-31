using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Graphics;

namespace CicdLight;

public sealed class SettingsWindow : Window
{
    private readonly ComboBox _provider = new() { MinWidth = 300 };
    private readonly TextBox _target = new() { MinWidth = 300 };
    private readonly TextBox _project = new() { MinWidth = 300 };
    private readonly StackPanel _projectField = new() { Spacing = 4 };
    private readonly PasswordBox _credential = new() { MinWidth = 300 };
    private readonly StackPanel _connections = new() { Spacing = 8 };
    private readonly TextBlock _connectionError = new() { TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock _targetLabel = new();
    private readonly TextBlock _projectLabel = new();
    private readonly TextBlock _credentialLabel = new();
    private readonly StackPanel _connectionsPage = new() { Spacing = 12, Padding = new Thickness(20) };
    private readonly StackPanel _generalPage = new() { Spacing = 10, Padding = new Thickness(20) };
    private readonly Grid _pageHost = new();
    private readonly NumberBox _poll = new() { Minimum = 15, Maximum = 3600, SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact, SmallChange = 15, MinWidth = 120, HorizontalAlignment = HorizontalAlignment.Left };
    private readonly CheckBox _notify = new() { Content = Strings.Get("Settings_Notify") };
    private readonly CheckBox _mine = new() { Content = Strings.Get("Settings_Mine") };
    private readonly CheckBox _startup = new() { Content = Strings.Get("Settings_Startup") };
    private readonly ComboBox _language = new() { MinWidth = 200, Items = { Strings.Get("Language_System"), "English", "Polski" } };
    private AppSettings _s;
    private readonly Action _onSaved;
    private readonly Action<IntegrationConnection> _onChoosePipelines;

    public SettingsWindow(AppSettings settings, Action onSaved, Action<IntegrationConnection> onChoosePipelines)
    {
        _s = settings;
        _onSaved = onSaved;
        _onChoosePipelines = onChoosePipelines;
        Title = Strings.Get("Settings_Title");
        WindowIcon.Apply(this);
        AppWindow.Resize(new SizeInt32(760, 720));

        _provider.Items.Add(Strings.Get("Connection_AzureDevOps"));
        _provider.Items.Add(Strings.Get("Connection_GitHub"));
        _provider.SelectedIndex = 0;
        UpdateProviderFields();
        _provider.SelectionChanged += (_, _) => UpdateProviderFields();
        RenderConnections();
        _poll.Value = Math.Clamp(settings.PollSeconds, 15, 3600);
        _notify.IsChecked = settings.Notifications;
        _mine.IsChecked = settings.OnlyMyBuilds;
        _startup.IsChecked = settings.StartWithWindows;
        _language.SelectedIndex = settings.Language switch { "en" => 1, "pl" => 2, _ => 0 };
        var save = new Button { Content = Strings.Get("Button_Save"), Style = (Style)Application.Current.Resources["AccentButtonStyle"] };
        var cancel = new Button { Content = Strings.Get("Button_Cancel") };
        save.Click += (_, _) => { Apply(); Close(); _onSaved(); };
        cancel.Click += (_, _) => Close();

        _connectionsPage.Children.Add(new TextBlock { Text = Strings.Get("Connections_Title"), FontSize = 20, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        _connectionsPage.Children.Add(BuildConnectionForm());
        _connectionsPage.Children.Add(new TextBlock { Text = Strings.Get("Connections_ListTitle"), FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Margin = new Thickness(0, 8, 0, 0) });
        _connectionsPage.Children.Add(_connections);
        _generalPage.Children.Add(new TextBlock { Text = Strings.Get("Settings_General"), FontSize = 20, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        _generalPage.Children.Add(Labeled(Strings.Get("Settings_Poll"), _poll));
        _generalPage.Children.Add(Labeled(Strings.Get("Settings_Language"), _language));
        _generalPage.Children.Add(_notify);
        _generalPage.Children.Add(_mine);
        _generalPage.Children.Add(_startup);

        var connectionsNav = new Button { Content = Strings.Get("Settings_Connections"), HorizontalAlignment = HorizontalAlignment.Stretch };
        var generalNav = new Button { Content = Strings.Get("Settings_General"), HorizontalAlignment = HorizontalAlignment.Stretch };
        connectionsNav.Click += (_, _) => ShowPage(_connectionsPage);
        generalNav.Click += (_, _) => ShowPage(_generalPage);
        var navigation = new StackPanel
        {
            Spacing = 4,
            Padding = new Thickness(12),
            Children = { connectionsNav, generalNav }
        };

        var saveButtons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { save, cancel }
        };
        var root = new Grid { ColumnSpacing = 4 };
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(180) });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        Grid.SetRowSpan(navigation, 2);
        root.Children.Add(navigation);
        Grid.SetColumn(_pageHost, 1);
        root.Children.Add(_pageHost);
        Grid.SetColumn(saveButtons, 1);
        Grid.SetRow(saveButtons, 1);
        root.Children.Add(saveButtons);
        ShowPage(_connectionsPage);
        Content = root;
        root.Loaded += (_, _) => FitHeight(root);
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

    private void ShowPage(FrameworkElement page)
    {
        _pageHost.Children.Clear();
        _pageHost.Children.Add(new ScrollViewer { Content = page });
    }

    private StackPanel BuildConnectionForm()
    {
        var form = new StackPanel { Spacing = 8 };
        form.Children.Add(Labeled(Strings.Get("Connection_Provider"), _provider));
        var target = new StackPanel { Spacing = 4, Children = { _targetLabel, _target } };
        _projectField.Children.Add(_projectLabel);
        _projectField.Children.Add(_project);
        form.Children.Add(target);
        form.Children.Add(_projectField);
        form.Children.Add(LabeledDynamicCredential());

        var add = new Button { Content = Strings.Get("Button_AddConnection"), MinWidth = 140 };
        add.Click += (_, _) => AddConnection();
        _target.KeyDown += (_, e) =>
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                AddConnection();
                e.Handled = true;
            }
        };
        form.Children.Add(_connectionError);
        form.Children.Add(add);
        return form;
    }

    private StackPanel LabeledDynamicCredential() => new()
    {
        Spacing = 4,
        Children = { _credentialLabel, _credential }
    };

    private void UpdateProviderFields()
    {
        var isAzure = _provider.SelectedIndex == 0;
        _targetLabel.Text = Strings.Get(isAzure ? "Connection_Organization" : "Connection_Repository");
        _target.PlaceholderText = Strings.Get(isAzure ? "Connection_OrganizationPlaceholder" : "Connection_RepositoryPlaceholder");
        _projectLabel.Text = Strings.Get("Connection_Project");
        _projectField.Visibility = isAzure ? Visibility.Visible : Visibility.Collapsed;
        _credentialLabel.Text = Strings.Get(isAzure ? "Connection_AzurePat" : "Connection_GitHubToken");
        _credential.PlaceholderText = isAzure ? "" : Strings.Get("Connection_GitHubTokenOptional");
    }

    private void AddConnection()
    {
        var isAzure = _provider.SelectedIndex == 0;
        var target = _target.Text.Trim().TrimEnd('/');
        var project = _project.Text.Trim();
        var credential = _credential.Password.Trim();
        if (target.Length == 0 || (isAzure && (project.Length == 0 || credential.Length == 0)))
        {
            _connectionError.Text = Strings.Get("Connection_RequiredFields");
            return;
        }

        if (isAzure)
        {
            if (_s.Connections.Any(connection => connection.Provider == IntegrationProvider.AzureDevOps &&
                connection.Organization.Equals(target, StringComparison.OrdinalIgnoreCase) &&
                connection.Project.Equals(project, StringComparison.OrdinalIgnoreCase)))
            {
                _connectionError.Text = Strings.Get("Connection_AlreadyExists");
                return;
            }
        }
        else
        {
            try
            {
                var canonicalUrl = GitHubActionsClient.GetRepositoryUrl(target);
                if (_s.Connections.Any(connection => connection.Provider == IntegrationProvider.GitHub &&
                    string.Equals(GitHubActionsClient.GetRepositoryUrl(connection.Repository), canonicalUrl, StringComparison.OrdinalIgnoreCase)))
                {
                    _connectionError.Text = Strings.Get("Connection_AlreadyExists");
                    return;
                }
            }
            catch (FormatException)
            {
                _connectionError.Text = Strings.Get("Settings_InvalidGitHubRepository");
                return;
            }
        }

        var connection = new IntegrationConnection
        {
            Provider = isAzure ? IntegrationProvider.AzureDevOps : IntegrationProvider.GitHub,
            Organization = isAzure ? target : "",
            Project = isAzure ? project : "",
            Repository = isAzure ? "" : target
        };
        connection.Credential = credential;
        _s.Connections.Add(connection);
        _target.Text = "";
        _project.Text = "";
        _credential.Password = "";
        _connectionError.Text = "";
        Apply();
        _s.Save();
        RenderConnections();
        _onSaved();
    }

    private void RenderConnections()
    {
        _connections.Children.Clear();
        foreach (var connection in _s.Connections)
        {
            var title = new TextBlock { Text = connection.DisplayName, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis };
            var subtitle = new TextBlock
            {
                Text = connection.LastError ?? Strings.Get("Connection_Configured"),
                FontSize = 12,
                Opacity = 0.75,
                TextWrapping = TextWrapping.Wrap
            };
            var texts = new StackPanel { Spacing = 2, Children = { title, subtitle } };
            var pipelines = new Button { Content = Strings.Get("Connection_Pipelines") };
            pipelines.Click += (_, _) =>
            {
                Apply();
                _s.Save();
                _onSaved();
                _onChoosePipelines(connection);
            };
            var remove = new Button
            {
                Content = new FontIcon { Glyph = "\uE74D", FontSize = 14 },
                MinWidth = 36,
                Padding = new Thickness(6, 2, 6, 2)
            };
            ToolTipService.SetToolTip(remove, Strings.Get("Button_Remove"));
            remove.Click += (_, _) =>
            {
                Apply();
                _s.Connections.Remove(connection);
                _s.Save();
                RenderConnections();
                _onSaved();
            };
            var row = new Grid { ColumnSpacing = 8, Padding = new Thickness(0, 6, 0, 6) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.Children.Add(texts);
            Grid.SetColumn(pipelines, 1);
            row.Children.Add(pipelines);
            Grid.SetColumn(remove, 2);
            row.Children.Add(remove);
            _connections.Children.Add(row);
        }
        if (_s.Connections.Count == 0)
            _connections.Children.Add(new TextBlock { Text = Strings.Get("Connections_Empty"), Opacity = 0.7 });
    }

    public void RefreshConnectionStatuses() => RenderConnections();

    public void UpdateSettings(AppSettings settings)
    {
        _s = settings;
        RenderConnections();
    }

    private void Apply()
    {
        _s.PollSeconds = double.IsNaN(_poll.Value) ? 60 : (int)Math.Clamp(_poll.Value, 15, 3600);
        _s.Notifications = _notify.IsChecked == true;
        _s.OnlyMyBuilds = _mine.IsChecked == true;
        _s.StartWithWindows = _startup.IsChecked == true;
        _s.Language = _language.SelectedIndex switch { 1 => "en", 2 => "pl", _ => "" };
        _s.Save();
        Autostart.Set(_s.StartWithWindows);
    }

}
