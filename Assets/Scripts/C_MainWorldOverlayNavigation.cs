using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;
using System.Linq;

public class C_MainWorldOverlayNavigation : MonoBehaviour
{
    public static C_MainWorldOverlayNavigation Instance { get; private set; }
    public static bool IsPaused => Instance != null && Instance.isPaused;
    public bool BlocksWorldInput => isPaused ||
                                    IsActive(skillsPanel) ||
                                    IsActive(waspInfoPanel) ||
                                    IsActive(hiveTrainingPanel) ||
                                    IsActive(builderHivePanel) ||
                                    IsActive(friendlyWaspPanel);

    [SerializeField] private GameObject waspInfoPanel;
    [SerializeField] private GameObject skillsPanel;
    [SerializeField] private GameObject hiveTrainingPanel;
    [SerializeField, Tooltip("Optional. Assign your own text element to show the control list in the options panel; one is created automatically when this is empty.")]
    private TMP_Text keybindsText;

    /// <summary>
    /// The control list shown in the pause options panel. Kept in one place so both the procedural
    /// and prefab-based pause menus show exactly the same thing.
    /// </summary>
    private const string KeybindSummary =
        "<b>CONTROLS</b>\n" +
        "Left Click  -  Select hex / open panel\n" +
        "Shift + Left Click  -  Add/remove a wasp\n" +
        "Left Drag  -  Box-select wasps\n" +
        "Shift + Drag  -  Add to selection\n" +
        "Right Click  -  Send wasps to that hex\n" +
        "Double Right Click  -  Clear selection\n" +
        "1 - 5  -  Select control group\n" +
        "Ctrl + 1 - 5  -  Assign control group\n" +
        "Middle Drag  -  Pan camera\n" +
        "Scroll Wheel  -  Zoom\n" +
        "H  -  Toggle map-only view\n" +
        "Esc  -  Pause / resume";
    [SerializeField] private GameObject pauseMenuPrefab;
    [SerializeField, Tooltip("Herbert Squarish Panel prefab, used as the background for panels this " +
                             "script builds in code. Must be assigned - it cannot be found by path in a build.")]
    private GameObject herbertPanelPrefab;
    [SerializeField, Tooltip("Herbert Button Variant prefab, used to skin buttons this script builds " +
                             "in code so they match the authored ones.")]
    private GameObject herbertButtonPrefab;
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
    private GameObject builderHivePanel;
    private TMP_Text builderHiveTitle;
    private TMP_Text builderHiveDetails;
    private TMP_Text builderHiveFeedback;
    private Button builderHiveSpawnButton;
    private WaspControl selectedBuilder;
    private GameObject friendlyWaspPanel;
    private TMP_Text friendlyWaspTitle;
    private TMP_Text friendlyWaspDetails;
    private TMP_Text friendlyWaspFeedback;
    private Button friendlyWaspReturnButton;
    private Button friendlyWaspBuilderButton;
    private WaspControl selectedFriendlyWasp;
    private Slider scrollSpeedSlider;
    private TMP_Text scrollSpeedValueText;
    private TMP_Text pauseTitleText;
    private bool isPaused;
    private bool mapMovementWasEnabled;

    private const string ScrollSpeedPreferenceKey = "Vespidae.ScrollWheelZoomSpeed";
    private const float DefaultScrollWheelZoomSpeed = 0.02f;

    private static bool IsActive(GameObject target)
    {
        return target != null && target.activeInHierarchy;
    }

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
        BindButton("HiveTrain_Builder", TrainBuilder);
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
        HideBuilderHivePanel();
        HideFriendlyWaspActions();
    }

    public void OpenFriendlyWaspActions(WaspControl wasp)
    {
        if (wasp == null)
            return;

        selectedFriendlyWasp = wasp;
        CloseWaspInfo();
        CloseSkills();
        HideHiveTraining();
        HideBuilderHivePanel();
        CreateFriendlyWaspPanel();

        if (friendlyWaspPanel != null)
            friendlyWaspPanel.SetActive(true);

        RefreshFriendlyWaspActions();
    }

    /// <summary>
    /// Puts the shared Herbert panel art behind a card that was built in code, so it matches the rest
    /// of the HUD. Silently leaves the plain fill in place if the prefab is missing, rather than
    /// dropping the card's background entirely.
    /// </summary>
    private void ApplyHerbertPanelShell(GameObject card, Image flatFill)
    {
        // Serialized rather than loaded by path. AssetDatabase does not exist in a build and the
        // prefab lives in the art group's folder rather than Resources, so a path lookup would
        // silently leave this card flat once built - working in the editor and nowhere else.
        GameObject prefab = herbertPanelPrefab;
        if (prefab == null)
            return;

        GameObject shell = Instantiate(prefab, card.transform);
        shell.name = "HerbertSquarishPanel";
        shell.transform.SetAsFirstSibling();

        RectTransform shellRect = shell.GetComponent<RectTransform>();
        if (shellRect != null)
        {
            shellRect.anchorMin = Vector2.zero;
            shellRect.anchorMax = Vector2.one;
            shellRect.offsetMin = Vector2.zero;
            shellRect.offsetMax = Vector2.zero;
            shellRect.localScale = Vector3.one;
        }

        // The art must never eat clicks meant for the card's own buttons.
        foreach (Graphic graphic in shell.GetComponentsInChildren<Graphic>(true))
            graphic.raycastTarget = false;

        SetLayerRecursively(shell, card.layer);

        if (flatFill != null)
        {
            Color color = flatFill.color;
            color.a = 0f;
            flatFill.color = color;
            flatFill.raycastTarget = true;
        }
    }

    /// <summary>
    /// Puts the shared Herbert button art on a button built in code, mirroring what
    /// VespidaeHerbertHudStyleSetup does to the authored ones. The original label is carried across
    /// and the flat fill made transparent, so the button keeps its behaviour and only changes skin.
    /// </summary>
    private void ApplyHerbertButtonSkin(GameObject buttonObject)
    {
        if (herbertButtonPrefab == null || buttonObject == null)
            return;

        Transform existing = buttonObject.transform.Find("HerbertButtonVisual");
        if (existing != null)
            Destroy(existing.gameObject);

        string label = string.Empty;
        TMP_Text originalLabel = buttonObject.GetComponentInChildren<TMP_Text>(true);
        if (originalLabel != null)
            label = originalLabel.text;

        GameObject visual = Instantiate(herbertButtonPrefab, buttonObject.transform);
        visual.name = "HerbertButtonVisual";
        visual.transform.SetAsFirstSibling();

        RectTransform rect = visual.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        // The skin is decoration; the real Button underneath keeps the click.
        foreach (Button nested in visual.GetComponentsInChildren<Button>(true))
            nested.enabled = false;
        foreach (Graphic graphic in visual.GetComponentsInChildren<Graphic>(true))
            graphic.raycastTarget = false;

        TMP_Text skinLabel = visual.GetComponentInChildren<TMP_Text>(true);
        if (skinLabel != null)
        {
            skinLabel.text = label;
            skinLabel.alignment = TextAlignmentOptions.Center;
            skinLabel.enableAutoSizing = true;
            skinLabel.fontSizeMin = 14f;
            skinLabel.fontSizeMax = 24f;
        }

        // Hide the original label and fill so only the skin shows.
        if (originalLabel != null && !originalLabel.transform.IsChildOf(visual.transform))
            originalLabel.enabled = false;

        Image fill = buttonObject.GetComponent<Image>();
        if (fill != null)
        {
            Color color = fill.color;
            color.a = 0f;
            fill.color = color;
            fill.raycastTarget = true;
        }

        SetLayerRecursively(visual, buttonObject.layer);
    }

    private static void SetLayerRecursively(GameObject target, int layer)
    {
        target.layer = layer;
        foreach (Transform child in target.transform)
            SetLayerRecursively(child.gameObject, layer);
    }

    public void HideFriendlyWaspActions()
    {
        if (friendlyWaspPanel != null)
            friendlyWaspPanel.SetActive(false);

        selectedFriendlyWasp = null;
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
            WaspInfoPanel panel = waspInfoPanel.GetComponent<WaspInfoPanel>();
            if (panel != null)
                panel.Close();

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
        HideBuilderHivePanel();
        HideFriendlyWaspActions();
    }

    public void ReturnToPreviousView()
    {
        HideFriendlyWaspActions();
        mainWorldNavigation?.CloseHexOptions();
        cameraFocus?.ReturnToPreviousView();
    }

    private void CreateFriendlyWaspPanel()
    {
        if (friendlyWaspPanel != null)
            return;

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
            canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
            return;

        friendlyWaspPanel = CreateUiObject(
            "FriendlyWaspActionsPanel",
            canvas.transform,
            Vector2.zero,
            Vector2.one,
            Vector2.zero,
            Vector2.zero);

        GameObject card = CreateUiObject(
            "FriendlyWaspActionsCard",
            friendlyWaspPanel.transform,
            new Vector2(0.78f, 0.5f),
            new Vector2(0.78f, 0.5f),
            Vector2.zero,
            new Vector2(470f, 520f));
        Image cardImage = card.AddComponent<Image>();
        cardImage.color = new Color(0.035f, 0.085f, 0.075f, 0.96f);

        // This card is built in code, so the editor styling tool cannot reach it. Drop the same
        // Herbert panel shell in behind the content and make the flat fill transparent, matching
        // what VespidaeHerbertHudStyleSetup does to the authored panels.
        ApplyHerbertPanelShell(card, cardImage);

        friendlyWaspTitle = CreateText(
            "FriendlyWaspTitle",
            card.transform,
            "FRIENDLY WASP",
            new Vector2(0f, 205f),
            new Vector2(400f, 52f),
            27f);
        friendlyWaspDetails = CreateText(
            "FriendlyWaspDetails",
            card.transform,
            string.Empty,
            new Vector2(0f, 116f),
            new Vector2(390f, 112f),
            18f);
        friendlyWaspDetails.alignment = TextAlignmentOptions.TopLeft;
        friendlyWaspFeedback = CreateText(
            "FriendlyWaspFeedback",
            card.transform,
            string.Empty,
            new Vector2(0f, 43f),
            new Vector2(390f, 34f),
            15f);

        CreateButton(
            card.transform,
            "FriendlyWaspInformation",
            "VIEW INFORMATION",
            -15f,
            ShowFriendlyWaspInformation);
        friendlyWaspReturnButton = CreateButton(
            card.transform,
            "FriendlyWaspReturnToBase",
            "RETURN TO BASE",
            -82f,
            SendSelectedFriendlyWaspHome);
        friendlyWaspBuilderButton = CreateButton(
            card.transform,
            "FriendlyWaspBuildHive",
            "BUILD HIVE",
            -149f,
            OpenSelectedBuilderHivePanel);
        CreateButton(
            card.transform,
            "FriendlyWaspHide",
            "HIDE",
            -216f,
            HideFriendlyWaspActions);

        friendlyWaspPanel.SetActive(false);
    }

    private void RefreshFriendlyWaspActions()
    {
        if (selectedFriendlyWasp == null)
        {
            HideFriendlyWaspActions();
            return;
        }

        WaspInfo info = selectedFriendlyWasp.WaspInfo;
        string commonName = info != null && !string.IsNullOrWhiteSpace(info.CommonName)
            ? info.CommonName
            : "Friendly Wasp";
        string scientificName = info != null ? info.ScientificName : string.Empty;
        string role = GetDisplayRole(selectedFriendlyWasp.AssignedFunction);
        string location = selectedFriendlyWasp.StationedHex != null
            ? selectedFriendlyWasp.StationedHex.HexName
            : selectedFriendlyWasp.WorkforceState == WaspWorkforceState.Idle
                ? "Home hive"
                : "Travelling";

        if (friendlyWaspTitle != null)
            friendlyWaspTitle.text = commonName.ToUpperInvariant();
        if (friendlyWaspDetails != null)
        {
            friendlyWaspDetails.text =
                $"{scientificName}\n\nRole: {role}\nStatus: {selectedFriendlyWasp.WorkforceState}\nLocation: {location}";
        }

        bool canReturn = selectedFriendlyWasp.WorkforceState == WaspWorkforceState.Stationed &&
                         selectedFriendlyWasp.HomeHive != null;
        if (friendlyWaspReturnButton != null)
            friendlyWaspReturnButton.interactable = canReturn;

        bool canUseBuilderAction = selectedFriendlyWasp.AssignedFunction == WaspFunction.Builder &&
                                   selectedFriendlyWasp.WorkforceState == WaspWorkforceState.Stationed &&
                                   selectedFriendlyWasp.StationedHex != null &&
                                   selectedFriendlyWasp.StationedHex.State == HexTile.HexState.Owned;
        if (friendlyWaspBuilderButton != null)
            friendlyWaspBuilderButton.gameObject.SetActive(canUseBuilderAction);
    }

    private void ShowFriendlyWaspInformation()
    {
        if (selectedFriendlyWasp == null || selectedFriendlyWasp.WaspInfo == null)
            return;

        WaspInfo info = selectedFriendlyWasp.WaspInfo;
        HideFriendlyWaspActions();
        WaspInfoPanel panel = waspInfoPanel != null ? waspInfoPanel.GetComponent<WaspInfoPanel>() : null;
        panel?.Open(info);
    }

    private void SendSelectedFriendlyWaspHome()
    {
        if (selectedFriendlyWasp == null)
            return;

        bool returning = selectedFriendlyWasp.ReturnToHomeHive();
        if (friendlyWaspFeedback != null)
            friendlyWaspFeedback.text = returning ? "Returning to the home hive." : "This wasp cannot return right now.";

        RefreshFriendlyWaspActions();
    }

    private void OpenSelectedBuilderHivePanel()
    {
        if (selectedFriendlyWasp == null)
            return;

        WaspControl builder = selectedFriendlyWasp;
        HideFriendlyWaspActions();
        OpenBuilderHivePanel(builder);
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
        if (pauseTitleText != null)
            pauseTitleText.text = "SETTINGS";
    }

    public void ShowPauseMain()
    {
        if (pauseMainButtons != null)
            pauseMainButtons.SetActive(true);
        if (pauseOptions != null)
            pauseOptions.SetActive(false);
        if (pauseTitleText != null)
            pauseTitleText.text = "PAUSED";
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

    public void QuitGame()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void OpenBuilderHivePanel(WaspControl builder)
    {
        if (builder == null)
            return;

        selectedBuilder = builder;
        CloseWaspInfo();
        CloseSkills();
        HideHiveTraining();
        CreateBuilderHivePanel();
        if (builderHivePanel != null)
            builderHivePanel.SetActive(true);
        RefreshBuilderHivePanel();
    }

    public void ReturnFromBuilderHivePanel()
    {
        HideBuilderHivePanel();
        cameraFocus?.ReturnToPreviousView();
    }

    private void SpawnBuilderHive()
    {
        HiveManagement hive = HiveManagement.GetOrCreate();
        bool created = hive != null && hive.TryBuildHive(selectedBuilder);
        if (builderHiveFeedback == null)
            return;

        if (created)
            builderHiveFeedback.text = "Native hive established on this territory.";
        else
            builderHiveFeedback.text = "Unable to establish a hive here.";
        RefreshBuilderHivePanel();
    }

    private void HideBuilderHivePanel()
    {
        if (builderHivePanel != null)
            builderHivePanel.SetActive(false);
        selectedBuilder = null;
    }

    private void CreateBuilderHivePanel()
    {
        if (builderHivePanel != null)
            return;

        Canvas canvas = GetComponentInParent<Canvas>();
        Transform parent = canvas != null ? canvas.transform : transform;
        builderHivePanel = CreateUiObject(
            "BuilderHivePanel",
            parent,
            Vector2.zero,
            Vector2.one,
            Vector2.zero,
            Vector2.zero);
        Image backdrop = builderHivePanel.AddComponent<Image>();
        backdrop.color = new Color(0.01f, 0.03f, 0.03f, 0.78f);

        GameObject card = CreateUiObject(
            "BuilderHiveCard",
            builderHivePanel.transform,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(620f, 390f));
        Image cardImage = card.AddComponent<Image>();
        cardImage.color = new Color(0.055f, 0.11f, 0.1f, 0.98f);

        builderHiveTitle = CreateText(
            "BuilderHiveTitle",
            card.transform,
            "BUILDER DEPLOYMENT",
            new Vector2(0f, 138f),
            new Vector2(530f, 44f),
            26f);
        builderHiveDetails = CreateText(
            "BuilderHiveDetails",
            card.transform,
            string.Empty,
            new Vector2(0f, 44f),
            new Vector2(520f, 132f),
            18f);
        builderHiveDetails.alignment = TextAlignmentOptions.TopLeft;
        builderHiveFeedback = CreateText(
            "BuilderHiveFeedback",
            card.transform,
            string.Empty,
            new Vector2(0f, -80f),
            new Vector2(520f, 34f),
            15f);
        builderHiveSpawnButton = CreateButton(
            card.transform,
            "BuilderHiveSpawn",
            "SPAWN HIVE",
            -132f,
            SpawnBuilderHive);
        CreateButton(
            card.transform,
            "BuilderHiveReturn",
            "RETURN",
            -200f,
            ReturnFromBuilderHivePanel);
        builderHivePanel.SetActive(false);
    }

    private void RefreshBuilderHivePanel()
    {
        if (selectedBuilder == null || builderHivePanel == null)
            return;

        HiveManagement hive = HiveManagement.GetOrCreate();
        HexTile target = selectedBuilder.StationedHex;
        SB_Wasp_Skill definition = hive != null ? hive.GetSkillDefinition(WaspFunction.Builder) : null;
        WaspSkillCost cost = definition != null ? definition.HiveConstructionCost : default;
        bool established = target != null && target.FriendlyHive != null;
        bool canBuild = hive != null && hive.CanBuildHive(selectedBuilder);

        if (builderHiveTitle != null)
            builderHiveTitle.text = established ? "HIVE ESTABLISHED" : "BUILDER DEPLOYMENT";
        if (builderHiveDetails != null)
        {
            builderHiveDetails.text =
                $"Target territory: {(target != null ? target.HexName : "Unavailable")}\n\n" +
                $"Construction cost\nNectar {cost.nectar:0}   Prey {cost.prey:0}   Fibre {cost.fibre:0}\n\n" +
                (established
                    ? "A friendly hive is already established on this hex."
                    : "The Builder will remain stationed after construction.");
        }

        if (builderHiveSpawnButton != null)
            builderHiveSpawnButton.interactable = canBuild;
        if (builderHiveFeedback != null && string.IsNullOrEmpty(builderHiveFeedback.text))
            builderHiveFeedback.text = established ? "Construction unavailable: hive already established." : string.Empty;
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

        DestroyStalePauseMenus(canvas);

        if (pauseMenuPrefab != null && CreateHerbertPauseMenu(canvas))
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
            new Vector2(0f, 172f),
            new Vector2(360f, 34f),
            22f);
        CreateText(
            "PauseScrollSpeedLabel",
            pauseOptions.transform,
            "Scroll wheel zoom speed",
            new Vector2(0f, 138f),
            new Vector2(360f, 26f),
            17f);
        scrollSpeedValueText = CreateText(
            "PauseScrollSpeedValue",
            pauseOptions.transform,
            string.Empty,
            new Vector2(0f, 112f),
            new Vector2(180f, 22f),
            15f);
        scrollSpeedSlider = CreateSlider(pauseOptions.transform, new Vector2(0f, 84f));
        CreateKeybindsText(pauseOptions.transform, new Vector2(0f, -40f), new Vector2(400f, 220f), 13f);
        CreateButton(pauseOptions.transform, "PauseOptionsBack", "BACK", -178f, ShowPauseMain);

        float speed = PlayerPrefs.GetFloat(
            ScrollSpeedPreferenceKey,
            DefaultScrollWheelZoomSpeed);
        SetScrollWheelZoomSpeed(speed);
        pauseMenu.SetActive(false);
    }

    /// <summary>
    /// The pause menu is always rebuilt at runtime because its reference is not serialised, so a
    /// copy accidentally saved into a scene would sit underneath the fresh one and show through as
    /// a ghost. Clear any leftovers before building.
    /// </summary>
    private void DestroyStalePauseMenus(Canvas canvas)
    {
        if (canvas == null)
            return;

        foreach (Transform child in canvas.transform)
        {
            if (child != null && child.name == "PauseMenu")
                Destroy(child.gameObject);
        }
    }

    private bool CreateHerbertPauseMenu(Canvas canvas)
    {
        pauseMenu = CreateUiObject(
            "PauseMenu",
            canvas.transform,
            Vector2.zero,
            Vector2.one,
            Vector2.zero,
            Vector2.zero);
        Image backdrop = pauseMenu.AddComponent<Image>();
        backdrop.color = new Color(0.015f, 0.025f, 0.03f, 0.78f);

        GameObject visual = Instantiate(pauseMenuPrefab, pauseMenu.transform);
        visual.name = "Herbert Pause Menu";
        RectTransform visualRect = visual.GetComponent<RectTransform>();
        visualRect.anchorMin = new Vector2(0.5f, 0.5f);
        visualRect.anchorMax = new Vector2(0.5f, 0.5f);
        visualRect.pivot = new Vector2(0.5f, 0.5f);
        visualRect.anchoredPosition = Vector2.zero;
        visualRect.localScale = Vector3.one * 1.2f;

        Button[] buttons = visual.GetComponentsInChildren<Button>(true);
        Button resumeButton = FindPauseButton(buttons, "Codex");
        Button optionsButton = FindPauseButton(buttons, "Settings");
        Button menuButton = FindPauseButton(buttons, "Menu");
        Button quitButton = FindPauseButton(buttons, "Quit Game");
        pauseTitleText = visual.GetComponentsInChildren<TMP_Text>(true)
            .FirstOrDefault(text => text.text == "Paused");

        if (resumeButton == null || optionsButton == null || menuButton == null || quitButton == null)
        {
            Destroy(pauseMenu);
            pauseMenu = null;
            pauseTitleText = null;
            return false;
        }

        Transform buttonContainer = resumeButton.transform.parent;
        VerticalLayoutGroup sourceLayout = buttonContainer.GetComponent<VerticalLayoutGroup>();
        if (sourceLayout != null)
            sourceLayout.enabled = false;

        pauseMainButtons = CreateUiObject(
            "PauseMenuMainButtons",
            buttonContainer,
            Vector2.zero,
            Vector2.one,
            Vector2.zero,
            Vector2.zero);
        VerticalLayoutGroup mainLayout = pauseMainButtons.AddComponent<VerticalLayoutGroup>();
        mainLayout.childAlignment = TextAnchor.MiddleCenter;
        mainLayout.spacing = 28f;
        mainLayout.childControlWidth = false;
        mainLayout.childControlHeight = false;
        mainLayout.childForceExpandWidth = false;
        mainLayout.childForceExpandHeight = false;
        mainLayout.childScaleWidth = true;
        mainLayout.childScaleHeight = true;

        Button[] orderedButtons = { resumeButton, optionsButton, menuButton, quitButton };
        foreach (Button button in orderedButtons)
        {
            button.transform.SetParent(pauseMainButtons.transform, false);
            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(350f, 100f);
            rect.localScale = Vector3.one * 0.5f;
        }

        ConfigureHerbertPauseButton(resumeButton, "RESUME", ResumeGame);
        ConfigureHerbertPauseButton(optionsButton, "OPTIONS", OpenPauseOptions);
        ConfigureHerbertPauseButton(menuButton, "MAIN MENU", QuitToMenu);
        ConfigureHerbertPauseButton(quitButton, "QUIT GAME", QuitGame);

        pauseOptions = CreateUiObject(
            "PauseMenuOptions",
            buttonContainer,
            Vector2.zero,
            Vector2.one,
            Vector2.zero,
            Vector2.zero);
        CreatePauseOptionsText(
            pauseOptions.transform,
            "SCROLL ZOOM SPEED",
            new Vector2(0f, 158f),
            new Vector2(290f, 28f),
            19f);
        scrollSpeedValueText = CreatePauseOptionsText(
            pauseOptions.transform,
            string.Empty,
            new Vector2(0f, 132f),
            new Vector2(180f, 24f),
            17f);
        scrollSpeedSlider = CreateSlider(pauseOptions.transform, new Vector2(0f, 108f));
        // Container is 300x351; keep inside it and inside the band above the Back button.
        CreateKeybindsText(pauseOptions.transform, new Vector2(0f, -16f), new Vector2(286f, 216f), 11f);

        Button backButton = Instantiate(resumeButton, pauseOptions.transform);
        backButton.gameObject.name = "PauseOptionsBack";
        RectTransform backRect = backButton.GetComponent<RectTransform>();
        backRect.anchorMin = new Vector2(0.5f, 0.5f);
        backRect.anchorMax = new Vector2(0.5f, 0.5f);
        backRect.pivot = new Vector2(0.5f, 0.5f);
        backRect.anchoredPosition = new Vector2(0f, -152f);
        backRect.sizeDelta = new Vector2(350f, 100f);
        backRect.localScale = Vector3.one * 0.5f;
        ConfigureHerbertPauseButton(backButton, "BACK", ShowPauseMain);

        pauseOptions.SetActive(false);
        float speed = PlayerPrefs.GetFloat(
            ScrollSpeedPreferenceKey,
            DefaultScrollWheelZoomSpeed);
        SetScrollWheelZoomSpeed(speed);
        pauseMenu.SetActive(false);
        return true;
    }

    private Button FindPauseButton(Button[] buttons, string label)
    {
        return buttons.FirstOrDefault(button =>
        {
            TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
            return text != null && text.text == label;
        });
    }

    private void ConfigureHerbertPauseButton(
        Button button,
        string label,
        UnityEngine.Events.UnityAction action)
    {
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
        TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
        if (text == null)
            return;
        text.text = label;
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = true;
        text.fontSizeMin = 20f;
        text.fontSizeMax = 34f;
    }

    /// <summary>
    /// Fills in the control list. If a text element was assigned in the inspector that one is used
    /// as-is, otherwise a left-aligned block is built inside the options panel.
    /// </summary>
    private TMP_Text CreateKeybindsText(Transform parent, Vector2 position, Vector2 size, float fontSize)
    {
        // Only reuse an element that was assigned in the inspector, and still refit it. Caching a
        // self-created one here would make later rebuilds skip the layout below and keep a stale rect.
        if (keybindsText != null && keybindsText.transform.IsChildOf(parent))
        {
            keybindsText.text = KeybindSummary;
            FitTextToHeight(keybindsText, ((RectTransform)keybindsText.transform).rect.height);
            return keybindsText;
        }

        TMP_Text text = CreateText("PauseOptionsKeybinds", parent, KeybindSummary, position, size, fontSize);
        text.alignment = TextAlignmentOptions.TopLeft;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.color = new Color(0.82f, 0.88f, 0.82f, 1f);
        // Auto-size instead of trusting a hand-picked point size: the two pause menus have
        // different container sizes, and the list is long enough to overflow either one.
        text.enableAutoSizing = true;
        text.fontSizeMax = fontSize;
        text.fontSizeMin = 6f;
        text.lineSpacing = -12f;
        // Auto-size only constrains width while wrapping is off, so shrink for height ourselves.
        FitTextToHeight(text, size.y);
        if (pauseTitleText != null)
        {
            text.font = pauseTitleText.font;
            text.fontSharedMaterial = pauseTitleText.fontSharedMaterial;
        }

        return text;
    }

    /// <summary>
    /// TMP's auto-sizing only fits width when word wrapping is disabled, so the control list can
    /// still overrun its box vertically. Step the size down until it fits the available height.
    /// </summary>
    private static void FitTextToHeight(TMP_Text text, float availableHeight)
    {
        if (availableHeight <= 0f)
            return;

        for (int attempt = 0; attempt < 12; attempt++)
        {
            text.ForceMeshUpdate();
            if (text.preferredHeight <= availableHeight || text.fontSizeMax <= text.fontSizeMin)
                return;

            text.fontSizeMax = Mathf.Max(text.fontSizeMin, text.fontSizeMax - 0.5f);
        }
    }

    private TMP_Text CreatePauseOptionsText(
        Transform parent,
        string value,
        Vector2 position,
        Vector2 size,
        float fontSize)
    {
        TMP_Text text = CreateText(
            "PauseOptionText",
            parent,
            value,
            position,
            size,
            fontSize);
        if (pauseTitleText != null)
        {
            text.font = pauseTitleText.font;
            text.fontSharedMaterial = pauseTitleText.fontSharedMaterial;
        }
        return text;
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
        // Every button this script builds gets the shared skin, so code-made panels stop looking
        // like a different game to the authored ones.
        ApplyHerbertButtonSkin(result);
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
        EnsureBuilderTrainingControls();
        ArrangeHiveTrainingLayout();
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
        SetText("nametag", "Native Hive");
        SetText("HiveTraining_Subtitle", "Train colony roles");
        SetText(
            "HiveTraining_Resources",
            resources == null
                ? "Nectar 0   Prey 0   Fibre 0"
                : $"Nectar {resources.Nectar:0}   Prey {resources.Prey:0}   Fibre {resources.Fibre:0}");

        SetRoleText(hive, WaspFunction.Scout, "Scout", "HiveTraining_ScoutInfo");
        SetRoleText(hive, WaspFunction.Forager, "Forager", "HiveTraining_ForagerInfo");
        SetRoleText(hive, WaspFunction.Builder, "Builder", "HiveTraining_BuilderInfo");
        SetRoleText(hive, WaspFunction.Guard, "Attacker", "HiveTraining_AttackerInfo");
        SetTrainingButton(hive, WaspFunction.Scout, "HiveTrain_Scout", "Train Scout");
        SetTrainingButton(hive, WaspFunction.Forager, "HiveTrain_Forager", "Train Forager");
        SetTrainingButton(hive, WaspFunction.Builder, "HiveTrain_Builder", "Train Builder");
        SetTrainingButton(hive, WaspFunction.Guard, "HiveTrain_Attacker", "Train Attacker");
        SetText("HiveTraining_Feedback", trainingFeedback);
        SetNestedText("HiveTraining_Hide", "scouted", trainingFeedback);
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

    private void TrainBuilder()
    {
        Train(WaspFunction.Builder);
    }

    private void EnsureBuilderTrainingControls()
    {
        if (hiveTrainingPanel == null || FindChild("HiveTrain_Builder") != null)
            return;

        CreateText(
            "HiveTraining_BuilderInfo",
            hiveTrainingPanel.transform,
            "Builder: 0 total   0 available",
            new Vector2(0f, -118f),
            new Vector2(340f, 38f),
            15f);
        CreateButton(
            hiveTrainingPanel.transform,
            "HiveTrain_Builder",
            "Train Builder",
            -162f,
            TrainBuilder);
    }

    private void ArrangeHiveTrainingLayout()
    {
        if (hiveTrainingPanel == null)
            return;

        if (FindChild("Herbert Hive Training") != null)
            return;

        RectTransform panelRect = hiveTrainingPanel.GetComponent<RectTransform>();
        if (panelRect != null)
            panelRect.sizeDelta = new Vector2(500f, 660f);

        SetHiveTrainingRect("HiveTraining_Title", 0f, 278f, 440f, 38f);
        SetHiveTrainingRect("HiveTraining_Subtitle", 0f, 240f, 440f, 28f);
        SetHiveTrainingRect("HiveTraining_Resources", 0f, 205f, 440f, 30f);
        SetHiveTrainingRect("HiveTraining_ScoutInfo", 0f, 154f, 420f, 26f);
        SetHiveTrainingRect("HiveTrain_Scout", 0f, 116f, 400f, 46f);
        SetHiveTrainingRect("HiveTraining_ForagerInfo", 0f, 65f, 420f, 26f);
        SetHiveTrainingRect("HiveTrain_Forager", 0f, 27f, 400f, 46f);
        SetHiveTrainingRect("HiveTraining_BuilderInfo", 0f, -24f, 420f, 26f);
        SetHiveTrainingRect("HiveTrain_Builder", 0f, -62f, 400f, 46f);
        SetHiveTrainingRect("HiveTraining_AttackerInfo", 0f, -113f, 420f, 26f);
        SetHiveTrainingRect("HiveTrain_Attacker", 0f, -151f, 400f, 46f);
        SetHiveTrainingRect("HiveTraining_Feedback", 0f, -218f, 440f, 30f);
        SetHiveTrainingRect("HiveTraining_Hide", 0f, -274f, 240f, 42f);

        SetTrainingLabelSize("HiveTrain_Scout");
        SetTrainingLabelSize("HiveTrain_Forager");
        SetTrainingLabelSize("HiveTrain_Builder");
        SetTrainingLabelSize("HiveTrain_Attacker");
    }

    private void SetHiveTrainingRect(string objectName, float x, float y, float width, float height)
    {
        GameObject target = FindChild(objectName);
        if (target == null)
            return;

        RectTransform rect = target.GetComponent<RectTransform>();
        if (rect == null)
            return;

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(width, height);
    }

    private void SetTrainingLabelSize(string buttonName)
    {
        GameObject target = FindChild(buttonName);
        TMP_Text label = target != null ? target.GetComponentInChildren<TMP_Text>(true) : null;
        if (label != null)
            label.fontSize = 14f;
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
        SB_Wasp_Skill definition = hive.GetSkillDefinition(function);
        WaspSkillCost cost = definition != null ? definition.TrainingCost : default;
        string costText = FormatCost(cost);
        SetText(
            objectName,
            string.IsNullOrEmpty(costText)
                ? $"{roleName}  •  {total} total  •  {available} available"
                : $"{roleName}  •  {total} total  •  {available} available  •  {costText}");
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

        GameObject labelObject = FindChild(objectName + "_Label");
        TMP_Text text = labelObject != null
            ? labelObject.GetComponent<TMP_Text>()
            : target.GetComponentInChildren<TMP_Text>(true);
        if (text != null)
            text.text = label;

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
        if (text == null && target != null)
            text = target.GetComponentInChildren<TMP_Text>(true);
        if (text != null)
            text.text = value;
    }

    private void SetNestedText(string parentName, string childName, string value)
    {
        GameObject parent = FindChild(parentName);
        if (parent == null)
            return;

        TMP_Text[] texts = parent.GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text text in texts)
        {
            if (text.gameObject.name != childName)
                continue;

            text.text = value;
            return;
        }
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
            button = buttonObject.GetComponentInChildren<Button>(true);
        if (button == null)
            button = buttonObject.GetComponentInParent<Button>(true);
        if (button == null)
        {
            return;
        }

        button.interactable = true;
        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }
}
