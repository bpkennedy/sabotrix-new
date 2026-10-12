using NUnit.Framework;
using Sabotrix.Core;

namespace Sabotrix.Tests.EditMode
{
    // Each Sabotage card with and without a backfire.
    // IdentityRandom(0.99) never backfires; IdentityRandom(0.0) always backfires.
    public class SabotageTests
    {
        private const double NoBackfire = 0.99;
        private const double Backfire = 0.0;

        private static Match SabotageThenJab(CardDefinition sabotage, double roll)
        {
            Match match = TestContent.PlayerHolding(TestContent.Hero("player", initiative: 1), new IdentityRandom(roll), sabotage);
            match.TrySelect(match.ActivePlayer.Hand[0]);
            match.TrySelect(match.ActivePlayer.Hand[1]);
            return match;
        }

        [Test]
        public void Sabotage_RollEqualsReversalChance_Backfires()
        {
            Match match = SabotageThenJab(TestContent.BeholderDrop, 0.45);

            match.EndTurn();

            Assert.That(match.Players[0].Hp, Is.EqualTo(100 - 10));
        }

        [Test]
        public void Blackout_NoBackfire_OpponentBlindForTheirNextTurnOnly()
        {
            Match match = SabotageThenJab(TestContent.Blackout, NoBackfire);
            PlayerState target = match.Players[1];

            match.EndTurn();
            Assert.That(target.IsBlind, Is.True);

            TestContent.PlayAnyAttack(match);
            Assert.That(target.IsBlind, Is.False);
        }

        [Test]
        public void Blackout_Backfire_PlayerBlindOnTheirNextTurn()
        {
            Match match = SabotageThenJab(TestContent.Blackout, Backfire);
            PlayerState player = match.Players[0];

            match.EndTurn();
            Assert.That(player.IsBlind, Is.False);
            Assert.That(match.Players[1].IsBlind, Is.False);

            TestContent.PlayAnyAttack(match);
            Assert.That(player.IsBlind, Is.True);
        }

        [Test]
        public void ShortCircuit_NoBackfire_OpponentLosesTwoAndPlayerGainsTwo()
        {
            Match match = TestContent.PlayerHolding(TestContent.Hero("player", initiative: 1), new IdentityRandom(NoBackfire), TestContent.ShortCircuit);
            PlayerState player = match.ActivePlayer;
            PlayerState target = match.Opponent;
            foreach (CardInstance card in player.Hand)
            {
                match.TrySelect(card);
            }

            Assert.That(player.Energy, Is.EqualTo(0));
            match.EndTurn();
            Assert.That(target.Energy, Is.EqualTo(6 - 2));

            TestContent.PlayAnyAttack(match);
            Assert.That(player.Energy, Is.EqualTo(0 + 3 + 2));
        }

        [Test]
        public void ShortCircuit_Backfire_PlayerLosesTwoAndOpponentUnaffected()
        {
            Match match = TestContent.PlayerHolding(TestContent.Hero("player", initiative: 1), new IdentityRandom(Backfire), TestContent.ShortCircuit);
            PlayerState player = match.ActivePlayer;
            PlayerState target = match.Opponent;
            foreach (CardInstance card in player.Hand)
            {
                match.TrySelect(card);
            }

            match.EndTurn();
            Assert.That(target.Energy, Is.EqualTo(6));

            TestContent.PlayAnyAttack(match);
            Assert.That(player.Energy, Is.EqualTo(0 + 3 - 2));
        }

        [Test]
        public void BeholderDrop_NoBackfire_OpponentTakesTwenty()
        {
            Match match = SabotageThenJab(TestContent.BeholderDrop, NoBackfire);

            match.EndTurn();

            Assert.That(match.Players[1].Hp, Is.EqualTo(1000 - 10 - 20));
            Assert.That(match.Players[0].Hp, Is.EqualTo(100));
        }

        [Test]
        public void BeholderDrop_Backfire_PlayerTakesTen()
        {
            Match match = SabotageThenJab(TestContent.BeholderDrop, Backfire);

            match.EndTurn();

            Assert.That(match.Players[1].Hp, Is.EqualTo(1000 - 10));
            Assert.That(match.Players[0].Hp, Is.EqualTo(100 - 10));
        }

        [Test]
        public void BeholderDrop_BackfireKnocksOutPlayer_OpponentWins()
        {
            Match match = SabotageThenJab(TestContent.BeholderDrop, Backfire);
            match.ActivePlayer.Hp = 10;

            match.EndTurn();

            Assert.That(match.IsOver, Is.True);
            Assert.That(match.Winner, Is.SameAs(match.Players[1]));
        }

        [Test]
        public void SpilledDrink_NoBackfire_OpponentAttackCardsCostOneMoreNextTurn()
        {
            Match match = SabotageThenJab(TestContent.SpilledDrink, NoBackfire);
            PlayerState target = match.Players[1];

            match.EndTurn();

            Assert.That(match.CostOf(target.Hand[0]), Is.EqualTo(1 + 1));
            Assert.That(match.CostOf(target.BasicAttack), Is.EqualTo(0));

            TestContent.PlayAnyAttack(match);
            Assert.That(target.AttackCostIncrease, Is.EqualTo(0));
        }

        [Test]
        public void SpilledDrink_Backfire_PlayerAttackCardsCostOneMoreOnTheirNextTurn()
        {
            Match match = SabotageThenJab(TestContent.SpilledDrink, Backfire);
            PlayerState player = match.Players[0];

            match.EndTurn();
            Assert.That(match.CostOf(match.Players[1].Hand[0]), Is.EqualTo(1));

            TestContent.PlayAnyAttack(match);
            Assert.That(player.AttackCostIncrease, Is.EqualTo(1));
            Assert.That(match.CostOf(player.Hand[0]), Is.EqualTo(1 + 1));
        }
    }
}
