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

    private readonly struct HexAreaDefinition
    {
        public readonly string Id;
        public readonly string Name;
        public readonly string Description;
        public readonly string Habitat;
        public readonly HexResourceType ResourceType;
        public readonly float Prey;
        public readonly float Nectar;
        public readonly float Fibre;

        public HexAreaDefinition(
            string id,
            string name,
            string description,
            string habitat,
            HexResourceType resourceType,
            float prey,
            float nectar,
            float fibre)
        {
            Id = id;
            Name = name;
            Description = description;
            Habitat = habitat;
            ResourceType = resourceType;
            Prey = prey;
            Nectar = nectar;
            Fibre = fibre;
        }
    }

    private static readonly HexAreaDefinition[] AreaDefinitions =
    {
        new("hex_01_fynbos_crown", "Fynbos Crown", "A high fynbos ridge where flowering shrubs shelter abundant insect prey and nectar sources.", "Mountain fynbos ridge", HexResourceType.PreyAndNectar, 1180f, 960f, 0f),
        new("hex_02_renosterveld_reach", "Renosterveld Reach", "Rolling renosterveld grassland with seasonal blooms and dry stems suited to colony building.", "Renosterveld grassland", HexResourceType.NectarAndFibre, 0f, 710f, 1040f),
        new("hex_03_protea_ridge", "Protea Ridge", "Dense protea stands create sheltered hunting lanes and supply strong plant fibres.", "Proteoid fynbos", HexResourceType.PreyAndFibre, 840f, 0f, 1130f),
        new("hex_04_silvermine_pass", "Silvermine Pass", "A cool mountain pass where insects gather in pockets between sandstone outcrops.", "Sandstone fynbos pass", HexResourceType.Prey, 930f, 0f, 0f),
        new("hex_05_table_mountain_edge", "Table Mountain Edge", "Wind-cut slopes of Table Mountain carry scattered flowering patches rich in nectar.", "Peninsula sandstone fynbos", HexResourceType.Nectar, 0f, 1210f, 0f),
        new("hex_06_kogelberg_stronghold", "Kogelberg Stronghold", "A protected mountain-fynbos stronghold with both hunting cover and flowering forage.", "Kogelberg mountain fynbos", HexResourceType.PreyAndNectar, 1100f, 780f, 0f),
        new("hex_07_cape_flats_frontier", "Cape Flats Frontier", "Lowland sand fynbos on the urban edge, offering flower forage and flexible building material.", "Cape Flats sand fynbos", HexResourceType.NectarAndFibre, 0f, 1140f, 690f),
        new("hex_08_agulhas_wilds", "Agulhas Wilds", "Exposed coastal scrub provides prey among dune plants and weathered fibre for nest repair.", "Agulhas coastal fynbos", HexResourceType.PreyAndFibre, 760f, 0f, 970f),
        new("hex_09_elgin_bloomlands", "Elgin Bloomlands", "A sheltered bloomland where seasonal flowers create a reliable nectar reserve.", "Elgin valley fynbos", HexResourceType.Nectar, 0f, 1290f, 0f),
        new("hex_10_cederberg_heights", "Cederberg Heights", "Dry highland vegetation yields tough reeds and plant fibres between rocky ledges.", "Cederberg sandstone fynbos", HexResourceType.Fibre, 0f, 0f, 1090f),
        new("hex_11_breede_river_basin", "Breede River Basin", "River-fed vegetation supports a varied hunting ground beside rich flowering banks.", "Breede riverine scrub", HexResourceType.PreyAndNectar, 1050f, 850f, 0f),
        new("hex_12_false_bay_crossing", "False Bay Crossing", "A coastal crossing where scavenging insects concentrate along wind-sheltered vegetation.", "Coastal strandveld", HexResourceType.Prey, 650f, 0f, 0f),
        new("hex_13_camissa_springs", "Camissa Springs", "Fresh-water seepage feeds lush vegetation with plentiful nectar and supple nesting fibres.", "Spring-fed urban fynbos", HexResourceType.NectarAndFibre, 0f, 1250f, 730f),
        new("hex_14_hoerikwaggo_rise", "Hoerikwaggo Rise", "Steep slopes and hardy shrubs offer resilient fibres for a colony holding the heights.", "Mountain slope fynbos", HexResourceType.Fibre, 0f, 0f, 1120f),
        new("hex_15_lions_head_outlook", "Lion's Head Outlook", "Open slopes make insect movement easy to spot, with sparse but useful construction material.", "Granite fynbos slope", HexResourceType.PreyAndFibre, 1180f, 0f, 560f),
        new("hex_16_constantia_valley", "Constantia Valley", "Sheltered valley gardens and fynbos edges provide both flowers and easy hunting.", "Valley fynbos edge", HexResourceType.PreyAndNectar, 880f, 1240f, 0f),
        new("hex_17_hout_bay_gate", "Hout Bay Gate", "A cool coastal entrance where flowering scrub maintains a steady nectar supply.", "Coastal mountain fynbos", HexResourceType.Nectar, 0f, 890f, 0f),
        new("hex_18_noordhoek_expanse", "Noordhoek Expanse", "Broad sandy vegetation contains long grasses and dry stems suitable for fibre collection.", "Sand fynbos plain", HexResourceType.Fibre, 0f, 0f, 1040f),
        new("hex_19_muizenberg_strand", "Muizenberg Strand", "Coastal vegetation supports hunting around dune scrub and flowering strandveld plants.", "Dune strandveld", HexResourceType.PreyAndNectar, 990f, 700f, 0f),
        new("hex_20_cape_point_threshold", "Cape Point Threshold", "A wind-battered cape where roaming insects form a valuable but exposed prey source.", "Maritime fynbos", HexResourceType.Prey, 1220f, 0f, 0f),
        new("hex_21_kuils_river_lowlands", "Kuils River Lowlands", "Lowland vegetation along the river provides flower forage and flexible fibres.", "River lowland scrub", HexResourceType.NectarAndFibre, 0f, 770f, 1090f),
        new("hex_22_tygerberg_watch", "Tygerberg Watch", "Elevated renosterveld patches give a clear view over prey routes and dry building stems.", "Tygerberg renosterveld", HexResourceType.PreyAndFibre, 680f, 0f, 1250f),
        new("hex_23_stellenbosch_foothills", "Stellenbosch Foothills", "Fynbos at the foothills blooms across the slopes, making this a nectar-rich territory.", "Winelands fynbos foothills", HexResourceType.Nectar, 0f, 1150f, 0f),
        new("hex_24_paarl_granite_reach", "Paarl Granite Reach", "Granite outcrops are lined with hardy shrubs that yield durable nest material.", "Granite fynbos", HexResourceType.Fibre, 0f, 0f, 750f),
        new("hex_25_franschhoek_valley", "Franschhoek Valley", "A warm valley mosaic of flowers and insect activity makes for productive colony forage.", "Valley fynbos mosaic", HexResourceType.PreyAndNectar, 1270f, 980f, 0f),
        new("hex_26_wellington_shield", "Wellington Shield", "A dry foothill shield where hunting insects gather around resilient shrub cover.", "Foothill shale fynbos", HexResourceType.Prey, 800f, 0f, 0f),
        new("hex_27_malmesbury_grainlands", "Malmesbury Grainlands", "Field edges and remaining natural patches supply flowers alongside dry stalk fibre.", "Swartland field margin", HexResourceType.NectarAndFibre, 0f, 1110f, 650f),
        new("hex_28_swartland_verge", "Swartland Verge", "A contested-looking verge of renosterveld remnants with prey cover and strong stems.", "Swartland renosterveld", HexResourceType.PreyAndFibre, 900f, 0f, 1010f),
        new("hex_29_atlantis_sandfields", "Atlantis Sandfields", "Sparse sandveld flowers offer a modest but valuable nectar reserve on the dry flats.", "Atlantis sand fynbos", HexResourceType.Nectar, 0f, 690f, 0f),
        new("hex_30_langebaan_salt_coast", "Langebaan Salt Coast", "Salt-tolerant coastal plants provide tough fibres despite the exposed shoreline.", "West Coast strandveld", HexResourceType.Fibre, 0f, 0f, 1200f),
        new("hex_31_west_coast_bloom", "West Coast Bloom", "Seasonal flower displays draw insects and create one of the best mixed forage sites.", "West Coast flower veld", HexResourceType.PreyAndNectar, 730f, 1160f, 0f),
        new("hex_32_darling_flowerlands", "Darling Flowerlands", "Wildflower corridors attract a roaming supply of insects for scout and worker wasps.", "Darling renosterveld", HexResourceType.Prey, 1060f, 0f, 0f),
        new("hex_33_piketberg_escarpment", "Piketberg Escarpment", "Rocky escarpment vegetation combines late flowers with long, dry fibre sources.", "Escarpment fynbos", HexResourceType.NectarAndFibre, 0f, 860f, 1230f),
        new("hex_34_tulbagh_mountain_bowl", "Tulbagh Mountain Bowl", "A mountain bowl with insect-rich shrubland and grasses suitable for colony repairs.", "Mountain valley fynbos", HexResourceType.PreyAndFibre, 1190f, 0f, 810f),
        new("hex_35_ceres_basin", "Ceres Basin", "Cool basin vegetation supports an extended flowering season and reliable nectar supply.", "Inland basin fynbos", HexResourceType.Nectar, 0f, 1040f, 0f),
        new("hex_36_worcester_drylands", "Worcester Drylands", "Dry inland scrub produces scarce but hardy fibres that reward careful collection.", "Karoo-edge scrub", HexResourceType.Fibre, 0f, 0f, 640f),
        new("hex_37_robertson_riverlands", "Robertson Riverlands", "River corridors host flowering vegetation and busy insect hunting routes.", "Breede river valley", HexResourceType.PreyAndNectar, 1170f, 910f, 0f),
        new("hex_38_caledon_grassveld", "Caledon Grassveld", "Open grassveld and renosterveld edges provide a steady movement of prey insects.", "Overberg grassveld", HexResourceType.Prey, 760f, 0f, 0f),
        new("hex_39_hermanus_coastal_rise", "Hermanus Coastal Rise", "Coastal fynbos on the rise offers dense flowering patches and wind-shaped fibres.", "Overberg coastal fynbos", HexResourceType.NectarAndFibre, 0f, 1280f, 560f),
        new("hex_40_stanford_marshes", "Stanford Marshes", "Wetland margins shelter insects and reed-like material for an established colony.", "Wetland margin vegetation", HexResourceType.PreyAndFibre, 950f, 0f, 1180f),
        new("hex_41_de_hoop_sanctuary", "De Hoop Sanctuary", "Protected coastal vegetation supports a broad nectar reserve among flowering shrubs.", "De Hoop coastal fynbos", HexResourceType.Nectar, 0f, 1000f, 0f),
        new("hex_42_cape_floral_heartland", "Cape Floral Heartland", "A diverse fynbos core with rich flowers and insect life at the centre of the campaign map.", "Cape Floral Region fynbos", HexResourceType.PreyAndNectar, 1300f, 1300f, 0f)
    };

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

        for (int index = 1; index <= 42; index++)
        {
            string path = $"{AreaFolder}/SO_Hex{index}.asset";
            SB_Hex_Area_Info areaInfo = AssetDatabase.LoadAssetAtPath<SB_Hex_Area_Info>(path);
            HexAreaDefinition definition = AreaDefinitions[index - 1];

            if (areaInfo == null)
            {
                areaInfo = ScriptableObject.CreateInstance<SB_Hex_Area_Info>();
                AssetDatabase.CreateAsset(areaInfo, path);
            }

            if (RequiresInitialConfiguration(areaInfo))
            {
                areaInfo.ConfigureForEditor(
                    definition.Id,
                    definition.Name,
                    definition.Description,
                    definition.Habitat,
                    definition.ResourceType,
                    GetStoredResourceValue(definition.Prey, index, 137, 211),
                    GetStoredResourceValue(definition.Nectar, index, 173, 97),
                    new List<SB_Wasps_Info>(),
                    GetStoredResourceValue(definition.Fibre, index, 191, 41));
                EditorUtility.SetDirty(areaInfo);
            }

            hexAreas.Add(areaInfo);
        }

        AssignHexAreasToScene(hexAreas);
    }

    private static bool RequiresInitialConfiguration(SB_Hex_Area_Info areaInfo)
    {
        return areaInfo == null ||
               areaInfo.AreaName.StartsWith("Hex ") ||
               areaInfo.AreaDescription == "Area details not assigned yet.";
    }

    private static float GetStoredResourceValue(
        float configuredValue,
        int index,
        int multiplier,
        int offset)
    {
        return configuredValue > 0f
            ? configuredValue
            : 500f + ((index * multiplier + offset) % 801);
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
