using TMPro;
using UnityEngine;

public class WaspInfoPanel : MonoBehaviour
{
    [Header("Text References")]
    [SerializeField] private TMP_Text commonNameText;
    [SerializeField] private TMP_Text scientificNameText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text ecologicalRoleText;

    private WaspInfo selectedWasp;

    public void Open(WaspInfo wasp)
    {
        if (wasp == null)
            return;

        selectedWasp = wasp;

        gameObject.SetActive(true);

        commonNameText.text = wasp.CommonName;
        scientificNameText.text = wasp.ScientificName;

        statusText.text = wasp.IsNative
            ? "Native Species"
            : "Invasive Species";

        descriptionText.text = wasp.Description;
        ecologicalRoleText.text = wasp.EcologicalRole;
    }

    public void Close()
    {
        selectedWasp = null;
        gameObject.SetActive(false);
    }
}