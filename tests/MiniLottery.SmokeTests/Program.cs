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
                Check(form.VerticalScroll.Visible, "Compact window scrolls to preserve controls");
                form.ClientSize = new Size(1240, 900);
                credit.Value = 10000;
                Find<Button>(form, "duelPlay").PerformClick();
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
    private static void Capture(Form form, string path)
    {
        using var image = new Bitmap(form.Width, form.Height);
        form.DrawToBitmap(image, new Rectangle(0, 0, image.Width, image.Height));
        image.Save(path, ImageFormat.Png);
    }
}
