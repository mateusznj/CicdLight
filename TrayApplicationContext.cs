using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Reflection;
using System.Runtime.InteropServices;

namespace CicdLight;

public sealed class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _tray;
    private readonly ContextMenuStrip _menu = new();
    private readonly System.Windows.Forms.Timer _timer = new();
    private readonly SynchronizationContext _ui = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();
    private AppSettings _settings = AppSettings.Load();
    private AzureDevOpsClient? _client;
    private CancellationTokenSource? _cts;
    private Snapshot _snapshot = Snapshot.Empty();
    private Snapshot _all = Snapshot.Empty();
    private Dictionary<string, BuildInfo>? _previousBuilds;
    private HashSet<int>? _previousReviewPrs;
    private string? _balloonUrl;
    private bool _refreshing;

    public TrayApplicationContext()
    {
        _tray = new NotifyIcon { Visible = true, ContextMenuStrip = _menu, Icon = MakeIcon(Health.Unknown), Text = "CicdLight" };
        _tray.MouseClick += (_, e) => { if (e.Button == MouseButtons.Left) ShowMenu(); };
        _tray.BalloonTipClicked += (_, _) => OpenUrl(_balloonUrl);

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
        _timer.Interval = Math.Max(15, _settings.PollSeconds) * 1000;
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
                    Balloon($"Build failed: {b.Definition}", $"{b.Branch} #{b.BuildNumber} ({b.RequestedFor})", b.Url, ToolTipIcon.Error);
                else if (b.IsSucceeded && old != null && old.IsFailed)
                    Balloon($"Build fixed: {b.Definition}", $"{b.Branch} #{b.BuildNumber}", b.Url, ToolTipIcon.Info);
            }

            foreach (var p in s.PullRequests.Where(p => p.NeedsMyReview && !_previousReviewPrs.Contains(p.Id)))
                Balloon($"Review requested: {p.Repository}", $"#{p.Id} {p.Title} - {p.Author}", p.Url, ToolTipIcon.Info);
        }

        _previousBuilds = builds;
        _previousReviewPrs = reviewPrs;
    }

    private void Balloon(string title, string text, string url, ToolTipIcon icon)
    {
        _balloonUrl = url;
        _tray.ShowBalloonTip(5000, title, text, icon);
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
        _tray.Text = text.Length > 127 ? text[..127] : text;
        RebuildMenu();
    }

    private void RebuildMenu()
    {
        _menu.Items.Clear();

        if (_snapshot.Error != null)
            _menu.Items.Add(new ToolStripMenuItem(_snapshot.Error) { Enabled = false });

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

        if (_menu.Items.Count > 0) _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add("Refresh now", null, async (_, _) => await RefreshAsync());
        _menu.Items.Add("Open Azure DevOps", null, (_, _) => OpenUrl(_settings.IsConfigured ? _settings.OrganizationUrl : null));
        _menu.Items.Add("Choose pipelines...", null, (_, _) => ChoosePipelines());
        _menu.Items.Add("Settings...", null, (_, _) => ShowSettings());
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add("Exit", null, (_, _) => ExitThread());
    }

    private void AddSection(string title, IEnumerable<(string Text, string Url)> items)
    {
        var list = items.ToList();
        if (list.Count == 0) return;
        if (_menu.Items.Count > 0) _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add(new ToolStripMenuItem($"{title} ({list.Count})") { Enabled = false, Font = new Font(_menu.Font, FontStyle.Bold) });
        foreach (var (text, url) in list)
            _menu.Items.Add(text, null, (_, _) => OpenUrl(url));
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

    private void ChoosePipelines()
    {
        var items = _all.Builds.Select(b => (Key: b.Key, Text: $"Pipeline: {b.Project} / {b.Definition}"))
            .Concat(_all.Releases.Select(r => (Key: r.Key, Text: $"Release: {r.Project} / {r.Definition}")))
            .OrderBy(i => i.Text).ToList();
        if (items.Count == 0) { MessageBox.Show("No pipelines loaded yet. Check the settings and refresh first.", "CicdLight"); return; }

        using var form = new Form
        {
            Text = "CicdLight - Choose pipelines", StartPosition = FormStartPosition.CenterScreen,
            Size = new Size(520, 520), MinimizeBox = false, MaximizeBox = false, FormBorderStyle = FormBorderStyle.FixedDialog
        };
        var list = new CheckedListBox { Dock = DockStyle.Fill, CheckOnClick = true };
        var watched = _settings.WatchedItems.ToHashSet();
        foreach (var i in items) list.Items.Add(new Entry(i.Key, i.Text), watched.Count == 0 || watched.Contains(i.Key));
        var ok = new Button { Text = "Save", DialogResult = DialogResult.OK, AutoSize = true };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, AutoSize = true };
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, FlowDirection = FlowDirection.RightToLeft, AutoSize = true };
        buttons.Controls.Add(cancel);
        buttons.Controls.Add(ok);
        form.Controls.Add(list);
        form.Controls.Add(buttons);
        form.AcceptButton = ok;
        form.CancelButton = cancel;
        if (form.ShowDialog() != DialogResult.OK) return;

        var chosen = list.CheckedItems.Cast<Entry>().Select(e => e.Key).ToList();
        _settings.WatchedItems = chosen.Count == items.Count ? [] : chosen;
        _settings.Save();
        _snapshot = _all.Filter(_settings.WatchedItems);
        Update();
    }

    private sealed record Entry(string Key, string Text)
    {
        public override string ToString() => Text;
    }

    private static string Trim(string s) => s.Length > 60 ? s[..57] + "..." : s;

    private void ShowSettings()
    {
        using var form = new SettingsForm(_settings);
        if (form.ShowDialog() != DialogResult.OK) return;
        _settings = AppSettings.Load();
        ApplySettings();
    }

    private void ShowMenu()
    {
        var mi = typeof(NotifyIcon).GetMethod("ShowContextMenu", BindingFlags.Instance | BindingFlags.NonPublic);
        mi?.Invoke(_tray, null);
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

    protected override void ExitThreadCore()
    {
        _timer.Stop();
        _cts?.Cancel();
        _client?.Dispose();
        _tray.Visible = false;
        _tray.Dispose();
        base.ExitThreadCore();
    }
}
