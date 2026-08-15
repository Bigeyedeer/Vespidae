using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Live 3D specimen viewer for the identification panel.
///
/// Follows the same pattern as the tutorial Pip portrait already used in this project: a staging
/// rig parked away from the map, a camera that renders only the specimen layer into a RenderTexture,
/// and a RawImage in the UI showing that texture. Dragging on the image spins the specimen so the
/// player can inspect antennae, legs and markings - the evidence the Codex asks them to compare.
/// </summary>
public class C_SpecimenViewport : MonoBehaviour, IDragHandler, IBeginDragHandler, IScrollHandler
{
    [Header("Stage")]
    [SerializeField, Tooltip("Empty parent the specimen is spawned under, parked away from the map.")]
    private Transform stageRoot;
    [SerializeField, Tooltip("Camera that renders the stage into the render texture.")]
    private Camera stageCamera;
    [SerializeField, Tooltip("Layer the specimen and stage camera use, so nothing else leaks in.")]
    private int specimenLayer = 11;

    [Header("Framing")]
    [SerializeField, Min(0.1f), Tooltip("How much of the view the specimen fills. Lower fills more of the card.")]
    private float framingMargin = 1.35f;
    [SerializeField] private Vector3 specimenEuler = new Vector3(0f, 150f, 0f);

    [Header("Zoom")]
    [SerializeField, Min(0.01f), Tooltip("How much one scroll notch changes the zoom.")]
    private float zoomStep = 0.12f;
    [SerializeField, Min(0.1f), Tooltip("Most zoomed out, as a multiple of the default framing.")]
    private float minZoom = 0.6f;
    [SerializeField, Min(1f), Tooltip("Most zoomed in, as a multiple of the default framing.")]
    private float maxZoom = 4f;

    [Header("Rotation")]
    [SerializeField, Min(0.05f)] private float dragSensitivity = 0.35f;
    [SerializeField, Tooltip("Idle spin when the player is not dragging.")]
    private float autoSpinSpeed = 12f;

    private GameObject currentSpecimen;
    private float yaw;
    private bool userHasDragged;
    private float zoom = 1f;
    private Vector3 framedCentre;
    private float framedDistance;
    private bool framePending;

    /// <summary>Spawns a display-only copy of this wasp's model onto the stage.</summary>
    public void ShowSpecimen(WaspInfo wasp)
    {
        ClearSpecimen();
        if (wasp == null || stageRoot == null)
            return;

        // Copy the whole visual hierarchy, then strip everything that could act on its own.
        currentSpecimen = Instantiate(wasp.gameObject, stageRoot);
        currentSpecimen.name = "Specimen";
        StripBehaviours(currentSpecimen);
        SetLayerRecursively(currentSpecimen, specimenLayer);

        currentSpecimen.transform.localPosition = Vector3.zero;
        yaw = specimenEuler.y;
        userHasDragged = false;
        zoom = 1f;   // every new specimen starts at the default framing
        currentSpecimen.transform.localRotation = Quaternion.Euler(specimenEuler);

        // The stage sits far off the map where the main camera never looks, so Unity would not
        // refresh skinned bounds and framing would use stale, inflated ones - which renders the
        // specimen tiny and off-centre. Force real bounds, then frame once they are valid.
        foreach (var smr in currentSpecimen.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            smr.updateWhenOffscreen = true;

        framePending = true;
        FrameSpecimen();
    }

    public void ClearSpecimen()
    {
        if (currentSpecimen == null)
            return;

        Destroy(currentSpecimen);
        currentSpecimen = null;
    }

    private void LateUpdate()
    {
        // Re-frame a frame after spawning, once the animator has posed the rig and the skinned
        // bounds are real rather than the bind-pose estimate.
        if (framePending && currentSpecimen != null)
        {
            framePending = false;
            FrameSpecimen();
        }
    }

    private void Update()
    {
        if (currentSpecimen == null || userHasDragged || autoSpinSpeed == 0f)
            return;

        yaw += autoSpinSpeed * Time.unscaledDeltaTime;
        currentSpecimen.transform.localRotation = Quaternion.Euler(specimenEuler.x, yaw, specimenEuler.z);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        userHasDragged = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (currentSpecimen == null)
            return;

        yaw -= eventData.delta.x * dragSensitivity;
        currentSpecimen.transform.localRotation = Quaternion.Euler(specimenEuler.x, yaw, specimenEuler.z);
    }

    /// <summary>
    /// Pulls the camera back far enough to fit the specimen, so different species and scales all
    /// present at a sensible size rather than needing per-prefab tuning.
    /// </summary>
    private void FrameSpecimen()
    {
        if (stageCamera == null || currentSpecimen == null)
            return;

        Bounds bounds = new Bounds(currentSpecimen.transform.position, Vector3.zero);
        bool any = false;
        foreach (Renderer r in currentSpecimen.GetComponentsInChildren<Renderer>(true))
        {
            if (any) bounds.Encapsulate(r.bounds);
            else { bounds = r.bounds; any = true; }
        }
        if (!any)
            return;

        float radius = Mathf.Max(bounds.extents.magnitude, 0.0001f);
        framedCentre = bounds.center;
        framedDistance = radius * framingMargin / Mathf.Tan(stageCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);

        // Clip planes are sized off the widest zoom so they never clip the specimen mid-zoom.
        stageCamera.nearClipPlane = Mathf.Max(0.001f, framedDistance / maxZoom - radius * 4f);
        stageCamera.farClipPlane = framedDistance / minZoom + radius * 8f;

        ApplyZoom();
    }

    private void ApplyZoom()
    {
        if (stageCamera == null || framedDistance <= 0f)
            return;

        stageCamera.transform.position = framedCentre - stageCamera.transform.forward * (framedDistance / zoom);
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (currentSpecimen == null)
            return;

        // Only fires while the cursor is over the viewport, so it never steals the map's zoom.
        zoom = Mathf.Clamp(zoom + eventData.scrollDelta.y * zoomStep, minZoom, maxZoom);
        ApplyZoom();
    }

    /// <summary>
    /// The specimen is a mannequin, not a unit: remove anything that would move it, fight, or
    /// register it with the colony systems. The Animator is deliberately kept so it still idles.
    /// </summary>
    private static void StripBehaviours(GameObject root)
    {
        foreach (var c in root.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (c == null || c is WaspRoleIconBillboard)
                continue;
            Destroy(c);
        }
        foreach (var col in root.GetComponentsInChildren<Collider>(true)) Destroy(col);
        foreach (var agent in root.GetComponentsInChildren<UnityEngine.AI.NavMeshAgent>(true)) Destroy(agent);
        // The floating role icon and health bar belong on the map, not in a specimen inspector.
        foreach (var canvas in root.GetComponentsInChildren<Canvas>(true)) Destroy(canvas.gameObject);
    }

    private static void SetLayerRecursively(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform) SetLayerRecursively(child.gameObject, layer);
    }

    /// <summary>Wiring hook for the editor setup tool.</summary>
    public void Configure(Transform stage, Camera camera, int layer)
    {
        stageRoot = stage;
        stageCamera = camera;
        specimenLayer = layer;
    }
}
