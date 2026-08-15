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
    public GameObject scanMessagePanel;
    public TextMeshProUGUI scanMessageText;
    private int lastMessageIndex = -1;

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

    public ZoomBehavior zoomBehavior;

    private void Start()
    {
        
    }

    public void BeginScan(ScannableObject target)
    {
        InfoPanel = target.InfoPanel;
        currentObject = target;
        //targetRadial = target.radialTarget;

        targetNotification = target.notificationImage;

        if (currentObject.hasBeenScanned)
        {
            InfoPanel.SetActive(true);
            zoomBehavior.SetFoV(zoomBehavior.zoomFOV, .5f);
            //UpdateInformation();
            return;
        }

        if (isScanning)
            return;

        targetRadial.gameObject.SetActive(true);

        zoomBehavior.SetFoV(zoomBehavior.zoomFOV, scanDuration);

        scanCoroutine = StartCoroutine(ScanRoutine());
    }

    public void CancelScan()
    {
        if (scanCoroutine != null)
            StopCoroutine(scanCoroutine);

        scanCoroutine = null;

        isScanning = false;
        //scanCompleted = false;

        if (targetRadial != null)
        {
            targetRadial.fillAmount = 1f;
            targetRadial.gameObject.SetActive(false);
        }

        InfoPanel.SetActive(false);

        zoomBehavior.CancelZoom(zoomBehavior.defaultFOV, 0.5f);

    }

    private IEnumerator ScanRoutine()
    {
        //FOV change
        

        isScanning = true;

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
        currentObject.completedScans++;
        StartCoroutine(ShowScanMessage(3));

        if (currentObject.completedScans >= currentObject.requiredScans)
        {
            currentObject.hasBeenScanned = true;
            DisplayAttributes();
            targetNotification.gameObject.SetActive(true);
        }

        
        scanCoroutine = null;
    }

    public IEnumerator ShowScanMessage(int sec)
    {
        int messageIndex = currentObject.completedScans -1;

        if (currentObject.requiredScans == 1)
        {
            yield break;
        }

        if (currentObject.scanMessages == null || messageIndex >= currentObject.scanMessages.Length)
        {
            yield break;
        }

        scanMessageText.text = currentObject.scanMessages[messageIndex];
        scanMessagePanel.SetActive(true);
        yield return new WaitForSeconds(sec);
        scanMessagePanel.SetActive(false);
    }
    private void DisplayAttributes()
    {
        //UpdateInformation(); old manual TextmeshproGUI changes
        InfoPanel.SetActive(true);
        //targetNotification.gameObject.SetActive(true);
    }

    public void ToggleAttributes()
    {
        if (currentObject != null && currentObject.hasBeenScanned)
        {
            InfoPanel.SetActive(!InfoPanel.activeSelf);
            //targetNotification.enabled = !InfoPanel.activeSelf;
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