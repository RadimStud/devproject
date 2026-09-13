using MiniLottery.Core;
using MiniLottery.UI;

namespace WinFormsApp1;

public partial class Form1 : Form
{
    private enum Game { Lucky, Bonus, Duel }
    private readonly Random random = new();
    private readonly CancellationTokenSource lifetime = new();
    private CancellationTokenSource? activeSession;
    private Game? runningGame;
    private Game? autoGame;
    private readonly GameStats[] stats = { new(), new(), new() };
    private readonly List<int> luckyRounds = new();
    private int[] commonNumbers = { 1, 1, 1 };
    private Draw[] luckyHistory = Array.Empty<Draw>();
    private Draw[] bonusHistory = Array.Empty<Draw>();

    public Form1()
    {
        InitializeComponent();
        luckyPlay.Click += async (_, _) => await StartAsync(Game.Lucky, false);
        bonusPlay.Click += async (_, _) => await StartAsync(Game.Bonus, false);
        duelPlay.Click += async (_, _) => await StartAsync(Game.Duel, false);
        luckyAuto.Click += async (_, _) => await StartAsync(Game.Lucky, true);
        bonusAuto.Click += async (_, _) => await StartAsync(Game.Bonus, true);
        duelAuto.Click += async (_, _) => await StartAsync(Game.Duel, true);
        historyButton.Click += (_, _) => ShowHistory();
        FormClosing += (_, _) => { lifetime.Cancel(); activeSession?.Cancel(); };
        RefreshControls();
    }

    private async Task StartAsync(Game game, bool automatic)
    {
        if (runningGame.HasValue)
        {
            if (runningGame == game) activeSession?.Cancel();
            return;
        }
        // Validate typed edits before taking an immutable snapshot of the stake and target.
        ValidateChildren();
        if (!ValidateStake()) return;
        using var session = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
        activeSession = session;
        runningGame = game;
        autoGame = automatic ? game : null;
        RefreshControls();
        try
        {
            do
            {
                session.Token.ThrowIfCancellationRequested();
                if (!ValidateStake()) break;
                decimal stake = stakeInput.Value;
                await PlayAsync(game, stake, session.Token);
                if (!automatic) break;
                footer.Text = "Automatické hraní · další hra za 1 sekundu. Zastavit můžete kdykoli.";
                await Task.Delay(1000, session.Token);
            } while (!session.IsCancellationRequested);
        }
        catch (OperationCanceledException)
        {
            if (!IsDisposed)
            {
                footer.Text = "Zastaveno. Nedokončená hra se do kreditu ani statistik nezapočítává.";
                if (game == Game.Duel && duelResult.Text == "Rozdáváme čísla…")
                    duelResult.Text = "Hra zrušena · kredit beze změny";
            }
        }
        catch (Exception ex)
        {
            if (!IsDisposed)
            {
                footer.Text = "Hru se nepodařilo dokončit.";
                MessageBox.Show(this, ex.Message, "MiniLottery", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        finally
        {
            activeSession = null;
            runningGame = null;
            autoGame = null;
            if (!IsDisposed && !Disposing) RefreshControls();
        }
    }

    private bool ValidateStake()
    {
        if (!GameRules.CanPlay(creditInput.Value, stakeInput.Value))
        {
            footer.Text = "Na tuto sázku nestačí kredit. Snižte sázku nebo upravte virtuální kredit.";
            return false;
        }
        if (creditInput.Value > creditInput.Maximum - stakeInput.Value)
        {
            footer.Text = "Snižte virtuální kredit, aby bylo možné připsat případnou výhru.";
            return false;
        }
        return true;
    }

    private async Task PlayAsync(Game game, decimal stake, CancellationToken token)
    {
        bool won;
        Label result;
        string detail;
        if (game == Game.Duel)
        {
            duelResult.Text = "Rozdáváme čísla…";
            duelResult.ForeColor = Theme.Muted;
            playerScore.Text = dealerScore.Text = "—";
            await Task.Delay(animate.Checked ? 450 : 1, token);
            int player = random.Next(1, 1001), dealer = random.Next(1, 1001);
            token.ThrowIfCancellationRequested();
            playerScore.Text = player.ToString("N0");
            dealerScore.Text = dealer.ToString("N0");
            won = GameRules.PlayerWins(player, dealer);
            result = duelResult;
            detail = player == dealer ? "Remíza · vyhrává krupiér" : won ? "Máte vyšší číslo" : "Krupiér má vyšší číslo";
        }
        else
        {
            int[] target = game == Game.Lucky ? digits.Select(d => (int)d.Value).ToArray() : (int[])commonNumbers.Clone();
            bool even = parity.SelectedIndex == 1;
            var simulation = new LotterySession(target, random);
            if (game == Game.Lucky)
            {
                luckyResult.Text = "Hledáme vaši kombinaci…";
                luckyResult.ForeColor = Theme.Muted;
                balls.ShowNumbers(null);
            }
            else
            {
                bonusTarget.Text = $"Cílová kombinace  {string.Join(" · ", target)}";
                bonusResult.ForeColor = Theme.Muted;
            }
            try
            {
                // A bounded batch keeps the message loop responsive without Application.DoEvents.
                while (!simulation.Complete)
                {
                    token.ThrowIfCancellationRequested();
                    simulation.Advance(animate.Checked ? 24 : 512);
                    if (game == Game.Lucky)
                    {
                        var draw = simulation.LastDraw;
                        balls.ShowNumbers(new[] { draw.First, draw.Second, draw.Third });
                        drawStatus.Text = $"{simulation.Rounds:N0}. tah  ·  losování probíhá";
                    }
                    else bonusResult.Text = $"Losujeme…  {simulation.Rounds:N0}. tah";
                    await Task.Delay(animate.Checked ? 16 : 1, token);
                }
                token.ThrowIfCancellationRequested();
            }
            catch (OperationCanceledException)
            {
                if (!IsDisposed)
                {
                    if (game == Game.Lucky)
                    {
                        drawStatus.Text = $"Zrušeno po {simulation.Rounds:N0} tazích";
                        luckyResult.Text = "Hra zrušena · kredit beze změny";
                    }
                    else bonusResult.Text = "Hra zrušena · kredit beze změny";
                }
                throw;
            }
            if (game == Game.Lucky)
            {
                won = GameRules.LuckyWins(simulation.Rounds);
                balls.ShowNumbers(target, won);
                drawStatus.Text = $"Shoda nalezena v {simulation.Rounds:N0}. tahu";
                luckyHistory = simulation.History.ToArray();
                luckyRounds.Add(simulation.Rounds);
                commonNumbers = Enumerable.Range(0, 10).OrderByDescending(i => simulation.Frequencies[i]).ThenBy(i => i).Take(3).ToArray();
                frequency.SetValues(simulation.Frequencies);
                roundStats.Text = $"Medián {GameRules.Median(luckyRounds):N0}    ·    Nejrychlejší {luckyRounds.Min():N0}    ·    Nejdelší {luckyRounds.Max():N0}";
                bonusTarget.Text = $"Cílová kombinace  {string.Join(" · ", commonNumbers)}";
                result = luckyResult;
                detail = won ? "Kombinace padla včas" : "Kombinace padla po limitu";
            }
            else
            {
                won = GameRules.BonusWins(simulation.Rounds, even);
                bonusHistory = simulation.History.ToArray();
                result = bonusResult;
                detail = $"{simulation.Rounds:N0} tahů · {(simulation.Rounds % 2 == 0 ? "sudý" : "lichý")} počet";
            }
        }
        // Cancellation and all animation finish before the single settlement point.
        token.ThrowIfCancellationRequested();
        decimal change = GameRules.NetChange(won, stake);
        creditInput.Value += change;
        stats[(int)game].Record(change);
        result.Text = $"{(won ? "Výhra" : "Prohra")}  {Signed(change)}  ·  {detail}";
        result.ForeColor = won ? Theme.Teal : Theme.Red;
        RefreshStats();
        footer.Text = "Hra dokončena · zvolte další losování, bonus nebo duel.";
    }

    private static string Signed(decimal value) => value > 0 ? $"+{value:N2}" : value.ToString("N2");
    private void RefreshStats()
    {
        var labels = new[] { luckyStats, bonusStats, duelStats };
        for (int i = 0; i < stats.Length; i++)
        {
            var s = stats[i];
            labels[i].Text = $"Her: {s.Games:N0}  ·  Bilance {Signed(s.Net)}\nMaximum {Signed(s.HighestNet)}  ·  Minimum {Signed(s.LowestNet)}";
        }
        decimal net = stats.Sum(s => s.Net);
        balance.Text = Signed(net);
        balance.ForeColor = net < 0 ? Theme.Red : Theme.Teal;
    }
    private void RefreshControls()
    {
        bool busy = runningGame.HasValue;
        var playButtons = new[] { luckyPlay, bonusPlay, duelPlay };
        var autoButtons = new[] { luckyAuto, bonusAuto, duelAuto };
        var titles = new[] { "Losovat", "Hrát bonus", "Hrát duel" };
        for (int i = 0; i < 3; i++)
        {
            bool current = runningGame == (Game)i;
            playButtons[i].Enabled = !busy || current;
            playButtons[i].Text = current ? "Zrušit" : titles[i];
            autoButtons[i].Enabled = !busy || autoGame == (Game)i;
            autoButtons[i].Active = autoGame == (Game)i;
            autoButtons[i].Text = autoGame == (Game)i ? "Zastavit" : "Auto";
            playButtons[i].AccessibleName = playButtons[i].Text;
            autoButtons[i].AccessibleName = $"{autoButtons[i].Text} · {titles[i]}";
            autoButtons[i].Invalidate();
        }
        creditInput.Enabled = stakeInput.Enabled = parity.Enabled = animate.Enabled = !busy;
        foreach (var digit in digits) digit.Enabled = !busy;
        historyButton.Enabled = !busy && (luckyHistory.Length > 0 || bonusHistory.Length > 0);
    }
    private void ShowHistory()
    {
        using var dialog = new Form
        {
            Text = "MiniLottery · historie tahů", ClientSize = new Size(580, 540), MinimumSize = new Size(420, 360),
            StartPosition = FormStartPosition.CenterParent, BackColor = Theme.Background, ForeColor = Theme.Text,
            Font = Theme.Font(), Padding = new Padding(16), ShowInTaskbar = false
        };
        var layout = Rows(55, -100);
        layout.Controls.Add(Copy("Posledních 100 tahů každé dokončené hry.\nGraf četností zahrnuje všechny tahy poslední hry Lucky Win.", 10, Theme.Muted), 0, 0);
        var tabs = new TabControl { Dock = DockStyle.Fill };
        void AddHistory(string name, Draw[] draws)
        {
            var page = new TabPage(name) { BackColor = Theme.Inset, Padding = new Padding(8) };
            var list = new ListBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, BackColor = Theme.Inset, ForeColor = Theme.Text, Font = new Font("Consolas", 11), HorizontalScrollbar = true, IntegralHeight = false };
            if (draws.Length == 0) list.Items.Add("Zatím žádná dokončená hra.");
            else list.Items.AddRange(draws.Reverse().Select(d => (object)d.ToString()).ToArray());
            page.Controls.Add(list);
            tabs.TabPages.Add(page);
        }
        AddHistory("Lucky Win", luckyHistory);
        AddHistory("Bonus", bonusHistory);
        layout.Controls.Add(tabs, 0, 1);
        dialog.Controls.Add(layout);
        dialog.ShowDialog(this);
    }
}
