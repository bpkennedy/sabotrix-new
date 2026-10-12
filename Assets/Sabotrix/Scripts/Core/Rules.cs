namespace Sabotrix.Core
{
    // Match-wide rule values from GDD Sections 7 and 9.
    public static class Rules
    {
        public const int StartingHandSize = 5;
        public const int MaxHandSize = 7;
        public const int MaxSabotagesPerMatch = 3;
        public const int TurnLimit = 16;
        public const int SpecialEnergyMax = 6;

        // Always available, never in a deck. Only usable when no Attack card is affordable.
        public static readonly CardDefinition BasicAttack =
            new CardDefinition("basic-attack", "Basic Attack", CardType.Attack, 0, 5);
    }
}
