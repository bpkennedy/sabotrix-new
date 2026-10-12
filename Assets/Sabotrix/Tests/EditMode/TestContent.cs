using System.Collections.Generic;
using System.Linq;
using Sabotrix.Core;

namespace Sabotrix.Tests.EditMode
{
    // Heroes, cards, and decks from GDD Section 10, built in code for tests.
    // The game's real data comes from ScriptableObjects (M1.4).
    public static class TestContent
    {
        public static readonly CardDefinition Haymaker = new CardDefinition("haymaker", "Haymaker", CardType.Ability, 3, 0);
        public static readonly CardDefinition PotLidGuard = new CardDefinition("pot-lid-guard", "Pot Lid Guard", CardType.Ability, 3, 0);

        public static readonly CardDefinition Jab = new CardDefinition("jab", "Jab", CardType.Attack, 1, 10);
        public static readonly CardDefinition Poke = new CardDefinition("poke", "Poke", CardType.Attack, 1, 8);
        public static readonly CardDefinition CounterStance = new CardDefinition("counter-stance", "Counter Stance", CardType.Attack, 1, 6);
        public static readonly CardDefinition Swing = new CardDefinition("swing", "Swing", CardType.Attack, 2, 18);
        public static readonly CardDefinition DoubleTap = new CardDefinition("double-tap", "Double Tap", CardType.Attack, 2, 8);
        public static readonly CardDefinition SecondWind = new CardDefinition("second-wind", "Second Wind", CardType.Attack, 2, 10);
        public static readonly CardDefinition BodySlam = new CardDefinition("body-slam", "Body Slam", CardType.Attack, 3, 25);
        public static readonly CardDefinition BigFinish = new CardDefinition("big-finish", "Big Finish", CardType.Attack, 3, 20);

        public static readonly CardDefinition Blackout = new CardDefinition("blackout", "Blackout", CardType.Sabotage, 2, 0);
        public static readonly CardDefinition ShortCircuit = new CardDefinition("short-circuit", "Short Circuit", CardType.Sabotage, 1, 0);
        public static readonly CardDefinition BeholderDrop = new CardDefinition("beholder-drop", "Beholder Drop", CardType.Sabotage, 3, 20);
        public static readonly CardDefinition SpilledDrink = new CardDefinition("spilled-drink", "Spilled Drink", CardType.Sabotage, 0, 0);

        public static readonly HeroDefinition Bonk = new HeroDefinition(
            "bonk", "Bonk", HeroClass.Fighter,
            maxHp: 160, strength: 3, initiative: 5, energyMax: 6, energyRegen: 3, specialEnergyGain: 1,
            ability: Haymaker);

        public static readonly HeroDefinition Pudge = new HeroDefinition(
            "pudge", "Pudge", HeroClass.Tank,
            maxHp: 220, strength: 0, initiative: 2, energyMax: 6, energyRegen: 3, specialEnergyGain: 1,
            ability: PotLidGuard);

        public static DeckDefinition BrawlerDeck() => new DeckDefinition("Brawler", new[]
        {
            Jab, Jab, Swing, Swing, BodySlam, BodySlam, DoubleTap, BigFinish,
            Blackout, ShortCircuit, BeholderDrop
        });

        public static DeckDefinition BunkerDeck() => new DeckDefinition("Bunker", new[]
        {
            Jab, Jab, Poke, Swing, CounterStance, CounterStance, SecondWind, SecondWind,
            Blackout, ShortCircuit, SpilledDrink
        });

        public static IEnumerable<CardDefinition> Copies(CardDefinition card, int count) => Enumerable.Repeat(card, count);
    }
}
