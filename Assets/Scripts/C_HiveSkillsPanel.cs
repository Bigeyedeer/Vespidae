using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class HiveSkillCardBinding
{
    [SerializeField] private WaspFunction function;
    [SerializeField] private GameObject cardRoot;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private TMP_Text effectText;
    [SerializeField] private Text legacyTitleText;
    [SerializeField] private Text legacyDescriptionText;
    [SerializeField] private Text legacyLevelText;
    [SerializeField] private Text legacyCostText;
    [SerializeField] private Text legacyEffectText;
    [SerializeField] private Button upgradeButton;

    public WaspFunction Function => function;
    public GameObject CardRoot => cardRoot;
    public TMP_Text TitleText => titleText;
    public TMP_Text DescriptionText => descriptionText;
    public TMP_Text LevelText => levelText;
    public TMP_Text CostText => costText;
    public TMP_Text EffectText => effectText;
    public Button UpgradeButton => upgradeButton;
    public bool HasSeparateLevelText => levelText != null || legacyLevelText != null;
    public bool HasSeparateEffectText => effectText != null || legacyEffectText != null;

    public void SetTitle(string value)
    {
        if (titleText != null) titleText.text = value;
        if (legacyTitleText != null) legacyTitleText.text = value;
    }

    public void SetDescription(string value)
    {
        if (descriptionText != null) descriptionText.text = value;
        if (legacyDescriptionText != null) legacyDescriptionText.text = value;
    }

    public void SetLevel(string value)
    {
        if (levelText != null) levelText.text = value;
        if (legacyLevelText != null) legacyLevelText.text = value;
    }

    public void SetCost(string value)
    {
        if (costText != null) costText.text = value;
        if (legacyCostText != null) legacyCostText.text = value;
    }

    public void SetEffect(string value)
    {
        if (effectText != null) effectText.text = value;
        if (legacyEffectText != null) legacyEffectText.text = value;
    }
}

public class C_HiveSkillsPanel : MonoBehaviour
{
    [SerializeField] private List<HiveSkillCardBinding> cards = new List<HiveSkillCardBinding>();

    private HiveManagement hive;

    private void OnEnable()
    {
        Subscribe();
        Refresh();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    public void Refresh()
    {
        hive = HiveManagement.GetOrCreate();
        if (hive == null)
            return;

        foreach (HiveSkillCardBinding card in cards)
        {
            if (card == null)
                continue;

            SB_Wasp_Skill definition = hive.GetSkillDefinition(card.Function);
            if (definition == null)
            {
                if (card.CardRoot != null)
                    card.CardRoot.SetActive(false);
                continue;
            }

            if (card.CardRoot != null)
                card.CardRoot.SetActive(true);
            int level = hive.GetSkillLevel(card.Function);
            WaspSkillCost cost = definition.GetUpgradeCost(level + 1);
            string effect = BuildEffectText(definition, level);
            card.SetTitle(card.HasSeparateLevelText ? definition.DisplayName : $"{definition.DisplayName}  Lv {level}");
            card.SetDescription(card.HasSeparateEffectText ? definition.Description : $"{definition.Description}\n{effect}");
            card.SetLevel($"Level {level}/{definition.MaximumLevel}");
            card.SetEffect(effect);
            card.SetCost(level >= definition.MaximumLevel ? "MAX LEVEL" : FormatCost(cost));

            if (card.UpgradeButton == null)
                continue;

            card.UpgradeButton.onClick.RemoveAllListeners();
            WaspFunction capturedFunction = card.Function;
            card.UpgradeButton.onClick.AddListener(() => Upgrade(capturedFunction));
            card.UpgradeButton.interactable = hive.CanUpgrade(card.Function);
        }
    }

    private void Upgrade(WaspFunction function)
    {
        hive?.TryUpgrade(function);
        Refresh();
    }

    private void Subscribe()
    {
        hive = HiveManagement.GetOrCreate();
        if (hive != null)
        {
            hive.SkillsChanged -= Refresh;
            hive.SkillsChanged += Refresh;
        }

        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.ResourcesChanged -= Refresh;
            ResourceManager.Instance.ResourcesChanged += Refresh;
        }
    }

    private void Unsubscribe()
    {
        if (HiveManagement.Instance != null)
            HiveManagement.Instance.SkillsChanged -= Refresh;
        if (ResourceManager.Instance != null)
            ResourceManager.Instance.ResourcesChanged -= Refresh;
    }

    /// <summary>
    /// Effect summary plus a line per highlighted stat showing what this upgrade actually changes,
    /// e.g. "Attack Speed  1.25 -> 1.5  (+0.25)". At max level the current values are shown instead.
    /// </summary>
    private static string BuildEffectText(SB_Wasp_Skill definition, int level)
    {
        List<string> lines = new List<string>();
        if (!string.IsNullOrWhiteSpace(definition.EffectSummary))
            lines.Add(definition.EffectSummary);

        bool atMaximum = level >= definition.MaximumLevel;
        foreach (WaspSkillStatPreview preview in definition.GetUpgradePreview(level))
        {
            string statName = SB_Wasp_Skill.GetStatDisplayName(preview.stat);
            lines.Add(atMaximum
                ? $"{statName}  {FormatValue(preview.currentValue)}"
                : $"{statName}  {FormatValue(preview.currentValue)} -> {FormatValue(preview.nextValue)}  (+{FormatValue(preview.increment)})");
        }

        return string.Join("\n", lines);
    }

    private static string FormatValue(float value)
    {
        // Invariant culture so stat values always read as "1.25", never "1,25" on locales
        // that use a comma decimal separator.
        return Mathf.Approximately(value, Mathf.Round(value))
            ? value.ToString("0", CultureInfo.InvariantCulture)
            : value.ToString("0.##", CultureInfo.InvariantCulture);
    }

    private static string FormatCost(WaspSkillCost cost)
    {
        List<string> parts = new List<string>();
        if (cost.nectar > 0f)
            parts.Add($"Nectar {cost.nectar:0}");
        if (cost.prey > 0f)
            parts.Add($"Prey {cost.prey:0}");
        if (cost.fibre > 0f)
            parts.Add($"Fibre {cost.fibre:0}");
        if (cost.skillPoints > 0)
            parts.Add($"Points {cost.skillPoints}");
        return string.Join("  ", parts);
    }
}
