using Sabotrix.Core;
using UnityEngine;

namespace Sabotrix.Game.Data
{
    // A card as an editable asset. The asset's file name is the card's id.
    [CreateAssetMenu(menuName = "Sabotrix/Card", fileName = "NewCard")]
    public sealed class CardData : ScriptableObject
    {
        [SerializeField] private string _displayName;
        [SerializeField] private CardType _type;

        [Tooltip("Energy for Attack and Sabotage cards, Special Energy for Ability cards.")]
        [SerializeField, Min(0)] private int _cost;

        [Header("Attack")]
        [Tooltip("Damage per hit, before Strength.")]
        [SerializeField, Min(0)] private int _damage;
        [SerializeField, Min(1)] private int _hits = 1;
        [Tooltip("Extra damage when the opponent's HP is at or below the threshold.")]
        [SerializeField, Min(0)] private int _finisherBonus;
        [SerializeField, Min(0)] private int _finisherHpThreshold;
        [Tooltip("Extra Special Energy when this Attack removes HP.")]
        [SerializeField, Min(0)] private int _bonusSpecialEnergy;

        [Header("Attack and Ability")]
        [SerializeField, Min(0)] private int _block;
        [SerializeField, Min(0)] private int _heal;
        [Tooltip("Ability only: the next Attack this turn deals double damage.")]
        [SerializeField] private bool _doublesNextAttack;

        [Header("Sabotage")]
        [SerializeField] private SabotageKind _sabotage;
        [SerializeField, Min(0)] private int _sabotageAmount;
        [Tooltip("Chance to backfire, from 0 (never) to 1 (always).")]
        [SerializeField, Range(0f, 1f)] private float _reversalChance;
        [Tooltip("Size of the effect when it backfires onto the player who played it.")]
        [SerializeField, Min(0)] private int _reversalAmount;

        public CardDefinition ToDefinition() => new CardDefinition(
            name,
            _displayName,
            _type,
            _cost,
            damage: _damage,
            hits: _hits,
            finisherBonus: _finisherBonus,
            finisherHpThreshold: _finisherHpThreshold,
            bonusSpecialEnergy: _bonusSpecialEnergy,
            block: _block,
            heal: _heal,
            doublesNextAttack: _doublesNextAttack,
            sabotage: _sabotage,
            sabotageAmount: _sabotageAmount,
            reversalChance: _reversalChance,
            reversalAmount: _reversalAmount);
    }
}
