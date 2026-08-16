using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[Serializable]
public class WaspControlGroupHudBinding
{
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text label;

    public Button Button => button;
    public TMP_Text Label => label;

    public void Configure(Button targetButton, TMP_Text targetLabel)
    {
        button = targetButton;
        label = targetLabel;
    }
}

[DefaultExecutionOrder(-100)]
public class WaspControlGroupManager : MonoBehaviour
{
    [SerializeField] private C_MainWorldCameraFocus cameraFocus;
    [SerializeField] private C_TutorialManager tutorialManager;
    [SerializeField] private RectTransform selectionCanvas;
    [SerializeField] private RectTransform selectionBox;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private WaspControlGroupHudBinding[] groupBindings = new WaspControlGroupHudBinding[5];
    [SerializeField, Tooltip("Only show a control group icon once that group has been created. Turn off " +
                             "to keep all five slots on screen at all times.")]
    private bool hideEmptyGroupSlots = true;
    [SerializeField, Range(1, 40)] private int maximumSelection = 20;
    [SerializeField, Min(2f)] private float dragThreshold = 10f;
    [SerializeField, Min(1f)] private float selectionRayDistance = 1000f;
    [SerializeField, Min(0.05f), Tooltip("Maximum gap between two right-clicks for them to count as a double-click deselect.")]
    private float doubleClickWindow = 0.3f;

    private readonly List<WaspControl> currentSelection = new List<WaspControl>();
    private readonly List<WaspControl>[] groups =
    {
        new List<WaspControl>(),
        new List<WaspControl>(),
        new List<WaspControl>(),
        new List<WaspControl>(),
        new List<WaspControl>()
    };

    private Vector2 dragStart;
    private bool pointerDown;
    private bool dragging;
    private int suppressClickFrame = -1;
    private int suppressOrderFrame = -1;
    private float lastRightClickTime = -10f;
    private int activeGroup = -1;

    public bool HasSelection => currentSelection.Count > 0;
    public bool IsDragging => dragging;
    public bool ShouldSuppressWorldClickThisFrame => suppressClickFrame == Time.frameCount;
    /// <summary>
    /// True on the frame a double right-click cleared the selection, so that second click is not
    /// also read as a move order.
    /// </summary>
    public bool ShouldSuppressOrderThisFrame => suppressOrderFrame == Time.frameCount;
    public IReadOnlyList<WaspControl> CurrentSelection => currentSelection;

    private void Awake()
    {
        if (cameraFocus == null)
            cameraFocus = FindFirstObjectByType<C_MainWorldCameraFocus>();

        if (tutorialManager == null)
            tutorialManager = FindFirstObjectByType<C_TutorialManager>();

        if (selectionCanvas == null && selectionBox != null)
            selectionCanvas = selectionBox.root as RectTransform;

        if (selectionBox != null)
            selectionBox.gameObject.SetActive(false);

        BindGroupButtons();
        RefreshHud();
    }

    private void Update()
    {
        CleanupSelections();
        RefreshHud();

        if ((C_MainWorldOverlayNavigation.Instance != null && C_MainWorldOverlayNavigation.Instance.BlocksWorldInput) ||
            (tutorialManager != null && tutorialManager.TutorialActive))
        {
            CancelDrag();
            return;
        }

        HandleGroupKeys();
        HandleDoubleRightClickDeselect();
        HandleDragSelection();
    }

    /// <summary>
    /// Double right-click clears the current selection. Runs before the hex raycaster (execution
    /// order -100) so it can flag the frame and stop the second click issuing a move order.
    /// </summary>
    private void HandleDoubleRightClickDeselect()
    {
        if (Mouse.current == null || !Mouse.current.rightButton.wasPressedThisFrame)
            return;

        float now = Time.unscaledTime;
        bool isSecondClick = now - lastRightClickTime <= doubleClickWindow;
        // Reset rather than keep the stamp, so a triple click is not read as two overlapping pairs.
        lastRightClickTime = isSecondClick ? -10f : now;

        if (!isSecondClick || IsPointerOverUi())
            return;

        suppressOrderFrame = Time.frameCount;
        ClearSelection();
    }

    public void ClearSelection()
    {
        bool had = currentSelection.Count > 0;
        ApplySelection(null);
        activeGroup = -1;

        if (feedbackText != null)
            feedbackText.text = had ? "Selection cleared." : "No wasps selected.";

        RefreshHud();
    }

    public WaspMoveOrderResult TryMoveSelectedToHex(HexTile target)
    {
        CleanupSelections();
        HiveManagement hive = HiveManagement.GetOrCreate();
        WaspMoveOrderResult result = hive != null
            ? hive.TryMoveWasps(currentSelection, target)
            : new WaspMoveOrderResult(currentSelection.Count, 0, currentSelection.Count, 0);

        if (feedbackText != null)
        {
            if (result.AnyMoved)
            {
                bool scouting = target.State == HexTile.HexState.Unknown && SelectionContains(WaspFunction.Scout);
                feedbackText.text = scouting
                    ? $"Moved {result.Moved}/{result.Requested} wasps to {target.HexName}. Scouting will begin on arrival."
                    : $"Moved {result.Moved}/{result.Requested} wasps to {target.HexName}.";
            }
            else if (result.Capped > 0)
                feedbackText.text = $"{target.HexName} is already at capacity for those roles.";
            else
                feedbackText.text = $"The selected wasps cannot move to {target.HexName}.";
        }

        return result;
    }

    public void AssignGroup(int index)
    {
        if (index < 0 || index >= groups.Length)
            return;

        CleanupSelections();
        groups[index].Clear();
        for (int member = 0; member < currentSelection.Count && groups[index].Count < maximumSelection; member++)
        {
            WaspControl wasp = currentSelection[member];
            if (IsSelectable(wasp))
                groups[index].Add(wasp);
        }

        activeGroup = index;
        if (feedbackText != null)
            feedbackText.text = groups[index].Count == 0
                ? $"Group {index + 1} cleared."
                : $"Group {index + 1} assigned with {groups[index].Count} wasps.";
        RefreshHud();
    }

    public void SelectGroup(int index)
    {
        if (index < 0 || index >= groups.Length)
            return;

        groups[index].RemoveAll(wasp => !IsSelectable(wasp));
        ApplySelection(groups[index]);
        activeGroup = index;
        if (feedbackText != null)
            feedbackText.text = groups[index].Count == 0
                ? $"Group {index + 1} is empty."
                : $"Group {index + 1} selected.";
        RefreshHud();
    }

    private void HandleGroupKeys()
    {
        if (Keyboard.current == null)
            return;

        bool control = Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed;
        for (int index = 0; index < groups.Length; index++)
        {
            if (!WasGroupKeyPressed(index))
                continue;

            if (control)
                AssignGroup(index);
            else
                SelectGroup(index);
            break;
        }
    }

    private void HandleDragSelection()
    {
        if (Mouse.current == null)
            return;

        Vector2 pointer = Mouse.current.position.ReadValue();
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (IsPointerOverUi())
                return;

            dragStart = pointer;
            pointerDown = true;
            dragging = false;
        }

        if (pointerDown && Mouse.current.leftButton.isPressed)
        {
            if (!dragging && Vector2.Distance(dragStart, pointer) >= dragThreshold)
            {
                dragging = true;
                if (selectionBox != null)
                    selectionBox.gameObject.SetActive(true);
            }

            if (dragging)
                UpdateSelectionBox(dragStart, pointer);
        }

        if (!pointerDown || !Mouse.current.leftButton.wasReleasedThisFrame)
            return;

        if (dragging)
        {
            SelectWithinScreenRect(BuildScreenRect(dragStart, pointer), IsAdditiveModifierHeld());
            suppressClickFrame = Time.frameCount;
        }
        else if (TryClickSelect(pointer, IsAdditiveModifierHeld()))
        {
            // Only swallow the click for a shift-click that actually hit a wasp. Plain clicks
            // are never swallowed, so they keep opening the wasp/hex panel as before.
            suppressClickFrame = Time.frameCount;
        }

        pointerDown = false;
        dragging = false;
        if (selectionBox != null)
            selectionBox.gameObject.SetActive(false);
    }

    private void SelectWithinScreenRect(Rect screenRect, bool additive)
    {
        Camera activeCamera = cameraFocus != null ? cameraFocus.ActiveCamera : Camera.main;
        HiveManagement hive = HiveManagement.GetOrCreate();
        if (activeCamera == null || hive == null)
            return;

        Vector2 centre = screenRect.center;
        List<(WaspControl wasp, float distance)> candidates = new List<(WaspControl, float)>();
        foreach (WaspControl wasp in hive.FriendlyWasps)
        {
            if (!IsSelectable(wasp))
                continue;

            Vector3 screenPosition = activeCamera.WorldToScreenPoint(wasp.transform.position);
            if (screenPosition.z <= 0f || !screenRect.Contains(new Vector2(screenPosition.x, screenPosition.y)))
                continue;

            candidates.Add((wasp, Vector2.SqrMagnitude(new Vector2(screenPosition.x, screenPosition.y) - centre)));
        }

        candidates.Sort((left, right) => left.distance.CompareTo(right.distance));

        List<WaspControl> selected = additive ? new List<WaspControl>(currentSelection) : new List<WaspControl>();
        for (int index = 0; index < candidates.Count && selected.Count < maximumSelection; index++)
        {
            WaspControl wasp = candidates[index].wasp;
            if (!selected.Contains(wasp))
                selected.Add(wasp);
        }

        ApplySelection(selected);
        activeGroup = -1;
        ReportSelection();
        RefreshHud();
    }

    /// <summary>
    /// Shift-click toggles the wasp under the cursor in or out of the selection. A plain click is
    /// deliberately ignored here so it keeps its original behaviour (opening the wasp/hex panel)
    /// and falls through to the hex raycaster untouched.
    /// </summary>
    private bool TryClickSelect(Vector2 pointer, bool additive)
    {
        if (!additive || IsPointerOverUi())
            return false;

        Camera activeCamera = cameraFocus != null ? cameraFocus.ActiveCamera : Camera.main;
        if (activeCamera == null)
            return false;

        Ray ray = activeCamera.ScreenPointToRay(pointer);
        RaycastHit[] hits = Physics.RaycastAll(ray, selectionRayDistance, ~0, QueryTriggerInteraction.Collide);
        Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));

        WaspControl clicked = null;
        foreach (RaycastHit hit in hits)
        {
            WaspControl candidate = hit.collider.GetComponentInParent<WaspControl>();
            if (IsSelectable(candidate))
            {
                clicked = candidate;
                break;
            }
        }

        if (clicked == null)
            return false;

        List<WaspControl> selected = new List<WaspControl>(currentSelection);
        if (!selected.Remove(clicked))
            selected.Add(clicked);

        ApplySelection(selected);
        activeGroup = -1;
        ReportSelection();
        RefreshHud();
        return true;
    }

    private bool SelectionContains(WaspFunction function)
    {
        foreach (WaspControl wasp in currentSelection)
        {
            if (wasp != null && wasp.AssignedFunction == function)
                return true;
        }

        return false;
    }

    private void ReportSelection()
    {
        if (feedbackText == null)
            return;

        feedbackText.text = currentSelection.Count == 0
            ? "No wasps selected."
            : $"{currentSelection.Count} {DescribeSelection()} selected.";
    }

    /// <summary>
    /// Names the selection by role when it is all one role, otherwise calls it a mixed group.
    /// </summary>
    private string DescribeSelection()
    {
        if (currentSelection.Count == 0)
            return "wasps";

        WaspFunction first = currentSelection[0].AssignedFunction;
        foreach (WaspControl wasp in currentSelection)
        {
            if (wasp.AssignedFunction != first)
                return "wasps";
        }

        return currentSelection.Count == 1 ? first.ToString() : $"{first}s";
    }

    private static bool IsAdditiveModifierHeld()
    {
        return Keyboard.current != null &&
               (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);
    }

    private void ApplySelection(IReadOnlyList<WaspControl> wasps)
    {
        foreach (WaspControl selected in currentSelection)
            selected?.SetSelected(false);

        currentSelection.Clear();
        if (wasps != null)
        {
            for (int index = 0; index < wasps.Count && currentSelection.Count < maximumSelection; index++)
            {
                WaspControl wasp = wasps[index];
                if (IsSelectable(wasp) && !currentSelection.Contains(wasp))
                    currentSelection.Add(wasp);
            }
        }

        foreach (WaspControl selected in currentSelection)
            selected.SetSelected(true);
    }

    private void UpdateSelectionBox(Vector2 start, Vector2 end)
    {
        if (selectionBox == null || selectionCanvas == null)
            return;

        Canvas canvas = selectionCanvas.GetComponentInParent<Canvas>();
        Camera canvasCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(selectionCanvas, start, canvasCamera, out Vector2 localStart);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(selectionCanvas, end, canvasCamera, out Vector2 localEnd);
        selectionBox.anchoredPosition = (localStart + localEnd) * 0.5f;
        selectionBox.sizeDelta = new Vector2(Mathf.Abs(localEnd.x - localStart.x), Mathf.Abs(localEnd.y - localStart.y));
    }

    private void BindGroupButtons()
    {
        if (groupBindings == null)
            return;

        for (int index = 0; index < groupBindings.Length; index++)
        {
            WaspControlGroupHudBinding binding = groupBindings[index];
            if (binding?.Button == null)
                continue;

            int capturedIndex = index;
            binding.Button.onClick.RemoveAllListeners();
            binding.Button.onClick.AddListener(() => SelectGroup(capturedIndex));
        }
    }

    private void RefreshHud()
    {
        if (groupBindings == null)
            return;

        for (int index = 0; index < groupBindings.Length && index < groups.Length; index++)
        {
            groups[index].RemoveAll(wasp => !IsSelectable(wasp));
            WaspControlGroupHudBinding binding = groupBindings[index];
            if (binding == null)
                continue;
            if (binding.Label != null)
            {
                binding.Label.text = $"{index + 1}\n{groups[index].Count}";
                binding.Label.color = activeGroup == index
                    ? new Color(1f, 0.76f, 0.05f, 1f)
                    : Color.white;
            }
            if (binding.Button != null)
            {
                bool exists = groups[index].Count > 0;
                binding.Button.interactable = exists;

                // Show a slot only once its group exists. Five permanent empty icons read as broken
                // UI; one appearing per group the player actually makes reads as feedback.
                if (hideEmptyGroupSlots && binding.Button.gameObject.activeSelf != exists)
                    binding.Button.gameObject.SetActive(exists);
            }
        }
    }

    private void CleanupSelections()
    {
        currentSelection.RemoveAll(wasp => !IsSelectable(wasp));
        foreach (List<WaspControl> group in groups)
            group.RemoveAll(wasp => !IsSelectable(wasp));
    }

    private void CancelDrag()
    {
        pointerDown = false;
        dragging = false;
        if (selectionBox != null)
            selectionBox.gameObject.SetActive(false);
    }

    private static Rect BuildScreenRect(Vector2 first, Vector2 second)
    {
        return Rect.MinMaxRect(
            Mathf.Min(first.x, second.x),
            Mathf.Min(first.y, second.y),
            Mathf.Max(first.x, second.x),
            Mathf.Max(first.y, second.y));
    }

    private static bool IsSelectable(WaspControl wasp)
    {
        return wasp != null && wasp.IsAlive;
    }

    private static bool IsPointerOverUi()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    private static bool WasGroupKeyPressed(int index)
    {
        switch (index)
        {
            case 0: return Keyboard.current.digit1Key.wasPressedThisFrame;
            case 1: return Keyboard.current.digit2Key.wasPressedThisFrame;
            case 2: return Keyboard.current.digit3Key.wasPressedThisFrame;
            case 3: return Keyboard.current.digit4Key.wasPressedThisFrame;
            case 4: return Keyboard.current.digit5Key.wasPressedThisFrame;
            default: return false;
        }
    }

    private void OnDestroy()
    {
        foreach (WaspControl wasp in currentSelection)
            wasp?.SetSelected(false);
    }
}
