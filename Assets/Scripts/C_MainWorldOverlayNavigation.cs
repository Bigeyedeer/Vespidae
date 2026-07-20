using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class C_MainWorldOverlayNavigation : MonoBehaviour
{
    public static C_MainWorldOverlayNavigation Instance { get; private set; }

    [SerializeField] private GameObject waspInfoPanel;
    [SerializeField] private GameObject skillsPanel;
    [SerializeField] private Key skillsKey = Key.K;

    private void Awake()
    {
        Instance = this;
        BindSceneReferences();
        CloseAllPanels();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current[skillsKey].wasPressedThisFrame)
        {
            if (skillsPanel != null && skillsPanel.activeSelf)
            {
                CloseSkills();
            }
            else
            {
                OpenSkills();
            }
        }
    }

    public void BindSceneReferences()
    {
        if (waspInfoPanel == null)
        {
            waspInfoPanel = FindChild("WaspInfoPanel");
        }

        if (skillsPanel == null)
        {
            skillsPanel = FindChild("SkillsPanel");
        }

        BindButton("Action_Codex", OpenWaspInfo);
        BindButton("Action_Return", CloseAllPanels);
        BindButton("WaspInfo_Return", CloseWaspInfo);
        BindButton("WaspInfo_Close", CloseWaspInfo);
        BindButton("Skills_Return", CloseSkills);
        BindButton("Skills_Close", CloseSkills);
    }

    public void OpenWaspInfo()
    {
        if (skillsPanel != null)
        {
            skillsPanel.SetActive(false);
        }

        if (waspInfoPanel != null)
        {
            waspInfoPanel.SetActive(true);
        }
    }

    public void OpenSkills()
    {
        if (waspInfoPanel != null)
        {
            waspInfoPanel.SetActive(false);
        }

        if (skillsPanel != null)
        {
            skillsPanel.SetActive(true);
        }
    }

    public void CloseWaspInfo()
    {
        if (waspInfoPanel != null)
        {
            waspInfoPanel.SetActive(false);
        }
    }

    public void CloseSkills()
    {
        if (skillsPanel != null)
        {
            skillsPanel.SetActive(false);
        }
    }

    public void CloseAllPanels()
    {
        CloseWaspInfo();
        CloseSkills();
    }

    private GameObject FindChild(string childName)
    {
        Transform child = transform.Find(childName);
        return child != null ? child.gameObject : null;
    }

    private void BindButton(string objectName, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = FindChild(objectName);
        if (buttonObject == null)
        {
            return;
        }

        Button button = buttonObject.GetComponent<Button>();
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }
}
