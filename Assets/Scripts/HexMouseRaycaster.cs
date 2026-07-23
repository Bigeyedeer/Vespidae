using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class HexMouseRaycaster : MonoBehaviour
{
    [Header("Raycast")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private LayerMask hexLayer;
    [SerializeField] private float rayDistance = 1000f;

    [Header("Selection")]
    [SerializeField] private HexCameraFocus cameraFocus;
    [SerializeField] private HexOptionsPanel optionsPanel;

    [Header("Debug")]
    [SerializeField] private bool showDebugRay;

    private HexHoverEffect currentHoveredHex;
    private HexTile currentHexTile;

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = GetComponent<Camera>();

        if (targetCamera == null)
            targetCamera = Camera.main;

        if (cameraFocus == null)
            cameraFocus = GetComponent<HexCameraFocus>();
    }

    private void Update()
    {
        DetectHexUnderCursor();

        if (Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            TrySelectCurrentHex();
        }
    }

    private void DetectHexUnderCursor()
    {
        if (Mouse.current == null || targetCamera == null)
            return;

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Ray ray = targetCamera.ScreenPointToRay(mousePosition);

        if (showDebugRay)
        {
            Debug.DrawRay(
                ray.origin,
                ray.direction * rayDistance,
                Color.red
            );
        }

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
        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        if (currentHexTile == null)
            return;

        cameraFocus.FocusOnHex(currentHexTile);
        optionsPanel.Open(currentHexTile);
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