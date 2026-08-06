using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class VespidaeCombatProgressionSetup
{
    private const string ScenePath = "Assets/Scenes/wasp RTS Lvl.unity";
    private const string HexPrefabPath = "Assets/Prefabs/HexTile.prefab";
    private const string FriendlyHivePath = "Assets/Prefabs/FriendlyHives/Friendly_hive.prefab";
    private const string EnemyHivePath = "Assets/Prefabs/EnemyHives/Enemy_hive.prefab";
    private const string FriendlyWaspPath = "Assets/Prefabs/Friendly/Wasp_Player.prefab";
    private const string HealthBackgroundPath = "Assets/Herbert/Hmm/Small panel bg in v2 1.png";
    private const string HealthFillPath = "Assets/Herbert/Hmm/Small panel bg in v2.png";

    private static readonly string[] EnemyWaspPaths =
    {
        "Assets/Prefabs/Enemy/Wasp_Enemy_PolistesDominula.prefab",
        "Assets/Prefabs/Enemy/Wasp_Enemy_PolistesMarginalis.prefab",
        "Assets/Prefabs/Enemy/Wasp_Enemy_VespulaGermanica.prefab"
    };

    private static readonly string[] SkillPaths =
    {
        "Assets/ScriptableObjectInstances/WaspSkills/SO_Wasp_Skill_Scout.asset",
        "Assets/ScriptableObjectInstances/WaspSkills/SO_Wasp_Skill_Forager.asset",
        "Assets/ScriptableObjectInstances/WaspSkills/SO_Wasp_Skill_Builder.asset",
        "Assets/ScriptableObjectInstances/WaspSkills/SO_Wasp_Skill_BroodCaretaker.asset",
        "Assets/ScriptableObjectInstances/WaspSkills/SO_Wasp_Skill_Guard.asset",
        "Assets/ScriptableObjectInstances/WaspSkills/SO_Wasp_Skill_Containment.asset"
    };

    private static readonly string[] SkillFields =
    {
        "scoutSkill",
        "foragerSkill",
        "builderSkill",
        "broodCaretakerSkill",
        "guardSkill",
        "containmentSkill"
    };

    [MenuItem("Tools/Vespidae Wars/Setup Combat And Progression")]
    public static void Setup()
    {
        ConfigureHexPrefab();
        ConfigureWaspPrefab(FriendlyWaspPath, false);
        foreach (string path in EnemyWaspPaths)
            ConfigureWaspPrefab(path, true);
        ConfigureHivePrefab(FriendlyHivePath, false);
        ConfigureHivePrefab(EnemyHivePath, true);
        ConfigureGuardSkill();

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        ConfigureScene(scene);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Vespidae combat and progression setup complete.");
    }

    [MenuItem("Tools/Vespidae Wars/Audit Combat And Progression")]
    public static void Audit()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        List<string> errors = new List<string>();
        List<string> optional = new List<string>();
        HexTile[] tiles = UnityEngine.Object.FindObjectsByType<HexTile>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (tiles.Length != 42)
            errors.Add($"Expected 42 HexTiles but found {tiles.Length}.");

        foreach (HexTile tile in tiles)
        {
            SerializedObject serialized = new SerializedObject(tile);
            Require(serialized, "areaInfo", tile.name, errors);
            Require(serialized, "gatheringRules", tile.name, errors);
            Require(serialized, "hexRenderer", tile.name, errors);
            Require(serialized, "ownedMaterial", tile.name, errors);
            Require(serialized, "unknownMaterial", tile.name, errors);
            Require(serialized, "lockedMaterial", tile.name, errors);
            Require(serialized, "enemyMaterial", tile.name, errors);
            Require(serialized, "focusPoint", tile.name, errors);
            Require(serialized, "waspCloseUpFocusPoint", tile.name, errors);
            Require(serialized, "hiveSpawnPoint", tile.name, errors);
            Require(serialized, "combatController", tile.name, errors);

            if (tile.AreaInfo == null || tile.AreaInfo.ConnectedHexIds == null || tile.AreaInfo.ConnectedHexIds.Count == 0)
                errors.Add($"{tile.name} has no connected hex IDs.");
            else if (tile.AreaInfo.ConnectedHexIds.Count > 6)
                errors.Add($"{tile.name} has more than six connected hex IDs.");
        }

        Dictionary<string, HexTile> byId = tiles.Where(tile => tile.AreaInfo != null).ToDictionary(tile => tile.AreaId, tile => tile);
        foreach (HexTile tile in tiles)
        {
            if (tile.AreaInfo == null)
                continue;
            foreach (string id in tile.AreaInfo.ConnectedHexIds)
            {
                if (!byId.TryGetValue(id, out HexTile neighbour))
                    errors.Add($"{tile.AreaId} links to missing {id}.");
                else if (!neighbour.AreaInfo.ConnectedHexIds.Contains(tile.AreaId))
                    errors.Add($"{tile.AreaId} to {id} is not bidirectional.");
            }
        }

        GameObject managerObject = GameObject.Find("Game Manager");
        if (managerObject == null)
            errors.Add("Game Manager is missing.");
        else
        {
            if (managerObject.GetComponent<HexProgressionManager>() == null)
                errors.Add("Game Manager is missing HexProgressionManager.");
            AuditManagerSkills(managerObject.GetComponent<HiveManagement>(), errors);
            AuditManagerSkills(managerObject.GetComponent<EnemyHiveControl>(), errors);
        }

        AuditWaspPrefab(FriendlyWaspPath, errors);
        foreach (string path in EnemyWaspPaths)
            AuditWaspPrefab(path, errors);
        AuditHivePrefab(FriendlyHivePath, errors);
        AuditHivePrefab(EnemyHivePath, errors);

        C_HiveSkillsPanel panel = UnityEngine.Object.FindObjectsByType<C_HiveSkillsPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault();
        if (panel == null)
            errors.Add("SkillsPanel is missing C_HiveSkillsPanel.");
        else
        {
            SerializedProperty cards = new SerializedObject(panel).FindProperty("cards");
            if (cards == null || cards.arraySize != 6)
                errors.Add("SkillsPanel does not contain six serialized card bindings.");
            else
            {
                for (int index = 0; index < cards.arraySize; index++)
                {
                    SerializedProperty card = cards.GetArrayElementAtIndex(index);
                    Require(card, "cardRoot", $"Skill card {index}", errors);
                    RequireEither(card, "titleText", "legacyTitleText", $"Skill card {index}", errors);
                    RequireEither(card, "descriptionText", "legacyDescriptionText", $"Skill card {index}", errors);
                    RequireEither(card, "costText", "legacyCostText", $"Skill card {index}", errors);
                    Require(card, "upgradeButton", $"Skill card {index}", errors);
                    if (card.FindPropertyRelative("levelText")?.objectReferenceValue == null)
                        optional.Add($"Skill card {index} uses the title for its level.");
                    if (card.FindPropertyRelative("effectText")?.objectReferenceValue == null)
                        optional.Add($"Skill card {index} appends the effect to its description.");
                }
            }
        }

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
            {
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) > 0)
                    errors.Add($"Missing script on {GetPath(transform)}.");
            }
        }

        foreach (string note in optional.Distinct())
            Debug.Log($"AUDIT OPTIONAL: {note}");
        if (errors.Count == 0)
            Debug.Log("AUDIT PASS: Combat, progression, prefabs, graph, managers, and required serialized references are valid.");
        else
        {
            foreach (string error in errors.Distinct())
                Debug.LogError($"AUDIT FAIL: {error}");
            Debug.LogError($"AUDIT FAILED with {errors.Distinct().Count()} issue(s).");
        }
    }

    private static void ConfigureScene(Scene scene)
    {
        GameObject managerObject = GameObject.Find("Game Manager");
        if (managerObject == null)
            throw new InvalidOperationException("Game Manager was not found.");

        if (managerObject.GetComponent<HexProgressionManager>() == null)
            managerObject.AddComponent<HexProgressionManager>();
        AssignSkillAssets(managerObject.GetComponent<HiveManagement>());
        AssignSkillAssets(managerObject.GetComponent<EnemyHiveControl>());

        HexTile[] tiles = UnityEngine.Object.FindObjectsByType<HexTile>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        BuildConnections(tiles);

        foreach (HexTile tile in tiles)
        {
            SerializedObject serialized = new SerializedObject(tile);
            SerializedProperty combat = serialized.FindProperty("combatController");
            if (combat != null)
                combat.objectReferenceValue = tile.GetComponent<HexCombatController>();
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        GameObject skillsObject = FindSceneObject(scene, "SkillsPanel");
        if (skillsObject != null)
        {
            C_HiveSkillsPanel panel = skillsObject.GetComponent<C_HiveSkillsPanel>();
            if (panel == null)
                panel = skillsObject.AddComponent<C_HiveSkillsPanel>();
            BindSkillCards(scene, panel);
        }

        foreach (HexOptionsPanel panel in UnityEngine.Object.FindObjectsByType<HexOptionsPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            SerializedObject serialized = new SerializedObject(panel);
            SerializedProperty closeButton = serialized.FindProperty("closeActionButton");
            SerializedProperty closeText = serialized.FindProperty("closeActionButtonText");
            if (closeText != null && closeButton?.objectReferenceValue is Button button)
                closeText.objectReferenceValue = button.GetComponentInChildren<TMP_Text>(true);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void ConfigureHexPrefab()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(HexPrefabPath);
        try
        {
            HexTile tile = root.GetComponent<HexTile>();
            HexCombatController combat = root.GetComponent<HexCombatController>();
            if (combat == null)
                combat = root.AddComponent<HexCombatController>();
            SetReference(combat, "hexTile", tile);
            SetReference(tile, "combatController", combat);
            PrefabUtility.SaveAsPrefabAsset(root, HexPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void ConfigureWaspPrefab(string path, bool enemy)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            WaspInfo info = root.GetComponent<WaspInfo>();
            WaspRoleIconBillboard billboard = root.GetComponentInChildren<WaspRoleIconBillboard>(true);
            WaspCombatant combatant = root.GetComponent<WaspCombatant>();
            if (combatant == null)
                combatant = root.AddComponent<WaspCombatant>();
            Image fill = EnsureWaspHealthBar(billboard, out GameObject healthRoot);
            SetReference(combatant, "waspInfo", info);
            SetReference(combatant, "roleIconBillboard", billboard);
            SetReference(combatant, "healthBarRoot", healthRoot);
            SetReference(combatant, "healthFill", fill);

            if (enemy)
                SetReference(root.GetComponent<EnemyWaspControl>(), "combatant", combatant);
            else
                SetReference(root.GetComponent<WaspControl>(), "combatant", combatant);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void ConfigureHivePrefab(string path, bool enemy)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            HiveCombatant combatant = root.GetComponent<HiveCombatant>();
            if (combatant == null)
                combatant = root.AddComponent<HiveCombatant>();
            Image fill = EnsureHiveHealthBar(root.transform, out GameObject healthRoot);
            SetReference(combatant, "healthBarRoot", healthRoot);
            SetReference(combatant, "healthFill", fill);

            if (enemy)
            {
                C_Enemy_Hive_Orc hive = root.GetComponent<C_Enemy_Hive_Orc>();
                SetReference(combatant, "enemyHive", hive);
                SetReference(hive, "combatant", combatant);
            }
            else
            {
                C_Friendly_Hive_Orc hive = root.GetComponent<C_Friendly_Hive_Orc>();
                SetReference(combatant, "friendlyHive", hive);
                SetReference(hive, "combatant", combatant);
            }

            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static Image EnsureWaspHealthBar(WaspRoleIconBillboard billboard, out GameObject healthRoot)
    {
        if (billboard == null)
            throw new InvalidOperationException("Wasp prefab has no WaspRoleIconBillboard.");

        Transform existing = billboard.transform.Find("Combat Health Bar");
        healthRoot = existing != null ? existing.gameObject : CreateBar("Combat Health Bar", billboard.transform, new Vector2(58f, 9f), new Vector2(0f, -43f), out _);
        return healthRoot.transform.Find("Fill").GetComponent<Image>();
    }

    private static Image EnsureHiveHealthBar(Transform hiveRoot, out GameObject healthRoot)
    {
        Transform canvasTransform = hiveRoot.Find("Hive Combat Health Canvas");
        if (canvasTransform == null)
        {
            GameObject canvasObject = new GameObject("Hive Combat Health Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(WorldHealthBarBillboard));
            canvasTransform = canvasObject.transform;
            canvasTransform.SetParent(hiveRoot, false);
            canvasTransform.localPosition = new Vector3(0f, 1f, 0f);
            canvasTransform.localScale = Vector3.one * 0.005f;
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 51;
        }

        Transform existing = canvasTransform.Find("Hive Health Bar");
        healthRoot = existing != null ? existing.gameObject : CreateBar("Hive Health Bar", canvasTransform, new Vector2(120f, 12f), Vector2.zero, out _);
        return healthRoot.transform.Find("Fill").GetComponent<Image>();
    }

    private static GameObject CreateBar(string name, Transform parent, Vector2 size, Vector2 position, out Image fill)
    {
        Sprite backgroundSprite = AssetDatabase.LoadAssetAtPath<Sprite>(HealthBackgroundPath);
        Sprite fillSprite = AssetDatabase.LoadAssetAtPath<Sprite>(HealthFillPath);
        GameObject background = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform backgroundRect = background.GetComponent<RectTransform>();
        backgroundRect.SetParent(parent, false);
        backgroundRect.anchorMin = backgroundRect.anchorMax = new Vector2(0.5f, 0.5f);
        backgroundRect.pivot = new Vector2(0.5f, 0.5f);
        backgroundRect.anchoredPosition = position;
        backgroundRect.sizeDelta = size;
        Image backgroundImage = background.GetComponent<Image>();
        backgroundImage.sprite = backgroundSprite;
        backgroundImage.type = Image.Type.Sliced;
        backgroundImage.raycastTarget = false;

        GameObject fillObject = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.SetParent(backgroundRect, false);
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(1f, 1f);
        fillRect.offsetMin = new Vector2(3f, 2f);
        fillRect.offsetMax = new Vector2(-3f, -2f);
        fill = fillObject.GetComponent<Image>();
        fill.sprite = fillSprite;
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = 0;
        fill.fillAmount = 1f;
        fill.color = new Color(0.36f, 0.8f, 0.2f, 1f);
        fill.raycastTarget = false;
        return background;
    }

    private static void AssignSkillAssets(MonoBehaviour manager)
    {
        if (manager == null)
            return;

        SerializedObject serialized = new SerializedObject(manager);
        for (int index = 0; index < SkillFields.Length; index++)
        {
            SerializedProperty field = serialized.FindProperty(SkillFields[index]);
            if (field != null)
                field.objectReferenceValue = AssetDatabase.LoadAssetAtPath<SB_Wasp_Skill>(SkillPaths[index]);
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void BindSkillCards(Scene scene, C_HiveSkillsPanel panel)
    {
        SerializedObject serialized = new SerializedObject(panel);
        SerializedProperty cards = serialized.FindProperty("cards");
        cards.arraySize = 6;
        for (int index = 0; index < 6; index++)
        {
            GameObject cardObject = FindSceneObject(scene, $"Skills_Card_{index}");
            SerializedProperty card = cards.GetArrayElementAtIndex(index);
            card.FindPropertyRelative("function").enumValueIndex = index;
            card.FindPropertyRelative("cardRoot").objectReferenceValue = cardObject;
            card.FindPropertyRelative("titleText").objectReferenceValue = FindComponent<TMP_Text>(scene, $"Skills_CardTitle_{index}");
            card.FindPropertyRelative("descriptionText").objectReferenceValue = FindComponent<TMP_Text>(scene, $"Skills_CardDesc_{index}");
            card.FindPropertyRelative("costText").objectReferenceValue = FindComponent<TMP_Text>(scene, $"Skills_CardCost_{index}");
            card.FindPropertyRelative("legacyTitleText").objectReferenceValue = FindComponent<Text>(scene, $"Skills_CardTitle_{index}");
            card.FindPropertyRelative("legacyDescriptionText").objectReferenceValue = FindComponent<Text>(scene, $"Skills_CardDesc_{index}");
            card.FindPropertyRelative("legacyCostText").objectReferenceValue = FindComponent<Text>(scene, $"Skills_CardCost_{index}");
            card.FindPropertyRelative("upgradeButton").objectReferenceValue = cardObject != null ? cardObject.GetComponent<Button>() : null;
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void BuildConnections(HexTile[] tiles)
    {
        List<float> nearestDistances = new List<float>();
        foreach (HexTile tile in tiles)
        {
            float nearest = tiles.Where(other => other != tile)
                .Select(other => HorizontalDistance(tile.transform.position, other.transform.position))
                .DefaultIfEmpty(float.MaxValue)
                .Min();
            if (nearest < float.MaxValue)
                nearestDistances.Add(nearest);
        }

        nearestDistances.Sort();
        float median = nearestDistances.Count > 0 ? nearestDistances[nearestDistances.Count / 2] : 1.4f;
        float threshold = median * 1.18f;
        foreach (HexTile tile in tiles)
        {
            List<string> ids = tiles
                .Where(other => other != tile && HorizontalDistance(tile.transform.position, other.transform.position) <= threshold)
                .OrderBy(other => HorizontalDistance(tile.transform.position, other.transform.position))
                .Take(6)
                .Where(other => other.AreaInfo != null)
                .Select(other => other.AreaId)
                .Distinct()
                .ToList();
            if (tile.AreaInfo != null)
            {
                tile.AreaInfo.ConfigureConnectionsForEditor(ids);
                EditorUtility.SetDirty(tile.AreaInfo);
            }
        }
    }

    private static float HorizontalDistance(Vector3 first, Vector3 second)
    {
        return Vector2.Distance(new Vector2(first.x, first.z), new Vector2(second.x, second.z));
    }

    private static void ConfigureGuardSkill()
    {
        SB_Wasp_Skill guard = AssetDatabase.LoadAssetAtPath<SB_Wasp_Skill>(SkillPaths[4]);
        if (guard == null)
            return;
        SerializedObject serialized = new SerializedObject(guard);
        serialized.FindProperty("maximumLevel").intValue = 5;
        serialized.FindProperty("effectSummary").stringValue = "Increases attacker damage and attack speed.";
        serialized.FindProperty("baseMaximumHealth").floatValue = 100f;
        serialized.FindProperty("maximumHealthPerLevel").floatValue = 10f;
        serialized.FindProperty("baseAttackDamage").floatValue = 10f;
        serialized.FindProperty("attackDamagePerLevel").floatValue = 5f;
        serialized.FindProperty("baseDefence").floatValue = 1f;
        serialized.FindProperty("defencePerLevel").floatValue = 0.1f;
        serialized.FindProperty("baseAttackSpeed").floatValue = 1f;
        serialized.FindProperty("attackSpeedPerLevel").floatValue = 0.25f;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(guard);
    }

    private static void AuditManagerSkills(MonoBehaviour manager, List<string> errors)
    {
        if (manager == null)
        {
            errors.Add("A hive manager is missing.");
            return;
        }

        SerializedObject serialized = new SerializedObject(manager);
        foreach (string field in SkillFields)
            Require(serialized, field, manager.GetType().Name, errors);
    }

    private static void AuditWaspPrefab(string path, List<string> errors)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            errors.Add($"Missing wasp prefab {path}.");
            return;
        }

        WaspCombatant combatant = prefab.GetComponent<WaspCombatant>();
        if (combatant == null)
        {
            errors.Add($"{path} has no WaspCombatant.");
            return;
        }
        SerializedObject serialized = new SerializedObject(combatant);
        Require(serialized, "waspInfo", path, errors);
        Require(serialized, "healthBarRoot", path, errors);
        Require(serialized, "healthFill", path, errors);
        Require(serialized, "roleIconBillboard", path, errors);
    }

    private static void AuditHivePrefab(string path, List<string> errors)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        HiveCombatant combatant = prefab != null ? prefab.GetComponent<HiveCombatant>() : null;
        if (combatant == null)
        {
            errors.Add($"{path} has no HiveCombatant.");
            return;
        }
        SerializedObject serialized = new SerializedObject(combatant);
        Require(serialized, "healthBarRoot", path, errors);
        Require(serialized, "healthFill", path, errors);
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

    private static void RequireEither(SerializedProperty parent, string first, string second, string owner, List<string> errors)
    {
        SerializedProperty firstProperty = parent.FindPropertyRelative(first);
        SerializedProperty secondProperty = parent.FindPropertyRelative(second);
        if ((firstProperty == null || firstProperty.objectReferenceValue == null) &&
            (secondProperty == null || secondProperty.objectReferenceValue == null))
            errors.Add($"{owner}.{first}/{second} are both empty.");
    }

    private static void SetReference(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
    {
        if (target == null)
            return;
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property != null)
            property.objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
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

    private static T FindComponent<T>(Scene scene, string name) where T : Component
    {
        GameObject target = FindSceneObject(scene, name);
        return target != null ? target.GetComponent<T>() : null;
    }

    private static string GetPath(Transform transform)
    {
        string path = transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            path = transform.name + "/" + path;
        }
        return path;
    }
}
