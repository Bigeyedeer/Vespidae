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
    private C_Friendly_Hive_Orc selectedHive;
    private C_Enemy_Hive_Orc selectedEnemyHive;

    public void SelectHex(HexTile hex)
    {
        if (hex == null)
        {
            return;
        }

        selectedHex = hex;
        selectedWasp = null;
        selectedHive = null;
        selectedEnemyHive = null;
        C_MainWorldOverlayNavigation navigation = ResolveOverlayNavigation();
        navigation?.CloseWaspInfo();
        navigation?.HideFriendlyWaspActions();
        navigation?.HideHiveTraining();
        waspInfoPanel?.Close();
        cameraFocus?.FocusOnHex(hex);
        hexOptionsPanel?.Open(hex);
        C_MainWorldHUD.GetOrCreate()?.ShowSelectedHex(hex);
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

    public void SelectHive(C_Friendly_Hive_Orc hive)
    {
        if (hive == null)
            return;

        selectedHive = hive;
        selectedEnemyHive = null;
        if (hive.OwnerHex != null)
            selectedHex = hive.OwnerHex;

        cameraFocus?.FocusOnHive(hive.CameraFocusPoint, hive.CameraLookPoint, hive.OwnerHex);
        ResolveOverlayNavigation()?.OpenHiveTraining(hive);
    }

    public void SelectHive(C_Enemy_Hive_Orc hive)
    {
        if (hive == null)
            return;

        selectedHive = null;
        selectedEnemyHive = hive;
        if (hive.OwnerHex != null)
            selectedHex = hive.OwnerHex;

        cameraFocus?.FocusOnHive(hive.CameraFocusPoint, hive.CameraLookPoint, hive.OwnerHex);
        ResolveOverlayNavigation()?.HideHiveTraining();
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
        C_MainWorldOverlayNavigation navigation = ResolveOverlayNavigation();

        if (navigation != null)
            navigation.CloseWaspInfo();

        waspInfoPanel?.Close();
    }

    public void CloseAllPanels()
    {
        ResolveOverlayNavigation()?.CloseAllPanels();
        hexOptionsPanel?.Close();
        waspInfoPanel?.Close();
    }

    public void CloseHexOptions()
    {
        hexOptionsPanel?.Close();
    }

    public void ReturnToMenu()
    {
        C_LoadingScreen.LoadScene(menuSceneName);
    }

    public HexTile SelectedHex => selectedHex;
    public WaspInfo SelectedWasp => selectedWasp;
    public C_Friendly_Hive_Orc SelectedHive => selectedHive;
    public C_Enemy_Hive_Orc SelectedEnemyHive => selectedEnemyHive;

    private C_MainWorldOverlayNavigation ResolveOverlayNavigation()
    {
        if (overlayNavigation == null)
        {
            overlayNavigation = C_MainWorldOverlayNavigation.Instance;
        }

        return overlayNavigation;
    }
}
