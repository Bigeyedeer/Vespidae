using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class C_HiveSkillsPanel : MonoBehaviour
{
    private static readonly WaspFunction[] cardFunctions =
    {
        WaspFunction.Scout,
        WaspFunction.Forager,
        WaspFunction.Builder,
        WaspFunction.BroodCaretaker,
        WaspFunction.Guard,
        WaspFunction.Containment
    };

    private HiveManagement hive;

    public void Refresh()
    {
        hive = HiveManagement.GetOrCreate();
        if (hive == null)
            return;

        for (int index = 0; index < cardFunctions.Length; index++)
        {
            WaspFunction function = cardFunctions[index];
            SB_Wasp_Skill definition = hive.GetSkillDefinition(function);
            if (definition == null)
                continue;

            int level = hive.GetSkillLevel(function);
            WaspSkillCost cost = definition.GetUpgradeCost(level + 1);
            SetText($"Skills_CardTitle_{index}", $"{definition.DisplayName}  Lv {level}");
            SetText($"Skills_CardDesc_{index}", definition.Description);
            SetText($"Skills_CardCost_{index}",
                level >= definition.MaximumLevel
                    ? "MAX LEVEL"
                    : $"Upgrade: {FormatCost(cost)}");

            GameObject card = FindSceneObject($"Skills_Card_{index}");
            Button button = card != null ? card.GetComponent<Button>() : null;
            if (button == null)
                continue;

            button.onClick.RemoveAllListeners();
            int capturedIndex = index;
            button.onClick.AddListener(() => Upgrade(capturedIndex));
            button.interactable = hive.CanUpgrade(function);
        }
    }

    private void Upgrade(int index)
    {
        if (hive == null || index < 0 || index >= cardFunctions.Length)
            return;

        hive.TryUpgrade(cardFunctions[index]);
        Refresh();
    }

    private string FormatCost(WaspSkillCost cost)
    {
        string result = string.Empty;
        if (cost.nectar > 0f)
            result += $"Nectar {cost.nectar:0} ";
        if (cost.prey > 0f)
            result += $"Prey {cost.prey:0} ";
        if (cost.fibre > 0f)
            result += $"Fibre {cost.fibre:0} ";
        if (cost.skillPoints > 0)
            result += $"Points {cost.skillPoints}";
        return result.Trim();
    }

    private void SetText(string objectName, string value)
    {
        GameObject target = FindSceneObject(objectName);
        if (target == null)
            return;

        TMP_Text tmp = target.GetComponent<TMP_Text>();
        if (tmp != null)
        {
            tmp.text = value;
            return;
        }

        Text legacy = target.GetComponent<Text>();
        if (legacy != null)
            legacy.text = value;
    }

    private GameObject FindSceneObject(string objectName)
    {
        GameObject[] objects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject sceneObject in objects)
        {
            if (sceneObject != null && sceneObject.scene.IsValid() && sceneObject.name == objectName)
                return sceneObject;
        }

        return null;
    }
}
