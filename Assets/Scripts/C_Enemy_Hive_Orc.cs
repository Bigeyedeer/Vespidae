using UnityEngine;

public class C_Enemy_Hive_Orc : MonoBehaviour
{
    [SerializeField] private GameObject[] enemyWaspPrefabs;
    [SerializeField] private Transform waspSpawnPoint;
    [SerializeField] private Transform cameraFocusPoint;
    [SerializeField] private Transform cameraLookPoint;
    [SerializeField, Min(0f)] private float spawnHeight = 0.35f;
    [SerializeField, Min(0.05f)] private float spawnSpacing = 0.25f;
    [SerializeField, Min(0.05f)] private float spawnRowSpacing = 0.25f;

    private HexTile ownerHex;
    private int nextSpawnIndex;

    public GameObject[] EnemyWaspPrefabs => enemyWaspPrefabs;
    public HexTile OwnerHex => ownerHex;
    public Transform WaspSpawnPoint => waspSpawnPoint != null ? waspSpawnPoint : transform;
    public Transform CameraFocusPoint => cameraFocusPoint != null ? cameraFocusPoint : transform;
    public Transform CameraLookPoint => cameraLookPoint != null ? cameraLookPoint : transform;

    public void Initialize(HexTile hex, GameObject[] waspPrefabs)
    {
        ownerHex = hex;
        AttachToOwnerHex();
        if (waspPrefabs != null && waspPrefabs.Length > 0)
            enemyWaspPrefabs = waspPrefabs;
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
        GameObject prefab = waspPrefab != null ? waspPrefab : GetDefaultWaspPrefab();
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
            control.InitializeEnemyWasp(this, function);
            nextSpawnIndex++;
        }

        return control;
    }

    private GameObject GetDefaultWaspPrefab()
    {
        return enemyWaspPrefabs != null && enemyWaspPrefabs.Length > 0 ? enemyWaspPrefabs[0] : null;
    }
}
