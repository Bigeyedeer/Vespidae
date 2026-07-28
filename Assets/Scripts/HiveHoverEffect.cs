using UnityEngine;

public class HiveHoverEffect : MonoBehaviour
{
    [SerializeField] private BoxCollider clickTrigger;

    public BoxCollider ClickTrigger => clickTrigger != null
        ? clickTrigger
        : GetComponentInChildren<BoxCollider>(true);

    public bool IsHovered { get; private set; }

    public void SetHovered(bool hovered)
    {
        IsHovered = hovered;
    }

    private void Awake()
    {
        if (clickTrigger == null)
            clickTrigger = GetComponentInChildren<BoxCollider>(true);
    }
}
