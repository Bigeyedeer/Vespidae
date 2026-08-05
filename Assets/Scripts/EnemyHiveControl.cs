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
    [SerializeField] private float scoutInterval = 10f;
    [SerializeField] private float foragerInterval = 6f;

    private float foragerTimer;

    private float scoutTimer;

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

        EnemyWaspControl[] sceneWasps = FindObjectsByType<EnemyWaspControl>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (EnemyWaspControl wasp in sceneWasps)
            Register(wasp);

        SpawnEnemyStartup();
    }
    
    private void Update()
    {
        scoutTimer += Time.deltaTime;

        if (scoutTimer < scoutInterval)
            return;

        scoutTimer = Random.Range(0f, scoutInterval * 0.3f);

        RunScoutBehaviour();
        
        foragerTimer += Time.deltaTime;

        if (foragerTimer >= foragerInterval)
        {
            foragerTimer = Random.Range(0f, foragerInterval * 0.3f);

            RunForagerBehaviour();
        }
    }
    
    private void RunForagerBehaviour()
    {
        foreach (C_Enemy_Hive_Orc hive in spawnedEnemyHives)
        {
            if (hive == null)
                continue;

            EnemyWaspControl forager = null;

            foreach (EnemyWaspControl wasp in GetFaction(WaspScopeRole.PrimaryInvasive))
            {
                if (wasp == null)
                    continue;

                if (wasp.HomeHive != hive)
                    continue;

                if (wasp.AssignedFunction != WaspFunction.Forager)
                    continue;

                forager = wasp;
                break;
            }

            if (forager == null)
                continue;

            if (forager.WorkforceState == WaspWorkforceState.Travelling)
                continue;

            HexTile target = ChooseForagingTarget(hive);

            if (target != null)
            {
                Debug.Log($"Enemy forager heading to {target.HexName}");

                forager.DispatchToHex(target);
            }
        }
    }
    
    private HexTile ChooseForagingTarget(C_Enemy_Hive_Orc hive)
    {
        if (hive == null)
            return null;

        List<HexTile> candidates = new List<HexTile>();

        foreach (HexTile hex in hive.KnownHexes)
        {
            if (hex == null)
                continue;

            if (!hex.HasResources)
                continue;

            candidates.Add(hex);
        }

        if (candidates.Count == 0)
            return null;

        candidates.Sort((a, b) =>
        {
            float da = Vector3.Distance(
                hive.OwnerHex.transform.position,
                a.transform.position);

            float db = Vector3.Distance(
                hive.OwnerHex.transform.position,
                b.transform.position);

            return da.CompareTo(db);
        });

        return candidates[0];
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
            {
                hive.SpawnWasp(speciesPrefab, WaspFunction.Scout);
                hive.SpawnWasp(speciesPrefab, WaspFunction.Forager);
            }

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
    
    private void RunScoutBehaviour()
    {
        foreach (C_Enemy_Hive_Orc hive in spawnedEnemyHives)
        {
            if (hive == null)
                continue;
            

            EnemyWaspControl scout = null;

            foreach (EnemyWaspControl wasp in GetFaction(WaspScopeRole.PrimaryInvasive))
            {
                if (wasp == null)
                    continue;

                if (wasp.HomeHive != hive)
                    continue;

                if (wasp.AssignedFunction != WaspFunction.Scout)
                    continue;

                scout = wasp;
                break;
            }

            if (scout == null)
                continue;
            
            if (scout.WorkforceState == WaspWorkforceState.Travelling)
                continue;

            HexTile target = ChooseScoutTarget(hive);

            if (target != null)
            {
                Debug.Log($"Enemy scout heading to {target.name}");
                scout.DispatchToHex(target);
            }
        }
    }

    private HexTile ChooseScoutTarget(C_Enemy_Hive_Orc hive)
    {
        if (hive == null)
            return null;

        HexTile[] allHexes = FindObjectsByType<HexTile>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        List<HexTile> candidates = new List<HexTile>();

        foreach (HexTile hex in allHexes)
        {
            if (hex == null)
                continue;

           
            if (hex.State == HexTile.HexState.Enemy)
                continue;

         
            if (hex.State == HexTile.HexState.Locked)
                continue;
            
            if (hex == hive.OwnerHex)
                continue;

            if (hive.KnowsHex(hex))
                continue;
            
            candidates.Add(hex);
        }

        if (candidates.Count == 0)
            return null;

        candidates.Sort((a, b) =>
        {
            float da = Vector3.Distance(
                hive.OwnerHex.transform.position,
                a.transform.position);

            float db = Vector3.Distance(
                hive.OwnerHex.transform.position,
                b.transform.position);

            return da.CompareTo(db);
        });

        int limit = Mathf.Min(5, candidates.Count);

        return candidates[Random.Range(0, limit)];
    }
    
    private GameObject GetEnemyWaspPrefab(int speciesIndex)
    {
        if (enemyWaspPrefabs == null || enemyWaspPrefabs.Length == 0)
            return null;

        int index = Mathf.Clamp(speciesIndex, 0, enemyWaspPrefabs.Length - 1);
        return enemyWaspPrefabs[index];
    }
}
