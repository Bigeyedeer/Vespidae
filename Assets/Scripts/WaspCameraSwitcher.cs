using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class WaspCameraSwitcher : MonoBehaviour
{
    [Header("Cameras")]
    [SerializeField] private Camera mapCamera;
    [SerializeField] private Camera closeUpCamera;

    [Header("UI")]
    [SerializeField] private WaspInfoPanel infoPanel;

    [Header("Transition")]
    [SerializeField] private float moveDuration = 1.2f;

    private Vector3 mapStartPosition;
    private Quaternion mapStartRotation;
    private float mapStartFieldOfView;

    private bool closeUpActive;
    private bool isTransitioning;

    private void Start()
    {
        if (mapCamera == null || closeUpCamera == null)
        {
            Debug.LogError("Both cameras must be assigned.");
            enabled = false;
            return;
        }

        mapCamera.gameObject.SetActive(true);
        closeUpCamera.gameObject.SetActive(false);
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

    public void ShowWaspCamera(WaspInfo wasp)
    {
        if (isTransitioning || closeUpActive)
            return;

        StartCoroutine(MoveToCloseUp(wasp));
    }

    public void ReturnToMap()
    {
        if (isTransitioning || !closeUpActive)
            return;

        StartCoroutine(MoveBackToMap());
    }

    private IEnumerator MoveToCloseUp(WaspInfo wasp)
    {
        isTransitioning = true;
        
        mapStartPosition = mapCamera.transform.position;
        mapStartRotation = mapCamera.transform.rotation;
        mapStartFieldOfView = mapCamera.fieldOfView;

        Transform movingCamera = mapCamera.transform;
        Transform targetCamera = closeUpCamera.transform;

        Vector3 startPosition = movingCamera.position;
        Quaternion startRotation = movingCamera.rotation;
        float startFieldOfView = mapCamera.fieldOfView;

        Vector3 targetPosition = targetCamera.position;
        Quaternion targetRotation = targetCamera.rotation;
        float targetFieldOfView = closeUpCamera.fieldOfView;

        float elapsedTime = 0f;

        while (elapsedTime < moveDuration)
        {
            elapsedTime += Time.deltaTime;

            float progress = Mathf.Clamp01(
                elapsedTime / moveDuration
            );

            // Smooth acceleration and deceleration.
            float smoothProgress = Mathf.SmoothStep(
                0f,
                1f,
                progress
            );

            movingCamera.position = Vector3.Lerp(
                startPosition,
                targetPosition,
                smoothProgress
            );

            movingCamera.rotation = Quaternion.Slerp(
                startRotation,
                targetRotation,
                smoothProgress
            );

            mapCamera.fieldOfView = Mathf.Lerp(
                startFieldOfView,
                targetFieldOfView,
                smoothProgress
            );

            yield return null;
        }

        movingCamera.position = targetPosition;
        movingCamera.rotation = targetRotation;
        mapCamera.fieldOfView = targetFieldOfView;
        
        closeUpCamera.gameObject.SetActive(true);
        mapCamera.gameObject.SetActive(false);

        closeUpActive = true;

        if (infoPanel != null && wasp != null)
            infoPanel.Open(wasp);

        isTransitioning = false;
    }

    private IEnumerator MoveBackToMap()
    {
        isTransitioning = true;
        
        mapCamera.transform.position =
            closeUpCamera.transform.position;

        mapCamera.transform.rotation =
            closeUpCamera.transform.rotation;

        mapCamera.fieldOfView =
            closeUpCamera.fieldOfView;

        mapCamera.gameObject.SetActive(true);
        closeUpCamera.gameObject.SetActive(false);

        if (infoPanel != null)
            infoPanel.Close();

        Vector3 startPosition =
            mapCamera.transform.position;

        Quaternion startRotation =
            mapCamera.transform.rotation;

        float startFieldOfView =
            mapCamera.fieldOfView;

        float elapsedTime = 0f;

        while (elapsedTime < moveDuration)
        {
            elapsedTime += Time.deltaTime;

            float progress = Mathf.Clamp01(
                elapsedTime / moveDuration
            );

            float smoothProgress = Mathf.SmoothStep(
                0f,
                1f,
                progress
            );

            mapCamera.transform.position = Vector3.Lerp(
                startPosition,
                mapStartPosition,
                smoothProgress
            );

            mapCamera.transform.rotation = Quaternion.Slerp(
                startRotation,
                mapStartRotation,
                smoothProgress
            );

            mapCamera.fieldOfView = Mathf.Lerp(
                startFieldOfView,
                mapStartFieldOfView,
                smoothProgress
            );

            yield return null;
        }

        mapCamera.transform.position =
            mapStartPosition;

        mapCamera.transform.rotation =
            mapStartRotation;

        mapCamera.fieldOfView =
            mapStartFieldOfView;

        closeUpActive = false;
        isTransitioning = false;
    }
}
