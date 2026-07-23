using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HexOptionsPanel : MonoBehaviour
{
    [Header("Information")]
    [SerializeField] private TMP_Text hexNameText;
    [SerializeField] private TMP_Text stateText;
    [SerializeField] private TMP_Text discoveryText;

    [Header("Action Button")]
    [SerializeField] private Button primaryActionButton;
    [SerializeField] private TMP_Text primaryActionButtonText;

    private HexTile selectedHex;

    public void Open(HexTile hex)
    {
        if (hex == null)
            return;

        selectedHex = hex;
        gameObject.SetActive(true);

        RefreshPanel();
    }

    private void RefreshPanel()
    {
        if (selectedHex == null)
            return;

        hexNameText.text = selectedHex.HexName;
        stateText.text = $"Status: {selectedHex.State}";

        if (selectedHex.State == HexTile.HexState.Unknown)
        {
            discoveryText.text = "Contents: Unknown";
        }
        else
        {
            discoveryText.text = $"Contents: {selectedHex.Content}";
        }

        primaryActionButton.onClick.RemoveAllListeners();
        primaryActionButton.gameObject.SetActive(true);

        switch (selectedHex.State)
        {
            case HexTile.HexState.Unknown:
                primaryActionButtonText.text = "Send Scout";
                primaryActionButton.onClick.AddListener(ScoutSelectedHex);
                break;

            case HexTile.HexState.Scouted:
                primaryActionButtonText.text = "Claim Hex";
                primaryActionButton.onClick.AddListener(ClaimSelectedHex);
                break;

            case HexTile.HexState.Owned:
                ConfigureOwnedHex();
                break;

            case HexTile.HexState.Enemy:
                primaryActionButtonText.text = "Attack Hex";
                break;

            case HexTile.HexState.Locked:
                primaryActionButton.gameObject.SetActive(false);
                discoveryText.text = "This territory is currently locked.";
                break;
        }
    }

    private void ConfigureOwnedHex()
    {
        if (selectedHex.Content == HexTile.HexContent.Protein)
        {
            primaryActionButtonText.text = "Gather Protein +0.5";
            primaryActionButton.onClick.AddListener(GatherProtein);
        }
        else
        {
            primaryActionButton.gameObject.SetActive(false);
            discoveryText.text = "Safe territory secured.";
        }
    }

    private void ScoutSelectedHex()
    {
        selectedHex.Scout();
        RefreshPanel();
    }

    private void ClaimSelectedHex()
    {
        selectedHex.Claim();
        RefreshPanel();
    }

    private void GatherProtein()
    {
        selectedHex.GatherProtein();
        RefreshPanel();
    }

    public void Close()
    {
        selectedHex = null;
        gameObject.SetActive(false);
    }
}
