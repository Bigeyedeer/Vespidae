using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WaspInfoPanel : MonoBehaviour
{
    [Header("Text References")]
    [SerializeField] private Graphic commonNameText;
    [SerializeField] private Graphic scientificNameText;
    [SerializeField] private Graphic statusText;
    [SerializeField] private Graphic descriptionText;
    [SerializeField] private Graphic ecologicalRoleText;
    [SerializeField] private Graphic aggressionText;
    [SerializeField] private Graphic classificationText;
    [SerializeField] private Graphic confidenceText;
    [SerializeField] private Image portraitImage;
    [SerializeField] private Image confidenceBar;

    private WaspInfo selectedWasp;

    public void Open(WaspInfo wasp)
    {
        if (wasp == null)
            return;

        selectedWasp = wasp;

        gameObject.SetActive(true);

        SetText(commonNameText, wasp.CommonName);
        SetText(scientificNameText, wasp.ScientificName);

        string classification = wasp.IsNative
            ? "Native Species"
            : "Invasive Species";

        SetText(statusText, classification);
        SetText(descriptionText, wasp.Description);
        SetText(ecologicalRoleText, wasp.EcologicalRole);
        SetText(aggressionText, $"Ecological role       {wasp.EcologicalRole}");
        SetText(classificationText, wasp.IsNative ? "Native" : "Invasive");
        SetText(confidenceText, "ID confidence                                      100%");

        if (portraitImage != null)
        {
            Sprite portrait = wasp.SpeciesInfo != null ? wasp.SpeciesInfo.Portrait : null;
            if (portrait != null)
                portraitImage.sprite = portrait;

            portraitImage.enabled = true;
        }

        if (confidenceBar != null)
        {
            confidenceBar.fillAmount = 1f;
        }
    }

    public void Close()
    {
        selectedWasp = null;
        gameObject.SetActive(false);
    }

    private static void SetText(Graphic target, string value)
    {
        if (target == null)
            return;

        TMP_Text tmp = target as TMP_Text;
        if (tmp != null)
        {
            tmp.text = value ?? string.Empty;
            return;
        }

        Text legacyText = target as Text;
        if (legacyText != null)
            legacyText.text = value ?? string.Empty;
    }
}
