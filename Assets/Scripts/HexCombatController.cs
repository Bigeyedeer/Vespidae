using System;
using System.Collections.Generic;
using UnityEngine;

public enum HexConflictState
{
    None,
    ScoutStandoff,
    AttackerBattle,
    HiveAssault
}

[RequireComponent(typeof(HexTile))]
public class HexCombatController : MonoBehaviour
{
    [SerializeField] private HexTile hexTile;
    [SerializeField, Range(1, 20)] private int maximumAttackersPerSide = 20;
    [SerializeField, Min(0.5f)] private float reinforcementResponseTime = 5f;

    private HexConflictState conflictState;
    private bool resolving;
    private bool responseActive;
    private bool friendlyPressing;
    private float responseTimeRemaining;

    public HexConflictState ConflictState => conflictState;
    public int MaximumAttackersPerSide => maximumAttackersPerSide;
    public bool HasScoutStandoff => conflictState == HexConflictState.ScoutStandoff;
    public int FriendlyAttackerCount => GetFriendlyAttackers().Count;
    public int EnemyAttackerCount => GetEnemyAttackers().Count;
    public float ResponseTimeRemaining => responseActive ? responseTimeRemaining : 0f;
    public bool HasActiveBattle => conflictState != HexConflictState.None;

    /// <summary>
    /// Who is winning the fight on this hex, as 0..1 from the player's point of view.
    /// 1 means the invaders are wiped out, 0 means the player's attackers are.
    /// Weighted by remaining health rather than headcount so a wounded group reads as losing.
    /// </summary>
    public float BattleBalance
    {
        get
        {
            float friendly = SumHealth(GetFriendlyAttackers());
            float enemy = SumHealth(GetEnemyAttackers());
            float total = friendly + enemy;
            return total <= 0f ? 0.5f : friendly / total;
        }
    }

    private static float SumHealth(List<WaspCombatant> combatants)
    {
        float total = 0f;
        foreach (WaspCombatant combatant in combatants)
        {
            if (combatant != null && combatant.IsAlive)
                total += combatant.CurrentHealth;
        }

        return total;
    }
    public event Action<HexCombatController> ConflictChanged;

    private bool engagementRegistered;

    private void Awake()
    {
        if (hexTile == null)
            hexTile = GetComponent<HexTile>();
    }

    private void Update()
    {
        if (hexTile == null || resolving)
            return;

        List<WaspCombatant> friendlyAttackers = GetFriendlyAttackers();
        WaspScopeRole defendingFaction = DetermineEnemyFactionAgainstFriendly();
        List<WaspCombatant> enemyAttackers = GetEnemyAttackers(defendingFaction);

        if (friendlyAttackers.Count > 0 && enemyAttackers.Count > 0)
        {
            ResetResponseWindow();
            SetConflictState(HexConflictState.AttackerBattle);
            NotifyPlayerEngagement(defendingFaction);
            RunWaspCombat(friendlyAttackers, enemyAttackers, defendingFaction);
            return;
        }

        if (friendlyAttackers.Count > 0)
        {
            HandleFriendlyOnlyAttackers(friendlyAttackers, defendingFaction);
            return;
        }

        List<WaspScopeRole> enemyFactions = GetEnemyFactionsWithAttackers();
        if (enemyFactions.Count > 1)
        {
            ResetResponseWindow();
            SetConflictState(HexConflictState.AttackerBattle);
            RunEnemyFactionCombat(enemyFactions[0], GetEnemyAttackers(enemyFactions[0]), enemyFactions[1], GetEnemyAttackers(enemyFactions[1]));
            return;
        }

        if (enemyFactions.Count > 0)
        {
            HandleEnemyOnlyAttackers(GetEnemyAttackers(enemyFactions[0]), enemyFactions[0]);
            return;
        }

        ResetResponseWindow();
        if (hexTile.HasFriendlyScout && hexTile.HasEnemyScout)
        {
            SetConflictState(HexConflictState.ScoutStandoff);
            EnemyHiveControl.Instance?.RequestCombatResponse(hexTile);
            return;
        }

        SetConflictState(HexConflictState.None);
    }

    public void NotifyOccupantsChanged()
    {
        if (hexTile == null)
            return;

        if (hexTile.HasFriendlyScout && hexTile.HasEnemyScout)
        {
            SetConflictState(HexConflictState.ScoutStandoff);
            EnemyHiveControl.Instance?.RequestCombatResponse(hexTile);
        }
    }

    public bool RecallFriendlyScout()
    {
        foreach (WaspControl wasp in new List<WaspControl>(hexTile.FriendlyWasps))
        {
            if (wasp != null && wasp.AssignedFunction == WaspFunction.Scout)
                return wasp.ReturnToHomeHive();
        }

        return false;
    }

    public bool IsCombatantEngaged(WaspCombatant combatant)
    {
        if (combatant == null || !combatant.IsAlive)
            return false;

        if (conflictState == HexConflictState.AttackerBattle)
            return GetFriendlyAttackers().Contains(combatant) || GetEnemyAttackers().Contains(combatant);

        if (conflictState != HexConflictState.HiveAssault)
            return false;

        if (combatant.IsEnemy)
            return hexTile.FriendlyHive != null && hexTile.FriendlyHive.Combatant != null && hexTile.FriendlyHive.Combatant.IsAlive;

        return hexTile.EnemyHive != null && hexTile.EnemyHive.Combatant != null && hexTile.EnemyHive.Combatant.IsAlive;
    }

    private void HandleFriendlyOnlyAttackers(List<WaspCombatant> attackers, WaspScopeRole enemyFaction)
    {
        HiveCombatant enemyHive = GetEnemyHiveCombatant(enemyFaction);
        if (enemyHive != null && enemyHive.IsAlive)
        {
            ResetResponseWindow();
            SetConflictState(HexConflictState.HiveAssault);
            NotifyPlayerEngagement(enemyFaction);
            RunHiveAssault(attackers, enemyHive, true, enemyFaction);
            return;
        }

        bool enemyGuardIncoming = GetEnemyAssignedAttackerCount(enemyFaction) > 0;
        if (HasEnemyScout(enemyFaction) || enemyGuardIncoming)
        {
            SetConflictState(HexConflictState.ScoutStandoff);
            EnemyHiveControl.Instance?.RequestCombatResponse(hexTile, enemyFaction);
            if (enemyGuardIncoming)
            {
                StartOrHoldResponseWindow(true, false);
                return;
            }

            if (!TickResponseWindow(true))
                return;

            ResolveVictory(true, attackers);
            return;
        }

        ResetResponseWindow();
        if (hexTile.State == HexTile.HexState.Enemy && hexTile.EnemyOwnerFaction == enemyFaction)
            ResolveVictory(true, attackers);
        else
            SetConflictState(HexConflictState.None);
    }

    private void HandleEnemyOnlyAttackers(List<WaspCombatant> attackers, WaspScopeRole faction)
    {
        HiveCombatant opposingEnemyHive = GetOpposingEnemyHiveCombatant(faction);
        if (opposingEnemyHive != null && opposingEnemyHive.IsAlive)
        {
            ResetResponseWindow();
            SetConflictState(HexConflictState.HiveAssault);
            RunHiveAssault(attackers, opposingEnemyHive, false, faction);
            return;
        }

        HiveCombatant friendlyHive = hexTile.FriendlyHive != null ? hexTile.FriendlyHive.Combatant : null;
        if (friendlyHive != null && friendlyHive.IsAlive)
        {
            ResetResponseWindow();
            SetConflictState(HexConflictState.HiveAssault);
            // The player is being attacked rather than attacking, but it is still an encounter with
            // this species and teaches the player just as much.
            NotifyPlayerEngagement(faction);
            RunHiveAssault(attackers, friendlyHive, false, faction);
            return;
        }

        // Taking player territory ALWAYS runs the claim timer, whatever the situation on the tile.
        // The player must get a window to respond rather than losing the hex the instant an
        // invasive attacker sets foot on it. HexTile performs the capture when the clock expires.
        if (hexTile.State == HexTile.HexState.Owned)
        {
            ResetResponseWindow();
            SetConflictState(HexConflictState.None);
            hexTile.BeginEnemyClaim(faction, ResolveClaimSeconds(faction));
            return;
        }

        bool friendlyGuardIncoming = GetFriendlyAssignedAttackerCount() > 0;
        if (hexTile.HasFriendlyScout || friendlyGuardIncoming)
        {
            SetConflictState(HexConflictState.ScoutStandoff);
            if (friendlyGuardIncoming)
            {
                StartOrHoldResponseWindow(false, false);
                return;
            }

            if (!TickResponseWindow(false))
                return;

            ResolveVictory(false, attackers);
            return;
        }

        ResetResponseWindow();
        if (hexTile.State == HexTile.HexState.Enemy && hexTile.EnemyOwnerFaction != faction)
            ResolveEnemyFactionVictory(faction, attackers);
        else
            SetConflictState(HexConflictState.None);
    }

    private bool TickResponseWindow(bool friendlySidePressing)
    {
        StartOrHoldResponseWindow(friendlySidePressing, true);
        responseTimeRemaining -= Time.deltaTime;
        hexTile.NotifyCombatInformationChanged();
        return responseTimeRemaining <= 0f;
    }

    private void StartOrHoldResponseWindow(bool friendlySidePressing, bool allowCountdown)
    {
        if (!responseActive || friendlyPressing != friendlySidePressing)
        {
            responseActive = true;
            friendlyPressing = friendlySidePressing;
            responseTimeRemaining = reinforcementResponseTime;
        }

        if (!allowCountdown)
            responseTimeRemaining = Mathf.Max(responseTimeRemaining, reinforcementResponseTime);
    }

    private void ResetResponseWindow()
    {
        responseActive = false;
        responseTimeRemaining = 0f;
    }

    private void RunWaspCombat(List<WaspCombatant> friendly, List<WaspCombatant> enemy, WaspScopeRole enemyFaction)
    {
        foreach (WaspCombatant attacker in friendly)
        {
            WaspCombatant target = FirstAlive(enemy);
            if (target == null)
                break;
            attacker.TickAttack(target, Time.deltaTime);
        }

        foreach (WaspCombatant attacker in enemy)
        {
            WaspCombatant target = FirstAlive(friendly);
            if (target == null)
                break;
            attacker.TickAttack(target, Time.deltaTime);
        }

        EliminateDead(friendly);
        EliminateDead(enemy);

        bool friendlyAlive = FirstAlive(friendly) != null;
        bool enemyAlive = FirstAlive(enemy) != null;
        if (friendlyAlive && !enemyAlive && GetEnemyAssignedAttackerCount(enemyFaction) == 0)
        {
            HiveCombatant enemyHive = GetEnemyHiveCombatant(enemyFaction);
            if (enemyHive != null && enemyHive.IsAlive)
                return;
            ResolveVictory(true, AliveOnly(friendly));
        }
        else if (enemyAlive && !friendlyAlive && GetFriendlyAssignedAttackerCount() == 0)
        {
            HiveCombatant friendlyHive = hexTile.FriendlyHive != null ? hexTile.FriendlyHive.Combatant : null;
            if (friendlyHive != null && friendlyHive.IsAlive)
                return;
            ResolveVictory(false, AliveOnly(enemy), enemyFaction);
        }
    }

    private void RunEnemyFactionCombat(WaspScopeRole firstFaction, List<WaspCombatant> first, WaspScopeRole secondFaction, List<WaspCombatant> second)
    {
        foreach (WaspCombatant attacker in first)
        {
            WaspCombatant target = FirstAlive(second);
            if (target == null)
                break;
            attacker.TickAttack(target, Time.deltaTime);
        }

        foreach (WaspCombatant attacker in second)
        {
            WaspCombatant target = FirstAlive(first);
            if (target == null)
                break;
            attacker.TickAttack(target, Time.deltaTime);
        }

        EliminateDead(first);
        EliminateDead(second);

        bool firstAlive = FirstAlive(first) != null;
        bool secondAlive = FirstAlive(second) != null;
        if (firstAlive && !secondAlive && GetEnemyAssignedAttackerCount(secondFaction) == 0)
        {
            HiveCombatant hive = GetEnemyHiveCombatant(secondFaction);
            if (hive != null && hive.IsAlive)
                return;
            ResolveEnemyFactionVictory(firstFaction, AliveOnly(first));
        }
        else if (secondAlive && !firstAlive && GetEnemyAssignedAttackerCount(firstFaction) == 0)
        {
            HiveCombatant hive = GetEnemyHiveCombatant(firstFaction);
            if (hive != null && hive.IsAlive)
                return;
            ResolveEnemyFactionVictory(secondFaction, AliveOnly(second));
        }
    }

    private void RunHiveAssault(List<WaspCombatant> attackers, HiveCombatant hive, bool friendlyAttackers, WaspScopeRole enemyFaction)
    {
        foreach (WaspCombatant attacker in attackers)
        {
            if (hive == null || !hive.IsAlive)
                break;
            attacker.TickAttack(hive, Time.deltaTime);
        }

        if (hive != null && !hive.IsAlive)
        {
            hive.Eliminate();
            ResolveVictory(friendlyAttackers, AliveOnly(attackers), enemyFaction);
        }
    }

    private void ResolveVictory(bool friendlyWon, List<WaspCombatant> winners)
    {
        ResolveVictory(friendlyWon, winners, hexTile != null ? hexTile.EnemyOwnerFaction : WaspScopeRole.PrimaryInvasive);
    }

    private void ResolveVictory(bool friendlyWon, List<WaspCombatant> winners, WaspScopeRole enemyWinnerFaction)
    {
        if (resolving)
            return;

        resolving = true;
        ResetResponseWindow();
        RetreatLosingNoncombatants(friendlyWon);
        if (friendlyWon)
            hexTile.CaptureForFriendly();
        else
            hexTile.CaptureForEnemy(enemyWinnerFaction);

        foreach (WaspCombatant winner in winners)
        {
            if (winner == null || !winner.IsAlive)
                continue;

            bool returning = friendlyWon
                ? winner.GetComponent<WaspControl>()?.ReturnToHomeHive() == true
                : winner.GetComponent<EnemyWaspControl>()?.ReturnToHomeHive() == true;
            if (!returning)
                winner.Eliminate();
        }

        SetConflictState(HexConflictState.None);
        resolving = false;
    }

    private void ResolveEnemyFactionVictory(WaspScopeRole winningFaction, List<WaspCombatant> winners)
    {
        if (resolving)
            return;

        resolving = true;
        ResetResponseWindow();
        RetreatLosingEnemyNoncombatants(winningFaction);
        hexTile.CaptureForEnemy(winningFaction);

        foreach (WaspCombatant winner in winners)
        {
            if (winner == null || !winner.IsAlive)
                continue;

            if (winner.GetComponent<EnemyWaspControl>()?.ReturnToHomeHive() != true)
                winner.Eliminate();
        }

        SetConflictState(HexConflictState.None);
        resolving = false;
    }

    private void RetreatLosingNoncombatants(bool friendlyWon)
    {
        if (friendlyWon)
        {
            foreach (EnemyWaspControl wasp in new List<EnemyWaspControl>(hexTile.EnemyWasps))
            {
                if (wasp == null || wasp.AssignedFunction == WaspFunction.Guard)
                    continue;
                if (!wasp.ReturnToHomeHive())
                    wasp.DestroyFromCombat();
            }
            return;
        }

        foreach (WaspControl wasp in new List<WaspControl>(hexTile.FriendlyWasps))
        {
            if (wasp == null || wasp.AssignedFunction == WaspFunction.Guard)
                continue;
            if (!wasp.ReturnToHomeHive())
                wasp.DestroyFromCombat();
        }
    }

    private int GetFriendlyAssignedAttackerCount()
    {
        int assigned = HiveManagement.Instance != null
            ? HiveManagement.Instance.GetAssignedWaspCount(hexTile, WaspFunction.Guard)
            : GetFriendlyAttackers().Count;
        return Mathf.Max(0, assigned - GetFriendlyAttackers().Count);
    }

    private int GetEnemyAssignedAttackerCount()
    {
        return GetEnemyAssignedAttackerCount(WaspScopeRole.PrimaryInvasive) +
               GetEnemyAssignedAttackerCount(WaspScopeRole.SecondaryInvasive);
    }

    private int GetEnemyAssignedAttackerCount(WaspScopeRole faction)
    {
        int assigned = EnemyHiveControl.Instance != null
            ? EnemyHiveControl.Instance.GetAssignedWaspCount(hexTile, WaspFunction.Guard, faction)
            : GetEnemyAttackers(faction).Count;
        return Mathf.Max(0, assigned - GetEnemyAttackers(faction).Count);
    }

    /// <summary>
    /// Claim length for the attacking faction, from its rules asset. Falls back to a sane default
    /// so a missing asset cannot make hexes flip instantly.
    /// </summary>
    private float ResolveClaimSeconds(WaspScopeRole faction)
    {
        SB_Enemy_Faction_Rules rules = EnemyHiveControl.Instance != null
            ? EnemyHiveControl.Instance.GetFactionRules(faction)
            : null;
        return rules != null ? rules.HexClaimSeconds : 40f;
    }

    private List<WaspCombatant> GetFriendlyAttackers()
    {
        List<WaspCombatant> result = new List<WaspCombatant>();
        if (hexTile == null)
            return result;

        foreach (WaspControl wasp in hexTile.FriendlyWasps)
        {
            WaspCombatant combatant = wasp != null ? wasp.Combatant : null;
            if (combatant != null && combatant.IsAttacker && combatant.IsAlive)
                result.Add(combatant);
        }

        AddFriendlyGarrison(result);
        return result;
    }

    /// <summary>
    /// Attackers idling at their home hive only count as "stationed" once they have been dispatched
    /// somewhere. Without this they sit on their own hex during an attack and never defend it, and
    /// the player has to re-order them onto the tile they are already standing on.
    /// </summary>
    private void AddFriendlyGarrison(List<WaspCombatant> result)
    {
        HiveManagement hive = HiveManagement.Instance;
        if (hive == null)
            return;

        foreach (WaspControl wasp in hive.FriendlyWasps)
        {
            if (wasp == null || !wasp.IsAlive)
                continue;

            // Only units sitting at home with no posting elsewhere.
            if (wasp.StationedHex != null || wasp.TargetHex != null)
                continue;
            if (wasp.HomeHive == null || wasp.HomeHive.OwnerHex != hexTile)
                continue;

            WaspCombatant combatant = wasp.Combatant;
            if (combatant != null && combatant.IsAttacker && combatant.IsAlive && !result.Contains(combatant))
                result.Add(combatant);
        }
    }

    private List<WaspCombatant> GetEnemyAttackers()
    {
        List<WaspCombatant> result = new List<WaspCombatant>();
        result.AddRange(GetEnemyAttackers(WaspScopeRole.PrimaryInvasive));
        result.AddRange(GetEnemyAttackers(WaspScopeRole.SecondaryInvasive));
        return result;
    }

    private List<WaspCombatant> GetEnemyAttackers(WaspScopeRole faction)
    {
        List<WaspCombatant> result = new List<WaspCombatant>();
        if (hexTile == null)
            return result;

        foreach (EnemyWaspControl wasp in hexTile.EnemyWasps)
        {
            WaspCombatant combatant = wasp != null ? wasp.Combatant : null;
            if (combatant != null && wasp.Faction == faction && combatant.IsAttacker && combatant.IsAlive)
                result.Add(combatant);
        }

        AddEnemyGarrison(result, faction);
        return result;
    }

    /// <summary>
    /// Mirror of <see cref="AddFriendlyGarrison"/>: invasive attackers sitting at their own hive
    /// defend it, so an enemy home hex is not free to walk into either.
    /// </summary>
    private void AddEnemyGarrison(List<WaspCombatant> result, WaspScopeRole faction)
    {
        EnemyHiveControl control = EnemyHiveControl.Instance;
        if (control == null || hexTile == null)
            return;

        foreach (EnemyWaspControl wasp in control.GetFaction(faction))
        {
            if (wasp == null)
                continue;
            if (wasp.StationedHex != null || wasp.TargetHex != null)
                continue;
            if (wasp.HomeHive == null || wasp.HomeHive.OwnerHex != hexTile)
                continue;

            WaspCombatant combatant = wasp.Combatant;
            if (combatant != null && combatant.IsAttacker && combatant.IsAlive && !result.Contains(combatant))
                result.Add(combatant);
        }
    }

    private List<WaspScopeRole> GetEnemyFactionsWithAttackers()
    {
        List<WaspScopeRole> result = new List<WaspScopeRole>();
        if (GetEnemyAttackers(WaspScopeRole.PrimaryInvasive).Count > 0)
            result.Add(WaspScopeRole.PrimaryInvasive);
        if (GetEnemyAttackers(WaspScopeRole.SecondaryInvasive).Count > 0)
            result.Add(WaspScopeRole.SecondaryInvasive);
        return result;
    }

    private WaspScopeRole DetermineEnemyFactionAgainstFriendly()
    {
        if (hexTile == null)
            return WaspScopeRole.PrimaryInvasive;

        if (hexTile.EnemyHive != null)
            return hexTile.EnemyHive.Faction;

        if (hexTile.State == HexTile.HexState.Enemy)
            return hexTile.EnemyOwnerFaction;

        foreach (EnemyWaspControl wasp in hexTile.EnemyWasps)
        {
            if (wasp != null)
                return wasp.Faction;
        }

        return WaspScopeRole.PrimaryInvasive;
    }

    private bool HasEnemyScout(WaspScopeRole faction)
    {
        if (hexTile == null)
            return false;

        foreach (EnemyWaspControl wasp in hexTile.EnemyWasps)
        {
            if (wasp != null && wasp.Faction == faction && wasp.AssignedFunction == WaspFunction.Scout)
                return true;
        }

        return false;
    }

    private HiveCombatant GetEnemyHiveCombatant(WaspScopeRole faction)
    {
        if (hexTile == null || hexTile.EnemyHive == null || hexTile.EnemyHive.Faction != faction)
            return null;

        return hexTile.EnemyHive.Combatant;
    }

    private HiveCombatant GetOpposingEnemyHiveCombatant(WaspScopeRole faction)
    {
        if (hexTile == null || hexTile.EnemyHive == null || hexTile.EnemyHive.Faction == faction)
            return null;

        return hexTile.EnemyHive.Combatant;
    }

    private void RetreatLosingEnemyNoncombatants(WaspScopeRole winningFaction)
    {
        foreach (EnemyWaspControl wasp in new List<EnemyWaspControl>(hexTile.EnemyWasps))
        {
            if (wasp == null || wasp.Faction == winningFaction || wasp.AssignedFunction == WaspFunction.Guard)
                continue;
            if (!wasp.ReturnToHomeHive())
                wasp.DestroyFromCombat();
        }
    }

    private static WaspCombatant FirstAlive(List<WaspCombatant> combatants)
    {
        foreach (WaspCombatant combatant in combatants)
        {
            if (combatant != null && combatant.IsAlive)
                return combatant;
        }
        return null;
    }

    private static List<WaspCombatant> AliveOnly(List<WaspCombatant> combatants)
    {
        return combatants.FindAll(combatant => combatant != null && combatant.IsAlive);
    }

    private static void EliminateDead(List<WaspCombatant> combatants)
    {
        foreach (WaspCombatant combatant in combatants)
        {
            if (combatant != null && !combatant.IsAlive)
                combatant.Eliminate();
        }
    }

    /// <summary>
    /// Counts one combat engagement against a faction towards its identification codex. Latched, so a
    /// battle running over many frames only ever counts once; the latch clears when the tile falls
    /// quiet, which makes the next fight here a fresh encounter.
    ///
    /// Only called from player-versus-invasive paths. Invasive-versus-invasive fighting teaches the
    /// player nothing they were present for, so it must not count.
    /// </summary>
    private void NotifyPlayerEngagement(WaspScopeRole faction)
    {
        if (engagementRegistered)
            return;

        engagementRegistered = true;
        if (SpeciesCodex.Instance != null)
            SpeciesCodex.Instance.RegisterEngagement(faction);
    }

    private void SetConflictState(HexConflictState value)
    {
        if (conflictState == value)
            return;

        if (value == HexConflictState.None)
            engagementRegistered = false;

        conflictState = value;
        hexTile?.NotifyCombatInformationChanged();
        ConflictChanged?.Invoke(this);
    }
}
