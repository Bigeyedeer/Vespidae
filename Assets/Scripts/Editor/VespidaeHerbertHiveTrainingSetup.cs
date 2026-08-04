using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class VespidaeHerbertHiveTrainingSetup
{
    private const string ScenePath = "Assets/Scenes/wasp RTS Lvl.unity";
    private const string PrefabPath = "Assets/Herbert/UI/HiveTraining info.prefab";
    private const string VisualName = "Herbert Hive Training";

    private static readonly string[] RoleButtons =
    {
        "HiveTrain_Scout",
        "HiveTrain_Forager",
        "HiveTrain_Builder",
        "HiveTrain_Attacker"
    };

    private static readonly string[] RoleInfo =
    {
        "HiveTraining_ScoutInfo",
        "HiveTraining_ForagerInfo",
        "HiveTraining_BuilderInfo",
        "HiveTraining_AttackerInfo"
    };

    private static readonly string[] RoleLabels =
    {
        "Train Scout",
        "Train Forager",
        "Train Builder",
        "Train Attacker"
    };

    [MenuItem("Tools/Vespidae Wars/Replace Hive Training With Herbert UI")]
    public static void Setup()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject panel = FindSceneObject(scene, "Hive Training Panel");
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);

        if (panel == null || source == null)
        {
            Debug.LogError("The Hive Training Panel or Herbert HiveTraining info prefab was not found.");
            return;
        }

        for (int index = panel.transform.childCount - 1; index >= 0; index--)
            Object.DestroyImmediate(panel.transform.GetChild(index).gameObject);

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 0.5f);
        panelRect.anchorMax = new Vector2(0f, 0.5f);
        panelRect.pivot = new Vector2(0f, 0.5f);
        panelRect.anchoredPosition = new Vector2(24f, 0f);
        panelRect.sizeDelta = new Vector2(500f, 687.2f);
        panelRect.localScale = Vector3.one;

        Image oldBackground = panel.GetComponent<Image>();
        if (oldBackground != null)
        {
            oldBackground.enabled = false;
            oldBackground.raycastTarget = false;
        }

        GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(source, panel.transform);
        visual.name = VisualName;
        RectTransform visualRect = visual.GetComponent<RectTransform>();
        visualRect.anchorMin = new Vector2(0.5f, 0.5f);
        visualRect.anchorMax = new Vector2(0.5f, 0.5f);
        visualRect.pivot = new Vector2(0.5f, 0.5f);
        visualRect.anchoredPosition = Vector2.zero;
        visualRect.sizeDelta = new Vector2(500f, 687.2f);
        visualRect.localScale = Vector3.one;

        Transform titleTag = FindDescendant(visual.transform, "nametag");
        Transform frame = FindDescendant(visual.transform, "Location Info stuff");
        Transform content = frame != null ? FindDescendant(frame, "content") : null;
        Transform buttonGroup = content != null ? FindDescendant(content, "buttons") : null;
        TMP_Text title = titleTag != null ? titleTag.GetComponentInChildren<TMP_Text>(true) : null;
        TMP_Text subtitle = FindText(content, "scouted");
        TMP_Text resources = FindText(content, "valuecontent");

        Button[] trainingButtons = buttonGroup == null
            ? new Button[0]
            : buttonGroup.GetComponentsInChildren<Button>(true)
                .OrderBy(button => button.transform.GetSiblingIndex())
                .Take(4)
                .ToArray();

        Button hideButton = visual.GetComponentsInChildren<Button>(true)
            .FirstOrDefault(button =>
                trainingButtons.All(training => training != button) &&
                button.transform != titleTag &&
                (titleTag == null || !button.transform.IsChildOf(titleTag)));

        if (title == null || subtitle == null || resources == null || trainingButtons.Length != 4 || hideButton == null)
        {
            Object.DestroyImmediate(visual);
            Debug.LogError("The Herbert HiveTraining info hierarchy did not match the expected design.");
            return;
        }

        title.gameObject.name = "HiveTraining_Title";
        subtitle.gameObject.name = "HiveTraining_Subtitle";
        resources.gameObject.name = "HiveTraining_Resources";
        ConfigureHeader(title, "Native Hive", 30f);
        ConfigureHeader(subtitle, "Train colony roles", 18f);
        ConfigureHeader(resources, "Nectar 0   Prey 0   Fibre 0", 16f);

        for (int index = 0; index < trainingButtons.Length; index++)
            ConfigureTrainingButton(trainingButtons[index], RoleButtons[index], RoleInfo[index], RoleLabels[index]);

        ConfigureHideButton(hideButton);
        DisableUnusedContentText(content, subtitle, resources, trainingButtons);
        DisableUnusedContent(visual.transform, frame, titleTag, hideButton.transform);
        SetLayerRecursively(visual, panel.layer);

        panel.SetActive(false);
        EditorUtility.SetDirty(panel);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Hive Training Panel now uses Herbert's HiveTraining info design.");
    }

    private static void ConfigureTrainingButton(Button button, string buttonName, string infoName, string label)
    {
        button.gameObject.name = buttonName;
        TMP_Text info = button.GetComponentsInChildren<TMP_Text>(true)
            .FirstOrDefault(text => text.gameObject.name == "scouted");
        TMP_Text buttonLabel = button.GetComponentsInChildren<TMP_Text>(true)
            .FirstOrDefault(text => text != info);

        if (info != null)
        {
            info.gameObject.name = infoName;
            info.text = label.Replace("Train ", string.Empty) + "  •  0 total  •  0 available";
            info.alignment = TextAlignmentOptions.Center;
            info.enableAutoSizing = true;
            info.fontSizeMin = 10f;
            info.fontSizeMax = 16f;
            info.margin = Vector4.zero;
            Record(info.gameObject);
            Record(info);
        }

        if (buttonLabel != null)
        {
            buttonLabel.gameObject.name = buttonName + "_Label";
            buttonLabel.text = label;
            buttonLabel.alignment = TextAlignmentOptions.Center;
            buttonLabel.enableAutoSizing = true;
            buttonLabel.fontSizeMin = 16f;
            buttonLabel.fontSizeMax = 28f;
            Record(buttonLabel.gameObject);
            Record(buttonLabel);
        }

        button.onClick = new Button.ButtonClickedEvent();
        Record(button.gameObject);
        Record(button);
    }

    private static void ConfigureHideButton(Button button)
    {
        button.gameObject.name = "HiveTraining_Hide";
        TMP_Text feedback = button.GetComponentsInChildren<TMP_Text>(true)
            .FirstOrDefault(text => text.gameObject.name == "scouted");
        TMP_Text label = button.GetComponentsInChildren<TMP_Text>(true)
            .FirstOrDefault(text => text != feedback);

        if (feedback != null)
        {
            feedback.gameObject.name = "HiveTraining_Feedback";
            feedback.text = string.Empty;
            feedback.alignment = TextAlignmentOptions.Center;
            feedback.enableAutoSizing = true;
            feedback.fontSizeMin = 11f;
            feedback.fontSizeMax = 16f;
            feedback.margin = Vector4.zero;
            Record(feedback.gameObject);
            Record(feedback);
        }

        if (label != null)
        {
            label.gameObject.name = "HiveTraining_Hide_Label";
            label.text = "Hide";
            label.alignment = TextAlignmentOptions.Center;
            label.enableAutoSizing = true;
            label.fontSizeMin = 16f;
            label.fontSizeMax = 28f;
            Record(label.gameObject);
            Record(label);
        }

        button.onClick = new Button.ButtonClickedEvent();
        Record(button.gameObject);
        Record(button);
    }

    private static void ConfigureHeader(TMP_Text text, string value, float maximumSize)
    {
        text.text = value;
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = true;
        text.fontSizeMin = 12f;
        text.fontSizeMax = maximumSize;
        text.margin = Vector4.zero;
        Record(text.gameObject);
        Record(text);
    }

    private static void DisableUnusedContent(Transform root, Transform frame, Transform titleTag, Transform hideButton)
    {
        foreach (Transform child in root)
        {
            bool keep = child == frame || child == titleTag || child == hideButton || hideButton.IsChildOf(child);
            child.gameObject.SetActive(keep);
            Record(child.gameObject);
        }
    }

    private static void DisableUnusedContentText(
        Transform content,
        TMP_Text subtitle,
        TMP_Text resources,
        Button[] trainingButtons)
    {
        TMP_Text[] texts = content.GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text text in texts)
        {
            bool belongsToButton = trainingButtons.Any(button => text.transform.IsChildOf(button.transform));
            bool keep = text == subtitle || text == resources || belongsToButton;
            text.gameObject.SetActive(keep);
            Record(text.gameObject);
        }
    }

    private static void Record(Object target)
    {
        EditorUtility.SetDirty(target);
        PrefabUtility.RecordPrefabInstancePropertyModifications(target);
    }

    private static TMP_Text FindText(Transform root, string name)
    {
        Transform match = FindDescendant(root, name);
        return match != null ? match.GetComponent<TMP_Text>() : null;
    }

    private static Transform FindDescendant(Transform root, string name)
    {
        if (root == null)
            return null;

        return root.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(child => child.name == name);
    }

    private static void SetLayerRecursively(GameObject root, int layer)
    {
        root.layer = layer;
        foreach (Transform child in root.transform)
            SetLayerRecursively(child.gameObject, layer);
    }

    private static GameObject FindSceneObject(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform match = root.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(child => child.name == objectName);
            if (match != null)
                return match.gameObject;
        }

        return null;
    }
}
