using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace CicdLight;

public static class ScrollViewerExtensions
{
    private const double WheelStep = 48.0 / 120.0;

    /// <summary>Ręczna obsługa kółka myszy; obejście sytuacji, gdy ScrollViewer nie dostaje kółka (np. monitor ze skalowaniem ≠ 100%).</summary>
    public static ScrollViewer WithWheelFallback(this ScrollViewer scroll)
    {
        // handledEventsToo = false: gdy ScrollViewer obsłuży kółko natywnie, nie przewijamy drugi raz.
        scroll.AddHandler(UIElement.PointerWheelChangedEvent, new PointerEventHandler(OnPointerWheelChanged), false);
        return scroll;
    }

    private static void OnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        var scroll = (ScrollViewer)sender;
        var properties = e.GetCurrentPoint(scroll).Properties;
        if (properties.IsHorizontalMouseWheel || scroll.ScrollableHeight <= 0)
        {
            return;
        }

        var target = Math.Clamp(scroll.VerticalOffset - properties.MouseWheelDelta * WheelStep, 0, scroll.ScrollableHeight);
        scroll.ChangeView(null, target, null, true);
        e.Handled = true;
    }
}
