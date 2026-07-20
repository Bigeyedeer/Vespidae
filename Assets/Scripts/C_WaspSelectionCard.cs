using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class C_WaspSelectionCard : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image background;
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Color normalColor = new Color(0.12f, 0.18f, 0.16f, 1f);

    private WaspFunctionInfo functionInfo;
    private Action<WaspFunctionInfo, C_WaspSelectionCard> selectionCallback;
    private SB_Wasps_Info waspInfo;
    private Action<SB_Wasps_Info, C_WaspSelectionCard> waspSelectionCallback;
    private Color selectedColor;

    public void Bind(
        WaspFunctionInfo info,
        Action<WaspFunctionInfo, C_WaspSelectionCard> callback,
        Color accentColor)
    {
        functionInfo = info;
        waspInfo = null;
        selectionCallback = callback;
        waspSelectionCallback = null;
        selectedColor = Color.Lerp(accentColor, Color.white, 0.15f);

        if (titleText != null)
        {
            titleText.text = info != null ? info.DisplayName : "Unknown";
        }

        if (descriptionText != null)
        {
            descriptionText.text = info != null ? info.Description : string.Empty;
        }

        if (icon != null)
        {
            icon.sprite = info?.Icon;
            icon.gameObject.SetActive(info?.Icon != null);
        }

        if (button != null)
        {
            button.onClick.RemoveListener(HandleClick);
            button.onClick.AddListener(HandleClick);
        }
    }

    public void BindSpecies(
        SB_Wasps_Info info,
        Action<SB_Wasps_Info, C_WaspSelectionCard> callback)
    {
        waspInfo = info;
        waspSelectionCallback = callback;
        functionInfo = null;
        selectionCallback = null;
        selectedColor = info != null
            ? Color.Lerp(info.AccentColor, Color.white, 0.15f)
            : Color.white;

        if (titleText != null)
        {
            titleText.text = info != null ? info.CommonName : "Unknown species";
        }

        if (descriptionText != null)
        {
            descriptionText.text = info == null
                ? string.Empty
                : $"<i>{info.ScientificName}</i>\n\n{info.GameplaySummary}";
        }

        if (icon != null)
        {
            icon.sprite = info?.Portrait;
            icon.gameObject.SetActive(info?.Portrait != null);
        }

        if (button != null)
        {
            button.onClick.RemoveListener(HandleClick);
            button.onClick.AddListener(HandleClick);
        }
    }

    public void SetSelected(bool selected)
    {
        if (background != null)
        {
            background.color = selected ? selectedColor : normalColor;
        }
    }

    private void HandleClick()
    {
        if (waspInfo != null)
        {
            waspSelectionCallback?.Invoke(waspInfo, this);
        }
        else
        {
            selectionCallback?.Invoke(functionInfo, this);
        }
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(HandleClick);
        }
    }
}
