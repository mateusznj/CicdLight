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
    private readonly Action<string?> _openUrl;
    private readonly IntPtr _hwnd;

    public bool IsVisible { get; private set; }
    public DateTime HiddenAtUtc { get; private set; } = DateTime.MinValue;

    public TrayPopupWindow(Action refresh, Action settings, Action choose, Action openOrg, Action exit, Action<string?> openUrl)
    {
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

        var refreshBtn = new Button { Content = new FontIcon { Glyph = "\uE72C", FontSize = 16 }, Style = subtle };
        ToolTipService.SetToolTip(refreshBtn, "Refresh now");
        refreshBtn.Click += (_, _) => refresh();

        var gear = new Button { Content = new FontIcon { Glyph = "\uE713", FontSize = 16 }, Style = subtle };
        var flyout = new MenuFlyout();
        flyout.Items.Add(MenuItem("Settings...", "\uE713", () => { Hide(); settings(); }));
        flyout.Items.Add(MenuItem("Choose pipelines...", "\uE762", () => { Hide(); choose(); }));
        flyout.Items.Add(MenuItem("Open Azure DevOps", "\uE774", () => { Hide(); openOrg(); }));
        flyout.Items.Add(new MenuFlyoutSeparator());
        flyout.Items.Add(MenuItem("Exit", "\uE711", exit));
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
        _status.Text = s.Error ?? $"{failed} failed, {reviews} to review";

        _list.Children.Clear();

        var builds = s.Builds.OrderByDescending(b => b.Time ?? DateTimeOffset.MinValue).Take(MaxBuilds).ToList();
        AddHeader("Pipelines", builds.Count == 0 ? null : $"latest {builds.Count}");
        foreach (var b in builds)
            AddRow(BuildGlyph(b), b.Definition, $"{b.Branch} · {b.RequestedFor}", Ago(b.Time), b.Url, $"{b.Project} #{b.BuildNumber}");

        var releases = s.Releases.OrderByDescending(r => r.IsFailed).ThenByDescending(r => r.IsRunning).Take(3).ToList();
        if (releases.Count > 0) AddHeader("Releases", null);
        foreach (var r in releases)
            AddRow(r.IsRunning ? Glyph.Running : r.IsFailed ? Glyph.Failed : Glyph.Ok, r.Definition, r.ReleaseName, "", r.Url, r.Project);

        var review = s.PullRequests.Where(p => p.NeedsMyReview).Take(5).ToList();
        if (review.Count > 0) AddHeader("Review requested", $"{reviews}");
        foreach (var p in review)
            AddRow(Glyph.Review, $"#{p.Id} {p.Title}", $"{p.Repository} · {p.Author}", "", p.Url, p.Project);

        var mine = s.PullRequests.Where(p => p.IsMine).Take(3).ToList();
        if (mine.Count > 0) AddHeader("My pull requests", null);
        foreach (var p in mine)
            AddRow(Glyph.Pr, $"#{p.Id} {p.Title}", p.Repository, "", p.Url, p.Project);

        if (_list.Children.Count == 0)
            _list.Children.Add(new TextBlock { Text = s.Error ?? "Nothing to show yet.", Margin = new Thickness(8), TextWrapping = TextWrapping.Wrap, Opacity = 0.7 });
    }

    private void AddHeader(string text, string? note)
    {
        var tb = new TextBlock { Text = note == null ? text : $"{text} ({note})", FontWeight = FontWeights.SemiBold, Opacity = 0.7, Margin = new Thickness(8, 10, 8, 2) };
        _list.Children.Add(tb);
    }

    private void AddRow((string Glyph, Windows.UI.Color Color) icon, string title, string subtitle, string time, string url, string tooltip)
    {
        var grid = new Grid { ColumnSpacing = 10, HorizontalAlignment = HorizontalAlignment.Stretch };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var glyph = new FontIcon { Glyph = icon.Glyph, FontSize = 20, Foreground = new SolidColorBrush(icon.Color), VerticalAlignment = VerticalAlignment.Center };
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

        var t = new TextBlock { Text = time, FontSize = 12, Opacity = 0.7, VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(t, 2);
        grid.Children.Add(t);

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

    private static MenuFlyoutItem MenuItem(string text, string glyph, Action onClick)
    {
        var item = new MenuFlyoutItem { Text = text, Icon = new FontIcon { Glyph = glyph } };
        item.Click += (_, _) => onClick();
        return item;
    }

    private static class Glyph
    {
        public static readonly (string, Windows.UI.Color) Ok = ("\uEC61", Windows.UI.Color.FromArgb(255, 46, 184, 92));
        public static readonly (string, Windows.UI.Color) Failed = ("\uEB90", Windows.UI.Color.FromArgb(255, 231, 76, 60));
        public static readonly (string, Windows.UI.Color) Running = ("\uE768", Windows.UI.Color.FromArgb(255, 52, 152, 219));
        public static readonly (string, Windows.UI.Color) Other = ("\uE91F", Windows.UI.Color.FromArgb(255, 150, 150, 150));
        public static readonly (string, Windows.UI.Color) Review = ("\uE8F4", Windows.UI.Color.FromArgb(255, 241, 196, 15));
        public static readonly (string, Windows.UI.Color) Pr = ("\uE8F4", Windows.UI.Color.FromArgb(255, 150, 150, 150));
    }

    private static (string, Windows.UI.Color) BuildGlyph(BuildInfo b) =>
        b.IsRunning ? Glyph.Running : b.IsFailed ? Glyph.Failed : b.IsSucceeded ? Glyph.Ok : Glyph.Other;

    private static string Ago(DateTimeOffset? t)
    {
        if (t == null) return "";
        var d = DateTimeOffset.Now - t.Value;
        if (d.TotalSeconds < 60) return "just now";
        if (d.TotalMinutes < 60) return $"{(int)d.TotalMinutes} min ago";
        if (d.TotalHours < 24) return $"{(int)d.TotalHours} h ago";
        return $"{(int)d.TotalDays} d ago";
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
