using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonSound : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler
{
    [SerializeField] private AudioClip customClickSound;
    [SerializeField] private AudioClip customHoverSound;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayClickSound(customClickSound);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayHoverSound(customHoverSound);
    }
}