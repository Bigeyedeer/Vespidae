using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Opens and closes the Game Goal panel.
///
/// Every visual part of this - the button, the card, the heading, the body copy, the back button -
/// is authored in the scene and assigned below. This script only decides when the panel is shown, so
/// the layout, wording, colours and fonts can all be changed in the Editor without touching code.
/// </summary>
public class C_GameGoalPanel : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField, Tooltip("The whole Game Goal overlay, including its backdrop.")]
    private GameObject panelRoot;

    [Header("Buttons")]
    [SerializeField, Tooltip("The menu button that opens the panel.")]
    private Button openButton;
    [SerializeField, Tooltip("The button inside the panel that closes it.")]
    private Button closeButton;

    [Header("Behaviour")]
    [SerializeField, Tooltip("Close the panel when Escape is pressed.")]
    private bool closeOnEscape = true;

    public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

    private void Awake()
    {
        Bind(openButton, Open);
        Bind(closeButton, Close);
        Close();
    }

    /// <summary>
    /// Binds the action to this button and any Button nested inside it.
    ///
    /// The menu button skin carries its own Button on a child, and that child sits on top in the
    /// raycast order - a listener on the outer object alone would never fire.
    /// </summary>
    private static void Bind(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
            return;

        foreach (Button nested in button.GetComponentsInChildren<Button>(true))
        {
            nested.onClick.RemoveListener(action);
            nested.onClick.AddListener(action);
        }
    }

    public void Open()
    {
        if (panelRoot != null)
            panelRoot.SetActive(true);
    }

    public void Close()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    public void Toggle()
    {
        if (IsOpen)
            Close();
        else
            Open();
    }

    private void Update()
    {
        // Read through the Input System package, which this project has active - the old
        // UnityEngine.Input class throws outright under that setting.
        if (closeOnEscape && IsOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            Close();
    }
}
