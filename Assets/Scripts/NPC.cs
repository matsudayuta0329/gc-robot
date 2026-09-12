using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NPC : MonoBehaviour
{
    [SerializeField, TextArea] private string message;
    [SerializeField] private string moveScene;
    [SerializeField] private GameObject guide;
    [SerializeField] private MessageWindow messageWindow;
    [SerializeField] private InputReader inputReader;
    [SerializeField] private Player player;
    private bool isInvoking;

    private void Awake() => SetGuideEnable(false);
    public void SetGuideEnable(bool enabled)
    {
        if (guide != null) guide.SetActive(enabled && !isInvoking);
    }

    public async Awaitable Invoke()
    {
        if (isInvoking || messageWindow == null || inputReader == null || player == null) return;
        isInvoking = true;
        SetGuideEnable(false);
        var previousActions = inputReader.GetEnabledActions();
        bool wasPaused = player.IsPaused;
        bool sceneLoaded = false;
        bool? answer = null;
        try
        {
            player.SetPaused(true);
            inputReader.SetEnableAction(Array.Empty<ActionType>());
            messageWindow.Init(message, () => answer = false, () => answer = true);
            messageWindow.Enable();
            while (!answer.HasValue && messageWindow != null && isActiveAndEnabled)
                await Awaitable.NextFrameAsync(destroyCancellationToken);
            if (messageWindow != null) messageWindow.Disable();
            float end = Time.unscaledTime + (messageWindow != null ? messageWindow.DisableDuration : 0);
            while (Time.unscaledTime < end)
                await Awaitable.NextFrameAsync(destroyCancellationToken);
            if (answer == true && !string.IsNullOrWhiteSpace(moveScene))
            {
                await SceneManager.LoadSceneAsync(moveScene);
                sceneLoaded = true;
            }
        }
        finally
        {
            isInvoking = false;
            if (messageWindow != null) messageWindow.Disable();
            if (!sceneLoaded)
            {
                if (inputReader != null) inputReader.SetEnableAction(previousActions);
                if (player != null) player.SetPaused(wasPaused);
            }
        }
    }
}
