namespace CicdLight;

internal static class Icons
{
    public static string GetGlyph(AppIcon icon) => icon switch
    {
        AppIcon.Refresh => "\uE72C",
        AppIcon.Settings => "\uE713",
        AppIcon.AzureDevOps => "\uE774",
        AppIcon.GitHub => "\uE8A7",
        AppIcon.Exit => "\uE711",
        AppIcon.Ok => "\uEC61",
        AppIcon.Failed => "\uEB90",
        AppIcon.Running => "\uE768",
        AppIcon.Other => "\uE91F",
        AppIcon.Review or AppIcon.PullRequest => "\uE8F4",
        AppIcon.Comments => "\uE8BD",
        _ => throw new ArgumentOutOfRangeException(nameof(icon), icon, "Unknown application icon.")
    };

    public static Windows.UI.Color GetColor(AppIcon icon) => icon switch
    {
        AppIcon.Ok => Windows.UI.Color.FromArgb(255, 46, 184, 92),
        AppIcon.Failed => Windows.UI.Color.FromArgb(255, 231, 76, 60),
        AppIcon.Running => Windows.UI.Color.FromArgb(255, 52, 152, 219),
        AppIcon.Review => Windows.UI.Color.FromArgb(255, 241, 196, 15),
        AppIcon.Comments => Windows.UI.Color.FromArgb(255, 241, 150, 15),
        _ => Windows.UI.Color.FromArgb(255, 150, 150, 150)
    };
}
