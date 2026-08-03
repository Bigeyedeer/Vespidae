using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class VespidaeHerbertResourceBarSetup
{
    private const string ScenePath = "Assets/Scenes/wasp RTS Lvl.unity";
    private const string PrefabPath = "Assets/Herbert/UI/Stats Variant.prefab";

    private static readonly string[] Labels =
    {
        "Nectar",
        "Prey",
        "Fibre",
        "Workers",
        "Strength",
        "Brood"
    };

    private static readonly string[] Values =
    {
        "150",
        "150",
        "300",
        "1",
        "600",
        "150/0"
    };

    [MenuItem("Tools/Vespidae Wars/Use Herbert Resource Stats")]
    public static void Setup()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject resourceBar = FindSceneObject(scene, "ResourceBar");
        GameObject statsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);

        if (resourceBar == null || statsPrefab == null)
        {
            Debug.LogError("The ResourceBar or Herbert Stats Variant prefab was not found.");
            return;
        }

        RectTransform barRect = resourceBar.GetComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0.5f, 1f);
        barRect.anchorMax = new Vector2(0.5f, 1f);
        barRect.pivot = new Vector2(0.5f, 1f);
        barRect.anchoredPosition = new Vector2(0f, -6f);
        barRect.sizeDelta = new Vector2(660f, 106f);

        Image barImage = resourceBar.GetComponent<Image>();
        if (barImage != null)
            Object.DestroyImmediate(barImage);

        foreach (Transform child in resourceBar.transform.Cast<Transform>().ToArray())
            Object.DestroyImmediate(child.gameObject);

        HorizontalLayoutGroup existingLayout = resourceBar.GetComponent<HorizontalLayoutGroup>();
        if (existingLayout != null)
            Object.DestroyImmediate(existingLayout);

        HorizontalLayoutGroup layout = resourceBar.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(5, 5, 3, 3);
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        for (int i = 0; i < Labels.Length; i++)
            CreateStatCard(scene, resourceBar.transform, statsPrefab, i);

        EditorUtility.SetDirty(resourceBar);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("The MainWorld resource bar now uses six Herbert Stats Variant cards.");
    }

    private static void CreateStatCard(
        Scene scene,
        Transform parent,
        GameObject statsPrefab,
        int index)
    {
        GameObject card = (GameObject)PrefabUtility.InstantiatePrefab(statsPrefab, scene);
        card.name = $"ResourceCard_{index}";
        card.transform.SetParent(parent, false);

        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.anchoredPosition = Vector2.zero;
        cardRect.sizeDelta = new Vector2(100f, 100f);
        cardRect.localScale = Vector3.one;

        LayoutElement element = card.GetComponent<LayoutElement>();
        if (element == null)
            element = card.AddComponent<LayoutElement>();
        element.preferredWidth = 100f;
        element.preferredHeight = 100f;

        TMP_Text[] texts = card.GetComponentsInChildren<TMP_Text>(true);
        TMP_Text title = texts.FirstOrDefault(text => text.text == "Text");
        TMP_Text value = texts.FirstOrDefault(text => text.text == "100000");

        if (title == null || value == null)
        {
            Debug.LogError($"ResourceCard_{index} does not contain the expected Herbert text fields.");
            return;
        }

        title.gameObject.name = $"ResourceLabel_{index}";
        title.text = Labels[index];
        title.alignment = TextAlignmentOptions.Center;
        title.enableAutoSizing = true;
        title.fontSizeMin = 13f;
        title.fontSizeMax = 18f;
        title.raycastTarget = false;

        value.gameObject.name = $"Resource_{index}";
        value.text = Values[index];
        value.alignment = TextAlignmentOptions.Center;
        value.enableAutoSizing = true;
        value.fontSizeMin = 16f;
        value.fontSizeMax = 22f;
        value.raycastTarget = false;
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
