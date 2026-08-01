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
    private Button foragerActionButton;
    private Button builderActionButton;
    private TMP_Text foragerActionButtonText;
    private TMP_Text builderActionButtonText;
    private RectTransform primaryActionRect;
    private Vector2 primaryDefaultPosition;
    private Vector2 primaryDefaultSize;
    private string actionFeedback;
    private HexTile.HexState displayedState;
    private bool displayedScouting;
    private int displayedSeconds = int.MinValue;

    private void Awake()
    {
        EnsureDispatchButtons();
    }

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

        UnsubscribeFromHex();
        selectedHex = hex;
        selectedHex.ResourcesChanged += OnHexResourcesChanged;
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

        EnsureDispatchButtons();
        displayedState = selectedHex.State;
        displayedScouting = selectedHex.IsScouting;
        displayedSeconds = selectedHex.IsScouting
            ? Mathf.CeilToInt(selectedHex.ScoutingTimeRemaining)
            : -1;

        if (hexNameText != null)
            hexNameText.text = selectedHex.HexName;
        if (stateText != null)
            stateText.text = $"Status: {selectedHex.State}";

        RefreshDetails();
        HideDispatchButtons();
        if (primaryActionButton == null)
            return;

        switch (selectedHex.State)
        {
            case HexTile.HexState.Unknown:
                ConfigureScoutAction();
                break;
            case HexTile.HexState.Scouted:
                SetSingleAction("Claim Land", ClaimSelectedHex, true);
                break;
            case HexTile.HexState.Owned:
                ConfigureOwnedHex();
                break;
            case HexTile.HexState.Enemy:
                SetSingleAction("Enemy Territory", null, false);
                break;
            case HexTile.HexState.Locked:
                if (discoveryText != null)
                    discoveryText.text = "This territory is currently locked.";
                break;
        }
    }

    private void RefreshDetails()
    {
        if (discoveryText == null)
            return;

        if (selectedHex.State == HexTile.HexState.Unknown)
        {
            discoveryText.text = string.IsNullOrEmpty(actionFeedback)
                ? "Contents: Unknown\nSend one Scout to survey this territory."
                : actionFeedback;
            return;
        }

        int foragerCount = selectedHex.GetFriendlyWaspCount(WaspFunction.Forager);
        discoveryText.text =
            $"Contents: {selectedHex.Content}\n" +
            $"Prey remaining: {selectedHex.PreyRemaining:0}\n" +
            $"Nectar remaining: {selectedHex.NectarRemaining:0}\n" +
            $"Fibre remaining: {selectedHex.FibreRemaining:0}\n" +
            $"Foragers: {foragerCount}/{selectedHex.MaximumForagersPerHex}\n" +
            $"Auto production / {selectedHex.GatheringTickIntervalSeconds:0.#} sec: " +
            $"P +{selectedHex.GetPreyGatherAmount(foragerCount):0}  " +
            $"N +{selectedHex.GetNectarGatherAmount(foragerCount):0}  " +
            $"F +{selectedHex.GetFibreGatherAmount(foragerCount):0}";
    }

    private void ConfigureOwnedHex()
    {
        HiveManagement hive = HiveManagement.GetOrCreate();
        ConfigureDispatchLayout();
        SetDispatchAction(
            primaryActionButton,
            primaryActionButtonText,
            WaspFunction.Scout,
            "Scout",
            hive);
        SetDispatchAction(
            foragerActionButton,
            foragerActionButtonText,
            WaspFunction.Forager,
            "Forager",
            hive);
        SetDispatchAction(
            builderActionButton,
            builderActionButtonText,
            WaspFunction.Builder,
            "Builder",
            hive);
    }

    private void ConfigureScoutAction()
    {
        if (selectedHex.IsScouting)
        {
            int secondsRemaining = Mathf.CeilToInt(selectedHex.ScoutingTimeRemaining);
            actionFeedback = $"Scout surveying territory. {secondsRemaining} seconds remaining.";
            SetSingleAction($"Scouting... {secondsRemaining}s", null, false);
            return;
        }

        HiveManagement hive = HiveManagement.GetOrCreate();
        bool alreadyAssigned = hive != null && hive.HasScoutAssignedTo(selectedHex);
        bool available = hive != null && hive.CanDispatchToHex(selectedHex, WaspFunction.Scout);
        if (alreadyAssigned)
        {
            SetSingleAction(selectedHex.HasFriendlyScout ? "Scout Stationed" : "Scout En Route", null, false);
            return;
        }

        SetSingleAction(available ? "Send Scout" : "No Scout Available", ScoutSelectedHex, available);
    }

    private void SetDispatchAction(Button button, TMP_Text text, WaspFunction role, string label, HiveManagement hive)
    {
        if (button == null)
            return;

        int available = hive != null ? hive.GetAvailableWaspCount(role) : 0;
        bool canDispatch = hive != null && hive.CanDispatchToHex(selectedHex, role);
        button.gameObject.SetActive(true);
        button.onClick.RemoveAllListeners();
        button.interactable = canDispatch;
        if (text != null)
            text.text = $"{label}\n{available} available";
        if (canDispatch)
            button.onClick.AddListener(() => Dispatch(role));
    }

    private void SetSingleAction(string label, UnityEngine.Events.UnityAction action, bool interactable)
    {
        RestorePrimaryLayout();
        primaryActionButton.gameObject.SetActive(true);
        primaryActionButton.onClick.RemoveAllListeners();
        primaryActionButton.interactable = interactable && action != null;
        if (primaryActionButtonText != null)
            primaryActionButtonText.text = label;
        if (action != null)
            primaryActionButton.onClick.AddListener(action);
    }

    private void ScoutSelectedHex()
    {
        Dispatch(WaspFunction.Scout);
    }

    private void Dispatch(WaspFunction role)
    {
        HiveManagement hive = HiveManagement.GetOrCreate();
        bool dispatched = hive != null && hive.TryDispatchWasp(selectedHex, role);
        actionFeedback = dispatched
            ? $"{role} dispatched to {selectedHex.HexName}."
            : $"No available {role} can be sent to this territory.";
        C_MainWorldHUD.GetOrCreate()?.ShowSelectedHex(selectedHex);
        RefreshPanel();
    }

    private void ClaimSelectedHex()
    {
        selectedHex.Claim();
        C_MainWorldHUD.GetOrCreate()?.ShowSelectedHex(selectedHex);
        RefreshPanel();
    }

    private void EnsureDispatchButtons()
    {
        if (primaryActionButton == null || foragerActionButton != null || builderActionButton != null)
            return;

        primaryActionRect = primaryActionButton.GetComponent<RectTransform>();
        if (primaryActionRect == null)
            return;

        primaryDefaultPosition = primaryActionRect.anchoredPosition;
        primaryDefaultSize = primaryActionRect.sizeDelta;
        foragerActionButton = CreateActionClone("Send Forager Button", out foragerActionButtonText);
        builderActionButton = CreateActionClone("Send Builder Button", out builderActionButtonText);
        HideDispatchButtons();
    }

    private Button CreateActionClone(string objectName, out TMP_Text text)
    {
        GameObject clone = Instantiate(primaryActionButton.gameObject, primaryActionButton.transform.parent);
        clone.name = objectName;
        Button button = clone.GetComponent<Button>();
        button.onClick.RemoveAllListeners();
        text = clone.GetComponentInChildren<TMP_Text>(true);
        clone.SetActive(false);
        return button;
    }

    private void ConfigureDispatchLayout()
    {
        RectTransform[] buttons =
        {
            primaryActionButton.GetComponent<RectTransform>(),
            foragerActionButton != null ? foragerActionButton.GetComponent<RectTransform>() : null,
            builderActionButton != null ? builderActionButton.GetComponent<RectTransform>() : null
        };

        float width = 136f;
        for (int index = 0; index < buttons.Length; index++)
        {
            if (buttons[index] == null)
                continue;

            buttons[index].anchoredPosition = new Vector2(24f + index * 148f, primaryDefaultPosition.y);
            buttons[index].sizeDelta = new Vector2(width, primaryDefaultSize.y);
        }
    }

    private void RestorePrimaryLayout()
    {
        if (primaryActionRect == null)
            return;

        primaryActionRect.anchoredPosition = primaryDefaultPosition;
        primaryActionRect.sizeDelta = primaryDefaultSize;
    }

    private void HideDispatchButtons()
    {
        if (foragerActionButton != null)
            foragerActionButton.gameObject.SetActive(false);
        if (builderActionButton != null)
            builderActionButton.gameObject.SetActive(false);
    }

    public void Close()
    {
        UnsubscribeFromWorkforce();
        UnsubscribeFromHex();
        selectedHex = null;
        gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        UnsubscribeFromWorkforce();
        UnsubscribeFromHex();
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

    private void UnsubscribeFromHex()
    {
        if (selectedHex != null)
            selectedHex.ResourcesChanged -= OnHexResourcesChanged;
    }

    private void OnHexResourcesChanged(HexTile hex)
    {
        RefreshPanel();
    }
}
