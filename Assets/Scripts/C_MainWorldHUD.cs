using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class C_MainWorldHUD : MonoBehaviour
{
    private static readonly Dictionary<string, GameObject> objectCache = new Dictionary<string, GameObject>();

    private HexTile selectedHex;

    public static C_MainWorldHUD GetOrCreate()
    {
        C_MainWorldHUD[] existing = Resources.FindObjectsOfTypeAll<C_MainWorldHUD>();
        foreach (C_MainWorldHUD hud in existing)
        {
            if (hud != null && hud.gameObject.scene.IsValid())
                return hud;
        }

        GameObject host = FindSceneObject("MainWorldHUD");
        if (host == null)
            host = GameObject.Find("Game Manager");
        if (host == null)
            return null;

        return host.AddComponent<C_MainWorldHUD>();
    }

    private void Awake()
    {
        CacheSceneObjects();
        GameObject resourcesPanel = FindSceneObject("Resources Panel");
        if (resourcesPanel != null)
            resourcesPanel.SetActive(true);
    }

    private void Start()
    {
        RefreshAll();
    }

    public void RefreshAll()
    {
        CacheSceneObjects();
        RefreshResources();
        RefreshColony();
        RefreshEcosystem();

        if (selectedHex != null)
            RefreshTerritory(selectedHex);
    }

    public void ShowSelectedHex(HexTile hex)
    {
        selectedHex = hex;
        RefreshTerritory(hex);
    }

    private void RefreshResources()
    {
        ResourceManager resources = ResourceManager.Instance;
        if (resources == null)
            return;

        SetText("Resource_0", $"Nectar\n{resources.Nectar:0}");
        SetText("Resource_1", $"Prey\n{resources.Prey:0}");
        SetText("Resource_2", $"Fibre\n{resources.Fibre:0}");
    }

    private void RefreshColony()
    {
        HiveManagement hive = HiveManagement.Instance;
        if (hive == null)
            return;

        SetText("Resource_3", $"Workers\n{hive.Workers:0}");
        SetText("Resource_4", $"Strength\n{hive.ColonyStrength:0}");
        SetText("Resource_5", $"Brood\n{hive.BroodProgress:0}/{hive.BroodCapacity:0}");
    }

    private void RefreshEcosystem()
    {
        HiveManagement hive = HiveManagement.Instance;
        if (hive == null)
            return;

        SetText("MetricName_0", "Habitat health");
        SetText("MetricName_1", "Biodiversity");
        SetText("MetricName_2", "Invasion pressure");
        SetText("MetricValue_0", $"{hive.HabitatHealth * 100f:0}%");
        SetText("MetricValue_1", $"{hive.Biodiversity * 100f:0}%");
        SetText("MetricValue_2", $"{hive.InvasionPressure * 100f:0}%");
        SetFill("MetricBarFill_0", hive.HabitatHealth);
        SetFill("MetricBarFill_1", hive.Biodiversity);
        SetFill("MetricBarFill_2", hive.InvasionPressure);
    }

    private void RefreshTerritory(HexTile hex)
    {
        if (hex == null)
        {
            SetText("TerritoryTitle", "Territory");
            SetText("TerritoryInfo", "Select a site to inspect it");
            SetText("TerritoryNote", "Click a hexagon to inspect its territory data.");
            return;
        }

        SetText("TerritoryTitle", hex.HexName);
        SetText("TerritoryInfo", $"{hex.HabitatCue}\n{hex.ConnectedSiteCount} connected sites");
        SetText("TerritoryNote", $"State: {hex.State} · Risk: {hex.RiskState}");
    }

    private void SetText(string objectName, string value)
    {
        GameObject target = FindSceneObject(objectName);
        if (target == null)
            return;

        TMP_Text tmp = target.GetComponent<TMP_Text>();
        if (tmp != null)
        {
            tmp.text = value;
            return;
        }

        UnityEngine.UI.Text legacyText = target.GetComponent<UnityEngine.UI.Text>();
        if (legacyText != null)
            legacyText.text = value;
    }

    private void SetFill(string objectName, float value)
    {
        GameObject target = FindSceneObject(objectName);
        RectTransform rect = target != null ? target.GetComponent<RectTransform>() : null;
        if (rect != null)
        {
            const float maximumWidth = 390f;
            Vector2 size = rect.sizeDelta;
            size.x = maximumWidth * Mathf.Clamp01(value);
            rect.sizeDelta = size;
        }
    }

    private void CacheSceneObjects()
    {
        if (objectCache.Count > 0)
            return;

        GameObject[] objects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject sceneObject in objects)
        {
            if (sceneObject == null || !sceneObject.scene.IsValid() || objectCache.ContainsKey(sceneObject.name))
                continue;

            objectCache.Add(sceneObject.name, sceneObject);
        }
    }

    private static GameObject FindSceneObject(string objectName)
    {
        if (objectCache.TryGetValue(objectName, out GameObject cached) && cached != null)
            return cached;

        GameObject[] objects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject sceneObject in objects)
        {
            if (sceneObject != null && sceneObject.scene.IsValid() && sceneObject.name == objectName)
            {
                objectCache[objectName] = sceneObject;
                return sceneObject;
            }
        }

        return null;
    }
}
