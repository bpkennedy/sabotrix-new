using NUnit.Framework;
using Sabotrix.Core;

namespace Sabotrix.Tests.EditMode
{
    // Each Sabotage card with and without a backfire.
    // IdentityRandom(0.99) never backfires; IdentityRandom(0.0) always backfires.
    // The target Jabs the player for 10 every round, in the Attack phase before any Sabotage.
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

            TestContent.EndRound(match);

            Assert.That(match.Players[0].Hp, Is.EqualTo(100 - 10 - 10));
        }

        [Test]
        public void Blackout_NoBackfire_EveryoneBlindForNextRoundOnly()
        {
            Match match = SabotageThenJab(TestContent.Blackout, NoBackfire);

            TestContent.EndRound(match);
            Assert.That(match.Players[0].IsBlind, Is.True);
            Assert.That(match.Players[1].IsBlind, Is.True);

            TestContent.PlayRound(match);
            Assert.That(match.Players[0].IsBlind, Is.False);
            Assert.That(match.Players[1].IsBlind, Is.False);
        }

        [Test]
        public void Blackout_Backfire_OnlyPlayerBlindNextRound()
        {
            Match match = SabotageThenJab(TestContent.Blackout, Backfire);

            TestContent.EndRound(match);
            Assert.That(match.Players[0].IsBlind, Is.True);
            Assert.That(match.Players[1].IsBlind, Is.False);

            TestContent.PlayRound(match);
            Assert.That(match.Players[0].IsBlind, Is.False);
        }

        [Test]
        public void ShortCircuit_NoBackfire_OpponentLosesTwoAndPlayerGainsTwoNextRound()
        {
            Match match = TestContent.PlayerHolding(TestContent.Hero("player", initiative: 1), new IdentityRandom(NoBackfire), TestContent.ShortCircuit);
            PlayerState player = match.ActivePlayer;
            PlayerState target = match.Opponent;
            foreach (CardInstance card in player.Hand)
            {
                match.TrySelect(card);
            }

            Assert.That(player.Energy, Is.EqualTo(0));
            TestContent.EndRound(match);

            // Target: 6 - 1 (Jab) + 3 regen, capped at 6, then - 2.
            Assert.That(target.Energy, Is.EqualTo(6 - 2));
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

            TestContent.EndRound(match);

            Assert.That(target.Energy, Is.EqualTo(6));
            Assert.That(player.Energy, Is.EqualTo(0 + 3 - 2));
        }

        [Test]
        public void BeholderDrop_NoBackfire_OpponentTakesTwenty()
        {
            Match match = SabotageThenJab(TestContent.BeholderDrop, NoBackfire);

            TestContent.EndRound(match);

            Assert.That(match.Players[1].Hp, Is.EqualTo(1000 - 10 - 20));
            Assert.That(match.Players[0].Hp, Is.EqualTo(100 - 10));
        }

        [Test]
        public void BeholderDrop_Backfire_PlayerTakesTen()
        {
            Match match = SabotageThenJab(TestContent.BeholderDrop, Backfire);

            TestContent.EndRound(match);

            Assert.That(match.Players[1].Hp, Is.EqualTo(1000 - 10));
            Assert.That(match.Players[0].Hp, Is.EqualTo(100 - 10 - 10));
        }

        [Test]
        public void BeholderDrop_BackfireKnocksOutPlayer_OpponentWins()
        {
            // 15 HP survives the target's Jab (10), then the backfire (10) finishes the player off.
            Match match = SabotageThenJab(TestContent.BeholderDrop, Backfire);
            match.ActivePlayer.Hp = 15;

            TestContent.EndRound(match);

            Assert.That(match.IsOver, Is.True);
            Assert.That(match.Winner, Is.SameAs(match.Players[1]));
        }

        [Test]
        public void SpilledDrink_NoBackfire_OpponentAttackCardsCostOneMoreNextRound()
        {
            Match match = SabotageThenJab(TestContent.SpilledDrink, NoBackfire);
            PlayerState target = match.Players[1];
            TestContent.EndRound(match);

            // The player picks first in round 2, then it's the target's pick.
            TestContent.PlayAnyAttack(match);
            Assert.That(match.CostOf(target.Hand[0]), Is.EqualTo(1 + 1));
            Assert.That(match.CostOf(target.BasicAttack), Is.EqualTo(0));

            TestContent.PlayAnyAttack(match);
            Assert.That(target.AttackCostIncrease, Is.EqualTo(0));
        }

        [Test]
        public void SpilledDrink_Backfire_PlayerAttackCardsCostOneMoreNextRound()
        {
            Match match = SabotageThenJab(TestContent.SpilledDrink, Backfire);
            PlayerState player = match.Players[0];

            TestContent.EndRound(match);

            Assert.That(player.AttackCostIncrease, Is.EqualTo(1));
            Assert.That(match.CostOf(player.Hand[0]), Is.EqualTo(1 + 1));
            Assert.That(match.Players[1].AttackCostIncrease, Is.EqualTo(0));
        }
    }
}
