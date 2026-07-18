using UnityEngine;
using System;

public class GarbageCounter: MonoBehaviour
{
    public int count{get;private set;} = 0;
    public event Action<int> OnUpdateCount;

    public void CoutUp()
    {
        count++;
        OnUpdateCount?.Invoke(count);
    }

    public void CountDown()
    {
        count--;
        OnUpdateCount?.Invoke(count);
    }
}