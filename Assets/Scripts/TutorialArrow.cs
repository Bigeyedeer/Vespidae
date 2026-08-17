using UnityEngine;

public class TutorialArrow : MonoBehaviour
{
    [Header("Floating Animation")]
    [SerializeField] private float floatDistance = 12f;
    [SerializeField] private float floatSpeed = 3f;

    private RectTransform rectTransform;
    private Vector2 startPosition;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        startPosition = rectTransform.anchoredPosition;
    }

    private void Update()
    {
        if (rectTransform == null)
            return;

        float offset =
            Mathf.Sin(Time.unscaledTime * floatSpeed) * floatDistance;

        rectTransform.anchoredPosition =
            startPosition + Vector2.up * offset;
    }
}
