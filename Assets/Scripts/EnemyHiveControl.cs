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
    [SerializeField, Range(1, 20)] private int maximumIdleGuardReserve = 20;

    private readonly Dictionary<WaspScopeRole, List<EnemyWaspControl>> factions = new Dictionary<WaspScopeRole, List<EnemyWaspControl>>();
    private readonly List<C_Enemy_Hive_Orc> spawnedEnemyHives = new List<C_Enemy_Hive_Orc>();
    private readonly Dictionary<C_Enemy_Hive_Orc, float> guardTrainingTimers = new Dictionary<C_Enemy_Hive_Orc, float>();
    private readonly Dictionary<C_Enemy_Hive_Orc, float> guardTrainingIntervals = new Dictionary<C_Enemy_Hive_Orc, float>();
    private readonly Dictionary<HexTile, HashSet<WaspScopeRole>> pendingCombatTargets = new Dictionary<HexTile, HashSet<WaspScopeRole>>();
    private readonly Dictionary<WaspScopeRole, int[]> factionSkillLevels = new Dictionary<WaspScopeRole, int[]>();
    private readonly Dictionary<WaspScopeRole, float> skillProgressionTimers = new Dictionary<WaspScopeRole, float>();
    private readonly Dictionary<WaspScopeRole, int> progressionFunctionIndexes = new Dictionary<WaspScopeRole, int>();
    private readonly WaspFunction[] progressionFunctions = { WaspFunction.Scout, WaspFunction.Guard };
    private bool enemyStartupSpawned;
    private float decisionTimer;
    private float scoutTimer;
    private float scoutInterval;
    private float enemyElapsedTime;
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
        EnsureFactionSkills();
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
        return GetSkillLevel(WaspScopeRole.PrimaryInvasive, function);
    }

    public int GetSkillLevel(WaspScopeRole faction, WaspFunction function)
    {
        EnsureFactionSkills();
        return factionSkillLevels[NormalizeEnemyFaction(faction)][(int)function];
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
        return GetEffectiveValue(WaspScopeRole.PrimaryInvasive, function, stat);
    }

    public float GetEffectiveValue(WaspScopeRole faction, WaspFunction function, WaspSkillStat stat)
    {
        SB_Wasp_Skill definition = GetSkillDefinition(function);
        return definition != null ? definition.GetEffectiveValue(stat, GetSkillLevel(faction, function)) : 1f;
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

        foreach (WaspScopeRole faction in GetRespondingFactions(target))
            RequestCombatResponse(target, faction);
    }

    public void RequestCombatResponse(HexTile target, WaspScopeRole faction)
    {
        if (target == null)
            return;

        faction = NormalizeEnemyFaction(faction);
        if (!pendingCombatTargets.TryGetValue(target, out HashSet<WaspScopeRole> factionsForTarget))
        {
            factionsForTarget = new HashSet<WaspScopeRole>();
            pendingCombatTargets[target] = factionsForTarget;
        }

        factionsForTarget.Add(faction);
        DispatchGuards(target, faction);
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
        List<HexTile> enemyTiles = new List<HexTile>();
        foreach (HexTile tile in tiles)
        {
            if (tile != null && tile.State == HexTile.HexState.Enemy)
                enemyTiles.Add(tile);
        }
        enemyTiles.Sort((left, right) => right.transform.position.z.CompareTo(left.transform.position.z));

        int enemyTileIndex = 0;
        foreach (HexTile tile in enemyTiles)
        {
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
        return GetAssignedWaspCount(target, function, WaspScopeRole.PrimaryInvasive) +
               GetAssignedWaspCount(target, function, WaspScopeRole.SecondaryInvasive);
    }

    public int GetAssignedWaspCount(HexTile target, WaspFunction function, WaspScopeRole faction)
    {
        int count = 0;
        faction = NormalizeEnemyFaction(faction);
        foreach (EnemyWaspControl wasp in GetAllWasps())
        {
            if (wasp != null &&
                wasp.Faction == faction &&
                wasp.AssignedFunction == function &&
                (wasp.TargetHex == target || wasp.StationedHex == target))
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
        EnsureFactionSkills();
        AdvanceFactionSkills(WaspScopeRole.PrimaryInvasive);
        AdvanceFactionSkills(WaspScopeRole.SecondaryInvasive);
    }

    private void AdvanceFactionSkills(WaspScopeRole faction)
    {
        faction = NormalizeEnemyFaction(faction);
        skillProgressionTimers[faction] += Time.deltaTime;
        if (skillProgressionTimers[faction] < enemySkillProgressionInterval)
            return;

        skillProgressionTimers[faction] = 0f;
        int[] levels = factionSkillLevels[faction];
        int progressionIndex = progressionFunctionIndexes[faction];

        for (int offset = 0; offset < progressionFunctions.Length; offset++)
        {
            int index = (progressionIndex + offset) % progressionFunctions.Length;
            WaspFunction function = progressionFunctions[index];
            SB_Wasp_Skill definition = GetSkillDefinition(function);
            if (definition == null || levels[(int)function] >= definition.MaximumLevel)
                continue;

            levels[(int)function]++;
            progressionFunctionIndexes[faction] = (index + 1) % progressionFunctions.Length;
            foreach (EnemyWaspControl wasp in GetFaction(faction))
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
        foreach (HexTile target in new List<HexTile>(pendingCombatTargets.Keys))
        {
            if (target == null || !RequiresCombatResponse(target))
            {
                pendingCombatTargets.Remove(target);
                continue;
            }

            foreach (WaspScopeRole faction in new List<WaspScopeRole>(pendingCombatTargets[target]))
                DispatchGuards(target, faction);
        }

        foreach (HexTile tile in tiles)
        {
            if (tile?.CombatController == null)
                continue;
            if (tile.CombatController.HasScoutStandoff ||
                tile.CombatController.ConflictState == HexConflictState.AttackerBattle ||
                tile.CombatController.ConflictState == HexConflictState.HiveAssault)
            {
                foreach (WaspScopeRole faction in GetRespondingFactions(tile))
                    RequestCombatResponse(tile, faction);
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
                    DispatchGuards(neighbour, hive.Faction);
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

    private void DispatchGuards(HexTile target, WaspScopeRole faction)
    {
        if (target == null)
            return;

        faction = NormalizeEnemyFaction(faction);
        int maximum = target.CombatController != null ? target.CombatController.MaximumAttackersPerSide : 20;
        int assigned = GetAssignedWaspCount(target, WaspFunction.Guard, faction);
        int desired = Mathf.Clamp(GetOpposingGuardCount(target, faction) + 1, 1, maximum);
        int toSend = Mathf.Max(0, desired - assigned);
        if (toSend <= 0)
            return;

        List<C_Enemy_Hive_Orc> orderedHives = new List<C_Enemy_Hive_Orc>(spawnedEnemyHives);
        orderedHives.RemoveAll(hive => hive == null || hive.OwnerHex == null || hive.Faction != faction);
        orderedHives.Sort((left, right) =>
            Vector3.SqrMagnitude(left.OwnerHex.transform.position - target.transform.position)
                .CompareTo(Vector3.SqrMagnitude(right.OwnerHex.transform.position - target.transform.position)));

        foreach (C_Enemy_Hive_Orc hive in orderedHives)
        {
            foreach (EnemyWaspControl wasp in GetFaction(faction))
            {
                if (toSend <= 0)
                    return;
                if (wasp == null || wasp.HomeHive != hive || wasp.AssignedFunction != WaspFunction.Guard || !wasp.IsAvailable)
                    continue;

                int formationIndex = target.EnemyWaspCount + GetAssignedWaspCount(target, WaspFunction.Guard, faction);
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

        HexTile source = scout != null &&
            scout.StationedHex != null &&
            scout.StationedHex.State == HexTile.HexState.Enemy &&
            scout.StationedHex.EnemyOwnerFaction == hive.Faction
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

    private void EnsureFactionSkills()
    {
        EnsureFactionSkillSet(WaspScopeRole.PrimaryInvasive);
        EnsureFactionSkillSet(WaspScopeRole.SecondaryInvasive);
    }

    private void EnsureFactionSkillSet(WaspScopeRole faction)
    {
        faction = NormalizeEnemyFaction(faction);
        int length = Enum.GetValues(typeof(WaspFunction)).Length;
        if (!factionSkillLevels.TryGetValue(faction, out int[] levels) || levels == null || levels.Length != length)
            factionSkillLevels[faction] = new int[length];
        if (!skillProgressionTimers.ContainsKey(faction))
            skillProgressionTimers[faction] = 0f;
        if (!progressionFunctionIndexes.ContainsKey(faction))
            progressionFunctionIndexes[faction] = 0;
    }

    private IEnumerable<WaspScopeRole> GetRespondingFactions(HexTile target)
    {
        HashSet<WaspScopeRole> result = new HashSet<WaspScopeRole>();
        if (target == null)
            return result;

        if (target.State == HexTile.HexState.Enemy)
            result.Add(NormalizeEnemyFaction(target.EnemyOwnerFaction));

        if (target.EnemyHive != null)
            result.Add(NormalizeEnemyFaction(target.EnemyHive.Faction));

        foreach (EnemyWaspControl wasp in target.EnemyWasps)
        {
            if (wasp != null &&
                (wasp.AssignedFunction == WaspFunction.Scout || wasp.AssignedFunction == WaspFunction.Guard))
            {
                result.Add(NormalizeEnemyFaction(wasp.Faction));
            }
        }

        if (result.Count == 0)
            result.Add(WaspScopeRole.PrimaryInvasive);

        return result;
    }

    private int GetOpposingGuardCount(HexTile target, WaspScopeRole faction)
    {
        int count = target != null ? target.GetFriendlyWaspCount(WaspFunction.Guard) : 0;
        if (target != null)
        {
            foreach (EnemyWaspControl wasp in target.EnemyWasps)
            {
                if (wasp != null && wasp.Faction != faction && wasp.AssignedFunction == WaspFunction.Guard)
                    count++;
            }
        }
        return Mathf.Max(0, count);
    }

    private void RemoveFromAllFactions(EnemyWaspControl wasp)
    {
        foreach (List<EnemyWaspControl> faction in factions.Values)
            faction.Remove(wasp);
    }

    private static WaspScopeRole NormalizeEnemyFaction(WaspScopeRole faction)
    {
        return faction == WaspScopeRole.SecondaryInvasive
            ? WaspScopeRole.SecondaryInvasive
            : WaspScopeRole.PrimaryInvasive;
    }

    private GameObject GetEnemyWaspPrefab(int speciesIndex)
    {
        if (enemyWaspPrefabs == null || enemyWaspPrefabs.Length == 0)
            return null;

        List<GameObject> invasivePrefabs = new List<GameObject>();
        foreach (GameObject prefab in enemyWaspPrefabs)
        {
            WaspInfo info = prefab != null ? prefab.GetComponent<WaspInfo>() : null;
            SB_Wasps_Info species = info != null ? info.SpeciesInfo : null;
            if (species != null && species.ScopeRole != WaspScopeRole.NativePlayer)
                invasivePrefabs.Add(prefab);
        }

        if (invasivePrefabs.Count > 0)
            return invasivePrefabs[Mathf.Clamp(speciesIndex, 0, invasivePrefabs.Count - 1)];

        return enemyWaspPrefabs[Mathf.Clamp(speciesIndex, 0, enemyWaspPrefabs.Length - 1)];
    }
}
