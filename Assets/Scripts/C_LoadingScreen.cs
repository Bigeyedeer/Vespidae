using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// The loading screen shown between scenes.
///
/// It builds its own canvas in code and survives the scene change, so there is nothing to wire up in
/// either scene and nothing to keep in sync when a scene is rebuilt. Call
/// <see cref="LoadScene(string)"/> anywhere a plain <c>SceneManager.LoadScene</c> would have been used.
///
/// The scene is loaded asynchronously but held at the activation gate until the bar has actually
/// reached the end, so the player never sees the bar jump from a third full straight to gone. Once the
/// new scene is live it waits a couple of frames for that scene's own Awake/Start work to finish before
/// fading out, which is what stops the first frame of the map showing half-built.
/// </summary>
public class C_LoadingScreen : MonoBehaviour
{
    private const int SortingOrder = 30000;
    private const float MinimumDisplaySeconds = 2.2f;
    private const float FactRotationSeconds = 6f;
    private const float FadeOutSeconds = 0.45f;
    private const float BarCatchUpSpeed = 0.9f;

    // Same palette as the HUD meters, so the bar reads as the one the rest of the game uses.
    private static readonly Color Backdrop = new Color(0.055f, 0.062f, 0.055f, 1f);
    private static readonly Color PanelFill = new Color(0.129f, 0.122f, 0.106f, 0.96f);
    private static readonly Color Amber = new Color(0.949f, 0.706f, 0.267f, 1f);
    private static readonly Color AmberDim = new Color(0.949f, 0.706f, 0.267f, 0.55f);
    private static readonly Color BarTrack = new Color(0.06f, 0.10f, 0.07f, 1f);
    private static readonly Color BarFill = new Color(0.38f, 0.72f, 0.16f, 1f);
    private static readonly Color BodyText = new Color(0.88f, 0.86f, 0.80f, 1f);

    public static C_LoadingScreen Instance { get; private set; }

    private CanvasGroup canvasGroup;
    private RectTransform barFill;
    private RectTransform barTrack;
    private TMP_Text percentLabel;
    private TMP_Text statusLabel;
    private TMP_Text factLabel;

    private float displayedProgress;

    /// <summary>True while a load is in flight, so callers cannot stack two loads on top of each other.</summary>
    public static bool IsLoading => Instance != null;

    /// <summary>
    /// Shows the loading screen and moves to <paramref name="sceneName"/>. Falls back to a direct load
    /// if the scene name is empty, so a mis-set field still gets the player somewhere.
    /// </summary>
    public static void LoadScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("A scene load was requested without a scene name.");
            return;
        }

        if (Instance != null)
            return;

        GameObject host = new GameObject("LoadingScreen");
        DontDestroyOnLoad(host);

        C_LoadingScreen screen = host.AddComponent<C_LoadingScreen>();
        screen.Build();
        screen.StartCoroutine(screen.RunLoad(sceneName));
    }

    /// <summary>Replaces the line under "Did you know?" with the next fact in the pool.</summary>
    public void ShowNextFact()
    {
        if (factLabel == null)
            return;

        string fact = WaspFacts.Next();
        if (!string.IsNullOrEmpty(fact))
            factLabel.text = fact;
    }

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private IEnumerator RunLoad(string sceneName)
    {
        float startTime = Time.unscaledTime;
        float nextFactTime = startTime + FactRotationSeconds;

        // One frame with the screen up before the load starts, otherwise the first synchronous chunk of
        // work happens before anything has been drawn and the player stares at the old scene instead.
        yield return null;

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        if (operation == null)
        {
            Debug.LogError($"Scene '{sceneName}' could not be loaded. Check that it is in the build settings.");
            Destroy(gameObject);
            yield break;
        }

        operation.allowSceneActivation = false;

        // Unity stalls a held load at 0.9, so that is what counts as "all the way along the bar".
        while (displayedProgress < 0.999f)
        {
            float elapsed = Time.unscaledTime - startTime;
            float target = Mathf.Clamp01(operation.progress / 0.9f);

            // Do not let the bar finish before the minimum display time, so a fast load still reads as
            // a load rather than a flicker.
            target = Mathf.Min(target, elapsed / MinimumDisplaySeconds);

            displayedProgress = Mathf.MoveTowards(
                displayedProgress,
                target,
                BarCatchUpSpeed * Time.unscaledDeltaTime);

            ApplyProgress();

            if (Time.unscaledTime >= nextFactTime)
            {
                nextFactTime = Time.unscaledTime + FactRotationSeconds;
                ShowNextFact();
            }

            yield return null;
        }

        SetStatus("Opening the map");
        operation.allowSceneActivation = true;

        while (!operation.isDone)
            yield return null;

        // A paused match that quits to the menu would otherwise hand the next scene a frozen clock.
        Time.timeScale = 1f;

        // Let the new scene's Awake, OnEnable and Start all run, plus one drawn frame, before the
        // curtain comes up.
        yield return null;
        yield return new WaitForEndOfFrame();
        yield return null;

        float fadeStart = Time.unscaledTime;
        while (Time.unscaledTime - fadeStart < FadeOutSeconds)
        {
            float t = (Time.unscaledTime - fadeStart) / FadeOutSeconds;
            canvasGroup.alpha = 1f - Mathf.SmoothStep(0f, 1f, t);
            yield return null;
        }

        Destroy(gameObject);
    }

    private void ApplyProgress()
    {
        if (barFill != null && barTrack != null)
        {
            Vector2 size = barFill.sizeDelta;
            size.x = barTrack.rect.width * Mathf.Clamp01(displayedProgress);
            barFill.sizeDelta = size;
        }

        if (percentLabel != null)
            percentLabel.text = $"{Mathf.RoundToInt(displayedProgress * 100f)}%";
    }

    private void SetStatus(string status)
    {
        if (statusLabel != null)
            statusLabel.text = status;
    }

    // ---------------------------------------------------------------------------------------------
    // UI construction
    // ---------------------------------------------------------------------------------------------

    private void Build()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SortingOrder;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        // The raycaster is here to swallow clicks aimed at whatever is behind the curtain.
        gameObject.AddComponent<GraphicRaycaster>();

        canvasGroup = gameObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        RectTransform root = GetComponent<RectTransform>();

        CreateImage("Backdrop", root, Backdrop, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        RectTransform panel = CreatePanel(root);
        BuildHeader(panel);
        BuildBar(panel);
        BuildFact(panel);

        ShowNextFact();
        ApplyProgress();
    }

    private RectTransform CreatePanel(RectTransform parent)
    {
        // Two stacked images give the amber outline the rest of the UI uses without needing a sprite.
        RectTransform frame = CreateImage(
            "Panel", parent, AmberDim,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(1040f, 460f));

        CreateImage(
            "PanelFill", frame, PanelFill,
            Vector2.zero, Vector2.one,
            Vector2.zero, Vector2.zero, new Vector2(3f, 3f));

        return frame;
    }

    private void BuildHeader(RectTransform panel)
    {
        TMP_Text title = CreateText(
            "Title", panel, "VESPIDAE WARS", 58f, Amber,
            TextAlignmentOptions.Center,
            new Vector2(0.5f, 1f), new Vector2(0f, -78f), new Vector2(900f, 70f));
        title.characterSpacing = 14f;
        title.fontStyle = FontStyles.Bold;

        statusLabel = CreateText(
            "Status", panel, "Preparing the colony", 26f, BodyText,
            TextAlignmentOptions.Center,
            new Vector2(0.5f, 1f), new Vector2(0f, -136f), new Vector2(900f, 34f));
    }

    private void BuildBar(RectTransform panel)
    {
        RectTransform barFrame = CreateImage(
            "BarFrame", panel, AmberDim,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -6f), new Vector2(846f, 30f));

        barTrack = CreateImage(
            "BarTrack", barFrame, BarTrack,
            Vector2.zero, Vector2.one,
            Vector2.zero, Vector2.zero, new Vector2(3f, 3f));

        // Left-anchored so growing the width fills the track from the left, the same way the HUD
        // meters are driven.
        barFill = CreateImage(
            "BarFill", barTrack, BarFill,
            new Vector2(0f, 0f), new Vector2(0f, 1f),
            Vector2.zero, new Vector2(0f, 0f));
        barFill.pivot = new Vector2(0f, 0.5f);
        barFill.anchoredPosition = Vector2.zero;

        percentLabel = CreateText(
            "Percent", panel, "0%", 24f, Amber,
            TextAlignmentOptions.Right,
            new Vector2(0.5f, 0.5f), new Vector2(423f, 30f), new Vector2(120f, 30f));
        percentLabel.rectTransform.pivot = new Vector2(1f, 0.5f);
    }

    private void BuildFact(RectTransform panel)
    {
        TMP_Text heading = CreateText(
            "FactHeading", panel, "DID YOU KNOW?", 22f, Amber,
            TextAlignmentOptions.Center,
            new Vector2(0.5f, 0f), new Vector2(0f, 172f), new Vector2(900f, 28f));
        heading.characterSpacing = 8f;
        heading.fontStyle = FontStyles.Bold;

        // Top aligned inside its box, so a one-line fact and a three-line fact both start directly
        // under the heading instead of drifting away from it.
        factLabel = CreateText(
            "Fact", panel, string.Empty, 26f, BodyText,
            TextAlignmentOptions.Top,
            new Vector2(0.5f, 0f), new Vector2(0f, 44f), new Vector2(880f, 108f));
        factLabel.enableAutoSizing = true;
        factLabel.fontSizeMin = 18f;
        factLabel.fontSizeMax = 26f;
    }

    private RectTransform CreateImage(
        string objectName,
        RectTransform parent,
        Color colour,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 anchoredPosition,
        Vector2 size,
        Vector2 inset = default)
    {
        GameObject item = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        RectTransform rect = item.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size - inset * 2f;

        Image image = item.GetComponent<Image>();
        image.color = colour;
        return rect;
    }

    private TMP_Text CreateText(
        string objectName,
        RectTransform parent,
        string content,
        float fontSize,
        Color colour,
        TextAlignmentOptions alignment,
        Vector2 anchor,
        Vector2 anchoredPosition,
        Vector2 size)
    {
        GameObject item = new GameObject(objectName, typeof(RectTransform));
        RectTransform rect = item.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        TextMeshProUGUI text = item.AddComponent<TextMeshProUGUI>();
        text.text = content;
        text.fontSize = fontSize;
        text.color = colour;
        text.alignment = alignment;
        text.raycastTarget = false;
        return text;
    }
}
