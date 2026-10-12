using NUnit.Framework;
using Sabotrix.Core;

namespace Sabotrix.Tests.EditMode
{
    public class WinConditionTests
    {
        [Test]
        public void EndRound_OpponentHpReachesZero_HitterWins()
        {
            Match match = TestContent.CreateMatch(
                TestContent.Hero("hitter", initiative: 1), TestContent.BrawlerDeck(),
                TestContent.Hero("fragile", maxHp: 10), TestContent.BunkerDeck());
            PlayerState hitter = match.ActivePlayer;
            match.TrySelect(hitter.Hand[0]);

            TestContent.EndRound(match);

            Assert.That(match.IsOver, Is.True);
            Assert.That(match.Winner, Is.SameAs(hitter));
            Assert.That(match.Players[1].Hp, Is.EqualTo(0));
        }

        [Test]
        public void EndRound_KnockedOutInAttacks_StillHitsBackThatPhase()
        {
            // Attacks in a phase land together: the fragile hero's Jab still resolves after it drops to 0.
            Match match = TestContent.CreateMatch(
                TestContent.Hero("hitter", initiative: 1), TestContent.BrawlerDeck(),
                TestContent.Hero("fragile", maxHp: 10), TestContent.BunkerDeck());
            match.TrySelect(match.ActivePlayer.Hand[0]);

            TestContent.EndRound(match);

            Assert.That(match.Players[0].Hp, Is.EqualTo(100 - 10));
        }

        [Test]
        public void EndRound_BothHeroesKnockedOut_IsDraw()
        {
            DeckDefinition jabs = TestContent.Deck(TestContent.Copies(TestContent.Jab, 8));
            Match match = TestContent.CreateMatch(
                TestContent.Hero("one", maxHp: 10, initiative: 1), jabs,
                TestContent.Hero("two", maxHp: 10), jabs);

            TestContent.PlayRound(match);

            Assert.That(match.IsOver, Is.True);
            Assert.That(match.Winner, Is.Null);
            Assert.That(match.Players[0].Hp, Is.EqualTo(0));
            Assert.That(match.Players[1].Hp, Is.EqualTo(0));
        }

        [Test]
        public void EndRound_OverkillDamage_HpStopsAtZero()
        {
            Match match = TestContent.CreateMatch(
                TestContent.Hero("hitter", initiative: 1), TestContent.BrawlerDeck(),
                TestContent.Hero("fragile", maxHp: 3), TestContent.BunkerDeck());
            match.TrySelect(match.ActivePlayer.Hand[0]);

            TestContent.EndRound(match);

            Assert.That(match.Players[1].Hp, Is.EqualTo(0));
        }

        [Test]
        public void MatchOver_NoMoreSelectionsOrTurns()
        {
            Match match = TestContent.BonkVsPudge();
            match.Forfeit(match.Players[1]);

            Assert.That(match.TrySelect(match.ActivePlayer.Hand[0]), Is.False);
            Assert.That(match.CanEndTurn(), Is.False);
        }

        [Test]
        public void Forfeit_Player_OpponentWins()
        {
            Match match = TestContent.BonkVsPudge();

            match.Forfeit(match.Players[0]);

            Assert.That(match.IsOver, Is.True);
            Assert.That(match.Winner, Is.SameAs(match.Players[1]));
        }

        [Test]
        public void RoundLimit_EqualHp_IsDraw()
        {
            HeroDefinition tank = TestContent.Hero("tank", maxHp: 1000);
            DeckDefinition jabs = TestContent.Deck(TestContent.Copies(TestContent.Jab, 8));
            Match match = TestContent.CreateMatch(tank, jabs, tank, jabs);

            PlayRounds(match, Rules.RoundLimit);

            Assert.That(match.IsOver, Is.True);
            Assert.That(match.Winner, Is.Null);
            Assert.That(match.Players[0].Hp, Is.EqualTo(1000 - Rules.RoundLimit * 10));
        }

        [Test]
        public void RoundLimit_HigherHp_Wins()
        {
            DeckDefinition jabs = TestContent.Deck(TestContent.Copies(TestContent.Jab, 8));
            Match match = TestContent.CreateMatch(
                TestContent.Hero("big", maxHp: 1000), jabs,
                TestContent.Hero("small", maxHp: 900), jabs);

            PlayRounds(match, Rules.RoundLimit);

            Assert.That(match.IsOver, Is.True);
            Assert.That(match.Winner, Is.SameAs(match.Players[0]));
        }

        [Test]
        public void RoundLimit_OneRoundBefore_MatchStillRunning()
        {
            HeroDefinition tank = TestContent.Hero("tank", maxHp: 1000);
            DeckDefinition jabs = TestContent.Deck(TestContent.Copies(TestContent.Jab, 8));
            Match match = TestContent.CreateMatch(tank, jabs, tank, jabs);

            PlayRounds(match, Rules.RoundLimit - 1);

            Assert.That(match.IsOver, Is.False);
            Assert.That(match.RoundNumber, Is.EqualTo(Rules.RoundLimit));
        }

        private static void PlayRounds(Match match, int rounds)
        {
            for (int round = 0; round < rounds; round++)
            {
                TestContent.PlayRound(match);
            }
        }
    }
}
