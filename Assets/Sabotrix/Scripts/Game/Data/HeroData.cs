using Sabotrix.Core;
using UnityEngine;

namespace Sabotrix.Game.Data
{
    // A hero as an editable asset. The asset's file name is the hero's id.
    [CreateAssetMenu(menuName = "Sabotrix/Hero", fileName = "NewHero")]
    public sealed class HeroData : ScriptableObject
    {
        [SerializeField] private string _displayName;
        [SerializeField] private HeroClass _class;
        [SerializeField, Min(1)] private int _maxHp = 100;
        [SerializeField, Min(0)] private int _strength;
        [SerializeField, Min(0)] private int _initiative;
        [SerializeField, Min(0)] private int _energyMax = 6;
        [SerializeField, Min(0)] private int _energyRegen = 3;
        [SerializeField, Min(0)] private int _specialEnergyGain = 1;

        [Tooltip("Must be an Ability card.")]
        [SerializeField] private CardData _ability;

        public string DisplayName => _displayName;

        public HeroDefinition ToDefinition() => new HeroDefinition(
            name,
            _displayName,
            _class,
            _maxHp,
            _strength,
            _initiative,
            _energyMax,
            _energyRegen,
            _specialEnergyGain,
            _ability.ToDefinition());
    }
}
