using System.Linq;
using NUnit.Framework;
using Sabotrix.Core;

namespace Sabotrix.Tests.EditMode
{
    public class MatchEventTests
    {
        [Test]
        public void EndRound_Jab_LogsCardPlayedThenDamage()
        {
            Match match = TestContent.PlayerHolding(TestContent.Jab);
            PlayerState player = match.ActivePlayer;
            PlayerState target = match.Opponent;
            match.TrySelect(player.Hand[0]);

            TestContent.EndRound(match);

            Assert.That(match.LastRoundEvents.Select(e => e.Kind).Take(2), Is.EqualTo(new[] { MatchEventKind.CardPlayed, MatchEventKind.Damage }));
            Assert.That(match.LastRoundEvents[0].Player, Is.SameAs(player));
            Assert.That(match.LastRoundEvents[0].Card, Is.SameAs(TestContent.Jab));
            Assert.That(match.LastRoundEvents[1].Player, Is.SameAs(target));
            Assert.That(match.LastRoundEvents[1].Amount, Is.EqualTo(10));
        }

        [Test]
        public void EndRound_DamagePartlyBlocked_LogsBlockedAmount()
        {
            Match match = TestContent.PlayerHolding(TestContent.Jab);
            PlayerState target = match.Opponent;
            target.Block = 4;
            match.TrySelect(match.ActivePlayer.Hand[0]);

            TestContent.EndRound(match);

            MatchEvent damage = match.LastRoundEvents.First(e => e.Kind == MatchEventKind.Damage && e.Player == target);
            Assert.That(damage.Amount, Is.EqualTo(6));
            Assert.That(damage.Blocked, Is.EqualTo(4));
        }

        [Test]
        public void EndRound_CounterStanceAndSecondWind_LogsBlockAndHeal()
        {
            Match match = TestContent.PlayerHolding(TestContent.CounterStance, TestContent.SecondWind);
            match.ActivePlayer.Hp = 50;
            match.TrySelect(match.ActivePlayer.Hand[0]);
            match.TrySelect(match.ActivePlayer.Hand[1]);

            TestContent.EndRound(match);

            Assert.That(match.LastRoundEvents.Single(e => e.Kind == MatchEventKind.BlockGained).Amount, Is.EqualTo(6));
            Assert.That(match.LastRoundEvents.Single(e => e.Kind == MatchEventKind.Heal).Amount, Is.EqualTo(8));
        }

        [Test]
        public void EndRound_SabotageBackfires_LogsBackfire()
        {
            Match match = TestContent.PlayerHolding(TestContent.Hero("player", initiative: 1), new IdentityRandom(0.0), TestContent.Blackout);
            match.TrySelect(match.ActivePlayer.Hand[0]);
            match.TrySelect(match.ActivePlayer.Hand[1]);

            TestContent.EndRound(match);

            Assert.That(match.LastRoundEvents.Last().Kind, Is.EqualTo(MatchEventKind.SabotageBackfired));
            Assert.That(match.LastRoundEvents.Last().Card, Is.SameAs(TestContent.Blackout));
        }

        [Test]
        public void EndRound_NextRound_ReplacesPreviousEvents()
        {
            Match match = TestContent.PlayerHolding(TestContent.Jab);
            TestContent.PlayRound(match);

            TestContent.PlayRound(match);

            Assert.That(match.LastRoundEvents.Count, Is.EqualTo(4));
            Assert.That(match.LastRoundEvents[0].Player, Is.SameAs(match.Players[0]));
        }
    }
}
