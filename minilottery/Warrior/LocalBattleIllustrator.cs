using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using MiniLottery.Core;
using MiniLottery.UI;

namespace MiniLottery.Warrior;

internal sealed class LocalBattleIllustrator : IDisposable
{
    private readonly Image arena;
    private readonly Image fighters;
    public LocalBattleIllustrator()
    {
        // Copy files into detached bitmaps so installation files never remain locked.
        arena = Load("arena.png");
        fighters = Load("fighters.png");
    }
    private static Image Load(string name)
    {
        using var image = Image.FromFile(Path.Combine(AppContext.BaseDirectory, "Assets", "Warrior", name));
        return new Bitmap(image);
    }
    public Bitmap Render(BattleRound? round)
    {
        var image = new Bitmap(1200, 675);
        using var g = Graphics.FromImage(image);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        string environment = round?.Scenario.Id ?? "gate";
        // Move the camera through the arena without changing the recurring fighters.
        RectangleF camera = environment switch {
            "gate" => new(0, 80, 1320, 880), "stairs" => new(230, 115, 1306, 865),
            "shadow" => new(190, 50, 1346, 880), "feint" => new(90, 155, 1350, 865),
            "crown" => new(0, 0, 1536, 1024), _ => new(0, 95, 1536, 914)
        };
        g.DrawImage(arena, new Rectangle(0, 0, 1200, 675), camera, GraphicsUnit.Pixel);
        using var atmosphere = new SolidBrush(Color.FromArgb(environment == "shadow" ? 100 : 42,
            environment == "fire" ? Color.DarkOrange : environment == "rain" ? Color.SteelBlue : Theme.Background));
        g.FillRectangle(atmosphere, 0, 0, 1200, 675);
        var visual = new Random(round?.VisualSeed ?? 713);
        DrawWeather(g, environment, visual);
        int leftPose = round == null || round.Winner == RoundWinner.Draw ? 0 : round.Winner == RoundWinner.Player ? 1 : 2;
        int rightPose = round == null || round.Winner == RoundWinner.Draw ? 0 : round.Winner == RoundWinner.Computer ? 1 : 2;
        DrawFighter(g, true, leftPose);
        DrawFighter(g, false, rightPose);
        if (round != null && round.Winner != RoundWinner.Draw)
        {
            int cx = round.Winner == RoundWinner.Player ? 780 : 440;
            using var spark = new Pen(Theme.Gold, 2);
            for (int i = 0; i < 18; i++)
            {
                double a = visual.NextDouble() * Math.PI * 2;
                int length = visual.Next(8, 35);
                g.DrawLine(spark, cx, 385, cx + (float)Math.Cos(a) * length, 385 + (float)Math.Sin(a) * length);
            }
        }
        using var vignette = new LinearGradientBrush(new Rectangle(0, 0, 1200, 675), Color.Transparent, Color.FromArgb(215, Theme.Background), 90f);
        g.FillRectangle(vignette, 0, 0, 1200, 675);
        // These small labels explain what the local illustration represents; AI output stays unlabelled.
        using var caption = Theme.Font(15, FontStyle.Bold);
        Theme.Write(g, "TY", caption, Theme.Teal, new Rectangle(60, 600, 420, 40));
        Theme.Write(g, "PC", caption, Theme.Red, new Rectangle(770, 600, 350, 40), TextFormatFlags.Right | TextFormatFlags.VerticalCenter);
        return image;
    }
    private void DrawWeather(Graphics g, string scene, Random random)
    {
        if (scene == "rain")
        {
            using var rain = new Pen(Color.FromArgb(130, 183, 219, 230), 1.3f);
            for (int i = 0; i < 260; i++) { int x = random.Next(1200), y = random.Next(675); g.DrawLine(rain, x, y, x - 8, y + 25); }
        }
        else if (scene == "arrows")
        {
            using var shaft = new Pen(Color.FromArgb(200, 222, 208, 176), 2);
            for (int i = 0; i < 18; i++)
            {
                int x = random.Next(100, 1150), y = random.Next(40, 360);
                g.DrawLine(shaft, x, y, x - 42, y + 75);
                g.DrawLine(shaft, x - 42, y + 75, x - 40, y + 60);
                g.DrawLine(shaft, x - 42, y + 75, x - 29, y + 66);
            }
        }
        else if (scene == "fire")
        {
            for (int i = 0; i < 180; i++)
            {
                using var ember = new SolidBrush(Color.FromArgb(random.Next(80, 230), 255, random.Next(125, 200), 70));
                int r = random.Next(2, 6); g.FillEllipse(ember, random.Next(1200), random.Next(550), r, r * 2);
            }
        }
        else if (scene == "shadow")
        {
            using var mist = new SolidBrush(Color.FromArgb(30, 150, 197, 195));
            for (int i = 0; i < 10; i++) g.FillEllipse(mist, random.Next(-200, 1000), random.Next(160, 480), 500, 80);
        }
        else if (scene == "crown")
        {
            using var light = new SolidBrush(Color.FromArgb(32, Theme.Gold));
            g.FillPolygon(light, new[] { new Point(420, 0), new Point(550, 0), new Point(1080, 675), new Point(200, 675) });
            using var fabric = new SolidBrush(Color.FromArgb(225, 104, 62, 22));
            using var trim = new Pen(Theme.Gold, 3);
            foreach (int x in new[] { 60, 1070 })
            {
                Point[] banner = { new(x, 0), new(x + 70, 0), new(x + 70, 155), new(x + 35, 182), new(x, 155) };
                g.FillPolygon(fabric, banner); g.DrawPolygon(trim, banner);
                g.DrawLines(trim, new[] { new Point(x + 15, 66), new Point(x + 21, 103), new Point(x + 49, 103), new Point(x + 56, 66), new Point(x + 43, 82), new Point(x + 35, 62), new Point(x + 27, 82), new Point(x + 15, 66) });
            }
            using var dust = new SolidBrush(Color.FromArgb(130, Theme.Gold));
            for (int i = 0; i < 85; i++) g.FillEllipse(dust, random.Next(1200), random.Next(550), 2, 4);
        }
        else if (scene is "attrition" or "feint" or "stairs")
        {
            using var dust = new SolidBrush(Color.FromArgb(45, 210, 189, 159));
            for (int i = 0; i < 18; i++) g.FillEllipse(dust, random.Next(50, 1100), random.Next(450, 580), random.Next(15, 90), random.Next(4, 15));
        }
    }
    private void DrawFighter(Graphics g, bool player, int pose)
    {
        // The art is an irregular atlas; clip each pose explicitly, including its sword.
        RectangleF source = pose switch {
            0 => new RectangleF(0, player ? 0 : 500, 480, 500),
            1 => new RectangleF(player ? 480 : 360, player ? 0 : 500, 750, 500),
            _ => new RectangleF(1050, player ? 0 : 500, 486, 500)
        };
        float scale = .91f;
        float x = player ? pose == 1 ? 135 : 140 : pose == 1 ? 545 : 695;
        float y = 122;
        var state = g.Save();
        g.TranslateTransform(x, y);
        g.ScaleTransform(scale, scale);
        using var clip = new GraphicsPath();
        if (pose == 1 && player)
            clip.AddPolygon(new[] { new PointF(0, 0), new PointF(750, 0), new PointF(750, 170), new PointF(590, 170), new PointF(590, 500), new PointF(0, 500) });
        else if (pose == 1)
            clip.AddPolygon(new[] { new PointF(0, 0), new PointF(750, 0), new PointF(750, 500), new PointF(120, 500), new PointF(120, 150), new PointF(0, 150) });
        else if (pose == 2) clip.AddRectangle(player
            ? new RectangleF(0, 175, 486, 325)
            : new RectangleF(130, 130, 356, 370)); // Exclude the adjacent attack pose's red cape.
        else if (!player)
            clip.AddPolygon(new[] { new PointF(0, 0), new PointF(480, 0), new PointF(480, 55), new PointF(350, 55), new PointF(350, 135), new PointF(480, 135), new PointF(480, 500), new PointF(0, 500) });
        else clip.AddRectangle(new RectangleF(0, 0, 480, 500));
        g.SetClip(clip);
        g.DrawImage(fighters, new RectangleF(0, 0, source.Width, source.Height), source, GraphicsUnit.Pixel);
        g.Restore(state);
    }
    public void Dispose() { arena.Dispose(); fighters.Dispose(); }
}
