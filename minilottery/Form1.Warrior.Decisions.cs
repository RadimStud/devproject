using MiniLottery.Core;
using MiniLottery.UI;

namespace WinFormsApp1;

public partial class Form1
{
    private void ApplyWarriorPreset()
    {
        if (runningGame.HasValue || battleBusy) return;
        int[][] presets = {
            new[] { 2, 2, 2, 2, 2, 2 }, new[] { 5, 1, 3, 1, 1, 1 },
            new[] { 1, 5, 1, 3, 1, 1 }, new[] { 1, 1, 3, 1, 5, 1 }
        };
        int index = warriorPreset.SelectedIndex;
        if (index < 0 || index >= presets.Length) return;
        for (int i = 0; i < perkInputs.Length; i++) perkInputs[i].Value = presets[index][i];
    }

    private void RefreshWarriorDecision()
    {
        var match = battleMatch;
        bool active = match?.State == WarriorMatchState.Active;
        for (int i = 0; i < tacticButtons.Length; i++)
        {
            tacticButtons[i].Enabled = active && !battleBusy;
            tacticButtons[i].Active = selectedTactic == (BattleTactic)i;
            tacticButtons[i].Invalidate();
        }
        if (match == null) return;
        if (active)
        {
            nextChapter.Text = $"DALŠÍ KOLO {match.Rounds.Count + 1} / 5 · {match.NextChapter.ToUpperInvariant()}";
            nextScenario.Text = match.NextScenario.Name;
            int basePower = WarriorMatch.Power(match.Player, match.NextScenario, 0, match.PlayerHealth);
            scenarioHint.Text = $"{match.NextScenario.Advantage}\nTvoje síla před hodem a taktikou: {basePower}.";
            if (match.PlayerRally > 0) scenarioHint.Text += " Vzepětí ve finále: +3.";
            if (match.ComputerRally > 0) scenarioHint.Text += " Vzepětí PC ve finále: +3.";
            opponentTell.Text = match.OpponentTell;
            warriorRecap.Text = selectedTactic.HasValue ? BattleTactics.Info(selectedTactic.Value).Description : "Správná protitaktika: ty +5 síly, PC −5. Nápověda může klamat.";
        }
        else
        {
            nextChapter.Text = match.State == WarriorMatchState.Forfeited ? "TURNAJ UKONČEN" : "PĚT KAPITOL ZA TEBOU";
            nextScenario.Text = match.Winner switch { RoundWinner.Player => "Koruna je tvoje", RoundWinner.Computer => "Čas na odvetu", _ => "Souboj bez vítěze" };
            scenarioHint.Text = $"Vyhraná kola: {match.PlayerWins} : {match.ComputerWins}.\nZdraví: ty {match.PlayerHealth}, PC {match.ComputerHealth}.";
            opponentTell.Text = match.State == WarriorMatchState.Forfeited ? "Vzdal jsi zápas. Rezervovaná sázka propadla." :
                match.Winner == RoundWinner.Player ? "Rozhodčí předává korunu. Tvůj soupeř sklání meč na znamení uznání." :
                match.Winner == RoundWinner.Computer ? $"Korunu získává {match.ComputerName}. Projdi kroniku a připrav jinou strategii." : "Oba rytíři odcházejí se ctí. Sázka se vrací a koruna čeká na odvetu.";
            int reads = match.Rounds.Count(r => r.PlayerTacticBonus > 0);
            warriorRecap.Text = $"Úspěšné přečtení soupeře: {reads} / {match.Rounds.Count}.\nUlož kroniku nebo si uprav perky na odvetu.";
        }
    }

    private void ShowBattleStory()
    {
        if (battleBusy || selectedRound < 0 || selectedRound >= battleGallery.Count) return;
        using var dialog = new Form {
            Text = battleTitle.Text, ClientSize = new Size(740, 560), BackColor = Theme.Background,
            StartPosition = FormStartPosition.CenterParent, Padding = new Padding(24), KeyPreview = true, ShowInTaskbar = false
        };
        dialog.Controls.Add(new TextBox {
            Text = battleStory.Text, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical,
            Dock = DockStyle.Fill, BackColor = Theme.Surface, ForeColor = Theme.Text, Font = Theme.Font(12), BorderStyle = BorderStyle.None
        });
        dialog.KeyDown += (_, e) => { if (e.KeyCode == Keys.Escape) dialog.Close(); };
        dialog.ShowDialog(this);
    }

    private void ShowLargeBattleImage()
    {
        if (battlePicture.Image == null || battleBusy) return;
        using var copy = new Bitmap(battlePicture.Image);
        using var preview = new Form {
            Text = battleTitle.Text + " · Esc zavře náhled", StartPosition = FormStartPosition.CenterParent,
            ClientSize = new Size(1200, 760), BackColor = Theme.Background, KeyPreview = true, ShowInTaskbar = false
        };
        preview.Controls.Add(new PictureBox { Image = copy, Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom });
        preview.KeyDown += (_, e) => { if (e.KeyCode == Keys.Escape) preview.Close(); };
        preview.ShowDialog(this);
    }
}
