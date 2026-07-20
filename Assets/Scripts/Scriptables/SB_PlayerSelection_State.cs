using System;
using UnityEngine;

[CreateAssetMenu(fileName = "SO_PlayerSelection", menuName = "Vespidae Wars/Player Selection State")]
public class SB_PlayerSelection_State : ScriptableObject
{
    [Header("Fallback used when MainWorld is opened directly")]
    [SerializeField] private SB_Wasps_Info defaultWasp;
    [SerializeField] private WaspFunction defaultFunction = WaspFunction.Scout;

    [NonSerialized] private SB_Wasps_Info selectedWasp;
    [NonSerialized] private WaspFunction selectedFunction;
    [NonSerialized] private bool hasSelection;

    public bool HasSelection => hasSelection;
    public SB_Wasps_Info SelectedWasp => hasSelection && selectedWasp != null ? selectedWasp : defaultWasp;
    public WaspFunction SelectedFunction => hasSelection ? selectedFunction : defaultFunction;

    public void SetSelection(SB_Wasps_Info wasp, WaspFunction function)
    {
        if (wasp == null)
        {
            throw new ArgumentNullException(nameof(wasp));
        }

        selectedWasp = wasp;
        selectedFunction = function;
        hasSelection = true;
    }

    public void ResetSelection()
    {
        selectedWasp = null;
        selectedFunction = defaultFunction;
        hasSelection = false;
    }

    public WaspFunctionInfo GetSelectedFunctionInfo()
    {
        return SelectedWasp != null ? SelectedWasp.GetFunction(SelectedFunction) : null;
    }

#if UNITY_EDITOR
    public void ConfigureDefaultsForEditor(SB_Wasps_Info wasp, WaspFunction function)
    {
        defaultWasp = wasp;
        defaultFunction = function;
    }
#endif
}
