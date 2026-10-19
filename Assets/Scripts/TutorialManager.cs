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
    private float forwardTravel;
    private float backwardTravel;
    private float turnTravel;
    private int collectedBeforeExperience;
    private bool dashStarted;
    private GarbageCounter garbageCounter;

    private async void Start()
    {
        if (tutorialUI == null || tutorialUI.MessageBox == null || tutorialUI.OperationGuide == null || inputReader == null || uiParent == null || player == null)
        {
            Debug.LogError("TutorialManager: TutorialUIと子UI、InputReader、UIParent、Playerを設定してください。", this);
            return;
        }
        inputReader.ActionPerformed += OnActionPerformed;
        var presenter = tutorialUI.Presenter;
        garbageCounter = player.GetComponent<GarbageCounter>();
        presenter.Bind(garbageCounter);
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
                forwardTravel = 0f;
                backwardTravel = 0f;
                turnTravel = 0f;
                dashStarted = false;
                collectedBeforeExperience = garbageCounter != null ? garbageCounter.CollectedCount : 0;
                if (data.isTutorial)
                {
                    presenter.Message.Hide();
                    var types = new[] { ActionType.Move, data.tutorialAction };
                    presenter.Guide.Show(data.message, new[] { data.tutorialAction });
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
                {
                    await Awaitable.NextFrameAsync(destroyCancellationToken);
                    if (isExperience) UpdateExperience();
                }
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
        if (isExperience) return;
        if (tutorialUI.Presenter.Message.IsTyping)
            tutorialUI.Presenter.Message.DisplayFullText();
        else nextRequested = true;
    }

    private void UpdateExperience()
    {
        switch (currentAction)
        {
            case ActionType.Move:
                var move = inputReader.GetMoveInput();
                forwardTravel += Mathf.Max(0f, move.y) * Time.deltaTime;
                backwardTravel += Mathf.Max(0f, -move.y) * Time.deltaTime;
                turnTravel += Mathf.Abs(move.x) * Time.deltaTime;
                nextRequested = forwardTravel >= 0.2f && backwardTravel >= 0.2f && turnTravel >= 0.2f;
                break;
            case ActionType.Vacuum:
                nextRequested = garbageCounter != null &&
                    garbageCounter.CollectedCount > collectedBeforeExperience;
                break;
            case ActionType.Dash:
                nextRequested = dashStarted && player != null && !player.IsDashing;
                break;
        }
    }

    // ダッシュは入力時に始まり、終了後に次へ進む。
    private void OnActionPerformed(ActionType action)
    {
        if (!isExperience || action != currentAction) return;
        if (action == ActionType.Dash) dashStarted = true;
        else if (action != ActionType.Move && action != ActionType.Vacuum) nextRequested = true;
    }

}
