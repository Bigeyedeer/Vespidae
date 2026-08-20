using UnityEngine;
using System;
using System.Collections.Generic;

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
    GatheringSpeed,
    BuildSpeed,
    BroodCare,
    Defence,
    AttackSpeed,
    Identification,
    MaximumHealth,
    AttackDamage
}

[Serializable]
public struct WaspSkillStatPreview
{
    public WaspSkillStat stat;
    public float currentValue;
    public float nextValue;
    public float increment;
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
    [SerializeField] private float baseGatheringSpeed = 1f;
    [SerializeField] private float baseBuildSpeed = 1f;
    [SerializeField] private float baseBroodCare = 1f;
    [SerializeField] private float baseDefence = 1f;
    [SerializeField] private float baseAttackSpeed = 1f;
    [SerializeField] private float baseIdentification = 1f;
    [SerializeField, Min(1f)] private float baseMaximumHealth = 100f;
    [SerializeField, Min(0f)] private float baseAttackDamage = 10f;

    [Header("Upgrade Values Per Level")]
    [SerializeField] private float scoutingRangePerLevel = 0.25f;
    [SerializeField] private float movementSpeedPerLevel = 0.05f;
    [SerializeField] private float gatheringMultiplierPerLevel = 0.1f;
    [SerializeField] private float gatheringSpeedPerLevel = 0.1f;
    [SerializeField] private float buildSpeedPerLevel = 0.1f;
    [SerializeField] private float broodCarePerLevel = 0.1f;
    [SerializeField] private float defencePerLevel = 0.1f;
    [SerializeField] private float attackSpeedPerLevel = 0.1f;
    [SerializeField] private float identificationPerLevel = 0.1f;
    [SerializeField] private float maximumHealthPerLevel = 15f;
    [SerializeField] private float attackDamagePerLevel = 2f;

    [Header("Upkeep")]
    [SerializeField, Min(0f), Tooltip("Nectar this role consumes per wasp on each upkeep tick.")]
    private float upkeepNectarPerTick = 0.25f;
    [SerializeField, Min(0f), Tooltip("Prey this role consumes per wasp on each upkeep tick.")]
    private float upkeepPreyPerTick;

    [Header("Training Time")]
    [SerializeField, Min(0f), Tooltip("Seconds to train one wasp of this role at skill level 0.")]
    private float baseTrainingSeconds = 5f;
    [SerializeField, Min(0f), Tooltip("Extra seconds added per skill level. A better wasp takes longer to " +
                                      "raise, so investing in a role is a trade against how fast you can " +
                                      "field it.")]
    private float trainingSecondsPerLevel = 1.5f;

    [Header("Upgrade Cost Curve")]
    [SerializeField, Range(1f, 3f), Tooltip("1 = linear (cost x level). Higher values make the last levels much more expensive.")]
    private float upgradeCostExponent = 1.6f;

    [Header("Upgrade Preview")]
    [SerializeField, Tooltip("Stats listed on this skill's upgrade card. Leave empty to use the sensible defaults for this role.")]
    private List<WaspSkillStat> upgradeStats = new List<WaspSkillStat>();

    public WaspFunction Function => function;
    public Sprite RoleIcon => roleIcon;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? function.ToString() : displayName;
    public string Description => description;
    public string EffectSummary => effectSummary;
    public int MaximumLevel => Mathf.Max(1, maximumLevel);
    public WaspSkillCost TrainingCost => trainingCost;
    public WaspSkillCost HiveConstructionCost => hiveConstructionCost;

    public float UpkeepNectarPerTick => upkeepNectarPerTick;
    public float UpkeepPreyPerTick => upkeepPreyPerTick;
    public float BaseTrainingSeconds => baseTrainingSeconds;
    public float TrainingSecondsPerLevel => trainingSecondsPerLevel;

    /// <summary>How long one wasp of this role takes to train at the given skill level.</summary>
    public float GetTrainingSeconds(int level)
    {
        return Mathf.Max(0f, baseTrainingSeconds + trainingSecondsPerLevel * Mathf.Max(0, level));
    }

    /// <summary>
    /// Cost of the next level. The curve is exponential rather than linear so the final levels are
    /// a real investment; a flat "cost x level" made maxing out trivial once the economy matured.
    /// </summary>
    public WaspSkillCost GetUpgradeCost(int nextLevel)
    {
        int level = Mathf.Max(1, nextLevel);
        float scale = Mathf.Pow(level, Mathf.Max(1f, upgradeCostExponent));
        return new WaspSkillCost(
            upgradeCost.nectar * scale,
            upgradeCost.prey * scale,
            upgradeCost.fibre * scale,
            Mathf.RoundToInt(upgradeCost.skillPoints * scale));
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
            case WaspSkillStat.GatheringSpeed:
                return baseGatheringSpeed + gatheringSpeedPerLevel * clampedLevel;
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
            case WaspSkillStat.MaximumHealth:
                return baseMaximumHealth + maximumHealthPerLevel * clampedLevel;
            case WaspSkillStat.AttackDamage:
                return baseAttackDamage + attackDamagePerLevel * clampedLevel;
            default:
                return 1f;
        }
    }

    public float GetIncrementPerLevel(WaspSkillStat stat)
    {
        switch (stat)
        {
            case WaspSkillStat.ScoutingRange:
                return scoutingRangePerLevel;
            case WaspSkillStat.MovementSpeed:
                return movementSpeedPerLevel;
            case WaspSkillStat.GatheringMultiplier:
                return gatheringMultiplierPerLevel;
            case WaspSkillStat.GatheringSpeed:
                return gatheringSpeedPerLevel;
            case WaspSkillStat.BuildSpeed:
                return buildSpeedPerLevel;
            case WaspSkillStat.BroodCare:
                return broodCarePerLevel;
            case WaspSkillStat.Defence:
                return defencePerLevel;
            case WaspSkillStat.AttackSpeed:
                return attackSpeedPerLevel;
            case WaspSkillStat.Identification:
                return identificationPerLevel;
            case WaspSkillStat.MaximumHealth:
                return maximumHealthPerLevel;
            case WaspSkillStat.AttackDamage:
                return attackDamagePerLevel;
            default:
                return 0f;
        }
    }

    /// <summary>
    /// Stats shown on this skill's upgrade card. Falls back to role defaults when nothing is authored.
    /// </summary>
    public IReadOnlyList<WaspSkillStat> GetUpgradeStats()
    {
        if (upgradeStats != null && upgradeStats.Count > 0)
            return upgradeStats;

        return GetDefaultUpgradeStats(function);
    }

    /// <summary>
    /// Current and next-level values for every stat this skill highlights.
    /// Stats that do not actually change per level are skipped.
    /// </summary>
    public List<WaspSkillStatPreview> GetUpgradePreview(int currentLevel)
    {
        List<WaspSkillStatPreview> previews = new List<WaspSkillStatPreview>();
        int level = Mathf.Clamp(currentLevel, 0, MaximumLevel);

        foreach (WaspSkillStat stat in GetUpgradeStats())
        {
            float increment = GetIncrementPerLevel(stat);
            if (Mathf.Approximately(increment, 0f))
                continue;

            previews.Add(new WaspSkillStatPreview
            {
                stat = stat,
                currentValue = GetEffectiveValue(stat, level),
                nextValue = GetEffectiveValue(stat, level + 1),
                increment = increment
            });
        }

        return previews;
    }

    public static IReadOnlyList<WaspSkillStat> GetDefaultUpgradeStats(WaspFunction role)
    {
        switch (role)
        {
            case WaspFunction.Scout:
                return new[] { WaspSkillStat.ScoutingRange, WaspSkillStat.Identification, WaspSkillStat.MovementSpeed };
            case WaspFunction.Forager:
                return new[] { WaspSkillStat.GatheringMultiplier, WaspSkillStat.GatheringSpeed, WaspSkillStat.MovementSpeed };
            case WaspFunction.Builder:
                return new[] { WaspSkillStat.BuildSpeed, WaspSkillStat.GatheringMultiplier };
            case WaspFunction.BroodCaretaker:
                return new[] { WaspSkillStat.BroodCare };
            case WaspFunction.Guard:
                return new[] { WaspSkillStat.AttackDamage, WaspSkillStat.AttackSpeed, WaspSkillStat.MaximumHealth, WaspSkillStat.Defence };
            case WaspFunction.Containment:
                return new[] { WaspSkillStat.Identification, WaspSkillStat.AttackDamage, WaspSkillStat.Defence };
            default:
                return Array.Empty<WaspSkillStat>();
        }
    }

    public static string GetStatDisplayName(WaspSkillStat stat)
    {
        switch (stat)
        {
            case WaspSkillStat.ScoutingRange:
                return "Scouting Range";
            case WaspSkillStat.MovementSpeed:
                return "Movement Speed";
            case WaspSkillStat.GatheringMultiplier:
                return "Gathering Yield";
            case WaspSkillStat.GatheringSpeed:
                return "Gathering Speed";
            case WaspSkillStat.BuildSpeed:
                return "Build Speed";
            case WaspSkillStat.BroodCare:
                return "Brood Care";
            case WaspSkillStat.Defence:
                return "Defence";
            case WaspSkillStat.AttackSpeed:
                return "Attack Speed";
            case WaspSkillStat.Identification:
                return "Identification";
            case WaspSkillStat.MaximumHealth:
                return "Health";
            case WaspSkillStat.AttackDamage:
                return "Attack Damage";
            default:
                return stat.ToString();
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
