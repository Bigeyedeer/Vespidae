using System.Collections.Generic;
using UnityEngine;

public class EnemyHiveControl : MonoBehaviour
{
    public static EnemyHiveControl Instance { get; private set; }

    [Header("Faction Species")]
    [SerializeField] private SB_Wasps_Info nativeSpecies;
    [SerializeField] private SB_Wasps_Info primaryInvasiveSpecies;
    [SerializeField] private SB_Wasps_Info secondaryInvasiveSpecies;
    [Header("Enemy Startup Spawning")]
    [SerializeField] private GameObject enemyHivePrefab;
    [SerializeField] private GameObject[] enemyWaspPrefabs;
    [SerializeField] private bool spawnEnemyStartup = true;
    [SerializeField] private bool spawnOneEnemyWasp = true;
    [SerializeField] private bool autoRegisterSceneWasps = true;

    private readonly Dictionary<WaspScopeRole, List<EnemyWaspControl>> factions = new Dictionary<WaspScopeRole, List<EnemyWaspControl>>();
    private readonly List<C_Enemy_Hive_Orc> spawnedEnemyHives = new List<C_Enemy_Hive_Orc>();
    private bool enemyStartupSpawned;

    public IReadOnlyList<EnemyWaspControl> NativeFaction => GetFaction(WaspScopeRole.NativePlayer);
    public IReadOnlyList<EnemyWaspControl> PrimaryInvasiveFaction => GetFaction(WaspScopeRole.PrimaryInvasive);
    public IReadOnlyList<EnemyWaspControl> SecondaryInvasiveFaction => GetFaction(WaspScopeRole.SecondaryInvasive);
    public SB_Wasps_Info NativeSpecies => nativeSpecies;
    public SB_Wasps_Info PrimaryInvasiveSpecies => primaryInvasiveSpecies;
    public SB_Wasps_Info SecondaryInvasiveSpecies => secondaryInvasiveSpecies;
    public GameObject EnemyHivePrefab => enemyHivePrefab;
    public IReadOnlyList<GameObject> EnemyWaspPrefabs => enemyWaspPrefabs;
    public IReadOnlyList<C_Enemy_Hive_Orc> SpawnedEnemyHives => spawnedEnemyHives;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        EnsureFactions();
    }

    private void Start()
    {
        if (!autoRegisterSceneWasps)
            return;

        EnemyWaspControl[] sceneWasps = FindObjectsByType<EnemyWaspControl>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (EnemyWaspControl wasp in sceneWasps)
            Register(wasp);

        SpawnEnemyStartup();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void Register(EnemyWaspControl wasp)
    {
        if (wasp == null)
            return;

        EnsureFactions();
        RemoveFromAllFactions(wasp);
        factions[wasp.Faction].Add(wasp);
    }

    public void Unregister(EnemyWaspControl wasp)
    {
        if (wasp == null)
            return;

        RemoveFromAllFactions(wasp);
    }

    public void RefreshRegistration(EnemyWaspControl wasp)
    {
        Register(wasp);
    }

    public IReadOnlyList<EnemyWaspControl> GetFaction(WaspScopeRole faction)
    {
        EnsureFactions();
        return factions[faction];
    }

    public int GetFactionCount(WaspScopeRole faction)
    {
        EnsureFactions();
        return factions[faction].Count;
    }

    public SB_Wasps_Info GetFactionSpecies(WaspScopeRole faction)
    {
        switch (faction)
        {
            case WaspScopeRole.NativePlayer:
                return nativeSpecies;
            case WaspScopeRole.PrimaryInvasive:
                return primaryInvasiveSpecies;
            case WaspScopeRole.SecondaryInvasive:
                return secondaryInvasiveSpecies;
            default:
                return null;
        }
    }

    public void SetFactionAlert(WaspScopeRole faction, bool value)
    {
        foreach (EnemyWaspControl wasp in GetFaction(faction))
        {
            if (wasp != null)
                wasp.SetAlerted(value);
        }
    }

    public void SetFactionDestination(WaspScopeRole faction, Vector3 worldPosition)
    {
        foreach (EnemyWaspControl wasp in GetFaction(faction))
        {
            if (wasp != null)
                wasp.SetDestination(worldPosition);
        }
    }

    public void SpawnEnemyStartup()
    {
        if (enemyStartupSpawned)
            return;

        enemyStartupSpawned = true;
        if (!spawnEnemyStartup)
            return;

        if (enemyHivePrefab == null)
        {
            Debug.LogWarning("EnemyHiveControl cannot spawn enemy hives because no enemy hive prefab is assigned.");
            return;
        }

        HexTile[] hexTiles = FindObjectsByType<HexTile>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        int enemyTileIndex = 0;
        foreach (HexTile hexTile in hexTiles)
        {
            if (hexTile == null || hexTile.State != HexTile.HexState.Enemy)
                continue;

            Transform spawnPoint = hexTile.HiveSpawnPoint;
            GameObject hiveObject = Instantiate(enemyHivePrefab, spawnPoint.position, spawnPoint.rotation);
            C_Enemy_Hive_Orc hive = hiveObject.GetComponent<C_Enemy_Hive_Orc>();
            if (hive == null)
            {
                Debug.LogWarning($"{enemyHivePrefab.name} does not contain C_Enemy_Hive_Orc.");
                enemyTileIndex++;
                continue;
            }

            hive.Initialize(hexTile, enemyWaspPrefabs);
            spawnedEnemyHives.Add(hive);
            GameObject speciesPrefab = GetEnemyWaspPrefab(enemyTileIndex);
            if (spawnOneEnemyWasp)
                hive.SpawnWasp(speciesPrefab, WaspFunction.Scout);

            enemyTileIndex++;
        }
    }

    public EnemyWaspControl SpawnEnemyWasp(C_Enemy_Hive_Orc hive, int speciesIndex)
    {
        if (hive == null)
            return null;

        return hive.SpawnWasp(GetEnemyWaspPrefab(speciesIndex), WaspFunction.Scout);
    }

    private void EnsureFactions()
    {
        if (factions.Count > 0)
            return;

        factions[WaspScopeRole.NativePlayer] = new List<EnemyWaspControl>();
        factions[WaspScopeRole.PrimaryInvasive] = new List<EnemyWaspControl>();
        factions[WaspScopeRole.SecondaryInvasive] = new List<EnemyWaspControl>();
    }

    private void RemoveFromAllFactions(EnemyWaspControl wasp)
    {
        foreach (List<EnemyWaspControl> faction in factions.Values)
            faction.Remove(wasp);
    }

    private GameObject GetEnemyWaspPrefab(int speciesIndex)
    {
        if (enemyWaspPrefabs == null || enemyWaspPrefabs.Length == 0)
            return null;

        int index = Mathf.Clamp(speciesIndex, 0, enemyWaspPrefabs.Length - 1);
        return enemyWaspPrefabs[index];
    }
}
