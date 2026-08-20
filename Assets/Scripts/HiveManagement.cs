using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public readonly struct WaspMoveOrderResult
{
    public WaspMoveOrderResult(int requested, int moved, int rejected, int capped)
    {
        Requested = requested;
        Moved = moved;
        Rejected = rejected;
        Capped = capped;
    }

    public int Requested { get; }
    public int Moved { get; }
    public int Rejected { get; }
    public int Capped { get; }
    public bool AnyMoved => Moved > 0;
}

public class HiveManagement : MonoBehaviour
{
    public static HiveManagement Instance { get; private set; }
    public event Action WorkforceChanged;
    public event Action SkillsChanged;

    [Header("Role Skill Assets")]
    [SerializeField] private SB_Wasp_Skill scoutSkill;
    [SerializeField] private SB_Wasp_Skill foragerSkill;
    [SerializeField] private SB_Wasp_Skill builderSkill;
    [SerializeField] private SB_Wasp_Skill broodCaretakerSkill;
    [SerializeField] private SB_Wasp_Skill guardSkill;
    [SerializeField] private SB_Wasp_Skill containmentSkill;
    [SerializeField] private C_MainWorldHUD hud;
    [SerializeField] private SB_PlayerSelection_State playerSelection;
    
    [Header("Action Costs")]
    [SerializeField] private float scoutDispatchCost = 1f;
    [SerializeField] private float foragerDispatchCost = 1f;
    [SerializeField] private float builderDispatchCost = 2f;
    [SerializeField, Min(0.1f)] private float baseHiveConstructionTime = 5f;

    [Header("Colony Upkeep")]
    [SerializeField] private SB_Colony_Upkeep_Rules upkeepRules;
    [SerializeField] private float upkeepInterval = 10f;
    [SerializeField] private float nectarUpkeepPerWorker = 0.25f;

    private float upkeepTimer;
    private float starvedSeconds;
    private float starvationDeathTimer;
    private bool upkeepUnpaid;
    
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

    [Header("Ecosystem Meter Tuning")]
    [SerializeField, Min(0.1f), Tooltip("Seconds between ecosystem meter recalculations.")]
    private float ecosystemInterval = 2f;
    [SerializeField, Range(0f, 1f), Tooltip("How much invasive-held territory drives invasion pressure.")]
    private float invasionTerritoryWeight = 0.7f;
    [SerializeField, Range(0f, 1f), Tooltip("How much banked invasive strength drives invasion pressure.")]
    private float invasionStrengthWeight = 0.3f;
    [SerializeField, Range(0f, 1f), Tooltip("How far invasion pressure drags habitat health down.")]
    private float invasionHabitatPenalty = 0.5f;
    [SerializeField, Range(0f, 1f), Tooltip("How much land still in native hands drives biodiversity.")]
    private float biodiversityTerritoryWeight = 0.75f;
    [SerializeField, Range(0f, 1f), Tooltip("How much forage left on worked land drives biodiversity.")]
    private float biodiversityForageWeight = 0.25f;

    private float ecosystemTimer;
    private float biodiversityDamage;

    private int[] skillLevels;
    private ResourceManager resourceManager;
    private readonly List<C_Friendly_Hive_Orc> spawnedFriendlyHives = new List<C_Friendly_Hive_Orc>();
    private readonly List<WaspControl> friendlyWasps = new List<WaspControl>();
    private bool friendlyStartupSpawned;
    private readonly HashSet<HexTile> hiveConstructionInProgress = new HashSet<HexTile>();

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

    /// <summary>
    /// Pulls every attacker back to its home hive to defend it. Wasps already sitting at home are left
    /// alone - the combat controller counts them as that hex's garrison already.
    /// </summary>
    /// <returns>How many wasps were actually recalled.</returns>
    public int RecallAttackersToDefend()
    {
        int recalled = 0;
        foreach (WaspControl wasp in friendlyWasps)
        {
            if (wasp == null || !wasp.IsAlive)
                continue;

            if (wasp.AssignedFunction != WaspFunction.Guard)
                continue;

            if (wasp.RecallForDefence())
                recalled++;
        }

        if (recalled > 0)
            NotifyWorkforceChanged();

        return recalled;
    }

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

    private void Update()
    {
        upkeepTimer += Time.deltaTime;

        if (upkeepTimer >= UpkeepInterval)
        {
            upkeepTimer = 0f;
            ApplyUpkeep();
        }

        UpdateStarvation();
        UpdateTraining();

        ecosystemTimer += Time.deltaTime;
        if (ecosystemTimer >= ecosystemInterval)
        {
            ecosystemTimer = 0f;
            RecalculateEcosystem();
        }
    }

    private float UpkeepInterval => upkeepRules != null ? upkeepRules.UpkeepTickSeconds : upkeepInterval;

    /// <summary>True while the colony could not pay its last upkeep bill.</summary>
    public bool IsStarving => upkeepUnpaid;

    /// <summary>
    /// 0 when fed, rising to 1 as starvation bites. Stats are blended toward their level-1 values
    /// by this amount.
    /// </summary>
    public float StarvationSeverity =>
        upkeepRules != null ? upkeepRules.GetSeverity(starvedSeconds) : 0f;

    /// <summary>Seconds the colony has gone without paying upkeep.</summary>
    public float StarvedSeconds => starvedSeconds;

    /// <summary>Training is the first thing to stop when the colony cannot feed itself.</summary>
    public bool CanAffordUpkeep => !upkeepUnpaid;

    /// <summary>
    /// Charges upkeep for every living wasp, using each role's own rate. If the colony cannot
    /// cover the bill nothing is spent and the starvation clock starts.
    /// </summary>
    private void ApplyUpkeep()
    {
        ResourceManager resources = GetResourceManager();
        if (resources == null)
            return;

        GetUpkeepCost(out float nectarCost, out float preyCost);
        if (nectarCost <= 0f && preyCost <= 0f)
        {
            upkeepUnpaid = false;
            return;
        }

        if (resources.TrySpend(nectarCost, preyCost, 0f))
        {
            upkeepUnpaid = false;
            return;
        }

        // Cannot pay: leave the resources alone and let the ladder take over.
        upkeepUnpaid = true;
    }

    /// <summary>
    /// Total upkeep per tick, summed per role so different roles can cost different amounts.
    /// Falls back to the flat per-worker rate when a role has no skill asset.
    /// </summary>
    public void GetUpkeepCost(out float nectar, out float prey)
    {
        nectar = 0f;
        prey = 0f;
        CleanupFriendlyWasps();

        foreach (WaspControl wasp in friendlyWasps)
        {
            if (wasp == null || !wasp.IsAlive)
                continue;

            SB_Wasp_Skill definition = GetSkillDefinition(wasp.AssignedFunction);
            if (definition == null)
            {
                nectar += nectarUpkeepPerWorker;
                continue;
            }

            nectar += definition.UpkeepNectarPerTick;
            prey += definition.UpkeepPreyPerTick;
        }
    }

    /// <summary>
    /// Advances or unwinds the starvation clock and kills wasps once the shortage has gone on too
    /// long. Deaths are spaced out so the colony withers rather than vanishing at once.
    /// </summary>
    private void UpdateStarvation()
    {
        if (upkeepRules == null)
            return;

        if (upkeepUnpaid)
        {
            starvedSeconds += Time.deltaTime;
        }
        else if (starvedSeconds > 0f)
        {
            starvedSeconds = Mathf.Max(0f, starvedSeconds - Time.deltaTime * upkeepRules.RecoveryRate);
            starvationDeathTimer = 0f;
        }

        if (!upkeepUnpaid || starvedSeconds < upkeepRules.StarvationDeathSeconds)
            return;

        starvationDeathTimer += Time.deltaTime;
        if (starvationDeathTimer < upkeepRules.StarvationDeathIntervalSeconds)
            return;

        starvationDeathTimer = 0f;
        StarveOneWasp();
    }

    private void StarveOneWasp()
    {
        CleanupFriendlyWasps();

        // Take a non-combat role first so a starving colony does not lose its defence instantly.
        WaspControl victim = null;
        foreach (WaspControl wasp in friendlyWasps)
        {
            if (wasp == null || !wasp.IsAlive || wasp.IsCombatLocked)
                continue;

            if (wasp.AssignedFunction != WaspFunction.Guard)
            {
                victim = wasp;
                break;
            }

            if (victim == null)
                victim = wasp;
        }

        if (victim == null)
            return;

        Debug.Log($"A {victim.AssignedFunction} starved - the colony cannot feed its wasps.");
        victim.DestroyFromCombat();
        NotifyWorkforceChanged();
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
    
    private float GetDispatchCost(WaspFunction function)
    {
        switch (function)
        {
            case WaspFunction.Scout:
                return scoutDispatchCost;

            case WaspFunction.Forager:
                return foragerDispatchCost;

            case WaspFunction.Builder:
                return builderDispatchCost;

            default:
                return 0f;
        }
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
        if (definition == null)
            return 1f;

        float value = definition.GetEffectiveValue(stat, GetSkillLevel(function));

        // A starving colony loses the benefit of its upgrades on the stats that show in the field.
        // The value decays toward what level 1 would give - never to zero, so wasps still function.
        float severity = StarvationSeverity;
        if (severity <= 0f || !IsStarvationAffected(stat))
            return value;

        float baseline = definition.GetEffectiveValue(stat, 1);

        // Starvation may only take upgrades away. A colony sitting at or below the baseline has
        // nothing left to lose, and must never be *improved* by going hungry.
        if (baseline >= value)
            return value;

        return Mathf.Lerp(value, baseline, severity);
    }

    private static bool IsStarvationAffected(WaspSkillStat stat)
    {
        return stat == WaspSkillStat.MovementSpeed || stat == WaspSkillStat.AttackSpeed;
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
        foreach (WaspControl wasp in friendlyWasps)
            wasp?.Combatant?.RefreshStats(false);
        SkillsChanged?.Invoke();
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

    /// <summary>
    /// Permanent damage to biodiversity, for when the player removes a native colony. This is the
    /// hook the identification work will call on a misidentified intervention.
    /// </summary>
    public void ApplyBiodiversityDamage(float amount)
    {
        biodiversityDamage = Mathf.Clamp01(biodiversityDamage + Mathf.Max(0f, amount));
        RecalculateEcosystem();
    }

    /// <summary>
    /// Derives the three ecosystem meters from the actual state of the map, so they respond to how
    /// the match is going instead of sitting on authored values.
    ///
    /// Invasion pressure  - how much of the map the invasives hold, and how strong they are.
    /// Habitat health     - how much forage is left on the land the player holds, hurt by invasion.
    /// Biodiversity       - native ground still standing, minus permanent damage the player caused.
    /// </summary>
    public void RecalculateEcosystem()
    {
        HexTile[] tiles = FindObjectsByType<HexTile>(FindObjectsSortMode.None);

        // Every hex counts, including Locked ones. Locked is a player progression gate, not empty
        // space - invasives can still scout and spread onto it. If locked land were excluded the
        // meters would jump every time the player unlocked a region, which reads as nonsense.
        int totalLand = 0, playerHeld = 0, invasiveHeld = 0;
        float forageRatioTotal = 0f;
        int foragedTiles = 0;

        foreach (HexTile tile in tiles)
        {
            if (tile == null)
                continue;

            totalLand++;
            if (tile.State == HexTile.HexState.Owned)
            {
                playerHeld++;

                // How much of this hex's forage is left, averaged over the resources it carries.
                // The starting amounts live on the area ScriptableObject, not the tile.
                SB_Hex_Area_Info area = tile.AreaInfo;
                if (area == null)
                    continue;

                float ratio = 0f;
                int kinds = 0;
                if (tile.HasNectar) { ratio += Mathf.Clamp01(tile.NectarRemaining / Mathf.Max(1f, area.StartingNectar)); kinds++; }
                if (tile.HasPrey)   { ratio += Mathf.Clamp01(tile.PreyRemaining  / Mathf.Max(1f, area.StartingPrey));   kinds++; }
                if (tile.HasFibre)  { ratio += Mathf.Clamp01(tile.FibreRemaining / Mathf.Max(1f, area.StartingFibre));  kinds++; }
                if (kinds > 0) { forageRatioTotal += ratio / kinds; foragedTiles++; }
            }
            else if (tile.State == HexTile.HexState.Enemy)
            {
                invasiveHeld++;
            }
        }

        if (totalLand == 0)
            return;

        float invasiveShare = invasiveHeld / (float)totalLand;

        // Invasive strength counts too, so a faction massing for an attack registers as pressure
        // even before it has taken ground.
        float strengthShare = 0f;
        EnemyHiveControl enemies = EnemyHiveControl.Instance;
        if (enemies != null)
        {
            float strength = 0f, capacity = 0f;
            foreach (WaspScopeRole f in new[] { WaspScopeRole.PrimaryInvasive, WaspScopeRole.SecondaryInvasive })
            {
                SB_Enemy_Faction_Rules rules = enemies.GetFactionRules(f);
                if (rules == null) continue;
                strength += enemies.GetFactionStrength(f);
                capacity += rules.MaximumStrength;
            }
            if (capacity > 0f) strengthShare = Mathf.Clamp01(strength / capacity);
        }

        invasionPressure = Mathf.Clamp01(invasiveShare * invasionTerritoryWeight
                                       + strengthShare * invasionStrengthWeight);

        // Land the player holds, scored by how much forage is left on it, then pushed down by
        // whatever the invasives are exerting.
        float forageHealth = foragedTiles > 0 ? forageRatioTotal / foragedTiles : 1f;
        habitatHealth = Mathf.Clamp01(forageHealth * (1f - invasionPressure * invasionHabitatPenalty));

        // Native ground still standing, plus the condition of the land being worked, less any
        // permanent damage the player has done. The Cape starts largely intact, so this starts high
        // and falls as the invasives spread - it is the meter the player is really defending.
        float nativeShare = 1f - invasiveShare;
        biodiversity = Mathf.Clamp01(nativeShare * biodiversityTerritoryWeight
                                   + forageHealth * biodiversityForageWeight
                                   - biodiversityDamage);

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

        // First rung of the starvation ladder: no new mouths while the colony cannot feed the
        // ones it already has.
        if (upkeepUnpaid)
            return false;

        WaspSkillCost cost = definition.TrainingCost;
        if (!resources.TrySpend(cost.nectar, cost.prey, cost.fibre))
            return false;

        // Resources are taken up front, but the wasp itself arrives after the training time. Higher
        // skill levels take longer, so specialising a role trades against how fast you can field it.
        float seconds = definition.GetTrainingSeconds(GetSkillLevel(function));
        trainingQueue.Add(new TrainingOrder(hive, function, seconds, cost));
        NotifyWorkforceChanged();
        return true;
    }

    /// <summary>A wasp paid for and being raised. Completes once its timer runs out.</summary>
    private class TrainingOrder
    {
        public readonly C_Friendly_Hive_Orc Hive;
        public readonly WaspFunction Function;
        public readonly float TotalSeconds;
        public readonly WaspSkillCost Cost;
        public float Remaining;

        public TrainingOrder(C_Friendly_Hive_Orc hive, WaspFunction function, float seconds, WaspSkillCost cost)
        {
            Hive = hive;
            Function = function;
            TotalSeconds = Mathf.Max(0.01f, seconds);
            Cost = cost;
            Remaining = TotalSeconds;
        }
    }

    private readonly List<TrainingOrder> trainingQueue = new List<TrainingOrder>();

    /// <summary>How many wasps of a role are currently being raised.</summary>
    public int GetTrainingCount(WaspFunction function)
    {
        int count = 0;
        foreach (TrainingOrder order in trainingQueue)
            if (order.Function == function)
                count++;
        return count;
    }

    /// <summary>Progress of the nearest-finished wasp of a role, 0 to 1. Returns 0 if none training.</summary>
    public float GetTrainingProgress(WaspFunction function)
    {
        float best = 0f;
        foreach (TrainingOrder order in trainingQueue)
        {
            if (order.Function != function)
                continue;

            float progress = 1f - Mathf.Clamp01(order.Remaining / order.TotalSeconds);
            if (progress > best)
                best = progress;
        }
        return best;
    }

    /// <summary>Seconds left on the nearest-finished wasp of a role, or 0 if none training.</summary>
    public float GetTrainingSecondsRemaining(WaspFunction function)
    {
        float best = 0f;
        foreach (TrainingOrder order in trainingQueue)
        {
            if (order.Function != function)
                continue;

            if (best <= 0f || order.Remaining < best)
                best = order.Remaining;
        }
        return best;
    }

    private void UpdateTraining()
    {
        if (trainingQueue.Count == 0)
            return;

        bool anyCompleted = false;
        for (int index = trainingQueue.Count - 1; index >= 0; index--)
        {
            TrainingOrder order = trainingQueue[index];
            order.Remaining -= Time.deltaTime;
            if (order.Remaining > 0f)
                continue;

            trainingQueue.RemoveAt(index);
            CompleteTraining(order);
            anyCompleted = true;
        }

        if (anyCompleted)
            NotifyWorkforceChanged();
    }

    private void CompleteTraining(TrainingOrder order)
    {
        // Resources were taken when the order was placed, so anything that stops the wasp arriving
        // has to give them back - otherwise losing a hive mid-training quietly costs the player
        // everything they paid with nothing to show for it.
        if (order.Hive == null)
        {
            Refund(order.Cost);
            return;
        }

        WaspControl wasp = order.Hive.SpawnWasp(friendlyWaspPrefab);
        if (wasp == null)
        {
            Refund(order.Cost);
            return;
        }

        if (!wasp.InitializeFriendlyWasp(order.Hive, order.Function, GetSelectedSpecies(wasp)))
        {
            Destroy(wasp.gameObject);
            Refund(order.Cost);
            return;
        }

        RegisterFriendlyWasp(wasp);
        AudioDirector.Play(GameSound.TrainingComplete);
    }

    public bool TryDispatchScout(HexTile target)
    {
        return TryDispatchWasp(target, WaspFunction.Scout);
    }

    public bool TryDispatchWasp(HexTile target, WaspFunction function)
    {
        if (!CanDispatchToHex(target, function))
            return false;

        ResourceManager resources = GetResourceManager();

        float cost = GetDispatchCost(function);

        if (resources == null || !resources.CanAfford(cost, 0f, 0f))
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
            if (!wasp.DispatchToHex(target, destination))
                return false;

            resources.TrySpend(cost, 0f, 0f);
            AudioDirector.Play(GameSound.WaspDispatched);

            return true;
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
                if (HexProgressionManager.Instance != null && !HexProgressionManager.Instance.CanPlayerTarget(target))
                    return false;
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
            case WaspFunction.Guard:
                if (HexProgressionManager.Instance != null && !HexProgressionManager.Instance.CanPlayerTarget(target))
                    return false;
                int maximum = target.CombatController != null
                    ? target.CombatController.MaximumAttackersPerSide
                    : 20;
                return target.State != HexTile.HexState.Locked &&
                       GetAssignedWaspCount(target, function) < maximum &&
                       GetAvailableWaspCount(function) > 0;
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

        hiveConstructionInProgress.Add(target);
        StartCoroutine(BuildHiveAfterDelay(target, cost));
        return true;
    }

    private IEnumerator BuildHiveAfterDelay(HexTile target, WaspSkillCost cost)
    {
        float buildSpeed = Mathf.Max(0.1f, GetEffectiveValue(WaspFunction.Builder, WaspSkillStat.BuildSpeed));
        yield return new WaitForSeconds(baseHiveConstructionTime / buildSpeed);

        if (target == null || target.State != HexTile.HexState.Owned || target.FriendlyHive != null)
        {
            hiveConstructionInProgress.Remove(target);
            Refund(cost);
            yield break;
        }

        Transform spawnPoint = target.HiveSpawnPoint;
        GameObject hiveObject = Instantiate(friendlyHivePrefab, spawnPoint.position, spawnPoint.rotation);
        C_Friendly_Hive_Orc hive = hiveObject.GetComponent<C_Friendly_Hive_Orc>();
        if (hive == null)
        {
            Destroy(hiveObject);
            hiveConstructionInProgress.Remove(target);
            Refund(cost);
            yield break;
        }

        hive.Initialize(target, friendlyWaspPrefab);
        spawnedFriendlyHives.Add(hive);
        target.SetFriendlyHive(hive);
        hiveConstructionInProgress.Remove(target);
        NotifyWorkforceChanged();
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
            hiveConstructionInProgress.Contains(target) ||
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

    public bool TryRecallScout(HexTile target)
    {
        if (target == null)
            return false;

        CleanupFriendlyWasps();
        foreach (WaspControl wasp in friendlyWasps)
        {
            if (wasp != null &&
                wasp.AssignedFunction == WaspFunction.Scout &&
                wasp.StationedHex == target)
            {
                return wasp.ReturnToHomeHive();
            }
        }
        return false;
    }

    public WaspMoveOrderResult TryMoveAttackers(IReadOnlyList<WaspControl> selectedWasps, HexTile target)
    {
        return TryMoveWasps(selectedWasps, target);
    }

    /// <summary>
    /// Moves any mix of selected wasps onto a hex, applying each role's own rules
    /// (attacker cap, forager cap, ownership). A scout arriving on an unscouted hex starts
    /// scouting automatically once it registers on arrival.
    /// </summary>
    public WaspMoveOrderResult TryMoveWasps(IReadOnlyList<WaspControl> selectedWasps, HexTile target)
    {
        int requested = selectedWasps != null ? selectedWasps.Count : 0;
        if (requested == 0 || target == null ||
            target.State == HexTile.HexState.Locked ||
            (HexProgressionManager.Instance != null && !HexProgressionManager.Instance.CanPlayerTarget(target)))
        {
            return new WaspMoveOrderResult(requested, 0, requested, 0);
        }

        CleanupFriendlyWasps();

        int attackerMaximum = target.CombatController != null ? target.CombatController.MaximumAttackersPerSide : 20;
        int attackerSlots = Mathf.Max(0, attackerMaximum - GetAssignedWaspCount(target, WaspFunction.Guard));
        int foragerSlots = Mathf.Max(0, target.MaximumForagersPerHex - GetAssignedWaspCount(target, WaspFunction.Forager));
        bool scoutSlotTaken = HasScoutAssignedTo(target);

        int moved = 0;
        int rejected = 0;
        int capped = 0;
        HashSet<WaspControl> processed = new HashSet<WaspControl>();

        foreach (WaspControl wasp in selectedWasps)
        {
            if (wasp == null || !processed.Add(wasp) || !wasp.IsAlive || wasp.IsCombatLocked)
            {
                rejected++;
                continue;
            }

            if (wasp.TargetHex == target || wasp.StationedHex == target)
            {
                rejected++;
                continue;
            }

            WaspFunction function = wasp.AssignedFunction;
            if (!CanRoleEnterHex(function, target))
            {
                rejected++;
                continue;
            }

            // Role capacity. Anything without its own cap simply moves.
            if (function == WaspFunction.Guard && attackerSlots <= 0)
            {
                capped++;
                continue;
            }

            if (function == WaspFunction.Forager && foragerSlots <= 0)
            {
                capped++;
                continue;
            }

            // Only one scout is needed to reveal an unknown hex.
            if (function == WaspFunction.Scout && target.State == HexTile.HexState.Unknown && scoutSlotTaken)
            {
                capped++;
                continue;
            }

            int formationIndex = target.FriendlyWaspCount + GetIncomingWaspCount(target);
            Vector3 destination = target.GetWaspFormationPosition(formationIndex, 0.25f, 0.25f);
            if (!wasp.TryIssueMoveOrder(target, destination))
            {
                rejected++;
                continue;
            }

            moved++;
            if (function == WaspFunction.Guard)
                attackerSlots--;
            else if (function == WaspFunction.Forager)
                foragerSlots--;
            else if (function == WaspFunction.Scout && target.State == HexTile.HexState.Unknown)
                scoutSlotTaken = true;
        }

        NotifyWorkforceChanged();
        return new WaspMoveOrderResult(requested, moved, rejected, capped);
    }

    /// <summary>
    /// Whether a role is allowed onto a hex at all. Scouts may enter unknown territory (that is
    /// how it gets revealed); the economy roles need the hex owned first.
    /// </summary>
    private static bool CanRoleEnterHex(WaspFunction function, HexTile target)
    {
        switch (function)
        {
            case WaspFunction.Scout:
                return target.State == HexTile.HexState.Unknown || target.State == HexTile.HexState.Owned;
            case WaspFunction.Guard:
            case WaspFunction.Containment:
                return target.State != HexTile.HexState.Locked;
            case WaspFunction.Forager:
                return target.State == HexTile.HexState.Owned && !target.ResourcesDepleted;
            default:
                return target.State == HexTile.HexState.Owned;
        }
    }

    public void HandleHiveDestroyed(C_Friendly_Hive_Orc hive)
    {
        if (hive == null)
            return;

        spawnedFriendlyHives.Remove(hive);
        if (hive.OwnerHex != null && hive.OwnerHex.FriendlyHive == hive)
            hive.OwnerHex.SetFriendlyHive(null);
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
