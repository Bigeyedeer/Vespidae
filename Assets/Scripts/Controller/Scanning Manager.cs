using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class ScanningManager : MonoBehaviour
{
    [Header("Universal UI")]
    public Image targetRadial;
    public Image targetNotification;

    public GameObject InfoPanel;
    public TextMeshProUGUI InfoTitle;
    public Image InfoImage;
    public TextMeshProUGUI InfoDescription;
    public TextMeshProUGUI InfoType;
    public Image InfoPanelColor;

    [Header("Settings")]
    public float scanDuration = 5f;

    public bool isScanning { get; private set; }
    public bool scanCompleted { get; private set; }

    private Coroutine scanCoroutine;

    private ScannableObject currentObject;

    private void Start()
    {
        targetRadial.fillAmount = 1f;
        targetRadial.gameObject.SetActive(false);
        InfoPanel.SetActive(false);
    }

    public void BeginScan(ScannableObject target)
    {
        currentObject = target;
        targetRadial = target.radialTarget;
        targetNotification = target.notificationImage;

        if (currentObject.hasBeenScanned)
        {
            InfoPanel.SetActive(true);
            UpdateInformation();
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
        InfoPanel.SetActive(false);
        targetNotification.gameObject.SetActive(false);

    }

    private IEnumerator ScanRoutine()
    {
        isScanning = true;
        currentObject.hasBeenScanned = false;

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
        currentObject.hasBeenScanned = true;

        DisplayAttributes();
        targetNotification.gameObject.SetActive(true);

        scanCoroutine = null;
    }

    private void DisplayAttributes()
    {
        // Debug.Log("DisplayWaspAttributes called");
        UpdateInformation();
        InfoPanel.SetActive(true);
    }

    public void ToggleAttributes()
    {
        if (currentObject.hasBeenScanned)
        {
            InfoPanel.SetActive(!InfoPanel.activeSelf);
            targetNotification.enabled = !InfoPanel.activeSelf;
        }
            
    }

    public void UpdateInformation()
    {
        InfoTitle.text = currentObject.objectName;
        InfoDescription.text = currentObject.description;
        InfoImage = currentObject.objectImage;
        InfoType.text = currentObject.scanType.ToString();
        InfoPanelColor.color = currentObject.uiColor;
    }
}