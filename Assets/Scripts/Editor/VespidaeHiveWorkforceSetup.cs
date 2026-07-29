using TMPro;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class VespidaeHiveWorkforceSetup
{
    private const string ScenePath = "Assets/Scenes/wasp RTS Lvl.unity";
    private const string PlayerPrefabPath = "Assets/Prefabs/Friendly/Wasp_Player.prefab";
    private const string HivePrefabPath = "Assets/Prefabs/FriendlyHives/Friendly_hive.prefab";
    private const string SelectionPath = "Assets/ScriptableObjectInstances/Runtime/SO_PlayerSelection.asset";

    [MenuItem("Tools/Vespidae Wars/Setup Hive Workforce")]
    public static void Setup()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        SetupNavigation();
        SetupPlayerPrefab();
        SetupHivePrefab();
        SetupManagers();
        SetupTrainingPanel();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Hive workforce, training UI, and NavMesh setup complete.");
    }

    private static void SetupNavigation()
    {
        GameObject navigation = GameObject.Find("Navigation");
        if (navigation == null)
            navigation = new GameObject("Navigation");

        GameObject oldSurface = FindSceneObject("Wasp Navigation Surface");
        if (oldSurface != null)
            Object.DestroyImmediate(oldSurface);

        GameObject surfaceObject = new GameObject("Wasp Navigation Surface");
        surfaceObject.transform.SetParent(navigation.transform, false);
        int navigationLayer = LayerMask.NameToLayer("WaspNavigation");
        if (navigationLayer < 0)
            navigationLayer = 8;
        surfaceObject.layer = navigationLayer;

        HexTile[] tiles = Object.FindObjectsByType<HexTile>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);
        Vector3 center = Vector3.zero;
        foreach (HexTile tile in tiles)
            center += tile.transform.position;
        if (tiles.Length > 0)
            center /= tiles.Length;

        surfaceObject.transform.position = new Vector3(center.x, center.y + 0.05f, center.z);
        BoxCollider box = surfaceObject.AddComponent<BoxCollider>();
        box.size = new Vector3(60f, 0.1f, 60f);

        NavMeshSurface surface = navigation.GetComponent<NavMeshSurface>();
        if (surface == null)
            surface = navigation.AddComponent<NavMeshSurface>();
        surface.RemoveData();
        surface.collectObjects = CollectObjects.Children;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.layerMask = 1 << navigationLayer;
        surface.ignoreNavMeshAgent = true;
        surface.ignoreNavMeshObstacle = true;
        surface.BuildNavMesh();
        EditorUtility.SetDirty(surface);
    }

    private static void SetupPlayerPrefab()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
        WaspControl control = root.GetComponentInChildren<WaspControl>(true);
        NavMeshAgent agent = root.GetComponentInChildren<NavMeshAgent>(true);
        if (agent == null)
            agent = root.AddComponent<NavMeshAgent>();
        if (control != null && agent != null)
        {
            agent.enabled = false;
            agent.radius = 0.15f;
            agent.height = 0.3f;
            agent.speed = 3.5f;
            agent.acceleration = 8f;
            agent.angularSpeed = 240f;
            agent.stoppingDistance = 0.2f;
            agent.autoBraking = true;
            agent.autoRepath = true;
            agent.updatePosition = false;
            agent.updateRotation = false;
            SerializedObject serialized = new SerializedObject(control);
            serialized.FindProperty("navMeshAgent").objectReferenceValue = agent;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
        PrefabUtility.UnloadPrefabContents(root);
    }

    private static void SetupHivePrefab()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(HivePrefabPath);
        C_Friendly_Hive_Orc controller = root.GetComponent<C_Friendly_Hive_Orc>();
        Transform spawnPoint = CreateHivePoint(root, "WaspSpawnPoint", new Vector3(0f, 0.8f, 0f));
        Transform focusPoint = CreateHivePoint(root, "HiveCameraPoint", new Vector3(0f, 2.2f, -4f));
        Transform lookPoint = CreateHivePoint(root, "HiveLookPoint", new Vector3(0f, 0.5f, 0f));

        if (controller != null)
        {
            SerializedObject serialized = new SerializedObject(controller);
            serialized.FindProperty("waspSpawnPoint").objectReferenceValue = spawnPoint;
            serialized.FindProperty("cameraFocusPoint").objectReferenceValue = focusPoint;
            serialized.FindProperty("cameraLookPoint").objectReferenceValue = lookPoint;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        HiveHoverEffect hover = root.GetComponent<HiveHoverEffect>();
        if (hover != null && hover.ClickTrigger != null)
        {
            hover.ClickTrigger.isTrigger = true;
            hover.ClickTrigger.enabled = true;
        }

        PrefabUtility.SaveAsPrefabAsset(root, HivePrefabPath);
        PrefabUtility.UnloadPrefabContents(root);
    }

    private static Transform CreateHivePoint(GameObject root, string name, Vector3 worldOffset)
    {
        Transform point = root.transform.Find(name);
        if (point == null)
        {
            point = new GameObject(name).transform;
            point.SetParent(root.transform, true);
        }

        point.position = root.transform.position + worldOffset;
        point.rotation = Quaternion.identity;
        return point;
    }

    private static void SetupManagers()
    {
        ResourceManager resources = Object.FindFirstObjectByType<ResourceManager>();
        if (resources != null)
        {
            SerializedObject serialized = new SerializedObject(resources);
            serialized.FindProperty("resetResourcesOnStart").boolValue = true;
            serialized.FindProperty("startingNectar").floatValue = 50f;
            serialized.FindProperty("startingPrey").floatValue = 50f;
            serialized.FindProperty("startingFibre").floatValue = 50f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(resources);
        }

        HiveManagement hive = Object.FindFirstObjectByType<HiveManagement>();
        if (hive != null)
        {
            SerializedObject serialized = new SerializedObject(hive);
            serialized.FindProperty("playerSelection").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<SB_PlayerSelection_State>(SelectionPath);
            serialized.FindProperty("spawnFriendlyStartup").boolValue = true;
            serialized.FindProperty("spawnOneFriendlyWasp").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(hive);
        }
    }

    private static void SetupTrainingPanel()
    {
        GameObject hudRoot = GameObject.Find("MainWorldHUD");
        if (hudRoot == null)
            return;

        GameObject oldPanel = FindSceneObject("Hive Training Panel");
        if (oldPanel != null)
            Object.DestroyImmediate(oldPanel);

        GameObject panel = CreateRect(
            "Hive Training Panel",
            hudRoot.transform,
            new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f),
            new Vector2(255f, 0f),
            new Vector2(470f, 640f));
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.025f, 0.06f, 0.055f, 0.95f);
        panelImage.raycastTarget = true;

        CreateText("HiveTraining_Title", panel.transform, "Native Hive", 34f, TextAlignmentOptions.Left, new Vector2(0f, 270f), new Vector2(410f, 54f));
        CreateText("HiveTraining_Subtitle", panel.transform, "Train colony roles", 18f, TextAlignmentOptions.Left, new Vector2(0f, 230f), new Vector2(410f, 36f));
        CreateText("HiveTraining_Resources", panel.transform, "Nectar 50   Prey 50   Fibre 50", 20f, TextAlignmentOptions.Center, new Vector2(0f, 185f), new Vector2(410f, 44f));
        CreateText("HiveTraining_ScoutInfo", panel.transform, "Scout: 0 total   0 available", 18f, TextAlignmentOptions.Left, new Vector2(0f, 133f), new Vector2(410f, 34f));
        CreateButton("HiveTrain_Scout", panel.transform, "Train Scout", new Vector2(0f, 83f), new Vector2(410f, 68f));
        CreateText("HiveTraining_ForagerInfo", panel.transform, "Forager: 1 total   1 available", 18f, TextAlignmentOptions.Left, new Vector2(0f, 28f), new Vector2(410f, 34f));
        CreateButton("HiveTrain_Forager", panel.transform, "Train Forager", new Vector2(0f, -22f), new Vector2(410f, 68f));
        CreateText("HiveTraining_AttackerInfo", panel.transform, "Attacker: 0 total   0 available", 18f, TextAlignmentOptions.Left, new Vector2(0f, -77f), new Vector2(410f, 34f));
        CreateButton("HiveTrain_Attacker", panel.transform, "Train Attacker", new Vector2(0f, -127f), new Vector2(410f, 68f));
        CreateText("HiveTraining_Feedback", panel.transform, string.Empty, 16f, TextAlignmentOptions.Center, new Vector2(0f, -190f), new Vector2(410f, 44f));
        CreateButton("HiveTraining_Hide", panel.transform, "Hide", new Vector2(0f, -258f), new Vector2(180f, 50f));

        C_MainWorldOverlayNavigation overlay = hudRoot.GetComponent<C_MainWorldOverlayNavigation>();
        if (overlay != null)
        {
            SerializedObject serialized = new SerializedObject(overlay);
            serialized.FindProperty("hiveTrainingPanel").objectReferenceValue = panel;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(overlay);
        }

        panel.SetActive(false);
    }

    private static GameObject CreateRect(
        string name,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 anchoredPosition,
        Vector2 size)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.layer = 5;
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        return gameObject;
    }

    private static GameObject CreateText(
        string name,
        Transform parent,
        string value,
        float fontSize,
        TextAlignmentOptions alignment,
        Vector2 position,
        Vector2 size)
    {
        GameObject gameObject = CreateRect(
            name,
            parent,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            position,
            size);
        TextMeshProUGUI text = gameObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = new Color(0.94f, 0.96f, 0.94f, 1f);
        text.textWrappingMode = TextWrappingModes.Normal;
        return gameObject;
    }

    private static GameObject CreateButton(
        string name,
        Transform parent,
        string label,
        Vector2 position,
        Vector2 size)
    {
        GameObject gameObject = CreateRect(
            name,
            parent,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            position,
            size);
        Image image = gameObject.AddComponent<Image>();
        image.color = new Color(0.10f, 0.24f, 0.20f, 0.98f);
        Button button = gameObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.10f, 0.24f, 0.20f, 1f);
        colors.highlightedColor = new Color(0.16f, 0.38f, 0.30f, 1f);
        colors.pressedColor = new Color(0.07f, 0.17f, 0.14f, 1f);
        colors.disabledColor = new Color(0.12f, 0.14f, 0.13f, 0.65f);
        button.colors = colors;
        CreateText(
            name + "_Label",
            gameObject.transform,
            label,
            22f,
            TextAlignmentOptions.Center,
            Vector2.zero,
            size - new Vector2(20f, 10f));
        return gameObject;
    }

    private static GameObject FindSceneObject(string name)
    {
        GameObject[] objects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject gameObject in objects)
        {
            if (gameObject != null &&
                gameObject.scene.IsValid() &&
                gameObject.name == name)
            {
                return gameObject;
            }
        }

        return null;
    }
}
