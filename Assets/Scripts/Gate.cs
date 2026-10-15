using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Gate : MonoBehaviour
{
    [SerializeField] private LobbyManager lobbyManager;
    [SerializeField, TextArea] private string message = "移動しますか？";
    [SerializeField] private string moveScene;
    private readonly HashSet<Collider> occupants = new HashSet<Collider>();

    private void OnTriggerEnter(Collider other)
    {
        var player = other.GetComponentInParent<Player>();
        if (lobbyManager == null || !lobbyManager.IsPlayer(player)) return;
        bool first = occupants.Count == 0;
        occupants.Add(other);
        if (first && !lobbyManager.IsBusy) Enter();
    }

    private void OnTriggerExit(Collider other) => occupants.Remove(other);
    private void OnDisable() => occupants.Clear();

    private async void Enter()
    {
        try { await lobbyManager.ConfirmAndTravelAsync(message, moveScene, destroyCancellationToken); }
        catch (OperationCanceledException) { }
        catch (Exception exception) { Debug.LogException(exception, this); }
    }

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }
}
