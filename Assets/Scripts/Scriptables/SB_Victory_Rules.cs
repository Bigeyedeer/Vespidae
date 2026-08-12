using UnityEngine;

/// <summary>
/// Defines how the match ends. Kept as an asset so win conditions can be retuned between
/// playtests without touching code.
/// </summary>
[CreateAssetMenu(fileName = "SO_VictoryRules", menuName = "Vespidae Wars/Victory Rules")]
public class SB_Victory_Rules : ScriptableObject
{
    [Header("Elimination")]
    [SerializeField, Tooltip("Win when every invasive faction has lost all of its hives.")]
    private bool requireAllHivesDestroyed = true;

    [Header("Territory")]
    [SerializeField, Tooltip("Also win by holding a share of the map, as a shortcut victory.")]
    private bool allowTerritoryVictory = true;
    [SerializeField, Range(0f, 100f), Tooltip("Percentage of claimable hexes the player must hold.")]
    private float territoryWinPercentage = 75f;

    [Header("Timing")]
    [SerializeField, Min(0f), Tooltip("Grace period before win/loss is evaluated, so startup spawning cannot trigger an instant result.")]
    private float evaluationGraceSeconds = 5f;
    [SerializeField, Min(0.1f)] private float evaluationIntervalSeconds = 1f;

    public bool RequireAllHivesDestroyed => requireAllHivesDestroyed;
    public bool AllowTerritoryVictory => allowTerritoryVictory;
    public float TerritoryWinPercentage => territoryWinPercentage;
    public float EvaluationGraceSeconds => evaluationGraceSeconds;
    public float EvaluationIntervalSeconds => Mathf.Max(0.1f, evaluationIntervalSeconds);
}
