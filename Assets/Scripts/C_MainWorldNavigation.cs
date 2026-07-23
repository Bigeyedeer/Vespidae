using UnityEngine;
using UnityEngine.SceneManagement;

public class C_MainWorldNavigation : MonoBehaviour
{
    [Header("Existing Wasp RTS systems")]
    [SerializeField] private C_MainWorldCameraFocus cameraFocus;
    [SerializeField] private HexOptionsPanel hexOptionsPanel;
    [SerializeField] private WaspInfoPanel waspInfoPanel;

    [Header("Migrated HUD")]
    [SerializeField] private C_MainWorldOverlayNavigation overlayNavigation;

    [Header("Scene flow")]
    [SerializeField] private string menuSceneName = "Menu";

    private HexTile selectedHex;
    private WaspInfo selectedWasp;

    public void SelectHex(HexTile hex)
    {
        if (hex == null)
        {
            return;
        }

        selectedHex = hex;
        cameraFocus?.FocusOnHex(hex);
        hexOptionsPanel?.Open(hex);
    }

    public void SelectWasp(WaspInfo wasp)
    {
        if (wasp == null)
        {
            return;
        }

        selectedWasp = wasp;
        cameraFocus?.FocusOnWasp(wasp);
    }

    public void OpenSkills()
    {
        ResolveOverlayNavigation()?.OpenSkills();
    }

    public void CloseSkills()
    {
        ResolveOverlayNavigation()?.CloseSkills();
    }

    public void OpenWaspInfo()
    {
        ResolveOverlayNavigation()?.OpenWaspInfo();
    }

    public void CloseWaspInfo()
    {
        ResolveOverlayNavigation()?.CloseWaspInfo();
        waspInfoPanel?.Close();
    }

    public void CloseAllPanels()
    {
        ResolveOverlayNavigation()?.CloseAllPanels();
        hexOptionsPanel?.Close();
        waspInfoPanel?.Close();
    }

    public void ReturnToMenu()
    {
        SceneManager.LoadScene(menuSceneName);
    }

    public HexTile SelectedHex => selectedHex;
    public WaspInfo SelectedWasp => selectedWasp;

    private C_MainWorldOverlayNavigation ResolveOverlayNavigation()
    {
        if (overlayNavigation == null)
        {
            overlayNavigation = C_MainWorldOverlayNavigation.Instance;
        }

        return overlayNavigation;
    }
}
