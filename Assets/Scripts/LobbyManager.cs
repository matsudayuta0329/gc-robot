using System;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyManager : MonoBehaviour
{
    [SerializeField] private UIParent uiParent;
    [SerializeField] private Player player;
    [SerializeField] private InputReader inputReader;
    [SerializeField] private MessageWindow popup;
    private bool ready;
    private bool busy;
    public bool IsBusy => busy || !ready;

    private async void Start()
    {
        if (uiParent == null || player == null || inputReader == null || popup == null)
        {
            Debug.LogError("LobbyManager: UIParent / Player / InputReader / Popupを設定してください。", this);
            return;
        }
        var actions = inputReader.GetEnabledActions();
        bool wasPaused = player.IsPaused;
        player.SetPaused(true);
        inputReader.SetEnableAction(Array.Empty<ActionType>());
        try
        {
            popup.PrepareEnable();
            await uiParent.Enable(destroyCancellationToken);
            ready = true;
            player.SetPaused(wasPaused);
            inputReader.SetEnableAction(actions);
        }
        catch (OperationCanceledException) { }
        catch (Exception exception) { Debug.LogException(exception, this); }
    }

    public bool IsPlayer(Player candidate) => candidate != null && candidate == player;

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
            bool accepted = await popup.Presenter.ShowAndWaitAsync(message, linked.Token);
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
