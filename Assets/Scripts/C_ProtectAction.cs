using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives the Protect button on the action bar.
///
/// Pressing it recalls every attacker to its home hive. That is the panic button: when invasives are
/// pushing into the colony the player should be able to pull the whole force back in one press rather
/// than re-ordering each group off its target.
///
/// The hive's own hex picks the returning wasps up as defenders automatically - the combat controller
/// counts an attacker sitting at home with no posting as part of that tile's garrison - so nothing has
/// to be registered by hand here.
/// </summary>
public class C_ProtectAction : MonoBehaviour
{
    [SerializeField, Tooltip("Optional. Found by name if left empty.")]
    private Button protectButton;
    [SerializeField, Tooltip("Optional. Brief confirmation shown after a recall.")]
    private TMP_Text feedbackText;
    [SerializeField, Min(0f), Tooltip("Seconds the confirmation stays on screen.")]
    private float feedbackSeconds = 2.5f;

    private float feedbackRemaining;

    private void Awake()
    {
        Resolve();
        Bind();
    }

    private void OnEnable()
    {
        Resolve();
        Bind();
    }

    private void Resolve()
    {
        if (protectButton != null)
            return;

        GameObject found = GameObject.Find("Action_Protect");
        if (found == null)
            return;

        protectButton = found.GetComponent<Button>();
        if (protectButton == null)
            protectButton = found.GetComponentInChildren<Button>(true);
    }

    private void Bind()
    {
        if (protectButton == null)
            return;

        protectButton.onClick.RemoveListener(Protect);
        protectButton.onClick.AddListener(Protect);
    }

    /// <summary>Recalls every attacker home. Safe to call with nothing to recall.</summary>
    public void Protect()
    {
        HiveManagement hive = HiveManagement.Instance;
        if (hive == null)
            return;

        int recalled = hive.RecallAttackersToDefend();
        // Only the recall itself is ours; button feedback is handled elsewhere.
        if (recalled > 0)
            AudioDirector.Play(GameSound.WaspRecalled);
        ShowFeedback(recalled > 0
            ? $"{recalled} attacker{(recalled == 1 ? string.Empty : "s")} recalled to defend"
            : "No attackers to recall");
    }

    private void ShowFeedback(string message)
    {
        if (feedbackText == null)
            return;

        feedbackText.text = message;
        feedbackText.gameObject.SetActive(true);
        feedbackRemaining = feedbackSeconds;
    }

    private void Update()
    {
        if (feedbackRemaining <= 0f || feedbackText == null)
            return;

        feedbackRemaining -= Time.deltaTime;
        if (feedbackRemaining <= 0f)
            feedbackText.gameObject.SetActive(false);
    }
}
