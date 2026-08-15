using System;
using System.Collections.Generic;
using UnityEngine;

public enum WaspClassification
{
    Native,
    Invasive
}

public enum WaspScopeRole
{
    NativePlayer,
    PrimaryInvasive,
    SecondaryInvasive
}

public enum WaspFunction
{
    Scout,
    Forager,
    Builder,
    BroodCaretaker,
    Guard,
    Containment
}

[Serializable]
public class WaspFunctionInfo
{
    [SerializeField] private WaspFunction function;
    [SerializeField] private string displayName;
    [SerializeField, TextArea(2, 4)] private string description;
    [SerializeField, TextArea(1, 3)] private string startingBenefit;
    [SerializeField] private Sprite icon;

    public WaspFunction Function => function;
    public string DisplayName => displayName;
    public string Description => description;
    public string StartingBenefit => startingBenefit;
    public Sprite Icon => icon;

    public WaspFunctionInfo(
        WaspFunction function,
        string displayName,
        string description,
        string startingBenefit,
        Sprite icon = null)
    {
        this.function = function;
        this.displayName = displayName;
        this.description = description;
        this.startingBenefit = startingBenefit;
        this.icon = icon;
    }
}

/// <summary>
/// One line of the identification card, released to the player after enough combat encounters with
/// this species. Ordered: entry 0 is revealed first.
/// </summary>
[Serializable]
public class WaspCodexEntry
{
    [SerializeField] private string label;
    [SerializeField, TextArea(1, 4)] private string value;

    public string Label => label;
    public string Value => value;

    public WaspCodexEntry(string label, string value)
    {
        this.label = label;
        this.value = value;
    }
}

[CreateAssetMenu(fileName = "SO_WaspSpecies", menuName = "Vespidae Wars/Wasp Species")]
public class SB_Wasps_Info : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string speciesId;
    [SerializeField] private string commonName;
    [SerializeField] private string scientificName;
    [SerializeField] private WaspClassification classification;
    [SerializeField] private WaspScopeRole scopeRole;
    [SerializeField] private bool isPlayable;

    [Header("Presentation")]
    [SerializeField] private Sprite portrait;
    [SerializeField] private Color accentColor = new Color(0.84f, 0.66f, 0.29f, 1f);

    [Header("Species Reference")]
    [SerializeField, TextArea(2, 5)] private string gameplaySummary;
    [SerializeField, TextArea(2, 5)] private string ecologicalRole;
    [SerializeField, TextArea(1, 3)] private string nestType;
    [SerializeField, TextArea(1, 3)] private string habitatCue;
    [SerializeField, TextArea(1, 3)] private string threatResponse;
    [SerializeField, TextArea(1, 3)] private string learningLesson;

    [Header("Identification Codex")]
    [SerializeField, Tooltip("Released one at a time as the player fights this species. Order matters - the " +
                             "first entry is revealed first. Add more freely; the panel shows as many as fit.")]
    private List<WaspCodexEntry> codexEntries = new List<WaspCodexEntry>();
    [SerializeField, Range(0f, 1f), Tooltip("Drives the threat response bar on the identification card.")]
    private float threatLevel = 0.5f;

    [Header("Playable Colony Functions")]
    [SerializeField] private List<WaspFunctionInfo> availableFunctions = new List<WaspFunctionInfo>();

    public string SpeciesId => speciesId;
    public string CommonName => commonName;
    public string ScientificName => scientificName;
    public WaspClassification Classification => classification;
    public WaspScopeRole ScopeRole => scopeRole;
    public bool IsPlayable => isPlayable;
    public Sprite Portrait => portrait;
    public Color AccentColor => accentColor;
    public string GameplaySummary => gameplaySummary;
    public string EcologicalRole => ecologicalRole;
    public string NestType => nestType;
    public string HabitatCue => habitatCue;
    public string ThreatResponse => threatResponse;
    public string LearningLesson => learningLesson;
    public IReadOnlyList<WaspFunctionInfo> AvailableFunctions => availableFunctions;
    public IReadOnlyList<WaspCodexEntry> CodexEntries => codexEntries;
    public float ThreatLevel => threatLevel;

    public WaspFunctionInfo GetFunction(WaspFunction function)
    {
        foreach (WaspFunctionInfo functionInfo in availableFunctions)
        {
            if (functionInfo != null && functionInfo.Function == function)
            {
                return functionInfo;
            }
        }

        return null;
    }

#if UNITY_EDITOR
    public void ConfigureForEditor(
        string id,
        string common,
        string scientific,
        WaspClassification waspClassification,
        WaspScopeRole waspScopeRole,
        bool playable,
        Color color,
        string summary,
        string role,
        string nest,
        string habitat,
        string threat,
        string lesson,
        List<WaspFunctionInfo> functions)
    {
        speciesId = id;
        commonName = common;
        scientificName = scientific;
        classification = waspClassification;
        scopeRole = waspScopeRole;
        isPlayable = playable;
        accentColor = color;
        gameplaySummary = summary;
        ecologicalRole = role;
        nestType = nest;
        habitatCue = habitat;
        threatResponse = threat;
        learningLesson = lesson;
        availableFunctions = functions ?? new List<WaspFunctionInfo>();
    }

    public void ConfigureCodexForEditor(List<WaspCodexEntry> entries, float threat)
    {
        codexEntries = entries ?? new List<WaspCodexEntry>();
        threatLevel = Mathf.Clamp01(threat);
    }
#endif
}
