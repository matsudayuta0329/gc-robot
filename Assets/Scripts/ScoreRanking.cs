using System;
using System.Collections.Generic;
using UnityEngine;

public static class ScoreRanking
{
    private const string SaveKey = "GcRobot.ScoreRanking.v1";
    [Serializable] public class Entry { public string name; public int count; }
    [Serializable] private class SaveData { public List<Entry> entries = new List<Entry>(); }
    public static IReadOnlyList<Entry> Entries => Load().entries;
    public static int GetRank(int count)
    {
        int rank = 1;
        foreach (var entry in Load().entries) if (entry.count > count) rank++;
        return rank;
    }
    public static int Submit(string playerName, int count)
    {
        var data = Load();
        data.entries.Add(new Entry { name = string.IsNullOrWhiteSpace(playerName) ? "Player" : playerName.Trim(), count = Mathf.Max(0, count) });
        data.entries.Sort((a, b) => b.count.CompareTo(a.count));
        if (data.entries.Count > 10) data.entries.RemoveRange(10, data.entries.Count - 10);
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
        return GetRank(count);
    }
    public static void Reset()
    {
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.Save();
    }

    private static SaveData Load()
    {
        if (!PlayerPrefs.HasKey(SaveKey)) return new SaveData();
        try { return JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(SaveKey)) ?? new SaveData(); }
        catch { return new SaveData(); }
    }
}
