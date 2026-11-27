using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using H.NotifyIcon.Interop;

namespace CicdLight;

/// <summary>Renders and caches icons for balloon notifications (builds, pull requests, comments).</summary>
internal sealed class ToastIconProvider : IDisposable
{
    private const float Grid = 32f;

    private readonly Dictionary<(ToastIcon Icon, int Size), IntPtr> _handles = [];

    /// <summary>Size the shell requires for a large custom balloon icon.</summary>
    public static int RequiredSize => IconUtilities.GetRequiredCustomIconSize(largeIcon: true).Width;

    /// <summary>Returns null for <see cref="ToastIcon.Info" />, which uses the standard system icon.</summary>
    public IntPtr? Get(ToastIcon icon)
    {
        if (icon == ToastIcon.Info)
        {
            return null;
        }

        var size = RequiredSize;
        var key = (icon, size);
        if (!_handles.TryGetValue(key, out var handle))
        {
            handle = Render(icon, size);
            _handles[key] = handle;
        }
        return handle;
    }

    private static IntPtr Render(ToastIcon icon, int size)
    {
        using var bmp = new Bitmap(size, size);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.ScaleTransform(size / Grid, size / Grid);
            using var background = new SolidBrush(GetColor(icon));
            using var white = new SolidBrush(Color.White);
            using var pen = new Pen(Color.White, 2.6f) { LineJoin = LineJoin.Round, StartCap = LineCap.Round, EndCap = LineCap.Round };

            FillRoundedRectangle(g, background, new RectangleF(1, 1, 30, 30), 10);
            switch (icon)
            {
                case ToastIcon.BuildFailed:
                    g.DrawLine(pen, 10, 10, 22, 22);
                    g.DrawLine(pen, 22, 10, 10, 22);
                    break;
                case ToastIcon.BuildFixed:
                    g.DrawLines(pen, [new PointF(8, 17), new PointF(14, 23), new PointF(24, 10)]);
                    break;
                case ToastIcon.ReviewRequested:
                    DrawPullRequest(g, pen);
                    break;
                case ToastIcon.UnresolvedComments:
                    DrawComment(g, white, GetColor(icon));
                    break;
            }
        }
        return bmp.GetHicon();
    }

    private static Color GetColor(ToastIcon icon) => icon switch
    {
        ToastIcon.BuildFailed => Color.FromArgb(231, 76, 60),
        ToastIcon.BuildFixed => Color.FromArgb(46, 184, 92),
        ToastIcon.ReviewRequested => Color.FromArgb(155, 89, 182),
        ToastIcon.UnresolvedComments => Color.FromArgb(241, 150, 15),
        _ => Color.Gray
    };

    private static void DrawPullRequest(Graphics g, Pen pen)
    {
        g.DrawEllipse(pen, 7, 6, 6, 6);
        g.DrawEllipse(pen, 7, 20, 6, 6);
        g.DrawEllipse(pen, 19, 20, 6, 6);
        g.DrawLine(pen, 10, 12, 10, 20);
        g.DrawLine(pen, 22, 20, 22, 11);
        g.DrawLines(pen, [new PointF(18, 15), new PointF(22, 11), new PointF(26, 15)]);
    }

    private static void DrawComment(Graphics g, Brush white, Color background)
    {
        FillRoundedRectangle(g, white, new RectangleF(6, 7, 20, 14), 5);
        g.FillPolygon(white, [new PointF(10, 20), new PointF(10, 27), new PointF(17, 20)]);
        using var linePen = new Pen(background, 2f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawLine(linePen, 10, 12, 22, 12);
        g.DrawLine(linePen, 10, 16, 18, 16);
    }

    private static void FillRoundedRectangle(Graphics g, Brush brush, RectangleF bounds, float diameter)
    {
        using var path = new GraphicsPath();
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        g.FillPath(brush, path);
    }

    public void Dispose()
    {
        foreach (var handle in _handles.Values)
        {
            DestroyIcon(handle);
        }
        _handles.Clear();
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);
}
