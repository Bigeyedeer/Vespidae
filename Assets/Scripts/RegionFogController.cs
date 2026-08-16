using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Drives the volumetric region fog.
///
/// The painted region mask is the single source of truth: its RGB channels mark the three bands,
/// and hexes work out which band they belong to by sampling the mask at their own world position.
/// That means the fog you can see and the regions the game reasons about cannot drift apart, however
/// the mask is repainted.
///
/// Revealing a band lerps that channel of <c>_RegionsRevealed</c> from 0 to 1, which the shader turns
/// into an erosion dissolve so the fog burns off in patches rather than fading out.
///
/// What actually triggers a reveal is deliberately not decided here - call <see cref="RevealRegion"/>
/// from whatever progression rule the design settles on.
/// </summary>
[DefaultExecutionOrder(-150)]
public class RegionFogController : MonoBehaviour
{
    public const int RegionCount = 3;
    public const int NoRegion = -1;

    private static readonly int RegionsRevealedId = Shader.PropertyToID("_RegionsRevealed");
    private static readonly int PlayMinId = Shader.PropertyToID("_PlayMin");
    private static readonly int PlayMaxId = Shader.PropertyToID("_PlayMax");
    private static readonly int EdgeStartId = Shader.PropertyToID("_EdgeStart");
    private static readonly int EdgeEndId = Shader.PropertyToID("_EdgeEnd");
    private static readonly int OpeningExpandId = Shader.PropertyToID("_OpeningExpand");
    private static readonly int PlayPaddingId = Shader.PropertyToID("_PlayPadding");

    public static RegionFogController Instance { get; private set; }

    [Header("Fog")]
    [SerializeField, Tooltip("Optional. Found on this object if left empty. The quad the fog marches through.")]
    private Renderer fogRenderer;

    [Header("Coverage")]
    [SerializeField, Min(0f), Tooltip("How wide the fog quad is forced to be, in world units. The quad is only " +
                                      "the surface the volume is marched from, so it has to cover the whole " +
                                      "screen however far the camera pans out.")]
    private float coverageExtent = 400f;
    [SerializeField, Tooltip("Keeps the quad centred under the camera in X and Z. The volume itself is worked " +
                             "out in world space, so sliding the quad moves nothing the player can see - it " +
                             "only guarantees there is always a surface in front of the camera to march from.")]
    private bool followCamera = false;

    [Header("Play Area")]
    [SerializeField, Tooltip("Measures the rectangle the fog leaves open from the hex tiles themselves, " +
                             "so the wall stays put if the map is ever re-laid out. Turn this off to keep " +
                             "whatever is authored on the material.")]
    private bool measurePlayAreaFromHexes = true;
    [SerializeField, Range(0.25f, 5f), Tooltip("How far the cleared middle spreads out from the hexes. The mask " +
                                               "stores band coverage as a long gradient, and this gains it up " +
                                               "before the fog is cut, so raising it exposes more of the map " +
                                               "without repainting anything. This is the dial for opening the " +
                                               "centre; padding below only moves the outer wall.")]
    private float openingExpand = 1.05f;

    [Header("Opening Animation")]
    [SerializeField, Tooltip("Start the match fully closed and open the fog to the value above, so the " +
                             "player watches their ground appear instead of arriving to a finished map.")]
    private bool animateOpenOnStart = true;
    [SerializeField, Min(0f), Tooltip("Seconds spent opening. Long on purpose - it is the establishing shot.")]
    private float openDuration = 9f;
    [SerializeField, Min(0f), Tooltip("Seconds to hold fully closed first, covering the handoff from the " +
                                      "loading screen.")]
    private float openDelay = 0.5f;
    [SerializeField, Tooltip("THE MAIN DIAL. How far past the outermost hex centre the map stays clear. " +
                             "Raise it to push the fog back off the edge tiles, lower it to close the " +
                             "opening in. Updates live as you drag it.")]
    private float playAreaPadding = -3.61f;
    [SerializeField, Min(0f), Tooltip("Extra distance beyond the clear area before the fog begins to " +
                                      "thicken at all. Controls how abruptly the wall starts.")]
    private float wallStart = 3.72f;
    [SerializeField, Min(0.1f), Tooltip("Distance beyond the clear area at which the fog is fully opaque. " +
                                        "The gap between this and Wall Start is how soft the wall looks.")]
    private float wallEnd = 26.5f;

    [Header("Reveal")]
    [SerializeField, Min(0f), Tooltip("Seconds a band takes to dissolve away once revealed.")]
    private float revealDuration = 3f;
    [SerializeField, Tooltip("Bands revealed when the match starts. Leave all off for a fully fogged map.")]
    private bool[] revealedAtStart = new bool[RegionCount];
    [SerializeField, Tooltip("Lift a band the moment the player owns or scouts any hex inside it. Turn off " +
                             "to drive reveals entirely from your own progression rules.")]
    private bool revealBandOnPlayerArrival = true;

    private readonly Dictionary<HexTile, int> hexRegions = new Dictionary<HexTile, int>();
    private readonly List<HexTile> watchedHexes = new List<HexTile>();
    private MaterialPropertyBlock propertyBlock;
    private Texture2D mask;
    private Vector2 maskMin;
    private Vector2 maskMax;

    private Vector3 revealed;
    private Vector3 revealTarget;
    private float restHeight;
    private float openingTarget;
    private float openingTimer;
    private bool opening;
    private bool playAreaResolved;
    private Vector4 playMin;
    private Vector4 playMax;

    /// <summary>Current reveal amount per band, 0 fogged through to 1 fully clear.</summary>
    public Vector3 Revealed => revealed;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        if (fogRenderer == null)
            fogRenderer = GetComponent<Renderer>();

        ApplyCoverage();

        if (!ResolveMask())
            return;

        propertyBlock = new MaterialPropertyBlock();

        for (int i = 0; i < RegionCount && i < revealedAtStart.Length; i++)
        {
            if (revealedAtStart[i])
            {
                revealed[i] = 1f;
                revealTarget[i] = 1f;
            }
        }

        AssignHexRegions();
        MeasurePlayArea();
        SubscribeToHexes();

        // Hold the authored width and start shut, so the opening is something the player watches
        // happen rather than a state they arrive into.
        openingTarget = openingExpand;
        if (animateOpenOnStart && openDuration > 0f)
        {
            openingExpand = 0f;
            openingTimer = -openDelay;
            opening = true;
        }

        PushToShader();
    }

    /// <summary>Opens the fog from shut to its authored width. Safe to call again mid-open.</summary>
    public void PlayOpeningAnimation()
    {
        openingExpand = 0f;
        openingTimer = -openDelay;
        opening = true;
        PushToShader();
    }

    private void TickOpening()
    {
        if (!opening)
            return;

        openingTimer += Time.deltaTime;
        if (openingTimer < 0f)
            return;

        float t = openDuration > 0f ? Mathf.Clamp01(openingTimer / openDuration) : 1f;
        // Eased so it drifts open rather than sliding at a constant rate.
        openingExpand = Mathf.Lerp(0f, openingTarget, t * t * (3f - 2f * t));

        if (t >= 1f)
        {
            openingExpand = openingTarget;
            opening = false;
        }

        PushToShader();
    }

    /// <summary>
    /// Watches every hex so a band lifts as soon as the player reaches into it. Fog of war that only
    /// answers to a scripted trigger reads as a cutscene; tying it to arrival makes expanding into
    /// the unknown the thing that clears it.
    /// </summary>
    private void SubscribeToHexes()
    {
        if (!revealBandOnPlayerArrival)
            return;

        foreach (HexTile tile in hexRegions.Keys)
        {
            if (tile == null)
                continue;

            tile.StateChanged -= HandleHexStateChanged;
            tile.StateChanged += HandleHexStateChanged;
            watchedHexes.Add(tile);

            // Anything the player already holds at kick-off opens its band immediately, so the
            // starting position is never sitting under fog.
            if (IsPlayerPresence(tile))
            {
                int region = RegionOf(tile);
                if (region != NoRegion)
                {
                    revealed[region] = 1f;
                    revealTarget[region] = 1f;
                }
            }
        }
    }

    private void HandleHexStateChanged(HexTile tile)
    {
        if (tile == null || !IsPlayerPresence(tile))
            return;

        RevealRegion(RegionOf(tile));
    }

    private static bool IsPlayerPresence(HexTile tile)
    {
        return tile.State == HexTile.HexState.Owned || tile.State == HexTile.HexState.Scouted;
    }

    /// <summary>
    /// Works out the rectangle of ground the fog leaves open and hands it to the shader. The shader
    /// ramps its outer wall up by world distance from this rectangle, so it has to match the hexes
    /// rather than the painted mask - the mask is neither square nor centred on the map.
    /// </summary>
    private void MeasurePlayArea()
    {
        playAreaResolved = false;

        if (!measurePlayAreaFromHexes)
            return;

        bool any = false;
        float minimumX = float.MaxValue;
        float maximumX = float.MinValue;
        float minimumZ = float.MaxValue;
        float maximumZ = float.MinValue;

        foreach (HexTile tile in FindObjectsByType<HexTile>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (tile == null)
                continue;

            Vector3 position = tile.transform.position;
            minimumX = Mathf.Min(minimumX, position.x);
            maximumX = Mathf.Max(maximumX, position.x);
            minimumZ = Mathf.Min(minimumZ, position.z);
            maximumZ = Mathf.Max(maximumZ, position.z);
            any = true;
        }

        if (!any)
            return;

        playMin = new Vector4(minimumX - playAreaPadding, minimumZ - playAreaPadding, 0f, 0f);
        playMax = new Vector4(maximumX + playAreaPadding, maximumZ + playAreaPadding, 0f, 0f);
        playAreaResolved = true;
    }

    private void OnDestroy()
    {
        foreach (HexTile tile in watchedHexes)
        {
            if (tile != null)
                tile.StateChanged -= HandleHexStateChanged;
        }
        watchedHexes.Clear();

        if (Instance == this)
            Instance = null;
    }

    /// <summary>
    /// Widens the quad so it always fills the screen. The quad lies flat, rotated a quarter turn about
    /// X, so its local X and Y are the world X and Z it spans.
    /// </summary>
    private void ApplyCoverage()
    {
        restHeight = transform.position.y;

        if (coverageExtent <= 0f)
            return;

        Vector3 scale = transform.localScale;
        scale.x = Mathf.Max(scale.x, coverageExtent);
        scale.y = Mathf.Max(scale.y, coverageExtent);
        transform.localScale = scale;
    }

    private void LateUpdate()
    {
        if (!followCamera)
            return;

        Camera view = Camera.main;
        if (view == null)
            return;

        Vector3 position = view.transform.position;
        position.y = restHeight;
        transform.position = position;
    }

    private bool ResolveMask()
    {
        Material material = fogRenderer != null ? fogRenderer.sharedMaterial : null;
        if (material == null)
        {
            Debug.LogWarning($"{name} has no fog material, so region fog is inactive.", this);
            return false;
        }

        mask = material.GetTexture("_RegionMask") as Texture2D;
        if (mask == null)
        {
            Debug.LogWarning($"{name} has no region mask assigned, so hexes cannot be assigned to bands.", this);
            return false;
        }

        if (!mask.isReadable)
        {
            Debug.LogWarning($"Region mask '{mask.name}' is not readable. Enable Read/Write on it or hexes " +
                             "cannot be matched to bands.", this);
            mask = null;
            return false;
        }

        Vector4 min = material.GetVector("_MaskMin");
        Vector4 max = material.GetVector("_MaskMax");
        maskMin = new Vector2(min.x, min.y);
        maskMax = new Vector2(max.x, max.y);
        return true;
    }

    /// <summary>
    /// Reads the mask at every hex's world position and records which band it falls in. Hexes that
    /// land on unpainted ground get <see cref="NoRegion"/> and stay fogged.
    /// </summary>
    public void AssignHexRegions()
    {
        hexRegions.Clear();
        if (mask == null)
            return;

        foreach (HexTile tile in FindObjectsByType<HexTile>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (tile == null)
                continue;

            hexRegions[tile] = SampleRegion(tile.transform.position);
        }
    }

    private int SampleRegion(Vector3 worldPosition)
    {
        Vector2 span = maskMax - maskMin;
        if (Mathf.Approximately(span.x, 0f) || Mathf.Approximately(span.y, 0f))
            return NoRegion;

        float u = (worldPosition.x - maskMin.x) / span.x;
        float v = (worldPosition.z - maskMin.y) / span.y;
        if (u < 0f || u > 1f || v < 0f || v > 1f)
            return NoRegion;

        Color sample = mask.GetPixelBilinear(u, v);

        // Strongest channel wins, so a hex sitting on a soft border still lands on one side.
        int best = NoRegion;
        float bestWeight = 0.25f;
        if (sample.r > bestWeight) { bestWeight = sample.r; best = 0; }
        if (sample.g > bestWeight) { bestWeight = sample.g; best = 1; }
        if (sample.b > bestWeight) { best = 2; }
        return best;
    }

    /// <summary>Which band this hex sits in, or <see cref="NoRegion"/> if it is outside every band.</summary>
    public int RegionOf(HexTile tile)
    {
        if (tile != null && hexRegions.TryGetValue(tile, out int region))
            return region;
        return NoRegion;
    }

    /// <summary>True once this hex's band has finished dissolving away.</summary>
    public bool IsHexVisible(HexTile tile)
    {
        int region = RegionOf(tile);
        return region != NoRegion && revealed[region] >= 0.999f;
    }

    public bool IsRegionRevealed(int region)
    {
        return region >= 0 && region < RegionCount && revealed[region] >= 0.999f;
    }

    /// <summary>Starts dissolving a band away. Safe to call again on a band already revealed.</summary>
    public void RevealRegion(int region)
    {
        if (region < 0 || region >= RegionCount)
            return;

        if (revealTarget[region] < 1f)
            AudioDirector.Play(GameSound.FogRevealed);

        revealTarget[region] = 1f;
    }

    /// <summary>Puts a band back under fog, for restarts or for scripted sequences.</summary>
    public void HideRegion(int region)
    {
        if (region < 0 || region >= RegionCount)
            return;

        revealTarget[region] = 0f;
    }

    private void Update()
    {
        if (propertyBlock == null)
            return;

        TickOpening();

        bool changed = false;
        float step = revealDuration > 0f ? Time.deltaTime / revealDuration : 1f;

        for (int i = 0; i < RegionCount; i++)
        {
            if (Mathf.Approximately(revealed[i], revealTarget[i]))
                continue;

            revealed[i] = Mathf.MoveTowards(revealed[i], revealTarget[i], step);
            changed = true;
        }

        if (changed)
            PushToShader();
    }

    private void PushToShader()
    {
        if (fogRenderer == null || propertyBlock == null)
            return;

        // A property block keeps runtime reveal state off the shared material, so play mode never
        // writes the current fog state back into the asset.
        fogRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetVector(RegionsRevealedId, new Vector4(revealed.x, revealed.y, revealed.z, 0f));

        if (playAreaResolved)
        {
            propertyBlock.SetVector(PlayMinId, playMin);
            propertyBlock.SetVector(PlayMaxId, playMax);
        }

        propertyBlock.SetFloat(EdgeStartId, wallStart);
        propertyBlock.SetFloat(EdgeEndId, wallEnd);
        propertyBlock.SetFloat(OpeningExpandId, openingExpand);
        // Padding now shifts the distance field rather than resizing a rectangle, so it still moves
        // the wall in and out but the wall keeps the shape of the hex cluster.
        propertyBlock.SetFloat(PlayPaddingId, playAreaPadding);

        fogRenderer.SetPropertyBlock(propertyBlock);
    }

    /// <summary>
    /// Re-measures the clear area and hands the wall settings to the shader. Called whenever the
    /// inspector values change so the sweet spot can be found by dragging rather than by replaying.
    /// </summary>
    private void ApplyPlayArea()
    {
        if (fogRenderer == null)
            fogRenderer = GetComponent<Renderer>();
        if (fogRenderer == null)
            return;

        MeasurePlayArea();

        if (Application.isPlaying && propertyBlock != null)
        {
            PushToShader();
            return;
        }

        // Outside play mode there is no property block, so these go straight onto the material.
        // That is also what makes them stick as authored settings rather than runtime state - unlike
        // the reveal vector, which must stay on the block so play mode never bakes itself in.
        Material material = fogRenderer.sharedMaterial;
        if (material == null)
            return;

        if (playAreaResolved)
        {
            material.SetVector(PlayMinId, playMin);
            material.SetVector(PlayMaxId, playMax);
        }

        material.SetFloat(EdgeStartId, wallStart);
        material.SetFloat(EdgeEndId, wallEnd);
        material.SetFloat(OpeningExpandId, openingExpand);
        material.SetFloat(PlayPaddingId, playAreaPadding);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (wallEnd <= wallStart)
            wallEnd = wallStart + 0.1f;

        if (Application.isPlaying)
        {
            ApplyPlayArea();
            return;
        }

        // Deferred, because touching materials directly inside OnValidate trips Unity's
        // "sending message during import" warning.
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this != null)
                ApplyPlayArea();
        };
    }
#endif

    [ContextMenu("Reveal all regions")]
    private void DebugRevealAll()
    {
        for (int i = 0; i < RegionCount; i++)
            RevealRegion(i);
    }

    [ContextMenu("Hide all regions")]
    private void DebugHideAll()
    {
        for (int i = 0; i < RegionCount; i++)
            HideRegion(i);
    }

    [ContextMenu("Log hex region assignment")]
    private void DebugLogAssignment()
    {
        if (mask == null && !ResolveMask())
            return;

        AssignHexRegions();
        int[] counts = new int[RegionCount + 1];
        foreach (KeyValuePair<HexTile, int> entry in hexRegions)
            counts[entry.Value == NoRegion ? RegionCount : entry.Value]++;

        Debug.Log($"Region fog: band 0 = {counts[0]} hexes, band 1 = {counts[1]}, band 2 = {counts[2]}, " +
                  $"outside every band = {counts[RegionCount]}.", this);
    }
}
