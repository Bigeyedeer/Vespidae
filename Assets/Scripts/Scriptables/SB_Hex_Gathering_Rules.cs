using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "SO_HexGathering_Rules", menuName = "Vespidae Wars/Hex Gathering Rules")]
public class SB_Hex_Gathering_Rules : ScriptableObject
{
    [Header("Gathering Timing")]
    [SerializeField, Min(0.1f)] private float gatheringTickIntervalSeconds = 2f;

    [Header("Per Wasp Per Tick")]
    [FormerlySerializedAs("proteinPerWaspPerTick")]
    [SerializeField, Min(0f)] private float preyPerWaspPerTick = 10f;
    [FormerlySerializedAs("sugarPerWaspPerTick")]
    [SerializeField, Min(0f)] private float nectarPerWaspPerTick = 20f;
    [SerializeField, Min(0f)] private float fibrePerWaspPerTick;

    [Header("Gathering Limits")]
    [SerializeField, Min(1)] private int maximumGatheringWasps = 20;

    public float GatheringTickIntervalSeconds => gatheringTickIntervalSeconds;
    public float PreyPerWaspPerTick => preyPerWaspPerTick;
    public float NectarPerWaspPerTick => nectarPerWaspPerTick;
    public float FibrePerWaspPerTick => fibrePerWaspPerTick;
    public int MaximumGatheringWasps => maximumGatheringWasps;

    public float GetPreyAmount(int waspCount)
    {
        return Mathf.Clamp(waspCount, 0, maximumGatheringWasps) * preyPerWaspPerTick;
    }

    public float GetNectarAmount(int waspCount)
    {
        return Mathf.Clamp(waspCount, 0, maximumGatheringWasps) * nectarPerWaspPerTick;
    }

    public float GetFibreAmount(int waspCount)
    {
        return Mathf.Clamp(waspCount, 0, maximumGatheringWasps) * fibrePerWaspPerTick;
    }

#if UNITY_EDITOR
    public void ConfigureForEditor(
        float tickInterval,
        float preyPerWasp,
        float nectarPerWasp,
        int maximumWasps)
    {
        gatheringTickIntervalSeconds = Mathf.Max(0.1f, tickInterval);
        preyPerWaspPerTick = Mathf.Max(0f, preyPerWasp);
        nectarPerWaspPerTick = Mathf.Max(0f, nectarPerWasp);
        maximumGatheringWasps = Mathf.Max(1, maximumWasps);
    }
#endif
}
