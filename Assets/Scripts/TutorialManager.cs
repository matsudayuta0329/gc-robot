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
        public List<string> messages = new List<string>();
        public ActionType tutorialAction;
    }

    [Serializable]
    public class TrashEntry
    {
        public GameObject prefab;
        [Range(1, 3)] public int size = 1;
    }

    [SerializeField] private List<TutorialData> tutorialData = new List<TutorialData>();
    [SerializeField] private List<TrashEntry> trashTypes = new List<TrashEntry>();
    [SerializeField, Min(1)] private int dashTrashCount = 10;
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
    private bool dashObserved;
    private GarbageCounter garbageCounter;
    private readonly List<Garbage> activeWave = new List<Garbage>();

    private async void Start()
    {
        if (tutorialUI == null || tutorialUI.MessageBox == null || tutorialUI.OperationGuide == null || inputReader == null || uiParent == null || player == null)
        {
            Debug.LogError("TutorialManager: TutorialUIと子UI、InputReader、UIParent、Playerを設定してください。", this);
            return;
        }
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
                isExperience = data.isTutorial;
                currentAction = data.tutorialAction;
                forwardTravel = 0f;
                backwardTravel = 0f;
                turnTravel = 0f;
                dashObserved = false;
                if (data.isTutorial)
                {
                    presenter.Message.Hide();
                    if (currentAction == ActionType.Vacuum) SpawnWave(false);
                    else if (currentAction == ActionType.Dash) SpawnWave(true);
                    var types = new[] { ActionType.Move, data.tutorialAction };
                    presenter.Guide.Show(data.message, new[] { data.tutorialAction });
                    player.SetPaused(false);
                    inputReader.SetEnableAction(types);
                    nextRequested = false;
                    while (!nextRequested)
                    {
                        await Awaitable.NextFrameAsync(destroyCancellationToken);
                        UpdateExperience();
                    }
                }
                else
                {
                    inputReader.SetEnableAction(Array.Empty<ActionType>());
                    player.SetPaused(true);
                    presenter.Guide.Hide();
                    if (data.messages != null && data.messages.Count > 0)
                    {
                        foreach (var line in data.messages)
                            await ShowLine(line, presenter);
                    }
                    else await ShowLine(data.message, presenter);
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
            presenter.Dispose();
            if (player != null) player.SetPaused(true);
            if (inputReader != null) inputReader.SetEnableAction(Array.Empty<ActionType>());
        }
    }

    private async Awaitable ShowLine(string line, TutorialUIPresenter presenter)
    {
        if (string.IsNullOrWhiteSpace(line)) return;
        nextRequested = false;
        await presenter.Message.ShowMessage(line, Advance);
        while (!nextRequested)
            await Awaitable.NextFrameAsync(destroyCancellationToken);
        presenter.Message.Hide();
        await tutorialUI.MessageBox.WaitForTransition(destroyCancellationToken);
    }

    private void SpawnWave(bool forDash)
    {
        activeWave.Clear();
        if (trashTypes.Count == 0) return;

        Vector3 forward = Vector3.ProjectOnPlane(player.transform.forward, Vector3.up).normalized;
        Vector3 right = Vector3.Cross(Vector3.up, forward);
        int count = forDash ? dashTrashCount : trashTypes.Count;
        for (int i = 0; i < count; i++)
        {
            var entry = trashTypes[i % trashTypes.Count];
            if (entry == null || entry.prefab == null) continue;
            Vector3 offset = forDash
                ? forward * (2f + i * 1.2f) + right * (i % 2 == 0 ? 0.5f : -0.5f)
                : Quaternion.Euler(0f, i * 360f / count, 0f) * forward * 3f;
            var instance = Instantiate(entry.prefab, player.transform.position + offset, Quaternion.identity);
            if (!instance.TryGetComponent(out Garbage garbage))
            {
                Destroy(instance);
                continue;
            }
            garbage.Init(entry.size);
            activeWave.Add(garbage);
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
                nextRequested = activeWave.Count > 0 &&
                    activeWave.TrueForAll(garbage => garbage == null || garbage.IsCollected);
                break;
            case ActionType.Dash:
                if (player.IsDashing) dashObserved = true;
                nextRequested = dashObserved && !player.IsDashing;
                break;
        }
    }
}
