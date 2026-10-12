namespace Sabotrix.Core
{
    // The printed card: what every copy of a card has in common.
    // Effects are data, not code, so card numbers can be tuned without programming.
    public sealed class CardDefinition
    {
        public string Id { get; }
        public string Name { get; }
        public CardType Type { get; }

        // Energy for Attack and Sabotage cards, Special Energy for Ability cards.
        public int Cost { get; }

        // Attack: damage per hit, and how many hits.
        public int Damage { get; }
        public int Hits { get; }

        // Extra damage when the opponent's HP is at or below the threshold (Big Finish).
        public int FinisherBonus { get; }
        public int FinisherHpThreshold { get; }

        // Extra Special Energy when this Attack deals damage (Poke).
        public int BonusSpecialEnergy { get; }

        // Gained or healed by the player when the card resolves.
        public int Block { get; }
        public int Heal { get; }

        // Ability: the next Attack this turn deals double damage (Haymaker).
        public bool DoublesNextAttack { get; }

        // Sabotage: the effect, its size, the chance to backfire (0 to 1), and the effect's size on a backfire.
        public SabotageKind Sabotage { get; }
        public int SabotageAmount { get; }
        public double ReversalChance { get; }
        public int ReversalAmount { get; }

        public CardDefinition(
            string id,
            string name,
            CardType type,
            int cost,
            int damage = 0,
            int hits = 1,
            int finisherBonus = 0,
            int finisherHpThreshold = 0,
            int bonusSpecialEnergy = 0,
            int block = 0,
            int heal = 0,
            bool doublesNextAttack = false,
            SabotageKind sabotage = SabotageKind.None,
            int sabotageAmount = 0,
            double reversalChance = 0,
            int reversalAmount = 0)
        {
            Id = id;
            Name = name;
            Type = type;
            Cost = cost;
            Damage = damage;
            Hits = hits;
            FinisherBonus = finisherBonus;
            FinisherHpThreshold = finisherHpThreshold;
            BonusSpecialEnergy = bonusSpecialEnergy;
            Block = block;
            Heal = heal;
            DoublesNextAttack = doublesNextAttack;
            Sabotage = sabotage;
            SabotageAmount = sabotageAmount;
            ReversalChance = reversalChance;
            ReversalAmount = reversalAmount;
        }
    }
}
