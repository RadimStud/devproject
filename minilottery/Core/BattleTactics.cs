namespace MiniLottery.Core;

public enum BattleTactic { Assault, Guard, Feint }
public sealed record TacticInfo(BattleTactic Id, string Name, string Description, string Tell);

public static class BattleTactics
{
    public static IReadOnlyList<TacticInfo> All { get; } = Array.AsReadOnly(new[] {
        new TacticInfo(BattleTactic.Assault, "Nápor", "Přemůže lest. Proti krytu ztrácí výhodu.", "PC přenáší váhu vpřed a zvedá meč. Čekáš nápor."),
        new TacticInfo(BattleTactic.Guard, "Kryt", "Zastaví nápor a sníží zranění o 2. Podléhá lsti.", "PC zpevňuje postoj a stahuje čepel k tělu. Čekáš kryt."),
        new TacticInfo(BattleTactic.Feint, "Lest", "Obejde kryt. Rychlý nápor ji může přerušit.", "PC krouží a naznačuje útok stranou. Čekáš lest.")
    });
    public static TacticInfo Info(BattleTactic tactic) => All[(int)tactic];
    public static BattleTactic Counter(BattleTactic tactic) => tactic switch {
        BattleTactic.Assault => BattleTactic.Guard, BattleTactic.Guard => BattleTactic.Feint, _ => BattleTactic.Assault
    };
    public static int Bonus(BattleTactic chosen, BattleTactic opponent) =>
        chosen == opponent ? 0 : chosen == Counter(opponent) ? 5 : -5;
}
