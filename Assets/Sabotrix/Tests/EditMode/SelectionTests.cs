using NUnit.Framework;
using Sabotrix.Core;

namespace Sabotrix.Tests.EditMode
{
    public class SelectionTests
    {
        [Test]
        public void TrySelect_AffordableAttack_ReservesEnergy()
        {
            Match match = TestContent.BonkVsPudge();
            CardInstance swing = match.ActivePlayer.Hand[2];

            bool selected = match.TrySelect(swing);

            Assert.That(selected, Is.True);
            Assert.That(match.ActivePlayer.Energy, Is.EqualTo(4));
            Assert.That(match.ActivePlayer.Selected, Is.EqualTo(new[] { swing }));
        }

        [Test]
        public void TrySelect_UnaffordableAttack_ReturnsFalse()
        {
            Match match = TestContent.BonkVsPudge();
            match.ActivePlayer.Energy = 0;

            Assert.That(match.TrySelect(match.ActivePlayer.Hand[0]), Is.False);
        }

        [Test]
        public void TrySelect_SameCardTwice_SecondReturnsFalse()
        {
            Match match = TestContent.BonkVsPudge();
            CardInstance jab = match.ActivePlayer.Hand[0];
            match.TrySelect(jab);

            Assert.That(match.TrySelect(jab), Is.False);
            Assert.That(match.ActivePlayer.Energy, Is.EqualTo(5));
        }

        [Test]
        public void TrySelect_OpponentsCard_ReturnsFalse()
        {
            Match match = TestContent.BonkVsPudge();

            Assert.That(match.TrySelect(match.Opponent.Hand[0]), Is.False);
        }

        [Test]
        public void TrySelect_CardStillInDeck_ReturnsFalse()
        {
            Match match = TestContent.BonkVsPudge();

            Assert.That(match.TrySelect(match.ActivePlayer.Deck[0]), Is.False);
        }

        [Test]
        public void TrySelect_SecondSabotage_ReturnsFalse()
        {
            Match match = SabotageFirstMatch(TestContent.Blackout, TestContent.ShortCircuit);
            PlayerState player = match.ActivePlayer;

            Assert.That(match.TrySelect(player.Hand[0]), Is.True);
            Assert.That(match.TrySelect(player.Hand[1]), Is.False);
        }

        [Test]
        public void TrySelect_ThreeSabotagesAlreadyPlayed_SabotageIsDead()
        {
            Match match = SabotageFirstMatch(TestContent.Blackout);
            match.ActivePlayer.SabotagesPlayed = Rules.MaxSabotagesPerMatch;

            Assert.That(match.TrySelect(match.ActivePlayer.Hand[0]), Is.False);
        }

        [Test]
        public void TrySelect_AbilityWithEnoughSpecialEnergy_ReservesSpecialEnergy()
        {
            Match match = TestContent.BonkVsPudge();
            match.ActivePlayer.SpecialEnergy = 3;

            Assert.That(match.TrySelect(match.ActivePlayer.Ability), Is.True);
            Assert.That(match.ActivePlayer.SpecialEnergy, Is.EqualTo(0));
            Assert.That(match.ActivePlayer.Energy, Is.EqualTo(6));
        }

        [Test]
        public void TrySelect_AbilityWithoutEnoughSpecialEnergy_ReturnsFalse()
        {
            Match match = TestContent.BonkVsPudge();
            match.ActivePlayer.SpecialEnergy = 2;

            Assert.That(match.TrySelect(match.ActivePlayer.Ability), Is.False);
        }

        [Test]
        public void TrySelect_BasicAttackWhileAttackAffordable_ReturnsFalse()
        {
            Match match = TestContent.BonkVsPudge();

            Assert.That(match.TrySelect(match.ActivePlayer.BasicAttack), Is.False);
        }

        [Test]
        public void TrySelect_BasicAttackWhenNoAttackAffordable_ReturnsTrue()
        {
            Match match = TestContent.BonkVsPudge();
            match.ActivePlayer.Energy = 0;

            Assert.That(match.TrySelect(match.ActivePlayer.BasicAttack), Is.True);
            Assert.That(match.CanEndTurn(), Is.True);
        }

        [Test]
        public void TrySelect_BasicAttackAfterAttackSelected_ReturnsFalse()
        {
            Match match = TestContent.BonkVsPudge();
            match.TrySelect(match.ActivePlayer.Hand[4]);
            match.TrySelect(match.ActivePlayer.Hand[5]);

            Assert.That(match.ActivePlayer.Energy, Is.EqualTo(0));
            Assert.That(match.TrySelect(match.ActivePlayer.BasicAttack), Is.False);
        }

        [Test]
        public void TryWithdraw_SelectedCard_RefundsEnergyAndUnselects()
        {
            Match match = TestContent.BonkVsPudge();
            CardInstance swing = match.ActivePlayer.Hand[2];
            match.TrySelect(swing);

            Assert.That(match.TryWithdraw(swing), Is.True);
            Assert.That(match.ActivePlayer.Energy, Is.EqualTo(6));
            Assert.That(match.ActivePlayer.Selected, Is.Empty);
        }

        [Test]
        public void TryWithdraw_Ability_RefundsSpecialEnergy()
        {
            Match match = TestContent.BonkVsPudge();
            match.ActivePlayer.SpecialEnergy = 3;
            match.TrySelect(match.ActivePlayer.Ability);

            Assert.That(match.TryWithdraw(match.ActivePlayer.Ability), Is.True);
            Assert.That(match.ActivePlayer.SpecialEnergy, Is.EqualTo(3));
        }

        [Test]
        public void TryWithdraw_UnselectedCard_ReturnsFalse()
        {
            Match match = TestContent.BonkVsPudge();

            Assert.That(match.TryWithdraw(match.ActivePlayer.Hand[0]), Is.False);
        }

        [Test]
        public void CanEndTurn_NothingSelected_IsFalse()
        {
            Match match = TestContent.BonkVsPudge();

            Assert.That(match.CanEndTurn(), Is.False);
            Assert.Throws<System.InvalidOperationException>(() => match.EndTurn());
        }

        [Test]
        public void CanEndTurn_OnlySabotageSelected_IsFalse()
        {
            Match match = SabotageFirstMatch(TestContent.Blackout);
            match.TrySelect(match.ActivePlayer.Hand[0]);

            Assert.That(match.CanEndTurn(), Is.False);
        }

        [Test]
        public void CanEndTurn_AttackSelected_IsTrue()
        {
            Match match = TestContent.BonkVsPudge();
            match.TrySelect(match.ActivePlayer.Hand[0]);

            Assert.That(match.CanEndTurn(), Is.True);
        }

        // Player one (higher Initiative) holds the given Sabotage cards first, then Jabs.
        private static Match SabotageFirstMatch(params CardDefinition[] sabotages)
        {
            CardDefinition[] cards = new CardDefinition[sabotages.Length + 5];
            sabotages.CopyTo(cards, 0);
            for (int i = sabotages.Length; i < cards.Length; i++)
            {
                cards[i] = TestContent.Jab;
            }

            return TestContent.CreateMatch(
                TestContent.Hero("saboteur", initiative: 1), TestContent.Deck(cards),
                TestContent.Hero("target"), TestContent.BrawlerDeck());
        }
    }
}
