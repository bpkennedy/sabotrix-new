namespace Sabotrix.Core
{
    public enum MatchEventKind
    {
        CardPlayed,
        Damage,
        Heal,
        BlockGained,
        SabotageLanded,
        SabotageBackfired
    }

    // Something that happened while a turn resolved. The screen turns these into the resolve log,
    // and later into animations and room effects.
    public sealed class MatchEvent
    {
        public MatchEventKind Kind { get; }

        // Who it happened to: the player who played the card, or the target who took damage.
        public PlayerState Player { get; }

        public CardDefinition Card { get; }

        // HP lost, HP healed, or Block gained.
        public int Amount { get; }

        // Damage soaked up by Block (Damage events only).
        public int Blocked { get; }

        public MatchEvent(MatchEventKind kind, PlayerState player, CardDefinition card = null, int amount = 0, int blocked = 0)
        {
            Kind = kind;
            Player = player;
            Card = card;
            Amount = amount;
            Blocked = blocked;
        }
    }
}
