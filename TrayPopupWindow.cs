using System.Runtime.InteropServices;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;
using WinRT.Interop;

namespace CicdLight;

/// <summary>Okno-popup nad zasobnikiem (styl OneDrive): ostatnie buildy, PR-y i menu pod kołem zębatym.</summary>
public sealed class TrayPopupWindow : Window
{
    private const int BaseWidth = 400;
    private const int BaseHeight = 540;
    private const int MaxBuilds = 5;

    private readonly StackPanel _list = new() { Spacing = 2, Padding = new Thickness(8, 0, 8, 8) };
    private readonly TextBlock _status = new() { FontSize = 12, Opacity = 0.7 };
    private readonly Func<IReadOnlyList<IntegrationConnection>> _getConnections;
    private readonly Action _exit;
    private readonly Action<string?> _openUrl;
    private readonly IntPtr _hwnd;

    public bool IsVisible { get; private set; }
    public DateTime HiddenAtUtc { get; private set; } = DateTime.MinValue;

    public TrayPopupWindow(Action refresh, Action settings,
        Func<IReadOnlyList<IntegrationConnection>> getConnections, Action exit, Action<string?> openUrl)
    {
        _getConnections = getConnections;
        _exit = exit;
        _openUrl = openUrl;
        _hwnd = WindowNative.GetWindowHandle(this);

        var presenter = OverlappedPresenter.Create();
        presenter.SetBorderAndTitleBar(true, false);
        presenter.IsResizable = false;
        presenter.IsMaximizable = false;
        presenter.IsMinimizable = false;
        presenter.IsAlwaysOnTop = true;
        AppWindow.SetPresenter(presenter);
        AppWindow.IsShownInSwitchers = false;
        SystemBackdrop = new DesktopAcrylicBackdrop();

        var subtle = (Style)Application.Current.Resources["SubtleButtonStyle"];

        var refreshBtn = new Button { Content = new FontIcon { Glyph = Icons.GetGlyph(AppIcon.Refresh), FontSize = 16 }, Style = subtle };
        ToolTipService.SetToolTip(refreshBtn, Strings.Get("Popup_Refresh"));        refreshBtn.Click += (_, _) => refresh();

        var gear = new Button { Content = new FontIcon { Glyph = Icons.GetGlyph(AppIcon.Settings), FontSize = 16 }, Style = subtle };
        var flyout = new MenuFlyout();
        flyout.Items.Add(MenuItem(Strings.Get("Menu_Settings"), AppIcon.Settings, () => { Hide(); settings(); }));
        flyout.Opening += (_, _) => PopulateConnectionMenus(flyout);
        gear.Flyout = flyout;

        var title = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            Children = { new TextBlock { Text = "CicdLight", FontWeight = FontWeights.SemiBold, FontSize = 16 }, _status }
        };
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 2, Children = { refreshBtn, gear } };
        var header = new Grid { Padding = new Thickness(16, 12, 8, 8) };
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        header.Children.Add(title);
        Grid.SetColumn(buttons, 1);
        header.Children.Add(buttons);

        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.Children.Add(header);
        var scroll = new ScrollViewer { Content = _list };
        Grid.SetRow(scroll, 1);
        root.Children.Add(scroll);
        Content = root;

        Activated += (_, e) =>
        {
            if (e.WindowActivationState == WindowActivationState.Deactivated) Hide();
        };
    }

    public void Show(Snapshot snapshot)
    {
        Render(snapshot);

        GetCursorPos(out var cursor);
        var dpi = GetDpiForWindow(_hwnd);
        if (dpi == 0) dpi = 96;
        var w = BaseWidth * (int)dpi / 96;
        var h = BaseHeight * (int)dpi / 96;
        var margin = 8 * (int)dpi / 96;

        var area = DisplayArea.GetFromPoint(new PointInt32(cursor.X, cursor.Y), DisplayAreaFallback.Nearest).WorkArea;
        h = Math.Min(h, area.Height - 2 * margin);
        var x = Math.Clamp(cursor.X - w / 2, area.X + margin, area.X + area.Width - w - margin);
        var y = cursor.Y < area.Y + area.Height / 2 ? area.Y + margin : area.Y + area.Height - h - margin;

        AppWindow.MoveAndResize(new RectInt32(x, y, w, h));
        AppWindow.Show();
        Activate();
        SetForegroundWindow(_hwnd);
        IsVisible = true;
    }

    public void Hide()
    {
        if (!IsVisible) return;
        IsVisible = false;
        HiddenAtUtc = DateTime.UtcNow;
        AppWindow.Hide();
    }

    public void Render(Snapshot s)
    {
        var failed = s.Builds.Count(b => b.IsFailed && !b.IsRunning);
        var reviews = s.PullRequests.Count(p => p.NeedsMyReview);
        _status.Text = s.Error == null
            ? Strings.Format("Status_Summary", failed, reviews, s.PullRequestsToRevise)
            : Strings.Get("Tray_ConnectionErrorShort");

        _list.Children.Clear();

        var builds = s.Builds.OrderByDescending(b => b.Time ?? DateTimeOffset.MinValue).Take(MaxBuilds).ToList();
        AddHeader(Strings.Get("Section_Pipelines"), builds.Count == 0 ? null : Strings.Format("Section_Latest", builds.Count));
        foreach (var b in builds)
            AddRow(BuildGlyph(b), b.Definition, $"{b.Branch} · {b.RequestedFor}", Ago(b.Time), b.Url, $"{b.Project} #{b.BuildNumber}");

        var releases = s.Releases.OrderByDescending(r => r.IsFailed).ThenByDescending(r => r.IsRunning).Take(3).ToList();
        if (releases.Count > 0) AddHeader(Strings.Get("Section_Releases"), null);
        foreach (var r in releases)
            AddRow(r.IsRunning ? AppIcon.Running : r.IsFailed ? AppIcon.Failed : AppIcon.Ok, r.Definition, r.ReleaseName, "", r.Url, r.Project);

        var review = s.PullRequests.Where(p => p.NeedsMyReview).Take(5).ToList();
        if (review.Count > 0) AddHeader(Strings.Get("Section_Review"), $"{reviews}");
        foreach (var p in review)
            AddRow(AppIcon.Review, $"#{p.Id} {p.Title}", $"{p.Repository} · {p.Author}", "", p.Url, p.Project, p.UnresolvedCommentCount);

        var mine = s.PullRequests.Where(p => p.IsMine).Take(3).ToList();
        if (mine.Count > 0) AddHeader(Strings.Get("Section_MyPrs"), null);
        foreach (var p in mine)
            AddRow(AppIcon.PullRequest, $"#{p.Id} {p.Title}", p.Repository, "", p.Url, p.Project, p.UnresolvedCommentCount);

        if (_list.Children.Count == 0)
            _list.Children.Add(new TextBlock { Text = s.Error ?? Strings.Get("Popup_Nothing"), Margin = new Thickness(8), TextWrapping = TextWrapping.Wrap, Opacity = 0.7 });
    }

    private void AddHeader(string text, string? note)
    {
        var tb = new TextBlock { Text = note == null ? text : $"{text} ({note})", FontWeight = FontWeights.SemiBold, Opacity = 0.7, Margin = new Thickness(8, 10, 8, 2) };
        _list.Children.Add(tb);
    }

    private void AddRow(AppIcon icon, string title, string subtitle, string time, string url, string tooltip, int unresolvedCommentCount = 0)
    {
        var grid = new Grid { ColumnSpacing = 10, HorizontalAlignment = HorizontalAlignment.Stretch };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var glyph = new FontIcon
        {
            Glyph = Icons.GetGlyph(icon),
            FontSize = 20,
            Foreground = new SolidColorBrush(Icons.GetColor(icon)),
            VerticalAlignment = VerticalAlignment.Center
        };
        grid.Children.Add(glyph);

        var texts = new StackPanel
        {
            Children =
            {
                new TextBlock { Text = title, FontWeight = FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis },
                new TextBlock { Text = subtitle, FontSize = 12, Opacity = 0.7, TextTrimming = TextTrimming.CharacterEllipsis }
            }
        };
        Grid.SetColumn(texts, 1);
        grid.Children.Add(texts);

        if (unresolvedCommentCount > 0)
        {
            var comments = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 4,
                VerticalAlignment = VerticalAlignment.Center,
                Children =
                {
                    new FontIcon
                    {
                        Glyph = Icons.GetGlyph(AppIcon.Comments),
                        FontSize = 16,
                        Foreground = new SolidColorBrush(Icons.GetColor(AppIcon.Comments)),
                        VerticalAlignment = VerticalAlignment.Center
                    },
                    new TextBlock
                    {
                        Text = unresolvedCommentCount.ToString(),
                        FontSize = 12,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                }
            };
            ToolTipService.SetToolTip(comments, Strings.Format("Popup_UnresolvedCommentsCount", unresolvedCommentCount));
            Grid.SetColumn(comments, 2);
            grid.Children.Add(comments);
        }
        else
        {
            var timeText = new TextBlock { Text = time, FontSize = 12, Opacity = 0.7, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(timeText, 2);
            grid.Children.Add(timeText);
        }

        var btn = new Button
        {
            Content = grid,
            Style = (Style)Application.Current.Resources["SubtleButtonStyle"],
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            Padding = new Thickness(8, 8, 8, 8)
        };
        ToolTipService.SetToolTip(btn, tooltip);
        btn.Click += (_, _) => { Hide(); _openUrl(url); };
        _list.Children.Add(btn);
    }

    private static MenuFlyoutItem MenuItem(string text, AppIcon glyph, Action onClick)
    {
        var item = new MenuFlyoutItem { Text = text, Icon = new FontIcon { Glyph = Icons.GetGlyph(glyph) } };
        item.Click += (_, _) => onClick();
        return item;
    }

    private void PopulateConnectionMenus(MenuFlyout flyout)
    {
        while (flyout.Items.Count > 1)
            flyout.Items.RemoveAt(1);

        var connections = _getConnections();
        AddConnectionMenu(flyout, Strings.Get("Menu_OpenAzureDevOps"), AppIcon.AzureDevOps,
            connections.Where(connection => connection.Provider == IntegrationProvider.AzureDevOps),
            connection => string.IsNullOrWhiteSpace(connection.Project)
                ? connection.OrganizationUrl
                : $"{connection.OrganizationUrl}/{Uri.EscapeDataString(connection.Project)}");
        AddConnectionMenu(flyout, Strings.Get("Menu_OpenGitHub"), AppIcon.GitHub,
            connections.Where(connection => connection.Provider == IntegrationProvider.GitHub),
            connection => GitHubActionsClient.GetRepositoryUrl(connection.Repository));

        flyout.Items.Add(new MenuFlyoutSeparator());
        flyout.Items.Add(MenuItem(Strings.Get("Menu_Exit"), AppIcon.Exit, _exit));
    }

    private void AddConnectionMenu(MenuFlyout flyout, string title, AppIcon glyph,
        IEnumerable<IntegrationConnection> connections, Func<IntegrationConnection, string> getUrl)
    {
        var items = connections.ToList();
        if (items.Count == 0) return;

        var providerMenu = new MenuFlyoutSubItem
        {
            Text = title,
            Icon = new FontIcon { Glyph = Icons.GetGlyph(glyph) }
        };
        foreach (var connection in items)
        {
            var item = new MenuFlyoutItem { Text = connection.DisplayName };
            item.Click += (_, _) => { Hide(); _openUrl(getUrl(connection)); };
            providerMenu.Items.Add(item);
        }
        flyout.Items.Add(providerMenu);
    }

    private static AppIcon BuildGlyph(BuildInfo b) =>
        b.IsRunning ? AppIcon.Running : b.IsFailed ? AppIcon.Failed : b.IsSucceeded ? AppIcon.Ok : AppIcon.Other;

    private static string Ago(DateTimeOffset? t)
    {
        if (t == null) return "";
        var age = DateTimeOffset.Now - t.Value;
        if (age < TimeSpan.FromMinutes(1)) return Strings.Get("Ago_Now");
        if (age < TimeSpan.FromHours(1)) return Strings.Format("Ago_Min", (int)age.TotalMinutes);
        if (age < TimeSpan.FromDays(1)) return Strings.Format("Ago_Hour", (int)age.TotalHours);
        return Strings.Format("Ago_Day", (int)age.TotalDays);
    }

    /// <summary>Wysuwa okno na wierzch (okna aplikacji z zasobnika bywają otwierane „pod spodem”).</summary>
    public static void BringToFront(Window window)
    {
        var hwnd = WindowNative.GetWindowHandle(window);
        window.Activate();
        SetForegroundWindow(hwnd);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X; public int Y; }

    [DllImport("user32.dll")] private static extern bool GetCursorPos(out POINT p);
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] private static extern uint GetDpiForWindow(IntPtr hWnd);
}
