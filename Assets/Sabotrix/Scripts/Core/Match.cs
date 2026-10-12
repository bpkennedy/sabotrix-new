using System.Collections.Generic;
using System.Linq;

namespace Sabotrix.Core
{
    public sealed class Match
    {
        public IReadOnlyList<PlayerState> Players { get; }
        public int TurnNumber { get; private set; }

        private Match(PlayerState playerOne, PlayerState playerTwo)
        {
            Players = new[] { playerOne, playerTwo };
        }

        public static Match Create(HeroDefinition heroOne, DeckDefinition deckOne, HeroDefinition heroTwo, DeckDefinition deckTwo)
        {
            int nextInstanceId = 0;
            List<CardInstance> CreateInstances(DeckDefinition deck) =>
                deck.Cards.Select(card => new CardInstance(nextInstanceId++, card)).ToList();

            PlayerState playerOne = new PlayerState(heroOne, CreateInstances(deckOne));
            PlayerState playerTwo = new PlayerState(heroTwo, CreateInstances(deckTwo));
            return new Match(playerOne, playerTwo);
        }
    }
}
