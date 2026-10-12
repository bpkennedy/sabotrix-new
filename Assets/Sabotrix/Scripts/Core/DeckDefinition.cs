using System;
using System.Collections.Generic;
using System.Linq;

namespace Sabotrix.Core
{
    public sealed class DeckDefinition
    {
        public const int MaxAttackCards = 8;
        public const int MaxSabotageCards = 3;

        public string Name { get; }
        public IReadOnlyList<CardDefinition> Cards { get; }

        public DeckDefinition(string name, IEnumerable<CardDefinition> cards)
        {
            CardDefinition[] cardArray = cards.ToArray();

            if (cardArray.Any(card => card.Type == CardType.Ability))
            {
                throw new ArgumentException("Ability cards live in the hero's Ability slot, not the deck.", nameof(cards));
            }

            int attackCount = cardArray.Count(card => card.Type == CardType.Attack);
            if (attackCount > MaxAttackCards)
            {
                throw new ArgumentException($"Deck has {attackCount} Attack cards. Max is {MaxAttackCards}.", nameof(cards));
            }

            int sabotageCount = cardArray.Count(card => card.Type == CardType.Sabotage);
            if (sabotageCount > MaxSabotageCards)
            {
                throw new ArgumentException($"Deck has {sabotageCount} Sabotage cards. Max is {MaxSabotageCards}.", nameof(cards));
            }

            Name = name;
            Cards = cardArray;
        }
    }
}
