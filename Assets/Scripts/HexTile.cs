using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class HexTile : MonoBehaviour
{
    public enum HexState
    {
        Owned,
        Unknown,
        Scouted,
        Enemy,
        Locked
    }

    [Header("Hex Data")]
    [SerializeField] private SB_Hex_Area_Info areaInfo;
    [SerializeField] private SB_Hex_Gathering_Rules gatheringRules;

    [Header("Territory State")]
    [SerializeField] private HexState state = HexState.Unknown;
    [SerializeField] private WaspScopeRole enemyOwnerFaction = WaspScopeRole.PrimaryInvasive;
    [SerializeField] private GameObject antGroup;

    private float currentPrey;
    private float currentNectar;
    private float currentFibre;
    private bool resourcesInitialized;
    private float gatheringTickElapsed;
    private readonly HashSet<WaspControl> friendlyWasps = new HashSet<WaspControl>();
    private readonly HashSet<EnemyWaspControl> enemyWasps = new HashSet<EnemyWaspControl>();
    private Collider formationCollider;
    private C_Friendly_Hive_Orc friendlyHive;
    private C_Enemy_Hive_Orc enemyHive;
    private HexState stateBeforeLock = HexState.Unknown;
    private bool playerAccessible = true;

    [Header("Combat")]
    [SerializeField] private HexCombatController combatController;

    [Header("Hex Materials")]
    [SerializeField] private Renderer hexRenderer;
    [SerializeField] private Material ownedMaterial;
    [SerializeField] private Material unknownMaterial;
    [SerializeField] private Material lockedMaterial;
    [SerializeField] private Material enemyMaterial;
    [FormerlySerializedAs("proteinMaterial")]
    [SerializeField] private Material preyMaterial;

    [Header("Camera")]
    [SerializeField] private Transform focusPoint;
    [SerializeField] private Transform waspCloseUpFocusPoint;

    [Header("Hive Spawning")]
    [SerializeField] private Transform hiveSpawnPoint;
    [SerializeField, Range(1, 60), Tooltip("How many wasps the formation spreads to fill the hex with. Higher values pack them tighter.")]
    private int waspFormationCapacity = 20;
    [SerializeField, Min(0f), Tooltip("Keep-out distance from the hexagon edge so wasp models never overhang the tile.")]
    private float waspFormationEdgeMargin = 0.18f;
    [SerializeField, Min(0f), Tooltip("Random offset applied to each formation slot so wasps look scattered rather than perfectly gridded.")]
    private float waspFormationJitter = 0.07f;

    [Header("Scouting")]
    [SerializeField, Min(0.1f)] private float scoutingDuration = 10f;

    private bool scoutingInProgress;
    private float scoutingTimeRemaining;

    private bool claimInProgress;
    private float claimTimeRemaining;
    private float claimDuration;
    private WaspScopeRole claimingFaction = WaspScopeRole.PrimaryInvasive;

    // 137.507764 degrees - the golden angle, which is what makes sunflower placement spread evenly.
    private const float GoldenAngleRadians = 2.39996323f;

    public SB_Hex_Area_Info AreaInfo => areaInfo;
    public SB_Hex_Gathering_Rules GatheringRules => gatheringRules;
    public string HexName => areaInfo != null ? areaInfo.AreaName : gameObject.name;
    public string AreaId => areaInfo != null ? areaInfo.AreaId : gameObject.name;
    public HexState State => state;
    public bool IsPlayerAccessible => playerAccessible;
    public HexResourceType Content => areaInfo != null ? areaInfo.ResourceType : HexResourceType.None;
    public string AreaDescription => areaInfo != null ? areaInfo.AreaDescription : string.Empty;
    public string HabitatCue => areaInfo != null ? areaInfo.HabitatCue : string.Empty;
    public HexTerritoryState TerritoryState => areaInfo != null ? areaInfo.TerritoryState : HexTerritoryState.Neutral;
    public HexRiskState RiskState => areaInfo != null ? areaInfo.RiskState : HexRiskState.SafeNativeHabitat;
    public HexVisibilityState VisibilityState => areaInfo != null ? areaInfo.VisibilityState : HexVisibilityState.Hidden;
    public int ConnectedSiteCount => areaInfo != null ? areaInfo.ConnectedSiteCount : 0;
    public IReadOnlyList<SB_Wasps_Info> WaspsPresent => areaInfo != null ? areaInfo.WaspsPresent : Array.Empty<SB_Wasps_Info>();
    public bool HasPrey => areaInfo != null && areaInfo.HasPrey;
    public bool HasNectar => areaInfo != null && areaInfo.HasNectar;
    public bool HasFibre => areaInfo != null && areaInfo.HasFibre;
    public float PreyRemaining => currentPrey;
    public float NectarRemaining => currentNectar;
    public float FibreRemaining => currentFibre;
    public float GatheringTickIntervalSeconds => gatheringRules != null ? gatheringRules.GatheringTickIntervalSeconds : 0f;
    public int MaximumForagersPerHex => gatheringRules != null ? gatheringRules.MaximumForagersPerHex : 0;
    public bool ResourcesDepleted =>
        (!HasPrey || PreyRemaining <= 0f) &&
        (!HasNectar || NectarRemaining <= 0f) &&
        (!HasFibre || FibreRemaining <= 0f);

    public Vector3 FocusPosition => focusPoint != null ? focusPoint.position : transform.position;
    public Vector3 FocusLookPosition => transform.position + Vector3.up * 0.35f;
    public Vector3 WaspCloseUpPosition => waspCloseUpFocusPoint != null ? waspCloseUpFocusPoint.position : FocusPosition;
    public Vector3 WaspCloseUpLookPosition => transform.position + Vector3.up * 0.35f;
    public Transform HiveSpawnPoint => hiveSpawnPoint != null ? hiveSpawnPoint : transform.Find("HiveSpawnpoint") ?? transform;
    public C_Friendly_Hive_Orc FriendlyHive => friendlyHive;
    public C_Enemy_Hive_Orc EnemyHive => enemyHive;
    public WaspScopeRole EnemyOwnerFaction => enemyHive != null ? enemyHive.Faction : enemyOwnerFaction;
    public HexCombatController CombatController => combatController;
    public bool HasFriendlyScout => GetFriendlyWaspCount(WaspFunction.Scout) > 0;
    public bool HasEnemyScout => GetEnemyWaspCount(WaspFunction.Scout) > 0;
    public IReadOnlyCollection<WaspControl> FriendlyWasps => friendlyWasps;
    public IReadOnlyCollection<EnemyWaspControl> EnemyWasps => enemyWasps;
    public int FriendlyWaspCount
    {
        get
        {
            friendlyWasps.RemoveWhere(wasp => wasp == null);
            return friendlyWasps.Count;
        }
    }
    public int EnemyWaspCount
    {
        get
        {
            enemyWasps.RemoveWhere(wasp => wasp == null);
            return enemyWasps.Count;
        }
    }
    public bool IsScouting => scoutingInProgress;
    public float ScoutingDuration => scoutingDuration;
    public float ScoutingTimeRemaining => scoutingTimeRemaining;
    public bool IsBeingClaimed => claimInProgress;
    public WaspScopeRole ClaimingFaction => claimingFaction;
    public float ClaimTimeRemaining => claimTimeRemaining;
    public float ClaimDuration => claimDuration;
    /// <summary>0 at the start of a claim, 1 the moment the hex flips.</summary>
    public float ClaimProgress => claimDuration <= 0f ? 0f : 1f - Mathf.Clamp01(claimTimeRemaining / claimDuration);
    public event Action<HexTile> ClaimChanged;
    public event Action<HexTile> TerritoryInformationChanged;
    public event Action<HexTile> ResourcesChanged;
    public event Action<HexTile> OccupantsChanged;
    public event Action<HexTile> StateChanged;

    public Vector3 GetWaspFormationPosition(
        int spawnIndex,
        float horizontalSpacing,
        float rowSpacing)
    {
        Vector3 center = GetWaspFormationCenter();
        horizontalSpacing = Mathf.Max(0.05f, horizontalSpacing);

        int index = Mathf.Max(0, spawnIndex);
        int capacity = Mathf.Max(1, waspFormationCapacity);

        // Spread wide enough to seat `capacity` wasps at the requested spacing, but never past
        // the hexagon's inradius, so the group always stays on the tile.
        float packedRadius = horizontalSpacing * Mathf.Sqrt(capacity);
        float usableRadius = Mathf.Min(packedRadius, GetFormationUsableRadius());
        if (usableRadius <= 0f)
            return center;

        // Golden-angle (sunflower) placement fills a disc evenly with no rows to run off an edge,
        // and keeps early spawns near the middle so small groups still look gathered.
        float denominator = Mathf.Max(capacity, index + 1);
        float radius = usableRadius * Mathf.Sqrt((index + 0.5f) / denominator);
        float angle = index * GoldenAngleRadians;

        Vector3 slot = center +
                       transform.right * (Mathf.Cos(angle) * radius) +
                       transform.forward * (Mathf.Sin(angle) * radius);

        return ApplyFormationJitter(slot, index);
    }

    /// <summary>
    /// Largest radius around the formation centre that is guaranteed to stay inside the hexagon.
    /// The tile's collider is an axis-aligned box around the hex, so its smaller horizontal extent
    /// is the hexagon's inradius - the shortest centre-to-edge distance, and therefore safe in
    /// every direction.
    /// </summary>
    private float GetFormationUsableRadius()
    {
        Collider collider = ResolveFormationCollider();
        if (collider == null)
            return 1f;

        float inradius = Mathf.Min(collider.bounds.extents.x, collider.bounds.extents.z);
        return Mathf.Max(0f, inradius - waspFormationEdgeMargin);
    }

    /// <summary>
    /// The formation always sits on the middle of the hexagon. It used to be nudged towards the
    /// hive, which pushed large groups off to one side of the tile.
    /// </summary>
    private Vector3 GetWaspFormationCenter()
    {
        Collider collider = ResolveFormationCollider();
        if (collider == null)
            return transform.position;

        Vector3 center = collider.bounds.center;
        center.y = transform.position.y;
        return center;
    }

    private void Start()
    {
        if (combatController == null)
            combatController = GetComponent<HexCombatController>();

        InitializeRuntimeResources();
        RefreshContentVisuals();
        RefreshHexMaterial();
        SynchronizeRuntimeTerritoryInformation();
    }

    private void Update()
    {
        UpdateScouting();
        UpdateGathering();
        UpdateEnemyClaim();
    }

    /// <summary>
    /// Counts down an invasive faction's claim on this hex. The claim only runs while that faction
    /// has an attacker here and the player has none — sending attackers of your own cancels it and
    /// hands the hex over to the normal combat path instead.
    /// </summary>
    private void UpdateEnemyClaim()
    {
        if (!claimInProgress)
            return;

        if (!CanClaimContinue())
        {
            CancelEnemyClaim();
            return;
        }

        claimTimeRemaining = Mathf.Max(0f, claimTimeRemaining - Time.deltaTime);
        ClaimChanged?.Invoke(this);

        if (claimTimeRemaining > 0f)
            return;

        WaspScopeRole faction = claimingFaction;
        CancelEnemyClaim();
        CaptureForEnemy(faction);
    }

    private bool CanClaimContinue()
    {
        if (state != HexState.Owned)
            return false;

        // Defer to the combat controller wherever possible: it is the authority on who is actually
        // fighting here, and it counts units garrisoned at their own hive. Using a different rule
        // here let the claim and the battle disagree about who was standing on the tile.
        if (combatController != null)
        {
            if (combatController.FriendlyAttackerCount > 0)
                return false;

            return combatController.EnemyAttackerCount > 0;
        }

        if (GetFriendlyWaspCount(WaspFunction.Guard) > 0)
            return false;

        return GetEnemyGuardCount(claimingFaction) > 0;
    }

    private int GetEnemyGuardCount(WaspScopeRole faction)
    {
        enemyWasps.RemoveWhere(wasp => wasp == null);
        int count = 0;
        foreach (EnemyWaspControl wasp in enemyWasps)
        {
            if (wasp != null &&
                wasp.AssignedFunction == WaspFunction.Guard &&
                NormalizeEnemyFaction(wasp.Faction) == NormalizeEnemyFaction(faction))
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// Starts (or keeps) an invasive claim on this player-owned hex. Safe to call repeatedly.
    /// </summary>
    public void BeginEnemyClaim(WaspScopeRole faction, float durationSeconds)
    {
        faction = NormalizeEnemyFaction(faction);
        if (state != HexState.Owned)
            return;

        if (claimInProgress && claimingFaction == faction)
            return;

        claimingFaction = faction;
        claimDuration = Mathf.Max(1f, durationSeconds);
        claimTimeRemaining = claimDuration;
        claimInProgress = true;
        // The countdown is the player's window to respond, so it needs to be audible.
        AudioDirector.Play(GameSound.HexClaimCountdown);
        ClaimChanged?.Invoke(this);
    }

    public void CancelEnemyClaim()
    {
        if (!claimInProgress)
            return;

        claimInProgress = false;
        claimTimeRemaining = 0f;
        ClaimChanged?.Invoke(this);
    }

    private void UpdateScouting()
    {
        if (!scoutingInProgress)
            return;

        bool friendlyCanScout = HasFriendlyScout && state == HexState.Unknown;
        bool enemyCanScout = HasEnemyScout && (state == HexState.Unknown || state == HexState.Locked);
        if ((!friendlyCanScout && !enemyCanScout) ||
            (HasFriendlyScout && HasEnemyScout))
        {
            CancelScoutingCountdown();
            return;
        }

        scoutingTimeRemaining = Mathf.Max(0f, scoutingTimeRemaining - Time.deltaTime);
        if (scoutingTimeRemaining > 0f)
            return;

        scoutingInProgress = false;

        if (HasEnemyScout)
            EnemyScout();
        else
            Scout();

        C_MainWorldHUD.GetOrCreate()?.ShowSelectedHex(this);
    }

    private void UpdateGathering()
    {
        int foragerCount = GetFriendlyWaspCount(WaspFunction.Forager);
        if (state != HexState.Owned || foragerCount <= 0 || gatheringRules == null)
        {
            gatheringTickElapsed = 0f;
            return;
        }

        float gatheringSpeed = HiveManagement.Instance != null
            ? Mathf.Max(0.1f, HiveManagement.Instance.GetEffectiveValue(WaspFunction.Forager, WaspSkillStat.GatheringSpeed))
            : 1f;
        gatheringTickElapsed += Time.deltaTime;
        if (gatheringTickElapsed < gatheringRules.GatheringTickIntervalSeconds / gatheringSpeed)
            return;

        gatheringTickElapsed = 0f;
        GatherAvailableResources(foragerCount);

        if (ResourcesDepleted)
            ReturnForagersToHive();
    }
    
    public int GetEnemyWaspCount(WaspFunction function)
    {
        enemyWasps.RemoveWhere(wasp => wasp == null);

        int count = 0;

        foreach (EnemyWaspControl wasp in enemyWasps)
        {
            if (wasp.AssignedFunction == function)
                count++;
        }

        return count;
    }

    public int GetEnemyWaspCount(WaspScopeRole faction, WaspFunction function)
    {
        enemyWasps.RemoveWhere(wasp => wasp == null);

        int count = 0;

        foreach (EnemyWaspControl wasp in enemyWasps)
        {
            if (wasp.Faction == faction && wasp.AssignedFunction == function)
                count++;
        }

        return count;
    }

    private void GatherAvailableResources(int foragerCount)
    {
        if (ResourceManager.Instance == null)
        {
            Debug.LogWarning("No ResourceManager exists in the scene.");
            return;
        }

        float gatheringMultiplier = HiveManagement.Instance != null
            ? Mathf.Max(0f, HiveManagement.Instance.GetEffectiveValue(WaspFunction.Forager, WaspSkillStat.GatheringMultiplier))
            : 1f;
        float gatheredPrey = HasPrey
            ? Mathf.Min(gatheringRules.GetPreyAmount(foragerCount) * gatheringMultiplier, currentPrey)
            : 0f;
        float gatheredNectar = HasNectar
            ? Mathf.Min(gatheringRules.GetNectarAmount(foragerCount) * gatheringMultiplier, currentNectar)
            : 0f;
        float gatheredFibre = HasFibre
            ? Mathf.Min(gatheringRules.GetFibreAmount(foragerCount) * gatheringMultiplier, currentFibre)
            : 0f;

        currentPrey = Mathf.Max(0f, currentPrey - gatheredPrey);
        currentNectar = Mathf.Max(0f, currentNectar - gatheredNectar);
        currentFibre = Mathf.Max(0f, currentFibre - gatheredFibre);

        if (gatheredPrey > 0f || gatheredNectar > 0f || gatheredFibre > 0f)
            ResourceManager.Instance.AddResources(gatheredNectar, gatheredPrey, gatheredFibre);

        ResourcesChanged?.Invoke(this);
        C_MainWorldHUD.GetOrCreate()?.ShowSelectedHex(this);
    }

    private void OnValidate()
    {
        RefreshContentVisuals();
        RefreshHexMaterial();
    }

    public void Scout()
    {
        if (state != HexState.Unknown)
        {
            Debug.LogWarning($"{HexName} cannot be scouted because its state is {state}.");
            return;
        }

        scoutingInProgress = false;
        scoutingTimeRemaining = 0f;
        state = HexState.Scouted;
        RefreshContentVisuals();
        RefreshHexMaterial();
        SynchronizeRuntimeTerritoryInformation();
        StateChanged?.Invoke(this);
        Debug.Log(HasResources ? $"{HexName} contains {Content}." : $"{HexName} is safe.");
    }
    
    public void Scout(bool enemyScout)
    {
        Scout();

        if (enemyScout)
        {
            Debug.Log($"{HexName} scouted by enemy.");
        }
    }
    
    public void EnemyScout()
    {
        EnemyScout(FindFirstEnemyScoutFaction());
    }

    public void EnemyScout(WaspScopeRole faction)
    {
        if (state != HexState.Unknown && state != HexState.Locked)
        {
            Debug.LogWarning($"{HexName} cannot be scouted because its state is {state}.");
            return;
        }

        scoutingInProgress = false;
        scoutingTimeRemaining = 0f;
        enemyOwnerFaction = NormalizeEnemyFaction(faction);
        state = HexState.Enemy;
        RefreshContentVisuals();
        RefreshHexMaterial();
        SynchronizeRuntimeTerritoryInformation();
        StateChanged?.Invoke(this);
        Debug.Log(HasResources ? $"{HexName} contains {Content}." : $"{HexName} is safe.");
    }

    public void Claim()
    {
        if (state != HexState.Scouted)
        {
            Debug.LogWarning($"{HexName} cannot be claimed because it has not been scouted.");
            return;
        }

        state = HexState.Owned;
        RefreshContentVisuals();
        RefreshHexMaterial();
        SynchronizeRuntimeTerritoryInformation();
        StateChanged?.Invoke(this);
        HexProgressionManager.Instance?.NotifyFriendlyClaimed(this);
        ReturnFriendlyScoutsToHive();
        Debug.Log($"{HexName} has been claimed.");
    }

    public void RegisterFriendlyWasp(WaspControl wasp)
    {
        if (wasp == null)
            return;

        friendlyWasps.Add(wasp);
        SynchronizeRuntimeTerritoryInformation();
        EnsureScoutingCountdownForRemainingScout();

        ResourcesChanged?.Invoke(this);
        OccupantsChanged?.Invoke(this);
        combatController?.NotifyOccupantsChanged();
    }

    public void UnregisterFriendlyWasp(WaspControl wasp)
    {
        if (wasp != null)
            friendlyWasps.Remove(wasp);

        if (scoutingInProgress && !HasFriendlyScout)
            CancelScoutingCountdown();

        SynchronizeRuntimeTerritoryInformation();
        ResourcesChanged?.Invoke(this);
        EnsureScoutingCountdownForRemainingScout();
        OccupantsChanged?.Invoke(this);
        combatController?.NotifyOccupantsChanged();
    }

    public int GetFriendlyWaspCount(WaspFunction function)
    {
        friendlyWasps.RemoveWhere(wasp => wasp == null);
        int count = 0;
        foreach (WaspControl wasp in friendlyWasps)
        {
            if (wasp.AssignedFunction == function)
                count++;
        }

        return count;
    }

    public void RegisterEnemyWasp(EnemyWaspControl wasp)
    {
        if (wasp == null)
            return;

        enemyWasps.Add(wasp);
        SynchronizeRuntimeTerritoryInformation();
        EnsureScoutingCountdownForRemainingScout();

        OccupantsChanged?.Invoke(this);
        combatController?.NotifyOccupantsChanged();
    }

    public void UnregisterEnemyWasp(EnemyWaspControl wasp)
    {
        if (wasp != null)
            enemyWasps.Remove(wasp);

        SynchronizeRuntimeTerritoryInformation();
        EnsureScoutingCountdownForRemainingScout();
        OccupantsChanged?.Invoke(this);
        combatController?.NotifyOccupantsChanged();
    }

    public void SynchronizeRuntimeTerritoryInformation()
    {
        if (areaInfo == null)
            return;

        int friendlyCount = FriendlyWaspCount;
        int enemyCount = EnemyWaspCount;
        HexTerritoryState territory;
        HexRiskState risk;
        HexVisibilityState visibility;

        if (friendlyCount > 0 && enemyCount > 0)
        {
            territory = HexTerritoryState.Contested;
            risk = enemyCount > friendlyCount
                ? HexRiskState.AdvancingInvasivePressure
                : HexRiskState.ContestedTerritory;
            visibility = HexVisibilityState.Scouted;
        }
        else if (enemyCount > 0 || state == HexState.Enemy)
        {
            territory = HexTerritoryState.Invasive;
            risk = HexRiskState.InvasiveHotspot;
            visibility = HexVisibilityState.Hidden;
        }
        else if (friendlyCount > 0 || state == HexState.Owned)
        {
            territory = HexTerritoryState.Native;
            risk = HexRiskState.SafeNativeHabitat;
            visibility = HexVisibilityState.Scouted;
        }
        else if (state == HexState.Scouted)
        {
            territory = HexTerritoryState.Neutral;
            risk = HexRiskState.Neutral;
            visibility = HexVisibilityState.Scouted;
        }
        else
        {
            territory = HexTerritoryState.Neutral;
            risk = HexRiskState.Neutral;
            visibility = HexVisibilityState.Hidden;
        }

        if (areaInfo.SetRuntimeTerritoryInformation(territory, risk, visibility))
            TerritoryInformationChanged?.Invoke(this);
    }

    public void GatherPrey()
    {
        GatherPrey(1);
    }

    public float GatherPrey(int waspCount)
    {
        if (!CanGather(HasPrey))
            return 0f;

        EnsureRuntimeResourcesInitialized();
        float requestedAmount = gatheringRules != null ? gatheringRules.GetPreyAmount(waspCount) : 0f;
        float gatheredAmount = Mathf.Min(requestedAmount, currentPrey);
        currentPrey -= gatheredAmount;
        ResourceManager.Instance.AddPrey(gatheredAmount);
        ResourcesChanged?.Invoke(this);
        return gatheredAmount;
    }

    public float GatherNectar(int waspCount)
    {
        if (!CanGather(HasNectar))
            return 0f;

        EnsureRuntimeResourcesInitialized();
        float requestedAmount = gatheringRules != null ? gatheringRules.GetNectarAmount(waspCount) : 0f;
        float gatheredAmount = Mathf.Min(requestedAmount, currentNectar);
        currentNectar -= gatheredAmount;
        ResourceManager.Instance.AddNectar(gatheredAmount);
        ResourcesChanged?.Invoke(this);
        return gatheredAmount;
    }

    public float GatherFibre(int waspCount)
    {
        if (!CanGather(HasFibre))
            return 0f;

        EnsureRuntimeResourcesInitialized();
        float requestedAmount = gatheringRules != null ? gatheringRules.GetFibreAmount(waspCount) : 0f;
        float gatheredAmount = Mathf.Min(requestedAmount, currentFibre);
        currentFibre -= gatheredAmount;
        ResourceManager.Instance.AddFibre(gatheredAmount);
        ResourcesChanged?.Invoke(this);
        return gatheredAmount;
    }

    public float GetPreyGatherAmount(int waspCount)
    {
        float multiplier = HiveManagement.Instance != null
            ? HiveManagement.Instance.GetEffectiveValue(WaspFunction.Forager, WaspSkillStat.GatheringMultiplier)
            : 1f;
        return gatheringRules != null ? Mathf.Min(gatheringRules.GetPreyAmount(waspCount) * multiplier, PreyRemaining) : 0f;
    }

    public float GetNectarGatherAmount(int waspCount)
    {
        float multiplier = HiveManagement.Instance != null
            ? HiveManagement.Instance.GetEffectiveValue(WaspFunction.Forager, WaspSkillStat.GatheringMultiplier)
            : 1f;
        return gatheringRules != null ? Mathf.Min(gatheringRules.GetNectarAmount(waspCount) * multiplier, NectarRemaining) : 0f;
    }

    public float GetFibreGatherAmount(int waspCount)
    {
        float multiplier = HiveManagement.Instance != null
            ? HiveManagement.Instance.GetEffectiveValue(WaspFunction.Forager, WaspSkillStat.GatheringMultiplier)
            : 1f;
        return gatheringRules != null ? Mathf.Min(gatheringRules.GetFibreAmount(waspCount) * multiplier, FibreRemaining) : 0f;
    }

    private bool CanGather(bool hasResource)
    {
        if (state != HexState.Owned)
        {
            Debug.LogWarning($"{HexName} must be owned before gathering resources.");
            return false;
        }

        if (!hasResource)
        {
            Debug.LogWarning($"{HexName} does not contain the requested resource.");
            return false;
        }

        if (ResourceManager.Instance == null)
        {
            Debug.LogWarning("No ResourceManager exists in the scene.");
            return false;
        }

        return true;
    }

    public void SetFriendlyHive(C_Friendly_Hive_Orc hive)
    {
        friendlyHive = hive;
        NotifyCombatInformationChanged();
    }

    public void SetEnemyHive(C_Enemy_Hive_Orc hive)
    {
        enemyHive = hive;
        if (hive != null)
            enemyOwnerFaction = NormalizeEnemyFaction(hive.Faction);
        NotifyCombatInformationChanged();
    }

    public void CaptureForFriendly()
    {
        CancelEnemyClaim();
        bool gained = state != HexState.Owned;
        state = HexState.Owned;
        playerAccessible = true;
        if (gained)
            AudioDirector.Play(GameSound.HexCaptured);
        RefreshStateVisuals();
        HexProgressionManager.Instance?.NotifyFriendlyClaimed(this);
        ReturnFriendlyScoutsToHive();
    }

    /// <summary>
    /// Hands a hex back to no-one. Used when an invasive faction capitulates: its territory should
    /// stop belonging to it without pretending the player never scouted it.
    /// </summary>
    public void ReleaseToNeutral()
    {
        CancelEnemyClaim();
        state = HexState.Scouted;
        RefreshStateVisuals();
        StateChanged?.Invoke(this);
        TerritoryInformationChanged?.Invoke(this);
    }

    public void CaptureForEnemy()
    {
        CaptureForEnemy(enemyOwnerFaction);
    }

    public void CaptureForEnemy(WaspScopeRole faction)
    {
        // Only sting when ground the player actually held is taken.
        if (state == HexState.Owned)
            AudioDirector.Play(GameSound.HexLost);

        enemyOwnerFaction = NormalizeEnemyFaction(faction);
        state = HexState.Enemy;
        RefreshStateVisuals();
        HexProgressionManager.Instance?.NotifyEnemyClaimed(this);
    }

    public void SetPlayerAccessible(bool value)
    {
        playerAccessible = value || state == HexState.Owned;
        if (!playerAccessible && state != HexState.Enemy)
        {
            if (state != HexState.Locked)
                stateBeforeLock = state;
            state = HexState.Locked;
        }
        else if (playerAccessible && state == HexState.Locked)
        {
            state = stateBeforeLock == HexState.Locked ? HexState.Unknown : stateBeforeLock;
        }

        RefreshStateVisuals();
    }

    public void RefreshStateVisuals()
    {
        RefreshContentVisuals();
        RefreshHexMaterial();
        SynchronizeRuntimeTerritoryInformation();
        StateChanged?.Invoke(this);
        NotifyCombatInformationChanged();
    }

    public void NotifyCombatInformationChanged()
    {
        ResourcesChanged?.Invoke(this);
        C_MainWorldHUD.GetOrCreate()?.ShowSelectedHex(this);
    }

    private void ReturnForagersToHive()
    {
        List<WaspControl> returningForagers = new List<WaspControl>();
        foreach (WaspControl wasp in friendlyWasps)
        {
            if (wasp != null && wasp.AssignedFunction == WaspFunction.Forager)
                returningForagers.Add(wasp);
        }

        foreach (WaspControl wasp in returningForagers)
            wasp.ReturnToHomeHive();
    }

    private void InitializeRuntimeResources()
    {
        currentPrey = areaInfo != null ? areaInfo.StartingPrey : 0f;
        currentNectar = areaInfo != null ? areaInfo.StartingNectar : 0f;
        currentFibre = areaInfo != null ? areaInfo.StartingFibre : 0f;
        resourcesInitialized = true;
    }

    private void EnsureRuntimeResourcesInitialized()
    {
        if (!resourcesInitialized)
            InitializeRuntimeResources();
    }

    public bool HasResources => HasPrey || HasNectar || HasFibre;

    private void CancelScoutingCountdown()
    {
        scoutingInProgress = false;
        scoutingTimeRemaining = 0f;
    }

    private void ReturnFriendlyScoutsToHive()
    {
        foreach (WaspControl wasp in new List<WaspControl>(friendlyWasps))
        {
            if (wasp != null && wasp.AssignedFunction == WaspFunction.Scout)
                wasp.ReturnToHomeHive();
        }
    }

    private void EnsureScoutingCountdownForRemainingScout()
    {
        bool validFriendly = HasFriendlyScout && state == HexState.Unknown;
        bool validEnemy = HasEnemyScout && (state == HexState.Unknown || state == HexState.Locked);
        if (scoutingInProgress || HasFriendlyScout == HasEnemyScout || (!validFriendly && !validEnemy))
            return;

        scoutingInProgress = true;
        float scoutingSpeed = HasEnemyScout
            ? EnemyHiveControl.Instance != null
                ? EnemyHiveControl.Instance.GetEffectiveValue(FindFirstEnemyScoutFaction(), WaspFunction.Scout, WaspSkillStat.Identification)
                : 1f
            : HiveManagement.Instance != null
                ? HiveManagement.Instance.GetEffectiveValue(WaspFunction.Scout, WaspSkillStat.Identification)
                : 1f;
        scoutingTimeRemaining = scoutingDuration / Mathf.Max(0.1f, scoutingSpeed);
    }

    private WaspScopeRole FindFirstEnemyScoutFaction()
    {
        enemyWasps.RemoveWhere(wasp => wasp == null);
        foreach (EnemyWaspControl wasp in enemyWasps)
        {
            if (wasp.AssignedFunction == WaspFunction.Scout)
                return NormalizeEnemyFaction(wasp.Faction);
        }
        return enemyOwnerFaction;
    }

    private static WaspScopeRole NormalizeEnemyFaction(WaspScopeRole faction)
    {
        return faction == WaspScopeRole.SecondaryInvasive
            ? WaspScopeRole.SecondaryInvasive
            : WaspScopeRole.PrimaryInvasive;
    }

    /// <summary>
    /// Scatters a slot by a small deterministic offset, so the same spawn index always resolves
    /// to the same spot even when the formation is recalculated. Falls back to the exact slot if
    /// the scattered point would land outside the hexagon.
    /// </summary>
    private Vector3 ApplyFormationJitter(Vector3 position, int spawnIndex)
    {
        if (waspFormationJitter <= 0f)
            return position;

        float angle = GetFormationNoise(spawnIndex * 2 + 1) * Mathf.PI * 2f;
        float radius = Mathf.Sqrt(GetFormationNoise(spawnIndex * 2 + 2)) * waspFormationJitter;
        Vector3 jittered = position +
                           transform.right * (Mathf.Cos(angle) * radius) +
                           transform.forward * (Mathf.Sin(angle) * radius);

        return IsInsideHexagon(jittered) ? jittered : position;
    }

    /// <summary>
    /// True point-in-regular-hexagon test, replacing the collider check. The tile collider is an
    /// axis-aligned box, so it wrongly reports the four box corners - which lie outside the
    /// hexagon - as valid ground.
    /// </summary>
    private bool IsInsideHexagon(Vector3 worldPosition)
    {
        Collider collider = ResolveFormationCollider();
        if (collider == null)
            return true;

        Vector3 local = worldPosition - collider.bounds.center;
        float x = Vector3.Dot(local, transform.right);
        float z = Vector3.Dot(local, transform.forward);

        float inradius = Mathf.Min(collider.bounds.extents.x, collider.bounds.extents.z) - waspFormationEdgeMargin;
        if (inradius <= 0f)
            return false;

        // A regular hexagon is the intersection of three slabs, each one inradius from the centre.
        const float Cos30 = 0.8660254f;
        return Mathf.Abs(x) <= inradius &&
               Mathf.Abs(0.5f * x + Cos30 * z) <= inradius &&
               Mathf.Abs(0.5f * x - Cos30 * z) <= inradius;
    }

    private static float GetFormationNoise(int value)
    {
        uint hash = (uint)value * 2654435761u;
        hash ^= hash >> 15;
        hash *= 2246822519u;
        hash ^= hash >> 13;
        return hash % 10000u / 10000f;
    }

    private Collider ResolveFormationCollider()
    {
        if (formationCollider != null)
            return formationCollider;

        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        float largestArea = 0f;

        foreach (Collider candidate in colliders)
        {
            if (candidate == null || candidate.isTrigger)
                continue;

            Bounds bounds = candidate.bounds;
            float area = bounds.size.x * bounds.size.z;
            if (area <= largestArea)
                continue;

            largestArea = area;
            formationCollider = candidate;
        }

        return formationCollider;
    }

    private void RefreshContentVisuals()
    {
        if (antGroup == null)
            return;

        antGroup.SetActive(HasResources && state != HexState.Unknown && state != HexState.Locked);
    }

    private void RefreshHexMaterial()
    {
        if (hexRenderer == null)
            return;

        if (!playerAccessible && state != HexState.Owned && state != HexState.Enemy)
        {
            if (lockedMaterial != null)
                hexRenderer.sharedMaterial = lockedMaterial;
            return;
        }

        switch (state)
        {
            case HexState.Locked:
                if (lockedMaterial != null)
                    hexRenderer.sharedMaterial = lockedMaterial;
                break;
            case HexState.Owned:
                if (ownedMaterial != null)
                    hexRenderer.sharedMaterial = ownedMaterial;
                break;
            case HexState.Scouted:
                if (HasResources && preyMaterial != null)
                    hexRenderer.sharedMaterial = preyMaterial;
                else if (unknownMaterial != null)
                    hexRenderer.sharedMaterial = unknownMaterial;
                break;
            case HexState.Unknown:
                if (unknownMaterial != null)
                    hexRenderer.sharedMaterial = unknownMaterial;
                break;
            case HexState.Enemy:
                if (enemyMaterial != null)
                    hexRenderer.sharedMaterial = enemyMaterial;
                break;
        }
    }
}
