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

    public SB_Hex_Area_Info AreaInfo => areaInfo;
    public SB_Hex_Gathering_Rules GatheringRules => gatheringRules;
    public string HexName => areaInfo != null ? areaInfo.AreaName : gameObject.name;
    public HexState State => state;
    public HexResourceType Content => areaInfo != null ? areaInfo.ResourceType : HexResourceType.None;
    public string AreaDescription => areaInfo != null ? areaInfo.AreaDescription : string.Empty;
    public string HabitatCue => areaInfo != null ? areaInfo.HabitatCue : string.Empty;
    public HexTerritoryState TerritoryState => areaInfo != null ? areaInfo.TerritoryState : HexTerritoryState.Neutral;
    public HexRiskState RiskState => areaInfo != null ? areaInfo.RiskState : HexRiskState.SafeNativeHabitat;
    public int ConnectedSiteCount => areaInfo != null ? areaInfo.ConnectedSiteCount : 0;
    public IReadOnlyList<SB_Wasps_Info> WaspsPresent => areaInfo != null ? areaInfo.WaspsPresent : Array.Empty<SB_Wasps_Info>();
    public bool HasPrey => areaInfo != null && areaInfo.HasPrey;
    public bool HasNectar => areaInfo != null && areaInfo.HasNectar;
    public bool HasFibre => areaInfo != null && areaInfo.HasFibre;
    public float PreyRemaining => currentPrey;
    public float NectarRemaining => currentNectar;
    public float FibreRemaining => currentFibre;
    public float GatheringTickIntervalSeconds => gatheringRules != null ? gatheringRules.GatheringTickIntervalSeconds : 0f;

    public Vector3 FocusPosition => focusPoint != null ? focusPoint.position : transform.position;

    private void Start()
    {
        InitializeRuntimeResources();
        RefreshContentVisuals();
        RefreshHexMaterial();
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

        state = HexState.Scouted;
        RefreshContentVisuals();
        RefreshHexMaterial();
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
        Debug.Log($"{HexName} has been claimed.");
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
