using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class VespidaeControlGroupsSetup
{
    private const string ScenePath = "Assets/Scenes/wasp RTS Lvl.unity";
    private const string FriendlyWaspPath = "Assets/Prefabs/Friendly/Wasp_Player.prefab";
    private const string ButtonPrefabPath = "Assets/herbert/UI/Button Variant.prefab";

    [MenuItem("Tools/Vespidae Wars/Setup Combat Response And Control Groups")]
    public static void Setup()
    {
        ConfigureFriendlyWaspPrefab();
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        ConfigureScene(scene);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Vespidae combat response and control groups setup complete.");
    }

    [MenuItem("Tools/Vespidae Wars/Audit Combat Response And Control Groups")]
    public static void Audit()
    {
        List<string> errors = new List<string>();
        GameObject managerObject = GameObject.Find("Game Manager");
        WaspControlGroupManager manager = managerObject != null ? managerObject.GetComponent<WaspControlGroupManager>() : null;
        if (manager == null)
            errors.Add("Game Manager is missing WaspControlGroupManager.");
        else
        {
            SerializedObject serialized = new SerializedObject(manager);
            Require(serialized, "cameraFocus", "WaspControlGroupManager", errors);
            Require(serialized, "tutorialManager", "WaspControlGroupManager", errors);
            Require(serialized, "selectionCanvas", "WaspControlGroupManager", errors);
            Require(serialized, "selectionBox", "WaspControlGroupManager", errors);
            Require(serialized, "feedbackText", "WaspControlGroupManager", errors);
            SerializedProperty bindings = serialized.FindProperty("groupBindings");
            if (bindings == null || bindings.arraySize != 5)
                errors.Add("WaspControlGroupManager requires five group bindings.");
            else
            {
                for (int index = 0; index < bindings.arraySize; index++)
                {
                    SerializedProperty binding = bindings.GetArrayElementAtIndex(index);
                    Require(binding, "button", $"Control group {index + 1}", errors);
                    Require(binding, "label", $"Control group {index + 1}", errors);
                }
            }
        }

        HexMouseRaycaster raycaster = managerObject != null ? managerObject.GetComponent<HexMouseRaycaster>() : null;
        if (raycaster == null)
            errors.Add("Game Manager is missing HexMouseRaycaster.");
        else
            Require(new SerializedObject(raycaster), "controlGroupManager", "HexMouseRaycaster", errors);

        GameObject friendlyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FriendlyWaspPath);
        WaspControl control = friendlyPrefab != null ? friendlyPrefab.GetComponent<WaspControl>() : null;
        if (control == null)
            errors.Add("Friendly wasp prefab is missing WaspControl.");
        else
            Require(new SerializedObject(control), "selectionIndicatorRoot", "Friendly wasp prefab", errors);

        if (errors.Count == 0)
            Debug.Log("AUDIT PASS: Combat response and control-group references are valid.");
        else
        {
            foreach (string error in errors)
                Debug.LogError($"AUDIT FAIL: {error}");
            Debug.LogError($"AUDIT FAILED with {errors.Count} issue(s).");
        }
    }

    private static void ConfigureFriendlyWaspPrefab()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(FriendlyWaspPath);
        try
        {
            WaspControl control = root.GetComponent<WaspControl>();
            WaspRoleIconBillboard billboard = root.GetComponentInChildren<WaspRoleIconBillboard>(true);
            if (control == null || billboard == null)
                return;

            Transform existing = billboard.transform.Find("Selection Indicator");
            GameObject indicator = existing != null ? existing.gameObject : CreateSelectionIndicator(billboard.transform);
            indicator.transform.SetAsFirstSibling();
            indicator.SetActive(false);
            SetReference(control, "selectionIndicatorRoot", indicator);
            PrefabUtility.SaveAsPrefabAsset(root, FriendlyWaspPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static GameObject CreateSelectionIndicator(Transform parent)
    {
        GameObject indicator = new GameObject("Selection Indicator", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rect = indicator.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(74f, 74f);
        Image image = indicator.GetComponent<Image>();
        image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        image.type = Image.Type.Sliced;
        image.color = new Color(1f, 0.78f, 0.05f, 0.34f);
        image.raycastTarget = false;
        return indicator;
    }

    private static void ConfigureScene(Scene scene)
    {
        GameObject managerObject = GameObject.Find("Game Manager");
        GameObject hudObject = FindSceneObject(scene, "OnMapHUD");
        if (managerObject == null || hudObject == null)
            return;

        WaspControlGroupManager manager = managerObject.GetComponent<WaspControlGroupManager>();
        if (manager == null)
            manager = managerObject.AddComponent<WaspControlGroupManager>();

        GameObject groupHud = FindSceneObject(scene, "Control Groups HUD");
        if (groupHud == null)
            groupHud = CreateGroupHud(hudObject.transform);

        ConfigureGroupHudLayout(groupHud);

        RectTransform selectionBox = EnsureSelectionBox(hudObject.transform);
        TMP_Text feedback = EnsureFeedback(groupHud.transform);
        WaspControlGroupHudBinding[] bindings = BuildGroupBindings(groupHud.transform);
        LayoutRebuilder.ForceRebuildLayoutImmediate(groupHud.GetComponent<RectTransform>());

        SerializedObject serialized = new SerializedObject(manager);
        serialized.FindProperty("cameraFocus").objectReferenceValue = managerObject.GetComponent<C_MainWorldCameraFocus>();
        serialized.FindProperty("tutorialManager").objectReferenceValue = Object.FindFirstObjectByType<C_TutorialManager>();
        serialized.FindProperty("selectionCanvas").objectReferenceValue = hudObject.GetComponent<RectTransform>();
        serialized.FindProperty("selectionBox").objectReferenceValue = selectionBox;
        serialized.FindProperty("feedbackText").objectReferenceValue = feedback;
        SerializedProperty groups = serialized.FindProperty("groupBindings");
        groups.arraySize = 5;
        for (int index = 0; index < groups.arraySize; index++)
        {
            SerializedProperty target = groups.GetArrayElementAtIndex(index);
            target.FindPropertyRelative("button").objectReferenceValue = bindings[index].Button;
            target.FindPropertyRelative("label").objectReferenceValue = bindings[index].Label;
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();

        HexMouseRaycaster raycaster = managerObject.GetComponent<HexMouseRaycaster>();
        SetReference(raycaster, "controlGroupManager", manager);
    }

    private static GameObject CreateGroupHud(Transform parent)
    {
        GameObject hud = new GameObject("Control Groups HUD", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        RectTransform rect = hud.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        HorizontalLayoutGroup layout = hud.GetComponent<HorizontalLayoutGroup>();
        ConfigureGroupHudLayout(hud);
        return hud;
    }

    private static void ConfigureGroupHudLayout(GameObject hud)
    {
        RectTransform rect = hud.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 84f);
        rect.sizeDelta = new Vector2(350f, 56f);

        HorizontalLayoutGroup layout = hud.GetComponent<HorizontalLayoutGroup>();
        if (layout == null)
            layout = hud.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 8f;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
    }

    private static WaspControlGroupHudBinding[] BuildGroupBindings(Transform hud)
    {
        WaspControlGroupHudBinding[] bindings = new WaspControlGroupHudBinding[5];
        GameObject buttonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ButtonPrefabPath);
        for (int index = 0; index < bindings.Length; index++)
        {
            Transform existing = hud.Find($"ControlGroup_{index + 1}");
            GameObject buttonObject = existing != null
                ? existing.gameObject
                : buttonPrefab != null
                    ? PrefabUtility.InstantiatePrefab(buttonPrefab, hud) as GameObject
                    : new GameObject($"ControlGroup_{index + 1}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.name = $"ControlGroup_{index + 1}";
            if (buttonObject.transform.parent != hud)
                buttonObject.transform.SetParent(hud, false);

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(62f, 48f);
            rect.localScale = Vector3.one;
            Button button = buttonObject.GetComponent<Button>();
            if (button == null)
                button = buttonObject.AddComponent<Button>();
            Image hitArea = buttonObject.GetComponent<Image>();
            if (hitArea == null)
                hitArea = buttonObject.AddComponent<Image>();
            hitArea.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            hitArea.type = Image.Type.Sliced;
            hitArea.color = new Color(1f, 1f, 1f, 0f);
            hitArea.raycastTarget = true;
            button.targetGraphic = hitArea;
            TMP_Text label = buttonObject.GetComponentInChildren<TMP_Text>(true);
            if (label == null)
                label = CreateLabel(buttonObject.transform, $"{index + 1}\n0");
            label.text = $"{index + 1}\n0";
            label.fontSize = 18f;
            label.alignment = TextAlignmentOptions.Center;

            Transform markerTransform = buttonObject.transform.Find("Selected Marker");
            if (markerTransform != null)
                Object.DestroyImmediate(markerTransform.gameObject);
            WaspControlGroupHudBinding binding = new WaspControlGroupHudBinding();
            bindings[index] = binding;
            SetBinding(binding, button, label);
        }
        return bindings;
    }

    private static void SetBinding(WaspControlGroupHudBinding binding, Button button, TMP_Text label)
    {
        binding.Configure(button, label);
    }

    private static TMP_Text CreateLabel(Transform parent, string value)
    {
        GameObject labelObject = new GameObject("Group Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform rect = labelObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        TMP_Text label = labelObject.GetComponent<TMP_Text>();
        label.text = value;
        label.color = Color.white;
        label.raycastTarget = false;
        return label;
    }

    private static RectTransform EnsureSelectionBox(Transform parent)
    {
        Transform existing = parent.Find("Drag Selection Box");
        GameObject box = existing != null ? existing.gameObject : new GameObject("Drag Selection Box", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
        RectTransform rect = box.GetComponent<RectTransform>();
        if (rect.parent != parent)
            rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        Image image = box.GetComponent<Image>();
        image.color = new Color(1f, 0.75f, 0.05f, 0.12f);
        image.raycastTarget = false;
        Outline outline = box.GetComponent<Outline>();
        outline.effectColor = new Color(1f, 0.8f, 0.1f, 0.9f);
        outline.effectDistance = new Vector2(2f, -2f);
        box.transform.SetAsLastSibling();
        box.SetActive(false);
        return rect;
    }

    private static TMP_Text EnsureFeedback(Transform hud)
    {
        Transform existing = hud.Find("Control Group Feedback");
        TMP_Text feedback = existing != null ? existing.GetComponent<TMP_Text>() : null;
        if (feedback != null)
            return feedback;

        GameObject feedbackObject = new GameObject("Control Group Feedback", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform rect = feedbackObject.GetComponent<RectTransform>();
        rect.SetParent(hud, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 54f);
        rect.sizeDelta = new Vector2(500f, 28f);
        feedback = feedbackObject.GetComponent<TMP_Text>();
        feedback.text = string.Empty;
        feedback.fontSize = 18f;
        feedback.alignment = TextAlignmentOptions.Center;
        feedback.color = Color.white;
        feedback.raycastTarget = false;
        LayoutElement layout = feedbackObject.AddComponent<LayoutElement>();
        layout.ignoreLayout = true;
        return feedback;
    }

    private static GameObject FindSceneObject(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
            {
                if (transform.name == name)
                    return transform.gameObject;
            }
        }
        return null;
    }

    private static void SetReference(Object target, string propertyName, Object value)
    {
        if (target == null)
            return;
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property != null)
            property.objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Require(SerializedObject serialized, string propertyName, string owner, List<string> errors)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null || property.objectReferenceValue == null)
            errors.Add($"{owner}.{propertyName} is empty.");
    }

    private static void Require(SerializedProperty parent, string propertyName, string owner, List<string> errors)
    {
        SerializedProperty property = parent.FindPropertyRelative(propertyName);
        if (property == null || property.objectReferenceValue == null)
            errors.Add($"{owner}.{propertyName} is empty.");
    }
}
