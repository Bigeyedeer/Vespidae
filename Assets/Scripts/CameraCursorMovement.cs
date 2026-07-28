using UnityEngine;
using UnityEngine.InputSystem;

public class CameraCursorMovement : MonoBehaviour
{
    [Header("Cursor Panning")]
    [SerializeField] private float horizontalRange = 3f;
    [SerializeField] private float verticalRange = 2f;
    [SerializeField] private float smoothTime = 0.25f;
    [SerializeField] private float deadZone = 0.08f;
    [SerializeField] private bool invertVerticalMovement;

    [Header("Middle Mouse Drag")]
    [SerializeField] private bool enableMiddleMouseDrag = true;
    [SerializeField] private float dragSensitivity = 0.01f;

    [Header("Hex Hover Zoom")]
    [SerializeField] private HexMouseRaycaster hexMouseRaycaster;
    [SerializeField] private bool enableHexHoverZoom = true;
    [SerializeField, Min(0.001f)] private float hexZoomSensitivity = 0.02f;
    [SerializeField, Min(0.1f)] private float minimumHexZoomDistance = 4f;
    [SerializeField, Min(0.1f)] private float maximumHexZoomDistance = 30f;

    private Vector3 startingPosition;
    private Vector3 movementVelocity;

    private bool movementEnabled = true;
    private bool isDragging;
    private Vector2 previousDragPosition;

    private void Start()
    {
        startingPosition = transform.position;
        isDragging = false;
        if (hexMouseRaycaster == null)
            hexMouseRaycaster = FindFirstObjectByType<HexMouseRaycaster>();
    }

    private void LateUpdate()
    {
        if (!movementEnabled || Mouse.current == null)
        {
            isDragging = false;
            return;
        }

        Vector2 mousePosition = Mouse.current.position.ReadValue();

        if (!isDragging && ApplyHexHoverZoom())
            return;

        if (enableMiddleMouseDrag &&
            Mouse.current.middleButton.wasPressedThisFrame)
        {
            isDragging = true;
            previousDragPosition = mousePosition;
            movementVelocity = Vector3.zero;
            return;
        }

        if (isDragging)
        {
            if (Mouse.current.middleButton.isPressed)
            {
                Vector2 dragDelta = mousePosition - previousDragPosition;
                previousDragPosition = mousePosition;

                Vector3 rightDirection = transform.right;
                rightDirection.y = 0f;
                rightDirection.Normalize();

                Vector3 forwardDirection = transform.forward;
                forwardDirection.y = 0f;
                forwardDirection.Normalize();

                Vector3 dragOffset =
                    (-rightDirection * dragDelta.x -
                     forwardDirection * dragDelta.y) * dragSensitivity;

                startingPosition += dragOffset;
                transform.position += dragOffset;

                return;
            }

            isDragging = false;
            movementVelocity = Vector3.zero;
        }

        ApplyCursorPanning(mousePosition);
    }

    private void ApplyCursorPanning(Vector2 mousePosition)
    {
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;

        if (screenWidth <= 0f || screenHeight <= 0f)
            return;

        float horizontalInput =
            (mousePosition.x / screenWidth - 0.5f) * 2f;

        float verticalInput =
            (mousePosition.y / screenHeight - 0.5f) * 2f;

        horizontalInput = ApplyDeadZone(horizontalInput);
        verticalInput = ApplyDeadZone(verticalInput);

        if (invertVerticalMovement)
            verticalInput *= -1f;

        Vector3 rightDirection = transform.right;
        rightDirection.y = 0f;
        rightDirection.Normalize();

        Vector3 forwardDirection = transform.forward;
        forwardDirection.y = 0f;
        forwardDirection.Normalize();

        Vector3 targetPosition =
            startingPosition +
            rightDirection * horizontalInput * horizontalRange +
            forwardDirection * verticalInput * verticalRange;

        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref movementVelocity,
            smoothTime
        );
    }

    private float ApplyDeadZone(float value)
    {
        if (Mathf.Abs(value) < deadZone)
            return 0f;

        float direction = Mathf.Sign(value);
        float adjustedValue = Mathf.InverseLerp(
            deadZone,
            1f,
            Mathf.Abs(value)
        );

        return adjustedValue * direction;
    }

    public void SetMovementEnabled(bool enabled)
    {
        movementEnabled = enabled;
        isDragging = false;

        if (!enabled)
            movementVelocity = Vector3.zero;
    }

    public void ResetCameraPosition()
    {
        startingPosition = transform.position;
        movementVelocity = Vector3.zero;
        isDragging = false;
    }

    public bool ZoomTowardsHex(HexTile hex, float scrollAmount)
    {
        if (!movementEnabled || hex == null || Mathf.Abs(scrollAmount) < 0.01f)
            return false;

        Vector3 target = hex.transform.position;
        Vector3 fromTarget = transform.position - target;
        float currentDistance = fromTarget.magnitude;
        if (currentDistance <= 0.001f)
            return false;

        float zoomStep = Mathf.Abs(scrollAmount) * hexZoomSensitivity;
        float desiredDistance = Mathf.Clamp(
            currentDistance - Mathf.Sign(scrollAmount) * zoomStep,
            minimumHexZoomDistance,
            maximumHexZoomDistance
        );

        Vector3 desiredPosition = target + fromTarget.normalized * desiredDistance;
        Vector3 movement = desiredPosition - transform.position;
        transform.position = desiredPosition;
        startingPosition += movement;
        movementVelocity = Vector3.zero;
        return true;
    }

    private bool ApplyHexHoverZoom()
    {
        if (!enableHexHoverZoom ||
            hexMouseRaycaster == null ||
            !hexMouseRaycaster.CanZoomCurrentHex)
        {
            return false;
        }

        float scrollAmount = Mouse.current.scroll.ReadValue().y;
        return ZoomTowardsHex(hexMouseRaycaster.CurrentHoveredHexTile, scrollAmount);
    }
}
