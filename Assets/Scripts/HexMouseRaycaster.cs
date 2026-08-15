using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class HexMouseRaycaster : MonoBehaviour
{
    [Header("Raycast Cameras")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Camera closeUpCamera;
    [SerializeField] private float rayDistance = 1000f;

    [Header("Interaction Layers")]
    [SerializeField] private LayerMask hexLayer;
    [SerializeField] private LayerMask waspLayer;

    [Header("Selection")]
    [SerializeField] private C_MainWorldNavigation mainWorldNavigation;
    [SerializeField] private C_MainWorldCameraFocus cameraFocus;
    [SerializeField] private HexOptionsPanel optionsPanel;
    [SerializeField] private WaspControlGroupManager controlGroupManager;

    [Header("Debug")]
    [SerializeField] private bool showDebugRay;

    private HexHoverEffect currentHoveredHex;
    private HexTile currentHexTile;
    private HiveHoverEffect currentHoveredHive;
    private C_Friendly_Hive_Orc currentHive;
    private C_Enemy_Hive_Orc currentEnemyHive;
    private WaspInfo currentWasp;
    private bool inHexView;

    public bool InHexView => inHexView;
    public HexTile CurrentHoveredHexTile => currentHexTile;
    public bool CanZoomCurrentHex => !inHexView &&
                                     currentHexTile != null &&
                                     !IsPointerOverUi();

    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (controlGroupManager == null)
            controlGroupManager = GetComponent<WaspControlGroupManager>();

        if (hexLayer.value == 0)
        {
            int hexLayerIndex = LayerMask.NameToLayer("HexTile");
            if (hexLayerIndex >= 0)
                hexLayer = 1 << hexLayerIndex;
        }

        inHexView = false;
    }

    private void Update()
    {
        if (Mouse.current == null)
            return;

        if (inHexView &&
            cameraFocus != null &&
            !cameraFocus.IsCloseUpActive &&
            !cameraFocus.IsTransitioning)
        {
            inHexView = false;
        }

        if (Mouse.current.rightButton.wasPressedThisFrame)
            TryIssueGroupOrder();

        if (inHexView)
        {
            if (cameraFocus == null || !cameraFocus.IsCloseUpActive)
                return;

            DetectCloseUpInteractable();
            if (Mouse.current.leftButton.wasReleasedThisFrame &&
                (controlGroupManager == null || !controlGroupManager.ShouldSuppressWorldClickThisFrame))
                TrySelectCloseUpInteractable();

            return;
        }

        DetectHexUnderCursor();

        if (Mouse.current.leftButton.wasReleasedThisFrame &&
            (controlGroupManager == null || !controlGroupManager.ShouldSuppressWorldClickThisFrame))
            TrySelectCurrentHex();
    }

    private void TryIssueGroupOrder()
    {
        if (controlGroupManager == null || !controlGroupManager.HasSelection || IsPointerOverUi())
            return;

        // A double right-click is a deselect, not a move order.
        if (controlGroupManager.ShouldSuppressOrderThisFrame)
            return;

        if (C_MainWorldOverlayNavigation.Instance != null && C_MainWorldOverlayNavigation.Instance.BlocksWorldInput)
            return;

        Camera activeCamera = cameraFocus != null ? cameraFocus.ActiveCamera : mainCamera;
        if (activeCamera == null)
            return;

        Ray ray = activeCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (!Physics.Raycast(ray, out RaycastHit hit, rayDistance, hexLayer, QueryTriggerInteraction.Ignore))
            return;

        HexTile target = hit.collider.GetComponentInParent<HexTile>();
        if (target != null)
            controlGroupManager.TryMoveSelectedToHex(target);
    }

    private void DetectHexUnderCursor()
    {
        if (mainCamera == null)
            return;

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Ray ray = mainCamera.ScreenPointToRay(mousePosition);

        if (showDebugRay)
            Debug.DrawRay(ray.origin, ray.direction * rayDistance, Color.red);

        // Hives and wasps sit on the hex and carry their own layer, so the mask has to include them
        // and then map back to the tile they belong to. Casting the hex layer alone means the cursor
        // crossing onto a hive drops the tile it is standing on.
        if (Physics.Raycast(
                ray,
                out RaycastHit hit,
                rayDistance,
                hexLayer | waspLayer,
                QueryTriggerInteraction.Ignore))
        {
            HexTile hoveredTile = ResolveHexFromCollider(hit.collider);
            HexHoverEffect hoveredHex = hoveredTile != null
                ? hoveredTile.GetComponent<HexHoverEffect>()
                : null;

            if (hoveredHex != currentHoveredHex)
            {
                ClearCurrentHover();

                currentHoveredHex = hoveredHex;
                currentHexTile = hoveredTile;

                if (currentHoveredHex != null)
                    currentHoveredHex.SetHovered(true);

                if (currentHexTile != null)
                    C_MainWorldHUD.GetOrCreate()?.ShowSelectedHex(currentHexTile);
            }
        }
        else
        {
            ClearCurrentHover();
        }
    }

    private void TrySelectCurrentHex()
    {
        if (IsPointerOverUi() || currentHexTile == null)
            return;

        if (cameraFocus == null)
            return;

        if (mainWorldNavigation != null)
            mainWorldNavigation.SelectHex(currentHexTile);
        else
            cameraFocus.FocusOnHex(currentHexTile);

        inHexView = true;

        if (mainWorldNavigation == null && optionsPanel != null)
            optionsPanel.Open(currentHexTile);
    }

    private void DetectCloseUpInteractable()
    {
        if (closeUpCamera == null)
            return;

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Ray ray = closeUpCamera.ScreenPointToRay(mousePosition);

        if (showDebugRay)
            Debug.DrawRay(ray.origin, ray.direction * rayDistance, Color.yellow);

        RaycastHit[] hits = Physics.RaycastAll(
            ray,
            rayDistance,
            ~0,
            QueryTriggerInteraction.Collide);

        Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
        C_Friendly_Hive_Orc hoveredHive = null;
        C_Enemy_Hive_Orc hoveredEnemyHive = null;
        HiveHoverEffect hoverEffect = null;
        WaspInfo hoveredWasp = null;
        HexHoverEffect hoveredHex = null;
        HexTile hoveredTile = null;

        foreach (RaycastHit hit in hits)
        {
            // Work out which hex this belongs to before deciding what was hit. Whether the cursor is
            // over the tile, the hive standing on it, or a wasp stationed there, it is all one hex as
            // far as the player is concerned, so the tile stays raised throughout.
            if (hoveredTile == null)
            {
                HexTile owner = ResolveHexFromCollider(hit.collider);
                if (owner != null)
                {
                    hoveredTile = owner;
                    hoveredHex = owner.GetComponent<HexHoverEffect>();
                }
            }

            C_Friendly_Hive_Orc hive = hit.collider.GetComponentInParent<C_Friendly_Hive_Orc>();
            if (hive != null)
            {
                hoveredHive = hive;
                hoverEffect = hive.GetComponent<HiveHoverEffect>();
                break;
            }

            C_Enemy_Hive_Orc enemyHive = hit.collider.GetComponentInParent<C_Enemy_Hive_Orc>();
            if (enemyHive != null)
            {
                hoveredEnemyHive = enemyHive;
                hoverEffect = enemyHive.GetComponent<HiveHoverEffect>();
                break;
            }

            WaspInfo wasp = hit.collider.GetComponentInParent<WaspInfo>();
            if (wasp != null)
            {
                hoveredWasp = wasp;
                break;
            }
        }

        if (hoveredHex != currentHoveredHex)
        {
            ClearCurrentHover();
            currentHoveredHex = hoveredHex;
            currentHexTile = hoveredTile;
            currentHoveredHex?.SetHovered(true);
        }
        else
        {
            currentHexTile = hoveredTile;
        }

        if (hoverEffect != currentHoveredHive)
        {
            ClearCurrentHiveHover();
            currentHoveredHive = hoverEffect;
            if (currentHoveredHive != null)
                currentHoveredHive.SetHovered(true);
        }

        currentHive = hoveredHive;
        currentEnemyHive = hoveredEnemyHive;
        currentWasp = hoveredWasp;
    }

    private void TrySelectCloseUpInteractable()
    {
        if (IsPointerOverUi())
            return;

        if (currentHive != null)
        {
            mainWorldNavigation?.SelectHive(currentHive);
            return;
        }

        if (currentEnemyHive != null)
        {
            mainWorldNavigation?.SelectHive(currentEnemyHive);
            return;
        }

        if (currentWasp != null)
        {
            if (mainWorldNavigation != null)
                mainWorldNavigation.SelectWasp(currentWasp);
            else
                cameraFocus?.FocusOnWasp(currentWasp);
            return;
        }

        if (currentHexTile != null)
        {
            if (mainWorldNavigation != null)
                mainWorldNavigation.SelectHex(currentHexTile);
            else
                cameraFocus?.FocusOnHex(currentHexTile);
        }
    }

    /// <summary>
    /// Works out which hex a collider belongs to, so that anything standing on a tile counts as the
    /// tile for hover purposes.
    ///
    /// Hives are parented under their hex, so the parent walk finds them. Wasps are not - they are
    /// spawned loose and move around - so they fall back to the hex they are stationed on, and then
    /// the one they are travelling to.
    /// </summary>
    private HexTile ResolveHexFromCollider(Collider collider)
    {
        if (collider == null)
            return null;

        HexTile tile = collider.GetComponentInParent<HexTile>();
        if (tile != null)
            return tile;

        C_Friendly_Hive_Orc friendlyHive = collider.GetComponentInParent<C_Friendly_Hive_Orc>();
        if (friendlyHive != null && friendlyHive.OwnerHex != null)
            return friendlyHive.OwnerHex;

        C_Enemy_Hive_Orc enemyHive = collider.GetComponentInParent<C_Enemy_Hive_Orc>();
        if (enemyHive != null && enemyHive.OwnerHex != null)
            return enemyHive.OwnerHex;

        WaspControl friendlyWasp = collider.GetComponentInParent<WaspControl>();
        if (friendlyWasp != null)
        {
            HexTile assigned = friendlyWasp.StationedHex != null ? friendlyWasp.StationedHex : friendlyWasp.TargetHex;
            if (assigned != null)
                return assigned;
        }

        EnemyWaspControl enemyWasp = collider.GetComponentInParent<EnemyWaspControl>();
        if (enemyWasp != null)
        {
            HexTile assigned = enemyWasp.StationedHex != null ? enemyWasp.StationedHex : enemyWasp.TargetHex;
            if (assigned != null)
                return assigned;
        }

        return FindHexBeneath(collider.transform.position);
    }

    /// <summary>
    /// Last resort: look straight down for a tile. A wasp idling at its home hive has no stationed or
    /// target hex, but it is still standing on one, and the player expects that tile to light up.
    /// </summary>
    private HexTile FindHexBeneath(Vector3 worldPosition)
    {
        if (Physics.Raycast(
                worldPosition + Vector3.up * 5f,
                Vector3.down,
                out RaycastHit hit,
                rayDistance,
                hexLayer,
                QueryTriggerInteraction.Ignore))
        {
            return hit.collider.GetComponentInParent<HexTile>();
        }

        return null;
    }

    private bool IsPointerOverUi()
    {
        return EventSystem.current != null &&
               EventSystem.current.IsPointerOverGameObject();
    }

    private void ClearCurrentHover()
    {
        if (currentHoveredHex != null)
            currentHoveredHex.SetHovered(false);

        currentHoveredHex = null;
        currentHexTile = null;
    }

    private void ClearCurrentHiveHover()
    {
        if (currentHoveredHive != null)
            currentHoveredHive.SetHovered(false);

        currentHoveredHive = null;
        currentHive = null;
        currentEnemyHive = null;
        currentWasp = null;
    }

    private void OnDisable()
    {
        ClearCurrentHover();
        ClearCurrentHiveHover();
    }
}
