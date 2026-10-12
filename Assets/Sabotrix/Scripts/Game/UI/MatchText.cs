using System.Collections.Generic;
using Sabotrix.Core;

namespace Sabotrix.Game.UI
{
    // Turns cards, players, and match events into the words shown on the gray-box screen.
    public static class MatchText
    {
        public static string PlayerName(Match match, PlayerState player)
        {
            int number = match.Players[0] == player ? 1 : 2;
            return $"P{number} {player.Hero.Name}";
        }

        // hidePicks: show Energy as it was at the start of the round, so the opponent's secret picks don't show.
        public static string Stats(Match match, PlayerState player, bool hidePicks)
        {
            int energy = hidePicks ? player.RoundStartEnergy : player.Energy;
            int specialEnergy = hidePicks ? player.RoundStartSpecialEnergy : player.SpecialEnergy;
            return $"{PlayerName(match, player)} ({player.Hero.Class})   " +
                $"HP {player.Hp}/{player.Hero.MaxHp}   Block {player.Block}   " +
                $"Energy {energy}/{player.Hero.EnergyMax}   Special {specialEnergy}/{Rules.SpecialEnergyMax}   " +
                $"Hand {player.Hand.Count}   Deck {player.Deck.Count}   Sabotage {player.SabotagesPlayed}/{Rules.MaxSabotagesPerMatch}";
        }

        public static string CardFace(CardDefinition card, int cost)
        {
            string costUnit = card.Type == CardType.Ability ? "Special" : "Energy";
            List<string> lines = new List<string> { card.Name.ToUpperInvariant(), $"{card.Type} - {cost} {costUnit}", "" };
            lines.AddRange(Effects(card));
            return string.Join("\n", lines);
        }

        public static string Describe(Match match, MatchEvent matchEvent)
        {
            string who = PlayerName(match, matchEvent.Player);
            switch (matchEvent.Kind)
            {
                case MatchEventKind.CardPlayed:
                    return $"{who} plays {matchEvent.Card.Name}";
                case MatchEventKind.Damage:
                    return matchEvent.Blocked > 0
                        ? $"   {who} takes {matchEvent.Amount} ({matchEvent.Blocked} blocked)"
                        : $"   {who} takes {matchEvent.Amount}";
                case MatchEventKind.Heal:
                    return $"   {who} heals {matchEvent.Amount}";
                case MatchEventKind.BlockGained:
                    return $"   {who} gains {matchEvent.Amount} Block";
                case MatchEventKind.SabotageLanded:
                    return $"   {matchEvent.Card.Name} lands!";
                case MatchEventKind.SabotageBackfired:
                    return $"   {matchEvent.Card.Name} BACKFIRES on {who}!";
                default:
                    return matchEvent.Kind.ToString();
            }
        }

        private static IEnumerable<string> Effects(CardDefinition card)
        {
            if (card.Damage > 0)
            {
                yield return card.Hits > 1 ? $"{card.Damage} dmg x{card.Hits}" : $"{card.Damage} dmg";
            }

            if (card.FinisherBonus > 0)
            {
                yield return $"+{card.FinisherBonus} dmg if foe has {card.FinisherHpThreshold} HP or less";
            }

            if (card.BonusSpecialEnergy > 0)
            {
                yield return $"+{card.BonusSpecialEnergy} Special";
            }

            if (card.Block > 0)
            {
                yield return $"+{card.Block} Block";
            }

            if (card.Heal > 0)
            {
                yield return $"Heal {card.Heal}";
            }

            if (card.DoublesNextAttack)
            {
                yield return "Next Attack deals double";
            }

            switch (card.Sabotage)
            {
                case SabotageKind.Blind:
                    yield return "Everyone plays blind next round";
                    yield return $"{Percent(card)} backfire: only you play blind";
                    break;
                case SabotageKind.EnergySteal:
                    yield return $"Steal {card.SabotageAmount} Energy";
                    yield return $"{Percent(card)} backfire: you lose {card.ReversalAmount} Energy";
                    break;
                case SabotageKind.Damage:
                    yield return $"{card.SabotageAmount} dmg to foe";
                    yield return $"{Percent(card)} backfire: you take {card.ReversalAmount}";
                    break;
                case SabotageKind.AttackCostIncrease:
                    yield return $"Foe's Attacks +{card.SabotageAmount} cost next round";
                    yield return $"{Percent(card)} backfire: yours +{card.ReversalAmount}";
                    break;
            }
        }

        private static string Percent(CardDefinition card) => $"{card.ReversalChance * 100:0}%";
    }
}
