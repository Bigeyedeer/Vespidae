using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class VespidaeHerbertHudStyleSetup
{
    private const string ScenePath = "Assets/Scenes/wasp RTS Lvl.unity";
    private const string PanelPrefabPath = "Assets/Herbert/UI/Squarish Panel.prefab";
    private const string ButtonPrefabPath = "Assets/Herbert/UI/Button Variant.prefab";
    private const string PanelShellName = "HerbertSquarishPanel";
    private const string ButtonVisualName = "HerbertButtonVisual";

    // ObjectivePanel and TerritoryPanel were removed from the HUD to reduce clutter; the hex menu
    // already carries the territory information. Listing them here would recreate them.
    private static readonly string[] PanelNames =
    {
        "HabitatPanel",
        "ColonyPanel",
        // The skills screen and the wasp action card were built before the Herbert set existed and
        // still carried their own flat visuals, which read as a different game next to the rest.
        "SkillsPanel",
        "FriendlyWaspActionsCard"
    };

    private static readonly string[] ButtonNames =
    {
        // Action_Scout and Action_Contain were removed from the bar - scouting is ordered from the
        // hex panel and containment was never wired. Listing them here would recreate them.
        "Action_Codex",
        "Action_Protect",
        "Action_Return",
        "Skills_Return",
        "Skills_Card_0",
        "Skills_Card_1",
        "Skills_Card_2",
        "Skills_Card_3",
        "Skills_Card_4",
        "Skills_Card_5"
    };

    [MenuItem("Tools/Vespidae Wars/Use Herbert HUD Panels and Buttons")]
    public static void Setup()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject panelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PanelPrefabPath);
        GameObject buttonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ButtonPrefabPath);

        if (panelPrefab == null || buttonPrefab == null)
        {
            Debug.LogError("The Herbert Squarish Panel or Button Variant prefab was not found.");
            return;
        }

        foreach (string panelName in PanelNames)
        {
            GameObject panel = FindSceneObject(scene, panelName);
            if (panel == null)
            {
                Debug.LogError($"{panelName} was not found in {ScenePath}.");
                continue;
            }

            ApplyPanelStyle(scene, panel, panelPrefab);
        }

        foreach (string buttonName in ButtonNames)
        {
            GameObject button = FindSceneObject(scene, buttonName);
            if (button == null)
            {
                Debug.LogError($"{buttonName} was not found in {ScenePath}.");
                continue;
            }

            ApplyButtonStyle(scene, button, buttonPrefab);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("The MainWorld HUD now uses Herbert Squarish Panel and Button Variant visuals.");
    }

    private static void ApplyPanelStyle(Scene scene, GameObject panel, GameObject panelPrefab)
    {
        RemoveDirectChild(panel.transform, PanelShellName);

        GameObject shell = (GameObject)PrefabUtility.InstantiatePrefab(panelPrefab, scene);
        shell.name = PanelShellName;
        shell.transform.SetParent(panel.transform, false);
        StretchToParent(shell.GetComponent<RectTransform>());
        shell.transform.SetAsFirstSibling();
        SetLayerRecursively(shell, panel.layer);

        foreach (Graphic graphic in shell.GetComponentsInChildren<Graphic>(true))
            graphic.raycastTarget = false;

        Image originalImage = panel.GetComponent<Image>();
        if (originalImage != null)
        {
            Color color = originalImage.color;
            color.a = 0f;
            originalImage.color = color;
            originalImage.raycastTarget = true;
        }

        EditorUtility.SetDirty(panel);
    }

    private static void ApplyButtonStyle(Scene scene, GameObject buttonObject, GameObject buttonPrefab)
    {
        RemoveDirectChild(buttonObject.transform, ButtonVisualName);

        string labelText = ReadButtonLabel(buttonObject);
        GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(buttonPrefab, scene);
        visual.name = ButtonVisualName;
        visual.transform.SetParent(buttonObject.transform, false);
        StretchToParent(visual.GetComponent<RectTransform>());
        visual.transform.SetAsFirstSibling();
        SetLayerRecursively(visual, buttonObject.layer);

        foreach (Button nestedButton in visual.GetComponentsInChildren<Button>(true))
            nestedButton.enabled = false;

        foreach (Graphic graphic in visual.GetComponentsInChildren<Graphic>(true))
            graphic.raycastTarget = false;

        TMP_Text variantLabel = visual.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault();
        if (variantLabel != null)
        {
            variantLabel.text = labelText;
            variantLabel.alignment = TextAlignmentOptions.Center;
            variantLabel.enableAutoSizing = true;
            variantLabel.fontSizeMin = 14f;
            variantLabel.fontSizeMax = 24f;
        }

        foreach (Text oldLabel in buttonObject.GetComponentsInChildren<Text>(true))
        {
            if (!oldLabel.transform.IsChildOf(visual.transform))
                oldLabel.enabled = false;
        }

        foreach (TMP_Text oldLabel in buttonObject.GetComponentsInChildren<TMP_Text>(true))
        {
            if (!oldLabel.transform.IsChildOf(visual.transform))
                oldLabel.enabled = false;
        }

        Image originalImage = buttonObject.GetComponent<Image>();
        if (originalImage != null)
        {
            Color color = originalImage.color;
            color.a = 0f;
            originalImage.color = color;
            originalImage.raycastTarget = true;
        }

        Button button = buttonObject.GetComponent<Button>();
        Image target = visual.GetComponentsInChildren<Image>(true)
            .FirstOrDefault(image => image.gameObject.name == "Image (1)");

        if (button != null && target != null)
        {
            button.targetGraphic = target;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.9f, 0.68f, 1f);
            colors.pressedColor = new Color(0.78f, 0.66f, 0.48f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.55f);
            button.colors = colors;
        }

        EditorUtility.SetDirty(buttonObject);
    }

    private static string ReadButtonLabel(GameObject buttonObject)
    {
        Text oldText = buttonObject.GetComponentsInChildren<Text>(true)
            .FirstOrDefault();

        if (oldText != null && !string.IsNullOrWhiteSpace(oldText.text))
            return oldText.text;

        TMP_Text oldTmp = buttonObject.GetComponentsInChildren<TMP_Text>(true)
            .FirstOrDefault();

        if (oldTmp != null && !string.IsNullOrWhiteSpace(oldTmp.text))
            return oldTmp.text;

        return buttonObject.name.Replace("Action_", string.Empty);
    }

    private static void StretchToParent(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;
    }

    private static void RemoveDirectChild(Transform parent, string childName)
    {
        Transform child = parent.Cast<Transform>().FirstOrDefault(item => item.name == childName);
        if (child != null)
            Object.DestroyImmediate(child.gameObject);
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
