using UnityEngine;

public class C_MainWorldHexNode : MonoBehaviour
{
    [SerializeField] private string habitatName = "Fynbos scrub";
    [SerializeField] private string nodeStatus = "Contested";
    [SerializeField] private string resourceSummary = "Nectar / prey: medium / low";
    [SerializeField] private string nestSummary = "Exposed paper comb";

    private C_MainWorldNavigation navigation;
    private Renderer nodeRenderer;
    private Color baseColor = Color.white;

    public string HabitatName => habitatName;
    public string NodeStatus => nodeStatus;
    public string ResourceSummary => resourceSummary;
    public string NestSummary => nestSummary;

    public void Configure(
        C_MainWorldNavigation owner,
        string habitat,
        string status,
        string resources,
        string nest)
    {
        navigation = owner;
        habitatName = habitat;
        nodeStatus = status;
        resourceSummary = resources;
        nestSummary = nest;
        nodeRenderer = GetComponent<Renderer>();
        if (nodeRenderer != null)
        {
            baseColor = nodeRenderer.sharedMaterial != null && nodeRenderer.sharedMaterial.HasProperty("_BaseColor")
                ? nodeRenderer.sharedMaterial.GetColor("_BaseColor")
                : nodeRenderer.sharedMaterial != null && nodeRenderer.sharedMaterial.HasProperty("_Color")
                    ? nodeRenderer.sharedMaterial.GetColor("_Color")
                    : Color.white;
        }
    }

    public void SetHighlighted(bool highlighted)
    {
        if (nodeRenderer == null)
        {
            nodeRenderer = GetComponent<Renderer>();
        }

        if (nodeRenderer == null || nodeRenderer.sharedMaterial == null)
        {
            return;
        }

        Color target = highlighted ? Color.Lerp(baseColor, Color.white, 0.45f) : baseColor;
        if (nodeRenderer.sharedMaterial.HasProperty("_BaseColor"))
        {
            nodeRenderer.sharedMaterial.SetColor("_BaseColor", target);
        }
        else if (nodeRenderer.sharedMaterial.HasProperty("_Color"))
        {
            nodeRenderer.sharedMaterial.SetColor("_Color", target);
        }
    }

    private void OnMouseDown()
    {
        navigation?.SelectHex(this);
    }

    private void OnMouseEnter()
    {
        navigation?.SetHoveredHex(this);
    }

    private void OnMouseExit()
    {
        navigation?.ClearHoveredHex(this);
    }
}
