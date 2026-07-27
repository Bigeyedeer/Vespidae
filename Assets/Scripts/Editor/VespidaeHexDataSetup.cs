#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class VespidaeHexDataSetup
{
    private const string AreaFolder = "Assets/ScriptableObjectInstances/HexAreas";
    private const string RulesFolder = "Assets/ScriptableObjectInstances/Rules";
    private const string AreaPath = AreaFolder + "/SO_FynbosScrub.asset";
    private const string RulesPath = RulesFolder + "/SO_DefaultHexGatheringRules.asset";
    private const string HexPrefabPath = "Assets/Prefabs/HexTile.prefab";
    private const string MainWorldScenePath = "Assets/Scenes/wasp RTS Lvl.unity";

    static VespidaeHexDataSetup()
    {
        EditorApplication.delayCall += EnsureHexData;
    }

    [MenuItem("Tools/Vespidae Wars/Setup Hex Data")]
    public static void EnsureHexData()
    {
        if (!EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EnsureFolder("Assets/ScriptableObjectInstances", "HexAreas");
            EnsureFolder("Assets/ScriptableObjectInstances", "Rules");

            SB_Hex_Area_Info areaInfo = LoadOrCreateAsset<SB_Hex_Area_Info>(AreaPath);
            SB_Hex_Gathering_Rules gatheringRules = LoadOrCreateAsset<SB_Hex_Gathering_Rules>(RulesPath);

            SB_Wasps_Info nativeWasp = AssetDatabase.LoadAssetAtPath<SB_Wasps_Info>("Assets/ScriptableObjectInstances/WaspSpecies/SO_PolistesMarginalis.asset");
            SB_Wasps_Info europeanWasp = AssetDatabase.LoadAssetAtPath<SB_Wasps_Info>("Assets/ScriptableObjectInstances/WaspSpecies/SO_PolistesDominula.asset");
            SB_Wasps_Info germanWasp = AssetDatabase.LoadAssetAtPath<SB_Wasps_Info>("Assets/ScriptableObjectInstances/WaspSpecies/SO_VespulaGermanica.asset");

            areaInfo.ConfigureForEditor(
                "fynbos_scrub",
                "Fynbos Scrub",
                "Native fynbos scrub and local stronghold territory with exposed paper nests, nectar, and soft-bodied prey.",
                "Native fynbos and local stronghold",
                HexResourceType.ProteinAndSugar,
                1000f,
                2000f,
                new List<SB_Wasps_Info> { nativeWasp, europeanWasp, germanWasp });

            gatheringRules.ConfigureForEditor(2f, 10f, 20f, 20);

            EditorUtility.SetDirty(areaInfo);
            EditorUtility.SetDirty(gatheringRules);
            AssignToHexPrefab(areaInfo, gatheringRules);
            CreateHexAreaInstances();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }

    private static void CreateHexAreaInstances()
    {
        List<SB_Hex_Area_Info> hexAreas = new List<SB_Hex_Area_Info>();

        for (int index = 1; index <= 33; index++)
        {
            string path = $"{AreaFolder}/SO_Hex{index}.asset";
            SB_Hex_Area_Info areaInfo = LoadOrCreateAsset<SB_Hex_Area_Info>(path);
            areaInfo.ConfigureForEditor(
                $"hex_{index}",
                $"Hex {index}",
                "Area details not assigned yet.",
                "Habitat not assigned yet.",
                HexResourceType.None,
                0f,
                0f,
                new List<SB_Wasps_Info>());
            EditorUtility.SetDirty(areaInfo);
            hexAreas.Add(areaInfo);
        }

        AssignHexAreasToScene(hexAreas);
    }

    private static void AssignHexAreasToScene(List<SB_Hex_Area_Info> hexAreas)
    {
        Scene targetScene = SceneManager.GetSceneByPath(MainWorldScenePath);
        bool openedForSetup = false;

        if (!targetScene.IsValid() || !targetScene.isLoaded)
        {
            targetScene = EditorSceneManager.OpenScene(
                MainWorldScenePath,
                OpenSceneMode.Additive);
            openedForSetup = true;
        }

        List<HexTile> hexTiles = new List<HexTile>();

        foreach (GameObject root in targetScene.GetRootGameObjects())
        {
            hexTiles.AddRange(root.GetComponentsInChildren<HexTile>(true));
        }

        hexTiles.Sort((left, right) =>
            string.Compare(
                GetHierarchyPath(left.transform),
                GetHierarchyPath(right.transform),
                System.StringComparison.Ordinal));

        if (hexTiles.Count != hexAreas.Count)
        {
            Debug.LogWarning(
                $"Expected {hexAreas.Count} hex tiles in {MainWorldScenePath}, found {hexTiles.Count}.");
        }

        int assignmentCount = Mathf.Min(hexTiles.Count, hexAreas.Count);

        for (int index = 0; index < assignmentCount; index++)
        {
            SetPrivateField(hexTiles[index], "areaInfo", hexAreas[index]);
            EditorUtility.SetDirty(hexTiles[index]);
        }

        if (assignmentCount > 0)
        {
            EditorSceneManager.MarkSceneDirty(targetScene);
            EditorSceneManager.SaveScene(targetScene);
        }

        if (openedForSetup)
        {
            EditorSceneManager.CloseScene(targetScene, true);
        }
    }

    private static string GetHierarchyPath(Transform transform)
    {
        List<string> names = new List<string>();
        Transform current = transform;

        while (current != null)
        {
            names.Add(current.name);
            current = current.parent;
        }

        names.Reverse();
        return string.Join("/", names);
    }

    private static void AssignToHexPrefab(
        SB_Hex_Area_Info areaInfo,
        SB_Hex_Gathering_Rules gatheringRules)
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(HexPrefabPath);
        if (prefabRoot == null)
            return;

        HexTile hexTile = prefabRoot.GetComponent<HexTile>();
        if (hexTile != null)
        {
            SetPrivateField(hexTile, "areaInfo", areaInfo);
            SetPrivateField(hexTile, "gatheringRules", gatheringRules);
            EditorUtility.SetDirty(hexTile);
        }

        PrefabUtility.SaveAsPrefabAsset(prefabRoot, HexPrefabPath);
        PrefabUtility.UnloadPrefabContents(prefabRoot);
    }

    private static T LoadOrCreateAsset<T>(string path) where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null)
            return asset;

        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    private static void EnsureFolder(string parent, string folderName)
    {
        string path = $"{parent}/{folderName}";
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, folderName);
    }

    private static void SetPrivateField(Object target, string fieldName, object value)
    {
        System.Reflection.FieldInfo field = target.GetType().GetField(
            fieldName,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        if (field != null)
            field.SetValue(target, value);
    }
}
#endif
