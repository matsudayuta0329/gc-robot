using UnityEngine;

public class NPC : MonoBehaviour
{
    [SerializeField, TextArea] private string message;
    [SerializeField] private GameObject guide;
    [SerializeField] private LobbyManager lobbyManager;
    private bool isInvoking;

    private void Awake() => SetGuideEnable(false);
    public void SetGuideEnable(bool enabled)
    {
        if (guide != null)
            guide.SetActive(enabled && !isInvoking && lobbyManager != null && !lobbyManager.IsBusy);
    }

    public async Awaitable Invoke()
    {
        if (isInvoking || lobbyManager == null || lobbyManager.IsBusy) return;
        isInvoking = true;
        SetGuideEnable(false);
        try { await lobbyManager.ShowConversationAsync(message, destroyCancellationToken); }
        finally { isInvoking = false; }
    }
}
