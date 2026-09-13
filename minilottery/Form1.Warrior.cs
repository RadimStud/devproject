using System.Drawing.Imaging;
using System.IO.Compression;
using System.Net.Http;
using System.Text;
using MiniLottery.Core;
using MiniLottery.UI;
using MiniLottery.Warrior;

namespace WinFormsApp1;

public partial class Form1
{
    private readonly NumericUpDown[] perkInputs = Enumerable.Range(0, 6).Select(i => NumberInput($"warriorPerk{i}", 0, 5, 2, 12)).ToArray();
    private readonly Label perkBudget = Copy("Rozděleno 12 / 12 bodů", 10, Theme.Teal, true, "warriorBudget");
    private readonly Label computerBuild = Copy("PC si zvolí perky po zahájení zápasu.\nDostane stejných 12 bodů.", 9, Theme.Muted, name: "warriorComputer");
    private readonly HealthMeter playerHealthMeter = new() { Name = "warriorPlayerHealth", Fighter = "TVŮJ RYTÍŘ", Accent = Theme.Teal, Margin = new Padding(0, 0, 16, 0) };
    private readonly HealthMeter computerHealthMeter = new() { Name = "warriorComputerHealth", Fighter = "SOUPEŘ", Accent = Theme.Red, Margin = new Padding(16, 0, 0, 0) };
    private readonly Label fighterScore = Copy("0 : 0", 17, Theme.Gold, true, "warriorScore");
    private readonly Label battleTitle = Copy("Bojovník · kronika tvého souboje", 15, Theme.Text, true, "warriorTitle");
    private readonly Label battleImageStatus = Copy("Místní ilustrace · připraveno", 8, Theme.Muted, name: "warriorImageStatus");
    private readonly Label battleSettlement = Copy("Výhra +sázka · prohra −sázka · remíza vrací sázku.", 9, Theme.Muted, name: "warriorSettlement");
    private readonly BattleSceneView battlePicture = new() { Name = "warriorPicture" };
    private readonly TextBox battleStory = new() { Name = "warriorStory", Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, BorderStyle = BorderStyle.None, BackColor = Theme.Surface, ForeColor = Theme.Text, Font = Theme.Font(10), Dock = DockStyle.Fill,
        Text = "Do arény vstupují dva rytíři. Korunu získá ten, kdo vyhraje více z pěti kol.\r\n\r\nSestav svého bojovníka a nastav sázku nahoře. Před každým kolem přečti soupeře a zvol nápor, kryt nebo lest. Tvoje volba ovlivní výsledek." };
    private readonly ComboBox imageMode = new() { Name = "warriorImageMode", DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill, Font = Theme.Font(9), AccessibleName = "Způsob generování ilustrací" };
    private readonly ActionButton warriorStart = Button("Zahájit zápas", "warriorStart", true);
    private readonly ActionButton warriorNext = Button("Odehrát 1. kolo", "warriorNext", true);
    private readonly ActionButton warriorForfeit = Button("Vzdát zápas", "warriorForfeit");
    private readonly ActionButton warriorSkipImage = Button("Přeskočit AI", "warriorSkipImage");
    private readonly ActionButton warriorExport = Button("Uložit kroniku", "warriorExport");
    private readonly ActionButton warriorImageSettings = Button("Nastavení AI obrázků", "warriorImageSettings");
    private readonly ActionButton[] roundButtons = Enumerable.Range(1, 5).Select(i => Button($"{i}. kolo", $"warriorRound{i}")).ToArray();
    private readonly HttpClient battleHttp = new(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = Timeout.InfiniteTimeSpan, MaxResponseContentBufferSize = 26000000 };
    private readonly List<(BattleRound Round, Bitmap Image, string Source)> battleGallery = new();
    private LocalBattleIllustrator? battleIllustrator;
    private Bitmap? battleIntro;
    private WarriorMatch? battleMatch;
    private CancellationTokenSource? battleImageCancellation;
    private string battleApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "";
    private bool battleBusy;
    private bool battleSettled;
    private int selectedRound = -1;
    private BattleTactic? selectedTactic;
    private bool editingWarriorBuild = true;
    private Control? warriorSetupView, warriorDecisionView;
    private readonly ToolTip warriorTips = new() { AutoPopDelay = 14000 };
    private readonly ComboBox warriorPreset = new() { Name = "warriorPreset", DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill, BackColor = Theme.Inset, ForeColor = Theme.Text, FlatStyle = FlatStyle.Flat, Font = Theme.Font(9) };
    private readonly Label nextChapter = Copy("DALŠÍ KAPITOLA", 9, Theme.Gold, true, "warriorNextChapter");
    private readonly Label nextScenario = Copy("", 12, Theme.Text, true, "warriorNextScenario");
    private readonly Label scenarioHint = Copy("", 9, Theme.Muted, name: "warriorScenarioHint");
    private readonly Label opponentTell = Copy("", 10, Theme.Text, name: "warriorTell");
    private readonly Label warriorRecap = Copy("", 9, Theme.Muted, name: "warriorRecap");
    private readonly ActionButton editWarriorBuild = Button("Upravit perky", "warriorEditBuild");
    private readonly ActionButton[] tacticButtons = BattleTactics.All.Select(t => Button(t.Name, $"warriorTactic{(int)t.Id}")).ToArray();

    private Control BuildWarriorPage()
    {
        var body = Columns(310, -100);
        body.Name = "warriorPage";
        var setup = Rows(32, 34, 26, 216, 27, 78, -100);
        setup.Controls.Add(Copy("Sestav svého rytíře", 15, Theme.Text, true), 0, 0);
        warriorPreset.Items.AddRange(new object[] { "Vyvážený rytíř", "Berserk · síla a obratnost", "Strážce · obrana a odolnost", "Šermíř · taktika a obratnost" });
        warriorPreset.SelectedIndex = 0;
        warriorPreset.AccessibleName = "Přednastavená sestava perků";
        warriorPreset.SelectedIndexChanged += (_, _) => ApplyWarriorPreset();
        setup.Controls.Add(warriorPreset, 0, 1);
        setup.Controls.Add(perkBudget, 0, 2);
        var perks = Rows(36, 36, 36, 36, 36, 36);
        for (int i = 0; i < 6; i++)
        {
            var row = Columns(-100, 72);
            var info = WarriorBuild.Perks[i];
            var caption = Copy(info.Name, 11, Theme.Text, true);
            row.Controls.Add(caption, 0, 0);
            warriorTips.SetToolTip(caption, info.Description);
            warriorTips.SetToolTip(perkInputs[i], info.Description);
            perkInputs[i].AccessibleName = info.Name;
            perkInputs[i].AccessibleDescription = info.Description;
            perkInputs[i].ValueChanged += (_, _) => RefreshWarriorControls();
            row.Controls.Add(perkInputs[i], 1, 0);
            perks.Controls.Add(row, 0, i);
        }
        setup.Controls.Add(perks, 0, 3);
        setup.Controls.Add(Copy("TVŮJ SOUPEŘ", 9, Theme.Gold, true), 0, 4);
        setup.Controls.Add(computerBuild, 0, 5);
        setup.Controls.Add(Copy("Rozděl přesně 12 bodů, nejvýše 5 do jednoho perku. Najeď na název perku pro vysvětlení.\n\nSázka platí pro celý zápas. Vzdáním zápasu sázku ztrácíš.", 9, Theme.Muted), 0, 6);
        warriorSetupView = setup;
        var decisions = Rows(25, 48, 53, 24, 72, 28, 54, 54, 54, 47, 32, -100);
        decisions.Controls.Add(nextChapter, 0, 0);
        decisions.Controls.Add(nextScenario, 0, 1);
        decisions.Controls.Add(scenarioHint, 0, 2);
        decisions.Controls.Add(Copy("ČTI SOUPEŘE", 9, Theme.Gold, true), 0, 3);
        decisions.Controls.Add(opponentTell, 0, 4);
        decisions.Controls.Add(Copy("ZVOL TAKTIKU PRO TOTO KOLO", 9, Theme.Teal, true), 0, 5);
        string[] shortRules = { "přemůže lest", "zastaví nápor · menší zranění", "obejde kryt" };
        for (int i = 0; i < 3; i++)
        {
            var tactic = (BattleTactic)i;
            var button = tacticButtons[i];
            button.Text = $"{i + 1}   {BattleTactics.Info(tactic).Name.ToUpperInvariant()}\n{shortRules[i]}";
            button.Font = Theme.Font(9, FontStyle.Bold);
            button.Margin = new Padding(0, 0, 0, 6);
            button.Click += (_, _) => { selectedTactic = tactic; RefreshWarriorControls(); };
            warriorTips.SetToolTip(button, BattleTactics.Info(tactic).Description + " Protitaktika přidává +5 k síle, soupeři −5.");
            decisions.Controls.Add(button, 0, i + 6);
        }
        warriorNext.Margin = new Padding(0, 0, 0, 6);
        decisions.Controls.Add(warriorNext, 0, 9);
        editWarriorBuild.Font = Theme.Font(9);
        editWarriorBuild.Click += (_, _) => { editingWarriorBuild = true; RefreshWarriorControls(); };
        decisions.Controls.Add(editWarriorBuild, 0, 10);
        decisions.Controls.Add(warriorRecap, 0, 11);
        warriorDecisionView = decisions;
        decisions.Visible = false;
        var choiceArea = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty };
        choiceArea.Controls.Add(setup);
        choiceArea.Controls.Add(decisions);
        var sidebar = Rows(-100, 45, 34, 36);
        sidebar.Controls.Add(choiceArea, 0, 0);
        sidebar.Controls.Add(warriorStart, 0, 1);
        imageMode.Items.AddRange(new object[] { "Místní ilustrace · bez připojení", "AI obrázky · OpenAI API" });
        imageMode.SelectedIndex = 0;
        imageMode.Margin = new Padding(0, 6, 0, 0);
        imageMode.SelectedIndexChanged += (_, _) => RefreshWarriorControls();
        imageMode.BackColor = Theme.Inset; imageMode.ForeColor = Theme.Text; imageMode.FlatStyle = FlatStyle.Flat;
        sidebar.Controls.Add(imageMode, 0, 2);
        warriorImageSettings.Font = Theme.Font(9);
        sidebar.Controls.Add(warriorImageSettings, 0, 3);
        body.Controls.Add(Wrap(sidebar, new Padding(0, 0, 14, 0)), 0, 0);

        var stage = Rows(30, 53, -100, 23, 105, 57, 38, 35);
        stage.Controls.Add(battleTitle, 0, 0);
        var scoreboard = Columns(-50, 76, -50);
        scoreboard.Controls.Add(playerHealthMeter, 0, 0);
        scoreboard.Controls.Add(fighterScore, 1, 0);
        scoreboard.Controls.Add(computerHealthMeter, 2, 0);
        fighterScore.TextAlign = ContentAlignment.MiddleCenter;
        stage.Controls.Add(scoreboard, 0, 1);
        stage.Controls.Add(battlePicture, 0, 2);
        stage.Controls.Add(battleImageStatus, 0, 3);
        battleStory.Margin = new Padding(0, 8, 0, 8);
        stage.Controls.Add(battleStory, 0, 4);
        var rounds = Columns(-20, -20, -20, -20, -20);
        for (int i = 0; i < 5; i++)
        {
            int index = i;
            roundButtons[i].Font = Theme.Font(9);
            roundButtons[i].Click += (_, _) => ShowBattleRound(index);
            rounds.Controls.Add(roundButtons[i], i, 0);
        }
        stage.Controls.Add(rounds, 0, 5);
        var actions = Columns(-33, -33, -34);
        foreach (var button in new[] { warriorForfeit, warriorSkipImage, warriorExport }) button.Font = Theme.Font(9);
        actions.Controls.Add(warriorForfeit, 0, 0);
        actions.Controls.Add(warriorSkipImage, 1, 0);
        actions.Controls.Add(warriorExport, 2, 0);
        stage.Controls.Add(actions, 0, 6);
        stage.Controls.Add(battleSettlement, 0, 7);
        body.Controls.Add(Wrap(stage, Padding.Empty), 1, 0);
        warriorStart.Click += (_, _) => StartWarriorMatch();
        warriorNext.Click += async (_, _) => await PlayWarriorRoundAsync();
        warriorForfeit.Click += (_, _) => ForfeitWarriorMatch();
        warriorSkipImage.Click += (_, _) => { if (battleBusy) battleImageCancellation?.Cancel(); else ShowBattleStory(); };
        warriorExport.Click += (_, _) => ExportWarriorChronicle();
        warriorImageSettings.Click += (_, _) => ConfigureBattleImages();
        battlePicture.DoubleClick += (_, _) => ShowLargeBattleImage();
        warriorTips.SetToolTip(battlePicture, "Dvojklik otevře ilustraci ve velkém náhledu.");
        return body;
    }

    private LocalBattleIllustrator Illustrator => battleIllustrator ??= new LocalBattleIllustrator();
    private void PrepareWarriorIntro()
    {
        if (battlePicture.Image != null) return;
        battleIntro ??= Illustrator.Render(null);
        battlePicture.Image = battleIntro;
        battlePicture.SetRound(null);
    }
    private void StartWarriorMatch()
    {
        if (runningGame.HasValue || battleBusy) return;
        ValidateChildren();
        if (!ValidateStake()) return;
        if (perkInputs.Sum(x => x.Value) != WarriorBuild.Budget) { perkBudget.Text = "Rozděl přesně 12 bodů."; return; }
        if (imageMode.SelectedIndex == 1 && string.IsNullOrWhiteSpace(battleApiKey))
        { battleImageStatus.Text = "Pro AI obrázky nejdřív nastav vlastní API klíč."; return; }
        var player = new WarriorBuild(perkInputs.Select(x => (int)x.Value));
        // Build everything that can fail before reserving the virtual stake.
        var match = new WarriorMatch(player, stakeInput.Value, random);
        var intro = Illustrator.Render(null);
        ClearBattleGallery();
        battleIntro?.Dispose();
        battleIntro = intro;
        battlePicture.Image = intro;
        battleMatch = match;
        selectedTactic = null;
        editingWarriorBuild = false;
        battleSettled = false;
        battleSettlement.ForeColor = Theme.Muted;
        creditInput.Value -= match.Stake;
        runningGame = Game.Warrior;
        computerBuild.Text = $"PC · {match.ComputerName}\n" + string.Join("\n", Enumerable.Range(0, 3).Select(i =>
            $"{WarriorBuild.Perks[i * 2].Name} {match.Computer[(WarriorPerk)(i * 2)]}    ·    {WarriorBuild.Perks[i * 2 + 1].Name} {match.Computer[(WarriorPerk)(i * 2 + 1)]}"));
        playerHealthMeter.UpdateHealth(100); computerHealthMeter.UpdateHealth(100);
        fighterScore.Text = "0 : 0";
        battlePicture.SetRound(null);
        battleTitle.Text = "Bojovník · postavy jsou připravené";
        battleStory.Text = $"Turnaj O korunu arény začíná. Tvůj soupeř: {match.ComputerName}.\r\n{match.Computer.Describe()}\r\n\r\nRezervovaná sázka: {match.Stake:N2}. Přečti soupeřův postoj vlevo, vyber taktiku a odehraj první kolo.";
        battleSettlement.Text = $"Rezervováno {match.Stake:N2} · výhra vrátí {match.Stake * 2:N2}, remíza {match.Stake:N2}.";
        battleImageStatus.Text = imageMode.SelectedIndex == 1 ? "AI režim · nejvýše 5 placených obrázků v tomto zápase" : "Místní ilustrace · obrázek se sestaví podle výsledku kola";
        footer.Text = "Bojovník · sázka i perky jsou zamčené až do konce zápasu.";
        RefreshControls();
    }

    private async Task PlayWarriorRoundAsync()
    {
        var match = battleMatch;
        if (match == null || match.State != WarriorMatchState.Active || battleBusy || selectedTactic == null) return;
        var tactic = selectedTactic.Value;
        battleBusy = true;
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
        battleImageCancellation = cancellation;
        RefreshWarriorControls();
        int galleryIndex = -1;
        try
        {
            var round = match.PlayRound(tactic);
            selectedTactic = null;
            if (match.State == WarriorMatchState.Completed) SettleWarriorMatch();
            var localImage = Illustrator.Render(round);
            battleGallery.Add((round, localImage, "Místní ilustrace · podle scénáře a výsledku kola"));
            galleryIndex = battleGallery.Count - 1;
            ShowBattleRound(galleryIndex);
            if (imageMode.SelectedIndex == 1)
            {
                battleImageStatus.Text = $"Vzniká AI obrázek {round.Number}/5… může to trvat až 150 s. Přeskočení nemění výsledek kola.";
                var service = new OpenAiBattleImages(battleHttp);
                byte[] bytes = await service.GenerateAsync(battleApiKey, BattleImagePrompt.Create(match, round), cancellation.Token);
                cancellation.Token.ThrowIfCancellationRequested();
                using var stream = new MemoryStream(bytes);
                using var decoded = Image.FromStream(stream, false, true);
                if (decoded.Width > 4096 || decoded.Height > 4096) throw new InvalidDataException("Obrázek má příliš velké rozměry.");
                var aiImage = new Bitmap(decoded);
                var old = battleGallery[galleryIndex].Image;
                battleGallery[galleryIndex] = (round, aiImage, $"Nový AI obrázek podle výsledku · {OpenAiBattleImages.Model}");
                ShowBattleRound(galleryIndex);
                old.Dispose();
            }
            else await Task.Delay(60, cancellation.Token);
        }
        catch (OperationCanceledException)
        {
            if (!IsDisposed && galleryIndex >= 0)
            {
                SetBattleImageSource(galleryIndex, "AI generování přeskočeno · zobrazena místní ilustrace");
                if (imageMode.SelectedIndex == 0) SetBattleImageSource(galleryIndex, "Místní ilustrace · kolo je dokončené");
            }
        }
        catch (Exception ex)
        {
            if (!IsDisposed)
            {
                string message = ex is InvalidOperationException or InvalidDataException ? ex.Message : "Ilustraci se nepodařilo dokončit.";
                if (galleryIndex >= 0) SetBattleImageSource(galleryIndex, message + " Místní ilustrace zachována.");
                else battleImageStatus.Text = message;
            }
        }
        finally
        {
            battleImageCancellation = null;
            battleBusy = false;
            if (match.State != WarriorMatchState.Active) runningGame = null;
            if (!IsDisposed && !Disposing) RefreshControls();
        }
    }
    private void SetBattleImageSource(int index, string source)
    {
        var entry = battleGallery[index];
        battleGallery[index] = (entry.Round, entry.Image, source);
        ShowBattleRound(index);
    }
    private void ShowBattleRound(int index)
    {
        if (index < 0 || index >= battleGallery.Count) return;
        selectedRound = index;
        var item = battleGallery[index];
        var r = item.Round;
        battlePicture.Image = item.Image;
        battlePicture.SetRound(r);
        battlePicture.AccessibleDescription = r.Story;
        battleTitle.Text = $"{r.Number}. kolo / 5 · {r.Scenario.Name}";
        playerHealthMeter.UpdateHealth(r.PlayerHealth, r.PlayerDamage);
        computerHealthMeter.UpdateHealth(r.ComputerHealth, r.ComputerDamage);
        fighterScore.Text = $"{r.PlayerWins} : {r.ComputerWins}";
        battleStory.Text = $"{BattleTactics.Info(r.PlayerTactic).Name} × {BattleTactics.Info(r.ComputerTactic).Name} · síla {r.PlayerPower} : {r.ComputerPower} · hody {r.PlayerRoll} : {r.ComputerRoll}\r\n\r\n" + r.Story.Replace("\n", "\r\n");
        battleImageStatus.Text = item.Source;
        RefreshWarriorControls();
    }
    private void SettleWarriorMatch()
    {
        if (battleMatch == null || battleMatch.State == WarriorMatchState.Active || battleSettled) return;
        decimal payout = battleMatch.TakePayout();
        creditInput.Value += payout;
        decimal net = payout - battleMatch.Stake;
        stats[(int)Game.Warrior].Record(net);
        battleSettled = true;
        string verdict = battleMatch.State == WarriorMatchState.Forfeited ? "Zápas vzdán" : battleMatch.Winner switch {
            RoundWinner.Player => "Vítězství", RoundWinner.Computer => "Porážka", _ => "Remíza"
        };
        battleSettlement.Text = $"{verdict} · konečné skóre {battleMatch.PlayerWins}:{battleMatch.ComputerWins} · bilance {Signed(net)}";
        battleSettlement.ForeColor = net > 0 ? Theme.Teal : net < 0 ? Theme.Red : Theme.Gold;
        footer.Text = $"Bojovník · {verdict.ToLowerInvariant()}. Zápas je vyúčtován; obrázky najdeš v kronice.";
        RefreshStats();
    }
    private void ForfeitWarriorMatch()
    {
        if (battleMatch?.State != WarriorMatchState.Active) return;
        battleMatch.Forfeit();
        battleImageCancellation?.Cancel();
        SettleWarriorMatch();
        if (!battleBusy) runningGame = null;
        RefreshControls();
    }
    private void RefreshWarriorControls()
    {
        bool active = battleMatch?.State == WarriorMatchState.Active;
        bool locked = runningGame.HasValue || battleBusy;
        int points = (int)perkInputs.Sum(x => x.Value);
        perkBudget.Text = $"Rozděleno {points} / 12 bodů";
        perkBudget.ForeColor = points == 12 ? Theme.Teal : Theme.Red;
        foreach (var perk in perkInputs) perk.Enabled = !locked;
        warriorPreset.Enabled = !locked;
        if (warriorSetupView != null && warriorDecisionView != null)
        {
            warriorSetupView.Visible = editingWarriorBuild;
            warriorDecisionView.Visible = !editingWarriorBuild;
            if (editingWarriorBuild) warriorSetupView.BringToFront(); else warriorDecisionView.BringToFront();
        }
        editWarriorBuild.Enabled = !locked;
        imageMode.Enabled = warriorImageSettings.Enabled = !locked;
        warriorStart.Enabled = !locked && points == 12;
        warriorStart.Text = battleMatch == null || editingWarriorBuild ? "Zahájit zápas" : "Odveta · nový zápas";
        warriorNext.Enabled = active && !battleBusy && selectedTactic.HasValue;
        warriorNext.Text = active ? selectedTactic.HasValue ? $"Odehrát {battleMatch!.Rounds.Count + 1}. kolo" : "Nejdřív zvol taktiku" : "Zápas dokončen";
        if (battleMatch == null) warriorNext.Text = "Odehrát 1. kolo";
        warriorForfeit.Enabled = active;
        warriorSkipImage.Text = battleBusy && imageMode.SelectedIndex == 1 ? "Přeskočit AI" : "Celý příběh";
        warriorSkipImage.Enabled = battleBusy ? imageMode.SelectedIndex == 1 : battleGallery.Count > 0;
        warriorExport.Enabled = battleGallery.Count > 0 && !battleBusy;
        RefreshWarriorDecision();
        for (int i = 0; i < 5; i++)
        {
            roundButtons[i].Enabled = i < battleGallery.Count && !battleBusy;
            roundButtons[i].Active = i == selectedRound;
            string[] chapters = { "Brána", "Zkouška", "Zlom", "Výdrž", "Koruna" };
            if (i < battleGallery.Count) roundButtons[i].Text = $"{i + 1}. {chapters[i]}\n{(battleGallery[i].Round.Winner == RoundWinner.Player ? "Výhra" : battleGallery[i].Round.Winner == RoundWinner.Computer ? "Prohra" : "Remíza")}";
            else roundButtons[i].Text = $"{i + 1}. {chapters[i]}\nčeká";
            roundButtons[i].Invalidate();
        }
    }
    private void ConfigureBattleImages()
    {
        using var dialog = new Form { Text = "Bojovník · AI obrázky", ClientSize = new Size(570, 290), BackColor = Theme.Background,
            ForeColor = Theme.Text, Font = Theme.Font(), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false, Padding = new Padding(22), ShowInTaskbar = false };
        var layout = Rows(35, 35, 95, 28, 45);
        layout.Controls.Add(Copy("Vlastní OpenAI API klíč", 14, Theme.Text, true), 0, 0);
        var key = new TextBox { Text = battleApiKey, UseSystemPasswordChar = true, Dock = DockStyle.Fill, BackColor = Theme.Inset, ForeColor = Theme.Text, AccessibleName = "OpenAI API klíč" };
        layout.Controls.Add(key, 0, 1);
        layout.Controls.Add(Copy($"Model: {OpenAiBattleImages.Model} · jeden nový obrázek po každém kole.\nCelý zápas může odeslat až 5 placených požadavků.\nAPI účtuje OpenAI odděleně od virtuální sázky a ChatGPT.\nKlíč zůstane jen v paměti tohoto spuštění; nikam se neukládá.", 10, Theme.Muted), 0, 2);
        var link = new LinkLabel { Text = "Vytvořit API klíč / zkontrolovat API účet", Dock = DockStyle.Fill, LinkColor = Theme.Teal };
        link.LinkClicked += (_, _) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://platform.openai.com/api-keys") { UseShellExecute = true });
        layout.Controls.Add(link, 0, 3);
        var save = Button("Použít pro toto spuštění", "saveBattleKey", true);
        save.Click += (_, _) => { battleApiKey = key.Text.Trim(); dialog.DialogResult = DialogResult.OK; };
        layout.Controls.Add(save, 0, 4);
        dialog.Controls.Add(layout);
        dialog.AcceptButton = save;
        dialog.ShowDialog(this);
    }
    private void ExportWarriorChronicle()
    {
        if (battleGallery.Count == 0 || battleMatch == null) return;
        using var dialog = new SaveFileDialog { Title = "Uložit kroniku Bojovníka", Filter = "Kronika s obrázky (*.zip)|*.zip", FileName = $"Bojovnik-{DateTime.Now:yyyyMMdd-HHmmss}.zip", OverwritePrompt = true };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            using var archive = new ZipArchive(File.Create(dialog.FileName), ZipArchiveMode.Create);
            var text = new StringBuilder($"BOJOVNÍK\nSázka: {battleMatch.Stake:N2}\nHráč: {battleMatch.Player.Describe()}\nPC: {battleMatch.ComputerName} · {battleMatch.Computer.Describe()}\n{battleSettlement.Text}\n\n");
            foreach (var item in battleGallery)
            {
                var entry = archive.CreateEntry($"kolo-{item.Round.Number:00}.png");
                using (var stream = entry.Open()) item.Image.Save(stream, ImageFormat.Png);
                text.AppendLine($"KOLO {item.Round.Number}: {item.Round.Scenario.Name}\n{item.Round.Story}\n{item.Source}\n");
            }
            using var writer = new StreamWriter(archive.CreateEntry("kronika.txt").Open(), Encoding.UTF8);
            writer.Write(text.ToString());
            footer.Text = "Kronika a obrázky byly uloženy do ZIP.";
        }
        catch (IOException) { footer.Text = "Kroniku se nepodařilo uložit. Vyber jinou složku nebo název."; }
        catch (UnauthorizedAccessException) { footer.Text = "Do vybrané složky nemáš právo zápisu."; }
    }
    private void ClearBattleGallery()
    {
        battlePicture.Image = null;
        foreach (var item in battleGallery) item.Image.Dispose();
        battleGallery.Clear();
        selectedRound = -1;
    }
    private void DisposeWarrior()
    {
        battleImageCancellation?.Cancel();
        ClearBattleGallery();
        battleIntro?.Dispose();
        battleIntro = null;
        battleIllustrator?.Dispose();
        battleIllustrator = null;
        battleHttp.Dispose();
        warriorTips.Dispose();
        battleApiKey = "";
    }
}
