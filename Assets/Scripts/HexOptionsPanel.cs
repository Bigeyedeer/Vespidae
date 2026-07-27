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

        if (hexNameText != null)
            hexNameText.text = selectedHex.HexName;
        if (stateText != null)
            stateText.text = $"Status: {selectedHex.State}";

        if (discoveryText != null)
        {
            if (selectedHex.State == HexTile.HexState.Unknown)
            {
                discoveryText.text = "Contents: Unknown";
            }
            else
            {
                discoveryText.text =
                    $"Contents: {selectedHex.Content}\n" +
                    $"Prey remaining: {selectedHex.PreyRemaining:0.##}\n" +
                    $"Nectar remaining: {selectedHex.NectarRemaining:0.##}\n" +
                    $"Fibre remaining: {selectedHex.FibreRemaining:0.##}\n" +
                    $"Gather tick: {selectedHex.GatheringTickIntervalSeconds:0.##} seconds";
            }
        }

        if (primaryActionButton == null)
            return;

        primaryActionButton.onClick.RemoveAllListeners();
        primaryActionButton.gameObject.SetActive(true);

        switch (selectedHex.State)
        {
            case HexTile.HexState.Unknown:
                SetAction("Send Scout", ScoutSelectedHex);
                break;
            case HexTile.HexState.Scouted:
                SetAction("Claim Hex", ClaimSelectedHex);
                break;
            case HexTile.HexState.Owned:
                ConfigureOwnedHex();
                break;
            case HexTile.HexState.Enemy:
                SetAction("Attack Hex", null);
                break;
            case HexTile.HexState.Locked:
                primaryActionButton.gameObject.SetActive(false);
                if (discoveryText != null)
                    discoveryText.text = "This territory is currently locked.";
                break;
        }
    }

    private void ConfigureOwnedHex()
    {
        bool hasPrey = selectedHex.HasPrey;
        bool hasNectar = selectedHex.HasNectar;
        bool hasFibre = selectedHex.HasFibre;

        if (hasPrey || hasNectar || hasFibre)
        {
            string label = $"Gather P +{selectedHex.GetPreyGatherAmount(1):0.##} / " +
                           $"N +{selectedHex.GetNectarGatherAmount(1):0.##} / " +
                           $"F +{selectedHex.GetFibreGatherAmount(1):0.##}";
            SetAction(label, GatherResources);
        }
        else
        {
            primaryActionButton.gameObject.SetActive(false);
            if (discoveryText != null)
                discoveryText.text = "Safe territory secured.";
        }
    }

    private void SetAction(string label, UnityEngine.Events.UnityAction action)
    {
        if (primaryActionButtonText != null)
            primaryActionButtonText.text = label;

        if (action != null)
            primaryActionButton.onClick.AddListener(action);
    }

    private void ScoutSelectedHex()
    {
        selectedHex.Scout();
        C_MainWorldHUD.GetOrCreate()?.ShowSelectedHex(selectedHex);
        RefreshPanel();
    }

    private void ClaimSelectedHex()
    {
        selectedHex.Claim();
        C_MainWorldHUD.GetOrCreate()?.ShowSelectedHex(selectedHex);
        RefreshPanel();
    }

    private void GatherResources()
    {
        if (selectedHex.HasPrey)
            selectedHex.GatherPrey();
        if (selectedHex.HasNectar)
            selectedHex.GatherNectar(1);
        if (selectedHex.HasFibre)
            selectedHex.GatherFibre(1);
        RefreshPanel();
    }

    public void Close()
    {
        selectedHex = null;
        gameObject.SetActive(false);
    }
}
