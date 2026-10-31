using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Graphics;

namespace CicdLight;

public sealed class ConnectionEditorWindow : Window
{
    private readonly ComboBox _provider = new() { MinWidth = 340 };
    private readonly TextBox _target = new() { MinWidth = 340 };
    private readonly TextBox _project = new() { MinWidth = 340 };
    private readonly StackPanel _projectField = new() { Spacing = 4 };
    private readonly PasswordBox _credential = new() { MinWidth = 340 };
    private readonly TextBlock _error = new() { TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock _targetLabel = new();
    private readonly TextBlock _projectLabel = new();
    private readonly TextBlock _credentialLabel = new();
    private readonly IntegrationConnection _draft;
    private readonly bool _isNew;
    private readonly Func<IntegrationConnection, bool> _isDuplicate;
    private readonly Action<IntegrationConnection> _onSaved;

    public ConnectionEditorWindow(
        IntegrationConnection? connection,
        Func<IntegrationConnection, bool> isDuplicate,
        Action<IntegrationConnection> onSaved)
    {
        _isNew = connection == null;
        _draft = connection == null ? new IntegrationConnection() : new IntegrationConnection
        {
            Id = connection.Id,
            Provider = connection.Provider,
            Repository = connection.Repository,
            Organization = connection.Organization,
            Project = connection.Project,
            EncryptedCredential = connection.EncryptedCredential,
            SelectedItems = [.. connection.SelectedItems],
            HasSelection = connection.HasSelection
        };
        _isDuplicate = isDuplicate;
        _onSaved = onSaved;
        Title = Strings.Get(_isNew ? "Connection_AddTitle" : "Connection_EditTitle");
        WindowIcon.Apply(this);
        AppWindow.Resize(new SizeInt32(520, 500));

        _provider.Items.Add(Strings.Get("Connection_AzureDevOps"));
        _provider.Items.Add(Strings.Get("Connection_GitHub"));
        _provider.SelectedIndex = _draft.Provider == IntegrationProvider.AzureDevOps ? 0 : 1;
        _provider.IsEnabled = _isNew;
        _provider.SelectionChanged += (_, _) => UpdateProviderFields();
        _target.Text = _draft.Provider == IntegrationProvider.AzureDevOps ? _draft.Organization : _draft.Repository;
        _project.Text = _draft.Project;
        _credential.PlaceholderText = _isNew
            ? Strings.Get(_draft.Provider == IntegrationProvider.AzureDevOps ? "Connection_AzurePat" : "Connection_GitHubTokenOptional")
            : Strings.Get("Connection_LeaveCredentialEmpty");
        UpdateProviderFields();

        var save = new Button
        {
            Content = Strings.Get(_isNew ? "Button_AddConnection" : "Button_UpdateConnection"),
            Style = (Style)Application.Current.Resources["AccentButtonStyle"]
        };
        var cancel = new Button { Content = Strings.Get("Button_Cancel") };
        save.Click += (_, _) => Save();
        cancel.Click += (_, _) => Close();

        var panel = new StackPanel { Spacing = 12, Padding = new Thickness(24) };
        panel.Children.Add(new TextBlock
        {
            Text = Strings.Get(_isNew ? "Connection_AddTitle" : "Connection_EditTitle"),
            FontSize = 20,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        });
        panel.Children.Add(Labeled(Strings.Get("Connection_Provider"), _provider));
        panel.Children.Add(LabeledDynamic(_targetLabel, _target));
        _projectField.Children.Add(_projectLabel);
        _projectField.Children.Add(_project);
        panel.Children.Add(_projectField);
        panel.Children.Add(LabeledDynamic(_credentialLabel, _credential));
        panel.Children.Add(_error);
        panel.Children.Add(new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Margin = new Thickness(0, 12, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { save, cancel }
        });
        Content = panel;
        panel.Loaded += (_, _) => FitHeight(panel);
    }

    private void UpdateProviderFields()
    {
        var isAzure = _provider.SelectedIndex == 0;
        _targetLabel.Text = Strings.Get(isAzure ? "Connection_Organization" : "Connection_Repository");
        _target.PlaceholderText = Strings.Get(isAzure ? "Connection_OrganizationPlaceholder" : "Connection_RepositoryPlaceholder");
        _projectLabel.Text = Strings.Get("Connection_Project");
        _projectField.Visibility = isAzure ? Visibility.Visible : Visibility.Collapsed;
        _credentialLabel.Text = Strings.Get(isAzure ? "Connection_AzurePat" : "Connection_GitHubToken");
    }

    private void Save()
    {
        var isAzure = _provider.SelectedIndex == 0;
        var target = _target.Text.Trim().TrimEnd('/');
        var project = _project.Text.Trim();
        var credential = _credential.Password.Trim();
        if (target.Length == 0 || (isAzure && (project.Length == 0 || (_isNew && credential.Length == 0))))
        {
            _error.Text = Strings.Get("Connection_RequiredFields");
            return;
        }

        _draft.Provider = isAzure ? IntegrationProvider.AzureDevOps : IntegrationProvider.GitHub;
        _draft.Organization = isAzure ? target : "";
        _draft.Project = isAzure ? project : "";
        _draft.Repository = isAzure ? "" : target;
        if (!string.IsNullOrEmpty(credential)) _draft.Credential = credential;

        if (!isAzure)
        {
            try { _ = GitHubActionsClient.GetRepositoryUrl(target); }
            catch (FormatException)
            {
                _error.Text = Strings.Get("Settings_InvalidGitHubRepository");
                return;
            }
        }

        if (_isDuplicate(_draft))
        {
            _error.Text = Strings.Get("Connection_AlreadyExists");
            return;
        }

        _onSaved(_draft);
        Close();
    }

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

    private static StackPanel LabeledDynamic(TextBlock label, FrameworkElement control) => new()
    {
        Spacing = 4,
        Children = { label, control }
    };
}