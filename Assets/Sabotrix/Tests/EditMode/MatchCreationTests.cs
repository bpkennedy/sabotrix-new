using System.Linq;
using NUnit.Framework;
using Sabotrix.Core;

namespace Sabotrix.Tests.EditMode
{
    public class MatchCreationTests
    {
        private static Match CreateBonkVsPudge() =>
            Match.Create(TestContent.Bonk, TestContent.BrawlerDeck(), TestContent.Pudge, TestContent.BunkerDeck());

        [Test]
        public void Create_TwoHeroes_HasTwoPlayersWithTheirHeroes()
        {
            Match match = CreateBonkVsPudge();

            Assert.That(match.Players.Count, Is.EqualTo(2));
            Assert.That(match.Players[0].Hero, Is.SameAs(TestContent.Bonk));
            Assert.That(match.Players[1].Hero, Is.SameAs(TestContent.Pudge));
        }

        [Test]
        public void Create_NewMatch_PlayersStartAtMaxHpAndEnergy()
        {
            Match match = CreateBonkVsPudge();

            Assert.That(match.Players[0].Hp, Is.EqualTo(160));
            Assert.That(match.Players[0].Energy, Is.EqualTo(6));
            Assert.That(match.Players[1].Hp, Is.EqualTo(220));
            Assert.That(match.Players[1].Energy, Is.EqualTo(6));
        }

        [Test]
        public void Create_NewMatch_SpecialEnergyBlockAndSabotagesStartAtZero()
        {
            Match match = CreateBonkVsPudge();

            foreach (PlayerState player in match.Players)
            {
                Assert.That(player.SpecialEnergy, Is.EqualTo(0));
                Assert.That(player.Block, Is.EqualTo(0));
                Assert.That(player.SabotagesPlayed, Is.EqualTo(0));
            }
        }

        [Test]
        public void Create_NewMatch_TurnNumberIsZero()
        {
            Match match = CreateBonkVsPudge();

            Assert.That(match.TurnNumber, Is.EqualTo(0));
        }

        [Test]
        public void Create_NewMatch_DeckHoldsAllCardsInDeckOrderAndHandIsEmpty()
        {
            DeckDefinition brawler = TestContent.BrawlerDeck();
            Match match = Match.Create(TestContent.Bonk, brawler, TestContent.Pudge, TestContent.BunkerDeck());
            PlayerState player = match.Players[0];

            Assert.That(player.Deck.Select(card => card.Definition), Is.EqualTo(brawler.Cards));
            Assert.That(player.Hand, Is.Empty);
            Assert.That(player.Selected, Is.Empty);
        }

        [Test]
        public void Create_DuplicateCards_EachCopyHasUniqueInstanceId()
        {
            Match match = CreateBonkVsPudge();

            int[] instanceIds = match.Players.SelectMany(player => player.Deck).Select(card => card.InstanceId).ToArray();

            Assert.That(instanceIds, Is.Unique);
        }
    }
}
