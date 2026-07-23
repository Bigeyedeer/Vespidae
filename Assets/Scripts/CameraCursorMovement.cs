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

    private Vector3 startingPosition;
    private Vector3 movementVelocity;

    private bool movementEnabled = true;
    private bool isDragging;
    private Vector2 previousDragPosition;

    private void Start()
    {
        startingPosition = transform.position;
        isDragging = false;
    }

    private void LateUpdate()
    {
        if (!movementEnabled || Mouse.current == null)
        {
            isDragging = false;
            return;
        }

        Vector2 mousePosition = Mouse.current.position.ReadValue();

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

                // Dragging left (negative X delta) moves the camera/world
                // anchor to the right.
                Vector3 dragOffset =
                    (-rightDirection * dragDelta.x -
                     forwardDirection * dragDelta.y) * dragSensitivity;

                startingPosition += dragOffset;
                transform.position += dragOffset;

                // Middle-mouse drag has exclusive control over the camera.
                return;
            }

            // Release ends the drag. Panning is allowed to resume below on
            // this frame because middle mouse is no longer held.
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

    // Kept for compatibility with older focus scripts that may still exist
    // elsewhere in the project. The rebuilt camera has no separate anchor.
    public void ResetCameraPosition()
    {
        startingPosition = transform.position;
        movementVelocity = Vector3.zero;
        isDragging = false;
    }
}
