using System.Linq;
using NUnit.Framework;
using Sabotrix.Core;

namespace Sabotrix.Tests.EditMode
{
    public class TurnFlowTests
    {
        [Test]
        public void EndTurn_Attack_DamagesOpponent()
        {
            Match match = TestContent.BonkVsPudge();
            match.TrySelect(match.ActivePlayer.Hand[0]);

            match.EndTurn();

            Assert.That(match.Players[1].Hp, Is.EqualTo(110 - 13));
        }

        [Test]
        public void EndTurn_PlayedCard_LeavesHandAndGoesToBottomOfDeck()
        {
            Match match = TestContent.BonkVsPudge();
            PlayerState bonk = match.ActivePlayer;
            CardInstance jab = bonk.Hand[0];
            match.TrySelect(jab);

            match.EndTurn();

            Assert.That(bonk.Hand, Has.No.Member(jab));
            Assert.That(bonk.Deck.Last(), Is.SameAs(jab));
            Assert.That(bonk.Selected, Is.Empty);
        }

        [Test]
        public void EndTurn_BasicAttack_DealsFiveAndIsNotAddedToDeck()
        {
            Match match = TestContent.BonkVsPudge();
            PlayerState bonk = match.ActivePlayer;
            int deckCount = bonk.Deck.Count;
            bonk.Energy = 0;
            match.TrySelect(bonk.BasicAttack);

            match.EndTurn();

            Assert.That(match.Players[1].Hp, Is.EqualTo(110 - 5));
            Assert.That(bonk.Deck.Count, Is.EqualTo(deckCount));
        }

        [Test]
        public void EndTurn_SabotageSelectedBeforeAttack_ResolvesAfterAttack()
        {
            Match match = TestContent.CreateMatch(
                TestContent.Hero("saboteur", initiative: 1), TestContent.Deck(TestContent.Blackout, TestContent.Jab, TestContent.Jab, TestContent.Jab, TestContent.Jab, TestContent.Jab),
                TestContent.Hero("target"), TestContent.BrawlerDeck());
            PlayerState player = match.ActivePlayer;
            CardInstance blackout = player.Hand[0];
            CardInstance jab = player.Hand[1];
            match.TrySelect(blackout);
            match.TrySelect(jab);

            match.EndTurn();

            Assert.That(player.Deck.Skip(player.Deck.Count - 2), Is.EqualTo(new[] { jab, blackout }));
            Assert.That(player.SabotagesPlayed, Is.EqualTo(1));
        }

        [Test]
        public void EndTurn_NextPlayer_BecomesActiveAndDrawsOne()
        {
            Match match = TestContent.BonkVsPudge();

            TestContent.PlayAnyAttack(match);

            Assert.That(match.ActivePlayer.Hero, Is.SameAs(TestContent.Pudge));
            Assert.That(match.ActivePlayer.Hand.Count, Is.EqualTo(Rules.StartingHandSize + 1));
            Assert.That(match.TurnNumber, Is.EqualTo(2));
        }

        [Test]
        public void StartTurn_EnergyRegen_AddsRegen()
        {
            Match match = TestContent.BonkVsPudge();
            PlayerState bonk = match.ActivePlayer;
            match.TrySelect(bonk.Hand[4]);
            match.TrySelect(bonk.Hand[2]);
            match.EndTurn();

            TestContent.PlayAnyAttack(match);

            Assert.That(bonk.Energy, Is.EqualTo(1 + 3));
        }

        [Test]
        public void StartTurn_EnergyRegen_CappedAtEnergyMax()
        {
            Match match = TestContent.BonkVsPudge();
            PlayerState bonk = match.ActivePlayer;

            TestContent.PlayAnyAttack(match);
            TestContent.PlayAnyAttack(match);

            Assert.That(bonk.Energy, Is.EqualTo(6));
        }

        [Test]
        public void StartTurn_HandFull_SkipsDraw()
        {
            // Energy Max 2 can't afford Body Slam (3), so each turn uses Basic Attack and the hand grows.
            HeroDefinition weak = TestContent.Hero("weak", maxHp: 1000, energyMax: 2, energyRegen: 0);
            DeckDefinition slams = TestContent.Deck(TestContent.Copies(TestContent.BodySlam, 8));
            Match match = TestContent.CreateMatch(weak, slams, weak, slams);
            PlayerState player = match.ActivePlayer;

            for (int turn = 0; turn < 8; turn++)
            {
                TestContent.PlayAnyAttack(match);
            }

            Assert.That(player.Hand.Count, Is.EqualTo(Rules.MaxHandSize));
            Assert.That(player.Deck.Count, Is.EqualTo(8 - Rules.MaxHandSize));
        }
    }
}
