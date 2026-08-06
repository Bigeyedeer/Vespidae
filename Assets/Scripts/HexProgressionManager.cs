using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-200)]
public class HexProgressionManager : MonoBehaviour
{
    public static HexProgressionManager Instance { get; private set; }

    [SerializeField] private bool initializeOnAwake = true;

    private readonly Dictionary<string, HexTile> tilesById = new Dictionary<string, HexTile>();
    private readonly Dictionary<HexTile, List<HexTile>> connections = new Dictionary<HexTile, List<HexTile>>();
    private bool initialized;

    public bool IsInitialized => initialized;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        if (initializeOnAwake)
            InitializeMap();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void InitializeMap()
    {
        tilesById.Clear();
        connections.Clear();

        HexTile[] tiles = FindObjectsByType<HexTile>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (HexTile tile in tiles)
        {
            if (tile == null || tile.AreaInfo == null || string.IsNullOrWhiteSpace(tile.AreaInfo.AreaId))
                continue;

            tilesById[tile.AreaInfo.AreaId] = tile;
            connections[tile] = new List<HexTile>();
        }

        foreach (KeyValuePair<HexTile, List<HexTile>> entry in connections)
        {
            IReadOnlyList<string> ids = entry.Key.AreaInfo.ConnectedHexIds;
            foreach (string id in ids)
            {
                if (tilesById.TryGetValue(id, out HexTile connected) && connected != entry.Key && !entry.Value.Contains(connected))
                    entry.Value.Add(connected);
            }
        }

        foreach (HexTile tile in tiles)
            tile.SetPlayerAccessible(tile.State == HexTile.HexState.Owned);

        foreach (HexTile tile in tiles)
        {
            if (tile.State == HexTile.HexState.Owned)
                UnlockPlayerNeighbours(tile);
        }

        initialized = true;
    }

    public IReadOnlyList<HexTile> GetConnectedHexes(HexTile tile)
    {
        if (tile != null && connections.TryGetValue(tile, out List<HexTile> result))
            return result;
        return System.Array.Empty<HexTile>();
    }

    public bool AreConnected(HexTile first, HexTile second)
    {
        return first != null && second != null &&
               connections.TryGetValue(first, out List<HexTile> result) &&
               result.Contains(second);
    }

    public bool CanPlayerTarget(HexTile tile)
    {
        return tile != null && tile.IsPlayerAccessible && tile.State != HexTile.HexState.Locked;
    }

    public bool CanEnemyTarget(HexTile tile)
    {
        if (tile == null)
            return false;

        foreach (KeyValuePair<HexTile, List<HexTile>> entry in connections)
        {
            if (entry.Key.State == HexTile.HexState.Enemy && entry.Value.Contains(tile))
                return true;
        }
        return false;
    }

    public void NotifyFriendlyClaimed(HexTile tile)
    {
        if (tile == null)
            return;

        tile.SetPlayerAccessible(true);
        UnlockPlayerNeighbours(tile);
    }

    public void NotifyEnemyClaimed(HexTile tile)
    {
        tile?.RefreshStateVisuals();
    }

    private void UnlockPlayerNeighbours(HexTile tile)
    {
        foreach (HexTile neighbour in GetConnectedHexes(tile))
            neighbour.SetPlayerAccessible(true);
    }
}
