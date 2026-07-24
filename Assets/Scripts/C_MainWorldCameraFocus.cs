using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class C_MainWorldCameraFocus : MonoBehaviour
{
    [Header("Cameras")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Camera closeUpCamera;

    [Header("Panels")]
    [SerializeField] private HexOptionsPanel hexOptionsPanel;
    [SerializeField] private WaspInfoPanel waspInfoPanel;

    [Header("Transition")]
    [SerializeField] private float blendDuration = 1.2f;

    [Header("Map Camera Controls")]
    [SerializeField] private CameraCursorMovement mapCameraMovement;

    private Vector3 mapStartPosition;
    private Quaternion mapStartRotation;
    private float mapStartFieldOfView;

    private bool closeUpActive;
    private bool isTransitioning;

    public bool IsCloseUpActive => closeUpActive;
    public bool IsTransitioning => isTransitioning;

    private void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera != null && mapCameraMovement == null)
            mapCameraMovement = mainCamera.GetComponent<CameraCursorMovement>();

        if (mainCamera == null || closeUpCamera == null)
        {
            Debug.LogError(
                "C_MainWorldCameraFocus requires both Main Camera and CloseUp Camera."
            );

            enabled = false;
            return;
        }

        mainCamera.gameObject.SetActive(true);
        closeUpCamera.gameObject.SetActive(false);
        closeUpActive = false;
        isTransitioning = false;
    }

    private void Update()
    {
        if (!closeUpActive || isTransitioning)
            return;

        if (Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ReturnToMap();
        }
    }

    public void FocusOnHex(HexTile hex)
    {
        if (hex == null)
            return;

        BeginFocus(
            hex.FocusPosition,
            hex.transform.position,
            null
        );
    }

    public void FocusOnWasp(WaspInfo wasp)
    {
        if (wasp == null || wasp.CameraPoint == null)
        {
            Debug.LogWarning(
                "The selected wasp needs a WaspCameraPoint empty assigned."
            );

            return;
        }

        if (closeUpActive)
        {
            StartCoroutine(BlendCloseUpToWasp(wasp));
            return;
        }

        BeginFocus(wasp.CameraPoint.position, wasp.LookPosition, wasp);
    }

    public void ReturnToMap()
    {
        if (isTransitioning || !closeUpActive)
            return;

        StartCoroutine(BlendBackToMap());
    }

    private void BeginFocus(
        Vector3 closeUpPosition,
        Vector3 lookPosition,
        WaspInfo wasp
    )
    {
        if (isTransitioning || closeUpActive)
            return;

        if (mainCamera == null || closeUpCamera == null)
            return;

        mapStartPosition = mainCamera.transform.position;
        mapStartRotation = mainCamera.transform.rotation;
        mapStartFieldOfView = mainCamera.fieldOfView;

        closeUpCamera.transform.position = closeUpPosition;

        Vector3 lookDirection = lookPosition - closeUpPosition;

        if (lookDirection.sqrMagnitude > 0.0001f)
        {
            closeUpCamera.transform.rotation = Quaternion.LookRotation(
                lookDirection.normalized,
                Vector3.up
            );
        }

        if (mapCameraMovement != null)
            mapCameraMovement.SetMovementEnabled(false);

        StartCoroutine(BlendToCloseUp(wasp));
    }

    private IEnumerator BlendToCloseUp(WaspInfo wasp)
    {
        isTransitioning = true;

        Vector3 startPosition = mainCamera.transform.position;
        Quaternion startRotation = mainCamera.transform.rotation;
        float startFieldOfView = mainCamera.fieldOfView;

        Vector3 targetPosition = closeUpCamera.transform.position;
        Quaternion targetRotation = closeUpCamera.transform.rotation;
        float targetFieldOfView = closeUpCamera.fieldOfView;

        float elapsedTime = 0f;

        while (elapsedTime < blendDuration)
        {
            elapsedTime += Time.deltaTime;

            float progress = Mathf.Clamp01(
                elapsedTime / Mathf.Max(0.01f, blendDuration)
            );

            float smoothProgress = Mathf.SmoothStep(
                0f,
                1f,
                progress
            );

            mainCamera.transform.position = Vector3.Lerp(
                startPosition,
                targetPosition,
                smoothProgress
            );

            mainCamera.transform.rotation = Quaternion.Slerp(
                startRotation,
                targetRotation,
                smoothProgress
            );

            mainCamera.fieldOfView = Mathf.Lerp(
                startFieldOfView,
                targetFieldOfView,
                smoothProgress
            );

            yield return null;
        }

        mainCamera.transform.position = targetPosition;
        mainCamera.transform.rotation = targetRotation;
        mainCamera.fieldOfView = targetFieldOfView;

        closeUpCamera.gameObject.SetActive(true);
        mainCamera.gameObject.SetActive(false);

        closeUpActive = true;
        isTransitioning = false;

        if (waspInfoPanel != null && wasp != null)
            waspInfoPanel.Open(wasp);
    }

    private IEnumerator BlendCloseUpToWasp(WaspInfo wasp)
    {
        if (wasp == null || closeUpCamera == null || isTransitioning)
            yield break;

        isTransitioning = true;

        Vector3 startPosition = closeUpCamera.transform.position;
        Quaternion startRotation = closeUpCamera.transform.rotation;
        Vector3 targetPosition = wasp.CameraPoint.position;
        Vector3 lookDirection = wasp.LookPosition - targetPosition;
        Quaternion targetRotation = startRotation;

        if (lookDirection.sqrMagnitude > 0.0001f)
        {
            targetRotation = Quaternion.LookRotation(
                lookDirection.normalized,
                Vector3.up
            );
        }

        float elapsedTime = 0f;

        if (hexOptionsPanel != null)
            hexOptionsPanel.Close();

        while (elapsedTime < blendDuration)
        {
            elapsedTime += Time.deltaTime;

            float progress = Mathf.Clamp01(
                elapsedTime / Mathf.Max(0.01f, blendDuration)
            );

            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);

            closeUpCamera.transform.position = Vector3.Lerp(
                startPosition,
                targetPosition,
                smoothProgress
            );

            closeUpCamera.transform.rotation = Quaternion.Slerp(
                startRotation,
                targetRotation,
                smoothProgress
            );

            yield return null;
        }

        closeUpCamera.transform.position = targetPosition;
        closeUpCamera.transform.rotation = targetRotation;

        isTransitioning = false;

        if (waspInfoPanel != null)
            waspInfoPanel.Open(wasp);
    }

    private IEnumerator BlendBackToMap()
    {
        isTransitioning = true;

        mainCamera.transform.position = closeUpCamera.transform.position;
        mainCamera.transform.rotation = closeUpCamera.transform.rotation;
        mainCamera.fieldOfView = closeUpCamera.fieldOfView;

        mainCamera.gameObject.SetActive(true);
        closeUpCamera.gameObject.SetActive(false);

        if (hexOptionsPanel != null)
            hexOptionsPanel.Close();

        if (waspInfoPanel != null)
            waspInfoPanel.Close();

        Vector3 startPosition = mainCamera.transform.position;
        Quaternion startRotation = mainCamera.transform.rotation;
        float startFieldOfView = mainCamera.fieldOfView;

        float elapsedTime = 0f;

        while (elapsedTime < blendDuration)
        {
            elapsedTime += Time.deltaTime;

            float progress = Mathf.Clamp01(
                elapsedTime / Mathf.Max(0.01f, blendDuration)
            );

            float smoothProgress = Mathf.SmoothStep(
                0f,
                1f,
                progress
            );

            mainCamera.transform.position = Vector3.Lerp(
                startPosition,
                mapStartPosition,
                smoothProgress
            );

            mainCamera.transform.rotation = Quaternion.Slerp(
                startRotation,
                mapStartRotation,
                smoothProgress
            );

            mainCamera.fieldOfView = Mathf.Lerp(
                startFieldOfView,
                mapStartFieldOfView,
                smoothProgress
            );

            yield return null;
        }

        mainCamera.transform.position = mapStartPosition;
        mainCamera.transform.rotation = mapStartRotation;
        mainCamera.fieldOfView = mapStartFieldOfView;

        if (mapCameraMovement != null)
        {
            mapCameraMovement.ResetCameraPosition();
            mapCameraMovement.SetMovementEnabled(true);
        }

        closeUpActive = false;
        isTransitioning = false;
    }
}
