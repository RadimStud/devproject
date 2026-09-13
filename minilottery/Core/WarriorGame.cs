namespace MiniLottery.Core;

public enum WarriorPerk { Strength, Guard, Agility, Endurance, Tactics, Luck }
public enum RoundWinner { Draw, Player, Computer }
public enum WarriorMatchState { Active, Completed, Forfeited }

public sealed record PerkInfo(WarriorPerk Id, string Name, string Description);
public sealed class WarriorBuild
{
    public const int Budget = 12;
    public const int Maximum = 5;
    public static IReadOnlyList<PerkInfo> Perks { get; } = Array.AsReadOnly(new[] {
        new PerkInfo(WarriorPerk.Strength, "Síla", "Průrazné výpady a boj zblízka."),
        new PerkInfo(WarriorPerk.Guard, "Obrana", "Krytí útoků a menší zranění."),
        new PerkInfo(WarriorPerk.Agility, "Obratnost", "Úhyby, déšť a nerovný terén."),
        new PerkInfo(WarriorPerk.Endurance, "Odolnost", "Menší vliv únavy a delší střety."),
        new PerkInfo(WarriorPerk.Tactics, "Taktika", "Léčky a využití prostředí."),
        new PerkInfo(WarriorPerk.Luck, "Štěstí", "Drobný bonus k hodu v každém kole.")
    });
    private readonly int[] points;
    public int this[WarriorPerk perk] => points[(int)perk];
    public WarriorBuild(IEnumerable<int> values)
    {
        points = values.ToArray();
        if (points.Length != 6 || points.Sum() != Budget || points.Any(n => n < 0 || n > Maximum))
            throw new ArgumentException("Rozdělte přesně 12 bodů. Každý perk může mít 0 až 5 bodů.");
    }
    public static WarriorBuild CreateComputer(Random random)
    {
        var values = new int[6];
        for (int n = 0; n < Budget; n++)
        {
            var available = Enumerable.Range(0, 6).Where(i => values[i] < Maximum).ToArray();
            values[available[random.Next(available.Length)]]++;
        }
        return new WarriorBuild(values);
    }
    public string Describe() => string.Join(" · ", Perks.Select(p => $"{p.Name} {this[p.Id]}"));
}

public sealed record BattleScenario(string Id, string Name, string Opening, string Environment,
    WarriorPerk Primary, WarriorPerk Secondary, string Action)
{
    public string Advantage => $"Rozhoduje {WarriorBuild.Perks[(int)Primary].Name.ToLowerInvariant()} a {WarriorBuild.Perks[(int)Secondary].Name.ToLowerInvariant()}.";
}

public sealed record BattleRound(int Number, BattleScenario Scenario, RoundWinner Winner,
    int PlayerPower, int ComputerPower, int PlayerRoll, int ComputerRoll,
    int PlayerDamage, int ComputerDamage, int PlayerHealth, int ComputerHealth,
    int PlayerWins, int ComputerWins, string Story, int VisualSeed);

public sealed class WarriorMatch
{
    public const int RoundCount = 5;
    public static IReadOnlyList<BattleScenario> Scenarios { get; } = Array.AsReadOnly(new[] {
        new BattleScenario("gate", "Ocel u rozbité brány", "Oba rytíři se střetávají pod kamenným obloukem. Prostor pro ústup se rychle zmenšuje.", "broken stone gate, close melee, sparks from clashing swords", WarriorPerk.Strength, WarriorPerk.Guard, "proráží soupeřův kryt prudkým výpadem"),
        new BattleScenario("rain", "Souboj v lijáku", "Arénu zasáhne déšť. Mokré dlaždice kloužou a každý chybný krok nabízí soupeři šanci.", "heavy rain, wet slippery flagstones, mist and splashes", WarriorPerk.Agility, WarriorPerk.Endurance, "uhýbá na mokrém kameni a vrací přesný úder"),
        new BattleScenario("arrows", "Pod palbou šípů", "Z ochozu přichází salva cvičných šípů. Rytíři hledají kryt a současně hlídají jeden druhého.", "arrows falling from the battlements, defensive posture near columns", WarriorPerk.Guard, WarriorPerk.Tactics, "využívá kryt sloupu a načasuje protiútok"),
        new BattleScenario("shadow", "Léčka mezi sloupy", "Mlha zakryje zničenou kolonádu. Siluety mizí ve stínu a přímý výpad se mění v hru o pozici.", "dense teal fog, ruined dark columns, ambush and silhouettes", WarriorPerk.Tactics, WarriorPerk.Agility, "prohlédne léčku a překvapí soupeře z boku"),
        new BattleScenario("fire", "Kruh z popela", "Vítr rozfouká ohniště a po zemi se rozběhnou jiskry. Žár nutí oba bojovníky zkrátit vzdálenost.", "orange braziers, flying embers, smoke, fire at the edges of the arena", WarriorPerk.Endurance, WarriorPerk.Strength, "ustojí žár a vytlačí soupeře ze středu arény"),
        new BattleScenario("stairs", "Výpad na schodišti", "Souboj se přesouvá ke schodišti. Vyšší stupeň nabízí výhodu, ale kamenné hrany komplikují pohyb.", "broken stone stairs, uneven terrain, elevated sword duel", WarriorPerk.Agility, WarriorPerk.Strength, "získává vyšší pozici a vede úder shora"),
        new BattleScenario("attrition", "Zkouška výdrže", "Dlouhá výměna úderů prověří ruce i dech. Rozhodne, kdo si udrží pevný kryt pod tlakem.", "dusty central arena, prolonged exhausting sword exchange", WarriorPerk.Endurance, WarriorPerk.Guard, "udrží tempo a zlomí soupeřův obranný rytmus"),
        new BattleScenario("feint", "Klamný výpad", "Rytíři krouží kolem znaku vytesaného do dlažby. Zdánlivé zaváhání může být pozvánkou do pasti.", "circular stone arena emblem, deceptive sword feint, dramatic sunset", WarriorPerk.Tactics, WarriorPerk.Strength, "vyláká soupeře z postoje a promění klam v zásah")
    });
    private readonly Random random;
    private readonly List<BattleRound> rounds = new();
    private readonly BattleScenario[] schedule;
    private bool payoutTaken;
    public WarriorBuild Player { get; }
    public WarriorBuild Computer { get; }
    public string ComputerName { get; }
    public decimal Stake { get; }
    public int PlayerHealth { get; private set; } = 100;
    public int ComputerHealth { get; private set; } = 100;
    public int PlayerWins { get; private set; }
    public int ComputerWins { get; private set; }
    public WarriorMatchState State { get; private set; } = WarriorMatchState.Active;
    public IReadOnlyList<BattleRound> Rounds => rounds.AsReadOnly();
    public RoundWinner Winner => State == WarriorMatchState.Forfeited ? RoundWinner.Computer :
        PlayerWins > ComputerWins ? RoundWinner.Player : ComputerWins > PlayerWins ? RoundWinner.Computer : RoundWinner.Draw;
    public BattleScenario NextScenario => State == WarriorMatchState.Active ? schedule[rounds.Count] : throw new InvalidOperationException("Zápas už skončil.");

    public WarriorMatch(WarriorBuild player, decimal stake, Random random)
    {
        if (stake <= 0 || decimal.Round(stake, 2) != stake || stake > 1000000)
            throw new ArgumentOutOfRangeException(nameof(stake), "Sázka musí být 0,01 až 1 000 000, nejvýše se dvěma desetinnými místy.");
        Player = player;
        Stake = stake;
        this.random = random;
        // Computer sees no player perk values: its budget is assigned independently.
        Computer = WarriorBuild.CreateComputer(random);
        string[] names = { "Železný havran", "Strážce popela", "Rudý tesák", "Rytíř soumraku", "Černý sokol" };
        ComputerName = names[random.Next(names.Length)];
        var shuffled = Scenarios.ToArray();
        random.Shuffle(shuffled);
        schedule = shuffled.Take(RoundCount).ToArray();
    }

    public BattleRound PlayRound()
    {
        if (State != WarriorMatchState.Active) throw new InvalidOperationException("Zápas už skončil.");
        var scene = NextScenario;
        int pr = random.Next(1, 21), cr = random.Next(1, 21);
        int pp = Power(Player, scene, pr, PlayerHealth), cp = Power(Computer, scene, cr, ComputerHealth);
        var winner = pp > cp ? RoundWinner.Player : cp > pp ? RoundWinner.Computer : RoundWinner.Draw;
        if (winner == RoundWinner.Player) PlayerWins++;
        if (winner == RoundWinner.Computer) ComputerWins++;
        int pd = Damage(winner == RoundWinner.Player, winner == RoundWinner.Draw, Player, Math.Abs(pp - cp));
        int cd = Damage(winner == RoundWinner.Computer, winner == RoundWinner.Draw, Computer, Math.Abs(pp - cp));
        PlayerHealth = Math.Max(0, PlayerHealth - pd);
        ComputerHealth = Math.Max(0, ComputerHealth - cd);
        string action = winner switch {
            RoundWinner.Player => $"Tvůj rytíř {scene.Action}. {ComputerName} ustupuje a na okamžik klesá na koleno. Kolo získáváš ty.",
            RoundWinner.Computer => $"{ComputerName} {scene.Action}. Tvůj rytíř ustupuje a na okamžik klesá na koleno. Kolo získává PC.",
            _ => "Oba rytíři čtou soupeřův záměr. Čepele se setkají, oba ustoupí do střehu a kolo končí remízou."
        };
        string story = $"{scene.Opening}\n\n{action}\n\nTy ztrácíš {pd} zdraví (zbývá {PlayerHealth}/100). PC ztrácí {cd} zdraví (zbývá {ComputerHealth}/100).";
        var round = new BattleRound(rounds.Count + 1, scene, winner, pp, cp, pr, cr, pd, cd,
            PlayerHealth, ComputerHealth, PlayerWins, ComputerWins, story, random.Next());
        rounds.Add(round);
        if (rounds.Count == RoundCount) State = WarriorMatchState.Completed;
        return round;
    }
    public static int Power(WarriorBuild build, BattleScenario scene, int roll, int health) =>
        3 * build[scene.Primary] + 2 * build[scene.Secondary] + build[WarriorPerk.Luck] + roll -
        Math.Max(0, (100 - health) / 12 - build[WarriorPerk.Endurance]);
    private static int Damage(bool won, bool draw, WarriorBuild build, int gap) =>
        Math.Max(1, (draw ? 8 : won ? 5 : 12 + Math.Min(8, gap / 3)) - build[WarriorPerk.Guard]);
    public void Forfeit()
    {
        if (State != WarriorMatchState.Active) return;
        State = WarriorMatchState.Forfeited;
    }
    // Stake is reserved by the wallet at match start; pay out once, only after the match ends.
    public decimal TakePayout()
    {
        if (State == WarriorMatchState.Active) throw new InvalidOperationException("Nejdřív dokončete všech pět kol.");
        if (payoutTaken) throw new InvalidOperationException("Sázka už byla vyúčtována.");
        payoutTaken = true;
        return Winner == RoundWinner.Player ? Stake * 2 : Winner == RoundWinner.Draw ? Stake : 0;
    }
}
