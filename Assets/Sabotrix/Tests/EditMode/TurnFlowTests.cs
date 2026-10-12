using System.Linq;
using NUnit.Framework;
using Sabotrix.Core;

namespace Sabotrix.Tests.EditMode
{
    public class TurnFlowTests
    {
        [Test]
        public void EndTurn_FirstPick_PassesPickWithoutResolving()
        {
            Match match = TestContent.BonkVsPudge();

            TestContent.PlayAnyAttack(match);

            Assert.That(match.ActivePlayer.Hero, Is.SameAs(TestContent.Pudge));
            Assert.That(match.IsFirstPick, Is.False);
            Assert.That(match.RoundNumber, Is.EqualTo(1));
            Assert.That(match.Players[1].Hp, Is.EqualTo(110));
            Assert.That(match.LastRoundEvents, Is.Empty);
        }

        [Test]
        public void EndTurn_SecondPick_ResolvesAndStartsNextRound()
        {
            Match match = TestContent.BonkVsPudge();

            TestContent.PlayRound(match);

            Assert.That(match.RoundNumber, Is.EqualTo(2));
            Assert.That(match.ActivePlayer.Hero, Is.SameAs(TestContent.Bonk));
            Assert.That(match.IsFirstPick, Is.True);
            Assert.That(match.LastRoundEvents, Is.Not.Empty);
        }

        [Test]
        public void EndRound_Attack_DamagesOpponent()
        {
            Match match = TestContent.BonkVsPudge();
            match.TrySelect(match.ActivePlayer.Hand[0]);

            TestContent.EndRound(match);

            Assert.That(match.Players[1].Hp, Is.EqualTo(110 - 13));
        }

        [Test]
        public void EndRound_BothPlayersAttack_BothTakeDamage()
        {
            // Bonk's Jab (10 + 3 Strength) and Pudge's Jab (10) both land.
            Match match = TestContent.BonkVsPudge();

            TestContent.PlayRound(match);

            Assert.That(match.Players[0].Hp, Is.EqualTo(80 - 10));
            Assert.That(match.Players[1].Hp, Is.EqualTo(110 - 13));
        }

        [Test]
        public void EndRound_BothPicked_AttacksForBothResolveBeforeSabotage()
        {
            // The player (higher Initiative) selects Blackout first, but it waits for both players' Attacks.
            Match match = TestContent.PlayerHolding(TestContent.Blackout, TestContent.Jab);
            PlayerState player = match.Players[0];
            PlayerState target = match.Players[1];
            match.TrySelect(player.Hand[0]);
            match.TrySelect(player.Hand[1]);

            TestContent.EndRound(match);

            MatchEvent[] played = match.LastRoundEvents.Where(e => e.Kind == MatchEventKind.CardPlayed).ToArray();
            Assert.That(played.Select(e => e.Player), Is.EqualTo(new[] { player, target, player }));
            Assert.That(played.Select(e => e.Card), Is.EqualTo(new[] { TestContent.Jab, TestContent.Jab, TestContent.Blackout }));
        }

        [Test]
        public void EndRound_PlayedCard_LeavesHandAndGoesToBottomOfDeck()
        {
            Match match = TestContent.BonkVsPudge();
            PlayerState bonk = match.ActivePlayer;
            CardInstance jab = bonk.Hand[0];
            match.TrySelect(jab);

            TestContent.EndRound(match);

            Assert.That(bonk.Hand, Has.No.Member(jab));
            Assert.That(bonk.Deck.Last(), Is.SameAs(jab));
            Assert.That(bonk.Selected, Is.Empty);
        }

        [Test]
        public void EndRound_BasicAttack_DealsFiveAndIsNotAddedToDeck()
        {
            Match match = TestContent.BonkVsPudge();
            PlayerState bonk = match.ActivePlayer;
            int deckCount = bonk.Deck.Count;
            bonk.Energy = 0;
            match.TrySelect(bonk.BasicAttack);

            TestContent.EndRound(match);

            // The deck only shrinks by the card Bonk draws for round 2.
            Assert.That(match.Players[1].Hp, Is.EqualTo(110 - 5));
            Assert.That(bonk.Deck.Count, Is.EqualTo(deckCount - 1));
        }

        [Test]
        public void EndRound_SabotageSelectedBeforeAttack_CyclesAfterAttack()
        {
            // 8 cards, so round 2's draw comes off the top and leaves the cycled cards at the bottom.
            Match match = TestContent.CreateMatch(
                TestContent.Hero("saboteur", initiative: 1),
                TestContent.Deck(new[] { TestContent.Blackout }.Concat(TestContent.Copies(TestContent.Jab, 7))),
                TestContent.Hero("target"), TestContent.BrawlerDeck());
            PlayerState player = match.ActivePlayer;
            CardInstance blackout = player.Hand[0];
            CardInstance jab = player.Hand[1];
            match.TrySelect(blackout);
            match.TrySelect(jab);

            TestContent.EndRound(match);

            Assert.That(player.Deck.Skip(player.Deck.Count - 2), Is.EqualTo(new[] { jab, blackout }));
            Assert.That(player.SabotagesPlayed, Is.EqualTo(1));
        }

        [Test]
        public void StartRound_BothPlayers_DrawOne()
        {
            Match match = TestContent.BonkVsPudge();

            TestContent.PlayRound(match);

            // Each played one card and drew one for round 2.
            Assert.That(match.Players[0].Hand.Count, Is.EqualTo(Rules.StartingHandSize + 1));
            Assert.That(match.Players[1].Hand.Count, Is.EqualTo(Rules.StartingHandSize + 1));
        }

        [Test]
        public void StartRound_EnergyRegen_AddsRegen()
        {
            Match match = TestContent.BonkVsPudge();
            PlayerState bonk = match.ActivePlayer;
            match.TrySelect(bonk.Hand[4]);
            match.TrySelect(bonk.Hand[2]);

            TestContent.EndRound(match);

            Assert.That(bonk.Energy, Is.EqualTo(1 + 3));
            Assert.That(bonk.RoundStartEnergy, Is.EqualTo(1 + 3));
        }

        [Test]
        public void StartRound_EnergyRegen_CappedAtEnergyMax()
        {
            Match match = TestContent.BonkVsPudge();
            PlayerState bonk = match.ActivePlayer;

            TestContent.PlayRound(match);

            Assert.That(bonk.Energy, Is.EqualTo(6));
        }

        [Test]
        public void StartRound_HandFull_SkipsDraw()
        {
            // Energy Max 2 can't afford Body Slam (3), so each round uses Basic Attack and the hand grows.
            HeroDefinition weak = TestContent.Hero("weak", maxHp: 1000, energyMax: 2, energyRegen: 0);
            DeckDefinition slams = TestContent.Deck(TestContent.Copies(TestContent.BodySlam, 8));
            Match match = TestContent.CreateMatch(weak, slams, weak, slams);
            PlayerState player = match.Players[0];

            for (int round = 0; round < 4; round++)
            {
                TestContent.PlayRound(match);
            }

            Assert.That(player.Hand.Count, Is.EqualTo(Rules.MaxHandSize));
            Assert.That(player.Deck.Count, Is.EqualTo(8 - Rules.MaxHandSize));
        }
    }
}
