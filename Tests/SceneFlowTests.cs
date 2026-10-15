using System;
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

// 完全なプロジェクトコピーで実行するシーン結合テスト。
public class SceneFlowTests
{
    private static IEnumerator Until(Func<bool> condition, float seconds = 12f)
    {
        float end = Time.realtimeSinceStartup + seconds;
        while (!condition())
        {
            Assert.Less(Time.realtimeSinceStartup, end, "Scene flow timed out");
            yield return null;
        }
    }

    [UnityTest]
    public IEnumerator GameResultGateAndTutorialReturnToLobby()
    {
        void ShortenGame(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "Game") return;
            var manager = UnityEngine.Object.FindFirstObjectByType<GameManager>();
            var fields = new SerializedObject(manager);
            fields.FindProperty("limit").floatValue = 0.1f;
            fields.ApplyModifiedPropertiesWithoutUndo();
        }
        SceneManager.sceneLoaded += ShortenGame;
        try
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/Scenes/Game.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return Until(() =>
            {
                var result = UnityEngine.Object.FindFirstObjectByType<ResultUI>();
                return result != null && result.IsVisible;
            });
            UnityEngine.Object.FindFirstObjectByType<ResultUI>().Close();
            yield return Until(() => SceneManager.GetActiveScene().name == "Lobby");
            yield return Until(() => !UnityEngine.Object.FindFirstObjectByType<LobbyManager>().IsBusy);

            var gate = UnityEngine.Object.FindFirstObjectByType<Gate>();
            Assert.IsNotNull(gate);
            var player = UnityEngine.Object.FindFirstObjectByType<Player>();
            var controller = player.GetComponent<CharacterController>();
            controller.enabled = false;
            player.transform.position = gate.GetComponent<Collider>().bounds.center;
            controller.enabled = true;
            Physics.SyncTransforms();
            controller.Move(Vector3.forward * 0.01f);
            yield return Until(() =>
            {
                var popup = UnityEngine.Object.FindFirstObjectByType<MessageWindow>();
                return popup != null && popup.IsVisible;
            });
            Assert.IsTrue(player.IsPaused);
            var popup = UnityEngine.Object.FindFirstObjectByType<MessageWindow>();
            var popupFields = new SerializedObject(popup);
            ((Button)popupFields.FindProperty("submitButton").objectReferenceValue).onClick.Invoke();
            yield return Until(() => SceneManager.GetActiveScene().name == "Game");
        }
        finally { SceneManager.sceneLoaded -= ShortenGame; }

        yield return SceneManager.LoadSceneAsync("Tutorial");
        yield return new WaitForSecondsRealtime(1f);
        float deadline = Time.realtimeSinceStartup + 15f;
        while (SceneManager.GetActiveScene().name == "Tutorial")
        {
            Assert.Less(Time.realtimeSinceStartup, deadline, "Tutorial did not complete");
            UnityEngine.Object.FindFirstObjectByType<TutorialManager>().Advance();
            yield return new WaitForSecondsRealtime(0.5f);
        }
        Assert.AreEqual("Lobby", SceneManager.GetActiveScene().name);
    }
}
