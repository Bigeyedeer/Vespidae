using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public enum HexResourceType
{
    None = 0,
    Prey = 1,
    Nectar = 2,
    PreyAndNectar = 3,
    Fibre = 4,
    PreyAndFibre = 5,
    NectarAndFibre = 6,
    PreyNectarAndFibre = 7,
    Protein = Prey,
    Sugar = Nectar,
    ProteinAndSugar = PreyAndNectar
}

public enum HexTerritoryState
{
    Neutral,
    Native,
    Invasive,
    Contested
}

public enum HexRiskState
{
    SafeNativeHabitat,
    ContestedTerritory,
    AdvancingInvasivePressure,
    InvasiveHotspot,
    Neutral
}

public enum HexVisibilityState
{
    Hidden,
    Scouted,
    Investigated
}

[CreateAssetMenu(fileName = "SO_HexArea_Info", menuName = "Vespidae Wars/Hex Area Information")]
public class SB_Hex_Area_Info : ScriptableObject
{
    [Header("Area Information")]
    [SerializeField] private string areaId;
    [SerializeField] private string areaName;
    [SerializeField, TextArea(2, 5)] private string areaDescription;
    [SerializeField] private string habitatCue;

    [Header("Territory Information")]
    [SerializeField] private HexTerritoryState territoryState;
    [SerializeField] private HexRiskState riskState;
    [SerializeField] private HexVisibilityState visibilityState;
    [SerializeField] private List<string> connectedHexIds = new List<string>();

    [Header("Area Resources")]
    [SerializeField] private HexResourceType resourceType = HexResourceType.None;
    [FormerlySerializedAs("startingProtein")]
    [SerializeField, Min(0f)] private float startingPrey;
    [FormerlySerializedAs("startingSugar")]
    [SerializeField, Min(0f)] private float startingNectar;
    [SerializeField, Min(0f)] private float startingFibre;

    [Header("Species Present")]
    [SerializeField] private List<SB_Wasps_Info> waspsPresent = new List<SB_Wasps_Info>();

    [NonSerialized] private bool hasRuntimeTerritoryInformation;
    [NonSerialized] private HexTerritoryState runtimeTerritoryState;
    [NonSerialized] private HexRiskState runtimeRiskState;
    [NonSerialized] private HexVisibilityState runtimeVisibilityState;

    public string AreaId => areaId;
    public string AreaName => areaName;
    public string AreaDescription => areaDescription;
    public string HabitatCue => habitatCue;
    public HexTerritoryState TerritoryState => hasRuntimeTerritoryInformation ? runtimeTerritoryState : territoryState;
    public HexRiskState RiskState => hasRuntimeTerritoryInformation ? runtimeRiskState : riskState;
    public HexVisibilityState VisibilityState => hasRuntimeTerritoryInformation ? runtimeVisibilityState : visibilityState;
    public IReadOnlyList<string> ConnectedHexIds => connectedHexIds;
    public int ConnectedSiteCount => connectedHexIds == null ? 0 : connectedHexIds.Count;
    public HexResourceType ResourceType => resourceType;
    public float StartingPrey => startingPrey;
    public float StartingNectar => startingNectar;
    public float StartingFibre => startingFibre;
    public IReadOnlyList<SB_Wasps_Info> WaspsPresent => waspsPresent;
    public bool HasPrey => ContainsResource(HexResourceType.Prey);
    public bool HasNectar => ContainsResource(HexResourceType.Nectar);
    public bool HasFibre => ContainsResource(HexResourceType.Fibre);

    public bool SetRuntimeTerritoryInformation(
        HexTerritoryState territory,
        HexRiskState risk,
        HexVisibilityState visibility)
    {
        bool changed = !hasRuntimeTerritoryInformation ||
                       runtimeTerritoryState != territory ||
                       runtimeRiskState != risk ||
                       runtimeVisibilityState != visibility;

        hasRuntimeTerritoryInformation = true;
        runtimeTerritoryState = territory;
        runtimeRiskState = risk;
        runtimeVisibilityState = visibility;
        return changed;
    }

    public void ClearRuntimeTerritoryInformation()
    {
        hasRuntimeTerritoryInformation = false;
    }

#if UNITY_EDITOR
    public void ConfigureForEditor(
        string id,
        string name,
        string description,
        string habitat,
        HexResourceType resources,
        float prey,
        float nectar,
        List<SB_Wasps_Info> species,
        float fibre = 0f)
    {
        areaId = id;
        areaName = name;
        areaDescription = description;
        habitatCue = habitat;
        resourceType = resources;
        startingPrey = Mathf.Max(0f, prey);
        startingNectar = Mathf.Max(0f, nectar);
        startingFibre = Mathf.Max(0f, fibre);
        waspsPresent = species ?? new List<SB_Wasps_Info>();
    }
#endif

    private bool ContainsResource(HexResourceType resource)
    {
        return resourceType == resource ||
               (resource == HexResourceType.Prey && (resourceType == HexResourceType.PreyAndNectar || resourceType == HexResourceType.PreyAndFibre || resourceType == HexResourceType.PreyNectarAndFibre)) ||
               (resource == HexResourceType.Nectar && (resourceType == HexResourceType.PreyAndNectar || resourceType == HexResourceType.NectarAndFibre || resourceType == HexResourceType.PreyNectarAndFibre)) ||
               (resource == HexResourceType.Fibre && (resourceType == HexResourceType.Fibre || resourceType == HexResourceType.PreyAndFibre || resourceType == HexResourceType.NectarAndFibre || resourceType == HexResourceType.PreyNectarAndFibre));
    }
}
