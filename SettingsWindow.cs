using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Graphics;

namespace CicdLight;

public sealed class SettingsWindow : Window
{
    private readonly StackPanel _connections = new() { Spacing = 8 };
    private readonly Button _addConnection = new();
    private readonly StackPanel _connectionsPage = new() { Spacing = 12, Padding = new Thickness(20) };
    private readonly StackPanel _generalPage = new() { Spacing = 10, Padding = new Thickness(20) };
    private readonly StackPanel _pipelinePage = new() { Spacing = 12, Padding = new Thickness(20) };
    private readonly Grid _pageHost = new();
    private readonly ScrollViewer _connectionsScroll = new();
    private readonly ScrollViewer _generalScroll = new() { Visibility = Visibility.Collapsed };
    private readonly ScrollViewer _pipelineScroll = new() { Visibility = Visibility.Collapsed };
    private StackPanel? _settingsFooter;
    private readonly NumberBox _poll = new() { Minimum = 15, Maximum = 3600, SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact, SmallChange = 15, MinWidth = 120, HorizontalAlignment = HorizontalAlignment.Left };
    private readonly CheckBox _notify = new() { Content = Strings.Get("Settings_Notify") };
    private readonly CheckBox _mine = new() { Content = Strings.Get("Settings_Mine") };
    private readonly CheckBox _startup = new() { Content = Strings.Get("Settings_Startup") };
    private readonly ComboBox _language = new() { MinWidth = 200, Items = { Strings.Get("Language_System"), "English", "Polski" } };
    private AppSettings _s;
    private readonly Action _onSaved;
    private readonly Func<IntegrationConnection, IReadOnlyList<(string Key, string Project, string Text)>> _getPipelineItems;
    private ConnectionEditorWindow? _connectionEditor;
    private IntegrationConnection? _pipelineConnection;

    public SettingsWindow(AppSettings settings, Action onSaved,
        Func<IntegrationConnection, IReadOnlyList<(string Key, string Project, string Text)>> getPipelineItems)
    {
        _s = settings;
        _onSaved = onSaved;
        _getPipelineItems = getPipelineItems;
        Title = Strings.Get("Settings_Title");
        WindowIcon.Apply(this);
        AppWindow.Resize(new SizeInt32(760, 720));

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
        _addConnection.Content = Strings.Get("Button_AddConnection");
        _addConnection.HorizontalAlignment = HorizontalAlignment.Left;
        _addConnection.Click += (_, _) => OpenConnectionEditor(null);
        _connectionsPage.Children.Add(_addConnection);
        _connectionsPage.Children.Add(new TextBlock { Text = Strings.Get("Connections_ListTitle"), FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Margin = new Thickness(0, 8, 0, 0) });
        _connectionsPage.Children.Add(_connections);
        _generalPage.Children.Add(new TextBlock { Text = Strings.Get("Settings_General"), FontSize = 20, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        _generalPage.Children.Add(Labeled(Strings.Get("Settings_Poll"), _poll));
        _generalPage.Children.Add(Labeled(Strings.Get("Settings_Language"), _language));
        _generalPage.Children.Add(_notify);
        _generalPage.Children.Add(_mine);
        _generalPage.Children.Add(_startup);

        _settingsFooter = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Margin = new Thickness(12, 12, 20, 18),
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { save, cancel }
        };
        var content = new Grid();
        content.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        _connectionsScroll.Content = _connectionsPage;
        _generalScroll.Content = _generalPage;
        _pipelineScroll.Content = _pipelinePage;
        _pageHost.Children.Add(_connectionsScroll);
        _pageHost.Children.Add(_generalScroll);
        _pageHost.Children.Add(_pipelineScroll);
        content.Children.Add(_pageHost);
        Grid.SetRow(_settingsFooter, 1);
        content.Children.Add(_settingsFooter);

        var navigation = new NavigationView
        {
            PaneDisplayMode = NavigationViewPaneDisplayMode.Left,
            OpenPaneLength = 190,
            CompactPaneLength = 44,
            IsBackButtonVisible = NavigationViewBackButtonVisible.Collapsed,
            IsPaneToggleButtonVisible = false,
            IsSettingsVisible = false,
            Content = content
        };
        navigation.MenuItems.Add(new NavigationViewItem
        {
            Content = Strings.Get("Settings_Connections"),
            Icon = new FontIcon { Glyph = "\uE8B7" },
            Tag = "connections"
        });
        navigation.MenuItems.Add(new NavigationViewItem
        {
            Content = Strings.Get("Settings_General"),
            Icon = new FontIcon { Glyph = "\uE713" },
            Tag = "general"
        });
        navigation.SelectionChanged += (_, e) =>
        {
            if (e.SelectedItem is not NavigationViewItem item) return;
            ShowPage(item.Tag as string == "general" ? _generalPage : _connectionsPage);
        };
        navigation.SelectedItem = navigation.MenuItems[0];
        ShowPage(_connectionsPage);
        Content = navigation;
        navigation.Loaded += (_, _) => FitHeight(navigation);
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
        _connectionsScroll.Visibility = ReferenceEquals(page, _connectionsPage) ? Visibility.Visible : Visibility.Collapsed;
        _generalScroll.Visibility = ReferenceEquals(page, _generalPage) ? Visibility.Visible : Visibility.Collapsed;
        _pipelineScroll.Visibility = ReferenceEquals(page, _pipelinePage) ? Visibility.Visible : Visibility.Collapsed;
        if (_settingsFooter != null)
            _settingsFooter.Visibility = ReferenceEquals(page, _pipelinePage) ? Visibility.Collapsed : Visibility.Visible;
    }

    private void ShowPipelineConfiguration(IntegrationConnection connection)
    {
        _pipelineConnection = connection;
        _pipelinePage.Children.Clear();
        var back = new Button { Content = Strings.Get("Button_Back") };
        back.Click += (_, _) => ShowPage(_connectionsPage);
        _pipelinePage.Children.Add(back);
        _pipelinePage.Children.Add(new TextBlock
        {
            Text = connection.DisplayName,
            FontSize = 20,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            TextTrimming = TextTrimming.CharacterEllipsis
        });

        var items = _getPipelineItems(connection);
        if (items.Count == 0)
        {
            _pipelinePage.Children.Add(new TextBlock
            {
                Text = Strings.Get("Pipeline_NoItems"),
                TextWrapping = TextWrapping.Wrap,
                Opacity = 0.75
            });
        }
        else
        {
            var projects = items.Select(item => item.Project).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            _pipelinePage.Children.Add(new PipelineConfigurationControl(
                items,
                projects,
                connection.HasSelection ? connection.SelectedItems : [],
                _s.ProjectNotifications,
                new ProjectNotificationSettings
                {
                    BuildFailed = _s.NotifyBuildFailed,
                    BuildFixed = _s.NotifyBuildFixed,
                    ReviewRequested = _s.NotifyReviewRequested
                },
                (selected, notifications) =>
                {
                    var current = _s.Connections.FirstOrDefault(entry => entry.Id == connection.Id);
                    if (current != null)
                    {
                        current.SelectedItems = selected;
                        current.HasSelection = true;
                    }
                    _s.ProjectNotifications = notifications;
                    Apply();
                    _onSaved();
                    RenderConnections();
                    ShowPage(_connectionsPage);
                },
                () => ShowPage(_connectionsPage)));
        }

        ShowPage(_pipelinePage);
    }

    private void OpenConnectionEditor(IntegrationConnection? connection)
    {
        if (_connectionEditor != null)
        {
            TrayPopupWindow.BringToFront(_connectionEditor);
            return;
        }

        _connectionEditor = new ConnectionEditorWindow(connection, IsDuplicateConnection, SaveConnection);
        _connectionEditor.Closed += (_, _) => _connectionEditor = null;
        TrayPopupWindow.BringToFront(_connectionEditor);
    }

    private bool IsDuplicateConnection(IntegrationConnection candidate) => _s.Connections.Any(connection =>
        connection.Id != candidate.Id && connection.Provider == candidate.Provider &&
        (candidate.Provider == IntegrationProvider.AzureDevOps
            ? connection.Organization.Equals(candidate.Organization, StringComparison.OrdinalIgnoreCase) &&
              connection.Project.Equals(candidate.Project, StringComparison.OrdinalIgnoreCase)
            : string.Equals(GitHubActionsClient.GetRepositoryUrl(connection.Repository),
                GitHubActionsClient.GetRepositoryUrl(candidate.Repository), StringComparison.OrdinalIgnoreCase)));

    private void SaveConnection(IntegrationConnection connection)
    {
        var existingIndex = _s.Connections.FindIndex(item => item.Id == connection.Id);
        if (existingIndex < 0) _s.Connections.Add(connection);
        else _s.Connections[existingIndex] = connection;
        _s.Save();
        _onSaved();
        RenderConnections();
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
            pipelines.Click += (_, _) => ShowPipelineConfiguration(connection);
            var edit = new Button { Content = Strings.Get("Button_Edit") };
            edit.Click += (_, _) => OpenConnectionEditor(connection);
            var remove = new Button
            {
                Content = new FontIcon { Glyph = "\uE74D", FontSize = 14 },
                MinWidth = 36,
                Padding = new Thickness(6, 2, 6, 2)
            };
            ToolTipService.SetToolTip(remove, Strings.Get("Button_Remove"));
            remove.Click += async (_, _) => await ConfirmRemoveConnectionAsync(connection);
            var row = new Grid { ColumnSpacing = 8, Padding = new Thickness(0, 8, 8, 8) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.Children.Add(texts);
            Grid.SetColumn(pipelines, 1);
            row.Children.Add(pipelines);
            Grid.SetColumn(edit, 2);
            row.Children.Add(edit);
            Grid.SetColumn(remove, 3);
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

    private async Task ConfirmRemoveConnectionAsync(IntegrationConnection connection)
    {
        var dialog = new ContentDialog
        {
            Title = Strings.Get("Connection_DeleteTitle"),
            Content = Strings.Format("Connection_DeleteConfirmation", connection.DisplayName),
            PrimaryButtonText = Strings.Get("Button_Remove"),
            CloseButtonText = Strings.Get("Button_Cancel"),
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = _connections.XamlRoot
        };
        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;

        _s.Connections.Remove(connection);
        _s.Save();
        _onSaved();
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
