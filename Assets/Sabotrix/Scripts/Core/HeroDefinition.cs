using System;

namespace Sabotrix.Core
{
    public sealed class HeroDefinition
    {
        public string Id { get; }
        public string Name { get; }
        public HeroClass Class { get; }
        public int MaxHp { get; }
        public int Strength { get; }
        public int Initiative { get; }
        public int EnergyMax { get; }
        public int EnergyRegen { get; }
        public int SpecialEnergyGain { get; }
        public CardDefinition Ability { get; }

        public HeroDefinition(
            string id,
            string name,
            HeroClass heroClass,
            int maxHp,
            int strength,
            int initiative,
            int energyMax,
            int energyRegen,
            int specialEnergyGain,
            CardDefinition ability)
        {
            if (ability.Type != CardType.Ability)
            {
                throw new ArgumentException($"Hero ability '{ability.Name}' must be an Ability card.", nameof(ability));
            }

            Id = id;
            Name = name;
            Class = heroClass;
            MaxHp = maxHp;
            Strength = strength;
            Initiative = initiative;
            EnergyMax = energyMax;
            EnergyRegen = energyRegen;
            SpecialEnergyGain = specialEnergyGain;
            Ability = ability;
        }
    }
}
