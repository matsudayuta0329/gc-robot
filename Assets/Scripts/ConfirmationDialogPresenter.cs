using System;
using System.Threading;
using UnityEngine;

public sealed class ConfirmationDialogPresenter
{
    private readonly ConfirmationDialog view;
    private bool asking;
    public ConfirmationDialogPresenter(ConfirmationDialog view) => this.view = view;

    public async Awaitable<bool> ShowAndWaitAsync(string message, CancellationToken cancellationToken = default)
    {
        if (asking) throw new InvalidOperationException("ConfirmationDialog is already waiting for an answer.");
        asking = true;
        bool? answer = null;
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, view.destroyCancellationToken);
        try
        {
            view.Init(message, () => answer ??= false, () => answer ??= true);
            view.Enable();
            await view.WaitForTransition(linked.Token);
            while (!answer.HasValue)
            {
                linked.Token.ThrowIfCancellationRequested();
                if (!view.IsVisible || !view.isActiveAndEnabled)
                    throw new OperationCanceledException("ConfirmationDialog disabled.");
                await Awaitable.NextFrameAsync(linked.Token);
            }
            view.Disable();
            await view.WaitForTransition(linked.Token);
            return answer.Value;
        }
        finally
        {
            asking = false;
            if (view != null)
            {
                view.ClearHandlers();
                view.Disable();
            }
        }
    }
}
