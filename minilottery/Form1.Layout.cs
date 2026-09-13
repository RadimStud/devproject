using MiniLottery.UI;

namespace WinFormsApp1;

public partial class Form1
{
    private readonly NumericUpDown creditInput = NumberInput("creditInput", 0, 100000000, 10000, 18, 2);
    private readonly NumericUpDown stakeInput = NumberInput("stakeInput", .01m, 1000000, 1, 18, 2);
    private readonly NumericUpDown[] digits = Enumerable.Range(0, 3).Select(i => NumberInput($"digit{i}", 0, 9, 1, 20)).ToArray();
    private readonly BallDisplay balls = new() { Dock = DockStyle.Fill, Name = "drawBalls" };
    private readonly FrequencyChart frequency = new() { Dock = DockStyle.Fill, Name = "frequencyChart" };
    private readonly Label balance = Copy("0,00", 21, Theme.Teal, true, "sessionBalance");
    private readonly Label drawStatus = Copy("Připraveno na první losování", 10, Theme.Muted, name: "drawStatus");
    private readonly Label luckyResult = Copy("Zvolte tři číslice a zkuste štěstí.", 11, Theme.Text, true, "luckyResult");
    private readonly Label bonusResult = Copy("Čeká na první hru", 10, Theme.Muted, name: "bonusResult");
    private readonly Label duelResult = Copy("Vyšší číslo vyhrává. Remíza patří krupiérovi.", 9, Theme.Muted, name: "duelResult");
    private readonly Label luckyStats = Copy("0 her  ·  Bilance 0,00", 9, Theme.Muted, name: "luckyStats");
    private readonly Label bonusStats = Copy("0 her  ·  Bilance 0,00", 9, Theme.Muted, name: "bonusStats");
    private readonly Label duelStats = Copy("0 her  ·  Bilance 0,00", 9, Theme.Muted, name: "duelStats");
    private readonly Label roundStats = Copy("Medián —     Nejrychlejší —     Nejdelší —", 9, Theme.Muted, name: "roundStats");
    private readonly Label bonusTarget = Copy("Cílová kombinace  1 · 1 · 1", 11, Theme.Text, true, "bonusTarget");
    private readonly Label playerScore = Copy("—", 25, Theme.Teal, true, "playerScore");
    private readonly Label dealerScore = Copy("—", 25, Theme.Gold, true, "dealerScore");
    private readonly ComboBox parity = new() { Name = "parityInput", DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Theme.Inset, ForeColor = Theme.Text, FlatStyle = FlatStyle.Flat, Dock = DockStyle.Fill, Font = Theme.Font(10) };
    private readonly ActionButton luckyPlay = Button("Losovat", "luckyPlay", true);
    private readonly ActionButton luckyAuto = Button("Auto", "luckyAuto");
    private readonly ActionButton bonusPlay = Button("Hrát bonus", "bonusPlay");
    private readonly ActionButton bonusAuto = Button("Auto", "bonusAuto");
    private readonly ActionButton duelPlay = Button("Hrát duel", "duelPlay");
    private readonly ActionButton duelAuto = Button("Auto", "duelAuto");
    private readonly ActionButton historyButton = Button("Historie tahů", "historyButton");
    private readonly CheckBox animate = new() { Text = "Animace", Name = "animationInput", Checked = true, AutoSize = true, ForeColor = Theme.Muted, BackColor = Theme.Surface, Dock = DockStyle.Fill, Font = Theme.Font(9), AccessibleName = "Animovat losování" };
    private readonly Label footer = Copy("MiniLottery  /  Tři malé hry, jedno místo pro štěstí.", 9, Theme.Muted, name: "footer");

    private static Label Copy(string text, float size = 10, Color? color = null, bool bold = false, string? name = null) => new()
    {
        Text = text, Name = name ?? "", AutoSize = false, Dock = DockStyle.Fill, Margin = Padding.Empty,
        ForeColor = color ?? Theme.Text, BackColor = Color.Transparent, Font = Theme.Font(size, bold ? FontStyle.Bold : FontStyle.Regular),
        TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true
    };
    private static ActionButton Button(string text, string name, bool primary = false) => new()
    {
        Text = text, Name = name, Primary = primary, AccessibleName = text, Dock = DockStyle.Fill, Margin = new Padding(0, 0, 8, 0)
    };
    private static NumericUpDown NumberInput(string name, decimal min, decimal max, decimal value, float fontSize, int places = 0) => new()
    {
        Name = name, Minimum = min, Maximum = max, Value = value, DecimalPlaces = places, ThousandsSeparator = true,
        Font = Theme.Font(fontSize, FontStyle.Bold), BackColor = Theme.Inset, ForeColor = Theme.Text,
        BorderStyle = BorderStyle.FixedSingle, TextAlign = HorizontalAlignment.Center, Dock = DockStyle.Fill,
        Margin = new Padding(0, 3, 12, 0), AccessibleName = name
    };
    private static TableLayoutPanel Rows(params float[] heights)
    {
        var table = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = heights.Length, Margin = Padding.Empty, BackColor = Color.Transparent };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        foreach (float h in heights) table.RowStyles.Add(h < 0 ? new RowStyle(SizeType.Percent, -h) : new RowStyle(SizeType.Absolute, h));
        return table;
    }
    private static TableLayoutPanel Columns(params float[] widths)
    {
        var table = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 1, ColumnCount = widths.Length, Margin = Padding.Empty, BackColor = Color.Transparent };
        table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        foreach (float w in widths) table.ColumnStyles.Add(w < 0 ? new ColumnStyle(SizeType.Percent, -w) : new ColumnStyle(SizeType.Absolute, w));
        return table;
    }
    private static Card Wrap(Control content, Padding? margin = null)
    {
        var card = new Card { Dock = DockStyle.Fill, Margin = margin ?? new Padding(0, 0, 0, 14) };
        card.Controls.Add(content);
        return card;
    }
    private void BuildInterface()
    {
        SuspendLayout();
        Text = "MiniLottery";
        Name = "MiniLottery";
        BackColor = Theme.Background;
        ForeColor = Theme.Text;
        Font = Theme.Font();
        AutoScaleDimensions = new SizeF(96, 96);
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(1240, 900);
        MinimumSize = new Size(980, 700);
        StartPosition = FormStartPosition.CenterScreen;
        DoubleBuffered = true;
        AutoScroll = false;

        var root = Rows(76, 114, -100, 30);
        root.Padding = new Padding(24, 10, 24, 8);
        root.MinimumSize = new Size(930, 850);
        var header = Columns(-38, -35, -27);
        var brand = Rows(38, 24);
        brand.Controls.Add(Copy("◈  MiniLottery", 24, Theme.Text, true), 0, 0);
        brand.Controls.Add(Copy("MALÁ HRA. VELKÁ ZVĚDAVOST.", 8, Theme.Muted), 0, 1);
        header.Controls.Add(brand, 0, 0);
        var badge = Copy("●  VIRTUÁLNÍ KREDIT", 9, Theme.Teal, true);
        badge.TextAlign = ContentAlignment.MiddleRight;
        header.Controls.Add(badge, 2, 0);
        var navigation = Columns(-50, -50);
        navigation.Padding = new Padding(0, 10, 0, 16);
        var lotteryTab = Button("Minihry", "lotteryTab");
        var warriorTab = Button("Bojovník", "warriorTab");
        lotteryTab.Active = true;
        lotteryTab.AccessibleRole = warriorTab.AccessibleRole = AccessibleRole.PageTab;
        navigation.Controls.Add(lotteryTab, 0, 0);
        navigation.Controls.Add(warriorTab, 1, 0);
        header.Controls.Add(navigation, 1, 0);
        root.Controls.Add(header, 0, 0);

        var wallet = Columns(-36, -28, -36);
        var credit = Rows(22, -100);
        credit.Controls.Add(Copy("VÁŠ KREDIT", 8, Theme.Muted, true), 0, 0);
        creditInput.AccessibleName = "Virtuální kredit";
        credit.Controls.Add(creditInput, 0, 1);
        wallet.Controls.Add(Wrap(credit, new Padding(0, 0, 14, 14)), 0, 0);
        var stake = Rows(22, -100);
        stake.Controls.Add(Copy("SÁZKA NA HRU", 8, Theme.Muted, true), 0, 0);
        stakeInput.AccessibleName = "Sázka na hru";
        stake.Controls.Add(stakeInput, 0, 1);
        wallet.Controls.Add(Wrap(stake, new Padding(0, 0, 14, 14)), 1, 0);
        var summary = Rows(22, -100);
        summary.Controls.Add(Copy("BILANCE TÉTO RELACE", 8, Theme.Muted, true), 0, 0);
        summary.Controls.Add(balance, 0, 1);
        wallet.Controls.Add(Wrap(summary), 2, 0);
        root.Controls.Add(wallet, 0, 1);

        var body = Columns(-63, -37);
        var left = Rows(-64, -36);
        left.Margin = new Padding(0, 0, 14, 0);
        left.Controls.Add(BuildLuckyCard(), 0, 0);
        left.Controls.Add(BuildStatisticsCard(), 0, 1);
        body.Controls.Add(left, 0, 0);
        var right = Rows(-51, -49);
        right.Controls.Add(BuildBonusCard(), 0, 0);
        right.Controls.Add(BuildDuelCard(), 0, 1);
        body.Controls.Add(right, 1, 0);
        var pages = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty, BackColor = Theme.Background };
        var warriorPage = BuildWarriorPage();
        warriorPage.Visible = false;
        pages.Controls.Add(warriorPage);
        pages.Controls.Add(body);
        void SelectPage(bool warriorSelected)
        {
            if (warriorSelected) PrepareWarriorIntro();
            body.Visible = !warriorSelected;
            warriorPage.Visible = warriorSelected;
            if (warriorSelected) warriorPage.BringToFront(); else body.BringToFront();
            lotteryTab.Active = !warriorSelected;
            warriorTab.Active = warriorSelected;
            lotteryTab.Invalidate();
            warriorTab.Invalidate();
            ArrangeContent();
            if (warriorSelected && ClientSize.Height < 980 && Screen.FromControl(this).WorkingArea.Height >= 1020)
                ClientSize = new Size(ClientSize.Width, 980);
            if (warriorSelected && battleMatch == null) footer.Text = "Bojovník · sestav rytíře, nastav sázku a vstup do turnaje o korunu arény.";
        }
        root.Controls.Add(pages, 0, 2);
        root.Controls.Add(footer, 0, 3);
        // A scroll viewport owns an explicitly sized canvas. DockStyle.Fill plus
        // MinimumSize alone clips content in WinForms and does not expose scrollbars.
        root.Dock = DockStyle.None;
        var viewport = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Name = "viewport", BackColor = Theme.Background };
        viewport.Controls.Add(root);
        bool arranging = false;
        void ArrangeContent()
        {
            if (arranging) return;
            arranging = true;
            try
            {
                int minWidth = (int)Math.Ceiling(930 * DeviceDpi / 96f);
                int minHeight = (int)Math.Ceiling((warriorPage.Visible ? 980 : 850) * DeviceDpi / 96f);
                viewport.AutoScrollMinSize = new Size(minWidth, minHeight);
                root.Size = new Size(Math.Max(minWidth, viewport.ClientSize.Width), Math.Max(minHeight, viewport.ClientSize.Height));
                root.Location = viewport.AutoScrollPosition;
            }
            finally { arranging = false; }
        }
        lotteryTab.Click += (_, _) => SelectPage(false);
        warriorTab.Click += (_, _) => SelectPage(true);
        viewport.ClientSizeChanged += (_, _) => ArrangeContent();
        DpiChanged += (_, _) => ArrangeContent();
        Controls.Add(viewport);
        ResumeLayout(true);
        ArrangeContent();
    }
    private Card BuildLuckyCard()
    {
        var stack = Rows(33, 25, 48, -100, 25, 31, 46, 40);
        var heading = Columns(-70, -30);
        heading.Controls.Add(Copy("01  /  Lucky Win", 17, Theme.Text, true), 0, 0);
        heading.Controls.Add(animate, 1, 0);
        stack.Controls.Add(heading, 0, 0);
        stack.Controls.Add(Copy("Váš tip · tři číslice od 0 do 9, pořadí rozhoduje", 9, Theme.Muted), 0, 1);
        var picks = Columns(77, 77, 77, -100);
        for (int i = 0; i < 3; i++)
        {
            digits[i].AccessibleName = $"Číslice {i + 1}";
            picks.Controls.Add(digits[i], i, 0);
        }
        var rule = Copy("Výhra při shodě\ndo 692. tahu", 9, Theme.Gold);
        rule.TextAlign = ContentAlignment.MiddleRight;
        picks.Controls.Add(rule, 3, 0);
        stack.Controls.Add(picks, 0, 2);
        stack.Controls.Add(balls, 0, 3);
        drawStatus.TextAlign = ContentAlignment.MiddleCenter;
        stack.Controls.Add(drawStatus, 0, 4);
        luckyResult.TextAlign = ContentAlignment.MiddleCenter;
        stack.Controls.Add(luckyResult, 0, 5);
        var actions = Columns(-58, -20, -22);
        actions.Controls.Add(luckyPlay, 0, 0);
        actions.Controls.Add(luckyAuto, 1, 0);
        historyButton.Font = Theme.Font(8);
        actions.Controls.Add(historyButton, 2, 0);
        stack.Controls.Add(actions, 0, 6);
        stack.Controls.Add(luckyStats, 0, 7);
        return Wrap(stack);
    }
    private Card BuildStatisticsCard()
    {
        var stack = Rows(28, 23, -100, 28);
        stack.Controls.Add(Copy("Čísla pod lupou", 14, Theme.Text, true), 0, 0);
        stack.Controls.Add(Copy("Četnosti poslední hry · přerušovaná linka = 10 %", 9, Theme.Muted), 0, 1);
        stack.Controls.Add(frequency, 0, 2);
        stack.Controls.Add(roundStats, 0, 3);
        return Wrap(stack, Padding.Empty);
    }
    private Card BuildBonusCard()
    {
        var stack = Rows(34, 39, 34, 31, -100, 44, 42);
        stack.Controls.Add(Copy("02  /  Sudá, nebo lichá?", 15, Theme.Text, true), 0, 0);
        stack.Controls.Add(Copy("Tipněte paritu počtu tahů do shody.\nPoužijí se nejčastější číslice Lucky Win.", 9, Theme.Muted), 0, 1);
        parity.Items.AddRange(new object[] { "Lichý počet tahů", "Sudý počet tahů" });
        parity.SelectedIndex = 0;
        parity.AccessibleName = "Výherní parita bonusu";
        stack.Controls.Add(parity, 0, 2);
        stack.Controls.Add(bonusTarget, 0, 3);
        stack.Controls.Add(bonusResult, 0, 4);
        var actions = Columns(-70, -30);
        actions.Controls.Add(bonusPlay, 0, 0);
        actions.Controls.Add(bonusAuto, 1, 0);
        stack.Controls.Add(actions, 0, 5);
        stack.Controls.Add(bonusStats, 0, 6);
        return Wrap(stack);
    }
    private Card BuildDuelCard()
    {
        var stack = Rows(34, 26, -100, 42, 44, 42);
        stack.Controls.Add(Copy("03  /  Duel s krupiérem", 15, Theme.Text, true), 0, 0);
        stack.Controls.Add(Copy("Každý dostane číslo od 1 do 1 000.", 9, Theme.Muted), 0, 1);
        var scores = Columns(-45, -10, -45);
        var player = Rows(21, -100);
        player.Controls.Add(Copy("VY", 8, Theme.Muted, true), 0, 0);
        player.Controls.Add(playerScore, 0, 1);
        scores.Controls.Add(player, 0, 0);
        scores.Controls.Add(Copy("vs", 10, Theme.Muted), 1, 0);
        var dealer = Rows(21, -100);
        dealer.Controls.Add(Copy("KRUPIÉR", 8, Theme.Muted, true), 0, 0);
        dealer.Controls.Add(dealerScore, 0, 1);
        scores.Controls.Add(dealer, 2, 0);
        stack.Controls.Add(scores, 0, 2);
        stack.Controls.Add(duelResult, 0, 3);
        var actions = Columns(-70, -30);
        actions.Controls.Add(duelPlay, 0, 0);
        actions.Controls.Add(duelAuto, 1, 0);
        stack.Controls.Add(actions, 0, 4);
        stack.Controls.Add(duelStats, 0, 5);
        return Wrap(stack, Padding.Empty);
    }
}
