using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class WaspMouseRaycaster : MonoBehaviour
{
    [Header("Raycast")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private LayerMask waspLayer;
    [SerializeField] private float rayDistance = 1000f;

    [Header("Selection")]
    [SerializeField] private WaspCameraSwitcher cameraSwitcher;

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = GetComponent<Camera>();

        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    private void Update()
    {
        if (Mouse.current == null)
            return;

        if (!Mouse.current.leftButton.wasPressedThisFrame)
            return;

        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        TrySelectWasp();
    }

    private void TrySelectWasp()
    {
        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Ray ray = targetCamera.ScreenPointToRay(mousePosition);

        if (!Physics.Raycast(
                ray,
                out RaycastHit hit,
                rayDistance,
                waspLayer,
                QueryTriggerInteraction.Ignore))
        {
            return;
        }

        WaspInfo wasp =
            hit.collider.GetComponentInParent<WaspInfo>();

        if (wasp == null)
            return;

        cameraSwitcher.ShowWaspCamera(wasp);
    }
}