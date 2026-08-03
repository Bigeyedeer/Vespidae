using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class VespidaeHerbertHexOptionsSetup
{
    private const string ScenePath = "Assets/Scenes/wasp RTS Lvl.unity";
    private const string PrefabPath = "Assets/Herbert/UI/Location info.prefab";
    private const string VisualName = "Herbert Location Information";

    [MenuItem("Tools/Vespidae Wars/Replace Hex Options With Herbert UI")]
    public static void Setup()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        HexOptionsPanel panel = Object.FindObjectsByType<HexOptionsPanel>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None)
            .FirstOrDefault();

        if (panel == null)
        {
            Debug.LogError("Hex Options Panel was not found in wasp RTS Lvl.");
            return;
        }

        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (source == null)
        {
            Debug.LogError("Herbert Location info prefab was not found.");
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

        GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(source, panel.transform);
        visual.name = VisualName;
        RectTransform visualRect = visual.GetComponent<RectTransform>();
        visualRect.anchorMin = new Vector2(0.5f, 0.5f);
        visualRect.anchorMax = new Vector2(0.5f, 0.5f);
        visualRect.pivot = new Vector2(0.5f, 0.5f);
        visualRect.anchoredPosition = Vector2.zero;
        visualRect.sizeDelta = new Vector2(500f, 687.2f);
        visualRect.localScale = Vector3.one;
        ConfigureLocationFrame(visual.transform);

        TMP_Text[] texts = visual.GetComponentsInChildren<TMP_Text>(true);
        TMP_Text nameText = texts.FirstOrDefault(text => text.text == "Unexplored");
        TMP_Text stateText = texts.FirstOrDefault(text => text.gameObject.name == "scouted");
        Button[] buttons = visual.GetComponentsInChildren<Button>(true);
        Button primaryButton = FindButton(buttons, "Scout");
        Button closeButton = FindButton(buttons, "Close");

        if (nameText == null || stateText == null || primaryButton == null || closeButton == null)
        {
            Object.DestroyImmediate(visual);
            Debug.LogError("The Herbert Location info hierarchy did not match the expected design.");
            return;
        }

        Transform content = stateText.transform.parent;
        RectTransform actionRow = primaryButton.transform.parent as RectTransform;
        GameObject detailsObject = Object.Instantiate(stateText.gameObject, content);
        detailsObject.name = "Hex Details";
        TMP_Text detailsText = detailsObject.GetComponent<TMP_Text>();

        for (int index = content.childCount - 1; index >= 0; index--)
        {
            Transform child = content.GetChild(index);
            bool keep = child == stateText.transform || child == detailsObject.transform || child == actionRow;
            child.gameObject.SetActive(keep);
        }

        ConfigureStateText(stateText);
        ConfigureDetailsText(detailsText);
        ConfigureActionRow(actionRow);
        ConfigureButton(primaryButton, "Send Scout");
        ConfigureButton(closeButton, "Close");

        nameText.text = "Territory";
        stateText.text = "Status: Unknown";
        detailsText.text = "Contents: Unknown\n\nSend one Scout to survey this territory.";

        SerializedObject serialized = new SerializedObject(panel);
        serialized.FindProperty("hexNameText").objectReferenceValue = nameText;
        serialized.FindProperty("stateText").objectReferenceValue = stateText;
        serialized.FindProperty("discoveryText").objectReferenceValue = detailsText;
        serialized.FindProperty("primaryActionButton").objectReferenceValue = primaryButton;
        serialized.FindProperty("primaryActionButtonText").objectReferenceValue = primaryButton.GetComponentInChildren<TMP_Text>(true);
        serialized.FindProperty("closeActionButton").objectReferenceValue = closeButton;
        serialized.FindProperty("actionButtonContainer").objectReferenceValue = actionRow;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        panel.gameObject.SetActive(false);
        EditorUtility.SetDirty(panel);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Hex Options Panel now uses Herbert's Location info design.");
    }

    private static Button FindButton(Button[] buttons, string label)
    {
        return buttons.FirstOrDefault(button =>
        {
            TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
            return text != null && text.text == label;
        });
    }

    private static void ConfigureStateText(TMP_Text text)
    {
        RectTransform rect = text.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -24f);
        rect.sizeDelta = new Vector2(350f, 42f);
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 18f;
        text.enableAutoSizing = false;
    }

    private static void ConfigureLocationFrame(Transform visual)
    {
        VerticalLayoutGroup layout = visual.GetComponent<VerticalLayoutGroup>();
        if (layout != null)
            layout.enabled = false;

        RectTransform title = visual.childCount > 0 ? visual.GetChild(0) as RectTransform : null;
        RectTransform frame = visual.Find("Location Info stuff") as RectTransform;
        if (title == null || frame == null)
            return;

        title.anchorMin = new Vector2(0.5f, 0.5f);
        title.anchorMax = new Vector2(0.5f, 0.5f);
        title.pivot = new Vector2(0.5f, 0.5f);
        title.anchoredPosition = new Vector2(0f, 265f);
        title.sizeDelta = new Vector2(454.7f, 100f);

        frame.anchorMin = new Vector2(0.5f, 0.5f);
        frame.anchorMax = new Vector2(0.5f, 0.5f);
        frame.pivot = new Vector2(0.5f, 0.5f);
        frame.anchoredPosition = Vector2.zero;
        frame.sizeDelta = new Vector2(400f, 430f);
    }

    private static void ConfigureDetailsText(TMP_Text text)
    {
        RectTransform rect = text.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(42f, 105f);
        rect.offsetMax = new Vector2(-42f, -82f);
        rect.localScale = Vector3.one;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.fontSize = 16f;
        text.enableAutoSizing = true;
        text.fontSizeMin = 12f;
        text.fontSizeMax = 16f;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.margin = new Vector4(4f, 4f, 4f, 4f);
    }

    private static void ConfigureActionRow(RectTransform row)
    {
        row.anchorMin = new Vector2(0.5f, 0f);
        row.anchorMax = new Vector2(0.5f, 0f);
        row.pivot = new Vector2(0.5f, 0f);
        row.anchoredPosition = new Vector2(0f, 24f);
        row.sizeDelta = new Vector2(430f, 58f);
        row.localScale = Vector3.one;

        HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
        if (layout == null)
            layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 8f;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
    }

    private static void ConfigureButton(Button button, string label)
    {
        RectTransform rect = button.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(200f, 90f);
        rect.localScale = Vector3.one * 0.5f;
        TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
        if (text == null)
            return;
        text.text = label;
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = true;
        text.fontSizeMin = 18f;
        text.fontSizeMax = 30f;
    }
}
