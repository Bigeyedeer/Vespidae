using System;
using System.Collections.Generic;
using UnityEngine;

public enum GameOutcome
{
    InProgress,
    Victory,
    Defeat
}

/// <summary>
/// Watches the hives on both sides and decides when the match is over.
///
/// A faction that loses every hive capitulates and is removed from play. Wiping out all invasive
/// factions wins the match; losing the player's last hive loses it. Holding a large enough share of
/// the map is an optional shortcut victory.
/// </summary>
[DefaultExecutionOrder(-50)]
public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }

    [SerializeField] private SB_Victory_Rules victoryRules;

    private readonly HashSet<WaspScopeRole> capitulatedFactions = new HashSet<WaspScopeRole>();
    private float elapsed;
    private float evaluationTimer;

    public GameOutcome Outcome { get; private set; } = GameOutcome.InProgress;
    public bool IsMatchOver => Outcome != GameOutcome.InProgress;
    public IReadOnlyCollection<WaspScopeRole> CapitulatedFactions => capitulatedFactions;

    /// <summary>Raised once when the match ends, with the result.</summary>
    public event Action<GameOutcome> MatchEnded;
    /// <summary>Raised when an invasive faction loses its last hive.</summary>
    public event Action<WaspScopeRole> FactionCapitulated;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (IsMatchOver)
            return;

        elapsed += Time.deltaTime;
        if (victoryRules == null || elapsed < victoryRules.EvaluationGraceSeconds)
            return;

        evaluationTimer += Time.deltaTime;
        if (evaluationTimer < victoryRules.EvaluationIntervalSeconds)
            return;

        evaluationTimer = 0f;
        Evaluate();
    }

    /// <summary>
    /// Checks capitulation and end conditions. Safe to call directly (tests, debug tooling).
    /// </summary>
    public void Evaluate()
    {
        if (IsMatchOver)
            return;

        CheckFactionCapitulation();

        if (GetPlayerHiveCount() <= 0)
        {
            EndMatch(GameOutcome.Defeat);
            return;
        }

        if (victoryRules != null && victoryRules.RequireAllHivesDestroyed && GetEnemyHiveCount() <= 0)
        {
            EndMatch(GameOutcome.Victory);
            return;
        }

        if (victoryRules != null && victoryRules.AllowTerritoryVictory &&
            GetPlayerTerritoryPercentage() >= victoryRules.TerritoryWinPercentage)
        {
            EndMatch(GameOutcome.Victory);
        }
    }

    private void CheckFactionCapitulation()
    {
        foreach (WaspScopeRole faction in new[] { WaspScopeRole.PrimaryInvasive, WaspScopeRole.SecondaryInvasive })
        {
            if (capitulatedFactions.Contains(faction))
                continue;

            // A faction only counts as beaten if it was ever actually in the match.
            if (!HasFactionEverExisted(faction) || GetEnemyHiveCount(faction) > 0)
                continue;

            capitulatedFactions.Add(faction);
            RemoveFactionFromPlay(faction);
            FactionCapitulated?.Invoke(faction);
            Debug.Log($"{faction} has capitulated - all of its hives are destroyed.");
        }
    }

    /// <summary>
    /// Clears out a beaten faction's surviving units and territory so it stops acting.
    /// </summary>
    private void RemoveFactionFromPlay(WaspScopeRole faction)
    {
        foreach (EnemyWaspControl wasp in FindObjectsByType<EnemyWaspControl>(FindObjectsSortMode.None))
        {
            if (wasp != null && NormalizeFaction(wasp.Faction) == faction)
                wasp.DestroyFromCombat();
        }

        foreach (HexTile hex in FindObjectsByType<HexTile>(FindObjectsSortMode.None))
        {
            if (hex != null && hex.State == HexTile.HexState.Enemy &&
                NormalizeFaction(hex.EnemyOwnerFaction) == faction)
            {
                hex.CancelEnemyClaim();
                hex.ReleaseToNeutral();
            }
        }
    }

    private bool HasFactionEverExisted(WaspScopeRole faction)
    {
        // Any hex still flagged for the faction, or any surviving wasp, means it was in play.
        if (GetEnemyHiveCount(faction) > 0)
            return true;

        foreach (HexTile hex in FindObjectsByType<HexTile>(FindObjectsSortMode.None))
        {
            if (hex != null && hex.State == HexTile.HexState.Enemy &&
                NormalizeFaction(hex.EnemyOwnerFaction) == faction)
            {
                return true;
            }
        }

        foreach (EnemyWaspControl wasp in FindObjectsByType<EnemyWaspControl>(FindObjectsSortMode.None))
        {
            if (wasp != null && NormalizeFaction(wasp.Faction) == faction)
                return true;
        }

        return false;
    }

    public int GetPlayerHiveCount()
    {
        HiveManagement hive = HiveManagement.Instance;
        if (hive == null)
            return 0;

        int count = 0;
        foreach (C_Friendly_Hive_Orc friendly in hive.SpawnedFriendlyHives)
        {
            if (friendly != null)
                count++;
        }

        return count;
    }

    public int GetEnemyHiveCount()
    {
        EnemyHiveControl control = EnemyHiveControl.Instance;
        if (control == null)
            return 0;

        int count = 0;
        foreach (C_Enemy_Hive_Orc enemy in control.SpawnedEnemyHives)
        {
            if (enemy != null)
                count++;
        }

        return count;
    }

    public int GetEnemyHiveCount(WaspScopeRole faction)
    {
        EnemyHiveControl control = EnemyHiveControl.Instance;
        if (control == null)
            return 0;

        faction = NormalizeFaction(faction);
        int count = 0;
        foreach (C_Enemy_Hive_Orc enemy in control.SpawnedEnemyHives)
        {
            if (enemy != null && NormalizeFaction(enemy.Faction) == faction)
                count++;
        }

        return count;
    }

    /// <summary>
    /// Share of contestable hexes the player holds. Locked hexes are excluded so an unreachable
    /// map edge cannot make the target impossible.
    /// </summary>
    public float GetPlayerTerritoryPercentage()
    {
        int owned = 0, contestable = 0;
        foreach (HexTile hex in FindObjectsByType<HexTile>(FindObjectsSortMode.None))
        {
            if (hex == null || hex.State == HexTile.HexState.Locked)
                continue;

            contestable++;
            if (hex.State == HexTile.HexState.Owned)
                owned++;
        }

        return contestable == 0 ? 0f : owned * 100f / contestable;
    }

    private void EndMatch(GameOutcome outcome)
    {
        if (IsMatchOver)
            return;

        Outcome = outcome;
        Debug.Log($"Match ended: {outcome}");
        MatchEnded?.Invoke(outcome);
    }

    private static WaspScopeRole NormalizeFaction(WaspScopeRole faction)
    {
        return faction == WaspScopeRole.SecondaryInvasive
            ? WaspScopeRole.SecondaryInvasive
            : WaspScopeRole.PrimaryInvasive;
    }
}
