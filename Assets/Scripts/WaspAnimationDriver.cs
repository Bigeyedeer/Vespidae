using UnityEngine;

/// <summary>
/// Drives a wasp's Animator from its travel state. Works for both the player's wasps
/// (<see cref="WaspControl"/>) and the invasive ones (<see cref="EnemyWaspControl"/>).
///
/// The graph only uses two states: the grounded blend tree (idle) and InAir (flying), switched by
/// the <c>Grounded</c> bool. Travelling to another hex flies; anything else idles.
///
/// <c>Speed</c> is deliberately held at 0 so the grounded blend tree always resolves to the idle
/// clip — the walk entries in that tree are not used by the RTS layer.
/// </summary>
[DisallowMultipleComponent]
public class WaspAnimationDriver : MonoBehaviour
{
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int GroundedHash = Animator.StringToHash("Grounded");
    private static readonly int MotionSpeedHash = Animator.StringToHash("MotionSpeed");

    [SerializeField] private Animator animator;
    [SerializeField, Tooltip("Optional. Found automatically. The player-side controller, if this is a friendly wasp.")]
    private WaspControl friendlyControl;
    [SerializeField, Tooltip("Optional. Found automatically. The invasive controller, if this is an enemy wasp.")]
    private EnemyWaspControl enemyControl;

    [Header("Grounded Blend Tree")]
    [SerializeField, Tooltip("Speed fed to the grounded blend tree. Left at 0 so it always plays the idle clip.")]
    private float groundedBlendSpeed;

    private void Awake()
    {
        Resolve();
    }

    private void OnEnable()
    {
        Resolve();
        Apply();
    }

    private void Resolve()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);
        if (friendlyControl == null)
            friendlyControl = GetComponent<WaspControl>();
        if (enemyControl == null)
            enemyControl = GetComponent<EnemyWaspControl>();
    }

    private void Update()
    {
        Apply();
    }

    private void Apply()
    {
        if (animator == null || animator.runtimeAnimatorController == null)
            return;

        animator.SetFloat(SpeedHash, groundedBlendSpeed);
        animator.SetFloat(MotionSpeedHash, 1f);
        // Grounded false drops the graph into InAir (the flight clip); true returns it to the
        // grounded blend tree, which idles because Speed stays at 0.
        animator.SetBool(GroundedHash, !IsTravelling());
    }

    /// <summary>True while this wasp is on its way to another hex.</summary>
    private bool IsTravelling()
    {
        if (friendlyControl != null)
            return friendlyControl.WorkforceState == WaspWorkforceState.Travelling;

        if (enemyControl != null)
            return enemyControl.WorkforceState == WaspWorkforceState.Travelling;

        return false;
    }
}
