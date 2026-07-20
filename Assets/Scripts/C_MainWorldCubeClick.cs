using UnityEngine;

public class C_MainWorldCubeClick : MonoBehaviour
{
    private void OnMouseDown()
    {
        if (C_MainWorldOverlayNavigation.Instance != null)
        {
            C_MainWorldOverlayNavigation.Instance.OpenWaspInfo();
        }
    }
}
