using MiniLottery.Core;

namespace MiniLottery.Warrior;

public static class BattleImagePrompt
{
    public static string Create(WarriorMatch match, BattleRound round) => $"""
        Create one premium cinematic, semi-realistic painterly fantasy game illustration, landscape composition.
        Same recurring characters throughout all five rounds: PLAYER on the LEFT, an adult human knight in
        brushed silver plate armor, teal embroidered surcoat and flowing teal cape, closed helmet with teal visor,
        straight silver sword. COMPUTER on the RIGHT, an adult human knight in blackened iron armor,
        crimson surcoat and red cape, closed helmet with red visor, straight steel sword.
        Keep those identities, clothing colors and left/right positions consistent. Both full bodies clearly visible.
        Scene: a ruined medieval tournament arena with a stone arch gate, columns, a staircase, braziers and distant mountains.
        This round's environment and action: {round.Scenario.Environment}.
        Round {round.Number} of 5. Exact outcome: {round.Winner switch {
            RoundWinner.Player => "The teal player knight wins the exchange, advancing with sword extended. The crimson computer knight is pushed back, kneeling but alive.",
            RoundWinner.Computer => "The crimson computer knight wins the exchange, advancing with sword extended. The teal player knight is pushed back, kneeling but alive.",
            _ => "A tied exchange. Both knights stand in a guarded stance facing each other, neither is victorious." }}
        Exact Czech battle story to illustrate: {round.Story}
        Player remaining health: {round.PlayerHealth}/100. Computer remaining health: {round.ComputerHealth}/100.
        Show fatigue through posture and light armor wear, never through graphic injuries. Neither knight dies.
        Player traits: {match.Player.Describe()}. Computer traits: {match.Computer.Describe()}.
        Previous score is player {round.PlayerWins}, computer {round.ComputerWins}; the image must depict this
        round's outcome, not invent a different winner based on the cumulative score.
        Rich environmental detail, realistic steel and cloth, dramatic teal shadows and amber firelight.
        No text, no numbers, no UI, no labels, no watermark, no gore.
        """;
}
