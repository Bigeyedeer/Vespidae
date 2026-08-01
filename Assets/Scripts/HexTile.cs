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

    [Header("Hive Spawning")]
    [SerializeField] private Transform hiveSpawnPoint;

    [Header("Scouting")]
    [SerializeField, Min(0.1f)] private float scoutingDuration = 10f;

    private bool scoutingInProgress;
    private float scoutingTimeRemaining;

    public SB_Hex_Area_Info AreaInfo => areaInfo;
    public SB_Hex_Gathering_Rules GatheringRules => gatheringRules;
    public string HexName => areaInfo != null ? areaInfo.AreaName : gameObject.name;
    public HexState State => state;
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
    public Transform HiveSpawnPoint => hiveSpawnPoint != null ? hiveSpawnPoint : transform.Find("HiveSpawnpoint") ?? transform;
    public C_Friendly_Hive_Orc FriendlyHive => friendlyHive;
    public bool HasFriendlyScout => GetFriendlyWaspCount(WaspFunction.Scout) > 0;
    public IReadOnlyCollection<WaspControl> FriendlyWasps => friendlyWasps;
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
    public event Action<HexTile> TerritoryInformationChanged;
    public event Action<HexTile> ResourcesChanged;

    public Vector3 GetWaspFormationPosition(
        int spawnIndex,
        float horizontalSpacing,
        float rowSpacing)
    {
        Vector3 center = transform.position;
        if (spawnIndex <= 0)
            return center;

        horizontalSpacing = Mathf.Max(0.05f, horizontalSpacing);
        rowSpacing = Mathf.Max(0.05f, rowSpacing);
        List<Vector3> positions = new List<Vector3>();
        AddFormationPositions(positions, center, -1f, 0, -1f, 0, horizontalSpacing, rowSpacing);
        AddFormationPositions(positions, center, 1f, 1, -1f, 0, horizontalSpacing, rowSpacing);
        AddFormationPositions(positions, center, -1f, 0, 1f, 1, horizontalSpacing, rowSpacing);
        AddFormationPositions(positions, center, 1f, 1, 1f, 1, horizontalSpacing, rowSpacing);

        return spawnIndex < positions.Count ? positions[spawnIndex] : center;
    }

    private void Start()
    {
        InitializeRuntimeResources();
        RefreshContentVisuals();
        RefreshHexMaterial();
        SynchronizeRuntimeTerritoryInformation();
    }

    private void Update()
    {
        UpdateScouting();
        UpdateGathering();
    }

    private void UpdateScouting()
    {
        if (!scoutingInProgress)
            return;

        if (state != HexState.Unknown || !HasFriendlyScout)
        {
            CancelScoutingCountdown();
            return;
        }

        scoutingTimeRemaining = Mathf.Max(0f, scoutingTimeRemaining - Time.deltaTime);
        if (scoutingTimeRemaining > 0f)
            return;

        scoutingInProgress = false;
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

        gatheringTickElapsed += Time.deltaTime;
        if (gatheringTickElapsed < gatheringRules.GatheringTickIntervalSeconds)
            return;

        gatheringTickElapsed = 0f;
        if (HasPrey && PreyRemaining > 0f)
            GatherPrey(foragerCount);
        if (HasNectar && NectarRemaining > 0f)
            GatherNectar(foragerCount);
        if (HasFibre && FibreRemaining > 0f)
            GatherFibre(foragerCount);
        ResourcesChanged?.Invoke(this);
        C_MainWorldHUD.GetOrCreate()?.ShowSelectedHex(this);

        if (ResourcesDepleted)
            ReturnForagersToHive();
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
        Debug.Log($"{HexName} has been claimed.");
    }

    public void RegisterFriendlyWasp(WaspControl wasp)
    {
        if (wasp == null)
            return;

        friendlyWasps.Add(wasp);
        SynchronizeRuntimeTerritoryInformation();
        if (wasp.AssignedFunction == WaspFunction.Scout &&
            state == HexState.Unknown &&
            !scoutingInProgress)
        {
            scoutingInProgress = true;
            scoutingTimeRemaining = scoutingDuration;
        }

        ResourcesChanged?.Invoke(this);
    }

    public void UnregisterFriendlyWasp(WaspControl wasp)
    {
        if (wasp != null)
            friendlyWasps.Remove(wasp);

        if (scoutingInProgress && !HasFriendlyScout)
            CancelScoutingCountdown();

        SynchronizeRuntimeTerritoryInformation();
        ResourcesChanged?.Invoke(this);
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
    }

    public void UnregisterEnemyWasp(EnemyWaspControl wasp)
    {
        if (wasp != null)
            enemyWasps.Remove(wasp);

        SynchronizeRuntimeTerritoryInformation();
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
        return gatheringRules != null ? Mathf.Min(gatheringRules.GetPreyAmount(waspCount), PreyRemaining) : 0f;
    }

    public float GetNectarGatherAmount(int waspCount)
    {
        return gatheringRules != null ? Mathf.Min(gatheringRules.GetNectarAmount(waspCount), NectarRemaining) : 0f;
    }

    public float GetFibreGatherAmount(int waspCount)
    {
        return gatheringRules != null ? Mathf.Min(gatheringRules.GetFibreAmount(waspCount), FibreRemaining) : 0f;
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

    private bool HasResources => HasPrey || HasNectar || HasFibre;

    private void CancelScoutingCountdown()
    {
        scoutingInProgress = false;
        scoutingTimeRemaining = 0f;
    }

    private bool ContainsFormationPoint(Vector3 worldPosition)
    {
        Collider collider = ResolveFormationCollider();
        if (collider == null)
            return true;

        Vector3 probe = new Vector3(
            worldPosition.x,
            collider.bounds.center.y,
            worldPosition.z
        );

        Vector3 closestPoint = collider.ClosestPoint(probe);
        return (closestPoint - probe).sqrMagnitude <= 0.0001f;
    }

    private void AddFormationPositions(
        List<Vector3> positions,
        Vector3 center,
        float rowDirection,
        int firstRow,
        float columnDirection,
        int firstColumn,
        float horizontalSpacing,
        float rowSpacing)
    {
        for (int row = firstRow; row < 64; row++)
        {
            Vector3 rowStart = center +
                               transform.forward *
                               row *
                               rowSpacing *
                               rowDirection;

            if (!ContainsFormationPoint(rowStart))
                break;

            for (int column = firstColumn; column < 64; column++)
            {
                Vector3 candidate = rowStart +
                                    transform.right *
                                    column *
                                    horizontalSpacing *
                                    columnDirection;

                if (!ContainsFormationPoint(candidate))
                    break;

                if (!positions.Contains(candidate))
                    positions.Add(candidate);
            }
        }
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
