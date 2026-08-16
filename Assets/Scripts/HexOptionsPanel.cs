using System.Text;
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
    [SerializeField] private Button closeActionButton;
    [SerializeField] private TMP_Text closeActionButtonText;
    [SerializeField] private RectTransform actionButtonContainer;

    private const float ActionButtonWidth = 298f;
    private const float ActionButtonHeight = 37f;

    private HexTile selectedHex;
    private Button foragerActionButton;
    private Button builderActionButton;
    private Button attackerActionButton;
    private TMP_Text foragerActionButtonText;
    private TMP_Text builderActionButtonText;
    private TMP_Text attackerActionButtonText;
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
        ConfigureInformationLayout();
        ConfigureActionLayout();
        HideCloseAction();
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
        selectedHex.OccupantsChanged += OnHexOccupantsChanged;
        selectedHex.StateChanged += OnHexStateChanged;
        if (selectedHex.CombatController != null)
            selectedHex.CombatController.ConflictChanged += OnConflictChanged;
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
            stateText.text = selectedHex.CombatController != null && selectedHex.CombatController.ConflictState != HexConflictState.None
                ? $"Status: {selectedHex.CombatController.ConflictState}"
                : $"Status: {selectedHex.State}";

        RefreshDetails();
        HideDispatchButtons();
        HideCloseAction();
        if (primaryActionButton == null)
            return;

        if (selectedHex.CombatController != null && selectedHex.CombatController.ConflictState != HexConflictState.None)
        {
            ConfigureConflictActions();
            return;
        }

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
                ConfigureEnemyHex();
                break;
            case HexTile.HexState.Locked:
                if (discoveryText != null)
                    discoveryText.text = "This territory is currently locked.";
                break;
        }

        // Every state shows a different number of buttons, so the container is measured once here
        // rather than in each branch.
        ResizeActionContainer();
    }

    private void RefreshDetails()
    {
        if (discoveryText == null)
            return;

        if (selectedHex.State == HexTile.HexState.Unknown)
        {
            string unknownDetails = string.IsNullOrEmpty(actionFeedback)
                ? "Contents: Unknown\nSend one Scout to survey this territory."
                : actionFeedback;
            discoveryText.text = $"{unknownDetails}\n\n{BuildFriendlyWaspDetails()}";
            return;
        }

        int foragerCount = selectedHex.GetFriendlyWaspCount(WaspFunction.Forager);
        discoveryText.text =
            $"{BuildResourceDetails(foragerCount)}\n\n{BuildFriendlyWaspDetails()}\n{BuildEnemyWaspDetails()}{BuildHiveHealthDetails()}";
    }

    private string BuildResourceDetails(int foragerCount)
    {
        StringBuilder details = new StringBuilder();
        StringBuilder names = new StringBuilder();
        StringBuilder production = new StringBuilder();

        AppendResource(
            selectedHex.HasPrey,
            "Prey",
            selectedHex.PreyRemaining,
            selectedHex.GetPreyGatherAmount(foragerCount),
            names,
            details,
            production);
        AppendResource(
            selectedHex.HasNectar,
            "Nectar",
            selectedHex.NectarRemaining,
            selectedHex.GetNectarGatherAmount(foragerCount),
            names,
            details,
            production);
        AppendResource(
            selectedHex.HasFibre,
            "Fibre",
            selectedHex.FibreRemaining,
            selectedHex.GetFibreGatherAmount(foragerCount),
            names,
            details,
            production);

        if (names.Length == 0)
            return "Resources: None";

        details.Insert(0, $"Resources: {names}\n");
        details.Append($"Foragers: {foragerCount}/{selectedHex.MaximumForagersPerHex}\n");
        details.Append($"Production / {selectedHex.GatheringTickIntervalSeconds:0.#} sec: {production}");
        return details.ToString();
    }

    private static void AppendResource(
        bool available,
        string resourceName,
        float remaining,
        float gathered,
        StringBuilder names,
        StringBuilder details,
        StringBuilder production)
    {
        if (!available)
            return;

        if (names.Length > 0)
            names.Append(" + ");
        if (production.Length > 0)
            production.Append("   ");

        names.Append(resourceName);
        details.Append($"{resourceName} remaining: {remaining:0}\n");
        production.Append($"{resourceName} +{gathered:0}");
    }

    private string BuildFriendlyWaspDetails()
    {
        return
            "Friendly wasps\n" +
            $"Scout: {selectedHex.GetFriendlyWaspCount(WaspFunction.Scout)}   " +
            $"Forager: {selectedHex.GetFriendlyWaspCount(WaspFunction.Forager)}   " +
            $"Builder: {selectedHex.GetFriendlyWaspCount(WaspFunction.Builder)}\n" +
            $"Brood: {selectedHex.GetFriendlyWaspCount(WaspFunction.BroodCaretaker)}   " +
            $"Guard: {selectedHex.GetFriendlyWaspCount(WaspFunction.Guard)}   " +
            $"Contain: {selectedHex.GetFriendlyWaspCount(WaspFunction.Containment)}";
    }

    private string BuildEnemyWaspDetails()
    {
        if (selectedHex.EnemyWaspCount <= 0)
            return string.Empty;

        StringBuilder details = new StringBuilder("\nEnemy wasps\n");
        AppendEnemyFactionDetails(details, WaspScopeRole.PrimaryInvasive, "Primary invasive");
        AppendEnemyFactionDetails(details, WaspScopeRole.SecondaryInvasive, "Secondary invasive");
        return details.ToString().TrimEnd();
    }

    private void AppendEnemyFactionDetails(StringBuilder details, WaspScopeRole faction, string label)
    {
        int scout = selectedHex.GetEnemyWaspCount(faction, WaspFunction.Scout);
        int forager = selectedHex.GetEnemyWaspCount(faction, WaspFunction.Forager);
        int builder = selectedHex.GetEnemyWaspCount(faction, WaspFunction.Builder);
        int guard = selectedHex.GetEnemyWaspCount(faction, WaspFunction.Guard);
        if (scout + forager + builder + guard <= 0)
            return;

        details.AppendLine($"{label}: Scout {scout}   Forager {forager}   Builder {builder}   Guard {guard}");
    }

    private string BuildHiveHealthDetails()
    {
        HiveCombatant friendly = selectedHex.FriendlyHive != null ? selectedHex.FriendlyHive.Combatant : null;
        HiveCombatant enemy = selectedHex.EnemyHive != null ? selectedHex.EnemyHive.Combatant : null;
        if (friendly != null)
            return $"\nFriendly hive: {friendly.CurrentHealth:0}/{friendly.MaximumHealth:0}";
        if (enemy != null && selectedHex.IsPlayerAccessible)
            return $"\nEnemy hive: {enemy.CurrentHealth:0}/{enemy.MaximumHealth:0}";
        return string.Empty;
    }

    private void ConfigureInformationLayout()
    {
        if (discoveryText == null)
            return;

        discoveryText.fontSize = 16f;
        discoveryText.lineSpacing = 1f;

        if (actionButtonContainer == null)
        {
            RectTransform rect = discoveryText.rectTransform;
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, 310f);
            discoveryText.lineSpacing = 2f;
        }
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
        SetDispatchAction(
            attackerActionButton,
            attackerActionButtonText,
            WaspFunction.Guard,
            "Attacker",
            hive);

        // Only now is it known how many buttons ended up visible for this hex.
        ResizeActionContainer();
    }

    private void ConfigureConflictActions()
    {
        HiveManagement hive = HiveManagement.GetOrCreate();
        int available = hive != null ? hive.GetAvailableWaspCount(WaspFunction.Guard) : 0;
        int assigned = hive != null ? hive.GetAssignedWaspCount(selectedHex, WaspFunction.Guard) : 0;
        int maximum = selectedHex.CombatController != null ? selectedHex.CombatController.MaximumAttackersPerSide : 20;
        bool canSend = hive != null && hive.CanDispatchToHex(selectedHex, WaspFunction.Guard);
        SetSingleAction(
            canSend ? $"Send Attacker\n{available} available  {assigned}/{maximum} sent" : $"No Attacker Available\n{assigned}/{maximum} sent",
            () => Dispatch(WaspFunction.Guard),
            canSend);

        if (selectedHex.HasFriendlyScout)
            ConfigureCloseAction("Recall Scout", RecallScout);
    }

    private void ConfigureEnemyHex()
    {
        HiveManagement hive = HiveManagement.GetOrCreate();
        int available = hive != null ? hive.GetAvailableWaspCount(WaspFunction.Guard) : 0;
        bool canSend = hive != null && hive.CanDispatchToHex(selectedHex, WaspFunction.Guard);
        SetSingleAction(canSend ? $"Send Attacker\n{available} available" : "No Attacker Available", () => Dispatch(WaspFunction.Guard), canSend);
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
        // One line rather than stacked. Two lines forced the autosizer down to about 10pt to fit the
        // button height; side by side it can use the full width and stay readable.
        if (text != null)
            text.text = $"{label}   {available} available";
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

    private void RecallScout()
    {
        bool recalled = HiveManagement.GetOrCreate()?.TryRecallScout(selectedHex) == true;
        actionFeedback = recalled ? "Scout returning to its home hive." : "Scout could not return to its home hive.";
        Close();
    }

    private void ConfigureCloseAction(string label, UnityEngine.Events.UnityAction action)
    {
        if (closeActionButton == null)
            return;

        closeActionButton.gameObject.SetActive(true);
        closeActionButton.onClick.RemoveAllListeners();
        closeActionButton.interactable = action != null;
        if (closeActionButtonText == null)
            closeActionButtonText = closeActionButton.GetComponentInChildren<TMP_Text>(true);
        if (closeActionButtonText != null)
            closeActionButtonText.text = label;
        if (action != null)
            closeActionButton.onClick.AddListener(action);
    }

    private void HideCloseAction()
    {
        if (closeActionButton == null)
            return;

        closeActionButton.onClick.RemoveAllListeners();
        closeActionButton.gameObject.SetActive(false);
    }

    private void EnsureDispatchButtons()
    {
        if (primaryActionButton == null ||
            (foragerActionButton != null && builderActionButton != null && attackerActionButton != null))
            return;

        primaryActionRect = primaryActionButton.GetComponent<RectTransform>();
        if (primaryActionRect == null)
            return;

        primaryDefaultPosition = primaryActionRect.anchoredPosition;
        primaryDefaultSize = primaryActionRect.sizeDelta;
        if (foragerActionButton == null)
            foragerActionButton = CreateActionClone("Send Forager Button", out foragerActionButtonText);
        if (builderActionButton == null)
            builderActionButton = CreateActionClone("Send Builder Button", out builderActionButtonText);
        if (attackerActionButton == null)
            attackerActionButton = CreateActionClone("Send Attacker Button", out attackerActionButtonText);
        HideDispatchButtons();
    }

    private Button CreateActionClone(string objectName, out TMP_Text text)
    {
        GameObject clone = Instantiate(primaryActionButton.gameObject, primaryActionButton.transform.parent);
        clone.name = objectName;
        Button button = clone.GetComponent<Button>();
        button.onClick.RemoveAllListeners();
        text = clone.GetComponentInChildren<TMP_Text>(true);

        if (closeActionButton != null && closeActionButton.transform.parent == clone.transform.parent)
            clone.transform.SetSiblingIndex(closeActionButton.transform.GetSiblingIndex());

        clone.SetActive(false);
        return button;
    }

    private void ConfigureDispatchLayout()
    {
        if (actionButtonContainer != null)
        {
            ConfigureActionButton(primaryActionButton);
            ConfigureActionButton(foragerActionButton);
            ConfigureActionButton(builderActionButton);
            ConfigureActionButton(attackerActionButton);
            ConfigureActionButton(closeActionButton);
            return;
        }

        RectTransform[] buttons =
        {
            primaryActionButton.GetComponent<RectTransform>(),
            foragerActionButton != null ? foragerActionButton.GetComponent<RectTransform>() : null,
            builderActionButton != null ? builderActionButton.GetComponent<RectTransform>() : null,
            attackerActionButton != null ? attackerActionButton.GetComponent<RectTransform>() : null
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
        if (actionButtonContainer != null)
        {
            ConfigureActionButton(primaryActionButton);
            ConfigureActionButton(closeActionButton);
            return;
        }

        if (primaryActionRect == null)
            return;

        primaryActionRect.anchoredPosition = primaryDefaultPosition;
        primaryActionRect.sizeDelta = primaryDefaultSize;
    }

    /// <summary>
    /// Grows the button container to fit whatever is actually showing.
    ///
    /// The three dispatch buttons are cloned in at runtime and shown per hex state, so any height
    /// authored in the editor is wrong the moment the panel opens - which is what crushed the buttons
    /// to a third of their size. Measuring the container's own layout group here is the only place
    /// that knows the real count.
    /// </summary>
    private void ResizeActionContainer()
    {
        if (actionButtonContainer == null)
            return;

        // Settle the children first. Measuring before they have been laid out reads the previous
        // hex's button count, which shows up as the container always being one refresh behind.
        LayoutRebuilder.ForceRebuildLayoutImmediate(actionButtonContainer);

        float preferred = LayoutUtility.GetPreferredHeight(actionButtonContainer);
        if (preferred <= 0f)
            return;

        actionButtonContainer.sizeDelta = new Vector2(actionButtonContainer.sizeDelta.x, preferred);
        GiveSlackToDetails(preferred);
        LayoutRebuilder.ForceRebuildLayoutImmediate(actionButtonContainer);
    }

    /// <summary>
    /// Hands whatever vertical space the buttons did not use to the detail text.
    ///
    /// The buttons stack from the top with everything else, so without this a hex with one button
    /// leaves a large hole beneath it and the text stays cramped. Growing the detail block instead
    /// pushes the buttons to the bottom of the card and gives the readout the room, whatever the
    /// button count for this hex turns out to be.
    /// </summary>
    private void GiveSlackToDetails(float buttonsHeight)
    {
        if (discoveryText == null || actionButtonContainer == null)
            return;

        RectTransform content = actionButtonContainer.parent as RectTransform;
        if (content == null)
            return;

        VerticalLayoutGroup group = content.GetComponent<VerticalLayoutGroup>();
        float spacing = group != null ? group.spacing : 0f;
        float padding = group != null ? group.padding.top + group.padding.bottom : 0f;

        float header = stateText != null ? stateText.rectTransform.rect.height : 0f;
        float slack = content.rect.height - padding - header - buttonsHeight - spacing * 2f;
        if (slack < 40f)
            return;

        RectTransform details = discoveryText.rectTransform;
        details.sizeDelta = new Vector2(details.sizeDelta.x, slack);
    }

    private void HideDispatchButtons()
    {
        if (foragerActionButton != null)
            foragerActionButton.gameObject.SetActive(false);
        if (builderActionButton != null)
            builderActionButton.gameObject.SetActive(false);
        if (attackerActionButton != null)
            attackerActionButton.gameObject.SetActive(false);
    }

    private void ConfigureActionLayout()
    {
        if (actionButtonContainer == null)
            return;

        HorizontalLayoutGroup layout = actionButtonContainer.GetComponent<HorizontalLayoutGroup>();
        if (layout != null)
        {
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 8f;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
        }

        ConfigureActionButton(primaryActionButton);
        ConfigureActionButton(foragerActionButton);
        ConfigureActionButton(builderActionButton);
        ConfigureActionButton(attackerActionButton);
        ConfigureActionButton(closeActionButton);
    }

    /// <summary>
    /// Sizes an action button for the stacked layout.
    ///
    /// This used to force 176x90 at half scale, which is where the squashed buttons came from: it runs
    /// on every open and overwrote whatever the scene had, so the panel could never be laid out in the
    /// editor. Full scale now, with a LayoutElement so the containing vertical group measures the
    /// button properly and grows to fit however many are showing.
    /// </summary>
    private static void ConfigureActionButton(Button button)
    {
        if (button == null)
            return;

        RectTransform rect = button.GetComponent<RectTransform>();
        if (rect == null)
            return;

        rect.sizeDelta = new Vector2(ActionButtonWidth, ActionButtonHeight);
        rect.localScale = Vector3.one;

        LayoutElement element = button.GetComponent<LayoutElement>();
        if (element == null)
            element = button.gameObject.AddComponent<LayoutElement>();

        element.preferredWidth = ActionButtonWidth;
        element.preferredHeight = ActionButtonHeight;
        element.flexibleHeight = 0f;
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
        {
            selectedHex.ResourcesChanged -= OnHexResourcesChanged;
            selectedHex.OccupantsChanged -= OnHexOccupantsChanged;
            selectedHex.StateChanged -= OnHexStateChanged;
            if (selectedHex.CombatController != null)
                selectedHex.CombatController.ConflictChanged -= OnConflictChanged;
        }
    }

    private void OnHexResourcesChanged(HexTile hex)
    {
        RefreshPanel();
    }

    private void OnHexOccupantsChanged(HexTile hex)
    {
        RefreshPanel();
    }

    private void OnHexStateChanged(HexTile hex)
    {
        RefreshPanel();
    }

    private void OnConflictChanged(HexCombatController controller)
    {
        RefreshPanel();
    }
}
