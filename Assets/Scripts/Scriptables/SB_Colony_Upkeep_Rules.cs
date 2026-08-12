using UnityEngine;

/// <summary>
/// How the colony pays for itself, and what happens when it cannot.
///
/// The starvation response is deliberately graduated rather than instantly lethal: training stops
/// first, then wasps slow down toward their untrained baseline, and only a prolonged shortage
/// starts killing them. That gives the player time to notice and react.
/// </summary>
[CreateAssetMenu(fileName = "SO_ColonyUpkeepRules", menuName = "Vespidae Wars/Colony Upkeep Rules")]
public class SB_Colony_Upkeep_Rules : ScriptableObject
{
    [Header("Timing")]
    [SerializeField, Min(0.5f), Tooltip("Seconds between upkeep charges.")]
    private float upkeepTickSeconds = 10f;

    [Header("Starvation Response")]
    [SerializeField, Min(0f), Tooltip("Seconds of unpaid upkeep before wasps start slowing down. Training stops immediately.")]
    private float starvationGraceSeconds = 15f;
    [SerializeField, Min(1f), Tooltip("Seconds of unpaid upkeep, past the grace period, before the decay reaches its floor.")]
    private float starvationRampSeconds = 45f;
    [SerializeField, Min(1f), Tooltip("Seconds of unpaid upkeep before wasps begin dying.")]
    private float starvationDeathSeconds = 90f;
    [SerializeField, Min(1f), Tooltip("Seconds between deaths once starvation is lethal.")]
    private float starvationDeathIntervalSeconds = 10f;

    [Header("Recovery")]
    [SerializeField, Min(0.1f), Tooltip("How fast starvation unwinds once upkeep is affordable again, as a multiple of real time.")]
    private float recoveryRate = 2f;

    public float UpkeepTickSeconds => Mathf.Max(0.5f, upkeepTickSeconds);
    public float StarvationGraceSeconds => starvationGraceSeconds;
    public float StarvationRampSeconds => Mathf.Max(1f, starvationRampSeconds);
    public float StarvationDeathSeconds => Mathf.Max(1f, starvationDeathSeconds);
    public float StarvationDeathIntervalSeconds => Mathf.Max(1f, starvationDeathIntervalSeconds);
    public float RecoveryRate => Mathf.Max(0.1f, recoveryRate);

    /// <summary>
    /// 0 = fed, 1 = fully degraded. Stats are blended toward their level-1 values by this amount,
    /// so a starving colony is slow and weak but never frozen.
    /// </summary>
    public float GetSeverity(float starvedSeconds)
    {
        if (starvedSeconds <= starvationGraceSeconds)
            return 0f;

        return Mathf.Clamp01((starvedSeconds - starvationGraceSeconds) / StarvationRampSeconds);
    }
}
