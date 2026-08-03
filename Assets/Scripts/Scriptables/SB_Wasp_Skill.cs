using UnityEngine;
using System;

[Serializable]
public struct WaspSkillCost
{
    [Min(0)] public float nectar;
    [Min(0)] public float prey;
    [Min(0)] public float fibre;
    [Min(0)] public int skillPoints;

    public WaspSkillCost(float nectar, float prey, float fibre, int skillPoints)
    {
        this.nectar = Mathf.Max(0f, nectar);
        this.prey = Mathf.Max(0f, prey);
        this.fibre = Mathf.Max(0f, fibre);
        this.skillPoints = Mathf.Max(0, skillPoints);
    }
}

public enum WaspSkillStat
{
    ScoutingRange,
    MovementSpeed,
    GatheringMultiplier,
    BuildSpeed,
    BroodCare,
    Defence,
    AttackSpeed,
    Identification
}

[CreateAssetMenu(fileName = "SO_Wasp_Skill", menuName = "Vespidae Wars/Wasp Skill")]
public class SB_Wasp_Skill : ScriptableObject
{
    [Header("Universal Role Identifier")]
    [SerializeField] private WaspFunction function;

    [Header("Skill Information")]
    [SerializeField] private Sprite roleIcon;
    [SerializeField] private string displayName;
    [SerializeField, TextArea(2, 4)] private string description;
    [SerializeField, TextArea(1, 3)] private string effectSummary;
    [SerializeField, Min(1)] private int maximumLevel = 3;
    [SerializeField] private WaspSkillCost trainingCost;
    [SerializeField] private WaspSkillCost upgradeCost = new WaspSkillCost(50f, 0f, 0f, 0);
    [SerializeField] private WaspSkillCost hiveConstructionCost;

    [Header("Base Values")]
    [SerializeField] private float baseScoutingRange = 1f;
    [SerializeField] private float baseMovementSpeed = 1f;
    [SerializeField] private float baseGatheringMultiplier = 1f;
    [SerializeField] private float baseBuildSpeed = 1f;
    [SerializeField] private float baseBroodCare = 1f;
    [SerializeField] private float baseDefence = 1f;
    [SerializeField] private float baseAttackSpeed = 1f;
    [SerializeField] private float baseIdentification = 1f;

    [Header("Upgrade Values Per Level")]
    [SerializeField] private float scoutingRangePerLevel = 0.25f;
    [SerializeField] private float movementSpeedPerLevel = 0.05f;
    [SerializeField] private float gatheringMultiplierPerLevel = 0.1f;
    [SerializeField] private float buildSpeedPerLevel = 0.1f;
    [SerializeField] private float broodCarePerLevel = 0.1f;
    [SerializeField] private float defencePerLevel = 0.1f;
    [SerializeField] private float attackSpeedPerLevel = 0.1f;
    [SerializeField] private float identificationPerLevel = 0.1f;

    public WaspFunction Function => function;
    public Sprite RoleIcon => roleIcon;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? function.ToString() : displayName;
    public string Description => description;
    public string EffectSummary => effectSummary;
    public int MaximumLevel => Mathf.Max(1, maximumLevel);
    public WaspSkillCost TrainingCost => trainingCost;
    public WaspSkillCost HiveConstructionCost => hiveConstructionCost;

    public WaspSkillCost GetUpgradeCost(int nextLevel)
    {
        int level = Mathf.Max(1, nextLevel);
        return new WaspSkillCost(
            upgradeCost.nectar * level,
            upgradeCost.prey * level,
            upgradeCost.fibre * level,
            upgradeCost.skillPoints * level);
    }

    public float GetEffectiveValue(WaspSkillStat stat, int level)
    {
        int clampedLevel = Mathf.Clamp(level, 0, MaximumLevel);

        switch (stat)
        {
            case WaspSkillStat.ScoutingRange:
                return baseScoutingRange + scoutingRangePerLevel * clampedLevel;
            case WaspSkillStat.MovementSpeed:
                return baseMovementSpeed + movementSpeedPerLevel * clampedLevel;
            case WaspSkillStat.GatheringMultiplier:
                return baseGatheringMultiplier + gatheringMultiplierPerLevel * clampedLevel;
            case WaspSkillStat.BuildSpeed:
                return baseBuildSpeed + buildSpeedPerLevel * clampedLevel;
            case WaspSkillStat.BroodCare:
                return baseBroodCare + broodCarePerLevel * clampedLevel;
            case WaspSkillStat.Defence:
                return baseDefence + defencePerLevel * clampedLevel;
            case WaspSkillStat.AttackSpeed:
                return baseAttackSpeed + attackSpeedPerLevel * clampedLevel;
            case WaspSkillStat.Identification:
                return baseIdentification + identificationPerLevel * clampedLevel;
            default:
                return 1f;
        }
    }

    public void ConfigureRuntime(
        WaspFunction role,
        string name,
        string detail,
        string effect,
        WaspSkillCost cost)
    {
        function = role;
        displayName = name;
        description = detail;
        effectSummary = effect;
        upgradeCost = cost;
    }
}
