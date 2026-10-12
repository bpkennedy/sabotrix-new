using System;
using System.Collections.Generic;
using System.Linq;

namespace Sabotrix.Core
{
    public sealed class Match
    {
        private readonly PlayerState[] _players;

        public IReadOnlyList<PlayerState> Players => _players;
        public int TurnNumber { get; private set; }
        public int ActivePlayerIndex { get; private set; }
        public PlayerState ActivePlayer => _players[ActivePlayerIndex];
        public PlayerState Opponent => _players[1 - ActivePlayerIndex];
        public bool IsOver { get; private set; }

        // Null while the match is running, and null when it ends in a draw.
        public PlayerState Winner { get; private set; }

        private Match(PlayerState playerOne, PlayerState playerTwo)
        {
            _players = new[] { playerOne, playerTwo };
        }

        // Builds the match, shuffles, deals, picks the first player, and starts turn 1.
        public static Match Create(HeroDefinition heroOne, DeckDefinition deckOne, HeroDefinition heroTwo, DeckDefinition deckTwo, IRandom random)
        {
            int nextInstanceId = 0;
            CardInstance NewInstance(CardDefinition card) => new CardInstance(nextInstanceId++, card);
            PlayerState NewPlayer(HeroDefinition hero, DeckDefinition deck) => new PlayerState(
                hero,
                deck.Cards.Select(NewInstance).ToList(),
                NewInstance(hero.Ability),
                NewInstance(Rules.BasicAttack));

            Match match = new Match(NewPlayer(heroOne, deckOne), NewPlayer(heroTwo, deckTwo));

            foreach (PlayerState player in match._players)
            {
                player.ShuffleDeck(random);
                for (int i = 0; i < Rules.StartingHandSize; i++)
                {
                    player.DrawCard();
                }
            }

            match.ActivePlayerIndex = FirstPlayerIndex(heroOne, heroTwo, random);
            match.StartTurn();
            return match;
        }

        public bool CanSelect(CardInstance card)
        {
            PlayerState player = ActivePlayer;
            if (IsOver || player.IsSelected(card))
            {
                return false;
            }

            if (card == player.Ability)
            {
                return player.SpecialEnergy >= card.Definition.Cost;
            }

            if (card == player.BasicAttack)
            {
                return !HasSelectedAttack(player) && !HasAffordableAttackInHand(player);
            }

            if (!player.Hand.Contains(card))
            {
                return false;
            }

            switch (card.Definition.Type)
            {
                case CardType.Attack:
                    return player.Energy >= card.Definition.Cost;
                case CardType.Sabotage:
                    return player.Energy >= card.Definition.Cost
                        && player.SabotagesPlayed < Rules.MaxSabotagesPerMatch
                        && !player.Selected.Any(selected => selected.Definition.Type == CardType.Sabotage);
                default:
                    return false;
            }
        }

        // Selects a card for this turn and reserves its cost. Returns false if the selection isn't allowed.
        public bool TrySelect(CardInstance card)
        {
            if (!CanSelect(card))
            {
                return false;
            }

            PlayerState player = ActivePlayer;
            if (card == player.Ability)
            {
                player.SpecialEnergy -= card.Definition.Cost;
            }
            else
            {
                player.Energy -= card.Definition.Cost;
            }

            player.Select(card);
            return true;
        }

        // Takes back a selected card and refunds its cost.
        public bool TryWithdraw(CardInstance card)
        {
            PlayerState player = ActivePlayer;
            if (IsOver || !player.IsSelected(card))
            {
                return false;
            }

            if (card == player.Ability)
            {
                player.SpecialEnergy += card.Definition.Cost;
            }
            else
            {
                player.Energy += card.Definition.Cost;
            }

            player.Unselect(card);
            return true;
        }

        public bool CanEndTurn() => !IsOver && HasSelectedAttack(ActivePlayer);

        // Resolves the selected cards, cycles them to the bottom of the deck, then starts the next turn.
        public void EndTurn()
        {
            if (!CanEndTurn())
            {
                throw new InvalidOperationException("End Turn needs at least one Attack selected.");
            }

            PlayerState player = ActivePlayer;
            List<CardInstance> playOrder = player.Selected.OrderBy(card => ResolveRank(card.Definition.Type)).ToList();

            foreach (CardInstance card in playOrder)
            {
                Resolve(card);
                if (Opponent.Hp <= 0)
                {
                    EndMatch(player);
                    return;
                }
            }

            player.CyclePlayedCards(playOrder);

            if (TurnNumber >= Rules.TurnLimit)
            {
                EndMatchByHp();
                return;
            }

            ActivePlayerIndex = 1 - ActivePlayerIndex;
            StartTurn();
        }

        public void Forfeit(PlayerState player)
        {
            if (IsOver)
            {
                return;
            }

            EndMatch(_players[0] == player ? _players[1] : _players[0]);
        }

        private static int FirstPlayerIndex(HeroDefinition heroOne, HeroDefinition heroTwo, IRandom random)
        {
            if (heroOne.Initiative == heroTwo.Initiative)
            {
                return random.Next(2);
            }

            return heroOne.Initiative > heroTwo.Initiative ? 0 : 1;
        }

        // Cards play out Ability, then Attacks (in selection order), then Sabotage.
        private static int ResolveRank(CardType type)
        {
            switch (type)
            {
                case CardType.Ability:
                    return 0;
                case CardType.Attack:
                    return 1;
                default:
                    return 2;
            }
        }

        private static bool HasSelectedAttack(PlayerState player) =>
            player.Selected.Any(card => card.Definition.Type == CardType.Attack);

        private static bool HasAffordableAttackInHand(PlayerState player) =>
            player.Hand.Any(card =>
                card.Definition.Type == CardType.Attack
                && !player.IsSelected(card)
                && player.Energy >= card.Definition.Cost);

        private void StartTurn()
        {
            TurnNumber++;
            PlayerState player = ActivePlayer;

            if (player.Hand.Count < Rules.MaxHandSize)
            {
                player.DrawCard();
            }

            player.Energy = Math.Min(player.Hero.EnergyMax, player.Energy + player.Hero.EnergyRegen);
        }

        // Card effects, Strength, and Block arrive in M1.3. For now Attacks deal their printed damage.
        private void Resolve(CardInstance card)
        {
            switch (card.Definition.Type)
            {
                case CardType.Attack:
                    Opponent.Hp = Math.Max(0, Opponent.Hp - card.Definition.Damage);
                    break;
                case CardType.Sabotage:
                    ActivePlayer.SabotagesPlayed++;
                    break;
            }
        }

        private void EndMatchByHp()
        {
            PlayerState one = _players[0];
            PlayerState two = _players[1];

            if (one.Hp == two.Hp)
            {
                EndMatch(null);
                return;
            }

            EndMatch(one.Hp > two.Hp ? one : two);
        }

        private void EndMatch(PlayerState winner)
        {
            IsOver = true;
            Winner = winner;
        }
    }
}
