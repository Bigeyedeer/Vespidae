using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;

public class C_MainWorldOverlayNavigation : MonoBehaviour
{
    public static C_MainWorldOverlayNavigation Instance { get; private set; }

    [SerializeField] private GameObject waspInfoPanel;
    [SerializeField] private GameObject skillsPanel;
    [SerializeField] private GameObject hiveTrainingPanel;
    [SerializeField] private Key skillsKey = Key.K;

    private C_HiveSkillsPanel skillsController;
    private C_Friendly_Hive_Orc selectedHive;
    private string trainingFeedback;

    private void Awake()
    {
        Instance = this;
        BindSceneReferences();
        CloseAllPanels();
    }

    private void OnDestroy()
    {
        Unsubscribe();
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current[skillsKey].wasPressedThisFrame)
        {
            if (skillsPanel != null && skillsPanel.activeSelf)
            {
                CloseSkills();
            }
            else
            {
                OpenSkills();
            }
        }
    }

    public void BindSceneReferences()
    {
        if (waspInfoPanel == null)
        {
            waspInfoPanel = FindChild("WaspInfoPanel");
        }

        if (skillsPanel == null)
        {
            skillsPanel = FindChild("SkillsPanel");
        }

        if (hiveTrainingPanel == null)
        {
            hiveTrainingPanel = FindChild("Hive Training Panel");
        }

        BindButton("Action_Codex", OpenWaspInfo);
        BindButton("Action_Return", CloseAllPanels);
        BindButton("WaspInfo_Return", CloseWaspInfo);
        BindButton("WaspInfo_Close", CloseWaspInfo);
        BindButton("Skills_Return", CloseSkills);
        BindButton("Skills_Close", CloseSkills);
        BindButton("HiveTrain_Scout", TrainScout);
        BindButton("HiveTrain_Worker", TrainWorker);
        BindButton("HiveTrain_Attacker", TrainAttacker);
        BindButton("HiveTraining_Hide", HideHiveTraining);
    }

    public void OpenWaspInfo()
    {
        if (skillsPanel != null)
        {
            skillsPanel.SetActive(false);
        }

        if (waspInfoPanel != null)
        {
            waspInfoPanel.SetActive(true);
        }

        HideHiveTraining();
    }

    public void OpenSkills()
    {
        if (waspInfoPanel != null)
        {
            waspInfoPanel.SetActive(false);
        }

        if (skillsPanel != null)
        {
            skillsPanel.SetActive(true);
            skillsController = skillsPanel.GetComponent<C_HiveSkillsPanel>();
            if (skillsController == null)
                skillsController = skillsPanel.AddComponent<C_HiveSkillsPanel>();
            skillsController.Refresh();
        }
    }

    public void CloseWaspInfo()
    {
        if (waspInfoPanel != null)
        {
            waspInfoPanel.SetActive(false);
        }

        HideHiveTraining();

    }

    public void CloseSkills()
    {
        if (skillsPanel != null)
        {
            skillsPanel.SetActive(false);
        }
    }

    public void CloseAllPanels()
    {
        CloseWaspInfo();
        CloseSkills();
        HideHiveTraining();
    }

    public void OpenHiveTraining(C_Friendly_Hive_Orc hive)
    {
        if (hive == null)
            return;

        selectedHive = hive;
        trainingFeedback = string.Empty;
        CloseWaspInfo();
        CloseSkills();
        BindSceneReferences();
        Subscribe();

        if (hiveTrainingPanel != null)
            hiveTrainingPanel.SetActive(true);

        RefreshHiveTraining();
    }

    public void HideHiveTraining()
    {
        if (hiveTrainingPanel != null)
            hiveTrainingPanel.SetActive(false);
    }

    public void RefreshHiveTraining()
    {
        HiveManagement hive = HiveManagement.GetOrCreate();
        ResourceManager resources = ResourceManager.Instance;
        if (hive == null)
            return;

        SetText("HiveTraining_Title", "Native Hive");
        SetText(
            "HiveTraining_Resources",
            resources == null
                ? "Nectar 0   Prey 0   Fibre 0"
                : $"Nectar {resources.Nectar:0}   Prey {resources.Prey:0}   Fibre {resources.Fibre:0}");

        SetRoleText(hive, WaspFunction.Scout, "Scout", "HiveTraining_ScoutInfo");
        SetRoleText(hive, WaspFunction.Forager, "Worker", "HiveTraining_WorkerInfo");
        SetRoleText(hive, WaspFunction.Guard, "Attacker", "HiveTraining_AttackerInfo");
        SetTrainingButton(hive, WaspFunction.Scout, "HiveTrain_Scout", "Train Scout");
        SetTrainingButton(hive, WaspFunction.Forager, "HiveTrain_Worker", "Train Worker");
        SetTrainingButton(hive, WaspFunction.Guard, "HiveTrain_Attacker", "Train Attacker");
        SetText("HiveTraining_Feedback", trainingFeedback);
    }

    private void TrainScout()
    {
        Train(WaspFunction.Scout);
    }

    private void TrainWorker()
    {
        Train(WaspFunction.Forager);
    }

    private void TrainAttacker()
    {
        Train(WaspFunction.Guard);
    }

    private void Train(WaspFunction function)
    {
        HiveManagement hive = HiveManagement.GetOrCreate();
        bool trained = hive != null && hive.TryTrainWasp(selectedHive, function);
        trainingFeedback = trained
            ? $"{GetDisplayRole(function)} trained."
            : $"Unable to train {GetDisplayRole(function)}.";
        RefreshHiveTraining();
    }

    private void SetRoleText(
        HiveManagement hive,
        WaspFunction function,
        string roleName,
        string objectName)
    {
        int total = hive.GetTotalWaspCount(function);
        int available = hive.GetAvailableWaspCount(function);
        SetText(objectName, $"{roleName}: {total} total   {available} available");
    }

    private void SetTrainingButton(
        HiveManagement hive,
        WaspFunction function,
        string objectName,
        string label)
    {
        GameObject target = FindChild(objectName);
        if (target == null)
            return;

        SB_Wasp_Skill definition = hive.GetSkillDefinition(function);
        WaspSkillCost cost = definition != null ? definition.TrainingCost : default;
        TMP_Text text = target.GetComponentInChildren<TMP_Text>(true);
        if (text != null)
            text.text = $"{label}\n{FormatCost(cost)}";

        Button button = target.GetComponent<Button>();
        if (button != null)
            button.interactable = selectedHive != null && hive.CanTrainWasp(selectedHive, function);
    }

    private string FormatCost(WaspSkillCost cost)
    {
        string result = string.Empty;
        if (cost.nectar > 0f)
            result += $"N {cost.nectar:0}  ";
        if (cost.prey > 0f)
            result += $"P {cost.prey:0}  ";
        if (cost.fibre > 0f)
            result += $"F {cost.fibre:0}";
        return result.Trim();
    }

    private string GetDisplayRole(WaspFunction function)
    {
        if (function == WaspFunction.Forager)
            return "Worker";
        if (function == WaspFunction.Guard)
            return "Attacker";
        return function.ToString();
    }

    private void SetText(string objectName, string value)
    {
        GameObject target = FindChild(objectName);
        TMP_Text text = target != null ? target.GetComponent<TMP_Text>() : null;
        if (text != null)
            text.text = value;
    }

    private void Subscribe()
    {
        HiveManagement hive = HiveManagement.GetOrCreate();
        if (hive != null)
        {
            hive.WorkforceChanged -= RefreshHiveTraining;
            hive.WorkforceChanged += RefreshHiveTraining;
        }

        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.ResourcesChanged -= RefreshHiveTraining;
            ResourceManager.Instance.ResourcesChanged += RefreshHiveTraining;
        }
    }

    private void Unsubscribe()
    {
        if (HiveManagement.Instance != null)
            HiveManagement.Instance.WorkforceChanged -= RefreshHiveTraining;

        if (ResourceManager.Instance != null)
            ResourceManager.Instance.ResourcesChanged -= RefreshHiveTraining;
    }

    private GameObject FindChild(string childName)
    {
        Transform[] children = GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
        {
            if (child.name == childName)
                return child.gameObject;
        }

        GameObject[] sceneObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject sceneObject in sceneObjects)
        {
            if (sceneObject != null && sceneObject.scene.IsValid() && sceneObject.name == childName)
                return sceneObject;
        }

        return null;
    }

    private void BindButton(string objectName, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = FindChild(objectName);
        if (buttonObject == null)
        {
            return;
        }

        Button button = buttonObject.GetComponent<Button>();
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }
}
