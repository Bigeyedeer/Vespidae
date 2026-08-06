using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemyHiveControl : MonoBehaviour
{
    public static EnemyHiveControl Instance { get; private set; }

    [Header("Faction Species")]
    [SerializeField] private SB_Wasps_Info nativeSpecies;
    [SerializeField] private SB_Wasps_Info primaryInvasiveSpecies;
    [SerializeField] private SB_Wasps_Info secondaryInvasiveSpecies;

    [Header("Role Skill Assets")]
    [SerializeField] private SB_Wasp_Skill scoutSkill;
    [SerializeField] private SB_Wasp_Skill foragerSkill;
    [SerializeField] private SB_Wasp_Skill builderSkill;
    [SerializeField] private SB_Wasp_Skill broodCaretakerSkill;
    [SerializeField] private SB_Wasp_Skill guardSkill;
    [SerializeField] private SB_Wasp_Skill containmentSkill;

    [Header("Enemy Startup Spawning")]
    [SerializeField] private GameObject enemyHivePrefab;
    [SerializeField] private GameObject[] enemyWaspPrefabs;
    [SerializeField] private bool spawnEnemyStartup = true;
    [SerializeField] private bool spawnOneEnemyWasp = true;
    [SerializeField] private bool autoRegisterSceneWasps = true;

    [Header("Enemy AI")]
    [SerializeField, Min(1f)] private float decisionInterval = 2f;
    [SerializeField, Min(1f)] private float progressionIntervalMinimum = 45f;
    [SerializeField, Min(1f)] private float progressionIntervalMaximum = 60f;
    [SerializeField, Min(30f)] private float openingExpansionDuration = 300f;
    [SerializeField, Min(1f)] private float openingExpansionIntervalMinimum = 45f;
    [SerializeField, Min(1f)] private float openingExpansionIntervalMaximum = 60f;
    [SerializeField, Min(30f)] private float expansionIntervalMinimum = 180f;
    [SerializeField, Min(30f)] private float expansionIntervalMaximum = 300f;
    [SerializeField, Min(60f)] private float enemySkillProgressionInterval = 600f;
    [SerializeField, Range(1, 5)] private int maximumIdleGuardReserve = 5;

    private readonly Dictionary<WaspScopeRole, List<EnemyWaspControl>> factions = new Dictionary<WaspScopeRole, List<EnemyWaspControl>>();
    private readonly List<C_Enemy_Hive_Orc> spawnedEnemyHives = new List<C_Enemy_Hive_Orc>();
    private readonly Dictionary<C_Enemy_Hive_Orc, float> guardTrainingTimers = new Dictionary<C_Enemy_Hive_Orc, float>();
    private readonly Dictionary<C_Enemy_Hive_Orc, float> guardTrainingIntervals = new Dictionary<C_Enemy_Hive_Orc, float>();
    private readonly HashSet<HexTile> pendingCombatTargets = new HashSet<HexTile>();
    private readonly int[] skillLevels = new int[Enum.GetValues(typeof(WaspFunction)).Length];
    private readonly WaspFunction[] progressionFunctions = { WaspFunction.Scout, WaspFunction.Guard };
    private bool enemyStartupSpawned;
    private float decisionTimer;
    private float scoutTimer;
    private float scoutInterval;
    private float skillProgressionTimer;
    private float enemyElapsedTime;
    private int progressionFunctionIndex;
    private int nextExpansionHiveIndex;

    public IReadOnlyList<EnemyWaspControl> NativeFaction => GetFaction(WaspScopeRole.NativePlayer);
    public IReadOnlyList<EnemyWaspControl> PrimaryInvasiveFaction => GetFaction(WaspScopeRole.PrimaryInvasive);
    public IReadOnlyList<EnemyWaspControl> SecondaryInvasiveFaction => GetFaction(WaspScopeRole.SecondaryInvasive);
    public SB_Wasps_Info NativeSpecies => nativeSpecies;
    public SB_Wasps_Info PrimaryInvasiveSpecies => primaryInvasiveSpecies;
    public SB_Wasps_Info SecondaryInvasiveSpecies => secondaryInvasiveSpecies;
    public GameObject EnemyHivePrefab => enemyHivePrefab;
    public IReadOnlyList<GameObject> EnemyWaspPrefabs => enemyWaspPrefabs;
    public IReadOnlyList<C_Enemy_Hive_Orc> SpawnedEnemyHives => spawnedEnemyHives;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        EnsureFactions();
        scoutInterval = GetNextExpansionInterval();
    }

    private void Start()
    {
        if (autoRegisterSceneWasps)
        {
            EnemyWaspControl[] sceneWasps = FindObjectsByType<EnemyWaspControl>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (EnemyWaspControl wasp in sceneWasps)
                Register(wasp);
        }

        SpawnEnemyStartup();
    }

    private void Update()
    {
        UpdateGuardTraining();
        UpdateSkillProgression();

        enemyElapsedTime += Time.deltaTime;
        decisionTimer += Time.deltaTime;
        scoutTimer += Time.deltaTime;
        if (decisionTimer >= decisionInterval)
        {
            decisionTimer = 0f;
            RunCombatResponse();
        }

        if (scoutTimer >= scoutInterval)
        {
            scoutTimer = 0f;
            scoutInterval = GetNextExpansionInterval();
            RunScoutExpansion();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public int GetSkillLevel(WaspFunction function)
    {
        return skillLevels[(int)function];
    }

    public SB_Wasp_Skill GetSkillDefinition(WaspFunction function)
    {
        switch (function)
        {
            case WaspFunction.Scout: return scoutSkill;
            case WaspFunction.Forager: return foragerSkill;
            case WaspFunction.Builder: return builderSkill;
            case WaspFunction.BroodCaretaker: return broodCaretakerSkill;
            case WaspFunction.Guard: return guardSkill;
            case WaspFunction.Containment: return containmentSkill;
            default: return null;
        }
    }

    public float GetEffectiveValue(WaspFunction function, WaspSkillStat stat)
    {
        SB_Wasp_Skill definition = GetSkillDefinition(function);
        return definition != null ? definition.GetEffectiveValue(stat, GetSkillLevel(function)) : 1f;
    }

    public void Register(EnemyWaspControl wasp)
    {
        if (wasp == null)
            return;

        EnsureFactions();
        RemoveFromAllFactions(wasp);
        factions[wasp.Faction].Add(wasp);
    }

    public void Unregister(EnemyWaspControl wasp)
    {
        if (wasp != null)
            RemoveFromAllFactions(wasp);
    }

    public void RequestCombatResponse(HexTile target)
    {
        if (target == null)
            return;

        pendingCombatTargets.Add(target);
        DispatchGuards(target);
    }

    public void RefreshRegistration(EnemyWaspControl wasp)
    {
        Register(wasp);
    }

    public IReadOnlyList<EnemyWaspControl> GetFaction(WaspScopeRole faction)
    {
        EnsureFactions();
        factions[faction].RemoveAll(wasp => wasp == null);
        return factions[faction];
    }

    public int GetFactionCount(WaspScopeRole faction)
    {
        return GetFaction(faction).Count;
    }

    public SB_Wasps_Info GetFactionSpecies(WaspScopeRole faction)
    {
        switch (faction)
        {
            case WaspScopeRole.NativePlayer: return nativeSpecies;
            case WaspScopeRole.PrimaryInvasive: return primaryInvasiveSpecies;
            case WaspScopeRole.SecondaryInvasive: return secondaryInvasiveSpecies;
            default: return null;
        }
    }

    public void SetFactionAlert(WaspScopeRole faction, bool value)
    {
        foreach (EnemyWaspControl wasp in GetFaction(faction))
            wasp?.SetAlerted(value);
    }

    public void SetFactionDestination(WaspScopeRole faction, Vector3 worldPosition)
    {
        foreach (EnemyWaspControl wasp in GetFaction(faction))
            wasp?.SetDestination(worldPosition);
    }

    public void SpawnEnemyStartup()
    {
        if (enemyStartupSpawned)
            return;

        enemyStartupSpawned = true;
        if (!spawnEnemyStartup || enemyHivePrefab == null)
            return;

        HexTile[] tiles = FindObjectsByType<HexTile>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        int enemyTileIndex = 0;
        foreach (HexTile tile in tiles)
        {
            if (tile == null || tile.State != HexTile.HexState.Enemy)
                continue;

            Transform spawnPoint = tile.HiveSpawnPoint;
            GameObject hiveObject = Instantiate(enemyHivePrefab, spawnPoint.position, spawnPoint.rotation);
            C_Enemy_Hive_Orc hive = hiveObject.GetComponent<C_Enemy_Hive_Orc>();
            if (hive == null)
            {
                Destroy(hiveObject);
                continue;
            }

            GameObject speciesPrefab = GetEnemyWaspPrefab(enemyTileIndex);
            hive.Initialize(tile, enemyWaspPrefabs);
            hive.SetDefaultWaspPrefab(speciesPrefab);
            spawnedEnemyHives.Add(hive);
            guardTrainingTimers[hive] = 0f;
            guardTrainingIntervals[hive] = GetNextProgressionInterval();
            tile.SetEnemyHive(hive);
            if (spawnOneEnemyWasp)
                hive.SpawnWasp(speciesPrefab, WaspFunction.Scout);
            enemyTileIndex++;
        }
    }

    public EnemyWaspControl SpawnEnemyWasp(C_Enemy_Hive_Orc hive, int speciesIndex)
    {
        return hive != null ? hive.SpawnWasp(GetEnemyWaspPrefab(speciesIndex), WaspFunction.Scout) : null;
    }

    public void HandleHiveDestroyed(C_Enemy_Hive_Orc hive)
    {
        if (hive == null)
            return;

        spawnedEnemyHives.Remove(hive);
        guardTrainingTimers.Remove(hive);
        guardTrainingIntervals.Remove(hive);
        if (hive.OwnerHex != null && hive.OwnerHex.EnemyHive == hive)
            hive.OwnerHex.SetEnemyHive(null);
    }

    public int GetAvailableWaspCount(C_Enemy_Hive_Orc hive, WaspFunction function)
    {
        int count = 0;
        foreach (EnemyWaspControl wasp in GetAllWasps())
        {
            if (wasp != null && wasp.HomeHive == hive && wasp.AssignedFunction == function && wasp.IsAvailable)
                count++;
        }
        return count;
    }

    public int GetAssignedWaspCount(HexTile target, WaspFunction function)
    {
        int count = 0;
        foreach (EnemyWaspControl wasp in GetAllWasps())
        {
            if (wasp != null && wasp.AssignedFunction == function && (wasp.TargetHex == target || wasp.StationedHex == target))
                count++;
        }
        return count;
    }

    private void UpdateGuardTraining()
    {
        foreach (C_Enemy_Hive_Orc hive in new List<C_Enemy_Hive_Orc>(spawnedEnemyHives))
        {
            if (hive == null)
                continue;

            guardTrainingTimers.TryGetValue(hive, out float timer);
            if (!guardTrainingIntervals.TryGetValue(hive, out float interval))
                interval = GetNextProgressionInterval();
            timer += Time.deltaTime;
            if (timer >= interval)
            {
                timer = 0f;
                interval = GetNextProgressionInterval();
                if (GetAvailableWaspCount(hive, WaspFunction.Guard) < maximumIdleGuardReserve)
                {
                    EnemyWaspControl guard = hive.SpawnWasp(hive.DefaultWaspPrefab, WaspFunction.Guard);
                    if (guard != null)
                        RunCombatResponse();
                }
            }
            guardTrainingTimers[hive] = timer;
            guardTrainingIntervals[hive] = interval;
        }
    }

    private void UpdateSkillProgression()
    {
        skillProgressionTimer += Time.deltaTime;
        if (skillProgressionTimer < enemySkillProgressionInterval)
            return;

        skillProgressionTimer = 0f;

        for (int offset = 0; offset < progressionFunctions.Length; offset++)
        {
            int index = (progressionFunctionIndex + offset) % progressionFunctions.Length;
            WaspFunction function = progressionFunctions[index];
            SB_Wasp_Skill definition = GetSkillDefinition(function);
            if (definition == null || skillLevels[(int)function] >= definition.MaximumLevel)
                continue;

            skillLevels[(int)function]++;
            progressionFunctionIndex = (index + 1) % progressionFunctions.Length;
            foreach (EnemyWaspControl wasp in GetAllWasps())
            {
                if (wasp != null && wasp.AssignedFunction == function)
                    wasp.RefreshSkillStats();
            }
            break;
        }
    }

    private float GetNextProgressionInterval()
    {
        float minimum = Mathf.Max(1f, progressionIntervalMinimum);
        float maximum = Mathf.Max(minimum, progressionIntervalMaximum);
        return UnityEngine.Random.Range(minimum, maximum);
    }

    private float GetNextExpansionInterval()
    {
        float minimum = IsOpeningExpansionPhase
            ? Mathf.Max(1f, openingExpansionIntervalMinimum)
            : Mathf.Max(30f, expansionIntervalMinimum);
        float maximum = IsOpeningExpansionPhase
            ? Mathf.Max(minimum, openingExpansionIntervalMaximum)
            : Mathf.Max(minimum, expansionIntervalMaximum);
        return UnityEngine.Random.Range(minimum, maximum);
    }

    private bool IsOpeningExpansionPhase => enemyElapsedTime < openingExpansionDuration;

    private void RunCombatResponse()
    {
        HexTile[] tiles = FindObjectsByType<HexTile>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        pendingCombatTargets.RemoveWhere(target => target == null || !RequiresCombatResponse(target));
        foreach (HexTile target in new List<HexTile>(pendingCombatTargets))
            DispatchGuards(target);

        foreach (HexTile tile in tiles)
        {
            if (tile?.CombatController == null)
                continue;
            if (tile.CombatController.HasScoutStandoff ||
                tile.CombatController.ConflictState == HexConflictState.AttackerBattle ||
                tile.CombatController.ConflictState == HexConflictState.HiveAssault)
            {
                pendingCombatTargets.Add(tile);
                DispatchGuards(tile);
            }
        }

        foreach (C_Enemy_Hive_Orc hive in spawnedEnemyHives)
        {
            if (hive?.OwnerHex == null || HexProgressionManager.Instance == null)
                continue;
            foreach (HexTile neighbour in HexProgressionManager.Instance.GetConnectedHexes(hive.OwnerHex))
            {
                if (neighbour != null && neighbour.State == HexTile.HexState.Owned)
                {
                    DispatchGuards(neighbour);
                    break;
                }
            }
        }
    }

    private bool RequiresCombatResponse(HexTile target)
    {
        if (target?.CombatController == null)
            return false;

        return target.CombatController.ConflictState != HexConflictState.None ||
               target.State == HexTile.HexState.Owned;
    }

    private void DispatchGuards(HexTile target)
    {
        if (target == null)
            return;

        int maximum = target.CombatController != null ? target.CombatController.MaximumAttackersPerSide : 5;
        int assigned = GetAssignedWaspCount(target, WaspFunction.Guard);
        int desired = Mathf.Clamp(target.GetFriendlyWaspCount(WaspFunction.Guard) + 1, 1, maximum);
        int toSend = Mathf.Max(0, desired - assigned);
        if (toSend <= 0)
            return;

        List<C_Enemy_Hive_Orc> orderedHives = new List<C_Enemy_Hive_Orc>(spawnedEnemyHives);
        orderedHives.RemoveAll(hive => hive == null || hive.OwnerHex == null);
        orderedHives.Sort((left, right) =>
            Vector3.SqrMagnitude(left.OwnerHex.transform.position - target.transform.position)
                .CompareTo(Vector3.SqrMagnitude(right.OwnerHex.transform.position - target.transform.position)));

        foreach (C_Enemy_Hive_Orc hive in orderedHives)
        {
            foreach (EnemyWaspControl wasp in GetAllWasps())
            {
                if (toSend <= 0)
                    return;
                if (wasp == null || wasp.HomeHive != hive || wasp.AssignedFunction != WaspFunction.Guard || !wasp.IsAvailable)
                    continue;

                int formationIndex = target.EnemyWaspCount + GetAssignedWaspCount(target, WaspFunction.Guard);
                Vector3 position = target.GetWaspFormationPosition(formationIndex, 0.25f, 0.25f);
                if (wasp.DispatchToHex(target, position))
                    toSend--;
            }
        }
    }

    private void RunScoutExpansion()
    {
        int hiveCount = spawnedEnemyHives.Count;
        if (hiveCount == 0)
            return;

        if (IsOpeningExpansionPhase)
        {
            foreach (C_Enemy_Hive_Orc hive in spawnedEnemyHives)
                TryDispatchExpansionScout(hive);
            return;
        }

        for (int offset = 0; offset < hiveCount; offset++)
        {
            int index = (nextExpansionHiveIndex + offset) % hiveCount;
            C_Enemy_Hive_Orc hive = spawnedEnemyHives[index];
            if (TryDispatchExpansionScout(hive))
            {
                nextExpansionHiveIndex = (index + 1) % hiveCount;
                return;
            }
        }

        nextExpansionHiveIndex = (nextExpansionHiveIndex + 1) % hiveCount;
    }

    private bool TryDispatchExpansionScout(C_Enemy_Hive_Orc hive)
    {
        if (hive == null)
            return false;

        EnemyWaspControl scout = FindScout(hive);
        if (scout != null && scout.StationedHex != null &&
            scout.StationedHex.CombatController != null &&
            scout.StationedHex.CombatController.HasScoutStandoff)
        {
            return false;
        }

        HexTile target = ChooseScoutTarget(hive, scout);
        return scout != null &&
               target != null &&
               scout.WorkforceState != WaspWorkforceState.Travelling &&
               scout.DispatchToHex(target);
    }

    private EnemyWaspControl FindScout(C_Enemy_Hive_Orc hive)
    {
        foreach (EnemyWaspControl wasp in GetAllWasps())
        {
            if (wasp != null && wasp.HomeHive == hive && wasp.AssignedFunction == WaspFunction.Scout)
                return wasp;
        }
        return null;
    }

    private HexTile ChooseScoutTarget(C_Enemy_Hive_Orc hive, EnemyWaspControl scout)
    {
        if (HexProgressionManager.Instance == null || hive?.OwnerHex == null)
            return null;

        HexTile source = scout != null && scout.StationedHex != null && scout.StationedHex.State == HexTile.HexState.Enemy
            ? scout.StationedHex
            : hive.OwnerHex;
        foreach (HexTile candidate in HexProgressionManager.Instance.GetConnectedHexes(source))
        {
            if (candidate != null && candidate.State != HexTile.HexState.Enemy && candidate.State != HexTile.HexState.Owned)
                return candidate;
        }
        return null;
    }

    private List<EnemyWaspControl> GetAllWasps()
    {
        List<EnemyWaspControl> result = new List<EnemyWaspControl>();
        foreach (List<EnemyWaspControl> list in factions.Values)
        {
            list.RemoveAll(wasp => wasp == null);
            result.AddRange(list);
        }
        return result;
    }

    private void EnsureFactions()
    {
        if (factions.Count > 0)
            return;
        factions[WaspScopeRole.NativePlayer] = new List<EnemyWaspControl>();
        factions[WaspScopeRole.PrimaryInvasive] = new List<EnemyWaspControl>();
        factions[WaspScopeRole.SecondaryInvasive] = new List<EnemyWaspControl>();
    }

    private void RemoveFromAllFactions(EnemyWaspControl wasp)
    {
        foreach (List<EnemyWaspControl> faction in factions.Values)
            faction.Remove(wasp);
    }

    private GameObject GetEnemyWaspPrefab(int speciesIndex)
    {
        if (enemyWaspPrefabs == null || enemyWaspPrefabs.Length == 0)
            return null;
        return enemyWaspPrefabs[Mathf.Clamp(speciesIndex, 0, enemyWaspPrefabs.Length - 1)];
    }
}
