using UnityEngine;
using UnityEngine.InputSystem;

public class HexCameraFocus : MonoBehaviour
{
    [Header("Focus Movement")]
    [SerializeField] private float focusHeight = 5f;
    [SerializeField] private float focusDistance = 4f;
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float rotationSpeed = 6f;

    private Vector3 originalPosition;
    private Quaternion originalRotation;

    private Vector3 targetPosition;
    private Quaternion targetRotation;

    private bool isFocused;

    private void Awake()
    {
        originalPosition = transform.position;
        originalRotation = transform.rotation;

        targetPosition = originalPosition;
        targetRotation = originalRotation;
    }

    private void Update()
    {
        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            moveSpeed * Time.deltaTime
        );

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );

        if (Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame &&
            isFocused)
        {
            ReturnToMap();
        }
    }

    public void FocusOnHex(HexTile hex)
    {
        Vector3 focusPosition = hex.FocusPosition;

        Vector3 cameraDirection = -transform.forward;
        cameraDirection.y = 0f;

        if (cameraDirection.sqrMagnitude < 0.01f)
            cameraDirection = Vector3.back;

        cameraDirection.Normalize();

        targetPosition =
            focusPosition +
            cameraDirection * focusDistance +
            Vector3.up * focusHeight;

        Vector3 lookDirection = focusPosition - targetPosition;

        targetRotation = Quaternion.LookRotation(
            lookDirection.normalized,
            Vector3.up
        );

        isFocused = true;
    }

    public void ReturnToMap()
    {
        targetPosition = originalPosition;
        targetRotation = originalRotation;

        isFocused = false;
    }
}