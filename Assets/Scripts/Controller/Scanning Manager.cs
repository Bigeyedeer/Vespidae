using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ScanningManager : MonoBehaviour
{
    [Header("UI")]
    public Image targetRadial;
    public GameObject waspInfoPanel;

    [Header("Settings")]
    public float scanDuration = 5f;

    public bool isScanning { get; private set; }
    public bool scanCompleted { get; private set; }

    private Coroutine scanCoroutine;

    private void Start()
    {
        targetRadial.fillAmount = 1f;
        targetRadial.gameObject.SetActive(false);
        waspInfoPanel.SetActive(false);
    }

    public void BeginScan()
    {
        if (scanCompleted)
        {
            waspInfoPanel.SetActive(true);
            return;
        }

        if (isScanning)
            return;

        targetRadial.gameObject.SetActive(true);
        scanCoroutine = StartCoroutine(ScanRoutine());
    }

    public void CancelScan()
    {
        if (scanCoroutine != null)
            StopCoroutine(scanCoroutine);

        scanCoroutine = null;

        isScanning = false;
        //scanCompleted = false;

        targetRadial.fillAmount = 1f;
        targetRadial.gameObject.SetActive(false);
        waspInfoPanel.SetActive(false);

    }

    private IEnumerator ScanRoutine()
    {
        isScanning = true;
        scanCompleted = false;

        float timer = scanDuration;

        while (timer > 0f)
        {
            timer -= Time.deltaTime;

            targetRadial.fillAmount = timer / scanDuration;

            yield return null;
        }

        targetRadial.fillAmount = 0f;
        targetRadial.gameObject.SetActive(false);

        isScanning = false;
        scanCompleted = true;

        DisplayWaspAttributes();

        scanCoroutine = null;
    }

    private void DisplayWaspAttributes()
    {
       // Debug.Log("DisplayWaspAttributes called");
        waspInfoPanel.SetActive(true);
    }

    public void ToggleAttributes()
    {
        if (scanCompleted)
            waspInfoPanel.SetActive(!waspInfoPanel.activeSelf);
    }
}