using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Fades the opening title card in and out, once per launch, before the menu is usable.
///
/// The card itself is authored in the scene - names, roles, title, backdrop and layout are all real
/// objects you can open and rearrange. This script only handles the timing and the fade, so changing
/// who is credited or how it looks never means touching code.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class C_StartCredits : MonoBehaviour
{
    /// <summary>Set once the card has played, so returning to the menu mid-session does not replay it.</summary>
    private static bool hasPlayed;

    [Header("Timing")]
    [SerializeField, Min(0f)] private float fadeInSeconds = 0.9f;
    [SerializeField, Min(0f), Tooltip("How long the card sits fully visible.")]
    private float holdSeconds = 3.6f;
    [SerializeField, Min(0f)] private float fadeOutSeconds = 0.9f;

    [Header("Behaviour")]
    [SerializeField, Tooltip("Let any key or click skip the card.")]
    private bool skippable = true;
    [SerializeField, Tooltip("Show only on the first visit to the menu each launch.")]
    private bool onlyOncePerLaunch = true;
    [SerializeField, Tooltip("Deactivate the card once it has finished, instead of leaving it at zero alpha.")]
    private bool disableWhenFinished = true;

    private CanvasGroup group;
    private float elapsed;
    private bool skipped;

    private void Awake()
    {
        group = GetComponent<CanvasGroup>();

        if (onlyOncePerLaunch && hasPlayed)
        {
            gameObject.SetActive(false);
            return;
        }

        hasPlayed = true;
        group.alpha = 0f;
        group.blocksRaycasts = true;      // Swallow clicks meant for the menu underneath.
    }

    private void Update()
    {
        elapsed += Time.unscaledDeltaTime;

        if (skippable && !skipped && elapsed > 0.35f && AnyInputThisFrame())
        {
            skipped = true;
            elapsed = Mathf.Max(elapsed, fadeInSeconds + holdSeconds);
        }

        float fadeOutStart = fadeInSeconds + holdSeconds;

        if (elapsed < fadeInSeconds)
            group.alpha = fadeInSeconds <= 0f ? 1f : Mathf.Clamp01(elapsed / fadeInSeconds);
        else if (elapsed < fadeOutStart)
            group.alpha = 1f;
        else
            group.alpha = fadeOutSeconds <= 0f ? 0f : Mathf.Clamp01(1f - (elapsed - fadeOutStart) / fadeOutSeconds);

        if (elapsed > fadeOutStart && group.alpha <= 0f)
        {
            group.blocksRaycasts = false;
            if (disableWhenFinished)
                gameObject.SetActive(false);
            else
                enabled = false;
        }
    }

    /// <summary>
    /// True on the frame any key or the left mouse button goes down.
    ///
    /// Read through the Input System package, which this project has active - the old
    /// <c>UnityEngine.Input</c> class throws outright under that setting.
    /// </summary>
    private static bool AnyInputThisFrame()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            return true;

        return Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame;
    }
}
