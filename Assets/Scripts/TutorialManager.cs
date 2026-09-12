using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TutorialManager : MonoBehaviour
{
    [Serializable]
    public class TutorialData
    {
        public bool isTutorial;
        [TextArea] public string message;
        public ActionType tutorialAction;
    }

    [SerializeField] private List<TutorialData> tutorialData = new List<TutorialData>();
    [SerializeField] private TutorialUI tutorialUI;
    [SerializeField] private InputReader inputReader;
    [SerializeField] private Player player;
    [SerializeField] private string lobbyScene;
    private bool nextRequested;
    private bool isExperience;
    private ActionType currentAction;

    private async void Start()
    {
        if (tutorialUI == null || tutorialUI.MessageWindow == null || tutorialUI.OperationGuide == null || inputReader == null)
        {
            Debug.LogError("TutorialManager: TutorialUIと子UI、InputReaderを設定してください。", this);
            return;
        }
        inputReader.ActionPerformed += OnActionPerformed;
        try
        {
            inputReader.SetEnableAction(Array.Empty<ActionType>());
            if (player != null) player.SetPaused(true);
            tutorialUI.Enable();
            await WaitRealtime(tutorialUI.EnableDuration);
            foreach (var data in tutorialData)
            {
                if (data == null) continue;
                nextRequested = false;
                isExperience = data.isTutorial;
                currentAction = data.tutorialAction;
                if (data.isTutorial)
                {
                    tutorialUI.MessageWindow.Disable();
                    tutorialUI.OperationGuide.Enable();
                    tutorialUI.OperationGuide.SetText(data.message);
                    var types = new[] { ActionType.Move, data.tutorialAction };
                    tutorialUI.OperationGuide.SetGuideEnable(types);
                    if (player != null) player.SetPaused(false);
                    inputReader.SetEnableAction(types);
                }
                else
                {
                    inputReader.SetEnableAction(Array.Empty<ActionType>());
                    if (player != null) player.SetPaused(true);
                    tutorialUI.OperationGuide.SetGuideEnable(Array.Empty<ActionType>());
                    tutorialUI.OperationGuide.Disable();
                    tutorialUI.MessageWindow.Enable();
                    tutorialUI.MessageWindow.Init(string.Empty, null, Advance);
                    await tutorialUI.MessageWindow.SetText(data.message);
                }
                while (!nextRequested)
                    await Awaitable.NextFrameAsync(destroyCancellationToken);
            }
            inputReader.SetEnableAction(Array.Empty<ActionType>());
            if (player != null) player.SetPaused(true);
            tutorialUI.Disable();
            await WaitRealtime(tutorialUI.DisableDuration);
            if (!string.IsNullOrWhiteSpace(lobbyScene)) await SceneManager.LoadSceneAsync(lobbyScene);
        }
        catch (OperationCanceledException) { }
        finally
        {
            if (inputReader != null) inputReader.ActionPerformed -= OnActionPerformed;
        }
    }

    public void Advance()
    {
        if (!isExperience && tutorialUI.MessageWindow.IsTyping)
            tutorialUI.MessageWindow.DisplayFullText();
        else nextRequested = true;
    }

    // 操作体験は対応入力の実行、または外部判定からのAdvanceで完了。
    private void OnActionPerformed(ActionType action)
    {
        if (isExperience && action == currentAction) nextRequested = true;
    }

    private async Awaitable WaitRealtime(float duration)
    {
        float end = Time.unscaledTime + duration;
        while (Time.unscaledTime < end)
            await Awaitable.NextFrameAsync(destroyCancellationToken);
    }
}
