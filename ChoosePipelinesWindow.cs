using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Graphics;

namespace CicdLight;

public sealed class ChoosePipelinesWindow : Window
{
    private readonly List<(string Key, CheckBox Box)> _boxes = [];

    public ChoosePipelinesWindow(IReadOnlyList<(string Key, string Text)> items, ICollection<string> watched, Action<List<string>> onSaved)
    {
        Title = "CicdLight - Choose pipelines";
        AppWindow.Resize(new SizeInt32(560, 560));

        var list = new StackPanel { Spacing = 2, Padding = new Thickness(16) };
        foreach (var (key, text) in items)
        {
            var box = new CheckBox { Content = text, IsChecked = watched.Count == 0 || watched.Contains(key) };
            _boxes.Add((key, box));
            list.Children.Add(box);
        }

        var save = new Button { Content = "Save", Style = (Style)Application.Current.Resources["AccentButtonStyle"] };
        var cancel = new Button { Content = "Cancel" };
        save.Click += (_, _) =>
        {
            var chosen = _boxes.Where(b => b.Box.IsChecked == true).Select(b => b.Key).ToList();
            Close();
            onSaved(chosen);
        };
        cancel.Click += (_, _) => Close();

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
        var scroll = new ScrollViewer { Content = list };
        Grid.SetRow(buttons, 1);
        root.Children.Add(scroll);
        root.Children.Add(buttons);
        Content = root;
    }
}
