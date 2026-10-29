using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Graphics;

namespace CicdLight;

public sealed class ChoosePipelinesWindow : Window
{
    private readonly List<(string Key, CheckBox Box)> _boxes = [];

    public ChoosePipelinesWindow(IReadOnlyList<(string Key, string Project, string Text)> items, ICollection<string> watched, Action<List<string>> onSaved)
    {
        Title = "CicdLight - Choose pipelines";
        AppWindow.Resize(new SizeInt32(560, 620));

        var list = new StackPanel { Spacing = 6, Padding = new Thickness(16) };
        foreach (var group in items.GroupBy(i => i.Project).OrderBy(g => g.Key, StringComparer.CurrentCultureIgnoreCase))
        {
            var children = new List<CheckBox>();
            var body = new StackPanel { Spacing = 2, Padding = new Thickness(24, 0, 0, 0) };
            foreach (var (key, _, text) in group.OrderBy(i => i.Text, StringComparer.CurrentCultureIgnoreCase))
            {
                var box = new CheckBox { Content = text, IsChecked = watched.Count == 0 || watched.Contains(key) };
                _boxes.Add((key, box));
                children.Add(box);
                body.Children.Add(box);
            }

            var header = new CheckBox { IsThreeState = true, Content = $"{group.Key} ({children.Count})" };
            var updating = false;
            void SyncHeader()
            {
                updating = true;
                var on = children.Count(c => c.IsChecked == true);
                header.IsChecked = on == 0 ? false : on == children.Count ? true : null;
                updating = false;
            }
            header.Click += (_, _) =>
            {
                if (updating) return;
                var value = header.IsChecked == true;
                foreach (var c in children) c.IsChecked = value;
                SyncHeader();
            };
            foreach (var c in children)
            {
                c.Checked += (_, _) => SyncHeader();
                c.Unchecked += (_, _) => SyncHeader();
            }
            SyncHeader();

            list.Children.Add(new Expander
            {
                Header = header,
                Content = body,
                IsExpanded = true,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Stretch
            });
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
