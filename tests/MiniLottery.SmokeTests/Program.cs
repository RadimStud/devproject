using System.Drawing.Imaging;
using System.Globalization;
using WinFormsApp1;

internal static class Program
{
    private static int checks;
    [STAThread]
    private static int Main(string[] args)
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo("cs-CZ");
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        int exitCode = 1;
        string output = args.Length > 0 ? args[0] : "artifacts/screenshots";
        Directory.CreateDirectory(output);
        using var form = new Form1();
        form.Shown += async (_, _) =>
        {
            try
            {
                // The runner desktop can be 1024x768. Set bounds after Show,
                // then capture offscreen via DrawToBitmap at the design size.
                form.ClientSize = new Size(1240, 900);
                await Task.Delay(200);
                Capture(form, Path.Combine(output, "minilottery-initial.png"));
                AssertVisible(form, "luckyPlay", "bonusPlay", "duelPlay", "creditInput", "stakeInput", "frequencyChart");
                Find<CheckBox>(form, "animationInput").Checked = false;
                var credit = Find<NumericUpDown>(form, "creditInput");
                var stake = Find<NumericUpDown>(form, "stakeInput");
                stake.Value = .25m;
                foreach (string mode in new[] { "lucky", "bonus", "duel" })
                {
                    decimal before = credit.Value;
                    Find<Button>(form, mode + "Play").PerformClick();
                    Check(!credit.Enabled, "Credit locked during game");
                    await Until(() => credit.Enabled);
                    Check(Math.Abs(credit.Value - before) == .25m, mode + " settles once with decimal stake");
                    Check(Find<Label>(form, mode + "Stats").Text.Contains("Her: 1"), mode + " game counted once");
                }
                Capture(form, Path.Combine(output, "minilottery-played.png"));
                CaptureCanvas(form, Path.Combine(output, "minilottery-preview.png"));
                // Auto has a cancellable delay between completed games and cannot re-enter another mode.
                Find<Button>(form, "duelAuto").PerformClick();
                Check(!Find<Button>(form, "luckyPlay").Enabled, "Other game disabled during auto");
                await Until(() => Find<Label>(form, "duelStats").Text.Contains("Her: 2"));
                decimal afterAuto = credit.Value;
                Find<Button>(form, "duelAuto").PerformClick();
                await Until(() => credit.Enabled);
                await Task.Delay(1100);
                Check(credit.Value == afterAuto, "Auto cannot continue after stop");
                // Cancel before the animation completes: no settlement or game count.
                Find<CheckBox>(form, "animationInput").Checked = true;
                string previousStats = Find<Label>(form, "duelStats").Text;
                Find<Button>(form, "duelPlay").PerformClick();
                Find<Button>(form, "duelPlay").PerformClick();
                await Until(() => credit.Enabled);
                Check(credit.Value == afterAuto && Find<Label>(form, "duelStats").Text == previousStats, "Cancellation leaves balance and stats unchanged");
                credit.Value = 0;
                Find<Button>(form, "bonusPlay").PerformClick();
                Check(credit.Enabled && credit.Value == 0, "Insufficient funds prevented");
                form.Size = new Size(1000, 760);
                await Task.Delay(150);
                Capture(form, Path.Combine(output, "minilottery-compact.png"));
                var viewport = Find<Panel>(form, "viewport");
                Check(viewport.VerticalScroll.Visible, "Compact window scrolls to preserve controls");
                viewport.ScrollControlIntoView(Find<Button>(form, "duelPlay"));
                // Scroll the canvas to the bottom: all lower actions must become reachable.
                viewport.AutoScrollPosition = new Point(0, viewport.VerticalScroll.Maximum);
                await Task.Delay(100);
                AssertVisible(form, "duelPlay", "frequencyChart");
                Capture(form, Path.Combine(output, "minilottery-compact-scrolled.png"));
                viewport.AutoScrollPosition = Point.Empty;
                form.ClientSize = new Size(1240, 900);
                credit.Value = 10000;
                Find<Button>(form, "warriorTab").PerformClick();
                Check(Find<Control>(form, "warriorPage").Visible, "Warrior tab opens");
                var picture = Find<PictureBox>(form, "warriorPicture");
                Check(picture.Image is { Width: 1200, Height: 675 }, "Packaged art renders without API key");
                CaptureCanvas(form, Path.Combine(output, "warrior-setup.png"));
                picture.Image!.Save(Path.Combine(output, "warrior-art-intro.png"), ImageFormat.Png);
                var firstPerk = Find<NumericUpDown>(form, "warriorPerk0");
                firstPerk.Value = 3;
                Check(!Find<Button>(form, "warriorStart").Enabled, "Invalid perk budget blocks start");
                firstPerk.Value = 2;
                stake.Value = .25m;
                Find<Button>(form, "warriorStart").PerformClick();
                Check(credit.Value == 9999.75m && !credit.Enabled && !stake.Enabled && !firstPerk.Enabled, "Stake reserved once and inputs locked");
                Check(Find<Label>(form, "warriorComputer").Text.StartsWith("PC ·"), "PC chooses and displays perks after start");
                Find<Button>(form, "lotteryTab").PerformClick();
                Check(!Find<Button>(form, "luckyPlay").Enabled, "Cannot play lottery during warrior match");
                Find<Button>(form, "warriorTab").PerformClick();
                var next = Find<Button>(form, "warriorNext");
                for (int round = 1; round <= 5; round++)
                {
                    next.PerformClick();
                    next.PerformClick();
                    await Until(() => Find<Button>(form, $"warriorRound{round}").Enabled);
                    Check(Find<Label>(form, "warriorTitle").Text.StartsWith($"{round}. kolo / 5"), "Double click does not play an extra round");
                    Check(picture.AccessibleDescription?.Contains("zbývá") == true, "Image and accessible story describe the completed round");
                    picture.Image!.Save(Path.Combine(output, $"warrior-art-round-{round}.png"), ImageFormat.Png);
                    if (round < 5) Check(credit.Value == 9999.75m && !credit.Enabled, "No early payout");
                }
                Check(credit.Enabled && !next.Enabled && !Find<Button>(form, "warriorForfeit").Enabled, "Fifth round ends match and unlocks wallet");
                string settlement = Find<Label>(form, "warriorSettlement").Text;
                decimal expected = settlement.StartsWith("Vítězství") ? 10000.25m : settlement.StartsWith("Porážka") ? 9999.75m : 10000m;
                Check(credit.Value == expected, "Wallet matches final win, loss or draw");
                CaptureCanvas(form, Path.Combine(output, "warrior-preview.png"));
                Find<Button>(form, "warriorRound1").PerformClick();
                Check(Find<Label>(form, "warriorTitle").Text.StartsWith("1. kolo") && credit.Value == expected, "Gallery replays history without reroll or payout");
                Check(Find<Button>(form, "warriorExport").Enabled, "Chronicle export available after play");
                Find<Button>(form, "warriorStart").PerformClick();
                Check(!Find<Button>(form, "warriorRound1").Enabled, "New match clears old gallery");
                next.PerformClick();
                Find<Button>(form, "warriorForfeit").PerformClick();
                await Until(() => credit.Enabled);
                Check(credit.Value == expected - .25m && Find<Label>(form, "warriorSettlement").Text.StartsWith("Zápas vzdán"), "Forfeit during illustration loses exactly one stake");
                form.Size = new Size(1000, 760);
                viewport.AutoScrollPosition = new Point(0, viewport.VerticalScroll.Maximum);
                await Task.Delay(100);
                AssertVisible(form, "warriorNext", "warriorExport");
                Capture(form, Path.Combine(output, "warrior-compact.png"));
                Find<Button>(form, "warriorStart").PerformClick();
                next.PerformClick();
                form.Close();
                await Task.Delay(500);
                Console.WriteLine($"PASS: {checks} Windows UI checks; screenshots: {output}");
                exitCode = 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                exitCode = 1;
            }
            finally { Application.ExitThread(); }
        };
        // An ApplicationContext keeps the pump alive long enough to verify closing during an async game.
        var context = new ApplicationContext();
        form.Show();
        Application.Run(context);
        return exitCode;
    }
    private static T Find<T>(Control root, string name) where T : Control =>
        (T)root.Controls.Find(name, true).Single();
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        checks++;
    }
    private static async Task Until(Func<bool> condition)
    {
        var timeout = DateTime.UtcNow.AddSeconds(15);
        while (!condition())
        {
            if (DateTime.UtcNow > timeout) throw new TimeoutException("UI did not finish in 15 seconds.");
            await Task.Delay(20);
        }
    }
    private static void AssertVisible(Form form, params string[] names)
    {
        var bounds = form.RectangleToScreen(form.ClientRectangle);
        foreach (string name in names)
        {
            var control = Find<Control>(form, name);
            Check(control.Visible && bounds.Contains(control.RectangleToScreen(control.ClientRectangle)), name + " visible in default window");
        }
    }
    private static void CaptureCanvas(Form form, string path)
    {
        // Capture the real native controls at a larger layout size, even when the
        // CI virtual desktop clamps the outer window to 1024x768.
        var canvas = Find<Panel>(form, "viewport").Controls[0];
        Size previous = canvas.Size;
        try
        {
            canvas.Size = new Size(1240, 900);
            canvas.PerformLayout();
            using var image = new Bitmap(canvas.Width, canvas.Height);
            canvas.DrawToBitmap(image, new Rectangle(Point.Empty, image.Size));
            image.Save(path, ImageFormat.Png);
        }
        finally { canvas.Size = previous; }
    }
    private static void Capture(Form form, string path)
    {
        using var image = new Bitmap(form.Width, form.Height);
        form.DrawToBitmap(image, new Rectangle(0, 0, image.Width, image.Height));
        image.Save(path, ImageFormat.Png);
    }
}
