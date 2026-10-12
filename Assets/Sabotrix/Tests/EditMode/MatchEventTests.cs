using System.Linq;
using NUnit.Framework;
using Sabotrix.Core;

namespace Sabotrix.Tests.EditMode
{
    public class MatchEventTests
    {
        [Test]
        public void EndTurn_Jab_LogsCardPlayedThenDamage()
        {
            Match match = TestContent.PlayerHolding(TestContent.Jab);
            PlayerState player = match.ActivePlayer;
            PlayerState target = match.Opponent;
            match.TrySelect(player.Hand[0]);

            match.EndTurn();

            Assert.That(match.LastTurnEvents.Select(e => e.Kind), Is.EqualTo(new[] { MatchEventKind.CardPlayed, MatchEventKind.Damage }));
            Assert.That(match.LastTurnEvents[0].Player, Is.SameAs(player));
            Assert.That(match.LastTurnEvents[0].Card, Is.SameAs(TestContent.Jab));
            Assert.That(match.LastTurnEvents[1].Player, Is.SameAs(target));
            Assert.That(match.LastTurnEvents[1].Amount, Is.EqualTo(10));
        }

        [Test]
        public void EndTurn_DamagePartlyBlocked_LogsBlockedAmount()
        {
            Match match = TestContent.PlayerHolding(TestContent.Jab);
            match.Opponent.Block = 4;
            match.TrySelect(match.ActivePlayer.Hand[0]);

            match.EndTurn();

            MatchEvent damage = match.LastTurnEvents.Single(e => e.Kind == MatchEventKind.Damage);
            Assert.That(damage.Amount, Is.EqualTo(6));
            Assert.That(damage.Blocked, Is.EqualTo(4));
        }

        [Test]
        public void EndTurn_CounterStanceAndSecondWind_LogsBlockAndHeal()
        {
            Match match = TestContent.PlayerHolding(TestContent.CounterStance, TestContent.SecondWind);
            match.ActivePlayer.Hp = 50;
            match.TrySelect(match.ActivePlayer.Hand[0]);
            match.TrySelect(match.ActivePlayer.Hand[1]);

            match.EndTurn();

            Assert.That(match.LastTurnEvents.Single(e => e.Kind == MatchEventKind.BlockGained).Amount, Is.EqualTo(6));
            Assert.That(match.LastTurnEvents.Single(e => e.Kind == MatchEventKind.Heal).Amount, Is.EqualTo(8));
        }

        [Test]
        public void EndTurn_SabotageBackfires_LogsBackfire()
        {
            Match match = TestContent.PlayerHolding(TestContent.Hero("player", initiative: 1), new IdentityRandom(0.0), TestContent.Blackout);
            match.TrySelect(match.ActivePlayer.Hand[0]);
            match.TrySelect(match.ActivePlayer.Hand[1]);

            match.EndTurn();

            Assert.That(match.LastTurnEvents.Last().Kind, Is.EqualTo(MatchEventKind.SabotageBackfired));
            Assert.That(match.LastTurnEvents.Last().Card, Is.SameAs(TestContent.Blackout));
        }

        [Test]
        public void EndTurn_NextTurn_ReplacesPreviousEvents()
        {
            Match match = TestContent.PlayerHolding(TestContent.Jab);
            TestContent.PlayAnyAttack(match);

            TestContent.PlayAnyAttack(match);

            Assert.That(match.LastTurnEvents.Count, Is.EqualTo(2));
            Assert.That(match.LastTurnEvents[0].Player, Is.SameAs(match.Players[1]));
        }
    }
}
