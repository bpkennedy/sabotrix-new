using System;
using System.Linq;
using NUnit.Framework;
using Sabotrix.Core;

namespace Sabotrix.Tests.EditMode
{
    public class DefinitionTests
    {
        [Test]
        public void DeckDefinition_StarterDecks_AreValid()
        {
            Assert.DoesNotThrow(() => TestContent.BrawlerDeck());
            Assert.DoesNotThrow(() => TestContent.BunkerDeck());
        }

        [Test]
        public void DeckDefinition_NineAttackCards_Throws()
        {
            Assert.Throws<ArgumentException>(() =>
                new DeckDefinition("Too many attacks", TestContent.Copies(TestContent.Jab, 9)));
        }

        [Test]
        public void DeckDefinition_FourSabotageCards_Throws()
        {
            Assert.Throws<ArgumentException>(() =>
                new DeckDefinition("Too many sabotages", TestContent.Copies(TestContent.Blackout, 4)));
        }

        [Test]
        public void DeckDefinition_ContainsAbilityCard_Throws()
        {
            Assert.Throws<ArgumentException>(() =>
                new DeckDefinition("Has ability", TestContent.Copies(TestContent.Jab, 1).Append(TestContent.Haymaker)));
        }

        [Test]
        public void HeroDefinition_NonAbilityCardAsAbility_Throws()
        {
            Assert.Throws<ArgumentException>(() => new HeroDefinition(
                "bad", "Bad", HeroClass.Fighter,
                maxHp: 100, strength: 0, initiative: 0, energyMax: 6, energyRegen: 3, specialEnergyGain: 1,
                ability: TestContent.Jab));
        }
    }
}
