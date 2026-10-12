using System.Collections.Generic;

namespace Sabotrix.Core
{
    // One player's side of the table during a match.
    public sealed class PlayerState
    {
        private readonly List<CardInstance> _deck;
        private readonly List<CardInstance> _hand = new List<CardInstance>();
        private readonly List<CardInstance> _selected = new List<CardInstance>();

        public HeroDefinition Hero { get; }
        public int Hp { get; private set; }
        public int Block { get; private set; }
        public int Energy { get; private set; }
        public int SpecialEnergy { get; private set; }
        public int SabotagesPlayed { get; private set; }

        // Index 0 is the top of the deck.
        public IReadOnlyList<CardInstance> Deck => _deck;
        public IReadOnlyList<CardInstance> Hand => _hand;

        // Cards chosen this turn, in play order.
        public IReadOnlyList<CardInstance> Selected => _selected;

        internal PlayerState(HeroDefinition hero, IEnumerable<CardInstance> deck)
        {
            Hero = hero;
            Hp = hero.MaxHp;
            Energy = hero.EnergyMax;
            _deck = new List<CardInstance>(deck);
        }
    }
}
