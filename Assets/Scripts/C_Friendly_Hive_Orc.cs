using UnityEngine;

public class C_Friendly_Hive_Orc : MonoBehaviour
{
    [SerializeField] private GameObject friendlyWaspPrefab;
    [SerializeField] private Transform waspSpawnPoint;
    [SerializeField] private Transform cameraFocusPoint;
    [SerializeField] private Transform cameraLookPoint;
    [SerializeField, Min(0.05f)] private float spawnSpacing = 0.25f;
    [SerializeField, Min(0.05f)] private float spawnRowSpacing = 0.25f;

    private HexTile ownerHex;
    private int nextSpawnIndex;

    public GameObject FriendlyWaspPrefab => friendlyWaspPrefab;
    public HexTile OwnerHex => ownerHex;
    public Transform WaspSpawnPoint => waspSpawnPoint != null ? waspSpawnPoint : transform;
    public Transform CameraFocusPoint => cameraFocusPoint != null ? cameraFocusPoint : transform;
    public Transform CameraLookPoint => cameraLookPoint != null ? cameraLookPoint : transform;

    public void Initialize(HexTile hex, GameObject waspPrefab)
    {
        ownerHex = hex;
        if (waspPrefab != null)
            friendlyWaspPrefab = waspPrefab;
    }

    public WaspControl SpawnWasp(GameObject waspPrefab = null)
    {
        GameObject prefab = waspPrefab != null ? waspPrefab : friendlyWaspPrefab;
        if (prefab == null)
        {
            Debug.LogWarning($"{name} has no friendly wasp prefab assigned.");
            return null;
        }

        Transform point = ownerHex != null ? ownerHex.transform : WaspSpawnPoint;
        Vector3 position = ownerHex != null
            ? ownerHex.GetWaspFormationPosition(nextSpawnIndex, spawnSpacing, spawnRowSpacing)
            : point.position;

        GameObject instance = Instantiate(prefab, position, point.rotation);
        WaspControl control = instance.GetComponent<WaspControl>();
        if (control == null)
            Debug.LogWarning($"{prefab.name} does not contain a WaspControl component.");
        else
            nextSpawnIndex++;

        return control;
    }
}
