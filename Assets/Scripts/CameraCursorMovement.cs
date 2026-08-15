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

    [Header("Map Bounds")]
    [SerializeField, Tooltip("Stops the camera being panned off the map and out into the skybox.")]
    private bool clampToMapBounds = true;
    [SerializeField, Tooltip("Leave empty to measure the bounds from the hex tiles in the scene on Start.")]
    private bool useManualBounds;
    [SerializeField, Tooltip("World-space rectangle the camera's ground focus may sit inside (X, Z).")]
    private Rect manualBounds = new Rect(-20f, -25f, 50f, 50f);
    [SerializeField, Tooltip("How far past the outermost hex the player may still push, in world units. " +
                             "The terrain plane only runs about four units past the last hex, so pushing " +
                             "this much higher walks the camera off the edge of the world.")]
    private float boundsPadding = 3f;
    [SerializeField, Tooltip("Ground height the camera's focus point is measured against.")]
    private float groundHeight;

    [Header("Hex Hover Zoom")]
    [SerializeField] private HexMouseRaycaster hexMouseRaycaster;
    [SerializeField] private bool enableHexHoverZoom = true;
    [SerializeField, Min(0.001f)] private float hexZoomSensitivity = 0.02f;
    [SerializeField, Min(0.1f)] private float minimumHexZoomDistance = 4f;
    [SerializeField, Min(0.1f)] private float maximumHexZoomDistance = 30f;
    [SerializeField, Min(0.1f)] private float hexZoomAcceleration = 32f;
    [SerializeField, Min(0.1f)] private float maximumHexZoomSpeed = 85f;
    [SerializeField, Min(0.1f)] private float hexZoomDeceleration = 48f;

    private Vector3 startingPosition;
    private Vector3 movementVelocity;

    private bool movementEnabled = true;
    [SerializeField, Tooltip("Optional. Found automatically. Used to freeze the camera during box-selection.")]
    private WaspControlGroupManager controlGroupManager;

    private bool isDragging;
    private Vector2 previousDragPosition;
    private float hexZoomVelocity;
    private Rect bounds;
    private bool boundsResolved;

    private const float ReferenceZoomSensitivity = 0.02f;
    private const float ScrollUnitsPerNotch = 120f;

    public bool MovementEnabled => movementEnabled;
    public float ScrollWheelZoomSpeed
    {
        get => hexZoomSensitivity;
        set => hexZoomSensitivity = Mathf.Max(0.001f, value);
    }

    private void Start()
    {
        startingPosition = transform.position;
        isDragging = false;
        if (hexMouseRaycaster == null)
            hexMouseRaycaster = FindFirstObjectByType<HexMouseRaycaster>();
        if (controlGroupManager == null)
            controlGroupManager = FindFirstObjectByType<WaspControlGroupManager>();

        ResolveBounds();
    }

    /// <summary>
    /// Works out the rectangle the camera's ground focus is allowed to sit in. Measuring it from the
    /// hexes rather than hard-coding it means the wall stays correct if the map is ever re-laid out.
    /// </summary>
    private void ResolveBounds()
    {
        if (useManualBounds)
        {
            bounds = manualBounds;
            boundsResolved = true;
            return;
        }

        HexTile[] tiles = FindObjectsByType<HexTile>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (tiles.Length == 0)
        {
            boundsResolved = false;
            return;
        }

        float minimumX = float.MaxValue;
        float maximumX = float.MinValue;
        float minimumZ = float.MaxValue;
        float maximumZ = float.MinValue;

        foreach (HexTile tile in tiles)
        {
            if (tile == null)
                continue;

            Vector3 position = tile.transform.position;
            minimumX = Mathf.Min(minimumX, position.x);
            maximumX = Mathf.Max(maximumX, position.x);
            minimumZ = Mathf.Min(minimumZ, position.z);
            maximumZ = Mathf.Max(maximumZ, position.z);
        }

        bounds = Rect.MinMaxRect(
            minimumX - boundsPadding,
            minimumZ - boundsPadding,
            maximumX + boundsPadding,
            maximumZ + boundsPadding);
        boundsResolved = true;
    }

    /// <summary>
    /// Pushes a camera position back inside the map wall. What gets clamped is the point the camera is
    /// looking at on the ground, not the camera itself, so the limit feels the same at every zoom level
    /// and pitch instead of tightening as the camera comes down.
    /// </summary>
    private Vector3 ClampToBounds(Vector3 position)
    {
        if (!clampToMapBounds || !boundsResolved)
            return position;

        Vector3 focus = position;
        Vector3 forward = transform.forward;

        // Only project when the camera actually points at the ground; a level camera would send the
        // intersection off to infinity.
        if (forward.y < -0.05f)
        {
            float distance = (position.y - groundHeight) / -forward.y;
            focus = position + forward * distance;
        }

        float clampedX = Mathf.Clamp(focus.x, bounds.xMin, bounds.xMax);
        float clampedZ = Mathf.Clamp(focus.z, bounds.yMin, bounds.yMax);

        position.x += clampedX - focus.x;
        position.z += clampedZ - focus.z;
        return position;
    }

    /// <summary>Clamps the camera and the pan anchor together, so neither can drift outside the wall.</summary>
    private void ApplyBounds()
    {
        if (!clampToMapBounds || !boundsResolved)
            return;

        Vector3 clamped = ClampToBounds(transform.position);
        Vector3 correction = clamped - transform.position;
        if (correction.sqrMagnitude > 0f)
        {
            transform.position = clamped;
            movementVelocity = Vector3.zero;
        }

        startingPosition = ClampToBounds(startingPosition);
    }

    private bool IsBoxSelecting()
    {
        if (controlGroupManager == null)
            controlGroupManager = FindFirstObjectByType<WaspControlGroupManager>();

        return controlGroupManager != null && controlGroupManager.IsDragging;
    }

    private void LateUpdate()
    {
        if (!movementEnabled || Mouse.current == null)
        {
            isDragging = false;
            hexZoomVelocity = 0f;
            return;
        }

        // Hold the camera still while the player is box-selecting, otherwise dragging near the
        // screen edge pans the map out from under the selection.
        if (IsBoxSelecting())
        {
            isDragging = false;
            movementVelocity = Vector3.zero;
            hexZoomVelocity = 0f;
            return;
        }

        Vector2 mousePosition = Mouse.current.position.ReadValue();

        if (enableMiddleMouseDrag &&
            Mouse.current.middleButton.wasPressedThisFrame)
        {
            isDragging = true;
            previousDragPosition = mousePosition;
            movementVelocity = Vector3.zero;
            hexZoomVelocity = 0f;
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
                ApplyBounds();

                return;
            }

            isDragging = false;
            movementVelocity = Vector3.zero;
        }

        if (ApplyHexHoverZoom())
            return;

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

        // Clamping the target rather than the result lets the camera glide to a stop against the wall
        // instead of being snapped back to it every frame.
        targetPosition = ClampToBounds(targetPosition);

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
        hexZoomVelocity = 0f;

        if (!enabled)
            movementVelocity = Vector3.zero;
    }

    public void ResetCameraPosition()
    {
        startingPosition = transform.position;
        movementVelocity = Vector3.zero;
        isDragging = false;
        hexZoomVelocity = 0f;

        // Focus and close-up moves are free to leave the map; the wall is re-applied the moment the
        // player has the map camera back.
        if (!boundsResolved)
            ResolveBounds();

        ApplyBounds();
    }

    /// <summary>Re-measures the map wall. Call this if hexes are added or moved at run time.</summary>
    public void RefreshMapBounds()
    {
        ResolveBounds();
        ApplyBounds();
    }

    private void OnDrawGizmosSelected()
    {
        if (!clampToMapBounds || !boundsResolved)
            return;

        Gizmos.color = new Color(1f, 0.75f, 0.2f, 0.85f);
        Vector3 centre = new Vector3(bounds.center.x, groundHeight, bounds.center.y);
        Gizmos.DrawWireCube(centre, new Vector3(bounds.width, 0.1f, bounds.height));
    }

    public bool ZoomTowardsHex(HexTile hex, float scrollAmount)
    {
        if (!movementEnabled || hex == null)
            return false;

        float deltaTime = Mathf.Max(Time.unscaledDeltaTime, 0.001f);
        float sensitivityMultiplier = Mathf.Max(0.05f, hexZoomSensitivity / ReferenceZoomSensitivity);

        if (Mathf.Abs(scrollAmount) >= 0.01f)
        {
            float direction = Mathf.Sign(scrollAmount);
            float notchStrength = Mathf.Clamp(
                Mathf.Abs(scrollAmount) / ScrollUnitsPerNotch,
                0.25f,
                4f);

            if (!Mathf.Approximately(hexZoomVelocity, 0f) &&
                Mathf.Sign(hexZoomVelocity) != direction)
            {
                hexZoomVelocity = 0f;
            }

            hexZoomVelocity += direction * notchStrength * hexZoomAcceleration * sensitivityMultiplier;
            hexZoomVelocity = Mathf.Clamp(
                hexZoomVelocity,
                -maximumHexZoomSpeed * sensitivityMultiplier,
                maximumHexZoomSpeed * sensitivityMultiplier);
        }
        else
        {
            hexZoomVelocity = Mathf.MoveTowards(
                hexZoomVelocity,
                0f,
                hexZoomDeceleration * sensitivityMultiplier * deltaTime);
        }

        if (Mathf.Abs(hexZoomVelocity) < 0.01f)
            return false;

        Vector3 target = hex.transform.position;
        Vector3 fromTarget = transform.position - target;
        float currentDistance = fromTarget.magnitude;
        if (currentDistance <= 0.001f)
            return false;

        float desiredDistance = Mathf.Clamp(
            currentDistance - hexZoomVelocity * deltaTime,
            minimumHexZoomDistance,
            maximumHexZoomDistance
        );

        if (Mathf.Approximately(currentDistance, desiredDistance))
        {
            hexZoomVelocity = 0f;
            return false;
        }

        Vector3 desiredPosition = ClampToBounds(target + fromTarget.normalized * desiredDistance);
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
            hexZoomVelocity = 0f;
            return false;
        }

        float scrollAmount = Mouse.current.scroll.ReadValue().y;
        return ZoomTowardsHex(hexMouseRaycaster.CurrentHoveredHexTile, scrollAmount);
    }
}
