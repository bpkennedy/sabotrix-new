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
        public int Hp { get; internal set; }
        public int Block { get; internal set; }
        public int Energy { get; internal set; }
        public int SpecialEnergy { get; internal set; }
        public int SabotagesPlayed { get; internal set; }

        // What the opponent sees while picks are secret: Energy and Special Energy before any cards were picked.
        public int RoundStartEnergy { get; internal set; }
        public int RoundStartSpecialEnergy { get; internal set; }

        // Sabotage effects active during this round. Blind: you can see your cards' types but not what they are.
        public bool IsBlind { get; internal set; }
        public int AttackCostIncrease { get; internal set; }

        // Set by an Ability like Haymaker; used up by this player's next Attack this round.
        internal bool DoubleNextAttack { get; set; }

        // Sabotage effects waiting for the next round to start.
        internal bool PendingBlind { get; set; }
        internal int PendingEnergyGain { get; set; }
        internal int PendingEnergyLoss { get; set; }
        internal int PendingAttackCostIncrease { get; set; }

        // Ability and Basic Attack live in their own slots, never in the deck or hand.
        public CardInstance Ability { get; }
        public CardInstance BasicAttack { get; }

        // Index 0 is the top of the deck.
        public IReadOnlyList<CardInstance> Deck => _deck;

        // Selected cards stay in the hand until the round resolves.
        public IReadOnlyList<CardInstance> Hand => _hand;

        // Cards chosen this round, in the order they were selected.
        public IReadOnlyList<CardInstance> Selected => _selected;

        internal PlayerState(HeroDefinition hero, IEnumerable<CardInstance> deck, CardInstance ability, CardInstance basicAttack)
        {
            Hero = hero;
            Hp = hero.MaxHp;
            Energy = hero.EnergyMax;
            Ability = ability;
            BasicAttack = basicAttack;
            _deck = new List<CardInstance>(deck);
        }

        public bool IsSelected(CardInstance card) => _selected.Contains(card);

        // Fisher-Yates shuffle.
        internal void ShuffleDeck(IRandom random)
        {
            for (int i = _deck.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (_deck[i], _deck[j]) = (_deck[j], _deck[i]);
            }
        }

        internal void DrawCard()
        {
            if (_deck.Count == 0)
            {
                return;
            }

            _hand.Add(_deck[0]);
            _deck.RemoveAt(0);
        }

        internal void Select(CardInstance card) => _selected.Add(card);

        internal void Unselect(CardInstance card) => _selected.Remove(card);

        // Moves played hand cards to the bottom of the deck, in the given order, and clears the selection.
        internal void CyclePlayedCards(IEnumerable<CardInstance> playedInOrder)
        {
            foreach (CardInstance card in playedInOrder)
            {
                if (_hand.Remove(card))
                {
                    _deck.Add(card);
                }
            }

            _selected.Clear();
        }
    }
}
