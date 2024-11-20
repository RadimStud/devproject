using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection.Emit;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinFormsApp1
{
    public partial class Form1 : Form
    {
        private List<int> mostCommonNumbers = new List<int>();
        private List<int> button1Rounds = new List<int>(); // Uchovává hodnoty počtu kol ze hry button1
        private int pocetKolButton1;
        private int pocetKolButton2;
        private string nejmensiKombinace = "";
        private int nejmensiPocetKol = int.MaxValue;
        private int maxPocetKol = 0; // Proměnná pro maximální počet kol
        private Random random = new Random();
        private bool isButton5LoopRunning = false; // Indikátor běhu smyčky pro button5
        private bool isButton4LoopRunning = false; // Indikátor běhu smyčky pro button6
        private bool isButton6LoopRunning = false; // Indikátor běhu smyčky pro button6
        private List<int> button6Rounds = new List<int>(); // Uchovává počet kol ze všech spuštění button1 v loopu button6
        private int nejvetsiVyhra1 = 0;
        private int nejvetsiProhra1 = 0;
        private int nejvetsiVyhra2 = 0;
        private int nejvetsiProhra2 = 0;
        private int nejvetsiVyhra3 = 0;
        private int nejvetsiProhra3 = 0;
        // Nové proměnné pro celkovou výhru/prohru
        private decimal totalWinLossButton1 = 0;
        private decimal totalWinLossButton2 = 0;
        private decimal totalWinLossButton3 = 0;

        public Form1()
        {
            InitializeComponent();
        }

        private Color GetRandomColor()
        {
            return Color.FromArgb(random.Next(256), random.Next(256), random.Next(256));
        }

        private void UpdateProbabilityDistribution(List<int> generatedNumbers)
        {
            var distribution = Enumerable.Range(0, 10)
                .Select(n => (Number: n, Probability: (double)generatedNumbers.Count(x => x == n) / generatedNumbers.Count * 100))
                .ToList();

            label5.Text = "Probability distribution: " + string.Join(", ", distribution.Select(d => $"{d.Number}: {d.Probability:F2}%"));

            double maxDifference = CalculateMaxProbabilityDifference(distribution);
            label6.Text = $"Probability dif.: {maxDifference:F2}%";
        }

        private double CalculateMaxProbabilityDifference(List<(int Number, double Probability)> distribution)
        {
            if (distribution.Count == 0) return 0;

            double maxProbability = distribution.Max(d => d.Probability);
            double minProbability = distribution.Min(d => d.Probability);

            return maxProbability - minProbability;
        }

        private int button1ClickCount = 0; // Počet kliknutí na button1

        private void button1_Click(object sender, EventArgs e)
        {
            button1ClickCount++; // Zvýšení počítadla kliknutí
            label16.Text = $"Click Count: {button1ClickCount}"; // Zobrazení počtu kliknutí v label16

            button1.BackColor = GetRandomColor();

            if (!int.TryParse(textBox1.Text, out int vstup1) ||
                !int.TryParse(textBox2.Text, out int vstup2) ||
                !int.TryParse(textBox3.Text, out int vstup3) ||
                vstup1 < 0 || vstup1 > 9 ||
                vstup2 < 0 || vstup2 > 9 ||
                vstup3 < 0 || vstup3 > 9)
            {
                MessageBox.Show("Enter valid numbers from 0 to 9 in all three fields.");
                return;
            }

            Random rand = new Random();
            pocetKolButton1 = 0;
            bool shoda = false;
            List<int> generatedNumbers = new List<int>();

            listBox1.Items.Clear();

            while (!shoda)
            {
                pocetKolButton1++;
                int nahodneCislo1 = rand.Next(0, 10);
                int nahodneCislo2 = rand.Next(0, 10);
                int nahodneCislo3 = rand.Next(0, 10);

                generatedNumbers.Add(nahodneCislo1);
                generatedNumbers.Add(nahodneCislo2);
                generatedNumbers.Add(nahodneCislo3);

                listBox1.Items.Add($"Round {pocetKolButton1}: {nahodneCislo1}, {nahodneCislo2}, {nahodneCislo3}");

                Application.DoEvents();

                if (nahodneCislo1 == vstup1 && nahodneCislo2 == vstup2 && nahodneCislo3 == vstup3)
                {
                    shoda = true;
                }
            }

            label1.Text = $"Number of rounds: {pocetKolButton1}";

            if (pocetKolButton1 < nejmensiPocetKol)
            {
                nejmensiPocetKol = pocetKolButton1;
                nejmensiKombinace = $"{vstup1}, {vstup2}, {vstup3}";
            }

            if (nejvetsiVyhra1 < totalWinLossButton1)
            {
                nejvetsiVyhra1 = (int)totalWinLossButton1;
            }

            if (nejvetsiProhra1 > totalWinLossButton1)
            {
                nejvetsiProhra1 = (int)totalWinLossButton1;
            }

            label14.Text = $"Best Round Combination: {nejmensiKombinace} (Rounds: {nejmensiPocetKol})";
            label24.Text = $"Best Round Combination: {nejvetsiVyhra1} Loose: {nejvetsiProhra1}";

            mostCommonNumbers = generatedNumbers
                .GroupBy(n => n)
                .OrderByDescending(g => g.Count())
                .Take(3)
                .Select(g => g.Key)
                .ToList();

            label2.Text = $"Most frequent numbers: {string.Join(", ", mostCommonNumbers.Select((n, i) => $"{n} ({generatedNumbers.Count(x => x == n)}x)"))}";

            UpdateProbabilityDistribution(generatedNumbers);

            UpdateKreditAfterGame(pocetKolButton1);

            // Uložení počtu kol do seznamu a výpočet mediánu
            button1Rounds.Add(pocetKolButton1);
            double medianRounds = CalculateMedian(button1Rounds);
            label13.Text = $"Median Rounds: {medianRounds:F2}";

            // Aktualizace maximálního počtu kol v label19
            UpdateMaxPocetKol(pocetKolButton1);

            // Uložení celkové výhry/prohry pro button1 do label21
            UpdateTotalWinLossButton1(pocetKolButton1);
        }

        private void UpdateMaxPocetKol(int currentRound)
        {
            if (currentRound > maxPocetKol)
            {
                maxPocetKol = currentRound;
                label19.Text = $"Max Rounds: {maxPocetKol}";
            }
        }

        private void UpdateKreditAfterGame(int pocetKol)
        {
            if (decimal.TryParse(textBox5.Text, out decimal vklad) && decimal.TryParse(textBox4.Text, out decimal kredit))
            {
                kredit -= vklad;

                if (pocetKol < 693)
                {
                    kredit += vklad * 2;
                    label4.Text = $"Výhra! Kredit zvýšen o {vklad}.";
                }
                else
                {
                    label4.Text = $"Prohra! Kredit snížen o {vklad}.";
                }

                textBox4.Text = kredit.ToString("0.##");
            }
            else
            {
                MessageBox.Show("Enter valid numeric values in the deposit and credit fields.");
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            button2.BackColor = GetRandomColor();

            int vstup1, vstup2, vstup3;

            if (mostCommonNumbers.Count < 3)
            {
                // Pokud seznam nemá dostatek čísel, použij výchozí hodnoty
                vstup1 = 1;
                vstup2 = 1;
                vstup3 = 1;

                // Možnost upozornit uživatele (nepovinné)
                // MessageBox.Show("Most frequent numbers are not available. Start the first competition using the game button.");
            }
            else
            {
                // Pokud seznam obsahuje alespoň 3 čísla, použij první tři
                vstup1 = mostCommonNumbers[0];
                vstup2 = mostCommonNumbers[1];
                vstup3 = mostCommonNumbers[2];
            }

            // Zobrazení hodnot
            label26.Text = $"{vstup1} {vstup2} {vstup3}";

            Random rand = new Random();
            pocetKolButton2 = 0;
            bool shoda = false;
            List<int> generatedNumbers = new List<int>();

            listBox2.Items.Clear();

            while (!shoda)
            {
                pocetKolButton2++;
                int nahodneCislo1 = rand.Next(0, 10);
                int nahodneCislo2 = rand.Next(0, 10);
                int nahodneCislo3 = rand.Next(0, 10);

                generatedNumbers.Add(nahodneCislo1);
                generatedNumbers.Add(nahodneCislo2);
                generatedNumbers.Add(nahodneCislo3);

                listBox2.Items.Add($"Round {pocetKolButton2}: {nahodneCislo1}, {nahodneCislo2}, {nahodneCislo3}");

 

                Application.DoEvents();

                if (nahodneCislo1 == vstup1 && nahodneCislo2 == vstup2 && nahodneCislo3 == vstup3)
                {
                    shoda = true;
                }
            }

            label3.Text = $"Number of rounds (Bonus): {pocetKolButton2}";

            if (decimal.TryParse(textBox5.Text, out decimal vklad) && decimal.TryParse(textBox4.Text, out decimal kredit))
            {
                bool isEven = pocetKolButton2 % 2 == 0;
                bool vyhra = checkBox1.Checked ? isEven : !isEven;

                if (vyhra)
                {
                    kredit += vklad;
                    label12.Text = $"Výhra! Kredit zvýšen o {vklad}.";
                }
                else
                {
                    kredit -= vklad;
                    label12.Text = $"Prohra! Kredit snížen o {vklad}.";
                }

                textBox4.Text = kredit.ToString("0.##");
            }
            else
            {
                MessageBox.Show("Enter valid numeric values in the deposit and credit fields.");
            }

            // Uložení celkové výhry/prohry pro button2 do label22
            UpdateTotalWinLossButton2(pocetKolButton2);

            if (nejvetsiProhra2 > totalWinLossButton2)
            {
                nejvetsiProhra2 = (int)totalWinLossButton2;
            }

            if (nejvetsiVyhra2 < totalWinLossButton2)
            {
                nejvetsiVyhra2 = (int)totalWinLossButton2;
            }

            label25.Text = $"Win Max: {nejvetsiVyhra2} Loose Max: {nejvetsiProhra2}";
        }

        private async void button3_Click(object sender, EventArgs e)
        {
            button3.BackColor = GetRandomColor();

            int dealerValue = random.Next(1, 1001);
            label18.Text = $"Dealer: {dealerValue}";

            int playerValue = random.Next(1, 1001);
            label10.Text = $"Player: {playerValue}";

            if (decimal.TryParse(textBox5.Text, out decimal vklad) && decimal.TryParse(textBox4.Text, out decimal kredit))
            {
                if (playerValue > dealerValue)
                {
                    kredit += vklad;
                    label17.Text = "Result: Player wins!";
                }
                else
                {
                    kredit -= vklad;
                    label17.Text = "Result: Dealer wins!";
                }

                textBox4.Text = kredit.ToString("0.##");
            }
            else
            {
                MessageBox.Show("Enter valid numeric values in the deposit and credit fields.");
            }

            // Uložení celkové výhry/prohry pro button3 do label20
            UpdateTotalWinLossButton3(playerValue, dealerValue);

            if (nejvetsiProhra3 > totalWinLossButton3)
            {
                nejvetsiProhra3 = (int)totalWinLossButton3;
            }

            if (nejvetsiVyhra3 < totalWinLossButton3)
            {
                nejvetsiVyhra3 = (int)totalWinLossButton3;
            }

            label23.Text = $"Win Max: {nejvetsiVyhra3} Loose Max: {nejvetsiProhra3}";
        }

        private void UpdateTotalWinLossButton1(int pocetKol)
        {
            if (decimal.TryParse(textBox5.Text, out decimal vklad))
            {
                if (pocetKol < 693)
                    totalWinLossButton1 += vklad;
                else
                    totalWinLossButton1 -= vklad;

                label21.Text = $"Total Win/Loss (Button1): {totalWinLossButton1}";
            }
        }

        private void UpdateTotalWinLossButton2(int pocetKol)
        {
            if (decimal.TryParse(textBox5.Text, out decimal vklad))
            {
                bool isEven = pocetKol % 2 == 0;
                bool vyhra = checkBox1.Checked ? isEven : !isEven;

                if (vyhra)
                    totalWinLossButton2 += vklad;
                else
                    totalWinLossButton2 -= vklad;

                label22.Text = $"Total Win/Loss (Button2): {totalWinLossButton2}";
            }
        }

        private void UpdateTotalWinLossButton3(int playerValue, int dealerValue)
        {
            if (decimal.TryParse(textBox5.Text, out decimal vklad))
            {
                if (playerValue > dealerValue)
                    totalWinLossButton3 += vklad;
                else
                    totalWinLossButton3 -= vklad;

                label20.Text = $"Total Win/Loss (Button3): {totalWinLossButton3}";
            }
        }

        private async void button4_Click(object sender, EventArgs e)
        {
            if (isButton4LoopRunning)
            {
                isButton4LoopRunning = false;
                button4.Text = "Start Loop";
                return;
            }

            isButton4LoopRunning = true;
            button4.Text = "Stop Loop";

            while (isButton4LoopRunning)
            {
                button3_Click(sender, e);
                await Task.Delay(1000);
            }
        }

        private async void button5_Click(object sender, EventArgs e)
        {
            if (isButton5LoopRunning)
            {
                isButton5LoopRunning = false;
                button5.Text = "Start Button1 Loop";
                return;
            }

            isButton5LoopRunning = true;
            button5.Text = "Stop Button1 Loop";

            int startRoundCount = button1ClickCount;

            while (isButton5LoopRunning)
            {
                button1_Click(sender, e);
                await Task.Delay(1000);
            }

            int rounds = button1ClickCount - startRoundCount;
        }

        private double CalculateMedian(List<int> values)
        {
            if (values == null || values.Count == 0)
                return 0;

            var sortedValues = values.OrderBy(x => x).ToList();
            int count = sortedValues.Count;

            if (count % 2 == 0)
            {
                return (sortedValues[count / 2 - 1] + sortedValues[count / 2]) / 2.0;
            }
            else
            {
                return sortedValues[count / 2];
            }
        }

        private async void button6_Click(object sender, EventArgs e)
        {
            if (isButton6LoopRunning)
            {
                isButton6LoopRunning = false;
                button6.Text = "Start Loop";
                return;
            }

            isButton6LoopRunning = true;
            button6.Text = "Stop Loop";

            while (isButton6LoopRunning)
            {
                button2_Click(sender, e);
                await Task.Delay(1000);
            }
        }
    }
}
