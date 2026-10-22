using UnityEngine;
using System;

public class GarbageCounter: MonoBehaviour
{
    public int count{get;private set;} = 0;
    public event Action<int> OnUpdateCount;
    public int CollectedCount {get{return count;}}

    public void RecordCollection(int score)
    {
        CountUp(Mathf.Clamp(score, 1, 3));
    }

    public void ResetCount()
    {
        count = 0;
        OnUpdateCount?.Invoke(count);
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
