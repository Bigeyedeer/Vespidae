using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class C_MainWorldSelectionDisplay : MonoBehaviour
{
    [SerializeField] private SB_PlayerSelection_State selectionState;
    [SerializeField] private TMP_Text commonNameText;
    [SerializeField] private TMP_Text scientificNameText;
    [SerializeField] private TMP_Text functionNameText;
    [SerializeField] private TMP_Text functionSummaryText;
    [SerializeField] private Image portraitImage;
    [SerializeField] private Image accentPanel;

    private void Awake()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (selectionState == null)
        {
            Debug.LogError("MainWorld selection display has no runtime selection state assigned.");
            return;
        }

        if (!selectionState.HasSelection)
        {
            Debug.LogWarning("MainWorld was opened without a menu selection. Using Polistes marginalis and Scout fallback data.");
        }

        SB_Wasps_Info wasp = selectionState.SelectedWasp;
        WaspFunctionInfo functionInfo = selectionState.GetSelectedFunctionInfo();

        if (wasp == null)
        {
            Debug.LogError("No selected or fallback wasp is configured.");
            return;
        }

        if (commonNameText != null)
        {
            commonNameText.text = wasp.CommonName;
        }

        if (scientificNameText != null)
        {
            scientificNameText.text = wasp.ScientificName;
        }

        if (functionNameText != null)
        {
            functionNameText.text = functionInfo != null ? functionInfo.DisplayName : selectionState.SelectedFunction.ToString();
        }

        if (functionSummaryText != null)
        {
            functionSummaryText.text = functionInfo != null
                ? $"{functionInfo.Description}\n\nStarting benefit: {functionInfo.StartingBenefit}"
                : wasp.GameplaySummary;
        }

        if (portraitImage != null)
        {
            portraitImage.sprite = wasp.Portrait;
            portraitImage.gameObject.SetActive(wasp.Portrait != null);
        }

        if (accentPanel != null)
        {
            Color accent = Color.Lerp(wasp.AccentColor, new Color(0.04f, 0.08f, 0.07f, 1f), 0.75f);
            accent.a = 0f;
            accentPanel.color = accent;
        }
    }
}
