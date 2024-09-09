using UnityEngine;
using Unity.MLAgents;

public class CustomLogger : MonoBehaviour
{
    private StatsRecorder statsRecorder;

    private void Start()
    {
        statsRecorder = Academy.Instance.StatsRecorder;
    }

    public void LogReward(float reward, string agentName)
    {
        statsRecorder.Add(agentName + "/Reward", reward);
    }

    public void EndEpisode(float cumulativeReward, string agentName)
    {
        statsRecorder.Add(agentName + "/CumulativeReward", cumulativeReward);
    }
}