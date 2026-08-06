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
    [SerializeField, Range(1, 5)] private int maximumSelection = 5;
    [SerializeField, Min(2f)] private float dragThreshold = 10f;

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
    private int activeGroup = -1;

    public bool HasSelection => currentSelection.Count > 0;
    public bool IsDragging => dragging;
    public bool ShouldSuppressWorldClickThisFrame => suppressClickFrame == Time.frameCount;
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
        HandleDragSelection();
    }

    public WaspMoveOrderResult TryMoveSelectedToHex(HexTile target)
    {
        CleanupSelections();
        HiveManagement hive = HiveManagement.GetOrCreate();
        WaspMoveOrderResult result = hive != null
            ? hive.TryMoveAttackers(currentSelection, target)
            : new WaspMoveOrderResult(currentSelection.Count, 0, currentSelection.Count, 0);

        if (feedbackText != null)
        {
            if (result.AnyMoved)
                feedbackText.text = $"Moved {result.Moved}/{result.Requested} attackers to {target.HexName}.";
            else if (result.Capped > 0)
                feedbackText.text = "The target already has five assigned attackers.";
            else
                feedbackText.text = "The selected attackers cannot move to that hex.";
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
                : $"Group {index + 1} assigned with {groups[index].Count} attackers.";
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
            SelectWithinScreenRect(BuildScreenRect(dragStart, pointer));
            suppressClickFrame = Time.frameCount;
        }

        pointerDown = false;
        dragging = false;
        if (selectionBox != null)
            selectionBox.gameObject.SetActive(false);
    }

    private void SelectWithinScreenRect(Rect screenRect)
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
        List<WaspControl> selected = new List<WaspControl>();
        for (int index = 0; index < candidates.Count && selected.Count < maximumSelection; index++)
            selected.Add(candidates[index].wasp);

        ApplySelection(selected);
        activeGroup = -1;
        if (feedbackText != null)
            feedbackText.text = selected.Count == 0 ? "No attackers selected." : $"{selected.Count} attackers selected.";
        RefreshHud();
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
                binding.Button.interactable = groups[index].Count > 0;
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
        return wasp != null && wasp.IsAlive && wasp.AssignedFunction == WaspFunction.Guard;
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
