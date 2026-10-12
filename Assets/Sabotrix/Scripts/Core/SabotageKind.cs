namespace Sabotrix.Core
{
    // What a Sabotage card does. On a backfire, the same kind of effect hits the player who played it.
    public enum SabotageKind
    {
        None,

        // Target's hand is face-down during their next turn.
        Blind,

        // Target loses Energy at the start of their next turn; you gain it at the start of yours.
        EnergySteal,

        // Damage to the target right away.
        Damage,

        // Target's Attack cards cost more Energy on their next turn.
        AttackCostIncrease
    }
}
