namespace Sabotrix.Core
{
    // The printed card: what every copy of a card has in common.
    public sealed class CardDefinition
    {
        public string Id { get; }
        public string Name { get; }
        public CardType Type { get; }

        // Energy for Attack and Sabotage cards, Special Energy for Ability cards.
        public int Cost { get; }

        public int Damage { get; }

        public CardDefinition(string id, string name, CardType type, int cost, int damage)
        {
            Id = id;
            Name = name;
            Type = type;
            Cost = cost;
            Damage = damage;
        }
    }
}
