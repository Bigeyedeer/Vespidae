using UnityEngine;
using UnityEngine.InputSystem;

public class CameraCursorMovement : MonoBehaviour
{
    [Header("Movement Range")]
    [SerializeField] private float horizontalRange = 3f;
    [SerializeField] private float verticalRange = 2f;

    [Header("Movement Feel")]
    [SerializeField] private float smoothTime = 0.25f;
    [SerializeField] private float deadZone = 0.08f;

    [Header("Optional")]
    [SerializeField] private bool invertVerticalMovement;

    private Vector3 startingPosition;
    private Vector3 movementVelocity;

    private bool movementEnabled = true;

    private void Start()
    {
        startingPosition = transform.position;
    }

    private void LateUpdate()
    {
        if (!movementEnabled || Mouse.current == null)
            return;

        Vector2 mousePosition = Mouse.current.position.ReadValue();

        float screenWidth = Screen.width;
        float screenHeight = Screen.height;

        if (screenWidth <= 0f || screenHeight <= 0f)
            return;

        // Convert the cursor position to a range from -1 to 1.
        float horizontalInput =
            (mousePosition.x / screenWidth - 0.5f) * 2f;

        float verticalInput =
            (mousePosition.y / screenHeight - 0.5f) * 2f;

        horizontalInput = ApplyDeadZone(horizontalInput);
        verticalInput = ApplyDeadZone(verticalInput);

        if (invertVerticalMovement)
            verticalInput *= -1f;

        // Camera right controls left/right movement.
        Vector3 rightDirection = transform.right;
        rightDirection.y = 0f;
        rightDirection.Normalize();

        // Flatten the camera's forward direction onto the map.
        Vector3 forwardDirection = transform.forward;
        forwardDirection.y = 0f;
        forwardDirection.Normalize();

        Vector3 horizontalOffset =
            rightDirection * horizontalInput * horizontalRange;

        Vector3 verticalOffset =
            forwardDirection * verticalInput * verticalRange;

        Vector3 targetPosition =
            startingPosition +
            horizontalOffset +
            verticalOffset;

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

        if (!enabled)
            movementVelocity = Vector3.zero;
    }

    public void ResetCameraPosition()
    {
        startingPosition = transform.position;
        movementVelocity = Vector3.zero;
    }
}
