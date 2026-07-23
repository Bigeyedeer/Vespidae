#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class VespidaeMenuSetup
{
    private const string MenuScenePath = "Assets/Scenes/Menu.unity";
    private const string MainWorldScenePath = "Assets/Scenes/wasp RTS Lvl.unity";
    private const string SpeciesFolder = "Assets/ScriptableObjectInstances/WaspSpecies";
    private const string RuntimeFolder = "Assets/ScriptableObjectInstances/Runtime";
    private const string PrefabFolder = "Assets/Prefabs/UI";

    [MenuItem("Tools/Vespidae Wars/Build Menu Flow")]
    public static void BuildMenuFlow()
    {
        EnsureTextMeshProResources();
        EnsureFolder("Assets", "ScriptableObjectInstances");
        EnsureFolder("Assets/ScriptableObjectInstances", "WaspSpecies");
        EnsureFolder("Assets/ScriptableObjectInstances", "Runtime");
        EnsureFolder("Assets", "Prefabs");
        EnsureFolder("Assets/Prefabs", "UI");

        SB_Wasps_Info nativeWasp = CreateOrUpdateNativeWasp();
        SB_Wasps_Info europeanPaperWasp = CreateOrUpdateEuropeanPaperWasp();
        SB_Wasps_Info germanWasp = CreateOrUpdateGermanWasp();
        SB_PlayerSelection_State selectionState = CreateOrUpdateSelectionState(nativeWasp);
        CreateOrUpdateFunctionCardPrefab();

        // Reload the saved prefab from the AssetDatabase before assigning it to a
        // scene component. The object returned while SaveAsPrefabAsset is importing
        // can otherwise become a missing reference after the temporary root is destroyed.
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(
            $"{PrefabFolder}/WaspFunctionCard.prefab",
            ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        nativeWasp = AssetDatabase.LoadAssetAtPath<SB_Wasps_Info>($"{SpeciesFolder}/SO_PolistesMarginalis.asset");
        europeanPaperWasp = AssetDatabase.LoadAssetAtPath<SB_Wasps_Info>($"{SpeciesFolder}/SO_PolistesDominula.asset");
        germanWasp = AssetDatabase.LoadAssetAtPath<SB_Wasps_Info>($"{SpeciesFolder}/SO_VespulaGermanica.asset");
        selectionState = AssetDatabase.LoadAssetAtPath<SB_PlayerSelection_State>($"{RuntimeFolder}/SO_PlayerSelection.asset");
        GameObject cardPrefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/WaspFunctionCard.prefab");
        C_WaspSelectionCard cardPrefab = cardPrefabRoot.GetComponent<C_WaspSelectionCard>();

        BuildMenuScene(nativeWasp, europeanPaperWasp, germanWasp, selectionState, cardPrefab);
        ConfigureBuildScenes();

        AssetDatabase.SaveAssets();
        RepairExternalSceneReferences();
        AssetDatabase.SaveAssets();
        Debug.Log("Vespidae Wars menu flow, species assets, prefab, scenes, and build settings were created successfully.");
    }

    private static void RepairExternalSceneReferences()
    {
        SB_Wasps_Info nativeWasp = AssetDatabase.LoadAssetAtPath<SB_Wasps_Info>($"{SpeciesFolder}/SO_PolistesMarginalis.asset");
        SB_Wasps_Info europeanPaperWasp = AssetDatabase.LoadAssetAtPath<SB_Wasps_Info>($"{SpeciesFolder}/SO_PolistesDominula.asset");
        SB_Wasps_Info germanWasp = AssetDatabase.LoadAssetAtPath<SB_Wasps_Info>($"{SpeciesFolder}/SO_VespulaGermanica.asset");
        SB_PlayerSelection_State selectionState = AssetDatabase.LoadAssetAtPath<SB_PlayerSelection_State>($"{RuntimeFolder}/SO_PlayerSelection.asset");
        GameObject cardPrefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/WaspFunctionCard.prefab");
        C_WaspSelectionCard cardPrefab = cardPrefabRoot != null ? cardPrefabRoot.GetComponent<C_WaspSelectionCard>() : null;

        if (nativeWasp == null || europeanPaperWasp == null || germanWasp == null || selectionState == null || cardPrefab == null)
        {
            throw new System.InvalidOperationException("Required Vespidae Wars assets could not be reloaded for final scene wiring.");
        }

        Scene menuScene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        GameObject menuControllerObject = GameObject.Find("MenuController");
        C_MainMenu_Ctrl mainController = menuControllerObject.GetComponent<C_MainMenu_Ctrl>();
        C_WaspSelection_Menu selectionController = menuControllerObject.GetComponent<C_WaspSelection_Menu>();
        SetPrivateField(mainController, "selectionState", selectionState);
        SetPrivateField(selectionController, "selectableWasps", new[] { nativeWasp, europeanPaperWasp, germanWasp });
        SetPrivateField(selectionController, "selectionState", selectionState);
        SetPrivateField(selectionController, "cardPrefab", cardPrefab);
        EditorSceneManager.MarkSceneDirty(menuScene);
        EditorSceneManager.SaveScene(menuScene);

        EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
    }

    private static SB_Wasps_Info CreateOrUpdateNativeWasp()
    {
        SB_Wasps_Info asset = LoadOrCreateAsset<SB_Wasps_Info>($"{SpeciesFolder}/SO_PolistesMarginalis.asset");
        asset.ConfigureForEditor(
            "polistes_marginalis",
            "South African paper wasp",
            "Polistes marginalis",
            WaspClassification.Native,
            WaspScopeRole.NativePlayer,
            true,
            new Color(0.84f, 0.66f, 0.29f, 1f),
            "The native player colony protects an exposed paper nest, raises brood, gathers nectar and prey, and prevents invasive establishment.",
            "Predates on caterpillars and other small insects, helping maintain ecological balance.",
            "Exposed, single-layer paper comb.",
            "Native fynbos habitat and local strongholds.",
            "Usually docile when undisturbed and organised around nest defence when threatened.",
            "Identify before intervening: native paper wasps are ecologically valuable and should not be removed indiscriminately.",
            new List<WaspFunctionInfo>
            {
                new WaspFunctionInfo(WaspFunction.Scout, "Scout", "Search adjacent territory and return with visual, nest, and habitat evidence.", "Reveal one additional identification clue at the start."),
                new WaspFunctionInfo(WaspFunction.Forager, "Forager", "Collect nectar for adult energy and hunt soft-bodied prey for larvae.", "Begin with a small nectar and prey gathering bonus."),
                new WaspFunctionInfo(WaspFunction.Builder, "Builder", "Gather plant fibre and expand or repair the exposed paper comb.", "Begin with improved nest repair speed."),
                new WaspFunctionInfo(WaspFunction.BroodCaretaker, "Brood Caretaker", "Feed larvae and convert delivered prey into healthy colony growth.", "Begin with increased brood progress."),
                new WaspFunctionInfo(WaspFunction.Guard, "Guard", "Respond to alarm and protect the Queen, brood, and nest entrance.", "Begin with faster defensive readiness."),
                new WaspFunctionInfo(WaspFunction.Containment, "Containment", "Intercept invasive workers, slow their spread, and hold contested territory.", "Begin with reduced invasion pressure in the first contested area.")
            });
        EditorUtility.SetDirty(asset);
        return asset;
    }

    private static SB_Wasps_Info CreateOrUpdateEuropeanPaperWasp()
    {
        SB_Wasps_Info asset = LoadOrCreateAsset<SB_Wasps_Info>($"{SpeciesFolder}/SO_PolistesDominula.asset");
        asset.ConfigureForEditor(
            "polistes_dominula",
            "European paper wasp",
            "Polistes dominula",
            WaspClassification.Invasive,
            WaspScopeRole.PrimaryInvasive,
            true,
            new Color(0.91f, 0.48f, 0.18f, 1f),
            "The primary invasive colony expands into contested habitat and competes for prey and nesting opportunities.",
            "Competes with native colonies for prey, nest sites, and territory at warm habitat edges.",
            "Exposed, above-ground paper nest in warm, sunny, sheltered locations.",
            "Warm exposed urban and agricultural edges advancing toward fynbos.",
            "Localised aggressive defence when workers or nest sites are threatened.",
            "Appearance alone is not enough: compare antennae, markings, legs, nest, habitat, and behaviour before intervening.",
            new List<WaspFunctionInfo>());
        EditorUtility.SetDirty(asset);
        return asset;
    }

    private static SB_Wasps_Info CreateOrUpdateGermanWasp()
    {
        SB_Wasps_Info asset = LoadOrCreateAsset<SB_Wasps_Info>($"{SpeciesFolder}/SO_VespulaGermanica.asset");
        asset.ConfigureForEditor(
            "vespula_germanica",
            "German wasp",
            "Vespula germanica",
            WaspClassification.Invasive,
            WaspScopeRole.SecondaryInvasive,
            true,
            new Color(0.95f, 0.76f, 0.18f, 1f),
            "An aggressive invasive colony that uses cool, moist habitat, defends its enclosed nest, and creates high-pressure territory contests.",
            "Creates high ecological pressure through aggressive defence and competition in suitable habitat.",
            "Commonly underground or enclosed rather than an exposed Polistes comb.",
            "Cooler, moister, river, irrigated, or human-modified habitat.",
            "High aggression with strong nest defence, chasing, and a shorter reaction window.",
            "Environmental conditions and nest architecture are important identification evidence.",
            new List<WaspFunctionInfo>());
        EditorUtility.SetDirty(asset);
        return asset;
    }

    private static SB_PlayerSelection_State CreateOrUpdateSelectionState(SB_Wasps_Info nativeWasp)
    {
        SB_PlayerSelection_State asset = LoadOrCreateAsset<SB_PlayerSelection_State>($"{RuntimeFolder}/SO_PlayerSelection.asset");
        asset.ConfigureDefaultsForEditor(nativeWasp, WaspFunction.Scout);
        EditorUtility.SetDirty(asset);
        return asset;
    }

    private static C_WaspSelectionCard CreateOrUpdateFunctionCardPrefab()
    {
        string prefabPath = $"{PrefabFolder}/WaspFunctionCard.prefab";
        GameObject root = CreateRectObject(
            "WaspFunctionCard",
            null,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(410f, 155f));
        Image background = root.AddComponent<Image>();
        background.color = new Color(0.12f, 0.18f, 0.16f, 1f);
        Button button = root.AddComponent<Button>();
        button.targetGraphic = background;

        Image icon = CreateImage("Icon", root.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(54f, 0f), new Vector2(70f, 70f), Color.white);
        TMP_Text title = CreateText("Title", root.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(25f, -70f), new Vector2(-25f, -15f), 26f, FontStyles.Bold, TextAlignmentOptions.TopLeft);
        TMP_Text description = CreateText("Description", root.transform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(25f, 16f), new Vector2(-25f, -65f), 16f, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        description.textWrappingMode = TextWrappingModes.Normal;

        C_WaspSelectionCard card = root.AddComponent<C_WaspSelectionCard>();
        SetReference(card, "button", button);
        SetReference(card, "background", background);
        SetReference(card, "icon", icon);
        SetReference(card, "titleText", title);
        SetReference(card, "descriptionText", description);

        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.ImportAsset(
            prefabPath,
            ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        return AssetDatabase.LoadAssetAtPath<C_WaspSelectionCard>(prefabPath);
    }

    private static void BuildMenuScene(
        SB_Wasps_Info nativeWasp,
        SB_Wasps_Info europeanPaperWasp,
        SB_Wasps_Info germanWasp,
        SB_PlayerSelection_State selectionState,
        C_WaspSelectionCard cardPrefab)
    {
        // Resolve persistent asset instances at the point where the scene is
        // serialized. AssetDatabase refreshes can invalidate earlier handles.
        nativeWasp = AssetDatabase.LoadAssetAtPath<SB_Wasps_Info>($"{SpeciesFolder}/SO_PolistesMarginalis.asset");
        europeanPaperWasp = AssetDatabase.LoadAssetAtPath<SB_Wasps_Info>($"{SpeciesFolder}/SO_PolistesDominula.asset");
        germanWasp = AssetDatabase.LoadAssetAtPath<SB_Wasps_Info>($"{SpeciesFolder}/SO_VespulaGermanica.asset");
        selectionState = AssetDatabase.LoadAssetAtPath<SB_PlayerSelection_State>($"{RuntimeFolder}/SO_PlayerSelection.asset");
        GameObject cardPrefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/WaspFunctionCard.prefab");
        cardPrefab = cardPrefabRoot != null ? cardPrefabRoot.GetComponent<C_WaspSelectionCard>() : null;

        if (nativeWasp == null || europeanPaperWasp == null || germanWasp == null || selectionState == null || cardPrefab == null)
        {
            throw new System.InvalidOperationException("Required Vespidae Wars menu assets could not be loaded before scene creation.");
        }

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "Menu";

        CreateCamera(new Color(0.02f, 0.05f, 0.04f, 1f));
        GameObject menuRoot = new GameObject("MenuRoot");
        Canvas canvas = CreateCanvas(menuRoot.transform);

        CreateImage("Background", canvas.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.025f, 0.055f, 0.045f, 1f), true);

        GameObject mainPanel = CreatePanel("MainMenuPanel", canvas.transform, new Vector2(0.5f, 0.5f), new Vector2(620f, 720f));
        TMP_Text title = CreateText("Title", mainPanel.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(35f, -45f), new Vector2(-35f, 130f), 56f, FontStyles.Bold, TextAlignmentOptions.Center);
        title.text = "VESPIDAE WARS";
        TMP_Text subtitle = CreateText("Subtitle", mainPanel.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(45f, -170f), new Vector2(-45f, 70f), 22f, FontStyles.Italic, TextAlignmentOptions.Center);
        subtitle.text = "Protect the native colony. Identify before intervening.";

        Button startButton = CreateButton("StartButton", mainPanel.transform, "START", new Vector2(0f, 55f), new Vector2(380f, 82f));
        Button optionsButton = CreateButton("OptionsButton", mainPanel.transform, "OPTIONS", new Vector2(0f, -55f), new Vector2(380f, 82f));
        Button quitButton = CreateButton("QuitButton", mainPanel.transform, "QUIT", new Vector2(0f, -165f), new Vector2(380f, 82f));

        GameObject optionsPanel = CreatePanel("OptionsPanel", canvas.transform, new Vector2(0.5f, 0.5f), new Vector2(720f, 650f));
        CreateLabel(optionsPanel.transform, "OPTIONS", new Vector2(0f, 235f), 46f);
        TMP_Text volumeLabel = CreateText("VolumeLabel", optionsPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 110f), new Vector2(520f, 40f), 24f, FontStyles.Bold, TextAlignmentOptions.Left);
        volumeLabel.text = "Master Volume";
        Slider volumeSlider = CreateSlider("MasterVolumeSlider", optionsPanel.transform, new Vector2(0f, 60f), new Vector2(520f, 30f));
        Toggle fullscreenToggle = CreateToggle("FullscreenToggle", optionsPanel.transform, "Fullscreen", new Vector2(0f, -40f));
        Button optionsBackButton = CreateButton("BackButton", optionsPanel.transform, "BACK", new Vector2(0f, -210f), new Vector2(300f, 72f));

        GameObject selectionPanel = CreatePanel("WaspSelectionPanel", canvas.transform, new Vector2(0.5f, 0.5f), new Vector2(1740f, 940f));
        TMP_Text selectionTitle = CreateText("SelectionTitle", selectionPanel.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(40f, -95f), new Vector2(-40f, -20f), 42f, FontStyles.Bold, TextAlignmentOptions.TopLeft);
        selectionTitle.text = "CHOOSE YOUR WASP SPECIES";
        TMP_Text speciesHeader = CreateText("SpeciesHeader", selectionPanel.transform, new Vector2(0f, 1f), new Vector2(0.55f, 1f), new Vector2(40f, -175f), new Vector2(-20f, -90f), 24f, FontStyles.Normal, TextAlignmentOptions.TopLeft);

        GameObject cardContainerObject = CreateRectObject("FunctionCardContainer", selectionPanel.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(40f, -45f), new Vector2(900f, 555f));
        cardContainerObject.GetComponent<RectTransform>().pivot = new Vector2(0f, 0.5f);
        GridLayoutGroup grid = cardContainerObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(410f, 155f);
        grid.spacing = new Vector2(20f, 20f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 2;
        grid.childAlignment = TextAnchor.UpperLeft;

        GameObject detailPanel = CreatePanel("SelectionDetails", selectionPanel.transform, new Vector2(0.78f, 0.52f), new Vector2(610f, 540f));
        Image detailBackground = detailPanel.GetComponent<Image>();
        detailBackground.color = new Color(0.075f, 0.12f, 0.105f, 0.98f);
        TMP_Text detailsTitle = CreateText("DetailsTitle", detailPanel.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(35f, -35f), new Vector2(-35f, 70f), 32f, FontStyles.Bold, TextAlignmentOptions.TopLeft);
        TMP_Text detailsDescription = CreateText("DetailsDescription", detailPanel.transform, new Vector2(0f, 0.35f), new Vector2(1f, 0.85f), new Vector2(35f, 0f), new Vector2(-35f, 0f), 21f, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        detailsDescription.textWrappingMode = TextWrappingModes.Normal;
        TMP_Text detailsBenefit = CreateText("DetailsBenefit", detailPanel.transform, new Vector2(0f, 0f), new Vector2(1f, 0.32f), new Vector2(35f, 25f), new Vector2(-35f, -10f), 20f, FontStyles.Italic, TextAlignmentOptions.TopLeft);
        detailsBenefit.textWrappingMode = TextWrappingModes.Normal;

        Button selectionBackButton = CreateButton("BackButton", selectionPanel.transform, "BACK", new Vector2(500f, -380f), new Vector2(270f, 70f));
        Button confirmButton = CreateButton("ConfirmButton", selectionPanel.transform, "CONFIRM", new Vector2(700f, -380f), new Vector2(300f, 70f));
        confirmButton.interactable = false;

        CreateEventSystem();
        GameObject controllerObject = new GameObject("MenuController");
        controllerObject.transform.SetParent(menuRoot.transform);
        C_MainMenu_Ctrl mainController = controllerObject.AddComponent<C_MainMenu_Ctrl>();
        C_WaspSelection_Menu selectionController = controllerObject.AddComponent<C_WaspSelection_Menu>();

        SetReference(mainController, "mainMenuPanel", mainPanel);
        SetReference(mainController, "optionsPanel", optionsPanel);
        SetReference(mainController, "waspSelectionPanel", selectionPanel);
        SetReference(mainController, "mainMenuDefaultButton", startButton);
        SetReference(mainController, "optionsDefaultButton", optionsBackButton);
        SetReference(mainController, "selectionDefaultButton", selectionBackButton);
        SetReference(mainController, "masterVolumeSlider", volumeSlider);
        SetReference(mainController, "fullscreenToggle", fullscreenToggle);
        SetPrivateField(mainController, "selectionState", selectionState);

        SetPrivateField(selectionController, "selectableWasps", new[] { nativeWasp, europeanPaperWasp, germanWasp });
        SetPrivateField(selectionController, "selectionState", selectionState);
        SetReference(selectionController, "mainMenuController", mainController);
        SetReference(selectionController, "cardContainer", cardContainerObject.transform);
        SetPrivateField(selectionController, "cardPrefab", cardPrefab);
        SetReference(selectionController, "speciesHeader", speciesHeader);
        SetReference(selectionController, "detailsTitle", detailsTitle);
        SetReference(selectionController, "detailsDescription", detailsDescription);
        SetReference(selectionController, "detailsBenefit", detailsBenefit);
        SetReference(selectionController, "confirmButton", confirmButton);

        UnityEventTools.AddPersistentListener(startButton.onClick, mainController.OpenWaspSelection);
        UnityEventTools.AddPersistentListener(optionsButton.onClick, mainController.OpenOptions);
        UnityEventTools.AddPersistentListener(quitButton.onClick, mainController.QuitGame);
        UnityEventTools.AddPersistentListener(optionsBackButton.onClick, mainController.ReturnToMainMenu);
        UnityEventTools.AddPersistentListener(selectionBackButton.onClick, selectionController.ReturnToMainMenu);
        UnityEventTools.AddPersistentListener(confirmButton.onClick, selectionController.ConfirmSelection);
        UnityEventTools.AddPersistentListener(volumeSlider.onValueChanged, mainController.SetMasterVolume);
        UnityEventTools.AddPersistentListener(fullscreenToggle.onValueChanged, mainController.SetFullscreen);

        mainPanel.SetActive(true);
        optionsPanel.SetActive(false);
        selectionPanel.SetActive(false);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, MenuScenePath);
    }

    private static void BuildMainWorldScene(SB_PlayerSelection_State selectionState)
    {
        selectionState = AssetDatabase.LoadAssetAtPath<SB_PlayerSelection_State>($"{RuntimeFolder}/SO_PlayerSelection.asset");
        if (selectionState == null)
        {
            throw new System.InvalidOperationException("The player selection state asset could not be loaded before MainWorld creation.");
        }

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "MainWorld";

        Camera camera = CreateCamera(new Color(0.012f, 0.025f, 0.022f, 1f));
        camera.transform.position = new Vector3(0f, 9.5f, -10.5f);
        camera.transform.rotation = Quaternion.Euler(38f, 0f, 0f);
        camera.orthographic = true;
        camera.orthographicSize = 7.2f;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 100f;

        GameObject lightObject = new GameObject("Directional Light");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.35f;
        lightObject.transform.rotation = Quaternion.Euler(48f, -30f, 0f);

        GameObject worldRoot = new GameObject("MainWorldRoot");
        GameObject navigationObject = new GameObject("MainWorldNavigation");
        navigationObject.transform.SetParent(worldRoot.transform);
        C_MainWorldNavigation navigation = navigationObject.AddComponent<C_MainWorldNavigation>();
        SetPrivateField(navigation, "selectionState", selectionState);

        GameObject gridRoot = new GameObject("SnappedHexGrid");
        gridRoot.transform.SetParent(worldRoot.transform);
        Vector3[] positions =
        {
            new Vector3(-3.15f, 0f, 2.15f), new Vector3(-1.05f, 0f, 2.15f), new Vector3(1.05f, 0f, 2.15f), new Vector3(3.15f, 0f, 2.15f),
            new Vector3(-2.10f, 0f, 0.35f), new Vector3(0f, 0f, 0.35f), new Vector3(2.10f, 0f, 0.35f),
            new Vector3(-3.15f, 0f, -1.45f), new Vector3(-1.05f, 0f, -1.45f), new Vector3(1.05f, 0f, -1.45f), new Vector3(3.15f, 0f, -1.45f)
        };
        Color[] nodeColors =
        {
            new Color(0.06f, 0.25f, 0.12f, 1f), new Color(0.28f, 0.16f, 0.035f, 1f), new Color(0.30f, 0.08f, 0.08f, 1f), new Color(0.18f, 0.18f, 0.17f, 1f),
            new Color(0.07f, 0.25f, 0.13f, 1f), new Color(0.12f, 0.28f, 0.20f, 1f), new Color(0.29f, 0.10f, 0.08f, 1f),
            new Color(0.08f, 0.24f, 0.11f, 1f), new Color(0.27f, 0.16f, 0.04f, 1f), new Color(0.09f, 0.25f, 0.15f, 1f), new Color(0.20f, 0.20f, 0.19f, 1f)
        };
        string[] habitats = { "Native fynbos", "Dry ridge", "Urban edge", "River corridor", "Fynbos scrub", "Local stronghold", "Irrigated edge", "Fynbos scrub", "Dry ridge", "Local stronghold", "Cool riverbank" };
        string[] statuses = { "Native", "Contested", "Invasive risk", "Open", "Native", "Selected", "Contested", "Native", "Contested", "Native", "Watch" };
        for (int i = 0; i < positions.Length; i++)
        {
            CreateHexNode(gridRoot.transform, navigation, i, positions[i], nodeColors[i], habitats[i], statuses[i]);
        }

        Canvas canvas = CreateCanvas(worldRoot.transform);

        GameObject resourcesPanel = CreatePanel("ResourceBar", canvas.transform, new Vector2(0.5f, 0.5f), new Vector2(930f, 72f));
        SetRectPosition(resourcesPanel, new Vector2(0f, 470f));
        SetPanelOpacity(resourcesPanel, 0f);
        TMP_Text resources = CreateText("Resources", resourcesPanel.transform, Vector2.zero, Vector2.one, new Vector2(25f, 0f), new Vector2(-25f, 0f), 22f, FontStyles.Bold, TextAlignmentOptions.Center);
        resources.text = "NECTAR  340     PREY  90     FIBRE  120     WORKERS  24     STR  61     BROOD  3/5";

        GameObject objectivePanel = CreatePanel("ObjectivePanel", canvas.transform, new Vector2(0.5f, 0.5f), new Vector2(430f, 128f));
        SetRectPosition(objectivePanel, new Vector2(-725f, 405f));
        SetPanelOpacity(objectivePanel, 0f);
        TMP_Text objective = CreateText("Objective", objectivePanel.transform, Vector2.zero, Vector2.one, new Vector2(24f, 14f), new Vector2(-24f, -14f), 20f, FontStyles.Bold, TextAlignmentOptions.TopLeft);
        objective.text = "OBJECTIVE - <color=#e66b70>THREAT: RISING</color>\nScout the ridge and identify the new nest";
        objective.textWrappingMode = TextWrappingModes.Normal;

        GameObject ecologyPanel = CreatePanel("EcologyPanel", canvas.transform, new Vector2(0.5f, 0.5f), new Vector2(430f, 198f));
        SetRectPosition(ecologyPanel, new Vector2(725f, 365f));
        SetPanelOpacity(ecologyPanel, 0f);
        TMP_Text ecology = CreateText("Ecology", ecologyPanel.transform, Vector2.zero, Vector2.one, new Vector2(24f, 14f), new Vector2(-24f, -14f), 19f, FontStyles.Bold, TextAlignmentOptions.TopLeft);
        ecology.text = "HABITAT HEALTH                         78%\n\nBIODIVERSITY                              70%\n\nINVASION PRESSURE                    <color=#e66b70>44%</color>";
        ecology.textWrappingMode = TextWrappingModes.Normal;

        GameObject hudPanel = CreatePanel("SelectedColonyHUD", canvas.transform, new Vector2(0.5f, 0.5f), new Vector2(650f, 280f));
        SetRectPosition(hudPanel, new Vector2(-555f, -330f));
        Image accentPanel = hudPanel.GetComponent<Image>();
        SetPanelOpacity(hudPanel, 0f);

        TMP_Text heading = CreateText("Heading", hudPanel.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(30f, -40f), new Vector2(-30f, -10f), 18f, FontStyles.Bold, TextAlignmentOptions.TopLeft);
        heading.text = "SELECTED SPECIES";
        heading.color = new Color(0.84f, 0.66f, 0.29f, 1f);
        TMP_Text commonName = CreateText("CommonName", hudPanel.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(30f, -82f), new Vector2(-155f, -45f), 27f, FontStyles.Bold, TextAlignmentOptions.TopLeft);
        TMP_Text scientificName = CreateText("ScientificName", hudPanel.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(30f, -116f), new Vector2(-155f, -88f), 18f, FontStyles.Italic, TextAlignmentOptions.TopLeft);
        TMP_Text functionName = CreateText("FunctionName", hudPanel.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(30f, -158f), new Vector2(-155f, -122f), 22f, FontStyles.Bold, TextAlignmentOptions.TopLeft);
        TMP_Text functionSummary = CreateText("FunctionSummary", hudPanel.transform, new Vector2(0f, 0f), new Vector2(1f, 0.40f), new Vector2(30f, 14f), new Vector2(-155f, -8f), 15f, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        functionSummary.textWrappingMode = TextWrappingModes.Normal;
        Image portrait = CreateImage("Portrait", hudPanel.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-95f, -105f), new Vector2(120f, 120f), Color.white);

        GameObject territoryPanel = CreatePanel("TerritoryPanel", canvas.transform, new Vector2(0.5f, 0.5f), new Vector2(290f, 170f));
        SetRectPosition(territoryPanel, new Vector2(710f, -350f));
        SetPanelOpacity(territoryPanel, 0f);
        TMP_Text territory = CreateText("Territory", territoryPanel.transform, Vector2.zero, Vector2.one, new Vector2(20f, 15f), new Vector2(-20f, -15f), 18f, FontStyles.Bold, TextAlignmentOptions.TopLeft);
        territory.text = "TERRITORY\n\n      o   o   o\n         o   o\n\nRidge network - 5 nodes";

        GameObject actionBar = CreatePanel("ActionBar", canvas.transform, new Vector2(0.5f, 0.5f), new Vector2(1300f, 66f));
        SetRectPosition(actionBar, new Vector2(0f, -492f));
        SetPanelOpacity(actionBar, 0f);
        Button scoutButton = CreateButton("ScoutButton", actionBar.transform, "SCOUT", new Vector2(-520f, 0f), new Vector2(220f, 54f));
        Button codexButton = CreateButton("CodexButton", actionBar.transform, "CODEX", new Vector2(-260f, 0f), new Vector2(220f, 54f));
        Button protectButton = CreateButton("ProtectButton", actionBar.transform, "PROTECT", new Vector2(0f, 0f), new Vector2(220f, 54f));
        Button containButton = CreateButton("ContainButton", actionBar.transform, "CONTAIN", new Vector2(260f, 0f), new Vector2(220f, 54f));
        Button skillsButton = CreateButton("SkillsButton", actionBar.transform, "SKILLS", new Vector2(520f, 0f), new Vector2(220f, 54f));

        GameObject displayObject = new GameObject("SelectionDisplayController");
        displayObject.transform.SetParent(worldRoot.transform);
        C_MainWorldSelectionDisplay display = displayObject.AddComponent<C_MainWorldSelectionDisplay>();
        SetPrivateField(display, "selectionState", selectionState);
        SetReference(display, "commonNameText", commonName);
        SetReference(display, "scientificNameText", scientificName);
        SetReference(display, "functionNameText", functionName);
        SetReference(display, "functionSummaryText", functionSummary);
        SetReference(display, "portraitImage", portrait);
        SetReference(display, "accentPanel", accentPanel);

        GameObject infoPanel = CreatePanel("WaspInfoPanel", canvas.transform, new Vector2(0.5f, 0.5f), new Vector2(1880f, 1040f));
        Image infoBackground = infoPanel.GetComponent<Image>();
        infoBackground.color = new Color(0.035f, 0.045f, 0.04f, 1f);
        GameObject specimenPanel = CreatePanel("SpecimenPanel", infoPanel.transform, new Vector2(0f, 0.5f), new Vector2(480f, 680f));
        SetRectPosition(specimenPanel, new Vector2(275f, 20f));
        Image specimenImage = CreateImage("Specimen", specimenPanel.transform, Vector2.zero, Vector2.one, new Vector2(18f, 90f), new Vector2(-18f, -18f), new Color(0.12f, 0.12f, 0.11f, 1f), true);
        TMP_Text specimenLabel = CreateText("SpecimenLabel", specimenPanel.transform, Vector2.zero, Vector2.one, new Vector2(25f, 150f), new Vector2(-25f, -25f), 27f, FontStyles.Bold, TextAlignmentOptions.Center);
        specimenLabel.text = "WASP SPECIMEN\n\n[ WASP ]\n\nClick Return to resume exploring";
        specimenLabel.color = new Color(0.84f, 0.66f, 0.29f, 1f);

        TMP_Text infoTitle = CreateText("InfoTitle", infoPanel.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(535f, -75f), new Vector2(-55f, -35f), 32f, FontStyles.Bold, TextAlignmentOptions.TopLeft);
        TMP_Text infoSubtitle = CreateText("InfoSubtitle", infoPanel.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(535f, -122f), new Vector2(-55f, -88f), 20f, FontStyles.Italic, TextAlignmentOptions.TopLeft);
        TMP_Text infoBody = CreateText("InfoBody", infoPanel.transform, new Vector2(0f, 0.5f), new Vector2(1f, 1f), new Vector2(535f, -80f), new Vector2(-55f, -155f), 18f, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        infoBody.textWrappingMode = TextWrappingModes.Normal;
        TMP_Text infoNode = CreateText("InfoNodeDetails", infoPanel.transform, new Vector2(0f, 0f), new Vector2(1f, 0.5f), new Vector2(535f, 120f), new Vector2(-55f, -20f), 18f, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        Button verifyButton = CreateButton("VerifyButton", infoPanel.transform, "VERIFY & LOG", new Vector2(300f, -455f), new Vector2(260f, 58f));
        Button flagButton = CreateButton("FlagButton", infoPanel.transform, "FLAG", new Vector2(600f, -455f), new Vector2(180f, 58f));
        Button infoReturnButton = CreateButton("ReturnButton", infoPanel.transform, "RETURN", new Vector2(900f, -455f), new Vector2(220f, 58f));

        GameObject skillsPanel = CreatePanel("SkillsPanel", canvas.transform, new Vector2(0.5f, 0.5f), new Vector2(1880f, 1040f));
        Image skillsBackground = skillsPanel.GetComponent<Image>();
        skillsBackground.color = new Color(0.035f, 0.045f, 0.04f, 1f);
        TMP_Text skillsTitle = CreateText("SkillsTitle", skillsPanel.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(35f, -85f), new Vector2(-35f, -35f), 30f, FontStyles.Bold, TextAlignmentOptions.TopLeft);
        skillsTitle.text = "NEST LEVEL 2 · 2 SKILL POINTS";
        TMP_Text skillResources = CreateText("SkillResources", skillsPanel.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(840f, -80f), new Vector2(-35f, -40f), 20f, FontStyles.Bold, TextAlignmentOptions.TopRight);
        skillResources.text = "NECTAR 340    PREY 90    FIBRE 120";
        TMP_Text functionalNote = CreateText("FunctionalNote", skillsPanel.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(35f, -135f), new Vector2(-35f, -105f), 17f, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        functionalNote.text = "[ ]  required functional skill for the slice";
        CreateText("ForagingHeader", skillsPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-565f, 220f), new Vector2(250f, 38f), 18f, FontStyles.Bold, TextAlignmentOptions.Center).text = "FORAGING";
        CreateText("NestHeader", skillsPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-190f, 220f), new Vector2(250f, 38f), 18f, FontStyles.Bold, TextAlignmentOptions.Center).text = "NEST";
        CreateText("DefenceHeader", skillsPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(185f, 220f), new Vector2(250f, 38f), 18f, FontStyles.Bold, TextAlignmentOptions.Center).text = "DEFENCE";
        CreateText("ColonyHeader", skillsPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(560f, 220f), new Vector2(250f, 38f), 18f, FontStyles.Bold, TextAlignmentOptions.Center).text = "COLONY";
        CreateText("EcologyHeader", skillsPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-565f, -65f), new Vector2(250f, 38f), 18f, FontStyles.Bold, TextAlignmentOptions.Center).text = "ECOLOGY";

        string[] skillNames = { "Efficient routes", "Strong fibre cells", "Alarm pheromone", "Brood care", "Keen ID", "Native stewardship" };
        string[] skillDescriptions = { "+nectar & prey yield", "+structure & repair", "calls guards faster", "+larval survival", "+1 Codex clue", "less habitat damage" };
        Vector2[] skillPositions = { new Vector2(-565f, 105f), new Vector2(-190f, 105f), new Vector2(185f, 105f), new Vector2(560f, 105f), new Vector2(-190f, -180f), new Vector2(560f, -180f) };
        for (int i = 0; i < skillNames.Length; i++)
        {
            Button skillCard = CreateButton($"SkillCard_{i}", skillsPanel.transform, $"{skillNames[i]}\n<size=16>{skillDescriptions[i]}</size>\n* 1 pt", skillPositions[i], new Vector2(330f, 122f));
            skillCard.GetComponentInChildren<TMP_Text>().alignment = TextAlignmentOptions.TopLeft;
            skillCard.GetComponentInChildren<TMP_Text>().margin = new Vector4(18f, 14f, 18f, 10f);
        }
        TMP_Text skillDetails = CreateText("SelectedSkillDetails", skillsPanel.transform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(30f, -328f), new Vector2(760f, 45f), 16f, FontStyles.Italic, TextAlignmentOptions.Left);
        skillDetails.text = "Select a skill card to preview it.";
        Button skillsReturnButton = CreateButton("SkillsReturnButton", skillsPanel.transform, "RETURN", new Vector2(630f, -327f), new Vector2(230f, 58f));

        SetReference(navigation, "waspInfoPanel", infoPanel);
        SetReference(navigation, "skillsPanel", skillsPanel);
        SetReference(navigation, "waspInfoTitle", infoTitle);
        SetReference(navigation, "waspInfoSubtitle", infoSubtitle);
        SetReference(navigation, "waspInfoBody", infoBody);
        SetReference(navigation, "waspInfoNodeDetails", infoNode);
        SetReference(navigation, "waspInfoPortrait", specimenImage);
        SetReference(navigation, "selectedSkillDetails", skillDetails);

        UnityEventTools.AddPersistentListener(skillsButton.onClick, navigation.OpenSkills);
        UnityEventTools.AddPersistentListener(infoReturnButton.onClick, navigation.CloseWaspInfo);
        UnityEventTools.AddPersistentListener(skillsReturnButton.onClick, navigation.CloseSkills);
        UnityEventTools.AddPersistentListener(verifyButton.onClick, navigation.CloseWaspInfo);
        UnityEventTools.AddPersistentListener(flagButton.onClick, navigation.CloseWaspInfo);

        infoPanel.SetActive(false);
        skillsPanel.SetActive(false);
        CreateEventSystem();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, MainWorldScenePath);
    }

    private static void SetRectPosition(GameObject gameObject, Vector2 position)
    {
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchoredPosition = position;
        }
    }

    private static void SetPanelOpacity(GameObject panel, float alpha)
    {
        Image image = panel != null ? panel.GetComponent<Image>() : null;
        if (image == null)
        {
            return;
        }

        Color color = image.color;
        color.a = alpha;
        image.color = color;
    }

    private static void CreateHexNode(
        Transform parent,
        C_MainWorldNavigation navigation,
        int index,
        Vector3 position,
        Color color,
        string habitat,
        string status)
    {
        GameObject hexObject = new GameObject($"HexNode_{index + 1}");
        hexObject.transform.SetParent(parent, false);
        hexObject.transform.localPosition = position;

        MeshFilter filter = hexObject.AddComponent<MeshFilter>();
        MeshRenderer renderer = hexObject.AddComponent<MeshRenderer>();
        filter.sharedMesh = CreateHexMesh($"HexMesh_{index + 1}", 1.08f, 0.16f);
        renderer.sharedMaterial = CreateWorldMaterial($"HexMaterial_{index + 1}", color);

        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = $"WaspCube_{index + 1}";
        cube.transform.SetParent(hexObject.transform, false);
        cube.transform.localPosition = new Vector3(0f, 0.76f, 0f);
        cube.transform.localScale = new Vector3(0.60f, 0.92f, 0.60f);
        Renderer cubeRenderer = cube.GetComponent<Renderer>();
        if (cubeRenderer != null)
        {
            cubeRenderer.sharedMaterial = CreateWorldMaterial($"WaspMaterial_{index + 1}", Color.Lerp(color, Color.white, 0.45f));
        }

        cube.AddComponent<HexTile>();
    }

    private static Mesh CreateHexMesh(string name, float radius, float height)
    {
        Mesh mesh = new Mesh { name = name };
        Vector3[] vertices = new Vector3[12];
        for (int i = 0; i < 6; i++)
        {
            float angle = Mathf.Deg2Rad * (60f * i + 30f);
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            vertices[i] = new Vector3(x, -height, z);
            vertices[i + 6] = new Vector3(x, height, z);
        }

        List<int> triangles = new List<int>();
        for (int i = 0; i < 6; i++)
        {
            int next = (i + 1) % 6;
            triangles.AddRange(new[] { i, next, i + 6, next, next + 6, i + 6 });
            triangles.AddRange(new[] { 6, next + 6, i + 6 });
            triangles.AddRange(new[] { 0, i, next });
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        return mesh;
    }

    private static Material CreateWorldMaterial(string name, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
        Material material = new Material(shader) { name = name };
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }
        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }
        return material;
    }

    private static void ConfigureBuildScenes()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(MenuScenePath, true),
            new EditorBuildSettingsScene(MainWorldScenePath, true)
        };
    }

    private static T LoadOrCreateAsset<T>(string path) where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null)
        {
            return asset;
        }

        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    private static void EnsureFolder(string parent, string folderName)
    {
        string path = $"{parent}/{folderName}";
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }

    private static Camera CreateCamera(Color backgroundColor)
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = backgroundColor;
        return camera;
    }

    private static Canvas CreateCanvas(Transform parent)
    {
        GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform));
        canvasObject.transform.SetParent(parent);
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private static void CreateEventSystem()
    {
        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<InputSystemUIInputModule>();
    }

    private static GameObject CreatePanel(string name, Transform parent, Vector2 anchor, Vector2 size)
    {
        GameObject panel = CreateRectObject(name, parent, anchor, anchor, Vector2.zero, size);
        Image image = panel.AddComponent<Image>();
        image.color = new Color(0.055f, 0.095f, 0.08f, 0.97f);
        return panel;
    }

    private static Button CreateButton(string name, Transform parent, string label, Vector2 position, Vector2 size)
    {
        GameObject buttonObject = CreateRectObject(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, size);
        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.20f, 0.30f, 0.24f, 1f);
        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.84f, 0.45f, 1f);
        colors.pressedColor = new Color(0.78f, 0.62f, 0.25f, 1f);
        colors.selectedColor = new Color(1f, 0.84f, 0.45f, 1f);
        button.colors = colors;

        TMP_Text text = CreateText("Label", buttonObject.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 24f, FontStyles.Bold, TextAlignmentOptions.Center);
        text.text = label;
        return button;
    }

    private static Slider CreateSlider(string name, Transform parent, Vector2 position, Vector2 size)
    {
        GameObject root = CreateRectObject(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, size);
        Slider slider = root.AddComponent<Slider>();

        Image background = CreateImage("Background", root.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.12f, 0.18f, 0.16f, 1f), true);
        Image fill = CreateImage("Fill", root.transform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(5f, 5f), new Vector2(-5f, -5f), new Color(0.84f, 0.66f, 0.29f, 1f), true);
        Image handle = CreateImage("Handle", root.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(28f, 46f), Color.white);

        slider.targetGraphic = handle;
        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle.rectTransform;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;
        return slider;
    }

    private static Toggle CreateToggle(string name, Transform parent, string label, Vector2 position)
    {
        GameObject root = CreateRectObject(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(520f, 55f));
        Toggle toggle = root.AddComponent<Toggle>();
        Image background = CreateImage("Background", root.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(25f, 0f), new Vector2(42f, 42f), new Color(0.12f, 0.18f, 0.16f, 1f));
        Image checkmark = CreateImage("Checkmark", background.transform, Vector2.zero, Vector2.one, new Vector2(7f, 7f), new Vector2(-7f, -7f), new Color(0.84f, 0.66f, 0.29f, 1f), true);
        TMP_Text text = CreateText("Label", root.transform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(60f, 0f), Vector2.zero, 24f, FontStyles.Normal, TextAlignmentOptions.Left);
        text.text = label;
        toggle.targetGraphic = background;
        toggle.graphic = checkmark;
        return toggle;
    }

    private static void CreateLabel(Transform parent, string textValue, Vector2 position, float size)
    {
        TMP_Text text = CreateText("Title", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(600f, 80f), size, FontStyles.Bold, TextAlignmentOptions.Center);
        text.text = textValue;
    }

    private static TMP_Text CreateText(
        string name,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 positionOrOffsetMin,
        Vector2 sizeOrOffsetMax,
        float fontSize,
        FontStyles fontStyle,
        TextAlignmentOptions alignment)
    {
        GameObject textObject = CreateRectObject(name, parent, anchorMin, anchorMax, positionOrOffsetMin, sizeOrOffsetMax);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        if (fontAsset != null)
        {
            text.font = fontAsset;
        }
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.color = new Color(0.94f, 0.96f, 0.93f, 1f);
        text.raycastTarget = false;
        return text;
    }

    private static Image CreateImage(
        string name,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 positionOrOffsetMin,
        Vector2 sizeOrOffsetMax,
        Color color,
        bool stretch = false)
    {
        GameObject imageObject = CreateRectObject(name, parent, anchorMin, anchorMax, positionOrOffsetMin, sizeOrOffsetMax);
        Image image = imageObject.AddComponent<Image>();
        image.color = color;
        if (stretch)
        {
            image.rectTransform.offsetMin = positionOrOffsetMin;
            image.rectTransform.offsetMax = sizeOrOffsetMax;
        }
        return image;
    }

    private static GameObject CreateRectObject(
        string name,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 positionOrOffsetMin,
        Vector2 sizeOrOffsetMax)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;

        if (anchorMin == anchorMax)
        {
            rect.anchoredPosition = positionOrOffsetMin;
            rect.sizeDelta = sizeOrOffsetMax;
        }
        else
        {
            rect.offsetMin = positionOrOffsetMin;
            rect.offsetMax = sizeOrOffsetMax;
        }

        return gameObject;
    }

    private static void SetReference(Object target, string propertyName, Object value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        serializedObject.Update();
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            Debug.LogError($"Could not find serialized property '{propertyName}' on {target.GetType().Name}.");
            return;
        }

        property.objectReferenceValue = value;
        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(target);
    }

    private static void SetPrivateField(Object target, string fieldName, object value)
    {
        System.Reflection.FieldInfo field = target.GetType().GetField(
            fieldName,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        if (field == null)
        {
            Debug.LogError($"Could not find field '{fieldName}' on {target.GetType().Name}.");
            return;
        }

        field.SetValue(target, value);
        EditorUtility.SetDirty(target);
    }

    private static void EnsureTextMeshProResources()
    {
        const string settingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";
        if (AssetDatabase.LoadAssetAtPath<TMP_Settings>(settingsPath) != null)
        {
            return;
        }

        TMP_PackageResourceImporter.ImportResources(true, false, false);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
    }
}
#endif
