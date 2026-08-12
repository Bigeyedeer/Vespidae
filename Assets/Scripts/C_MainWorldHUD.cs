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
    }

    private void Start()
    {
        RefreshAll();
    }

    private void OnDestroy()
    {
        SubscribeToSelectedHex(null);
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
        SubscribeToSelectedHex(hex);
        selectedHex = hex;
        RefreshTerritory(hex);
    }

    private void SubscribeToSelectedHex(HexTile hex)
    {
        if (selectedHex != null)
            selectedHex.TerritoryInformationChanged -= RefreshSelectedTerritory;

        if (hex != null)
            hex.TerritoryInformationChanged += RefreshSelectedTerritory;
    }

    private void RefreshSelectedTerritory(HexTile hex)
    {
        if (hex == selectedHex)
            RefreshTerritory(hex);
    }

    private void RefreshResources()
    {
        ResourceManager resources = ResourceManager.Instance;
        if (resources == null)
            return;

        SetText("ResourceLabel_0", "Nectar");
        SetText("ResourceLabel_1", "Prey");
        SetText("ResourceLabel_2", "Fibre");
        SetText("Resource_0", $"{resources.Nectar:0}");
        SetText("Resource_1", $"{resources.Prey:0}");
        SetText("Resource_2", $"{resources.Fibre:0}");
    }

    private void RefreshColony()
    {
        HiveManagement hive = HiveManagement.Instance;
        if (hive == null)
            return;

        // The Workers card was removed from the resource bar: the colony panel below already
        // reports the same headcount in more detail.
        SetText("ResourceLabel_4", "Strength");
        SetText("ResourceLabel_5", "Brood");
        SetText("Resource_4", $"{hive.ColonyStrength:0}");
        SetText("Resource_5", $"{hive.BroodProgress:0}/{hive.BroodCapacity:0}");

        // Brood and Containment chips were removed from the colony panel. "Attackers" is the
        // player-facing name for WaspFunction.Guard.
        SetText("RoleChipLabel_0", $"Scout {hive.GetTotalWaspCount(WaspFunction.Scout)}");
        SetText("RoleChipLabel_1", $"Forager {hive.GetTotalWaspCount(WaspFunction.Forager)}");
        SetText("RoleChipLabel_2", $"Builder {hive.GetTotalWaspCount(WaspFunction.Builder)}");
        SetText("RoleChipLabel_4", $"Attackers {hive.GetTotalWaspCount(WaspFunction.Guard)}");
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

        if (hex.VisibilityState == HexVisibilityState.Hidden)
        {
            SetText("TerritoryTitle", "Unidentified Territory");
            SetText("TerritoryInfo", "Visibility: Hidden\nDispatch a Scout to investigate.");
            SetText("TerritoryNote", "Status: Unknown");
            return;
        }

        SetText("TerritoryTitle", hex.HexName);
        SetText("TerritoryInfo", $"{hex.HabitatCue}\nTerritory: {hex.TerritoryState} · {hex.ConnectedSiteCount} connected sites");
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
        if (rect == null)
            return;

        // Measure the matching bar background rather than assuming a width, so resizing the
        // panel in the scene cannot leave the fill over- or under-shooting its track.
        GameObject background = FindSceneObject(objectName.Replace("Fill", "Background"));
        RectTransform backgroundRect = background != null ? background.GetComponent<RectTransform>() : null;
        float maximumWidth = backgroundRect != null ? backgroundRect.sizeDelta.x : 390f;

        Vector2 size = rect.sizeDelta;
        size.x = maximumWidth * Mathf.Clamp01(value);
        rect.sizeDelta = size;
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
