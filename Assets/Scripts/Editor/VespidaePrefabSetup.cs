#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

[InitializeOnLoad]
public static class VespidaePrefabSetup
{
    private const string FriendlyHivePath = "Assets/Prefabs/FriendlyHives/Friendly_hive.prefab";
    private const string EnemyHivePath = "Assets/Prefabs/EnemyHives/Enemy_hive.prefab";
    private const string WaspPrefabPath = "Assets/Prefabs/Wasp_pre.prefab";

    static VespidaePrefabSetup()
    {
        EditorApplication.delayCall += EnsurePrefabComponents;
    }

    [MenuItem("Tools/Vespidae Wars/Setup Hive Triggers and Wasp Navigation")]
    public static void EnsurePrefabComponents()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        SetupHive(FriendlyHivePath);
        SetupHive(EnemyHivePath);
        SetupWaspNavigation();
        AssetDatabase.SaveAssets();
    }

    private static void SetupHive(string path)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        if (root == null)
            return;

        HiveHoverEffect hoverEffect = root.GetComponent<HiveHoverEffect>();
        if (hoverEffect == null)
            hoverEffect = root.AddComponent<HiveHoverEffect>();

        Transform triggerTransform = root.transform.Find("HiveClickTrigger");
        if (triggerTransform == null)
        {
            GameObject triggerObject = new GameObject("HiveClickTrigger");
            triggerTransform = triggerObject.transform;
            triggerTransform.SetParent(root.transform, false);
        }

        BoxCollider trigger = triggerTransform.GetComponent<BoxCollider>();
        if (trigger == null)
            trigger = triggerTransform.gameObject.AddComponent<BoxCollider>();

        trigger.isTrigger = true;
        trigger.center = Vector3.zero;
        trigger.size = new Vector3(1.25f, 1.25f, 1.25f);

        SerializedObject serializedHover = new SerializedObject(hoverEffect);
        SerializedProperty triggerProperty = serializedHover.FindProperty("clickTrigger");
        if (triggerProperty != null)
        {
            serializedHover.Update();
            triggerProperty.objectReferenceValue = trigger;
            serializedHover.ApplyModifiedPropertiesWithoutUndo();
        }

        EditorUtility.SetDirty(root);
        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
    }

    private static void SetupWaspNavigation()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(WaspPrefabPath);
        if (root == null)
            return;

        NavMeshAgent agent = root.GetComponent<NavMeshAgent>();
        if (agent == null)
            agent = root.AddComponent<NavMeshAgent>();

        agent.enabled = false;
        EditorUtility.SetDirty(root);
        PrefabUtility.SaveAsPrefabAsset(root, WaspPrefabPath);
        PrefabUtility.UnloadPrefabContents(root);
    }
}
#endif
