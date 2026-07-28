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
    private string actionFeedback;
    private HexTile.HexState displayedState;
    private bool displayedScouting;
    private int displayedSeconds = int.MinValue;

    private void Update()
    {
        if (selectedHex == null)
            return;

        int secondsRemaining = selectedHex.IsScouting
            ? Mathf.CeilToInt(selectedHex.ScoutingTimeRemaining)
            : -1;

        if (selectedHex.State != displayedState ||
            selectedHex.IsScouting != displayedScouting ||
            secondsRemaining != displayedSeconds)
        {
            RefreshPanel();
        }
    }

    public void Open(HexTile hex)
    {
        if (hex == null)
            return;

        selectedHex = hex;
        actionFeedback = string.Empty;
        displayedSeconds = int.MinValue;
        gameObject.SetActive(true);
        SubscribeToWorkforce();
        RefreshPanel();
    }

    private void RefreshPanel()
    {
        if (selectedHex == null)
            return;

        displayedState = selectedHex.State;
        displayedScouting = selectedHex.IsScouting;
        displayedSeconds = selectedHex.IsScouting
            ? Mathf.CeilToInt(selectedHex.ScoutingTimeRemaining)
            : -1;

        if (hexNameText != null)
            hexNameText.text = selectedHex.HexName;
        if (stateText != null)
            stateText.text = $"Status: {selectedHex.State}";

        if (discoveryText != null)
        {
            if (selectedHex.State == HexTile.HexState.Unknown)
            {
                discoveryText.text = string.IsNullOrEmpty(actionFeedback)
                    ? "Contents: Unknown"
                    : actionFeedback;
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
                ConfigureScoutAction();
                break;
            case HexTile.HexState.Scouted:
                SetAction("Claim Land", ClaimSelectedHex);
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

        primaryActionButton.interactable = action != null;
        if (action != null)
            primaryActionButton.onClick.AddListener(action);
    }

    private void ScoutSelectedHex()
    {
        HiveManagement hive = HiveManagement.GetOrCreate();
        bool dispatched = hive != null && hive.TryDispatchScout(selectedHex);
        actionFeedback = dispatched
            ? "Scout dispatched. The territory remains unknown until scouting is resolved."
            : "No available Scout can be sent to this territory.";
        C_MainWorldHUD.GetOrCreate()?.ShowSelectedHex(selectedHex);
        RefreshPanel();
    }

    private void ConfigureScoutAction()
    {
        if (selectedHex.IsScouting)
        {
            int secondsRemaining = Mathf.CeilToInt(selectedHex.ScoutingTimeRemaining);
            actionFeedback = $"Scout surveying territory. {secondsRemaining} seconds remaining.";
            SetAction($"Scouting... {secondsRemaining}s", null);
            return;
        }

        HiveManagement hive = HiveManagement.GetOrCreate();
        bool alreadyAssigned = hive != null && hive.HasScoutAssignedTo(selectedHex);
        bool available = hive != null && hive.GetAvailableWaspCount(WaspFunction.Scout) > 0;

        if (alreadyAssigned)
        {
            SetAction(selectedHex.HasFriendlyScout ? "Scout Stationed" : "Scout En Route", null);
            return;
        }

        SetAction(available ? "Send Scout" : "No Scout Available", available ? ScoutSelectedHex : null);
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
        UnsubscribeFromWorkforce();
        selectedHex = null;
        gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        UnsubscribeFromWorkforce();
    }

    private void SubscribeToWorkforce()
    {
        HiveManagement hive = HiveManagement.GetOrCreate();
        if (hive == null)
            return;

        hive.WorkforceChanged -= RefreshPanel;
        hive.WorkforceChanged += RefreshPanel;
    }

    private void UnsubscribeFromWorkforce()
    {
        if (HiveManagement.Instance != null)
            HiveManagement.Instance.WorkforceChanged -= RefreshPanel;
    }
}
