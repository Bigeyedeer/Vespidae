using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class C_MainWorldCameraFocus : MonoBehaviour
{
    private enum FocusView
    {
        Map,
        Hex,
        Wasp,
        Hive
    }

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

    [Header("Close-Up Zoom")]
    [SerializeField] private bool enableCloseUpZoom = true;
    [SerializeField, Min(0.001f)] private float closeUpZoomSensitivity = 0.02f;
    [SerializeField, Min(0.1f)] private float minimumCloseUpZoomDistance = 1.5f;
    [SerializeField, Min(0.1f)] private float maximumCloseUpZoomDistance = 20f;

    private Vector3 mapStartPosition;
    private Quaternion mapStartRotation;
    private float mapStartFieldOfView;

    private bool closeUpActive;
    private bool isTransitioning;
    private FocusView currentView = FocusView.Map;
    private HexTile focusedHex;
    private Vector3 hexViewPosition;
    private Quaternion hexViewRotation;
    private float hexViewFieldOfView;
    private bool hasHexView;
    private Vector3 currentCloseUpLookPosition;

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
        currentView = FocusView.Map;
        focusedHex = null;
        hasHexView = false;
    }

    private void Update()
    {
        if (!closeUpActive || isTransitioning)
            return;

        ApplyCloseUpScrollZoom();

        if (Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ReturnToPreviousView();
        }
    }

    public void FocusOnHex(HexTile hex)
    {
        if (hex == null)
            return;

        focusedHex = hex;
        currentView = FocusView.Hex;
        currentCloseUpLookPosition = hex.transform.position;

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
            currentView = FocusView.Wasp;
            currentCloseUpLookPosition = wasp.LookPosition;
            StartCoroutine(BlendCloseUpToWasp(wasp));
            return;
        }

        currentView = FocusView.Wasp;
        currentCloseUpLookPosition = wasp.LookPosition;
        BeginFocus(wasp.CameraPoint.position, wasp.LookPosition, wasp);
    }

    public void ReturnToMap()
    {
        ReturnToPreviousView();
    }

    public void ReturnToPreviousView()
    {
        if (isTransitioning)
            return;

        if (currentView == FocusView.Hive)
            C_MainWorldOverlayNavigation.Instance?.HideHiveTraining();

        if ((currentView == FocusView.Wasp || currentView == FocusView.Hive) && hasHexView)
        {
            StartCoroutine(BlendBackToHex());
            return;
        }

        if (!closeUpActive)
            return;

        StartCoroutine(BlendBackToMap());
    }

    public void FocusOnHive(Transform focusPoint, Transform lookPoint, HexTile hex)
    {
        if (focusPoint == null)
            return;

        if (hex != null)
            focusedHex = hex;

        currentView = FocusView.Hive;
        currentCloseUpLookPosition = lookPoint != null ? lookPoint.position : focusPoint.position;

        if (closeUpActive)
        {
            StartCoroutine(BlendCloseUpToPoint(focusPoint.position, lookPoint != null ? lookPoint.position : focusPoint.position));
            return;
        }

        BeginFocus(
            focusPoint.position,
            lookPoint != null ? lookPoint.position : focusPoint.position,
            null
        );
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

        currentCloseUpLookPosition = lookPosition;
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

        if (currentView == FocusView.Hex)
        {
            hexViewPosition = closeUpCamera.transform.position;
            hexViewRotation = closeUpCamera.transform.rotation;
            hexViewFieldOfView = closeUpCamera.fieldOfView;
            hasHexView = true;
        }

        if (mapCameraMovement != null)
            mapCameraMovement.SetMovementEnabled(false);

        StartCoroutine(BlendToCloseUp(wasp));
    }

    public bool ZoomCloseUp(float scrollAmount)
    {
        if (!closeUpActive ||
            isTransitioning ||
            closeUpCamera == null ||
            Mathf.Abs(scrollAmount) < 0.01f)
        {
            return false;
        }

        Vector3 fromTarget = closeUpCamera.transform.position - currentCloseUpLookPosition;
        float currentDistance = fromTarget.magnitude;
        if (currentDistance <= 0.001f)
            return false;

        float zoomStep = Mathf.Abs(scrollAmount) * closeUpZoomSensitivity;
        float desiredDistance = Mathf.Clamp(
            currentDistance - Mathf.Sign(scrollAmount) * zoomStep,
            minimumCloseUpZoomDistance,
            maximumCloseUpZoomDistance
        );

        closeUpCamera.transform.position =
            currentCloseUpLookPosition + fromTarget.normalized * desiredDistance;

        if (currentView == FocusView.Hex)
        {
            hexViewPosition = closeUpCamera.transform.position;
            hexViewRotation = closeUpCamera.transform.rotation;
            hexViewFieldOfView = closeUpCamera.fieldOfView;
            hasHexView = true;
        }

        return true;
    }

    private void ApplyCloseUpScrollZoom()
    {
        if (!enableCloseUpZoom || Mouse.current == null)
            return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        ZoomCloseUp(Mouse.current.scroll.ReadValue().y);
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
        currentCloseUpLookPosition = wasp.LookPosition;

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
        currentCloseUpLookPosition = wasp.LookPosition;

        isTransitioning = false;

        if (waspInfoPanel != null)
            waspInfoPanel.Open(wasp);
    }

    private IEnumerator BlendCloseUpToPoint(Vector3 targetPosition, Vector3 lookPosition)
    {
        if (closeUpCamera == null || isTransitioning)
            yield break;

        isTransitioning = true;
        currentCloseUpLookPosition = lookPosition;

        Vector3 startPosition = closeUpCamera.transform.position;
        Quaternion startRotation = closeUpCamera.transform.rotation;
        Vector3 lookDirection = lookPosition - targetPosition;
        Quaternion targetRotation = startRotation;

        if (lookDirection.sqrMagnitude > 0.0001f)
        {
            targetRotation = Quaternion.LookRotation(
                lookDirection.normalized,
                Vector3.up
            );
        }

        if (hexOptionsPanel != null)
            hexOptionsPanel.Close();

        float elapsedTime = 0f;

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
        currentCloseUpLookPosition = lookPosition;
        isTransitioning = false;
    }

    private IEnumerator BlendBackToHex()
    {
        if (mainCamera == null || closeUpCamera == null || focusedHex == null)
            yield break;

        isTransitioning = true;
        currentCloseUpLookPosition = focusedHex.transform.position;

        if (waspInfoPanel != null)
            waspInfoPanel.Close();

        Camera activeCamera = closeUpActive ? closeUpCamera : mainCamera;
        Vector3 startPosition = activeCamera.transform.position;
        Quaternion startRotation = activeCamera.transform.rotation;
        float startFieldOfView = activeCamera.fieldOfView;

        closeUpCamera.transform.position = startPosition;
        closeUpCamera.transform.rotation = startRotation;
        closeUpCamera.fieldOfView = startFieldOfView;
        closeUpCamera.gameObject.SetActive(true);
        mainCamera.gameObject.SetActive(false);
        closeUpActive = true;

        float elapsedTime = 0f;

        while (elapsedTime < blendDuration)
        {
            elapsedTime += Time.deltaTime;

            float progress = Mathf.Clamp01(
                elapsedTime / Mathf.Max(0.01f, blendDuration)
            );

            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);

            closeUpCamera.transform.position = Vector3.Lerp(
                startPosition,
                hexViewPosition,
                smoothProgress
            );

            closeUpCamera.transform.rotation = Quaternion.Slerp(
                startRotation,
                hexViewRotation,
                smoothProgress
            );

            closeUpCamera.fieldOfView = Mathf.Lerp(
                startFieldOfView,
                hexViewFieldOfView,
                smoothProgress
            );

            yield return null;
        }

        closeUpCamera.transform.position = hexViewPosition;
        closeUpCamera.transform.rotation = hexViewRotation;
        closeUpCamera.fieldOfView = hexViewFieldOfView;

        if (focusedHex != null && hexOptionsPanel != null)
            hexOptionsPanel.Open(focusedHex);

        currentView = FocusView.Hex;
        currentCloseUpLookPosition = focusedHex.transform.position;
        isTransitioning = false;
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
        currentView = FocusView.Map;
        focusedHex = null;
        hasHexView = false;
        currentCloseUpLookPosition = Vector3.zero;
    }
}
