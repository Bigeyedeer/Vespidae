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

    [Header("Debug")]
    [SerializeField] private bool showDebugRay;

    private HexHoverEffect currentHoveredHex;
    private HexTile currentHexTile;
    private bool inHexView;

    public bool InHexView => inHexView;

    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

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

        if (inHexView)
        {
            ClearCurrentHover();

            if (cameraFocus == null || !cameraFocus.IsCloseUpActive)
                return;

            if (Mouse.current.leftButton.wasPressedThisFrame)
                TrySelectWasp();

            return;
        }

        DetectHexUnderCursor();

        if (Mouse.current.leftButton.wasPressedThisFrame)
            TrySelectCurrentHex();
    }

    private void DetectHexUnderCursor()
    {
        if (mainCamera == null)
            return;

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Ray ray = mainCamera.ScreenPointToRay(mousePosition);

        if (showDebugRay)
            Debug.DrawRay(ray.origin, ray.direction * rayDistance, Color.red);

        if (Physics.Raycast(
                ray,
                out RaycastHit hit,
                rayDistance,
                hexLayer,
                QueryTriggerInteraction.Ignore))
        {
            HexHoverEffect hoveredHex =
                hit.collider.GetComponentInParent<HexHoverEffect>();

            HexTile hoveredTile =
                hit.collider.GetComponentInParent<HexTile>();

            if (hoveredHex != currentHoveredHex)
            {
                ClearCurrentHover();

                currentHoveredHex = hoveredHex;
                currentHexTile = hoveredTile;

                if (currentHoveredHex != null)
                    currentHoveredHex.SetHovered(true);
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

    private void TrySelectWasp()
    {
        if (IsPointerOverUi() || closeUpCamera == null)
            return;

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Ray ray = closeUpCamera.ScreenPointToRay(mousePosition);

        if (showDebugRay)
            Debug.DrawRay(ray.origin, ray.direction * rayDistance, Color.yellow);

        if (!Physics.Raycast(
                ray,
                out RaycastHit hit,
                rayDistance,
                waspLayer,
                QueryTriggerInteraction.Ignore))
        {
            return;
        }

        WaspInfo wasp = hit.collider.GetComponentInParent<WaspInfo>();

        if (wasp == null)
            return;

        if (mainWorldNavigation != null)
            mainWorldNavigation.SelectWasp(wasp);
        else
            cameraFocus?.FocusOnWasp(wasp);
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

    private void OnDisable()
    {
        ClearCurrentHover();
    }
}
