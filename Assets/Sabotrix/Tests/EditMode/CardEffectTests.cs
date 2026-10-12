using System.Linq;
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

            TestContent.EndRound(match);

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

            TestContent.EndRound(match);

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

            TestContent.EndRound(match);

            Assert.That(match.Players[1].Hp, Is.EqualTo(1000 - 20 - 10));
        }

        [Test]
        public void PotLidGuard_Resolves_GainsTwentyBlockBeforeOpponentsAttack()
        {
            Match match = TestContent.PlayerHolding(TestContent.Pudge, null, TestContent.Jab);
            PlayerState pudge = match.ActivePlayer;
            pudge.SpecialEnergy = 3;
            match.TrySelect(pudge.Ability);
            match.TrySelect(pudge.Hand[0]);

            TestContent.EndRound(match);

            Assert.That(match.LastRoundEvents.Single(e => e.Kind == MatchEventKind.BlockGained).Amount, Is.EqualTo(20));
            Assert.That(pudge.Hp, Is.EqualTo(110));
        }

        [Test]
        public void Poke_DealsDamage_EarnsOneExtraSpecialEnergy()
        {
            Match match = TestContent.PlayerHolding(TestContent.Poke);
            match.TrySelect(match.ActivePlayer.Hand[0]);

            TestContent.EndRound(match);

            Assert.That(match.Players[1].Hp, Is.EqualTo(1000 - 8));
            Assert.That(match.Players[0].SpecialEnergy, Is.EqualTo(2));
        }

        [Test]
        public void CounterStance_ResolvesFirst_DealsSixAndBlocksSixOfOpponentsAttack()
        {
            // The player has higher Initiative, so their Counter Stance resolves before the target's Jab (10).
            Match match = TestContent.PlayerHolding(TestContent.CounterStance);
            match.TrySelect(match.ActivePlayer.Hand[0]);

            TestContent.EndRound(match);

            Assert.That(match.Players[1].Hp, Is.EqualTo(1000 - 6));
            Assert.That(match.Players[0].Hp, Is.EqualTo(100 - (10 - 6)));
        }

        [Test]
        public void DoubleTap_HeroHasStrength_HitsTwiceWithStrengthAndEarnsSpecialEnergyPerHit()
        {
            Match match = TestContent.PlayerHolding(TestContent.Hero("strong", strength: 3, initiative: 1), null, TestContent.DoubleTap);
            match.TrySelect(match.ActivePlayer.Hand[0]);

            TestContent.EndRound(match);

            Assert.That(match.Players[1].Hp, Is.EqualTo(1000 - (8 + 3) * 2));
            Assert.That(match.Players[0].SpecialEnergy, Is.EqualTo(2));
        }

        [Test]
        public void SecondWind_Damaged_HealsEight()
        {
            Match match = TestContent.PlayerHolding(TestContent.SecondWind);
            match.ActivePlayer.Hp = 50;
            match.TrySelect(match.ActivePlayer.Hand[0]);

            TestContent.EndRound(match);

            // Heals 50 -> 58, then the target's Jab hits for 10.
            Assert.That(match.Players[0].Hp, Is.EqualTo(58 - 10));
            Assert.That(match.Players[1].Hp, Is.EqualTo(1000 - 10));
        }

        [Test]
        public void SecondWind_NearFullHp_HealCappedAtMaxHp()
        {
            Match match = TestContent.PlayerHolding(TestContent.SecondWind);
            match.ActivePlayer.Hp = 95;
            match.TrySelect(match.ActivePlayer.Hand[0]);

            TestContent.EndRound(match);

            // Heals 95 -> 100 (the cap), then the target's Jab hits for 10.
            Assert.That(match.Players[0].Hp, Is.EqualTo(100 - 10));
        }

        [Test]
        public void BigFinish_OpponentAboveThreshold_NoBonus()
        {
            Match match = TestContent.PlayerHolding(TestContent.BigFinish);
            match.TrySelect(match.ActivePlayer.Hand[0]);

            TestContent.EndRound(match);

            Assert.That(match.Players[1].Hp, Is.EqualTo(1000 - 20));
        }

        [Test]
        public void BigFinish_OpponentAtThreshold_AddsTenDamage()
        {
            // Block keeps the target alive so the extra damage shows: 30 damage - 10 Block = 20 HP lost.
            Match match = TestContent.PlayerHolding(TestContent.BigFinish);
            match.Opponent.Hp = 25;
            match.Opponent.Block = 10;
            match.TrySelect(match.ActivePlayer.Hand[0]);

            TestContent.EndRound(match);

            Assert.That(match.Players[1].Hp, Is.EqualTo(25 - 20));
        }

        [Test]
        public void BigFinish_EarlierAttackReachesThreshold_AddsTenDamage()
        {
            // Jab takes 35 HP down to 25, so Big Finish hits for 30 and knocks out the last 25.
            // Without the bonus it would deal only 20.
            Match match = TestContent.PlayerHolding(TestContent.Jab, TestContent.BigFinish);
            match.Opponent.Hp = 35;
            match.TrySelect(match.ActivePlayer.Hand[0]);
            match.TrySelect(match.ActivePlayer.Hand[1]);

            TestContent.EndRound(match);

            PlayerState target = match.Players[1];
            Assert.That(match.LastRoundEvents.Last(e => e.Kind == MatchEventKind.Damage && e.Player == target).Amount, Is.EqualTo(25));
            Assert.That(match.Players[1].Hp, Is.EqualTo(0));
        }
    }
}
