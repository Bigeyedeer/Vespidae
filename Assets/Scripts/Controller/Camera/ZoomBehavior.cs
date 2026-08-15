using System;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class ZoomBehavior : MonoBehaviour
{
    public CinemachineCamera mainCamera;
    public float defaultFOV = 40;
    public float introFOV;
    public float introDuration = 1f;
    public float zoomFOV;

    private Coroutine zoomCoroutine;
    void Start()
    {
        StartCoroutine(IntroSequence());
    }

    public void StartScanZoom(float duration)
    {
        // Stop any existing zoom
        if (zoomCoroutine != null)
            StopCoroutine(zoomCoroutine);

        zoomCoroutine = StartCoroutine(ScanSequence(duration));
    }

    public IEnumerator IntroSequence()
    {
        //mainCamera.Lens.FieldOfView = Mathf.Lerp(introFOV,defaultFOV, introDuration);

        float timer = 0f;

        // Start at intro FOV
        mainCamera.Lens.FieldOfView = introFOV;

        while (timer < introDuration)
        {
            timer += Time.deltaTime;

            float progress = timer / introDuration;

            mainCamera.Lens.FieldOfView = Mathf.Lerp(
                introFOV,
                defaultFOV,
                progress
            );

            yield return null;
        }

        mainCamera.Lens.FieldOfView = defaultFOV;
    }

    public IEnumerator ScanSequence(float duration)
    {
        /*mainCamera.Lens.FieldOfView = Mathf.Lerp(defaultFOV,zoomFOV, duration);
        yield return new WaitForSeconds(duration);
        mainCamera.Lens.FieldOfView = defaultFOV;*/

        float timer = 0f;

        float startingFOV = mainCamera.Lens.FieldOfView;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float progress = timer / duration;

            mainCamera.Lens.FieldOfView = Mathf.Lerp(
                startingFOV,
                zoomFOV,
                progress
            );

            yield return null;
        }

        mainCamera.Lens.FieldOfView = zoomFOV;

        // Hold zoomed FOV for the duration
        yield return new WaitForSeconds(1f);

        timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float progress = timer / duration;

            mainCamera.Lens.FieldOfView = Mathf.Lerp(
                zoomFOV,
                defaultFOV,
                progress
            );

            yield return null;
        }

        mainCamera.Lens.FieldOfView = defaultFOV;
    }

    public void CancelZoom(float targetFOV, float duration)
    {
        if (zoomCoroutine != null)
        {
            StopCoroutine(zoomCoroutine);
            zoomCoroutine = null;
        }

        // Immediately restore normal FOV
        SetFoV(targetFOV, duration);
    }


    public void SetFoV(float endFOV, float duration)
    {
        if (zoomCoroutine != null)
        {
            StopCoroutine(zoomCoroutine);
        }

        zoomCoroutine = StartCoroutine(ZoomFOV(
            mainCamera.Lens.FieldOfView,
            endFOV,
            duration
        ));
    }

    private IEnumerator ZoomFOV(float startingFOV, float endFOV, float duration)
    {
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float progress = timer / duration;

            mainCamera.Lens.FieldOfView = Mathf.Lerp(
                startingFOV,
                endFOV,
                progress
            );

            yield return null;
        }

        mainCamera.Lens.FieldOfView = endFOV;

        zoomCoroutine = null;
    }
}
