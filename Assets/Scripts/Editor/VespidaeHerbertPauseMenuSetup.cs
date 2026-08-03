using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class VespidaeHerbertPauseMenuSetup
{
    private const string ScenePath = "Assets/Scenes/wasp RTS Lvl.unity";
    private const string PrefabPath = "Assets/Herbert/UI/Menu.prefab";

    [MenuItem("Tools/Vespidae Wars/Use Herbert Pause Menu")]
    public static void Setup()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        C_MainWorldOverlayNavigation navigation = Object.FindObjectsByType<C_MainWorldOverlayNavigation>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None)
            .FirstOrDefault();
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);

        if (navigation == null || prefab == null)
        {
            Debug.LogError("The main-world overlay navigation or Herbert Menu prefab was not found.");
            return;
        }

        SerializedObject serialized = new SerializedObject(navigation);
        serialized.FindProperty("pauseMenuPrefab").objectReferenceValue = prefab;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(navigation);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("The MainWorld pause flow now uses Herbert's Menu prefab.");
    }
}
