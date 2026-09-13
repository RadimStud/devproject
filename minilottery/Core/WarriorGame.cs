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
    int PlayerWins, int ComputerWins, string Story, int VisualSeed)
{
    public BattleTactic PlayerTactic { get; init; }
    public BattleTactic ComputerTactic { get; init; }
    public int PlayerTacticBonus { get; init; }
    public int ComputerTacticBonus { get; init; }
    public int PlayerRally { get; init; }
    public int ComputerRally { get; init; }
    public string Chapter { get; init; } = "";
}

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
        new BattleScenario("feint", "Klamný výpad", "Rytíři krouží kolem znaku vytesaného do dlažby. Zdánlivé zaváhání může být pozvánkou do pasti.", "circular stone arena emblem, deceptive sword feint, dramatic sunset", WarriorPerk.Tactics, WarriorPerk.Strength, "vyláká soupeře z postoje a promění klam v zásah"),
        new BattleScenario("crown", "Koruna arény", "Poslední úder zvonu svolá oba rytíře pod prapor šampionů. Rozhodčí zvedá korunu a aréna ztichne před poslední výměnou.", "championship final at the central arena, tall gold banners, brilliant amber spotlights, spectators behind distant battlements", WarriorPerk.Tactics, WarriorPerk.Endurance, "využije poslední rezervy a prosadí rozhodující výpad")
    });
    private readonly Random random;
    private readonly List<BattleRound> rounds = new();
    private readonly BattleScenario[] schedule;
    private bool payoutTaken;
    private BattleTactic plannedComputerTactic;
    public BattleTactic SuspectedComputerTactic { get; private set; }
    public string OpponentTell => BattleTactics.Info(SuspectedComputerTactic).Tell + " Může to být klam.";
    public static IReadOnlyList<string> Chapters { get; } = Array.AsReadOnly(new[] {
        "Vstup do arény", "První zkouška", "Zlom souboje", "Poslední síly", "Koruna arény"
    });
    public string NextChapter => State == WarriorMatchState.Active ? Chapters[rounds.Count] : Chapters[^1];
    public int PlayerRally => State == WarriorMatchState.Active && rounds.Count == 4 && PlayerWins < ComputerWins ? 3 : 0;
    public int ComputerRally => State == WarriorMatchState.Active && rounds.Count == 4 && ComputerWins < PlayerWins ? 3 : 0;
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
        var shuffled = Scenarios.Where(s => s.Id is not "gate" and not "crown").ToArray();
        random.Shuffle(shuffled);
        schedule = new[] { Scenarios[0] }.Concat(shuffled.Take(3)).Append(Scenarios[^1]).ToArray();
        PrepareComputerTactic();
    }

    public BattleRound PlayRound(BattleTactic playerTactic = BattleTactic.Assault)
    {
        if (State != WarriorMatchState.Active) throw new InvalidOperationException("Zápas už skončil.");
        if (!Enum.IsDefined(playerTactic)) throw new ArgumentOutOfRangeException(nameof(playerTactic));
        var scene = NextScenario;
        var computerTactic = plannedComputerTactic;
        string chapter = NextChapter;
        int playerRally = PlayerRally, computerRally = ComputerRally;
        int playerBonus = BattleTactics.Bonus(playerTactic, computerTactic), computerBonus = -playerBonus;
        string continuation = rounds.Count == 0
            ? $"Přicházíš do turnaje O korunu arény. Proti tobě stojí {ComputerName}. K vítězství potřebuješ více vyhraných kol než soupeř."
            : $"Po střetu „{rounds[^1].Scenario.Name}“ pokračuješ se zdravím {PlayerHealth}/100. {ComputerName} má {ComputerHealth}/100. " +
                (rounds[^1].Winner == RoundWinner.Player ? "Soupeř hledá odpověď na tvůj poslední úspěch." : rounds[^1].Winner == RoundWinner.Computer ? "Po ztraceném kole měníš rytmus a hledáš cestu zpět." : "Poslední výměna nerozhodla. Ani jeden nechce ustoupit.");
        int pr = random.Next(1, 21), cr = random.Next(1, 21);
        int pp = Power(Player, scene, pr, PlayerHealth) + playerBonus + playerRally;
        int cp = Power(Computer, scene, cr, ComputerHealth) + computerBonus + computerRally;
        var winner = pp > cp ? RoundWinner.Player : cp > pp ? RoundWinner.Computer : RoundWinner.Draw;
        if (winner == RoundWinner.Player) PlayerWins++;
        if (winner == RoundWinner.Computer) ComputerWins++;
        int pd = Math.Max(1, Damage(winner == RoundWinner.Player, winner == RoundWinner.Draw, Player, Math.Abs(pp - cp)) - (playerTactic == BattleTactic.Guard ? 2 : 0));
        int cd = Math.Max(1, Damage(winner == RoundWinner.Computer, winner == RoundWinner.Draw, Computer, Math.Abs(pp - cp)) - (computerTactic == BattleTactic.Guard ? 2 : 0));
        PlayerHealth = Math.Max(0, PlayerHealth - pd);
        ComputerHealth = Math.Max(0, ComputerHealth - cd);
        string action = winner switch {
            RoundWinner.Player => $"Tvůj rytíř {scene.Action}. {ComputerName} ustupuje a na okamžik klesá na koleno. Kolo získáváš ty.",
            RoundWinner.Computer => $"{ComputerName} {scene.Action}. Tvůj rytíř ustupuje a na okamžik klesá na koleno. Kolo získává PC.",
            _ => "Oba rytíři čtou soupeřův záměr. Čepele se setkají, oba ustoupí do střehu a kolo končí remízou."
        };
        string tactics = $"Volíš {BattleTactics.Info(playerTactic).Name.ToLowerInvariant()}, PC odhaluje {BattleTactics.Info(computerTactic).Name.ToLowerInvariant()}. " +
            (playerBonus > 0 ? "Přečetl jsi soupeře: získáváš +5 k bojové síle, PC −5." : playerBonus < 0 ? "Soupeř vystihl tvůj záměr: získává +5 k bojové síle, ty −5." : "Stejné taktiky nepřinášejí výhodu ani jedné straně.");
        string rally = playerRally > 0 ? "\nPoslední vzepětí: ve finále získáváš +3 k síle jako dotahující hráč." : computerRally > 0 ? "\nPoslední vzepětí: PC ve finále získává +3 k síle jako dotahující soupeř." : "";
        string story = $"{continuation}\n\n{scene.Opening}\n\n{tactics}{rally}\n\n{action}\n\nTy ztrácíš {pd} zdraví (zbývá {PlayerHealth}/100). PC ztrácí {cd} zdraví (zbývá {ComputerHealth}/100).";
        if (rounds.Count == 4) story += "\n\n" + (PlayerWins > ComputerWins ? "Rozhodčí ti předává korunu arény. Soupeř sklání meč na znamení uznání." : ComputerWins > PlayerWins ? $"Korunu získává {ComputerName}. Odcházíš z arény se zkušeností a možností odvety." : "Rozhodčí vyhlašuje remízu. Oba rytíři opouštějí arénu se ctí a sázka se vrací.");
        var round = new BattleRound(rounds.Count + 1, scene, winner, pp, cp, pr, cr, pd, cd,
            PlayerHealth, ComputerHealth, PlayerWins, ComputerWins, story, random.Next()) {
                PlayerTactic = playerTactic, ComputerTactic = computerTactic, PlayerTacticBonus = playerBonus, ComputerTacticBonus = computerBonus,
                PlayerRally = playerRally, ComputerRally = computerRally, Chapter = chapter
            };
        rounds.Add(round);
        if (rounds.Count == RoundCount) State = WarriorMatchState.Completed;
        else PrepareComputerTactic();
        return round;
    }
    private void PrepareComputerTactic()
    {
        // Commit before the next player choice; only past revealed tactics can influence the opponent.
        if (rounds.Count > 0 && random.Next(100) < 35)
            plannedComputerTactic = BattleTactics.Counter(rounds[^1].PlayerTactic);
        else
        {
            int[] weights = { 2 + Computer[WarriorPerk.Strength], 2 + Computer[WarriorPerk.Guard], 2 + Computer[WarriorPerk.Tactics] };
            int pick = random.Next(weights.Sum()), i = 0;
            while (pick >= weights[i]) pick -= weights[i++];
            plannedComputerTactic = (BattleTactic)i;
        }
        SuspectedComputerTactic = random.Next(100) < 70 ? plannedComputerTactic : (BattleTactic)(((int)plannedComputerTactic + random.Next(1, 3)) % 3);
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
