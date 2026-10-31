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
    private readonly TrayPopupWindow _popup;
    private readonly DispatcherQueueTimer _timer;
    private AppSettings _settings = AppSettings.Load();
    private readonly List<(IntegrationConnection Connection, IIntegrationClient Client)> _clients = [];
    private CancellationTokenSource? _cts;
    private Snapshot _snapshot = Snapshot.Empty();
    private Snapshot _all = Snapshot.Empty();
    private Dictionary<string, BuildInfo>? _previousBuilds;
    private HashSet<int>? _previousReviewPrs;
    private string? _notificationUrl;
    private bool _refreshing;
    private SettingsWindow? _settingsWindow;
    private ChoosePipelinesWindow? _choose;

    public TrayController()
    {
        Strings.SetLanguage(_settings.Language);
        _tray = new TaskbarIcon
        {
            Icon = MakeIcon(Health.Unknown),
            ToolTipText = "CicdLight"
        };
        _tray.ForceCreate();
        _popup = new TrayPopupWindow(() => _ = RefreshAsync(), ShowSettings, ChoosePipelines,
            OpenDefaultUrl, Exit, OpenUrl);
        _tray.TrayIcon.MessageWindow.MouseEventReceived += (_, e) =>
        {
            switch (e.MouseEvent)
            {
                case MouseEvent.BalloonToolTipClicked:
                    OpenUrl(_notificationUrl);
                    break;
                case MouseEvent.IconLeftMouseUp:
                case MouseEvent.IconRightMouseUp:
                    TogglePopup();
                    break;
            }
        };

        _timer = DispatcherQueue.GetForCurrentThread().CreateTimer();
        _timer.IsRepeating = true;
        _timer.Tick += async (_, _) => await RefreshAsync();

        ApplySettings();

        if (!_settings.IsConfigured) ShowSettings();
    }

    private void ApplySettings()
    {
        Strings.SetLanguage(_settings.Language);
        foreach (var client in _clients) client.Client.Dispose();
        _clients.Clear();
        foreach (var connection in _settings.Connections)
        {
            IIntegrationClient client = connection.Provider switch
            {
                IntegrationProvider.AzureDevOps => new AzureDevOpsClient(_settings, connection),
                IntegrationProvider.GitHub => new GitHubActionsClient(connection),
                _ => throw new InvalidOperationException($"Unsupported provider: {connection.Provider}")
            };
            _clients.Add((connection, client));
        }
        _previousBuilds = null;
        _previousReviewPrs = null;
        _timer.Interval = TimeSpan.FromSeconds(Math.Max(15, _settings.PollSeconds));
        _timer.Start();
        _ = RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        if (_refreshing) return;
        if (_clients.Count == 0)
        {
            _snapshot = Snapshot.Empty(Strings.Get("Tray_NotConfigured"));
            Update();
            return;
        }

        _refreshing = true;
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        try
        {
            var requests = _clients.Select(async entry =>
            {
                var snapshot = await entry.Client.FetchAsync(_cts.Token);
                entry.Connection.LastError = snapshot.Error;
                return (entry.Connection, Snapshot: snapshot);
            });
            var results = await Task.WhenAll(requests);
            var errors = results.Count(result => result.Snapshot.Error != null);
            _all = new Snapshot(
                results.SelectMany(result => result.Snapshot.Builds).ToList(),
                results.SelectMany(result => result.Snapshot.PullRequests).ToList(),
                null,
                results.SelectMany(result => result.Snapshot.Releases).ToList());
            _snapshot = new Snapshot(
                results.SelectMany(result => result.Connection.HasSelection
                    ? result.Snapshot.Builds.Where(build => result.Connection.SelectedItems.Contains(build.Key))
                    : result.Snapshot.Builds).ToList(),
                results.SelectMany(result => result.Snapshot.PullRequests).ToList(),
                errors == 0 ? null : Strings.Format("Tray_ConnectionErrors", errors),
                results.SelectMany(result => result.Connection.HasSelection
                    ? result.Snapshot.Releases.Where(release => result.Connection.SelectedItems.Contains(release.Key))
                    : result.Snapshot.Releases).ToList());
            _settingsWindow?.RefreshConnectionStatuses();
            Notify(_snapshot);
            Update();
        }
        catch (OperationCanceledException) { }
        finally { _refreshing = false; }
    }

    private void Notify(Snapshot s)
    {
        var builds = s.Builds.ToDictionary(b => b.Key);
        var reviewPrs = s.PullRequests.Where(p => p.NeedsMyReview).Select(p => p.Id).ToHashSet();

        // First successful poll only establishes the baseline.
        if (_previousBuilds != null && _previousReviewPrs != null && _settings.Notifications)
        {
            foreach (var b in builds.Values.Where(b => !b.IsRunning && b.Result != null))
            {
                _previousBuilds.TryGetValue(b.Key, out var old);
                if (old != null && old.Id == b.Id && old.IsRunning == b.IsRunning) continue;
                var notify = GetProjectNotifications(b.Project);

                if (b.IsFailed && notify.BuildFailed)
                    Toast(Strings.Format("Toast_BuildFailed", b.Definition), Strings.Format("Toast_BuildFailedText", b.Branch, b.BuildNumber, b.RequestedFor), b.Url, NotificationIcon.Error);
                else if (b.IsSucceeded && old != null && old.IsFailed && notify.BuildFixed)
                    Toast(Strings.Format("Toast_BuildFixed", b.Definition), Strings.Format("Toast_BuildFixedText", b.Branch, b.BuildNumber), b.Url, NotificationIcon.Info);
            }

            foreach (var p in s.PullRequests.Where(p => p.NeedsMyReview && !_previousReviewPrs.Contains(p.Id) && GetProjectNotifications(p.Project).ReviewRequested))
                Toast(Strings.Format("Toast_Review", p.Repository), Strings.Format("Toast_ReviewText", p.Id, p.Title, p.Author), p.Url, NotificationIcon.Info);
        }

        _previousBuilds = builds;
        _previousReviewPrs = reviewPrs;
    }

    private void Toast(string title, string text, string url, NotificationIcon icon)
    {
        _notificationUrl = url;
        _tray.ShowNotification(title, text, icon);
    }

    private ProjectNotificationSettings GetProjectNotifications(string project)
    {
        var existing = _settings.ProjectNotifications.FirstOrDefault(entry =>
            string.Equals(entry.Key, project, StringComparison.OrdinalIgnoreCase)).Value;
        return existing ?? new ProjectNotificationSettings
        {
            BuildFailed = _settings.NotifyBuildFailed,
            BuildFixed = _settings.NotifyBuildFixed,
            ReviewRequested = _settings.NotifyReviewRequested
        };
    }

    private void Update()
    {
        var old = _tray.Icon;
        _tray.Icon = MakeIcon(_snapshot.Health);
        DestroyIconHandle(old);

        var failed = _snapshot.Builds.Count(b => b.IsFailed && !b.IsRunning);
        var reviews = _snapshot.PullRequests.Count(p => p.NeedsMyReview);
        var text = _snapshot.Error != null
            ? Strings.Format("Tooltip_Error", _snapshot.Error)
            : Strings.Format("Tooltip_Summary", failed, reviews, _snapshot.PullRequestsToRevise);
        _tray.ToolTipText = text.Length > 127 ? text[..127] : text;
        if (_popup.IsVisible) _popup.Render(_snapshot);
    }

    private void TogglePopup()
    {
        // Kliknięcie w ikonę dezaktywuje popup (Hide), więc ignorujemy ponowne otwarcie tuż po zamknięciu.
        if (_popup.IsVisible) { _popup.Hide(); return; }
        if ((DateTime.UtcNow - _popup.HiddenAtUtc).TotalMilliseconds < 300) return;
        _popup.Show(_snapshot);
    }

    private void ChoosePipelines()
    {
        if (_settings.Connections.Count == 1)
        {
            ChoosePipelines(_settings.Connections[0]);
            return;
        }
        ShowSettings();
    }

    private void ChoosePipelines(IntegrationConnection connection)
    {
        var items = _all.Builds.Where(build => build.ConnectionId == connection.Id)
            .Select(b => (Key: b.Key, Project: b.Project, Text: b.Definition))
            .Concat(_all.Releases.Where(release => release.ConnectionId == connection.Id)
                .Select(r => (Key: r.Key, Project: r.Project, Text: r.Definition)))
            .OrderBy(i => i.Project).ThenBy(i => i.Text).ToList();
        var projects = items.Select(i => i.Project)
            .Concat(_all.PullRequests.Where(p => p.ConnectionId == connection.Id).Select(p => p.Project))
            .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(p => p, StringComparer.CurrentCultureIgnoreCase).ToList();
        if (items.Count == 0)
        {
            _tray.ShowNotification("CicdLight", Strings.Get("Toast_NoPipelines"), NotificationIcon.Info);
            return;
        }

        if (_choose != null) { TrayPopupWindow.BringToFront(_choose); return; }
        _choose = new ChoosePipelinesWindow(items, projects, connection.HasSelection ? connection.SelectedItems : [], _settings.ProjectNotifications,
            new ProjectNotificationSettings
            {
                BuildFailed = _settings.NotifyBuildFailed,
                BuildFixed = _settings.NotifyBuildFixed,
                ReviewRequested = _settings.NotifyReviewRequested
            }, (chosen, projectNotifications) =>
        {
            var current = _settings.Connections.FirstOrDefault(item => item.Id == connection.Id);
            if (current != null)
            {
                current.SelectedItems = chosen;
                current.HasSelection = true;
            }
            _settings.ProjectNotifications = projectNotifications;
            _settings.Save();
            _ = RefreshAsync();
        });
        _choose.Closed += (_, _) => _choose = null;
        TrayPopupWindow.BringToFront(_choose);
    }

    private void ShowSettings()
    {
        if (_settingsWindow != null) { TrayPopupWindow.BringToFront(_settingsWindow); return; }
        _settingsWindow = new SettingsWindow(_settings, () =>
        {
            _settings = AppSettings.Load();
            ApplySettings();
            _settingsWindow?.UpdateSettings(_settings);
        }, ChoosePipelines);
        _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        TrayPopupWindow.BringToFront(_settingsWindow);
    }

    private static void OpenUrl(string? url)
    {
        if (!string.IsNullOrEmpty(url))
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }

    private void OpenDefaultUrl()
    {
        var azure = _settings.Connections.FirstOrDefault(connection => connection.Provider == IntegrationProvider.AzureDevOps);
        if (azure != null)
        {
            OpenUrl(azure.OrganizationUrl);
            return;
        }

        var github = _settings.Connections.FirstOrDefault(connection => connection.Provider == IntegrationProvider.GitHub);
        if (github != null) OpenUrl(GitHubActionsClient.GetRepositoryUrl(github.Repository));
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
            using var pen = new Pen(Color.White, 2.4f) { LineJoin = LineJoin.Round, StartCap = LineCap.Round, EndCap = LineCap.Round };
            using var path = new GraphicsPath();
            const float x = 2, w = 28, d = 12;
            path.AddArc(x, x, d, d, 180, 90);
            path.AddArc(x + w - d, x, d, d, 270, 90);
            path.AddArc(x + w - d, x + w - d, d, d, 0, 90);
            path.AddArc(x, x + w - d, d, d, 90, 90);
            path.CloseFigure();
            g.FillPath(brush, path);
            g.DrawLines(pen, [new PointF(6f, 17f), new PointF(11.5f, 17f), new PointF(14.5f, 9.5f), new PointF(18.5f, 22.5f), new PointF(21f, 16f), new PointF(26f, 16f)]);
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
        foreach (var client in _clients) client.Client.Dispose();
        _clients.Clear();
        _tray.Dispose();
    }
}
