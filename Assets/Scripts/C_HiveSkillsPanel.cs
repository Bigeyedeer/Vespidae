using System;
using System.Collections.Generic;
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
            card.SetTitle(card.HasSeparateLevelText ? definition.DisplayName : $"{definition.DisplayName}  Lv {level}");
            card.SetDescription(card.HasSeparateEffectText ? definition.Description : $"{definition.Description}\n{definition.EffectSummary}");
            card.SetLevel($"Level {level}/{definition.MaximumLevel}");
            card.SetEffect(definition.EffectSummary);
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
