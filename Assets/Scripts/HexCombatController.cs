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
    [SerializeField, Range(1, 5)] private int maximumAttackersPerSide = 5;
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
    public event Action<HexCombatController> ConflictChanged;

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
        List<WaspCombatant> enemyAttackers = GetEnemyAttackers();

        if (friendlyAttackers.Count > 0 && enemyAttackers.Count > 0)
        {
            ResetResponseWindow();
            SetConflictState(HexConflictState.AttackerBattle);
            RunWaspCombat(friendlyAttackers, enemyAttackers);
            return;
        }

        if (friendlyAttackers.Count > 0)
        {
            HandleFriendlyOnlyAttackers(friendlyAttackers);
            return;
        }

        if (enemyAttackers.Count > 0)
        {
            HandleEnemyOnlyAttackers(enemyAttackers);
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

    private void HandleFriendlyOnlyAttackers(List<WaspCombatant> attackers)
    {
        HiveCombatant enemyHive = hexTile.EnemyHive != null ? hexTile.EnemyHive.Combatant : null;
        if (enemyHive != null && enemyHive.IsAlive)
        {
            ResetResponseWindow();
            SetConflictState(HexConflictState.HiveAssault);
            RunHiveAssault(attackers, enemyHive, true);
            return;
        }

        bool enemyGuardIncoming = GetEnemyAssignedAttackerCount() > 0;
        if (hexTile.HasEnemyScout || enemyGuardIncoming)
        {
            SetConflictState(HexConflictState.ScoutStandoff);
            EnemyHiveControl.Instance?.RequestCombatResponse(hexTile);
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
        if (hexTile.State == HexTile.HexState.Enemy)
            ResolveVictory(true, attackers);
        else
            SetConflictState(HexConflictState.None);
    }

    private void HandleEnemyOnlyAttackers(List<WaspCombatant> attackers)
    {
        HiveCombatant friendlyHive = hexTile.FriendlyHive != null ? hexTile.FriendlyHive.Combatant : null;
        if (friendlyHive != null && friendlyHive.IsAlive)
        {
            ResetResponseWindow();
            SetConflictState(HexConflictState.HiveAssault);
            RunHiveAssault(attackers, friendlyHive, false);
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
        if (hexTile.State == HexTile.HexState.Owned)
            ResolveVictory(false, attackers);
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

    private void RunWaspCombat(List<WaspCombatant> friendly, List<WaspCombatant> enemy)
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
        if (friendlyAlive && !enemyAlive && GetEnemyAssignedAttackerCount() == 0)
            ResolveVictory(true, AliveOnly(friendly));
        else if (enemyAlive && !friendlyAlive && GetFriendlyAssignedAttackerCount() == 0)
            ResolveVictory(false, AliveOnly(enemy));
    }

    private void RunHiveAssault(List<WaspCombatant> attackers, HiveCombatant hive, bool friendlyAttackers)
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
            ResolveVictory(friendlyAttackers, AliveOnly(attackers));
        }
    }

    private void ResolveVictory(bool friendlyWon, List<WaspCombatant> winners)
    {
        if (resolving)
            return;

        resolving = true;
        ResetResponseWindow();
        RetreatLosingNoncombatants(friendlyWon);
        if (friendlyWon)
            hexTile.CaptureForFriendly();
        else
            hexTile.CaptureForEnemy();

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
        int assigned = EnemyHiveControl.Instance != null
            ? EnemyHiveControl.Instance.GetAssignedWaspCount(hexTile, WaspFunction.Guard)
            : GetEnemyAttackers().Count;
        return Mathf.Max(0, assigned - GetEnemyAttackers().Count);
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
        return result;
    }

    private List<WaspCombatant> GetEnemyAttackers()
    {
        List<WaspCombatant> result = new List<WaspCombatant>();
        if (hexTile == null)
            return result;

        foreach (EnemyWaspControl wasp in hexTile.EnemyWasps)
        {
            WaspCombatant combatant = wasp != null ? wasp.Combatant : null;
            if (combatant != null && combatant.IsAttacker && combatant.IsAlive)
                result.Add(combatant);
        }
        return result;
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

    private void SetConflictState(HexConflictState value)
    {
        if (conflictState == value)
            return;

        conflictState = value;
        hexTile?.NotifyCombatInformationChanged();
        ConflictChanged?.Invoke(this);
    }
}
