using UnityEngine;

public class WaspInfo : MonoBehaviour
{
    [Header("Species Information")]
    [SerializeField] private SB_Wasps_Info speciesInfo;

    [Header("Camera Focus")]
    [SerializeField] private Transform cameraPoint;
    [SerializeField] private Transform lookPoint;

    public SB_Wasps_Info SpeciesInfo => speciesInfo;
    public string CommonName => speciesInfo != null ? speciesInfo.CommonName : string.Empty;
    public string ScientificName => speciesInfo != null ? speciesInfo.ScientificName : string.Empty;
    public string Description => speciesInfo != null ? speciesInfo.GameplaySummary : string.Empty;
    public string EcologicalRole => speciesInfo != null ? speciesInfo.EcologicalRole : string.Empty;
    public bool IsNative => speciesInfo != null && speciesInfo.Classification == WaspClassification.Native;

    public Transform CameraPoint => cameraPoint;

    public Vector3 LookPosition
    {
        get
        {
            if (lookPoint != null)
                return lookPoint.position;

            return transform.position;
        }
    }
}
