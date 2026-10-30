using System;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyManager : MonoBehaviour
{
    [SerializeField] private UIParent uiParent;
    [SerializeField] private Player player;
    [SerializeField] private InputReader inputReader;
    [SerializeField] private ConfirmationDialog confirmationDialog;
    [SerializeField] private MessageBox messageBox;
    [SerializeField] private SceneBgm bgm = new SceneBgm();
    private bool ready;
    private bool busy;
    public bool IsBusy => busy || !ready;

    private void Awake() => bgm?.Play(gameObject);

    private async void Start()
    {
        if (uiParent == null || player == null || inputReader == null || confirmationDialog == null || messageBox == null)
        {
            Debug.LogError("LobbyManager: UIParent / Player / InputReader / ConfirmationDialog / MessageBoxを設定してください。", this);
            return;
        }
        var actions = inputReader.GetEnabledActions();
        bool wasPaused = player.IsPaused;
        player.SetPaused(true);
        inputReader.SetEnableAction(Array.Empty<ActionType>());
        try
        {
            confirmationDialog.PrepareEnable();
            messageBox.PrepareEnable();
            await uiParent.Enable(destroyCancellationToken);
            CreateRankingBoard();
            ready = true;
            player.SetPaused(wasPaused);
            inputReader.SetEnableAction(actions);
        }
        catch (OperationCanceledException) { }
        catch (Exception exception) { Debug.LogException(exception, this); }
    }

    private void CreateRankingBoard()
    {
        var boardObject = new GameObject("RankingBoard");
        var forward = Vector3.ProjectOnPlane(player.transform.forward, Vector3.up).normalized;
        boardObject.transform.SetPositionAndRotation(player.transform.position + forward * 2.2f, Quaternion.LookRotation(forward));
        boardObject.AddComponent<RankingBoard>().Initialize(player, inputReader);
    }

    public bool TryBeginRanking()
    {
        if (IsBusy) return false;
        busy = true;
        return true;
    }

    public void EndRanking() => busy = false;

    public bool IsPlayer(Player candidate) => candidate != null && candidate == player;

    public async Awaitable ShowConversationAsync(string message, CancellationToken cancellationToken = default)
    {
        if (IsBusy) return;
        busy = true;
        var actions = inputReader.GetEnabledActions();
        bool wasPaused = player.IsPaused;
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, destroyCancellationToken);
        try
        {
            player.SetPaused(true);
            inputReader.SetEnableAction(Array.Empty<ActionType>());
            await messageBox.Presenter.ShowAndWaitAsync(message, linked.Token);
        }
        finally
        {
            busy = false;
            if (player != null) player.SetPaused(wasPaused);
            if (inputReader != null) inputReader.SetEnableAction(actions);
        }
    }

    public async Awaitable ConfirmAndTravelAsync(string message, string sceneName,
        CancellationToken cancellationToken = default)
    {
        if (IsBusy) return;
        busy = true;
        var actions = inputReader.GetEnabledActions();
        bool wasPaused = player.IsPaused;
        bool sceneLoaded = false;
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, destroyCancellationToken);
        try
        {
            player.SetPaused(true);
            inputReader.SetEnableAction(Array.Empty<ActionType>());
            bool accepted = await confirmationDialog.Presenter.ShowAndWaitAsync(message, linked.Token);
            if (accepted && !string.IsNullOrWhiteSpace(sceneName))
            {
                await uiParent.FinishAsync(linked.Token);
                linked.Token.ThrowIfCancellationRequested();
                await SceneManager.LoadSceneAsync(sceneName);
                sceneLoaded = true;
            }
        }
        finally
        {
            busy = false;
            if (!sceneLoaded)
            {
                if (player != null) player.SetPaused(wasPaused);
                if (inputReader != null) inputReader.SetEnableAction(actions);
            }
        }
    }
}
