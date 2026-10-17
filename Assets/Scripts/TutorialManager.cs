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
    [SerializeField] private UIParent uiParent;
    [SerializeField] private InputReader inputReader;
    [SerializeField] private Player player;
    [SerializeField] private string lobbyScene;
    private bool nextRequested;
    private bool isExperience;
    private ActionType currentAction;

    private async void Start()
    {
        if (tutorialUI == null || tutorialUI.MessageBox == null || tutorialUI.OperationGuide == null || inputReader == null || uiParent == null)
        {
            Debug.LogError("TutorialManager: TutorialUIと子UI、InputReaderを設定してください。", this);
            return;
        }
        inputReader.ActionPerformed += OnActionPerformed;
        var presenter = tutorialUI.Presenter;
        if (player != null) presenter.Bind(player.GetComponent<GarbageCounter>());
        try
        {
            inputReader.SetEnableAction(Array.Empty<ActionType>());
            if (player != null) player.SetPaused(true);
            await uiParent.Enable(destroyCancellationToken);
            foreach (var data in tutorialData)
            {
                if (data == null) continue;
                nextRequested = false;
                isExperience = data.isTutorial;
                currentAction = data.tutorialAction;
                if (data.isTutorial)
                {
                    presenter.Message.Hide();
                    var types = new[] { ActionType.Move, data.tutorialAction };
                    presenter.Guide.Show(data.message, types);
                    if (player != null) player.SetPaused(false);
                    inputReader.SetEnableAction(types);
                }
                else
                {
                    inputReader.SetEnableAction(Array.Empty<ActionType>());
                    if (player != null) player.SetPaused(true);
                    presenter.Guide.Hide();
                    await presenter.Message.ShowMessage(data.message, Advance);
                }
                while (!nextRequested)
                    await Awaitable.NextFrameAsync(destroyCancellationToken);
            }
            inputReader.SetEnableAction(Array.Empty<ActionType>());
            if (player != null) player.SetPaused(true);
            presenter.Message.Hide();
            presenter.Guide.Hide();
            await uiParent.FinishAsync(destroyCancellationToken);
            if (!string.IsNullOrWhiteSpace(lobbyScene)) await SceneManager.LoadSceneAsync(lobbyScene);
        }
        catch (OperationCanceledException) { }
        catch (Exception exception) { Debug.LogException(exception, this); }
        finally
        {
            if (inputReader != null) inputReader.ActionPerformed -= OnActionPerformed;
            presenter.Dispose();
            if (player != null) player.SetPaused(true);
            if (inputReader != null) inputReader.SetEnableAction(Array.Empty<ActionType>());
        }
    }

    public void Advance()
    {
        if (!isExperience && tutorialUI.Presenter.Message.IsTyping)
            tutorialUI.Presenter.Message.DisplayFullText();
        else nextRequested = true;
    }

    // 操作体験は対応入力の実行、または外部判定からのAdvanceで完了。
    private void OnActionPerformed(ActionType action)
    {
        if (isExperience && action == currentAction) nextRequested = true;
    }

}
