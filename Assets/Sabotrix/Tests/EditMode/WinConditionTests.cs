using NUnit.Framework;
using Sabotrix.Core;

namespace Sabotrix.Tests.EditMode
{
    public class WinConditionTests
    {
        [Test]
        public void EndTurn_OpponentHpReachesZero_ActivePlayerWins()
        {
            Match match = TestContent.CreateMatch(
                TestContent.Hero("hitter", initiative: 1), TestContent.BrawlerDeck(),
                TestContent.Hero("fragile", maxHp: 10), TestContent.BunkerDeck());
            PlayerState hitter = match.ActivePlayer;
            match.TrySelect(hitter.Hand[0]);

            match.EndTurn();

            Assert.That(match.IsOver, Is.True);
            Assert.That(match.Winner, Is.SameAs(hitter));
            Assert.That(match.Opponent.Hp, Is.EqualTo(0));
        }

        [Test]
        public void EndTurn_OverkillDamage_HpStopsAtZero()
        {
            Match match = TestContent.CreateMatch(
                TestContent.Hero("hitter", initiative: 1), TestContent.BrawlerDeck(),
                TestContent.Hero("fragile", maxHp: 3), TestContent.BunkerDeck());
            match.TrySelect(match.ActivePlayer.Hand[0]);

            match.EndTurn();

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
        public void TurnLimit_EqualHp_IsDraw()
        {
            HeroDefinition tank = TestContent.Hero("tank", maxHp: 1000);
            DeckDefinition jabs = TestContent.Deck(TestContent.Copies(TestContent.Jab, 8));
            Match match = TestContent.CreateMatch(tank, jabs, tank, jabs);

            PlayTurns(match, Rules.TurnLimit);

            Assert.That(match.IsOver, Is.True);
            Assert.That(match.Winner, Is.Null);
            Assert.That(match.Players[0].Hp, Is.EqualTo(1000 - Rules.TurnLimit / 2 * 10));
        }

        [Test]
        public void TurnLimit_HigherHp_Wins()
        {
            DeckDefinition jabs = TestContent.Deck(TestContent.Copies(TestContent.Jab, 8));
            Match match = TestContent.CreateMatch(
                TestContent.Hero("big", maxHp: 1000), jabs,
                TestContent.Hero("small", maxHp: 900), jabs);

            PlayTurns(match, Rules.TurnLimit);

            Assert.That(match.IsOver, Is.True);
            Assert.That(match.Winner, Is.SameAs(match.Players[0]));
        }

        [Test]
        public void TurnLimit_OneTurnBefore_MatchStillRunning()
        {
            HeroDefinition tank = TestContent.Hero("tank", maxHp: 1000);
            DeckDefinition jabs = TestContent.Deck(TestContent.Copies(TestContent.Jab, 8));
            Match match = TestContent.CreateMatch(tank, jabs, tank, jabs);

            PlayTurns(match, Rules.TurnLimit - 1);

            Assert.That(match.IsOver, Is.False);
            Assert.That(match.TurnNumber, Is.EqualTo(Rules.TurnLimit));
        }

        private static void PlayTurns(Match match, int turns)
        {
            for (int turn = 0; turn < turns; turn++)
            {
                TestContent.PlayAnyAttack(match);
            }
        }
    }
}
