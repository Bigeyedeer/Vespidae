using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;

public class C_MainWorldOverlayNavigation : MonoBehaviour
{
    public static C_MainWorldOverlayNavigation Instance { get; private set; }
    public static bool IsPaused => Instance != null && Instance.isPaused;

    [SerializeField] private GameObject waspInfoPanel;
    [SerializeField] private GameObject skillsPanel;
    [SerializeField] private GameObject hiveTrainingPanel;
    [SerializeField] private Key skillsKey = Key.K;

    private C_HiveSkillsPanel skillsController;
    private C_Friendly_Hive_Orc selectedHive;
    private string trainingFeedback;
    private C_MainWorldCameraFocus cameraFocus;
    private C_MainWorldNavigation mainWorldNavigation;
    private CameraCursorMovement mapCameraMovement;
    private GameObject pauseMenu;
    private GameObject pauseMainButtons;
    private GameObject pauseOptions;
    private Slider scrollSpeedSlider;
    private TMP_Text scrollSpeedValueText;
    private bool isPaused;
    private bool mapMovementWasEnabled;

    private const string ScrollSpeedPreferenceKey = "Vespidae.ScrollWheelZoomSpeed";
    private const float DefaultScrollWheelZoomSpeed = 0.02f;

    private void Awake()
    {
        Instance = this;
        BindSceneReferences();
        CreatePauseMenu();
        CloseAllPanels();
    }

    private void OnDestroy()
    {
        ResumeGame();
        Unsubscribe();
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (isPaused)
                ResumeGame();
            else
                PauseGame();

            return;
        }

        if (isPaused)
            return;

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

        if (cameraFocus == null)
            cameraFocus = FindFirstObjectByType<C_MainWorldCameraFocus>();

        if (mainWorldNavigation == null)
            mainWorldNavigation = FindFirstObjectByType<C_MainWorldNavigation>();

        if (mapCameraMovement == null && Camera.main != null)
            mapCameraMovement = Camera.main.GetComponent<CameraCursorMovement>();

        BindButton("Action_Codex", OpenWaspInfo);
        BindButton("Action_Return", ReturnToPreviousView);
        BindButton("WaspInfo_Return", CloseWaspInfo);
        BindButton("WaspInfo_Close", CloseWaspInfo);
        BindButton("Skills_Return", CloseSkills);
        BindButton("Skills_Close", CloseSkills);
        BindButton("HiveTrain_Scout", TrainScout);
        BindButton("HiveTrain_Forager", TrainForager);
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

    public void ReturnToPreviousView()
    {
        cameraFocus?.ReturnToPreviousView();
    }

    public void PauseGame()
    {
        if (isPaused)
            return;

        BindSceneReferences();
        CreatePauseMenu();
        if (pauseMenu == null)
            return;

        isPaused = true;
        mapMovementWasEnabled = mapCameraMovement != null && mapCameraMovement.MovementEnabled;
        mapCameraMovement?.SetMovementEnabled(false);
        Time.timeScale = 0f;
        ShowPauseMain();
        pauseMenu.SetActive(true);
    }

    public void ResumeGame()
    {
        if (!isPaused)
            return;

        Time.timeScale = 1f;
        isPaused = false;
        if (pauseMenu != null)
            pauseMenu.SetActive(false);

        if (mapCameraMovement != null)
            mapCameraMovement.SetMovementEnabled(mapMovementWasEnabled);
    }

    public void OpenPauseOptions()
    {
        if (pauseMainButtons != null)
            pauseMainButtons.SetActive(false);
        if (pauseOptions != null)
            pauseOptions.SetActive(true);
    }

    public void ShowPauseMain()
    {
        if (pauseMainButtons != null)
            pauseMainButtons.SetActive(true);
        if (pauseOptions != null)
            pauseOptions.SetActive(false);
    }

    public void SetScrollWheelZoomSpeed(float value)
    {
        float speed = Mathf.Clamp(value, 0.005f, 0.08f);
        if (mapCameraMovement != null)
            mapCameraMovement.ScrollWheelZoomSpeed = speed;
        if (cameraFocus != null)
            cameraFocus.ScrollWheelZoomSpeed = speed;
        if (scrollSpeedSlider != null && !Mathf.Approximately(scrollSpeedSlider.value, speed))
            scrollSpeedSlider.SetValueWithoutNotify(speed);
        if (scrollSpeedValueText != null)
            scrollSpeedValueText.text = $"{speed:0.000}";
        PlayerPrefs.SetFloat(ScrollSpeedPreferenceKey, speed);
        PlayerPrefs.Save();
    }

    public void QuitToMenu()
    {
        ResumeGame();
        if (mainWorldNavigation != null)
            mainWorldNavigation.ReturnToMenu();
    }

    private void CreatePauseMenu()
    {
        if (pauseMenu != null)
            return;

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
            canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
            return;

        pauseMenu = CreateUiObject(
            "PauseMenu",
            canvas.transform,
            Vector2.zero,
            Vector2.one,
            Vector2.zero,
            Vector2.zero);
        Image backdrop = pauseMenu.AddComponent<Image>();
        backdrop.color = new Color(0.015f, 0.035f, 0.04f, 0.82f);

        GameObject card = CreateUiObject(
            "PauseMenuCard",
            pauseMenu.transform,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(460f, 420f));
        Image cardImage = card.AddComponent<Image>();
        cardImage.color = new Color(0.07f, 0.12f, 0.12f, 0.98f);

        CreateText(
            "PauseMenuTitle",
            card.transform,
            "PAUSED",
            new Vector2(0f, 154f),
            new Vector2(380f, 46f),
            30f);

        pauseMainButtons = CreateUiObject(
            "PauseMenuMainButtons",
            card.transform,
            Vector2.zero,
            Vector2.one,
            Vector2.zero,
            Vector2.zero);
        CreateButton(pauseMainButtons.transform, "PauseResume", "RESUME", 60f, ResumeGame);
        CreateButton(pauseMainButtons.transform, "PauseOptions", "OPTIONS", -12f, OpenPauseOptions);
        CreateButton(pauseMainButtons.transform, "PauseQuit", "QUIT TO MENU", -84f, QuitToMenu);

        pauseOptions = CreateUiObject(
            "PauseMenuOptions",
            card.transform,
            Vector2.zero,
            Vector2.one,
            Vector2.zero,
            Vector2.zero);
        CreateText(
            "PauseOptionsTitle",
            pauseOptions.transform,
            "OPTIONS",
            new Vector2(0f, 76f),
            new Vector2(360f, 38f),
            22f);
        CreateText(
            "PauseScrollSpeedLabel",
            pauseOptions.transform,
            "Scroll wheel zoom speed",
            new Vector2(0f, 22f),
            new Vector2(360f, 30f),
            18f);
        scrollSpeedValueText = CreateText(
            "PauseScrollSpeedValue",
            pauseOptions.transform,
            string.Empty,
            new Vector2(0f, -12f),
            new Vector2(180f, 24f),
            16f);
        scrollSpeedSlider = CreateSlider(pauseOptions.transform, new Vector2(0f, -48f));
        CreateButton(pauseOptions.transform, "PauseOptionsBack", "BACK", -118f, ShowPauseMain);

        float speed = PlayerPrefs.GetFloat(
            ScrollSpeedPreferenceKey,
            DefaultScrollWheelZoomSpeed);
        SetScrollWheelZoomSpeed(speed);
        pauseMenu.SetActive(false);
    }

    private GameObject CreateUiObject(
        string objectName,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 anchoredPosition,
        Vector2 size)
    {
        GameObject result = new GameObject(objectName, typeof(RectTransform));
        RectTransform rect = result.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        return result;
    }

    private TMP_Text CreateText(
        string objectName,
        Transform parent,
        string value,
        Vector2 position,
        Vector2 size,
        float fontSize)
    {
        GameObject result = CreateUiObject(
            objectName,
            parent,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            position,
            size);
        TextMeshProUGUI text = result.AddComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(0.92f, 0.96f, 0.92f, 1f);
        return text;
    }

    private Button CreateButton(
        Transform parent,
        string objectName,
        string label,
        float y,
        UnityEngine.Events.UnityAction action)
    {
        GameObject result = CreateUiObject(
            objectName,
            parent,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, y),
            new Vector2(320f, 54f));
        Image image = result.AddComponent<Image>();
        image.color = new Color(0.16f, 0.3f, 0.25f, 1f);
        Button button = result.AddComponent<Button>();
        button.targetGraphic = image;
        CreateText("Label", result.transform, label, Vector2.zero, new Vector2(290f, 40f), 18f);
        button.onClick.AddListener(action);
        return button;
    }

    private Slider CreateSlider(Transform parent, Vector2 position)
    {
        GameObject result = CreateUiObject(
            "PauseScrollSpeedSlider",
            parent,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            position,
            new Vector2(320f, 24f));
        Image background = result.AddComponent<Image>();
        background.color = new Color(0.02f, 0.06f, 0.06f, 1f);
        Slider slider = result.AddComponent<Slider>();
        slider.minValue = 0.005f;
        slider.maxValue = 0.08f;
        slider.wholeNumbers = false;

        GameObject fillArea = CreateUiObject(
            "Fill Area",
            result.transform,
            Vector2.zero,
            Vector2.one,
            Vector2.zero,
            new Vector2(-30f, -6f));
        GameObject fill = CreateUiObject(
            "Fill",
            fillArea.transform,
            Vector2.zero,
            Vector2.one,
            Vector2.zero,
            Vector2.zero);
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = new Color(0.25f, 0.67f, 0.42f, 1f);

        GameObject handle = CreateUiObject(
            "Handle",
            result.transform,
            new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f),
            Vector2.zero,
            new Vector2(18f, 30f));
        Image handleImage = handle.AddComponent<Image>();
        handleImage.color = new Color(0.9f, 0.95f, 0.9f, 1f);

        slider.fillRect = fill.GetComponent<RectTransform>();
        slider.handleRect = handle.GetComponent<RectTransform>();
        slider.targetGraphic = handleImage;
        slider.onValueChanged.AddListener(SetScrollWheelZoomSpeed);
        return slider;
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
        SetRoleText(hive, WaspFunction.Forager, "Forager", "HiveTraining_ForagerInfo");
        SetRoleText(hive, WaspFunction.Guard, "Attacker", "HiveTraining_AttackerInfo");
        SetTrainingButton(hive, WaspFunction.Scout, "HiveTrain_Scout", "Train Scout");
        SetTrainingButton(hive, WaspFunction.Forager, "HiveTrain_Forager", "Train Forager");
        SetTrainingButton(hive, WaspFunction.Guard, "HiveTrain_Attacker", "Train Attacker");
        SetText("HiveTraining_Feedback", trainingFeedback);
    }

    private void TrainScout()
    {
        Train(WaspFunction.Scout);
    }

    private void TrainForager()
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
