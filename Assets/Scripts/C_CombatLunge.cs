using UnityEngine;

/// <summary>
/// Throws a wasp at whatever it is attacking and pulls it back, so a fight reads as a fight.
///
/// Combat is resolved entirely in numbers - cooldowns, damage, defence - and until now nothing on
/// screen moved while those numbers ran. Two stacks of wasps would hover motionless and one would
/// quietly vanish. This does not change the maths at all; it only makes each resolved strike visible,
/// so the player can see who is trading blows and roughly how fast.
///
/// The lunge is applied to a child transform's <see cref="Transform.localPosition"/> rather than to
/// the wasp root. The root's world position is owned by the navigation code, which rewrites it every
/// frame - anything written there would be stamped over instantly. A local offset on a child rides
/// along with the root wherever it goes and cannot be fought over.
/// </summary>
[DisallowMultipleComponent]
public class C_CombatLunge : MonoBehaviour
{
    [SerializeField, Tooltip("The visual to throw. Left empty, the first child is used, then this object.")]
    private Transform visual;
    [SerializeField, Min(0.01f), Tooltip("How far to travel toward the target, in world units.")]
    private float lungeDistance = 0.42f;
    [SerializeField, Min(0.05f), Tooltip("Seconds for the full out-and-back.")]
    private float lungeDuration = 0.28f;
    [SerializeField, Range(0f, 0.9f), Tooltip("Share of the duration spent striking out. The rest is the recovery, which reads better when it is the slower half.")]
    private float strikeShare = 0.35f;

    private Vector3 restPosition;
    private Vector3 lungeOffset;
    private float elapsed;
    private bool lunging;

    private void Awake()
    {
        if (visual == null)
            visual = transform.childCount > 0 ? transform.GetChild(0) : transform;

        restPosition = visual.localPosition;
    }

    /// <summary>
    /// Starts a strike toward a world position. Safe to call every time an attack lands - a strike
    /// already in flight is retargeted rather than restarted, so a fast attack rate reads as a flurry
    /// instead of a stutter.
    /// </summary>
    public void Strike(Vector3 targetWorldPosition)
    {
        if (visual == null)
            return;

        Vector3 toTarget = targetWorldPosition - visual.position;
        toTarget.y *= 0.35f;      // Keep the lunge mostly horizontal, so wasps do not dive at the floor.
        if (toTarget.sqrMagnitude < 0.0001f)
            return;

        // Convert to the space localPosition actually lives in, so the offset is correct whatever the
        // parent's rotation and scale happen to be.
        Vector3 worldOffset = toTarget.normalized * lungeDistance;
        Transform parent = visual.parent;
        lungeOffset = parent != null ? parent.InverseTransformVector(worldOffset) : worldOffset;

        if (!lunging)
            restPosition = visual.localPosition;

        elapsed = 0f;
        lunging = true;
    }

    private void LateUpdate()
    {
        if (!lunging || visual == null)
            return;

        // Unscaled, so strikes still animate while the game is paused mid-fight.
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / lungeDuration);

        float reach;
        if (t < strikeShare)
        {
            // Out fast and linear - a strike should snap rather than ease.
            reach = t / strikeShare;
        }
        else
        {
            // Back slower and eased, which is what sells it as recoil rather than a bounce.
            float back = (t - strikeShare) / (1f - strikeShare);
            reach = 1f - Mathf.SmoothStep(0f, 1f, back);
        }

        visual.localPosition = restPosition + lungeOffset * reach;

        if (t >= 1f)
        {
            visual.localPosition = restPosition;
            lunging = false;
        }
    }

    private void OnDisable()
    {
        // Never leave a wasp parked mid-lunge if it is pooled or hidden part way through a strike.
        if (visual != null && lunging)
            visual.localPosition = restPosition;

        lunging = false;
    }

    /// <summary>Finds or adds the lunge on a combatant, so nothing has to be authored per prefab.</summary>
    public static C_CombatLunge Attach(Component owner)
    {
        if (owner == null)
            return null;

        C_CombatLunge existing = owner.GetComponent<C_CombatLunge>();
        return existing != null ? existing : owner.gameObject.AddComponent<C_CombatLunge>();
    }
}
