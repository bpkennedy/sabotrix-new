using System;
using System.Collections.Generic;
using System.Linq;
using Sabotrix.Core;
using UnityEngine;

namespace Sabotrix.Game.Data
{
    // A prebuilt deck as an editable asset: a list of cards and how many copies of each.
    [CreateAssetMenu(menuName = "Sabotrix/Deck", fileName = "NewDeck")]
    public sealed class DeckData : ScriptableObject
    {
        [Serializable]
        private struct Entry
        {
            public CardData Card;
            [Min(1)] public int Count;
        }

        [SerializeField] private string _displayName;
        [SerializeField] private List<Entry> _cards = new List<Entry>();

        public string DisplayName => _displayName;
        public IEnumerable<CardData> Cards => _cards.Select(entry => entry.Card);

        public DeckDefinition ToDefinition()
        {
            List<CardDefinition> cards = new List<CardDefinition>();
            foreach (Entry entry in _cards)
            {
                CardDefinition card = entry.Card.ToDefinition();
                cards.AddRange(Enumerable.Repeat(card, entry.Count));
            }

            return new DeckDefinition(_displayName, cards);
        }
    }
}
