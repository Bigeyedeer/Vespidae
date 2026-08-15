using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks how often the player has fought each invasive faction, and releases one more piece of that
/// species' identification card every few engagements.
///
/// The point is that field knowledge is earned rather than handed over. A faction the player has
/// never met stays a blank card; the one they have been fighting slowly fills in, starting with its
/// name. Counts are per faction, so the two invasives reveal independently.
///
/// Progress lives on this component and so resets with the match, which is deliberate - every
/// playtest sees the whole arc from blank to identified.
/// </summary>
[DefaultExecutionOrder(-160)]
public class SpeciesCodex : MonoBehaviour
{
    public static SpeciesCodex Instance { get; private set; }

    [SerializeField, Min(1), Tooltip("Combat engagements with a faction needed to release each further " +
                                     "piece of information about it.")]
    private int engagementsPerUnlock = 3;
    [SerializeField, Tooltip("The player's own colony is not a mystery to them, so its card starts complete. " +
                             "Turn off to make the native species earn its entries too.")]
    private bool nativeStartsRevealed = true;

    private readonly Dictionary<WaspScopeRole, int> engagements = new Dictionary<WaspScopeRole, int>();

    /// <summary>Raised when a faction crosses into a new tier, so open UI can refresh itself.</summary>
    public event Action<WaspScopeRole> CodexAdvanced;

    public int EngagementsPerUnlock => engagementsPerUnlock;

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

    /// <summary>Records one combat engagement against a faction.</summary>
    public void RegisterEngagement(WaspScopeRole faction)
    {
        engagements.TryGetValue(faction, out int previous);
        int updated = previous + 1;
        engagements[faction] = updated;

        // Only shout when a tier is actually crossed, not on every skirmish.
        if (previous / engagementsPerUnlock != updated / engagementsPerUnlock)
            CodexAdvanced?.Invoke(faction);
    }

    public int EngagementCount(WaspScopeRole faction)
    {
        engagements.TryGetValue(faction, out int count);
        return count;
    }

    /// <summary>How many codex entries this faction has released so far.</summary>
    public int UnlockedTierCount(WaspScopeRole faction)
    {
        if (nativeStartsRevealed && faction == WaspScopeRole.NativePlayer)
            return int.MaxValue;

        return EngagementCount(faction) / engagementsPerUnlock;
    }

    public bool IsTierUnlocked(WaspScopeRole faction, int tier)
    {
        return tier < UnlockedTierCount(faction);
    }

    /// <summary>Engagements still needed before the next entry is released.</summary>
    public int EngagementsUntilNextUnlock(WaspScopeRole faction)
    {
        if (nativeStartsRevealed && faction == WaspScopeRole.NativePlayer)
            return 0;

        int remainder = EngagementCount(faction) % engagementsPerUnlock;
        return engagementsPerUnlock - remainder;
    }

    /// <summary>Fraction of a species' card that has been filled in, for the confidence bar.</summary>
    public float IdentificationProgress(SB_Wasps_Info species)
    {
        if (species == null)
            return 0f;

        int total = species.CodexEntries != null ? species.CodexEntries.Count : 0;
        if (total <= 0)
            return 0f;

        return Mathf.Clamp01(Mathf.Min(UnlockedTierCount(species.ScopeRole), total) / (float)total);
    }

    [ContextMenu("Log codex progress")]
    private void DebugLogProgress()
    {
        foreach (WaspScopeRole faction in Enum.GetValues(typeof(WaspScopeRole)))
        {
            Debug.Log($"{faction}: {EngagementCount(faction)} engagements, " +
                      $"{UnlockedTierCount(faction)} entries unlocked, " +
                      $"{EngagementsUntilNextUnlock(faction)} until next.", this);
        }
    }
}
