using System;
using System.Collections.Generic;
using System.Linq;

namespace Sabotrix.Core
{
    public sealed class Match
    {
        private readonly PlayerState[] _players;
        private readonly IRandom _random;

        // Set by an Ability like Haymaker; used up by the next Attack this turn.
        private bool _doubleNextAttack;

        public IReadOnlyList<PlayerState> Players => _players;
        public int TurnNumber { get; private set; }
        public int ActivePlayerIndex { get; private set; }
        public PlayerState ActivePlayer => _players[ActivePlayerIndex];
        public PlayerState Opponent => _players[1 - ActivePlayerIndex];
        public bool IsOver { get; private set; }

        // Null while the match is running, and null when it ends in a draw.
        public PlayerState Winner { get; private set; }

        private Match(PlayerState playerOne, PlayerState playerTwo, IRandom random)
        {
            _players = new[] { playerOne, playerTwo };
            _random = random;
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

            Match match = new Match(NewPlayer(heroOne, deckOne), NewPlayer(heroTwo, deckTwo), random);

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

        // Energy cost for the active player this turn, including Sabotage cost increases.
        // Ability cards cost Special Energy instead.
        public int CostOf(CardInstance card)
        {
            bool isAttackCard = card.Definition.Type == CardType.Attack && card != ActivePlayer.BasicAttack;
            return card.Definition.Cost + (isAttackCard ? ActivePlayer.AttackCostIncrease : 0);
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
                    return player.Energy >= CostOf(card);
                case CardType.Sabotage:
                    return player.Energy >= CostOf(card)
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
                player.Energy -= CostOf(card);
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
                player.Energy += CostOf(card);
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
                if (TryEndMatchOnKnockout())
                {
                    return;
                }
            }

            player.CyclePlayedCards(playOrder);
            player.IsBlind = false;
            player.AttackCostIncrease = 0;

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

        private bool HasSelectedAttack(PlayerState player) =>
            player.Selected.Any(card => card.Definition.Type == CardType.Attack);

        private bool HasAffordableAttackInHand(PlayerState player) =>
            player.Hand.Any(card =>
                card.Definition.Type == CardType.Attack
                && !player.IsSelected(card)
                && player.Energy >= CostOf(card));

        private void StartTurn()
        {
            TurnNumber++;
            _doubleNextAttack = false;
            PlayerState player = ActivePlayer;

            // Unused Block disappears at the start of your next turn.
            player.Block = 0;

            if (player.Hand.Count < Rules.MaxHandSize)
            {
                player.DrawCard();
            }

            int energyMax = player.Hero.EnergyMax;
            player.Energy = Math.Min(energyMax, player.Energy + player.Hero.EnergyRegen + player.PendingEnergyGain);
            player.Energy = Math.Max(0, player.Energy - player.PendingEnergyLoss);
            player.IsBlind = player.PendingBlind;
            player.AttackCostIncrease = player.PendingAttackCostIncrease;

            player.PendingEnergyGain = 0;
            player.PendingEnergyLoss = 0;
            player.PendingBlind = false;
            player.PendingAttackCostIncrease = 0;
        }

        private void Resolve(CardInstance card)
        {
            switch (card.Definition.Type)
            {
                case CardType.Ability:
                    ResolveAbility(card.Definition);
                    break;
                case CardType.Attack:
                    ResolveAttack(card);
                    break;
                case CardType.Sabotage:
                    ResolveSabotage(card.Definition);
                    break;
            }
        }

        private void ResolveAbility(CardDefinition ability)
        {
            ActivePlayer.Block += ability.Block;
            if (ability.DoublesNextAttack)
            {
                _doubleNextAttack = true;
            }
        }

        private void ResolveAttack(CardInstance card)
        {
            PlayerState player = ActivePlayer;
            PlayerState target = Opponent;
            CardDefinition attack = card.Definition;

            // Attack damage = card damage + Strength (Basic Attack gets no Strength).
            int damagePerHit = attack.Damage + (card == player.BasicAttack ? 0 : player.Hero.Strength);
            if (attack.FinisherBonus > 0 && target.Hp <= attack.FinisherHpThreshold)
            {
                damagePerHit += attack.FinisherBonus;
            }

            if (_doubleNextAttack)
            {
                damagePerHit *= 2;
                _doubleNextAttack = false;
            }

            bool dealtDamage = false;
            for (int hit = 0; hit < attack.Hits && target.Hp > 0; hit++)
            {
                if (DealDamage(target, damagePerHit) > 0)
                {
                    dealtDamage = true;
                    GainSpecialEnergy(player, player.Hero.SpecialEnergyGain);
                }
            }

            if (dealtDamage)
            {
                GainSpecialEnergy(player, attack.BonusSpecialEnergy);
            }

            player.Block += attack.Block;
            player.Hp = Math.Min(player.Hero.MaxHp, player.Hp + attack.Heal);
        }

        private void ResolveSabotage(CardDefinition sabotage)
        {
            PlayerState player = ActivePlayer;
            PlayerState target = Opponent;
            player.SabotagesPlayed++;

            bool backfired = sabotage.ReversalChance > 0 && _random.NextDouble() <= sabotage.ReversalChance;
            PlayerState affected = backfired ? player : target;
            int amount = backfired ? sabotage.ReversalAmount : sabotage.SabotageAmount;

            switch (sabotage.Sabotage)
            {
                case SabotageKind.Blind:
                    affected.PendingBlind = true;
                    break;
                case SabotageKind.EnergySteal:
                    affected.PendingEnergyLoss += amount;
                    if (!backfired)
                    {
                        player.PendingEnergyGain += amount;
                    }
                    break;
                case SabotageKind.Damage:
                    DealDamage(affected, amount);
                    break;
                case SabotageKind.AttackCostIncrease:
                    affected.PendingAttackCostIncrease += amount;
                    break;
            }
        }

        // Block absorbs damage first. Returns the HP actually lost.
        private static int DealDamage(PlayerState target, int amount)
        {
            int absorbed = Math.Min(target.Block, amount);
            target.Block -= absorbed;

            int hpLost = Math.Min(target.Hp, amount - absorbed);
            target.Hp -= hpLost;
            return hpLost;
        }

        private static void GainSpecialEnergy(PlayerState player, int amount)
        {
            player.SpecialEnergy = Math.Min(Rules.SpecialEnergyMax, player.SpecialEnergy + amount);
        }

        // A backfire can knock out the player who played the Sabotage.
        private bool TryEndMatchOnKnockout()
        {
            if (Opponent.Hp <= 0)
            {
                EndMatch(ActivePlayer);
                return true;
            }

            if (ActivePlayer.Hp <= 0)
            {
                EndMatch(Opponent);
                return true;
            }

            return false;
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
