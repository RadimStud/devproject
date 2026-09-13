using System.Drawing.Drawing2D;

namespace MiniLottery.UI;

internal static class Theme
{
    public static readonly Color Background = Color.FromArgb(13, 19, 29);
    public static readonly Color Surface = Color.FromArgb(22, 31, 44);
    public static readonly Color Inset = Color.FromArgb(15, 23, 34);
    public static readonly Color Border = Color.FromArgb(43, 57, 73);
    public static readonly Color Text = Color.FromArgb(237, 243, 247);
    public static readonly Color Muted = Color.FromArgb(157, 175, 193);
    public static readonly Color Gold = Color.FromArgb(243, 195, 109);
    public static readonly Color Teal = Color.FromArgb(99, 221, 198);
    public static readonly Color Red = Color.FromArgb(255, 145, 153);
    public static Font Font(float size = 10, FontStyle style = FontStyle.Regular) => new("Segoe UI", size, style);
    public static GraphicsPath Round(RectangleF rect, float radius)
    {
        var path = new GraphicsPath();
        float d = Math.Min(radius * 2, Math.Min(rect.Width, rect.Height));
        if (d <= 0) return path;
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
    public static void Write(Graphics g, string text, Font font, Color color, Rectangle bounds,
        TextFormatFlags flags = TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis) =>
        TextRenderer.DrawText(g, text, font, bounds, color, flags | TextFormatFlags.NoPadding);
}

internal sealed class Card : Panel
{
    public Card()
    {
        DoubleBuffered = true;
        BackColor = Theme.Surface;
        Padding = new Padding(20);
        ResizeRedraw = true;
    }
    protected override void OnPaintBackground(PaintEventArgs e)
    {
        e.Graphics.Clear(Theme.Background);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = Theme.Round(new RectangleF(.5f, .5f, Width - 1, Height - 1), 14 * DeviceDpi / 96f);
        using var fill = new SolidBrush(Theme.Surface);
        e.Graphics.FillPath(fill, path);
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = Theme.Round(new RectangleF(.5f, .5f, Width - 1, Height - 1), 14 * DeviceDpi / 96f);
        using var pen = new Pen(Theme.Border);
        e.Graphics.DrawPath(pen, path);
    }
}

internal sealed class ActionButton : Button
{
    public bool Primary { get; set; }
    public bool Active { get; set; }
    private bool hovered;
    public ActionButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        Cursor = Cursors.Hand;
        Font = Theme.Font(10, FontStyle.Bold);
        DoubleBuffered = true;
        Height = 44;
        UseVisualStyleBackColor = false;
    }
    protected override void OnMouseEnter(EventArgs e) { hovered = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { hovered = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(Theme.Surface);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        Color fill = !Enabled ? Theme.Border : Active ? Theme.Teal : Primary ? Theme.Gold : Theme.Inset;
        if (hovered && Enabled) fill = ControlPaint.Light(fill, .08f);
        using var path = Theme.Round(new RectangleF(1, 1, Width - 3, Height - 3), 9 * DeviceDpi / 96f);
        using var brush = new SolidBrush(fill);
        using var border = new Pen(Active ? Theme.Teal : Primary ? fill : Theme.Border);
        e.Graphics.FillPath(brush, path);
        e.Graphics.DrawPath(border, path);
        Theme.Write(e.Graphics, Text, Font, !Enabled ? Theme.Muted : Primary || Active ? Theme.Background : Theme.Text,
            ClientRectangle, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        if (Focused && ShowFocusCues)
            ControlPaint.DrawFocusRectangle(e.Graphics, Rectangle.Inflate(ClientRectangle, -6, -6), Theme.Text, fill);
    }
}

internal sealed class BallDisplay : Control
{
    private int[]? numbers;
    private bool matched;
    public BallDisplay()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
        AccessibleRole = AccessibleRole.StaticText;
        AccessibleName = "Vylosovaná čísla";
    }
    public void ShowNumbers(int[]? value, bool win = false)
    {
        numbers = value;
        matched = win;
        AccessibleDescription = value == null ? "Čeká na losování" : string.Join(", ", value);
        Invalidate();
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        float scale = DeviceDpi / 96f;
        float diameter = Math.Min(100 * scale, Math.Min(Height - 16 * scale, (Width - 56 * scale) / 3f));
        if (diameter <= 4) return;
        float gap = 22 * scale;
        float start = (Width - (diameter * 3 + gap * 2)) / 2;
        using var numberFont = Theme.Font(diameter / scale * .32f, FontStyle.Bold);
        for (int i = 0; i < 3; i++)
        {
            var r = new RectangleF(start + i * (diameter + gap), (Height - diameter) / 2, diameter, diameter);
            using var glow = new SolidBrush(Color.FromArgb(25, matched ? Theme.Gold : Theme.Teal));
            g.FillEllipse(glow, RectangleF.Inflate(r, 5 * scale, 5 * scale));
            using var body = new LinearGradientBrush(r, matched ? Color.FromArgb(247, 213, 150) : Color.FromArgb(74, 119, 127),
                matched ? Color.FromArgb(135, 91, 41) : Color.FromArgb(20, 43, 56), 65f);
            g.FillEllipse(body, r);
            using var edge = new Pen(matched ? Theme.Gold : Color.FromArgb(103, 169, 169), 1.2f * scale);
            g.DrawEllipse(edge, r);
            using var shine = new Pen(Color.FromArgb(95, Color.White), 2 * scale);
            g.DrawArc(shine, RectangleF.Inflate(r, -6 * scale, -6 * scale), 208, 78);
            var face = Rectangle.Round(RectangleF.Inflate(r, -diameter * .2f, -diameter * .2f));
            using var faceBrush = new SolidBrush(matched ? Color.FromArgb(255, 240, 208) : Color.FromArgb(225, 240, 236));
            g.FillEllipse(faceBrush, face);
            Theme.Write(g, numbers == null ? "–" : numbers[i].ToString(), numberFont, Theme.Background, face,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }
}

internal sealed class FrequencyChart : Control
{
    private int[] values = new int[10];
    public FrequencyChart()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        BackColor = Theme.Surface;
        AccessibleRole = AccessibleRole.Chart;
        AccessibleName = "Četnosti číslic v posledním losování";
    }
    public void SetValues(int[] frequencies)
    {
        values = (int[])frequencies.Clone();
        AccessibleDescription = string.Join(", ", values.Select((v, i) => $"{i}: {v} výskytů"));
        Invalidate();
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        float s = DeviceDpi / 96f;
        int top = (int)(24 * s), bottom = (int)(24 * s);
        int chartHeight = Height - top - bottom;
        if (chartHeight < 1 || Width < 10) return;
        int sum = values.Sum();
        double max = sum == 0 ? .15 : Math.Max(.15, (double)values.Max() / sum * 1.12);
        using var small = Theme.Font(8);
        using var baseline = new Pen(Theme.Border) { DashStyle = DashStyle.Dash };
        float referenceY = top + chartHeight * (1 - (float)(.10 / max));
        g.DrawLine(baseline, 0, referenceY, Width, referenceY);
        float slot = Width / 10f;
        for (int i = 0; i < 10; i++)
        {
            double p = sum == 0 ? 0 : (double)values[i] / sum;
            float h = (float)(p / max) * chartHeight;
            var rect = new RectangleF(i * slot + slot * .2f, top + chartHeight - Math.Max(2, h), slot * .6f, Math.Max(2, h));
            using var brush = new LinearGradientBrush(rect, i == Array.IndexOf(values, values.Max()) && sum > 0 ? Theme.Gold : Theme.Teal,
                Color.FromArgb(36, 77, 81), 90f);
            g.FillRectangle(brush, rect);
            Theme.Write(g, sum == 0 ? "–" : $"{p:P0}", small, Theme.Muted, new Rectangle((int)(i * slot), 0, (int)slot, top),
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            Theme.Write(g, i.ToString(), small, Theme.Text, new Rectangle((int)(i * slot), Height - bottom, (int)slot, bottom),
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }
}
