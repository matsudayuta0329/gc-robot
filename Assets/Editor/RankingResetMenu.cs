using System.IO;
using UnityEditor;
using UnityEngine;

public static class RankingResetMenu
{
    private const string RequestPath = "Logs/reset-ranking.request";

    [MenuItem("Tools/GC Robot/Reset Ranking")]
    public static void ResetRanking()
    {
        ScoreRanking.Reset();
        foreach (var board in Object.FindObjectsByType<RankingBoard>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            board.RefreshDisplay();
        Debug.Log("GC Robot: ランキングデータをリセットしました。");
    }

    // 明示的なリセット要求ファイルがある場合だけ、一度実行して削除する。
    [InitializeOnLoadMethod]
    private static void ApplyPendingRequest()
    {
        EditorApplication.delayCall += () =>
        {
            if (!File.Exists(RequestPath)) return;
            ResetRanking();
            File.Delete(RequestPath);
            File.WriteAllText("Logs/reset-ranking.completed", "Ranking reset completed.");
        };
    }
}
