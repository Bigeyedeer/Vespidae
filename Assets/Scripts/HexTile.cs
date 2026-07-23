using UnityEngine;

public class HexTile : MonoBehaviour
{
    public enum HexState
    {
        Owned,
        Unknown,
        Scouted,
        Enemy,
        Locked
    }

    public enum HexContent
    {
        Safe,
        Protein
    }

    [Header("Hex Information")]
    [SerializeField] private string hexName = "Unexplored Grassland";

    [Header("Territory State")]
    [SerializeField] private HexState state = HexState.Unknown;

    [Header("Hidden Content")]
    [SerializeField] private HexContent content = HexContent.Safe;

    [Header("Protein Content")]
    [SerializeField] private GameObject antGroup;
    [SerializeField] private float proteinPerGather = 0.5f;

    [Header("Hex Materials")]
    [SerializeField] private Renderer hexRenderer;
    [SerializeField] private Material ownedMaterial;
    [SerializeField] private Material unknownMaterial;
    [SerializeField] private Material lockedMaterial;
    [SerializeField] private Material proteinMaterial;

    [Header("Camera")]
    [SerializeField] private Transform focusPoint;

    public string HexName => hexName;
    public HexState State => state;
    public HexContent Content => content;

    public Vector3 FocusPosition
    {
        get
        {
            if (focusPoint != null)
                return focusPoint.position;

            return transform.position;
        }
    }

    private void Start()
    {
        RefreshContentVisuals();
        RefreshHexMaterial();
    }

    private void OnValidate()
    {
        RefreshContentVisuals();
        RefreshHexMaterial();
    }

    public void Scout()
    {
        if (state != HexState.Unknown)
        {
            Debug.LogWarning(
                $"{hexName} cannot be scouted because its state is {state}."
            );

            return;
        }

        state = HexState.Scouted;

        RefreshContentVisuals();
        RefreshHexMaterial();

        if (content == HexContent.Protein)
        {
            Debug.Log($"{hexName} contains protein.");
        }
        else
        {
            Debug.Log($"{hexName} is safe.");
        }
    }

    public void Claim()
    {
        if (state != HexState.Scouted)
        {
            Debug.LogWarning(
                $"{hexName} cannot be claimed because it has not been scouted."
            );

            return;
        }

        state = HexState.Owned;

        RefreshContentVisuals();
        RefreshHexMaterial();

        Debug.Log($"{hexName} has been claimed.");
    }

    public void GatherProtein()
    {
        if (state != HexState.Owned)
        {
            Debug.LogWarning(
                $"{hexName} must be owned before gathering resources."
            );

            return;
        }

        if (content != HexContent.Protein)
        {
            Debug.LogWarning(
                $"{hexName} does not contain protein."
            );

            return;
        }

        if (ResourceManager.Instance == null)
        {
            Debug.LogWarning(
                "No ResourceManager exists in the scene."
            );

            return;
        }

        ResourceManager.Instance.AddProtein(proteinPerGather);
    }

    private void RefreshContentVisuals()
    {
        if (antGroup == null)
            return;

        bool showAnts =
            content == HexContent.Protein &&
            state != HexState.Unknown &&
            state != HexState.Locked;

        antGroup.SetActive(showAnts);
    }

    private void RefreshHexMaterial()
    {
        if (hexRenderer == null)
        {
            Debug.LogWarning(
                $"{hexName} does not have a Hex Renderer assigned."
            );

            return;
        }

        switch (state)
        {
            case HexState.Locked:

                if (lockedMaterial != null)
                    hexRenderer.sharedMaterial = lockedMaterial;

                break;

            case HexState.Owned:

                if (ownedMaterial != null)
                    hexRenderer.sharedMaterial = ownedMaterial;

                break;

            case HexState.Scouted:

                if (content == HexContent.Protein)
                {
                    if (proteinMaterial != null)
                        hexRenderer.sharedMaterial = proteinMaterial;
                }
                else
                {
                    if (unknownMaterial != null)
                        hexRenderer.sharedMaterial = unknownMaterial;
                }

                break;

            case HexState.Unknown:

                if (unknownMaterial != null)
                    hexRenderer.sharedMaterial = unknownMaterial;

                break;

            case HexState.Enemy:
                
                break;
        }
    }
}
