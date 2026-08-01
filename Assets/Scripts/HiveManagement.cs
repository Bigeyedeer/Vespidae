using System;
using System.Collections.Generic;
using UnityEngine;

public class HiveManagement : MonoBehaviour
{
    public static HiveManagement Instance { get; private set; }
    public event Action WorkforceChanged;

    [Header("Role Skill Assets")]
    [SerializeField] private SB_Wasp_Skill scoutSkill;
    [SerializeField] private SB_Wasp_Skill foragerSkill;
    [SerializeField] private SB_Wasp_Skill builderSkill;
    [SerializeField] private SB_Wasp_Skill broodCaretakerSkill;
    [SerializeField] private SB_Wasp_Skill guardSkill;
    [SerializeField] private SB_Wasp_Skill containmentSkill;
    [SerializeField] private C_MainWorldHUD hud;
    [SerializeField] private SB_PlayerSelection_State playerSelection;

    [Header("Friendly Startup Spawning")]
    [SerializeField] private GameObject friendlyHivePrefab;
    [SerializeField] private GameObject friendlyWaspPrefab;
    [SerializeField] private bool spawnFriendlyStartup = true;
    [SerializeField] private bool spawnOneFriendlyWasp = true;

    [Header("Colony Values")]
    [SerializeField, Min(0)] private int workers;
    [SerializeField, Min(0)] private float colonyStrength;
    [SerializeField, Min(0)] private float broodProgress;
    [SerializeField, Min(0)] private float broodCapacity;
    [SerializeField, Min(0)] private float nestIntegrity;
    [SerializeField, Min(0)] private int skillPoints;

    [Header("Ecosystem Values")]
    [SerializeField, Range(0f, 1f)] private float habitatHealth;
    [SerializeField, Range(0f, 1f)] private float biodiversity;
    [SerializeField, Range(0f, 1f)] private float invasionPressure;

    private int[] skillLevels;
    private ResourceManager resourceManager;
    private readonly List<C_Friendly_Hive_Orc> spawnedFriendlyHives = new List<C_Friendly_Hive_Orc>();
    private readonly List<WaspControl> friendlyWasps = new List<WaspControl>();
    private bool friendlyStartupSpawned;

    public int Workers => workers;
    public float ColonyStrength => colonyStrength;
    public float BroodProgress => broodProgress;
    public float BroodCapacity => broodCapacity;
    public float NestIntegrity => nestIntegrity;
    public int SkillPoints => skillPoints;
    public float HabitatHealth => habitatHealth;
    public float Biodiversity => biodiversity;
    public float InvasionPressure => invasionPressure;
    public GameObject FriendlyHivePrefab => friendlyHivePrefab;
    public GameObject FriendlyWaspPrefab => friendlyWaspPrefab;
    public IReadOnlyList<C_Friendly_Hive_Orc> SpawnedFriendlyHives => spawnedFriendlyHives;
    public IReadOnlyList<WaspControl> FriendlyWasps => friendlyWasps;

    public static HiveManagement GetOrCreate()
    {
        if (Instance != null)
            return Instance;

        HiveManagement[] existing = Resources.FindObjectsOfTypeAll<HiveManagement>();
        foreach (HiveManagement manager in existing)
        {
            if (manager != null && manager.gameObject.scene.IsValid())
            {
                Instance = manager;
                return manager;
            }
        }

        GameObject host = GameObject.Find("Game Manager");
        if (host == null)
            return null;

        return host.AddComponent<HiveManagement>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        skillLevels = new int[Enum.GetValues(typeof(WaspFunction)).Length];
    }

    private void Start()
    {
        resourceManager = ResourceManager.Instance;
        hud = ResolveHud();
        RefreshHud();
        SpawnFriendlyStartup();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public int GetSkillLevel(WaspFunction function)
    {
        EnsureSkillLevels();
        return skillLevels[(int)function];
    }

    public SB_Wasp_Skill GetSkillDefinition(WaspFunction function)
    {
        switch (function)
        {
            case WaspFunction.Scout:
                return scoutSkill;
            case WaspFunction.Forager:
                return foragerSkill;
            case WaspFunction.Builder:
                return builderSkill;
            case WaspFunction.BroodCaretaker:
                return broodCaretakerSkill;
            case WaspFunction.Guard:
                return guardSkill;
            case WaspFunction.Containment:
                return containmentSkill;
            default:
                return null;
        }
    }

    public float GetEffectiveValue(WaspFunction function, WaspSkillStat stat)
    {
        SB_Wasp_Skill definition = GetSkillDefinition(function);
        return definition == null ? 1f : definition.GetEffectiveValue(stat, GetSkillLevel(function));
    }

    public bool CanUpgrade(WaspFunction function)
    {
        SB_Wasp_Skill definition = GetSkillDefinition(function);
        if (definition == null || GetSkillLevel(function) >= definition.MaximumLevel)
            return false;

        WaspSkillCost cost = definition.GetUpgradeCost(GetSkillLevel(function) + 1);
        return GetResourceManager() != null &&
               skillPoints >= cost.skillPoints &&
               resourceManager.CanAfford(cost.nectar, cost.prey, cost.fibre);
    }

    public bool TryUpgrade(WaspFunction function)
    {
        SB_Wasp_Skill definition = GetSkillDefinition(function);
        ResourceManager resources = GetResourceManager();

        if (definition == null || resources == null)
            return false;

        int nextLevel = GetSkillLevel(function) + 1;
        if (nextLevel > definition.MaximumLevel)
            return false;

        WaspSkillCost cost = definition.GetUpgradeCost(nextLevel);
        if (skillPoints < cost.skillPoints || !resources.TrySpend(cost.nectar, cost.prey, cost.fibre))
            return false;

        skillPoints -= cost.skillPoints;
        skillLevels[(int)function] = nextLevel;
        RefreshHud();
        return true;
    }

    public void AddSkillPoints(int amount)
    {
        skillPoints = Mathf.Max(0, skillPoints + amount);
        RefreshHud();
    }

    public void SetColonyValues(int workerCount, float strength, float brood, float capacity, float integrity)
    {
        workers = Mathf.Max(0, workerCount);
        colonyStrength = Mathf.Max(0f, strength);
        broodProgress = Mathf.Max(0f, brood);
        broodCapacity = Mathf.Max(0f, capacity);
        nestIntegrity = Mathf.Max(0f, integrity);
        RefreshHud();
    }

    public void SetEcosystemValues(float health, float diversity, float pressure)
    {
        habitatHealth = Mathf.Clamp01(health);
        biodiversity = Mathf.Clamp01(diversity);
        invasionPressure = Mathf.Clamp01(pressure);
        RefreshHud();
    }

    public void RecalculateFromResources()
    {
        ResourceManager resources = GetResourceManager();
        if (resources == null)
            return;

        colonyStrength = Mathf.Max(0f, resources.Nectar + resources.Prey + resources.Fibre);
        broodProgress = Mathf.Max(0f, resources.Prey);
        RefreshHud();
    }

    public void RefreshHud()
    {
        hud = ResolveHud();
        hud?.RefreshAll();
    }

    public void SpawnFriendlyStartup()
    {
        if (friendlyStartupSpawned)
            return;

        friendlyStartupSpawned = true;
        if (!spawnFriendlyStartup)
            return;

        if (friendlyHivePrefab == null)
        {
            Debug.LogWarning("HiveManagement cannot spawn friendly hives because no friendly hive prefab is assigned.");
            return;
        }

        HexTile[] hexTiles = FindObjectsByType<HexTile>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (HexTile hexTile in hexTiles)
        {
            if (hexTile == null || hexTile.State != HexTile.HexState.Owned)
                continue;

            Transform spawnPoint = hexTile.HiveSpawnPoint;
            GameObject hiveObject = Instantiate(friendlyHivePrefab, spawnPoint.position, spawnPoint.rotation);
            C_Friendly_Hive_Orc hive = hiveObject.GetComponent<C_Friendly_Hive_Orc>();
            if (hive == null)
            {
                Debug.LogWarning($"{friendlyHivePrefab.name} does not contain C_Friendly_Hive_Orc.");
                continue;
            }

            hive.Initialize(hexTile, friendlyWaspPrefab);
            spawnedFriendlyHives.Add(hive);
            hexTile.SetFriendlyHive(hive);
            if (spawnOneFriendlyWasp)
            {
                WaspControl starter = hive.SpawnWasp(friendlyWaspPrefab);
                if (starter != null)
                {
                    if (starter.InitializeFriendlyWasp(hive, WaspFunction.Scout, GetSelectedSpecies(starter)))
                    {
                        RegisterFriendlyWasp(starter);
                        hexTile.RegisterFriendlyWasp(starter);
                    }
                    else
                        Destroy(starter.gameObject);
                }
            }
        }
    }

    public WaspControl SpawnFriendlyWasp(C_Friendly_Hive_Orc hive, GameObject waspPrefab = null)
    {
        if (hive == null)
            return null;

        return hive.SpawnWasp(waspPrefab != null ? waspPrefab : friendlyWaspPrefab);
    }

    public bool CanTrainWasp(C_Friendly_Hive_Orc hive, WaspFunction function)
    {
        SB_Wasp_Skill definition = GetSkillDefinition(function);
        ResourceManager resources = GetResourceManager();
        if (hive == null || definition == null || resources == null)
            return false;

        WaspSkillCost cost = definition.TrainingCost;
        return resources.CanAfford(cost.nectar, cost.prey, cost.fibre);
    }

    public bool TryTrainWasp(C_Friendly_Hive_Orc hive, WaspFunction function)
    {
        SB_Wasp_Skill definition = GetSkillDefinition(function);
        ResourceManager resources = GetResourceManager();
        if (hive == null || definition == null || resources == null)
            return false;

        WaspSkillCost cost = definition.TrainingCost;
        if (!resources.TrySpend(cost.nectar, cost.prey, cost.fibre))
            return false;

        WaspControl wasp = hive.SpawnWasp(friendlyWaspPrefab);
        if (wasp == null)
        {
            Refund(cost);
            return false;
        }

        if (!wasp.InitializeFriendlyWasp(hive, function, GetSelectedSpecies(wasp)))
        {
            Destroy(wasp.gameObject);
            Refund(cost);
            return false;
        }

        RegisterFriendlyWasp(wasp);
        return true;
    }

    public bool TryDispatchScout(HexTile target)
    {
        return TryDispatchWasp(target, WaspFunction.Scout);
    }

    public bool TryDispatchWasp(HexTile target, WaspFunction function)
    {
        if (!CanDispatchToHex(target, function))
            return false;

        CleanupFriendlyWasps();
        foreach (WaspControl wasp in friendlyWasps)
        {
            if (wasp == null ||
                wasp.AssignedFunction != function ||
                !wasp.IsAvailable)
            {
                continue;
            }

            int formationIndex = target.FriendlyWaspCount + GetIncomingWaspCount(target);
            Vector3 destination = target.GetWaspFormationPosition(formationIndex, 0.25f, 0.25f);
            return wasp.DispatchToHex(target, destination);
        }

        return false;
    }

    public bool CanDispatchToHex(HexTile target, WaspFunction function)
    {
        if (target == null)
            return false;

        switch (function)
        {
            case WaspFunction.Scout:
                if (target.State == HexTile.HexState.Unknown)
                    return !HasScoutAssignedTo(target) && GetAvailableWaspCount(function) > 0;
                return target.State == HexTile.HexState.Owned && GetAvailableWaspCount(function) > 0;
            case WaspFunction.Forager:
                return target.State == HexTile.HexState.Owned &&
                       !target.ResourcesDepleted &&
                       GetAssignedWaspCount(target, function) < target.MaximumForagersPerHex &&
                       GetAvailableWaspCount(function) > 0;
            case WaspFunction.Builder:
                return target.State == HexTile.HexState.Owned && GetAvailableWaspCount(function) > 0;
            default:
                return false;
        }
    }

    public int GetTotalWaspCount(WaspFunction function)
    {
        CleanupFriendlyWasps();
        int count = 0;
        foreach (WaspControl wasp in friendlyWasps)
        {
            if (wasp != null && wasp.AssignedFunction == function)
                count++;
        }

        return count;
    }

    public int GetAvailableWaspCount(WaspFunction function)
    {
        CleanupFriendlyWasps();
        int count = 0;
        foreach (WaspControl wasp in friendlyWasps)
        {
            if (wasp != null &&
                wasp.AssignedFunction == function &&
                wasp.IsAvailable)
            {
                count++;
            }
        }

        return count;
    }

    public bool HasScoutAssignedTo(HexTile hex)
    {
        if (hex == null)
            return false;

        CleanupFriendlyWasps();
        foreach (WaspControl wasp in friendlyWasps)
        {
            if (wasp == null || wasp.AssignedFunction != WaspFunction.Scout)
                continue;

            if (wasp.TargetHex == hex || wasp.StationedHex == hex)
                return true;
        }

        return false;
    }

    public int GetAssignedWaspCount(HexTile hex, WaspFunction function)
    {
        if (hex == null)
            return 0;

        CleanupFriendlyWasps();
        int count = 0;
        foreach (WaspControl wasp in friendlyWasps)
        {
            if (wasp != null &&
                wasp.AssignedFunction == function &&
                (wasp.TargetHex == hex || wasp.StationedHex == hex))
            {
                count++;
            }
        }

        return count;
    }

    private int GetIncomingWaspCount(HexTile hex)
    {
        int count = 0;
        foreach (WaspControl wasp in friendlyWasps)
        {
            if (wasp != null && wasp.TargetHex == hex)
                count++;
        }

        return count;
    }

    public bool TryBuildHive(WaspControl builder)
    {
        if (!CanBuildHive(builder))
            return false;

        HexTile target = builder.StationedHex;
        SB_Wasp_Skill definition = GetSkillDefinition(WaspFunction.Builder);
        ResourceManager resources = GetResourceManager();
        WaspSkillCost cost = definition.HiveConstructionCost;
        if (!resources.TrySpend(cost.nectar, cost.prey, cost.fibre))
            return false;

        Transform spawnPoint = target.HiveSpawnPoint;
        GameObject hiveObject = Instantiate(friendlyHivePrefab, spawnPoint.position, spawnPoint.rotation);
        C_Friendly_Hive_Orc hive = hiveObject.GetComponent<C_Friendly_Hive_Orc>();
        if (hive == null)
        {
            Destroy(hiveObject);
            Refund(cost);
            return false;
        }

        hive.Initialize(target, friendlyWaspPrefab);
        spawnedFriendlyHives.Add(hive);
        target.SetFriendlyHive(hive);
        NotifyWorkforceChanged();
        return true;
    }

    public bool CanBuildHive(WaspControl builder)
    {
        if (builder == null ||
            builder.AssignedFunction != WaspFunction.Builder ||
            builder.WorkforceState != WaspWorkforceState.Stationed)
        {
            return false;
        }

        HexTile target = builder.StationedHex;
        SB_Wasp_Skill definition = GetSkillDefinition(WaspFunction.Builder);
        ResourceManager resources = GetResourceManager();
        if (target == null ||
            target.State != HexTile.HexState.Owned ||
            target.FriendlyHive != null ||
            definition == null ||
            resources == null ||
            friendlyHivePrefab == null)
        {
            return false;
        }

        WaspSkillCost cost = definition.HiveConstructionCost;
        return resources.CanAfford(cost.nectar, cost.prey, cost.fibre);
    }

    public void RegisterFriendlyWasp(WaspControl wasp)
    {
        if (wasp == null || friendlyWasps.Contains(wasp))
            return;

        friendlyWasps.Add(wasp);
        NotifyWorkforceChanged();
    }

    public void UnregisterFriendlyWasp(WaspControl wasp)
    {
        if (wasp == null || !friendlyWasps.Remove(wasp))
            return;

        NotifyWorkforceChanged();
    }

    public void NotifyWorkforceChanged()
    {
        CleanupFriendlyWasps();
        workers = friendlyWasps.Count;
        RefreshHud();
        WorkforceChanged?.Invoke();
    }

    private ResourceManager GetResourceManager()
    {
        if (resourceManager == null)
            resourceManager = ResourceManager.Instance;

        return resourceManager;
    }

    private SB_Wasps_Info GetSelectedSpecies(WaspControl fallback)
    {
        if (playerSelection != null && playerSelection.SelectedWasp != null)
            return playerSelection.SelectedWasp;

        return fallback != null ? fallback.SpeciesInfo : null;
    }

    private void Refund(WaspSkillCost cost)
    {
        ResourceManager resources = GetResourceManager();
        if (resources == null)
            return;

        resources.AddNectar(cost.nectar);
        resources.AddPrey(cost.prey);
        resources.AddFibre(cost.fibre);
    }

    private void CleanupFriendlyWasps()
    {
        friendlyWasps.RemoveAll(wasp => wasp == null);
    }

    private C_MainWorldHUD ResolveHud()
    {
        if (hud != null)
            return hud;

        return C_MainWorldHUD.GetOrCreate();
    }

    private void EnsureSkillLevels()
    {
        if (skillLevels == null || skillLevels.Length != Enum.GetValues(typeof(WaspFunction)).Length)
            skillLevels = new int[Enum.GetValues(typeof(WaspFunction)).Length];
    }
}
