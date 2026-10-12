using NUnit.Framework;
using Sabotrix.Core;

namespace Sabotrix.Tests.EditMode
{
    // One or more tests per Ability and Attack card in GDD Section 10.
    public class CardEffectTests
    {
        [TestCase("jab", 10)]
        [TestCase("swing", 18)]
        [TestCase("body-slam", 25)]
        public void PlainAttack_NoStrength_DealsPrintedDamage(string cardId, int damage)
        {
            CardDefinition card = cardId == "jab" ? TestContent.Jab : cardId == "swing" ? TestContent.Swing : TestContent.BodySlam;
            Match match = TestContent.PlayerHolding(card);
            match.TrySelect(match.ActivePlayer.Hand[0]);

            match.EndTurn();

            Assert.That(match.Players[1].Hp, Is.EqualTo(1000 - damage));
        }

        [Test]
        public void Haymaker_BeforeAttack_DoublesCardDamagePlusStrength()
        {
            Match match = TestContent.PlayerHolding(TestContent.Hero("strong", strength: 3, initiative: 1), null, TestContent.Jab);
            PlayerState player = match.ActivePlayer;
            player.SpecialEnergy = 3;
            match.TrySelect(player.Ability);
            match.TrySelect(player.Hand[0]);

            match.EndTurn();

            Assert.That(match.Players[1].Hp, Is.EqualTo(1000 - (10 + 3) * 2));
        }

        [Test]
        public void Haymaker_TwoAttacks_OnlyFirstIsDoubled()
        {
            Match match = TestContent.PlayerHolding(TestContent.Jab, TestContent.Jab);
            PlayerState player = match.ActivePlayer;
            player.SpecialEnergy = 3;
            match.TrySelect(player.Ability);
            match.TrySelect(player.Hand[0]);
            match.TrySelect(player.Hand[1]);

            match.EndTurn();

            Assert.That(match.Players[1].Hp, Is.EqualTo(1000 - 20 - 10));
        }

        [Test]
        public void PotLidGuard_Resolves_GainsTwentyBlock()
        {
            Match match = TestContent.PlayerHolding(TestContent.Pudge, null, TestContent.Jab);
            PlayerState pudge = match.ActivePlayer;
            pudge.SpecialEnergy = 3;
            match.TrySelect(pudge.Ability);
            match.TrySelect(pudge.Hand[0]);

            match.EndTurn();

            Assert.That(pudge.Block, Is.EqualTo(20));
        }

        [Test]
        public void Poke_DealsDamage_EarnsOneExtraSpecialEnergy()
        {
            Match match = TestContent.PlayerHolding(TestContent.Poke);
            match.TrySelect(match.ActivePlayer.Hand[0]);

            match.EndTurn();

            Assert.That(match.Players[1].Hp, Is.EqualTo(1000 - 8));
            Assert.That(match.Players[0].SpecialEnergy, Is.EqualTo(2));
        }

        [Test]
        public void CounterStance_Resolves_DealsSixAndGainsSixBlock()
        {
            Match match = TestContent.PlayerHolding(TestContent.CounterStance);
            match.TrySelect(match.ActivePlayer.Hand[0]);

            match.EndTurn();

            Assert.That(match.Players[1].Hp, Is.EqualTo(1000 - 6));
            Assert.That(match.Players[0].Block, Is.EqualTo(6));
        }

        [Test]
        public void DoubleTap_HeroHasStrength_HitsTwiceWithStrengthAndEarnsSpecialEnergyPerHit()
        {
            Match match = TestContent.PlayerHolding(TestContent.Hero("strong", strength: 3, initiative: 1), null, TestContent.DoubleTap);
            match.TrySelect(match.ActivePlayer.Hand[0]);

            match.EndTurn();

            Assert.That(match.Players[1].Hp, Is.EqualTo(1000 - (8 + 3) * 2));
            Assert.That(match.Players[0].SpecialEnergy, Is.EqualTo(2));
        }

        [Test]
        public void SecondWind_Damaged_HealsEight()
        {
            Match match = TestContent.PlayerHolding(TestContent.SecondWind);
            match.ActivePlayer.Hp = 50;
            match.TrySelect(match.ActivePlayer.Hand[0]);

            match.EndTurn();

            Assert.That(match.Players[0].Hp, Is.EqualTo(58));
            Assert.That(match.Players[1].Hp, Is.EqualTo(1000 - 10));
        }

        [Test]
        public void SecondWind_NearFullHp_HealCappedAtMaxHp()
        {
            Match match = TestContent.PlayerHolding(TestContent.SecondWind);
            match.ActivePlayer.Hp = 95;
            match.TrySelect(match.ActivePlayer.Hand[0]);

            match.EndTurn();

            Assert.That(match.Players[0].Hp, Is.EqualTo(100));
        }

        [Test]
        public void BigFinish_OpponentAboveThreshold_NoBonus()
        {
            Match match = TestContent.PlayerHolding(TestContent.BigFinish);
            match.TrySelect(match.ActivePlayer.Hand[0]);

            match.EndTurn();

            Assert.That(match.Players[1].Hp, Is.EqualTo(1000 - 20));
        }

        [Test]
        public void BigFinish_OpponentAtThreshold_AddsTenDamage()
        {
            Match match = TestContent.PlayerHolding(TestContent.BigFinish);
            match.Opponent.Hp = 50;
            match.TrySelect(match.ActivePlayer.Hand[0]);

            match.EndTurn();

            Assert.That(match.Players[1].Hp, Is.EqualTo(50 - 30));
        }

        [Test]
        public void BigFinish_EarlierAttackReachesThreshold_AddsTenDamage()
        {
            Match match = TestContent.PlayerHolding(TestContent.Jab, TestContent.BigFinish);
            match.Opponent.Hp = 60;
            match.TrySelect(match.ActivePlayer.Hand[0]);
            match.TrySelect(match.ActivePlayer.Hand[1]);

            match.EndTurn();

            Assert.That(match.Players[1].Hp, Is.EqualTo(60 - 10 - 30));
        }
    }
}
