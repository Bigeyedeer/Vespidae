using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    public GameObject settingsPanel;
    //public GameObject 

    [System.Serializable]
    public struct PanelData
    {
        public string panelName;
        public CanvasGroup panelCanvasGroup;
    }

    [Header("Panels Setup")]
    [SerializeField] private List<PanelData> panels = new List<PanelData>();
    [SerializeField] private string defaultPanelName = "MainMenu";
    [SerializeField] private float fadeDuration = 0.3f;

    private Dictionary<string, CanvasGroup> panelDict = new Dictionary<string, CanvasGroup>();
    private Stack<CanvasGroup> historyStack = new Stack<CanvasGroup>();
    private Dictionary<CanvasGroup, Coroutine> runningCoroutines = new Dictionary<CanvasGroup, Coroutine>();

    /*private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        foreach (var p in panels)
        {
            if (p.panelCanvasGroup != null && !panelDict.ContainsKey(p.panelName))
            {
                panelDict.Add(p.panelName, p.panelCanvasGroup);

                // Hide panels instantly at startup
                p.panelCanvasGroup.alpha = 0f;
                p.panelCanvasGroup.interactable = false;
                p.panelCanvasGroup.blocksRaycasts = false;
                p.panelCanvasGroup.gameObject.SetActive(false);
            }
        }
    }

    private void Start()
    {
        if (!string.IsNullOrEmpty(defaultPanelName))
        {
            OpenPanel(defaultPanelName);
        }
    }

    public void OpenPanel(string panelName)
    {
        if (!panelDict.TryGetValue(panelName, out CanvasGroup targetPanel))
        {
            Debug.LogWarning($"UIManager: Panel '{panelName}' not found!");
            return;
        }

        if (historyStack.Count > 0)
        {
            CanvasGroup currentPanel = historyStack.Peek();
            FadePanel(currentPanel, false);
        }

        FadePanel(targetPanel, true);
        historyStack.Push(targetPanel);
    }

    public void CloseCurrentPanel()
    {
        if (historyStack.Count <= 0) return;

        CanvasGroup topPanel = historyStack.Pop();
        FadePanel(topPanel, false);

        if (historyStack.Count > 0)
        {
            CanvasGroup previousPanel = historyStack.Peek();
            FadePanel(previousPanel, true);
        }
    }

    private void FadePanel(CanvasGroup canvasGroup, bool fadeIn)
    {
        if (runningCoroutines.ContainsKey(canvasGroup) && runningCoroutines[canvasGroup] != null)
        {
            StopCoroutine(runningCoroutines[canvasGroup]);
        }

        runningCoroutines[canvasGroup] = StartCoroutine(FadeRoutine(canvasGroup, fadeIn));
    }

    private IEnumerator FadeRoutine(CanvasGroup canvasGroup, bool fadeIn)
    {
        if (fadeIn)
        {
            canvasGroup.gameObject.SetActive(true);
        }

        float startAlpha = canvasGroup.alpha;
        float targetAlpha = fadeIn ? 1f : 0f;
        float timer = 0f;

        canvasGroup.interactable = fadeIn;
        canvasGroup.blocksRaycasts = fadeIn;

        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, timer / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;

        if (!fadeIn)
        {
            canvasGroup.gameObject.SetActive(false);
        }

        runningCoroutines[canvasGroup] = null;
    }*/

    public void OpenSettings()
    {
        settingsPanel.gameObject.SetActive(true);
    }

    public void CloseSettings()
    {
        settingsPanel.gameObject.SetActive(false);
    }
}