using System.Linq;
using NUnit.Framework;
using Sabotrix.Core;
using Sabotrix.Game.Data;
using UnityEditor;

namespace Sabotrix.Tests.EditMode
{
    // Checks the ScriptableObject assets in Assets/Sabotrix/Data match GDD Section 10 (as captured in TestContent).
    // If Malcolm tunes a number in the Inspector, update TestContent and the GDD to match.
    public class StarterAssetTests
    {
        private const string DataFolder = "Assets/Sabotrix/Data";

        private static T Load<T>(string path) where T : UnityEngine.Object =>
            AssetDatabase.LoadAssetAtPath<T>($"{DataFolder}/{path}.asset");

        [Test]
        public void CardAssets_AllCards_MatchGdd()
        {
            CardDefinition[] expected =
            {
                TestContent.Haymaker, TestContent.PotLidGuard,
                TestContent.Jab, TestContent.Poke, TestContent.CounterStance, TestContent.Swing,
                TestContent.DoubleTap, TestContent.SecondWind, TestContent.BodySlam, TestContent.BigFinish,
                TestContent.Blackout, TestContent.ShortCircuit, TestContent.BeholderDrop, TestContent.SpilledDrink
            };

            CardDefinition[] actual = AssetDatabase.FindAssets("t:CardData", new[] { DataFolder })
                .Select(guid => AssetDatabase.LoadAssetAtPath<CardData>(AssetDatabase.GUIDToAssetPath(guid)).ToDefinition())
                .ToArray();

            Assert.That(actual.Length, Is.EqualTo(expected.Length));
            foreach (CardDefinition card in expected)
            {
                CardDefinition asset = actual.SingleOrDefault(candidate => candidate.Name == card.Name);
                Assert.That(asset, Is.Not.Null, $"Missing card asset for {card.Name}");
                AssertSameCard(asset, card);
            }
        }

        [TestCase("Bonk")]
        [TestCase("Pudge")]
        public void HeroAssets_StarterHeroes_MatchGdd(string heroName)
        {
            HeroDefinition expected = heroName == "Bonk" ? TestContent.Bonk : TestContent.Pudge;

            HeroDefinition actual = Load<HeroData>($"Heroes/{heroName}").ToDefinition();

            Assert.That(actual.Name, Is.EqualTo(expected.Name));
            Assert.That(actual.Class, Is.EqualTo(expected.Class));
            Assert.That(actual.MaxHp, Is.EqualTo(expected.MaxHp));
            Assert.That(actual.Strength, Is.EqualTo(expected.Strength));
            Assert.That(actual.Initiative, Is.EqualTo(expected.Initiative));
            Assert.That(actual.EnergyMax, Is.EqualTo(expected.EnergyMax));
            Assert.That(actual.EnergyRegen, Is.EqualTo(expected.EnergyRegen));
            Assert.That(actual.SpecialEnergyGain, Is.EqualTo(expected.SpecialEnergyGain));
            AssertSameCard(actual.Ability, expected.Ability);
        }

        [TestCase("Brawler")]
        [TestCase("Bunker")]
        public void DeckAssets_StarterDecks_MatchGdd(string deckName)
        {
            DeckDefinition expected = deckName == "Brawler" ? TestContent.BrawlerDeck() : TestContent.BunkerDeck();

            DeckDefinition actual = Load<DeckData>($"Decks/{deckName}").ToDefinition();

            Assert.That(actual.Cards.Select(card => card.Name), Is.EquivalentTo(expected.Cards.Select(card => card.Name)));
        }

        [Test]
        public void StarterAssets_FullMatch_PlaysToTheEnd()
        {
            Match match = Match.Create(
                Load<HeroData>("Heroes/Bonk").ToDefinition(), Load<DeckData>("Decks/Brawler").ToDefinition(),
                Load<HeroData>("Heroes/Pudge").ToDefinition(), Load<DeckData>("Decks/Bunker").ToDefinition(),
                new SeededRandom(1));

            while (!match.IsOver)
            {
                TestContent.PlayAnyAttack(match);
            }

            Assert.That(match.TurnNumber, Is.LessThanOrEqualTo(Rules.TurnLimit));
        }

        private static void AssertSameCard(CardDefinition actual, CardDefinition expected)
        {
            string card = expected.Name;
            Assert.That(actual.Name, Is.EqualTo(expected.Name), card);
            Assert.That(actual.Type, Is.EqualTo(expected.Type), card);
            Assert.That(actual.Cost, Is.EqualTo(expected.Cost), card);
            Assert.That(actual.Damage, Is.EqualTo(expected.Damage), card);
            Assert.That(actual.Hits, Is.EqualTo(expected.Hits), card);
            Assert.That(actual.FinisherBonus, Is.EqualTo(expected.FinisherBonus), card);
            Assert.That(actual.FinisherHpThreshold, Is.EqualTo(expected.FinisherHpThreshold), card);
            Assert.That(actual.BonusSpecialEnergy, Is.EqualTo(expected.BonusSpecialEnergy), card);
            Assert.That(actual.Block, Is.EqualTo(expected.Block), card);
            Assert.That(actual.Heal, Is.EqualTo(expected.Heal), card);
            Assert.That(actual.DoublesNextAttack, Is.EqualTo(expected.DoublesNextAttack), card);
            Assert.That(actual.Sabotage, Is.EqualTo(expected.Sabotage), card);
            Assert.That(actual.SabotageAmount, Is.EqualTo(expected.SabotageAmount), card);
            Assert.That(actual.ReversalChance, Is.EqualTo(expected.ReversalChance).Within(0.0001), card);
            Assert.That(actual.ReversalAmount, Is.EqualTo(expected.ReversalAmount), card);
        }
    }
}
