using UnityEngine;
using UnityEngine.InputSystem;

public class WaspCameraFocus : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float positionSmoothTime = 0.35f;
    [SerializeField] private float rotationSpeed = 7f;

    [Header("Other Camera Systems")]
    [SerializeField] private CameraCursorMovement cursorMovement;
    [SerializeField] private HexCameraFocus hexCameraFocus;

    [Header("UI")]
    [SerializeField] private WaspInfoPanel infoPanel;

    private Vector3 mapPosition;
    private Quaternion mapRotation;

    private Vector3 targetPosition;
    private Quaternion targetRotation;

    private Vector3 movementVelocity;

    private bool isFocused;

    private void Start()
    {
        mapPosition = transform.position;
        mapRotation = transform.rotation;

        targetPosition = mapPosition;
        targetRotation = mapRotation;
    }

    private void Update()
    {
        if (!isFocused)
            return;

        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref movementVelocity,
            positionSmoothTime
        );

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );

        if (Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ReturnToMap();
        }
    }

    public void FocusOnWasp(WaspInfo wasp)
    {
        if (wasp == null || wasp.CameraPoint == null)
        {
            Debug.LogWarning("The wasp does not have a Camera Point assigned.");
            return;
        }

        // Save the camera's current map position before moving.
        mapPosition = transform.position;
        mapRotation = transform.rotation;

        targetPosition = wasp.CameraPoint.position;

        Vector3 lookDirection =
            wasp.LookPosition - targetPosition;

        targetRotation = Quaternion.LookRotation(
            lookDirection.normalized,
            Vector3.up
        );

        movementVelocity = Vector3.zero;
        isFocused = true;

        if (cursorMovement != null)
            cursorMovement.SetMovementEnabled(false);

        if (hexCameraFocus != null)
            hexCameraFocus.enabled = false;
    }

    public void ReturnToMap()
    {
        transform.position = mapPosition;
        transform.rotation = mapRotation;

        targetPosition = mapPosition;
        targetRotation = mapRotation;

        movementVelocity = Vector3.zero;
        isFocused = false;

        if (cursorMovement != null)
        {
            cursorMovement.ResetCameraPosition();
            cursorMovement.SetMovementEnabled(true);
        }

        if (hexCameraFocus != null)
            hexCameraFocus.enabled = true;

        if (infoPanel != null)
            infoPanel.Close();
    }
}