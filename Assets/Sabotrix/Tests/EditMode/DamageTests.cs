using NUnit.Framework;
using Sabotrix.Core;

namespace Sabotrix.Tests.EditMode
{
    public class DamageTests
    {
        private static HeroDefinition StrongHero() => TestContent.Hero("strong", strength: 3, initiative: 1);

        [Test]
        public void Attack_HeroHasStrength_AddsStrengthToCardDamage()
        {
            Match match = TestContent.PlayerHolding(StrongHero(), null, TestContent.Jab);
            match.TrySelect(match.ActivePlayer.Hand[0]);

            match.EndTurn();

            Assert.That(match.Players[1].Hp, Is.EqualTo(1000 - 13));
        }

        [Test]
        public void BasicAttack_HeroHasStrength_NoStrengthBonus()
        {
            Match match = TestContent.PlayerHolding(StrongHero(), null, TestContent.Jab);
            match.ActivePlayer.Energy = 0;
            match.TrySelect(match.ActivePlayer.BasicAttack);

            match.EndTurn();

            Assert.That(match.Players[1].Hp, Is.EqualTo(1000 - 5));
        }

        [Test]
        public void Attack_TargetHasBlock_BlockAbsorbsFirst()
        {
            Match match = TestContent.PlayerHolding(TestContent.Jab);
            match.Opponent.Block = 6;
            match.TrySelect(match.ActivePlayer.Hand[0]);

            match.EndTurn();

            Assert.That(match.Players[1].Hp, Is.EqualTo(1000 - 4));
            Assert.That(match.Players[1].Block, Is.EqualTo(0));
        }

        [Test]
        public void Attack_FullyBlocked_NoHpLostAndNoSpecialEnergy()
        {
            Match match = TestContent.PlayerHolding(TestContent.Jab);
            match.Opponent.Block = 20;
            match.TrySelect(match.ActivePlayer.Hand[0]);

            match.EndTurn();

            Assert.That(match.Players[1].Hp, Is.EqualTo(1000));
            Assert.That(match.Players[0].SpecialEnergy, Is.EqualTo(0));
        }

        [Test]
        public void Attack_DealsDamage_GainsSpecialEnergy()
        {
            Match match = TestContent.PlayerHolding(TestContent.Jab);
            match.TrySelect(match.ActivePlayer.Hand[0]);

            match.EndTurn();

            Assert.That(match.Players[0].SpecialEnergy, Is.EqualTo(1));
        }

        [Test]
        public void BasicAttack_DealsDamage_GainsSpecialEnergy()
        {
            Match match = TestContent.PlayerHolding(TestContent.Jab);
            match.ActivePlayer.Energy = 0;
            match.TrySelect(match.ActivePlayer.BasicAttack);

            match.EndTurn();

            Assert.That(match.Players[0].SpecialEnergy, Is.EqualTo(1));
        }

        [Test]
        public void SpecialEnergy_AtMax_StaysAtMax()
        {
            Match match = TestContent.PlayerHolding(TestContent.Jab);
            match.ActivePlayer.SpecialEnergy = Rules.SpecialEnergyMax;
            match.TrySelect(match.ActivePlayer.Hand[0]);

            match.EndTurn();

            Assert.That(match.Players[0].SpecialEnergy, Is.EqualTo(Rules.SpecialEnergyMax));
        }

        [Test]
        public void Block_Unused_DisappearsAtStartOfOwnersNextTurn()
        {
            Match match = TestContent.PlayerHolding(TestContent.Jab);
            PlayerState player = match.ActivePlayer;
            player.Block = 50;
            match.TrySelect(player.Hand[0]);
            match.EndTurn();

            TestContent.PlayAnyAttack(match);

            Assert.That(player.Hp, Is.EqualTo(100));
            Assert.That(player.Block, Is.EqualTo(0));
        }
    }
}
