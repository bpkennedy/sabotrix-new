using System.Linq;
using NUnit.Framework;
using Sabotrix.Core;

namespace Sabotrix.Tests.EditMode
{
    public class MatchCreationTests
    {
        [Test]
        public void Create_TwoHeroes_HasTwoPlayersWithTheirHeroes()
        {
            Match match = TestContent.BonkVsPudge();

            Assert.That(match.Players.Count, Is.EqualTo(2));
            Assert.That(match.Players[0].Hero, Is.SameAs(TestContent.Bonk));
            Assert.That(match.Players[1].Hero, Is.SameAs(TestContent.Pudge));
        }

        [Test]
        public void Create_NewMatch_PlayersStartAtMaxHpAndEnergy()
        {
            Match match = TestContent.BonkVsPudge();

            Assert.That(match.Players[0].Hp, Is.EqualTo(160));
            Assert.That(match.Players[0].Energy, Is.EqualTo(6));
            Assert.That(match.Players[1].Hp, Is.EqualTo(220));
            Assert.That(match.Players[1].Energy, Is.EqualTo(6));
        }

        [Test]
        public void Create_NewMatch_SpecialEnergyBlockAndSabotagesStartAtZero()
        {
            Match match = TestContent.BonkVsPudge();

            foreach (PlayerState player in match.Players)
            {
                Assert.That(player.SpecialEnergy, Is.EqualTo(0));
                Assert.That(player.Block, Is.EqualTo(0));
                Assert.That(player.SabotagesPlayed, Is.EqualTo(0));
            }
        }

        [Test]
        public void Create_NewMatch_SecondPlayerHasFiveCardsAndFirstPlayerDrewOneMore()
        {
            Match match = TestContent.BonkVsPudge();

            Assert.That(match.Players[0].Hand.Count, Is.EqualTo(Rules.StartingHandSize + 1));
            Assert.That(match.Players[1].Hand.Count, Is.EqualTo(Rules.StartingHandSize));
            Assert.That(match.Players.All(player => player.Selected.Count == 0), Is.True);
        }

        [Test]
        public void Create_SeededShuffle_EveryDeckCardIsInDeckOrHand()
        {
            DeckDefinition brawler = TestContent.BrawlerDeck();
            Match match = Match.Create(TestContent.Bonk, brawler, TestContent.Pudge, TestContent.BunkerDeck(), new SeededRandom(7));
            PlayerState player = match.Players[0];

            CardDefinition[] dealt = player.Hand.Concat(player.Deck).Select(card => card.Definition).ToArray();

            Assert.That(dealt, Is.EquivalentTo(brawler.Cards));
        }

        [Test]
        public void Create_SameSeed_DealsSameHands()
        {
            Match first = Match.Create(TestContent.Bonk, TestContent.BrawlerDeck(), TestContent.Pudge, TestContent.BunkerDeck(), new SeededRandom(7));
            Match second = Match.Create(TestContent.Bonk, TestContent.BrawlerDeck(), TestContent.Pudge, TestContent.BunkerDeck(), new SeededRandom(7));

            Assert.That(first.Players[0].Hand.Select(card => card.Definition), Is.EqualTo(second.Players[0].Hand.Select(card => card.Definition)));
            Assert.That(first.Players[1].Hand.Select(card => card.Definition), Is.EqualTo(second.Players[1].Hand.Select(card => card.Definition)));
        }

        [Test]
        public void Create_DuplicateCards_EachCopyHasUniqueInstanceId()
        {
            Match match = TestContent.BonkVsPudge();

            int[] instanceIds = match.Players
                .SelectMany(player => player.Deck.Concat(player.Hand).Append(player.Ability).Append(player.BasicAttack))
                .Select(card => card.InstanceId)
                .ToArray();

            Assert.That(instanceIds, Is.Unique);
        }

        [Test]
        public void Create_HigherInitiative_GoesFirst()
        {
            Match bonkFirst = TestContent.BonkVsPudge();
            Match bonkSecond = TestContent.CreateMatch(TestContent.Pudge, TestContent.BunkerDeck(), TestContent.Bonk, TestContent.BrawlerDeck());

            Assert.That(bonkFirst.ActivePlayer.Hero, Is.SameAs(TestContent.Bonk));
            Assert.That(bonkSecond.ActivePlayer.Hero, Is.SameAs(TestContent.Bonk));
        }

        [Test]
        public void Create_InitiativeTie_RandomPicksFirstPlayer()
        {
            HeroDefinition hero = TestContent.Hero("twin");

            Match match = TestContent.CreateMatch(hero, TestContent.BrawlerDeck(), hero, TestContent.BrawlerDeck());

            Assert.That(match.ActivePlayerIndex, Is.EqualTo(1));
        }

        [Test]
        public void Create_NewMatch_StartsOnTurnOne()
        {
            Match match = TestContent.BonkVsPudge();

            Assert.That(match.TurnNumber, Is.EqualTo(1));
            Assert.That(match.IsOver, Is.False);
        }
    }
}
