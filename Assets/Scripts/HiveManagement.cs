using System;
using UnityEngine;

public class HiveManagement : MonoBehaviour
{
    public static HiveManagement Instance { get; private set; }

    [Header("Role Skill Assets")]
    [SerializeField] private SB_Wasp_Skill scoutSkill;
    [SerializeField] private SB_Wasp_Skill foragerSkill;
    [SerializeField] private SB_Wasp_Skill builderSkill;
    [SerializeField] private SB_Wasp_Skill broodCaretakerSkill;
    [SerializeField] private SB_Wasp_Skill guardSkill;
    [SerializeField] private SB_Wasp_Skill containmentSkill;
    [SerializeField] private C_MainWorldHUD hud;

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

    public int Workers => workers;
    public float ColonyStrength => colonyStrength;
    public float BroodProgress => broodProgress;
    public float BroodCapacity => broodCapacity;
    public float NestIntegrity => nestIntegrity;
    public int SkillPoints => skillPoints;
    public float HabitatHealth => habitatHealth;
    public float Biodiversity => biodiversity;
    public float InvasionPressure => invasionPressure;

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

    private ResourceManager GetResourceManager()
    {
        if (resourceManager == null)
            resourceManager = ResourceManager.Instance;

        return resourceManager;
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
