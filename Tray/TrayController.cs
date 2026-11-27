using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Text.Json;
using H.NotifyIcon;
using H.NotifyIcon.Core;
using Microsoft.UI.Dispatching;

namespace CicdLight;

public sealed class TrayController : IDisposable
{
    private static readonly TimeSpan TestNotificationInterval = TimeSpan.FromSeconds(6);
    private bool _testNotificationsRunning;
    private readonly TaskbarIcon _tray;
    private readonly TrayPopupWindow _popup;
    private readonly DispatcherQueueTimer _timer;
    private readonly ToastIconProvider _toastIcons = new();
    private AppSettings _settings = AppSettings.Load();
    private readonly List<(IntegrationConnection Connection, IIntegrationClient Client)> _clients = [];
    private CancellationTokenSource? _cts;
    private Snapshot _snapshot = Snapshot.Empty();
    private Snapshot _all = Snapshot.Empty();
    private readonly Dictionary<string, IReadOnlyList<string>> _connectionProjects = [];
    private readonly Dictionary<string, IReadOnlyList<RepositoryInfo>> _connectionRepositories = [];
    private Dictionary<string, BuildInfo>? _previousBuilds;
    private HashSet<string>? _previousReviewPrs;
    private Dictionary<string, int>? _previousUnresolvedComments;
    private string? _notificationUrl;
    private bool _refreshing;
    private bool _refreshPending;
    private bool _updateCheckInProgress;
    private SettingsWindow? _settingsWindow;

    public TrayController()
    {
        Strings.SetLanguage(_settings.Language);
        _tray = new TaskbarIcon
        {
            Icon = MakeIcon(Health.Unknown),
            ToolTipText = "CicdLight"
        };
        _tray.ForceCreate();
        _popup = new TrayPopupWindow(() => _ = RefreshAsync(force: true), ShowSettings,
            () => _settings.Connections, Exit, OpenUrl);
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

        if (DemoIntegrationClient.IsEnabled)
        {
            _ = ShowDemoPopupAsync();
        }
        else if (!_settings.IsConfigured)
        {
            ShowSettings();
        }
    }

    private async Task ShowDemoPopupAsync()
    {
        await Task.Delay(TimeSpan.FromSeconds(3));
        _popup.Show(_snapshot);
    }

    private void ApplySettings()
    {
        Strings.SetLanguage(_settings.Language);
        if (_refreshing)
        {
            _refreshPending = true;
            _cts?.Cancel();
        }
        foreach (var client in _clients)
        {
            client.Client.Dispose();
        }
        _clients.Clear();
        if (DemoIntegrationClient.IsEnabled)
        {
            _clients.Add((new IntegrationConnection { Organization = "demo" }, new DemoIntegrationClient()));
        }
        var enabledConnections = DemoIntegrationClient.IsEnabled
            ? Enumerable.Empty<IntegrationConnection>()
            : _settings.Connections.Where(connection => connection.Enabled);
        foreach (var connection in enabledConnections)
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
        _previousUnresolvedComments = null;
        _timer.Interval = TimeSpan.FromSeconds(Math.Max(15, _settings.PollSeconds));
        _timer.Start();
        _ = RefreshAsync();
        _ = CheckForUpdatesDailyAsync();
    }

    private async Task CheckForUpdatesDailyAsync()
    {
        if (_updateCheckInProgress)
        {
            return;
        }

        var lastCheck = _settings.LastUpdateCheckUtc;
        if (lastCheck.HasValue && DateTimeOffset.UtcNow - lastCheck.Value < TimeSpan.FromHours(24))
        {
            return;
        }

        _updateCheckInProgress = true;
        _settings.LastUpdateCheckUtc = DateTimeOffset.UtcNow;
        _settings.Save();
        try
        {
            var result = await UpdateChecker.CheckAsync(_settings, CancellationToken.None);
            if (result.UpdateAvailable &&
                _settings.Notifications &&
                !string.Equals(_settings.LastNotifiedUpdateVersion, result.LatestVersion, StringComparison.OrdinalIgnoreCase))
            {
                _notificationUrl = result.ReleaseUrl;
                _settings.LastNotifiedUpdateVersion = result.LatestVersion;
                _settings.Save();
                Toast(
                    Strings.Get("Update_AvailableTitle"),
                    Strings.Format("Update_AvailableText", result.CurrentVersion, result.LatestVersion),
                    result.ReleaseUrl,
                    ToastIcon.Info);
            }
        }
        catch (HttpRequestException)
        {
        }
        catch (TaskCanceledException)
        {
        }
        catch (InvalidOperationException)
        {
        }
        catch (JsonException)
        {
        }
        finally
        {
            _updateCheckInProgress = false;
        }
    }

    private async Task RefreshAsync(bool force = false)
    {
        _ = CheckForUpdatesDailyAsync();
        if (_refreshing)
        {
            if (force)
            {
                _refreshPending = true;
                _cts?.Cancel();
            }
            return;
        }
        if (_clients.Count == 0)
        {
            var message = _settings.IsConfigured
                ? Strings.Get("Tray_NoActiveConnections")
                : Strings.Get("Tray_NotConfigured");
            _all = Snapshot.Empty();
            _snapshot = Snapshot.Empty(message);
            Update();
            return;
        }

        _refreshing = true;
        var refreshCts = new CancellationTokenSource();
        _cts = refreshCts;
        try
        {
            var requests = _clients.Select(async entry =>
            {
                var snapshot = await entry.Client.FetchAsync(refreshCts.Token);
                entry.Connection.LastError = snapshot.Error;
                return (entry.Connection, Snapshot: snapshot);
            });
            var results = await Task.WhenAll(requests);
            foreach (var result in results)
            {
                _connectionProjects[result.Connection.Id] = result.Snapshot.ProjectList ?? [];
                _connectionRepositories[result.Connection.Id] = result.Snapshot.RepositoryList ?? [];
            }
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
                results.SelectMany(result => result.Snapshot.PullRequests)
                    .Where(pullRequest => GetProjectNotifications(pullRequest.Project).ShowPullRequests).ToList(),
                errors == 0 ? null : Strings.Format("Tray_ConnectionErrors", errors),
                results.SelectMany(result => result.Connection.HasSelection
                    ? result.Snapshot.Releases.Where(release => result.Connection.SelectedItems.Contains(release.Key))
                    : result.Snapshot.Releases).ToList(),
                null,
                _settings.TrayAlertMyPrComments,
                _settings.TrayAlertOtherPrComments,
                _settings.ShowBuildEta);
            _settingsWindow?.RefreshConnectionStatuses();
            Notify(_snapshot);
            Update();
        }
        catch (OperationCanceledException) { }
        finally
        {
            _refreshing = false;
            if (ReferenceEquals(_cts, refreshCts))
            {
                _cts = null;
            }
            refreshCts.Dispose();
            UpdateRefreshInterval();
            if (_refreshPending)
            {
                _refreshPending = false;
                _ = RefreshAsync();
            }
        }
    }

    private void UpdateRefreshInterval()
    {
        var interval = TimeSpan.FromSeconds(Math.Max(15, _settings.PollSeconds));
        var rateLimitRetry = _clients
            .Select(entry => entry.Client)
            .OfType<GitHubActionsClient>()
            .Select(client => client.NextRetryAt)
            .Where(retryAt => retryAt.HasValue)
            .Select(retryAt => retryAt!.Value)
            .DefaultIfEmpty(DateTimeOffset.MaxValue)
            .Min();

        if (rateLimitRetry != DateTimeOffset.MaxValue)
        {
            var retryDelay = rateLimitRetry - DateTimeOffset.UtcNow;
            if (retryDelay > TimeSpan.Zero && retryDelay < interval)
            {
                interval = retryDelay;
            }
        }

        _timer.Interval = interval;
    }

    private void Notify(Snapshot s)
    {
        var builds = s.Builds.ToDictionary(b => b.Key);
        var reviewPrs = s.PullRequests.Where(p => p.NeedsMyReview).Select(p => p.Key).ToHashSet();
        var unresolvedComments = s.PullRequests.Where(p => p.HasUnresolvedComments)
            .ToDictionary(p => p.Key, p => p.UnresolvedCommentCount);

        // First successful poll only establishes the baseline.
        if (_previousBuilds != null && _previousReviewPrs != null && _previousUnresolvedComments != null && _settings.Notifications)
        {
            foreach (var b in builds.Values.Where(b => !b.IsRunning && b.Result != null))
            {
                _previousBuilds.TryGetValue(b.Key, out var old);
                if (old != null && old.Id == b.Id && old.IsRunning == b.IsRunning)
                {
                    continue;
                }
                var notify = GetProjectNotifications(b.Project);

                if (b.IsFailed && notify.BuildFailed)
                {
                    Toast(Strings.Format("Toast_BuildFailed", b.Definition), Strings.Format("Toast_BuildFailedText", b.Branch, b.BuildNumber, b.RequestedFor), b.Url, ToastIcon.BuildFailed);
                }
                else if (b.IsSucceeded && old != null && old.IsFailed && notify.BuildFixed)
                {
                    Toast(Strings.Format("Toast_BuildFixed", b.Definition), Strings.Format("Toast_BuildFixedText", b.Branch, b.BuildNumber), b.Url, ToastIcon.BuildFixed);
                }
            }

            foreach (var p in s.PullRequests.Where(p => p.NeedsMyReview && !_previousReviewPrs.Contains(p.Key) && GetProjectNotifications(p.Project).ReviewRequested))
            {
                Toast(Strings.Format("Toast_Review", p.Repository), Strings.Format("Toast_ReviewText", p.Id, p.Title, p.Author), p.Url, ToastIcon.ReviewRequested);
            }

            foreach (var p in s.PullRequests.Where(p =>
                         p.UnresolvedCommentCount > _previousUnresolvedComments.GetValueOrDefault(p.Key) &&
                         (p.IsMine ? s.AlertMyPrComments : s.AlertOtherPrComments) &&
                         GetProjectNotifications(p.Project).UnresolvedComments))
                Toast(Strings.Get("Toast_UnresolvedComment"),
                    Strings.Format("Toast_UnresolvedCommentText", p.Id, p.Title, p.UnresolvedCommentCount),
                    p.Url, ToastIcon.UnresolvedComments);
        }

        _previousBuilds = builds;
        _previousReviewPrs = reviewPrs;
        _previousUnresolvedComments = unresolvedComments;
    }

    private void Toast(string title, string text, string url, ToastIcon icon)
    {
        _notificationUrl = url;
        var customIcon = _toastIcons.Get(icon);
        try
        {
            _tray.ShowNotification(title, text, NotificationIcon.Info, customIcon, largeIcon: customIcon != null);
        }
        catch (InvalidOperationException) when (customIcon != null)
        {
            // The shell rejected the custom icon (e.g. size mismatch); fall back to the standard one.
            _tray.ShowNotification(title, text, NotificationIcon.Info);
        }
    }

    private ProjectNotificationSettings GetProjectNotifications(string project)
    {
        var existing = _settings.ProjectNotifications.FirstOrDefault(entry =>
            string.Equals(entry.Key, project, StringComparison.OrdinalIgnoreCase)).Value;
        return existing ?? new ProjectNotificationSettings
        {
            BuildFailed = _settings.NotifyBuildFailed,
            BuildFixed = _settings.NotifyBuildFixed,
            ReviewRequested = _settings.NotifyReviewRequested,
            UnresolvedComments = true,
            ShowPullRequests = true
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
            : Strings.Format("Tooltip_Summary", failed, reviews, _snapshot.MyPullRequestsToRevise, _snapshot.OtherPullRequestsToRevise);
        _tray.ToolTipText = text.Length > 127 ? text[..127] : text;
        if (_popup.IsVisible)
        {
            _popup.Render(_snapshot);
        }
    }

    private void TogglePopup()
    {
        // Kliknięcie w ikonę dezaktywuje popup (Hide), więc ignorujemy ponowne otwarcie tuż po zamknięciu.
        if (_popup.IsVisible) { _popup.Hide(); return; }
        if ((DateTime.UtcNow - _popup.HiddenAtUtc).TotalMilliseconds < 300)
        {
            return;
        }
        _popup.Show(_snapshot);
    }

    private IReadOnlyList<(string Key, string Project, string Text)> GetPipelineItems(IntegrationConnection connection) =>
        _all.Builds.Where(build => build.ConnectionId == connection.Id)
            .Select(b => (Key: b.Key, Project: b.Project, Text: b.Definition))
            .Concat(_all.Releases.Where(release => release.ConnectionId == connection.Id)
                .Select(r => (Key: r.Key, Project: r.Project, Text: r.Definition)))
            .OrderBy(i => i.Project).ThenBy(i => i.Text).ToList();

    private void ShowSettings()
    {
        if (_settingsWindow != null) { TrayPopupWindow.BringToFront(_settingsWindow); return; }
        _settingsWindow = new SettingsWindow(_settings, () =>
        {
            _settings = AppSettings.Load();
            ApplySettings();
            _settingsWindow?.UpdateSettings(_settings);
        }, GetPipelineItems, GetProjects, GetRepositories, () => _ = ShowTestNotificationsAsync());
        _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        TrayPopupWindow.BringToFront(_settingsWindow);
    }

    private IReadOnlyList<string> GetProjects(IntegrationConnection connection)
    {
        if (connection.Provider == IntegrationProvider.GitHub)
        {
            return [GitHubActionsClient.GetRepositoryName(connection.Repository)];
        }

        return _all.Builds.Where(build => build.ConnectionId == connection.Id).Select(build => build.Project)
            .Concat(_all.Releases.Where(release => release.ConnectionId == connection.Id).Select(release => release.Project))
            .Concat(_all.PullRequests.Where(pullRequest => pullRequest.ConnectionId == connection.Id).Select(pullRequest => pullRequest.Project))
            .Concat(string.IsNullOrWhiteSpace(connection.Project) ? [] : [connection.Project])
            .Concat(_connectionProjects.GetValueOrDefault(connection.Id) ?? [])
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(project => project, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    private IReadOnlyList<RepositoryInfo> GetRepositories(IntegrationConnection connection) =>
        _connectionRepositories.GetValueOrDefault(connection.Id) ?? [];

    private async Task ShowTestNotificationsAsync()
    {
        if (_testNotificationsRunning)
        {
            return;
        }

        _testNotificationsRunning = true;
        try
        {
            var notifications = new (string Title, string Text, ToastIcon Icon)[]
            {
                (Strings.Get("Toast_TestNotification"), Strings.Get("Toast_TestNotificationText"), ToastIcon.Info),
                (Strings.Format("Toast_BuildFailed", "CicdLight-CI"), Strings.Format("Toast_BuildFailedText", "main", "123", "Jan Kowalski"), ToastIcon.BuildFailed),
                (Strings.Format("Toast_BuildFixed", "CicdLight-CI"), Strings.Format("Toast_BuildFixedText", "main", "124"), ToastIcon.BuildFixed),
                (Strings.Format("Toast_Review", "CicdLight"), Strings.Format("Toast_ReviewText", 42, "Add repository filter", "Jan Kowalski"), ToastIcon.ReviewRequested),
                (Strings.Get("Toast_UnresolvedComment"), Strings.Format("Toast_UnresolvedCommentText", 42, "Add repository filter", 2), ToastIcon.UnresolvedComments),
                (Strings.Get("Update_AvailableTitle"), Strings.Format("Update_AvailableText", "1.2.0", "1.3.0"), ToastIcon.Info)
            };

            for (var i = 0; i < notifications.Length; i++)
            {
                if (i > 0)
                {
                    // A new balloon replaces the one still on screen, so give each one time to be seen.
                    await Task.Delay(TestNotificationInterval);
                }

                var (title, text, icon) = notifications[i];
                Toast(title, text, "", icon);
            }
        }
        finally
        {
            _testNotificationsRunning = false;
        }
    }

    private static void OpenUrl(string? url)
    {
        if (!string.IsNullOrEmpty(url))
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
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
        IntPtr iconHandle;
        using (var bmp = new Bitmap(32, 32))
        {
            using (var g = Graphics.FromImage(bmp))
            using (var brush = new SolidBrush(color))
            using (var pen = new Pen(Color.White, 2.4f) { LineJoin = LineJoin.Round, StartCap = LineCap.Round, EndCap = LineCap.Round })
            using (var path = new GraphicsPath())
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                const float x = 2, w = 28, d = 12;
                path.AddArc(x, x, d, d, 180, 90);
                path.AddArc(x + w - d, x, d, d, 270, 90);
                path.AddArc(x + w - d, x + w - d, d, d, 0, 90);
                path.AddArc(x, x + w - d, d, d, 90, 90);
                path.CloseFigure();
                g.FillPath(brush, path);
                g.DrawLines(pen, [new PointF(6f, 17f), new PointF(11.5f, 17f), new PointF(14.5f, 9.5f), new PointF(18.5f, 22.5f), new PointF(21f, 16f), new PointF(26f, 16f)]);
            }
            iconHandle = bmp.GetHicon();
        }
        return Icon.FromHandle(iconHandle);
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    private static void DestroyIconHandle(Icon? icon)
    {
        if (icon != null)
        {
            DestroyIcon(icon.Handle);
        }
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
        foreach (var client in _clients)
        {
            client.Client.Dispose();
        }
        _clients.Clear();
        _toastIcons.Dispose();
        _tray.Dispose();
    }
}
