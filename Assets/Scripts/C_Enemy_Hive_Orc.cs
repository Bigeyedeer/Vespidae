using System.Collections.Generic;
using UnityEngine;

public class C_Enemy_Hive_Orc : MonoBehaviour
{
    [SerializeField] private GameObject[] enemyWaspPrefabs;
    [SerializeField] private Transform waspSpawnPoint;
    [SerializeField] private Transform cameraFocusPoint;
    [SerializeField] private Transform cameraLookPoint;
    [SerializeField] private HiveCombatant combatant;
    [SerializeField] private WaspScopeRole faction = WaspScopeRole.PrimaryInvasive;
    [SerializeField, Min(0f)] private float spawnHeight = 0.35f;
    [SerializeField, Min(0.05f)] private float spawnSpacing = 0.25f;
    [SerializeField, Min(0.05f)] private float spawnRowSpacing = 0.25f;
    

    private float storedPrey;
    private float storedNectar;
    private float storedFibre;
    
    public float StoredPrey => storedPrey;
    public float StoredNectar => storedNectar;
    public float StoredFibre => storedFibre;
    
    private HexTile ownerHex;
    private int nextSpawnIndex;
    private GameObject defaultWaspPrefab;
    private readonly List<HexTile> knownHexes = new List<HexTile>();

    public GameObject[] EnemyWaspPrefabs => enemyWaspPrefabs;
    public HexTile OwnerHex => ownerHex;
    public IReadOnlyList<HexTile> KnownHexes => knownHexes;
    public Transform WaspSpawnPoint => waspSpawnPoint != null ? waspSpawnPoint : transform;
    public Transform CameraFocusPoint => cameraFocusPoint != null ? cameraFocusPoint : transform;
    public Transform CameraLookPoint => cameraLookPoint != null ? cameraLookPoint : transform;
    public HiveCombatant Combatant => combatant;
    public GameObject DefaultWaspPrefab => defaultWaspPrefab != null ? defaultWaspPrefab : GetDefaultWaspPrefab();
    public WaspScopeRole Faction => faction;

    public void Initialize(HexTile hex, GameObject[] waspPrefabs)
    {
        ownerHex = hex;
        if (combatant == null)
            combatant = GetComponent<HiveCombatant>();
        combatant?.Initialize(hex, true);
        AttachToOwnerHex();
        if (waspPrefabs != null && waspPrefabs.Length > 0)
            enemyWaspPrefabs = waspPrefabs;
        UpdateFactionFromPrefab(DefaultWaspPrefab);
        RememberHex(ownerHex);
        ownerHex?.SetEnemyHive(this);
    }
    
    public void AddResources(float prey, float nectar, float fibre)
    {
        storedPrey += prey;
        storedNectar += nectar;
        storedFibre += fibre;
    }

    private void AttachToOwnerHex()
    {
        if (ownerHex == null)
            return;

        Transform spawnPoint = ownerHex.HiveSpawnPoint;
        transform.SetParent(spawnPoint, true);
        transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
    }

    public EnemyWaspControl SpawnWasp(
        GameObject waspPrefab = null,
        WaspFunction function = WaspFunction.Scout)
    {
        GameObject prefab = waspPrefab != null ? waspPrefab : DefaultWaspPrefab;
        if (prefab == null)
        {
            Debug.LogWarning($"{name} has no enemy wasp prefab assigned.");
            return null;
        }

        Transform point = ownerHex != null ? ownerHex.transform : WaspSpawnPoint;
        Vector3 basePosition = ownerHex != null
            ? ownerHex.GetWaspFormationPosition(nextSpawnIndex, spawnSpacing, spawnRowSpacing)
            : point.position;

        Vector3 position = basePosition + Vector3.up * spawnHeight;
        GameObject instance = Instantiate(prefab, position, point.rotation);
        EnemyWaspControl control = instance.GetComponent<EnemyWaspControl>();
        if (control == null)
            Debug.LogWarning($"{prefab.name} does not contain an EnemyWaspControl component.");
        else
        {
            control.SetFaction(faction);
            control.InitializeEnemyWasp(this, function);
            nextSpawnIndex++;
        }

        return control;
    }

    public void SetDefaultWaspPrefab(GameObject prefab)
    {
        if (prefab != null)
        {
            defaultWaspPrefab = prefab;
            UpdateFactionFromPrefab(prefab);
            ownerHex?.SetEnemyHive(this);
        }
    }

    private void UpdateFactionFromPrefab(GameObject prefab)
    {
        WaspInfo info = prefab != null ? prefab.GetComponent<WaspInfo>() : null;
        if (info != null && info.SpeciesInfo != null)
            faction = info.SpeciesInfo.ScopeRole;
    }

    public void RememberHex(HexTile hex)
    {
        if (hex == null)
            return;

        if (knownHexes.Contains(hex))
            return;

        knownHexes.Add(hex);

        Debug.Log($"{name} discovered {hex.HexName}");
    }
    
    public bool KnowsHex(HexTile hex)
    {
        return knownHexes.Contains(hex);
    }
    
    private GameObject GetDefaultWaspPrefab()
    {
        return enemyWaspPrefabs != null && enemyWaspPrefabs.Length > 0 ? enemyWaspPrefabs[0] : null;
    }
}
