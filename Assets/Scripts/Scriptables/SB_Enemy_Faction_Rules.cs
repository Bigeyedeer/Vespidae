using UnityEngine;

/// <summary>
/// Tuning for one invasive faction's behaviour: how fast it builds strength, when it is willing to
/// attack player territory, and how long it needs to hold a hex before claiming it.
///
/// Each faction gets its own asset so Primary and Secondary invasives can behave differently.
/// Strength is a single abstract budget rather than a mirror of the player's Nectar/Prey/Fibre —
/// the player never sees the number, only the behaviour it produces.
/// </summary>
[CreateAssetMenu(fileName = "SO_EnemyFactionRules", menuName = "Vespidae Wars/Enemy Faction Rules")]
public class SB_Enemy_Faction_Rules : ScriptableObject
{
    [Header("Strength Economy")]
    [SerializeField, Min(0.1f), Tooltip("Seconds between strength accrual ticks.")]
    private float strengthTickSeconds = 5f;
    [SerializeField, Min(0f), Tooltip("Strength gained per owned hex on each tick.")]
    private float strengthPerOwnedHexPerTick = 1f;
    [SerializeField, Min(0f), Tooltip("Strength held before the faction will consider anything.")]
    private float startingStrength = 10f;
    [SerializeField, Min(0f), Tooltip("Ceiling so an ignored faction cannot bank an unstoppable reserve.")]
    private float maximumStrength = 200f;

    [Header("Costs")]
    [SerializeField, Min(0f)] private float attackerTrainingCost = 8f;
    [SerializeField, Min(0f), Tooltip("Strength spent to launch one attack on a player hex.")]
    private float attackCost = 20f;

    [Header("Aggression Gating")]
    [SerializeField, Min(0), Tooltip("Hexes this faction must own before it will attack the player at all.")]
    private int minimumHexesBeforeAggression = 3;
    [SerializeField, Min(0f), Tooltip("Seconds of match time before this faction turns aggressive.")]
    private float earliestAggressionSeconds = 180f;
    [SerializeField, Min(1), Tooltip("Idle attackers required before an attack is considered.")]
    private int minimumAttackersToAttack = 2;

    [Header("Attack Timing")]
    [SerializeField, Min(1f)] private float attackIntervalMinimum = 45f;
    [SerializeField, Min(1f)] private float attackIntervalMaximum = 90f;

    [Header("Territory")]
    [SerializeField, Min(1f), Tooltip("Seconds an attacker must hold a player hex unopposed before it flips.")]
    private float hexClaimSeconds = 40f;

    public float StrengthTickSeconds => Mathf.Max(0.1f, strengthTickSeconds);
    public float StrengthPerOwnedHexPerTick => strengthPerOwnedHexPerTick;
    public float StartingStrength => startingStrength;
    public float MaximumStrength => Mathf.Max(0f, maximumStrength);
    public float AttackerTrainingCost => attackerTrainingCost;
    public float AttackCost => attackCost;
    public int MinimumHexesBeforeAggression => minimumHexesBeforeAggression;
    public float EarliestAggressionSeconds => earliestAggressionSeconds;
    public int MinimumAttackersToAttack => Mathf.Max(1, minimumAttackersToAttack);
    public float HexClaimSeconds => Mathf.Max(1f, hexClaimSeconds);

    /// <summary>
    /// A randomised gap between attacks so the faction does not behave like a metronome.
    /// </summary>
    public float GetNextAttackInterval()
    {
        float minimum = Mathf.Min(attackIntervalMinimum, attackIntervalMaximum);
        float maximum = Mathf.Max(attackIntervalMinimum, attackIntervalMaximum);
        return Random.Range(minimum, maximum);
    }
}
