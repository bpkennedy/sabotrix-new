using System;
using System.Collections.Generic;
using System.Linq;

namespace Sabotrix.Core
{
    // A match is played in rounds. Each round both players pick their cards in secret, one after the other,
    // then all the picked cards resolve together, phase by phase: Abilities, then Attacks, then Sabotage.
    public sealed class Match
    {
        private static readonly CardType[] ResolvePhases = { CardType.Ability, CardType.Attack, CardType.Sabotage };

        private readonly PlayerState[] _players;
        private readonly IRandom _random;
        private readonly List<MatchEvent> _lastRoundEvents = new List<MatchEvent>();

        // The higher-Initiative player picks first every round and acts first within each phase.
        private int _firstPickerIndex;

        public IReadOnlyList<PlayerState> Players => _players;

        // What happened when the most recent round resolved, in order.
        public IReadOnlyList<MatchEvent> LastRoundEvents => _lastRoundEvents;
        public int RoundNumber { get; private set; }

        // The player picking cards right now, and the other player.
        public int ActivePlayerIndex { get; private set; }
        public PlayerState ActivePlayer => _players[ActivePlayerIndex];
        public PlayerState Opponent => _players[1 - ActivePlayerIndex];

        // True until the first player of the round locks in their cards.
        public bool IsFirstPick => ActivePlayerIndex == _firstPickerIndex;
        public bool IsOver { get; private set; }

        // Null while the match is running, and null when it ends in a draw.
        public PlayerState Winner { get; private set; }

        private Match(PlayerState playerOne, PlayerState playerTwo, IRandom random)
        {
            _players = new[] { playerOne, playerTwo };
            _random = random;
        }

        // Builds the match, shuffles, deals, picks the first picker, and starts round 1.
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

            match._firstPickerIndex = FirstPickerIndex(heroOne, heroTwo, random);
            match.StartRound();
            return match;
        }

        // Energy cost for the picking player this round, including Sabotage cost increases.
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

        // Selects a card for this round and reserves its cost. Returns false if the selection isn't allowed.
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

        // Locks in the picking player's cards. The first lock-in passes the pick to the other player;
        // the second resolves the round and starts the next one.
        public void EndTurn()
        {
            if (!CanEndTurn())
            {
                throw new InvalidOperationException("End Turn needs at least one Attack selected.");
            }

            if (IsFirstPick)
            {
                ActivePlayerIndex = 1 - _firstPickerIndex;
                return;
            }

            ResolveRound();
        }

        public void Forfeit(PlayerState player)
        {
            if (IsOver)
            {
                return;
            }

            EndMatch(_players[0] == player ? _players[1] : _players[0]);
        }

        private static int FirstPickerIndex(HeroDefinition heroOne, HeroDefinition heroTwo, IRandom random)
        {
            if (heroOne.Initiative == heroTwo.Initiative)
            {
                return random.Next(2);
            }

            return heroOne.Initiative > heroTwo.Initiative ? 0 : 1;
        }

        // Each phase resolves the faster player's cards (in selection order), then the other player's.
        // Knockouts are checked at the end of each phase, so both heroes can go down in the same round.
        private void ResolveRound()
        {
            _lastRoundEvents.Clear();
            PlayerState first = _players[_firstPickerIndex];
            PlayerState second = _players[1 - _firstPickerIndex];

            foreach (CardType phase in ResolvePhases)
            {
                foreach (PlayerState player in new[] { first, second })
                {
                    PlayerState target = player == first ? second : first;
                    foreach (CardInstance card in player.Selected.Where(card => card.Definition.Type == phase))
                    {
                        _lastRoundEvents.Add(new MatchEvent(MatchEventKind.CardPlayed, player, card.Definition));
                        Resolve(player, target, card);
                    }
                }

                if (TryEndMatchOnKnockout())
                {
                    return;
                }
            }

            foreach (PlayerState player in _players)
            {
                player.CyclePlayedCards(ResolvePhases.SelectMany(phase => player.Selected.Where(card => card.Definition.Type == phase)).ToList());
                player.IsBlind = false;
                player.AttackCostIncrease = 0;
            }

            if (RoundNumber >= Rules.RoundLimit)
            {
                EndMatchByHp();
                return;
            }

            StartRound();
        }

        private bool HasSelectedAttack(PlayerState player) =>
            player.Selected.Any(card => card.Definition.Type == CardType.Attack);

        private bool HasAffordableAttackInHand(PlayerState player) =>
            player.Hand.Any(card =>
                card.Definition.Type == CardType.Attack
                && !player.IsSelected(card)
                && player.Energy >= CostOf(card));

        // Both players draw, regain Energy, and pick up any Sabotage effects aimed at this round.
        private void StartRound()
        {
            RoundNumber++;
            ActivePlayerIndex = _firstPickerIndex;

            foreach (PlayerState player in _players)
            {
                // Unused Block disappears at the start of the next round.
                player.Block = 0;
                player.DoubleNextAttack = false;

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

                player.RoundStartEnergy = player.Energy;
                player.RoundStartSpecialEnergy = player.SpecialEnergy;
            }
        }

        private void Resolve(PlayerState player, PlayerState target, CardInstance card)
        {
            switch (card.Definition.Type)
            {
                case CardType.Ability:
                    ResolveAbility(player, card.Definition);
                    break;
                case CardType.Attack:
                    ResolveAttack(player, target, card);
                    break;
                case CardType.Sabotage:
                    ResolveSabotage(player, target, card.Definition);
                    break;
            }
        }

        private void ResolveAbility(PlayerState player, CardDefinition ability)
        {
            GainBlock(player, ability.Block);
            if (ability.DoublesNextAttack)
            {
                player.DoubleNextAttack = true;
            }
        }

        private void ResolveAttack(PlayerState player, PlayerState target, CardInstance card)
        {
            CardDefinition attack = card.Definition;

            // Attack damage = card damage + Strength (Basic Attack gets no Strength).
            int damagePerHit = attack.Damage + (card == player.BasicAttack ? 0 : player.Hero.Strength);
            if (attack.FinisherBonus > 0 && target.Hp <= attack.FinisherHpThreshold)
            {
                damagePerHit += attack.FinisherBonus;
            }

            if (player.DoubleNextAttack)
            {
                damagePerHit *= 2;
                player.DoubleNextAttack = false;
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

            GainBlock(player, attack.Block);

            int healed = Math.Min(player.Hero.MaxHp - player.Hp, attack.Heal);
            if (healed > 0)
            {
                player.Hp += healed;
                _lastRoundEvents.Add(new MatchEvent(MatchEventKind.Heal, player, attack, healed));
            }
        }

        private void ResolveSabotage(PlayerState player, PlayerState target, CardDefinition sabotage)
        {
            player.SabotagesPlayed++;

            bool backfired = sabotage.ReversalChance > 0 && _random.NextDouble() <= sabotage.ReversalChance;
            PlayerState affected = backfired ? player : target;
            int amount = backfired ? sabotage.ReversalAmount : sabotage.SabotageAmount;
            MatchEventKind outcome = backfired ? MatchEventKind.SabotageBackfired : MatchEventKind.SabotageLanded;
            _lastRoundEvents.Add(new MatchEvent(outcome, player, sabotage));

            switch (sabotage.Sabotage)
            {
                // The lights go out for everyone. On a backfire they flicker back for the opponent only.
                case SabotageKind.Blind:
                    player.PendingBlind = true;
                    target.PendingBlind = !backfired || target.PendingBlind;
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
        private int DealDamage(PlayerState target, int amount)
        {
            int absorbed = Math.Min(target.Block, amount);
            target.Block -= absorbed;

            int hpLost = Math.Min(target.Hp, amount - absorbed);
            target.Hp -= hpLost;
            _lastRoundEvents.Add(new MatchEvent(MatchEventKind.Damage, target, amount: hpLost, blocked: absorbed));
            return hpLost;
        }

        private void GainBlock(PlayerState player, int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            player.Block += amount;
            _lastRoundEvents.Add(new MatchEvent(MatchEventKind.BlockGained, player, amount: amount));
        }

        private static void GainSpecialEnergy(PlayerState player, int amount)
        {
            player.SpecialEnergy = Math.Min(Rules.SpecialEnergyMax, player.SpecialEnergy + amount);
        }

        // Either hero (even the one whose Sabotage backfired) can be knocked out. Both at once is a draw.
        private bool TryEndMatchOnKnockout()
        {
            bool oneDown = _players[0].Hp <= 0;
            bool twoDown = _players[1].Hp <= 0;
            if (!oneDown && !twoDown)
            {
                return false;
            }

            EndMatch(oneDown && twoDown ? null : oneDown ? _players[1] : _players[0]);
            return true;
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
