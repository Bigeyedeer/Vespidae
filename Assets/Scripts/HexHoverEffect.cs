using System.Collections;
using UnityEngine;

public class HexHoverEffect : MonoBehaviour
{
    [Header("Hover Appearance")]
    [SerializeField] private float hoverScaleMultiplier = 1.08f;
    [SerializeField] private float hoverHeight = 0.15f;
    [SerializeField] private float animationSpeed = 10f;

    private Vector3 originalScale;
    private Vector3 originalLocalPosition;

    private Vector3 targetScale;
    private Vector3 targetLocalPosition;

    private Coroutine animationCoroutine;
    private bool isHovered;

    private void Awake()
    {
        originalScale = transform.localScale;
        originalLocalPosition = transform.localPosition;

        targetScale = originalScale;
        targetLocalPosition = originalLocalPosition;
    }

    public void SetHovered(bool hovered)
    {
        if (isHovered == hovered)
            return;

        isHovered = hovered;

        if (hovered)
        {
            targetScale = originalScale * hoverScaleMultiplier;
            targetLocalPosition =
                originalLocalPosition + Vector3.up * hoverHeight;
        }
        else
        {
            targetScale = originalScale;
            targetLocalPosition = originalLocalPosition;
        }

        StartAnimation();
    }

    private void StartAnimation()
    {
        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);

        animationCoroutine = StartCoroutine(AnimateHex());
    }

    private IEnumerator AnimateHex()
    {
        while (
            Vector3.Distance(transform.localScale, targetScale) > 0.001f ||
            Vector3.Distance(transform.localPosition, targetLocalPosition) > 0.001f
        )
        {
            transform.localScale = Vector3.Lerp(
                transform.localScale,
                targetScale,
                animationSpeed * Time.deltaTime
            );

            transform.localPosition = Vector3.Lerp(
                transform.localPosition,
                targetLocalPosition,
                animationSpeed * Time.deltaTime
            );

            yield return null;
        }

        transform.localScale = targetScale;
        transform.localPosition = targetLocalPosition;

        animationCoroutine = null;
    }

    private void OnDisable()
    {
        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);

        transform.localScale = originalScale;
        transform.localPosition = originalLocalPosition;

        isHovered = false;
    }
}