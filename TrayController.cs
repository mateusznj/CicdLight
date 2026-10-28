using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using H.NotifyIcon;
using H.NotifyIcon.Core;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Controls;

namespace CicdLight;

public sealed class TrayController : IDisposable
{
    private readonly TaskbarIcon _tray;
    private readonly MenuFlyout _menu = new();
    private readonly DispatcherQueueTimer _timer;
    private AppSettings _settings = AppSettings.Load();
    private AzureDevOpsClient? _client;
    private CancellationTokenSource? _cts;
    private Snapshot _snapshot = Snapshot.Empty();
    private Snapshot _all = Snapshot.Empty();
    private Dictionary<string, BuildInfo>? _previousBuilds;
    private HashSet<int>? _previousReviewPrs;
    private string? _notificationUrl;
    private bool _refreshing;

    public TrayController()
    {
        _tray = new TaskbarIcon
        {
            Icon = MakeIcon(Health.Unknown),
            ToolTipText = "CicdLight",
            ContextFlyout = _menu,
            ContextMenuMode = ContextMenuMode.PopupMenu,
            MenuActivation = PopupActivationMode.LeftOrRightClick,
            NoLeftClickDelay = true
        };
        _tray.ForceCreate();
        _tray.TrayIcon.MessageWindow.BalloonToolTipClicked += (_, _) => OpenUrl(_notificationUrl);

        _timer = DispatcherQueue.GetForCurrentThread().CreateTimer();
        _timer.IsRepeating = true;
        _timer.Tick += async (_, _) => await RefreshAsync();

        ApplySettings();
        RebuildMenu();

        if (!_settings.IsConfigured) ShowSettings();
    }

    private void ApplySettings()
    {
        _client?.Dispose();
        _client = _settings.IsConfigured ? new AzureDevOpsClient(_settings) : null;
        _previousBuilds = null;
        _previousReviewPrs = null;
        _timer.Interval = TimeSpan.FromSeconds(Math.Max(15, _settings.PollSeconds));
        _timer.Start();
        _ = RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        if (_refreshing) return;
        if (_client == null)
        {
            _snapshot = Snapshot.Empty("Not configured");
            Update();
            return;
        }

        _refreshing = true;
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        try
        {
            _all = await _client.FetchAsync(_cts.Token);
            _snapshot = _all.Filter(_settings.WatchedItems);
            Notify(_snapshot);
            Update();
        }
        catch (OperationCanceledException) { }
        finally { _refreshing = false; }
    }

    private void Notify(Snapshot s)
    {
        if (s.Error != null) return;

        var builds = s.Builds.ToDictionary(b => b.Key);
        var reviewPrs = s.PullRequests.Where(p => p.NeedsMyReview).Select(p => p.Id).ToHashSet();

        // First successful poll only establishes the baseline.
        if (_previousBuilds != null && _previousReviewPrs != null && _settings.Notifications)
        {
            foreach (var b in builds.Values.Where(b => !b.IsRunning && b.Result != null))
            {
                _previousBuilds.TryGetValue(b.Key, out var old);
                if (old != null && old.Id == b.Id && old.IsRunning == b.IsRunning) continue;

                if (b.IsFailed)
                    Toast($"Build failed: {b.Definition}", $"{b.Branch} #{b.BuildNumber} ({b.RequestedFor})", b.Url, NotificationIcon.Error);
                else if (b.IsSucceeded && old != null && old.IsFailed)
                    Toast($"Build fixed: {b.Definition}", $"{b.Branch} #{b.BuildNumber}", b.Url, NotificationIcon.Info);
            }

            foreach (var p in s.PullRequests.Where(p => p.NeedsMyReview && !_previousReviewPrs.Contains(p.Id)))
                Toast($"Review requested: {p.Repository}", $"#{p.Id} {p.Title} - {p.Author}", p.Url, NotificationIcon.Info);
        }

        _previousBuilds = builds;
        _previousReviewPrs = reviewPrs;
    }

    private void Toast(string title, string text, string url, NotificationIcon icon)
    {
        _notificationUrl = url;
        _tray.ShowNotification(title, text, icon);
    }

    private void Update()
    {
        var old = _tray.Icon;
        _tray.Icon = MakeIcon(_snapshot.Health);
        DestroyIconHandle(old);

        var failed = _snapshot.Builds.Count(b => b.IsFailed && !b.IsRunning);
        var reviews = _snapshot.PullRequests.Count(p => p.NeedsMyReview);
        var text = _snapshot.Error != null
            ? $"CicdLight: {_snapshot.Error}"
            : $"CicdLight: {failed} failed, {reviews} to review";
        _tray.ToolTipText = text.Length > 127 ? text[..127] : text;
        RebuildMenu();
    }

    private void RebuildMenu()
    {
        _menu.Items.Clear();

        if (_snapshot.Error != null)
            _menu.Items.Add(new MenuFlyoutItem { Text = _snapshot.Error, IsEnabled = false });

        AddSection("Pipelines", _snapshot.Builds
            .OrderByDescending(b => b.IsFailed).ThenByDescending(b => b.IsRunning).ThenBy(b => b.Project).ThenBy(b => b.Definition)
            .Select(b => (BuildText(b), b.Url)));

        AddSection("Releases", _snapshot.Releases
            .OrderByDescending(r => r.IsFailed).ThenByDescending(r => r.IsRunning).ThenBy(r => r.Project).ThenBy(r => r.Definition)
            .Select(r => (ReleaseText(r), r.Url)));

        AddSection("Review requested", _snapshot.PullRequests.Where(p => p.NeedsMyReview)
            .Select(p => ($"{p.Repository}  #{p.Id}  {Trim(p.Title)}  ({p.Author})", p.Url)));

        AddSection("My pull requests", _snapshot.PullRequests.Where(p => p.IsMine)
            .Select(p => ($"{p.Repository}  #{p.Id}  {Trim(p.Title)}", p.Url)));

        if (_menu.Items.Count > 0) _menu.Items.Add(new MenuFlyoutSeparator());
        AddItem("Refresh now", async () => await RefreshAsync());
        AddItem("Open Azure DevOps", () => OpenUrl(_settings.IsConfigured ? _settings.OrganizationUrl : null));
        AddItem("Choose pipelines...", ChoosePipelines);
        AddItem("Settings...", ShowSettings);
        _menu.Items.Add(new MenuFlyoutSeparator());
        AddItem("Exit", Exit);
    }

    private void AddItem(string text, Action onClick)
    {
        var item = new MenuFlyoutItem { Text = text };
        item.Click += (_, _) => onClick();
        _menu.Items.Add(item);
    }

    private void AddSection(string title, IEnumerable<(string Text, string Url)> items)
    {
        var list = items.ToList();
        if (list.Count == 0) return;
        if (_menu.Items.Count > 0) _menu.Items.Add(new MenuFlyoutSeparator());
        _menu.Items.Add(new MenuFlyoutItem
        {
            Text = $"{title} ({list.Count})",
            IsEnabled = false,
            FontWeight = FontWeights.Bold
        });
        foreach (var (text, url) in list)
            AddItem(text, () => OpenUrl(url));
    }

    private static string BuildText(BuildInfo b)
    {
        var mark = b.IsRunning ? "▶" : b.IsFailed ? "✖" : b.IsSucceeded ? "✔" : "•";
        return $"{mark}  {b.Project} / {b.Definition}";
    }

    private static string ReleaseText(ReleaseInfo r)
    {
        var mark = r.IsRunning ? "▶" : r.IsFailed ? "✖" : "✔";
        return $"{mark}  {r.Project} / {r.Definition}";
    }

    private static string Trim(string s) => s.Length > 60 ? s[..57] + "..." : s;

    private void ChoosePipelines()
    {
        // Implemented in task 03 (ChoosePipelinesWindow).
    }

    private void ShowSettings()
    {
        // Implemented in task 03 (SettingsWindow).
    }

    private static void OpenUrl(string? url)
    {
        if (!string.IsNullOrEmpty(url))
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }

    private static Icon MakeIcon(Health health)
    {
        var color = health switch
        {
            Health.Ok => Color.FromArgb(46, 184, 92),
            Health.Running => Color.FromArgb(52, 152, 219),
            Health.Warning => Color.FromArgb(241, 196, 15),
            Health.Failed => Color.FromArgb(231, 76, 60),
            _ => Color.Gray
        };
        using var bmp = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var brush = new SolidBrush(color);
            using var pen = new Pen(Color.FromArgb(200, 255, 255, 255), 2);
            g.FillEllipse(brush, 3, 3, 26, 26);
            g.DrawEllipse(pen, 3, 3, 26, 26);
        }
        return Icon.FromHandle(bmp.GetHicon());
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    private static void DestroyIconHandle(Icon? icon)
    {
        if (icon != null) DestroyIcon(icon.Handle);
    }

    private void Exit()
    {
        Dispose();
        Microsoft.UI.Xaml.Application.Current.Exit();
    }

    public void Dispose()
    {
        _timer.Stop();
        _cts?.Cancel();
        _client?.Dispose();
        _tray.Dispose();
    }
}
