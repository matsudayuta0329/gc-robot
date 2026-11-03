using UnityEngine;
using unityroom.Api;

public static class UnityroomScoreSender
{
    public static bool SendScore(int boardNo, int score)
    {
        if (Application.platform != RuntimePlatform.WebGLPlayer || boardNo < 1) return false;
        var client = Object.FindFirstObjectByType<UnityroomApiClient>();
        if (client == null)
        {
            var prefab = Resources.Load<UnityroomApiClient>("UnityroomRankingClient");
            if (prefab == null)
            {
                Debug.LogError("UnityroomRankingClientプレハブが見つかりません。");
                return false;
            }
            client = Object.Instantiate(prefab);
        }

        // SDKはfloatを受け取るが、ゲームから渡す値は整数の回収スコア。
        client.SendScore(boardNo, score, ScoreboardWriteMode.HighScoreDesc);
        return true;
    }
}
