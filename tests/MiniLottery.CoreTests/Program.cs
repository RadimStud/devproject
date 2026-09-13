using MiniLottery.Core;

int checks = 0;
void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
    checks++;
}
Check(GameRules.LuckyWins(1), "First round wins");
Check(GameRules.LuckyWins(692), "692 wins");
Check(!GameRules.LuckyWins(693), "693 loses");
Check(!GameRules.LuckyWins(0), "Incomplete round cannot win");
Check(GameRules.BonusWins(8, true) && !GameRules.BonusWins(8, false), "Even selection");
Check(GameRules.BonusWins(9, false) && !GameRules.BonusWins(9, true), "Odd selection");
Check(!GameRules.PlayerWins(10, 10), "Ties go to dealer");
Check(GameRules.PlayerWins(1000, 999) && !GameRules.PlayerWins(1, 2), "Duel comparison");
Check(GameRules.NetChange(true, .25m) == .25m && GameRules.NetChange(false, .25m) == -.25m, "Decimal stakes");
Check(GameRules.CanPlay(1, 1) && !GameRules.CanPlay(.5m, 1) && !GameRules.CanPlay(10, 0) && !GameRules.CanPlay(10, -1), "Stake validity");
Check(GameRules.Median(Array.Empty<int>()) == 0 && GameRules.Median(new[] { 9, 1, 5 }) == 5 && GameRules.Median(new[] { 1, 2 }) == 1.5, "Medians");
var stats = new GameStats();
stats.Record(.25m); stats.Record(-.5m); stats.Record(.1m);
Check(stats.Games == 3 && stats.Net == -.15m && stats.HighestNet == .25m && stats.LowestNet == -.25m, "Extrema recorded after settlement without truncation");
for (int seed = 0; seed < 50; seed++)
{
    var target = new[] { seed % 10, 2, 9 };
    var simulation = new LotterySession(target, new Random(seed));
    while (!simulation.Complete && simulation.Rounds < 100000) simulation.Advance(64);
    Check(simulation.Complete, "Seeded draw terminates");
    Check(simulation.LastDraw.First == target[0] && simulation.LastDraw.Second == 2 && simulation.LastDraw.Third == 9, "Exact ordered match");
    Check(simulation.Frequencies.Sum() == simulation.Rounds * 3, "Frequencies include every draw");
    Check(simulation.History.Count == Math.Min(simulation.Rounds, LotterySession.HistoryLimit), "History bounded");
    Check(simulation.History.Last() == simulation.LastDraw, "Final match in history");
    int rounds = simulation.Rounds;
    simulation.Advance(64);
    Check(rounds == simulation.Rounds, "Completed sessions never draw again");
}
try { new LotterySession(new[] { 0, 10, 2 }, new Random(0)); throw new Exception("Accepted invalid digit"); }
catch (ArgumentException) { checks++; }
Console.WriteLine($"PASS: {checks} checks (rules, balance, seeded simulations, history and frequencies).");
