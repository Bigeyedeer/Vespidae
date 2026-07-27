using UnityEngine;

public class WaspInfo : MonoBehaviour
{
    [Header("Species Information")]
    [SerializeField] private SB_Wasps_Info speciesInfo;

    [Header("Assigned Role Skill")]
    [SerializeField] private SB_Wasp_Skill assignedSkill;

    [Header("Camera Focus")]
    [SerializeField] private Transform cameraPoint;
    [SerializeField] private Transform lookPoint;

    public SB_Wasps_Info SpeciesInfo => speciesInfo;
    public string CommonName => speciesInfo != null ? speciesInfo.CommonName : string.Empty;
    public string ScientificName => speciesInfo != null ? speciesInfo.ScientificName : string.Empty;
    public string Description => speciesInfo != null ? speciesInfo.GameplaySummary : string.Empty;
    public string EcologicalRole => speciesInfo != null ? speciesInfo.EcologicalRole : string.Empty;
    public bool IsNative => speciesInfo != null && speciesInfo.Classification == WaspClassification.Native;
    public WaspFunction FunctionRole => assignedSkill != null ? assignedSkill.Function : WaspFunction.Scout;
    public int SkillLevel => HiveManagement.Instance != null ? HiveManagement.Instance.GetSkillLevel(FunctionRole) : 0;
    public SB_Wasp_Skill SkillDefinition => assignedSkill != null ? assignedSkill : HiveManagement.Instance?.GetSkillDefinition(FunctionRole);
    public float ScoutingRange => GetSkillValue(WaspSkillStat.ScoutingRange);
    public float MovementSpeedMultiplier => GetSkillValue(WaspSkillStat.MovementSpeed);
    public float GatheringMultiplier => GetSkillValue(WaspSkillStat.GatheringMultiplier);
    public float BuildSpeedMultiplier => GetSkillValue(WaspSkillStat.BuildSpeed);
    public float BroodCareMultiplier => GetSkillValue(WaspSkillStat.BroodCare);
    public float DefenceMultiplier => GetSkillValue(WaspSkillStat.Defence);
    public float AttackSpeedMultiplier => GetSkillValue(WaspSkillStat.AttackSpeed);
    public float IdentificationMultiplier => GetSkillValue(WaspSkillStat.Identification);
    public Transform CameraPoint => cameraPoint;

    public float GetSkillValue(WaspSkillStat stat)
    {
        if (HiveManagement.Instance != null)
            return HiveManagement.Instance.GetEffectiveValue(FunctionRole, stat);

        return SkillDefinition != null ? SkillDefinition.GetEffectiveValue(stat, 0) : 1f;
    }

    public Vector3 LookPosition => lookPoint != null ? lookPoint.position : transform.position;
}
