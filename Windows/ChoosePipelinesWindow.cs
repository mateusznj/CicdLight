using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace CicdLight;

public sealed class PipelineConfigurationControl : UserControl
{
    private readonly List<(string Key, CheckBox Box)> _boxes = [];
    private readonly List<(string Project, CheckBox BuildFailed, CheckBox BuildFixed, CheckBox ReviewRequested, CheckBox UnresolvedComments, CheckBox ShowPullRequests)> _notificationBoxes = [];
    private readonly List<(string Project, string Repository, CheckBox Box)> _repositoryBoxes = [];

    public PipelineConfigurationControl(
        IReadOnlyList<(string Key, string Project, string Text)> items,
        IReadOnlyList<string> projects,
        IReadOnlyList<RepositoryInfo> repositories,
        ICollection<string> watched,
        IReadOnlyDictionary<string, ProjectNotificationSettings> notifications,
        ProjectNotificationSettings defaults,
        Action<List<string>, Dictionary<string, ProjectNotificationSettings>> onSaved,
        Action onCancelled)
    {
        var list = new StackPanel { Spacing = 6, Padding = new Thickness(16) };
        list.Children.Add(new TextBlock
        {
            Text = Strings.Get("Choose_Notifications"),
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 4)
        });
        var notificationGrid = new Grid { ColumnSpacing = 8, RowSpacing = 4, MinWidth = 500 };
        notificationGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        notificationGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
        notificationGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        notificationGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        notificationGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        notificationGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        notificationGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var headers = new[]
        {
            Strings.Get("Choose_Project"),
            Strings.Get("Settings_NotifyBuildFailed"),
            Strings.Get("Settings_NotifyBuildFixed"),
            Strings.Get("Settings_NotifyReview"),
            Strings.Get("Settings_NotifyUnresolvedComments"),
            Strings.Get("Choose_ShowPullRequests")
        };
        for (var column = 0; column < headers.Length; column++)
        {
            var header = new TextBlock { Text = headers[column], FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap };
            Grid.SetColumn(header, column);
            notificationGrid.Children.Add(header);
        }

        var row = 1;
        foreach (var project in projects.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(p => p, StringComparer.CurrentCultureIgnoreCase))
        {
            notificationGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var current = notifications.FirstOrDefault(pair => string.Equals(pair.Key, project, StringComparison.OrdinalIgnoreCase)).Value ?? defaults;
            var projectName = new TextBlock { Text = project, VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis };
            Grid.SetRow(projectName, row);
            notificationGrid.Children.Add(projectName);

            var failed = CreateNotificationBox(notificationGrid, project, headers[1], current.BuildFailed, 1, row);
            var fixedBuild = CreateNotificationBox(notificationGrid, project, headers[2], current.BuildFixed, 2, row);
            var review = CreateNotificationBox(notificationGrid, project, headers[3], current.ReviewRequested, 3, row);
            var unresolvedComments = CreateNotificationBox(notificationGrid, project, headers[4], current.UnresolvedComments, 4, row);
            var showPullRequests = CreateNotificationBox(notificationGrid, project, headers[5], current.ShowPullRequests, 5, row);
            _notificationBoxes.Add((project, failed, fixedBuild, review, unresolvedComments, showPullRequests));
            row++;
        }
        list.Children.Add(notificationGrid);

        var repositoryGroups = repositories
            .GroupBy(repository => repository.Project, StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group.Key, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
        if (repositoryGroups.Count > 0)
        {
            list.Children.Add(new TextBlock
            {
                Text = Strings.Get("Choose_PullRequestRepositories"),
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Margin = new Thickness(0, 12, 0, 0)
            });
            list.Children.Add(new TextBlock
            {
                Text = Strings.Get("Choose_PullRequestRepositoriesHint"),
                TextWrapping = TextWrapping.Wrap,
                Opacity = 0.7,
                Margin = new Thickness(0, 0, 0, 4)
            });
            foreach (var group in repositoryGroups)
            {
                var settings = FindProjectSettings(notifications, group.Key) ?? defaults;
                list.Children.Add(CreateRepositoryGroup(group.Key,
                    group.Select(repository => repository.Name).Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(name => name, StringComparer.CurrentCultureIgnoreCase).ToList(),
                    settings));
            }
        }
        list.Children.Add(new TextBlock
        {
            Text = Strings.Get("Choose_Pipelines"),
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Margin = new Thickness(0, 12, 0, 4)
        });

        foreach (var group in items.GroupBy(i => i.Project).OrderBy(g => g.Key, StringComparer.CurrentCultureIgnoreCase))
        {
            var children = new List<CheckBox>();
            var body = new StackPanel { Spacing = 2, Padding = new Thickness(24, 0, 0, 0) };
            foreach (var (prefix, title) in new[] { ("B:", Strings.Get("Section_Pipelines")), ("R:", Strings.Get("Section_Releases")) })
            {
                var part = group.Where(i => i.Key.StartsWith(prefix))
                    .OrderBy(i => i.Text, StringComparer.CurrentCultureIgnoreCase).ToList();
                if (part.Count == 0)
                {
                    continue;
                }

                body.Children.Add(new TextBlock
                {
                    Text = $"{title} ({part.Count})",
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    Opacity = 0.7,
                    Margin = new Thickness(0, body.Children.Count == 0 ? 0 : 8, 0, 2)
                });
                foreach (var (key, _, text) in part)
                {
                    var box = new CheckBox { Content = text, IsChecked = watched.Count == 0 || watched.Contains(key) };
                    _boxes.Add((key, box));
                    children.Add(box);
                    body.Children.Add(box);
                }
            }

            var header = new CheckBox { IsThreeState = true, Content = $"{group.Key} ({children.Count})" };
            var updating = false;
            void SyncHeader()
            {
                updating = true;
                var on = children.Count(c => c.IsChecked == true);
                header.IsChecked = on == 0 ? false : on == children.Count ? true : null;
                updating = false;
            }
            header.Click += (_, _) =>
            {
                if (updating)
                {
                    return;
                }
                var value = header.IsChecked == true;
                foreach (var c in children)
                {
                    c.IsChecked = value;
                }
                SyncHeader();
            };
            foreach (var c in children)
            {
                c.Checked += (_, _) => SyncHeader();
                c.Unchecked += (_, _) => SyncHeader();
            }
            SyncHeader();

            list.Children.Add(new Expander
            {
                Header = header,
                Content = body,
                IsExpanded = true,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Stretch
            });
        }

        var save = new Button { Content = Strings.Get("Button_Save"), Style = (Style)Application.Current.Resources["AccentButtonStyle"] };
        var cancel = new Button { Content = Strings.Get("Button_Cancel") };
        save.Click += (_, _) =>
        {
            var chosen = _boxes.Where(b => b.Box.IsChecked == true).Select(b => b.Key).ToList();
            var savedNotifications = new Dictionary<string, ProjectNotificationSettings>(notifications, StringComparer.OrdinalIgnoreCase);
            foreach (var (project, failed, fixedBuild, review, unresolvedComments, showPullRequests) in _notificationBoxes)
            {
                var listed = _repositoryBoxes.Where(box => string.Equals(box.Project, project, StringComparison.OrdinalIgnoreCase)).ToList();
                var previouslyHidden = (FindProjectSettings(notifications, project) ?? defaults).HiddenPullRequestRepositories;
                // Keep hidden repositories that were not listed (e.g. the repository list failed to load).
                var hidden = previouslyHidden
                    .Where(name => !listed.Any(box => string.Equals(box.Repository, name, StringComparison.OrdinalIgnoreCase)))
                    .Concat(listed.Where(box => box.Box.IsChecked != true).Select(box => box.Repository))
                    .ToList();
                savedNotifications[project] = new ProjectNotificationSettings
                {
                    BuildFailed = failed.IsChecked == true,
                    BuildFixed = fixedBuild.IsChecked == true,
                    ReviewRequested = review.IsChecked == true,
                    UnresolvedComments = unresolvedComments.IsChecked == true,
                    ShowPullRequests = showPullRequests.IsChecked == true,
                    HiddenPullRequestRepositories = hidden
                };
            }
            onSaved(chosen, savedNotifications);
        };
        cancel.Click += (_, _) => onCancelled();

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Padding = new Thickness(16),
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { save, cancel }
        };

        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        var scroll = new ScrollViewer { Content = list }.WithWheelFallback();
        Grid.SetRow(buttons, 1);
        root.Children.Add(scroll);
        root.Children.Add(buttons);
        Content = root;
    }

    private static ProjectNotificationSettings? FindProjectSettings(
        IReadOnlyDictionary<string, ProjectNotificationSettings> notifications, string project) =>
        notifications.FirstOrDefault(pair => string.Equals(pair.Key, project, StringComparison.OrdinalIgnoreCase)).Value;

    private Expander CreateRepositoryGroup(string project, IReadOnlyList<string> names, ProjectNotificationSettings settings)
    {
        var children = new List<CheckBox>();
        var body = new StackPanel { Spacing = 2, Padding = new Thickness(24, 0, 0, 0) };
        foreach (var name in names)
        {
            var box = new CheckBox { Content = name, IsChecked = !settings.IsRepositoryHidden(name) };
            _repositoryBoxes.Add((project, name, box));
            children.Add(box);
            body.Children.Add(box);
        }

        var header = new CheckBox { IsThreeState = true, Content = $"{project} ({names.Count})" };
        var updating = false;
        void SyncHeader()
        {
            updating = true;
            var on = children.Count(c => c.IsChecked == true);
            header.IsChecked = on == 0 ? false : on == children.Count ? true : null;
            updating = false;
        }
        header.Click += (_, _) =>
        {
            if (updating)
            {
                return;
            }
            var value = header.IsChecked == true;
            foreach (var c in children)
            {
                c.IsChecked = value;
            }
            SyncHeader();
        };
        foreach (var c in children)
        {
            c.Checked += (_, _) => SyncHeader();
            c.Unchecked += (_, _) => SyncHeader();
        }
        SyncHeader();

        return new Expander
        {
            Header = header,
            Content = body,
            IsExpanded = false,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch
        };
    }

    private static CheckBox CreateNotificationBox(Grid grid, string project, string eventName, bool isChecked, int column, int row)
    {
        var box = new CheckBox { IsChecked = isChecked, MinWidth = 36, HorizontalAlignment = HorizontalAlignment.Center };
        ToolTipService.SetToolTip(box, $"{project}: {eventName}");
        Grid.SetColumn(box, column);
        Grid.SetRow(box, row);
        grid.Children.Add(box);
        return box;
    }
}
