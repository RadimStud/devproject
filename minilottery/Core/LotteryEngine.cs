namespace MiniLottery.Core;

public readonly record struct Draw(int Round, int First, int Second, int Third)
{
    public override string ToString() => $"{Round,7:N0}     {First}  ·  {Second}  ·  {Third}";
}

// One session owns its random draws and a bounded display history. No UI or money mutation.
public sealed class LotterySession
{
    private readonly Random random;
    private readonly int[] target;
    private readonly Queue<Draw> history = new();
    public const int HistoryLimit = 100;
    public int Rounds { get; private set; }
    public int[] Frequencies { get; } = new int[10];
    public Draw LastDraw { get; private set; }
    public bool Complete { get; private set; }
    public IReadOnlyCollection<Draw> History => history;

    public LotterySession(IEnumerable<int> numbers, Random random)
    {
        target = numbers.ToArray();
        if (target.Length != 3 || target.Any(n => n < 0 || n > 9))
            throw new ArgumentException("Choose exactly three digits from 0 to 9.", nameof(numbers));
        this.random = random;
    }

    public void Advance(int count)
    {
        if (count < 1) throw new ArgumentOutOfRangeException(nameof(count));
        for (int i = 0; i < count && !Complete; i++)
        {
            var draw = new Draw(++Rounds, random.Next(10), random.Next(10), random.Next(10));
            LastDraw = draw;
            Frequencies[draw.First]++;
            Frequencies[draw.Second]++;
            Frequencies[draw.Third]++;
            history.Enqueue(draw);
            if (history.Count > HistoryLimit) history.Dequeue();
            Complete = draw.First == target[0] && draw.Second == target[1] && draw.Third == target[2];
        }
    }
}

public static class GameRules
{
    public const int LuckyWinThreshold = 693;
    public static bool LuckyWins(int rounds) => rounds > 0 && rounds < LuckyWinThreshold;
    public static bool BonusWins(int rounds, bool even) => (rounds % 2 == 0) == even;
    // The original game awards ties to the dealer.
    public static bool PlayerWins(int player, int dealer) => player > dealer;
    public static decimal NetChange(bool won, decimal stake) => won ? stake : -stake;
    public static bool CanPlay(decimal credit, decimal stake) => stake > 0 && credit >= stake;
    public static double Median(IEnumerable<int> rounds)
    {
        var values = rounds.OrderBy(n => n).ToArray();
        int n = values.Length;
        return n == 0 ? 0 : n % 2 == 1 ? values[n / 2] : ((double)values[n / 2 - 1] + values[n / 2]) / 2;
    }
}

public sealed class GameStats
{
    public int Games { get; private set; }
    public decimal Net { get; private set; }
    public decimal HighestNet { get; private set; }
    public decimal LowestNet { get; private set; }
    public void Record(decimal change)
    {
        Games++;
        Net += change;
        HighestNet = Math.Max(HighestNet, Net);
        LowestNet = Math.Min(LowestNet, Net);
    }
}
