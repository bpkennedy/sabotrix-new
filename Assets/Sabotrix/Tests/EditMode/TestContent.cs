using System.Collections.Generic;
using System.Linq;
using Sabotrix.Core;

namespace Sabotrix.Tests.EditMode
{
    // Heroes, cards, and decks from GDD Section 10, built in code for tests.
    // The game's real data comes from ScriptableObjects (M1.4).
    public static class TestContent
    {
        public static readonly CardDefinition Haymaker = new CardDefinition("haymaker", "Haymaker", CardType.Ability, 3, doublesNextAttack: true);
        public static readonly CardDefinition PotLidGuard = new CardDefinition("pot-lid-guard", "Pot Lid Guard", CardType.Ability, 3, block: 20);

        public static readonly CardDefinition Jab = new CardDefinition("jab", "Jab", CardType.Attack, 1, damage: 10);
        public static readonly CardDefinition Poke = new CardDefinition("poke", "Poke", CardType.Attack, 1, damage: 8, bonusSpecialEnergy: 1);
        public static readonly CardDefinition CounterStance = new CardDefinition("counter-stance", "Counter Stance", CardType.Attack, 1, damage: 6, block: 6);
        public static readonly CardDefinition Swing = new CardDefinition("swing", "Swing", CardType.Attack, 2, damage: 18);
        public static readonly CardDefinition DoubleTap = new CardDefinition("double-tap", "Double Tap", CardType.Attack, 2, damage: 8, hits: 2);
        public static readonly CardDefinition SecondWind = new CardDefinition("second-wind", "Second Wind", CardType.Attack, 2, damage: 10, heal: 8);
        public static readonly CardDefinition BodySlam = new CardDefinition("body-slam", "Body Slam", CardType.Attack, 3, damage: 25);
        public static readonly CardDefinition BigFinish = new CardDefinition("big-finish", "Big Finish", CardType.Attack, 3, damage: 20, finisherBonus: 10, finisherHpThreshold: 25);

        public static readonly CardDefinition Blackout = new CardDefinition("blackout", "Blackout", CardType.Sabotage, 2,
            sabotage: SabotageKind.Blind, reversalChance: 0.35);
        public static readonly CardDefinition ShortCircuit = new CardDefinition("short-circuit", "Short Circuit", CardType.Sabotage, 1,
            sabotage: SabotageKind.EnergySteal, sabotageAmount: 2, reversalChance: 0.20, reversalAmount: 2);
        public static readonly CardDefinition BeholderDrop = new CardDefinition("beholder-drop", "Beholder Drop", CardType.Sabotage, 3,
            sabotage: SabotageKind.Damage, sabotageAmount: 20, reversalChance: 0.45, reversalAmount: 10);
        public static readonly CardDefinition SpilledDrink = new CardDefinition("spilled-drink", "Spilled Drink", CardType.Sabotage, 0,
            sabotage: SabotageKind.AttackCostIncrease, sabotageAmount: 1, reversalChance: 0.15, reversalAmount: 1);

        public static readonly HeroDefinition Bonk = new HeroDefinition(
            "bonk", "Bonk", HeroClass.Fighter,
            maxHp: 80, strength: 3, initiative: 5, energyMax: 6, energyRegen: 3, specialEnergyGain: 1,
            ability: Haymaker);

        public static readonly HeroDefinition Pudge = new HeroDefinition(
            "pudge", "Pudge", HeroClass.Tank,
            maxHp: 110, strength: 0, initiative: 2, energyMax: 6, energyRegen: 3, specialEnergyGain: 1,
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

        public static DeckDefinition Deck(params CardDefinition[] cards) => new DeckDefinition("Test", cards);

        public static DeckDefinition Deck(IEnumerable<CardDefinition> cards) => new DeckDefinition("Test", cards);

        public static HeroDefinition Hero(
            string id,
            int maxHp = 100,
            int strength = 0,
            int initiative = 0,
            int energyMax = 6,
            int energyRegen = 3) => new HeroDefinition(
                id, id, HeroClass.Fighter,
                maxHp, strength, initiative, energyMax, energyRegen, specialEnergyGain: 1,
                ability: Haymaker);

        // Bonk (Initiative 5) goes first. Decks stay in listed order:
        // Bonk's hand is Jab, Jab, Swing, Swing, Body Slam + Body Slam drawn on turn 1.
        // Pudge's hand is Jab, Jab, Poke, Swing, Counter Stance.
        public static Match BonkVsPudge() =>
            Match.Create(Bonk, BrawlerDeck(), Pudge, BunkerDeck(), new IdentityRandom());

        public static Match CreateMatch(HeroDefinition heroOne, DeckDefinition deckOne, HeroDefinition heroTwo, DeckDefinition deckTwo, IRandom random = null) =>
            Match.Create(heroOne, deckOne, heroTwo, deckTwo, random ?? new IdentityRandom());

        // Player one ("player", Initiative 1) goes first holding the given cards in order, topped up with Jabs.
        // Player two ("target", 1000 HP) holds Jabs.
        public static Match PlayerHolding(HeroDefinition player, IRandom random, params CardDefinition[] cards)
        {
            DeckDefinition deck = Deck(cards.Concat(Copies(Jab, 6 - cards.Length)));
            DeckDefinition jabs = Deck(Copies(Jab, 8));
            return CreateMatch(player, deck, Hero("target", maxHp: 1000), jabs, random);
        }

        public static Match PlayerHolding(params CardDefinition[] cards) =>
            PlayerHolding(Hero("player", initiative: 1), null, cards);

        // Selects the first affordable Attack card in hand (or Basic Attack), then ends the turn.
        public static void PlayAnyAttack(Match match)
        {
            PlayerState player = match.ActivePlayer;
            CardInstance attack = player.Hand.FirstOrDefault(card =>
                card.Definition.Type == CardType.Attack && match.CanSelect(card)) ?? player.BasicAttack;

            match.TrySelect(attack);
            match.EndTurn();
        }
    }
}
