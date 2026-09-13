using System.Drawing.Drawing2D;
using MiniLottery.Core;

namespace MiniLottery.UI;

internal sealed class HealthMeter : Control
{
    private int health = 100, damage;
    public string Fighter { get; set; } = "TY";
    public Color Accent { get; set; } = Theme.Teal;
    public HealthMeter() { DoubleBuffered = true; Dock = DockStyle.Fill; BackColor = Theme.Surface; }
    public void UpdateHealth(int value, int lost = 0)
    {
        health = Math.Clamp(value, 0, 100); damage = lost;
        AccessibleName = $"{Fighter}: zdraví {health} ze 100";
        AccessibleDescription = lost > 0 ? $"V tomto kole ztráta {lost} zdraví." : "";
        Invalidate();
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        float s = DeviceDpi / 96f;
        var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
        using var label = Theme.Font(9, FontStyle.Bold);
        Theme.Write(g, Fighter, label, Accent, new Rectangle(0, 0, Width / 2, (int)(25 * s)));
        Theme.Write(g, $"{health} / 100" + (damage > 0 ? $"   −{damage}" : ""), label, Theme.Text,
            new Rectangle(Width / 2, 0, Width / 2, (int)(25 * s)), TextFormatFlags.Right | TextFormatFlags.VerticalCenter);
        var track = new RectangleF(0, 28 * s, Math.Max(1, Width - 1), 10 * s);
        using var path = Theme.Round(track, 5 * s);
        using var bg = new SolidBrush(Theme.Inset); g.FillPath(bg, path);
        if (damage > 0)
        {
            using var lost = new SolidBrush(Color.FromArgb(90, Accent));
            g.FillRectangle(lost, 0, track.Y, track.Width * Math.Min(100, health + damage) / 100, track.Height);
        }
        if (health > 0)
        {
            var filled = new RectangleF(0, track.Y, track.Width * health / 100, track.Height);
            using var fill = new LinearGradientBrush(filled, ControlPaint.Dark(Accent), Accent, 0f);
            using var rounded = Theme.Round(filled, 5 * s); g.FillPath(fill, rounded);
        }
    }
}

internal sealed class BattleSceneView : PictureBox
{
    private string headline = "O KORUNU ARÉNY", detail = "Pět kapitol. Tvoje rozhodnutí. Jeden soupeř.";
    private Color accent = Theme.Gold;
    public BattleSceneView()
    {
        DoubleBuffered = true; Dock = DockStyle.Fill; BackColor = Theme.Inset;
        SizeMode = PictureBoxSizeMode.Zoom; Cursor = Cursors.Hand;
        AccessibleName = "Ilustrace souboje; dvojklik otevře velký náhled";
    }
    public void SetRound(BattleRound? round)
    {
        headline = round == null ? "O KORUNU ARÉNY" : round.Winner switch {
            RoundWinner.Player => "KOLO PRO TEBE", RoundWinner.Computer => "KOLO PRO SOUPEŘE", _ => "VYROVNANÝ STŘET"
        };
        detail = round == null ? "Pět kapitol. Tvoje rozhodnutí. Jeden soupeř." :
            $"{round.Chapter}  ·  {BattleTactics.Info(round.PlayerTactic).Name} proti {BattleTactics.Info(round.ComputerTactic).Name}";
        accent = round == null || round.Winner == RoundWinner.Draw ? Theme.Gold : round.Winner == RoundWinner.Player ? Theme.Teal : Theme.Red;
        Invalidate();
    }
    protected override void OnPaint(PaintEventArgs pe)
    {
        base.OnPaint(pe);
        float s = DeviceDpi / 96f;
        int h = (int)(68 * s);
        if (Height < h + 20 || Width < 30) return;
        var bounds = new Rectangle(0, Height - h, Width, h);
        using var shade = new LinearGradientBrush(bounds, Color.Transparent, Color.FromArgb(240, Theme.Background), 90f);
        pe.Graphics.FillRectangle(shade, bounds);
        using var title = Theme.Font(13, FontStyle.Bold);
        using var caption = Theme.Font(8);
        Theme.Write(pe.Graphics, headline, title, accent, new Rectangle((int)(16 * s), bounds.Y + (int)(12 * s), Width - (int)(32 * s), (int)(27 * s)));
        Theme.Write(pe.Graphics, detail, caption, Theme.Text, new Rectangle((int)(16 * s), bounds.Y + (int)(39 * s), Width - (int)(32 * s), (int)(23 * s)));
    }
}
