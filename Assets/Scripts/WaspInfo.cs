using System;
using UnityEngine;

public class WaspInfo : MonoBehaviour
{
    [Header("Species Information")]
    [SerializeField] private SB_Wasps_Info speciesInfo;

    [Header("Assigned Role Skill")]
    [SerializeField] private SB_Wasp_Skill assignedSkill;

    private SB_Wasps_Info runtimeSpecies;
    private WaspFunction runtimeFunction;
    private bool hasRuntimeFunction;

    public SB_Wasps_Info SpeciesInfo => runtimeSpecies != null ? runtimeSpecies : speciesInfo;
    public string CommonName => SpeciesInfo != null ? SpeciesInfo.CommonName : string.Empty;
    public string ScientificName => SpeciesInfo != null ? SpeciesInfo.ScientificName : string.Empty;
    public string Description => SpeciesInfo != null ? SpeciesInfo.GameplaySummary : string.Empty;
    public string EcologicalRole => SpeciesInfo != null ? SpeciesInfo.EcologicalRole : string.Empty;
    public bool IsNative => SpeciesInfo != null && SpeciesInfo.Classification == WaspClassification.Native;
    public WaspFunction FunctionRole => hasRuntimeFunction
        ? runtimeFunction
        : assignedSkill != null
            ? assignedSkill.Function
            : WaspFunction.Scout;
    public int SkillLevel => HiveManagement.Instance != null ? HiveManagement.Instance.GetSkillLevel(FunctionRole) : 0;
    public SB_Wasp_Skill SkillDefinition
    {
        get
        {
            if (assignedSkill != null && assignedSkill.Function == FunctionRole)
                return assignedSkill;

            return HiveManagement.Instance?.GetSkillDefinition(FunctionRole) ?? assignedSkill;
        }
    }
    public Sprite RoleIcon => SkillDefinition != null ? SkillDefinition.RoleIcon : null;
    public float ScoutingRange => GetSkillValue(WaspSkillStat.ScoutingRange);
    public float MovementSpeedMultiplier => GetSkillValue(WaspSkillStat.MovementSpeed);
    public float GatheringMultiplier => GetSkillValue(WaspSkillStat.GatheringMultiplier);
    public float BuildSpeedMultiplier => GetSkillValue(WaspSkillStat.BuildSpeed);
    public float BroodCareMultiplier => GetSkillValue(WaspSkillStat.BroodCare);
    public float DefenceMultiplier => GetSkillValue(WaspSkillStat.Defence);
    public float AttackSpeedMultiplier => GetSkillValue(WaspSkillStat.AttackSpeed);
    public float IdentificationMultiplier => GetSkillValue(WaspSkillStat.Identification);
    public event Action AssignmentChanged;

    public void SetRuntimeAssignment(SB_Wasps_Info species, WaspFunction function)
    {
        if (species != null)
            runtimeSpecies = species;

        runtimeFunction = function;
        hasRuntimeFunction = true;
        AssignmentChanged?.Invoke();
    }

    public float GetSkillValue(WaspSkillStat stat)
    {
        if (HiveManagement.Instance != null)
            return HiveManagement.Instance.GetEffectiveValue(FunctionRole, stat);

        return SkillDefinition != null ? SkillDefinition.GetEffectiveValue(stat, 0) : 1f;
    }
}
