using UnityEngine;
using System;

public class GarbageCounter: MonoBehaviour
{
    public int count{get;private set;} = 0;
    public event Action<int> OnUpdateCount;
    public int CollectedCount { get; private set; }
    public int Score { get; private set; }
    public event Action<int> OnUpdateScore;

    public void RecordCollection(int score)
    {
        CollectedCount++;
        Score += Mathf.Clamp(score, 1, 3);
        CountUp(1);
        OnUpdateScore?.Invoke(Score);
    }

    public void ResetCount()
    {
        count = 0;
        CollectedCount = 0;
        Score = 0;
        OnUpdateCount?.Invoke(count);
        OnUpdateScore?.Invoke(Score);
    }

    public void CountUp(int amount)
    {
        count += amount;
        OnUpdateCount?.Invoke(count);
    }

    public void CountDown(int amount)
    {
        count -= amount;
        OnUpdateCount?.Invoke(count);
    }
}
