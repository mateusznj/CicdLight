namespace CicdLight;

public sealed class SettingsForm : Form
{
    private readonly TextBox _org = new() { Width = 300 };
    private readonly TextBox _projects = new() { Width = 300 };
    private readonly TextBox _pat = new() { Width = 300, UseSystemPasswordChar = true };
    private readonly NumericUpDown _poll = new() { Minimum = 15, Maximum = 3600, Width = 80 };
    private readonly CheckBox _notify = new() { Text = "Show notifications", AutoSize = true };
    private readonly CheckBox _mine = new() { Text = "Only builds requested by me", AutoSize = true };
    private readonly CheckBox _startup = new() { Text = "Start with Windows", AutoSize = true };
    private readonly AppSettings _s;

    public SettingsForm(AppSettings settings)
    {
        _s = settings;
        Text = "CicdLight - Settings";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;

        _org.Text = settings.Organization;
        _projects.Text = settings.Projects;
        _pat.Text = settings.Pat;
        _poll.Value = Math.Clamp(settings.PollSeconds, 15, 3600);
        _notify.Checked = settings.Notifications;
        _mine.Checked = settings.OnlyMyBuilds;
        _startup.Checked = settings.StartWithWindows;

        var ok = new Button { Text = "Save", DialogResult = DialogResult.OK, AutoSize = true };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, AutoSize = true };
        AcceptButton = ok;
        CancelButton = cancel;
        ok.Click += (_, _) => Apply();

        var grid = new TableLayoutPanel { AutoSize = true, ColumnCount = 2, Padding = new Padding(12) };
        void Row(string label, Control c)
        {
            grid.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3, 8, 8, 3) });
            grid.Controls.Add(c);
        }
        Row("Organization (name or URL)", _org);
        Row("Projects (comma separated, empty = all)", _projects);
        Row("Personal Access Token", _pat);
        Row("Refresh every (seconds)", _poll);
        grid.Controls.Add(new Label());  grid.Controls.Add(_notify);
        grid.Controls.Add(new Label());  grid.Controls.Add(_mine);
        grid.Controls.Add(new Label());  grid.Controls.Add(_startup);
        var buttons = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.RightToLeft };
        buttons.Controls.Add(cancel);
        buttons.Controls.Add(ok);
        grid.Controls.Add(new Label());
        grid.Controls.Add(buttons);
        Controls.Add(grid);
    }

    private void Apply()
    {
        _s.Organization = _org.Text.Trim();
        _s.Projects = _projects.Text.Trim();
        _s.Pat = _pat.Text.Trim();
        _s.PollSeconds = (int)_poll.Value;
        _s.Notifications = _notify.Checked;
        _s.OnlyMyBuilds = _mine.Checked;
        _s.StartWithWindows = _startup.Checked;
        _s.Save();
        Autostart.Set(_s.StartWithWindows);
    }
}

internal static class Autostart
{
    private const string Key = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public static void Set(bool enabled)
    {
        using var k = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(Key, writable: true);
        if (k == null) return;
        if (enabled) k.SetValue("CicdLight", $"\"{Environment.ProcessPath}\"");
        else k.DeleteValue("CicdLight", throwOnMissingValue: false);
    }
}
