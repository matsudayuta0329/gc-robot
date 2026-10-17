using System;
using System.Threading;
using UnityEngine;

public sealed class MessageBoxPresenter
{
    private readonly MessageBox view;
    private bool waiting;
    public MessageBoxPresenter(MessageBox view) => this.view = view;
    public bool IsTyping => view.IsTyping;
    public void DisplayFullText() => view.DisplayFullText();
    public void Hide() => view.Disable();

    public async Awaitable ShowMessage(string message, Action advance)
    {
        view.SetAdvanceHandler(advance);
        view.Enable();
        await view.SetText(message);
    }

    public async Awaitable ShowAndWaitAsync(string message, CancellationToken cancellationToken = default)
    {
        if (waiting) throw new InvalidOperationException("MessageBox is already waiting.");
        waiting = true;
        bool completed = false;
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, view.destroyCancellationToken);
        try
        {
            view.SetAdvanceHandler(() =>
            {
                if (view.IsTyping) view.DisplayFullText();
                else completed = true;
            });
            view.Enable();
            await view.SetText(message, linked.Token);
            await view.WaitForTransition(linked.Token);
            while (!completed)
            {
                linked.Token.ThrowIfCancellationRequested();
                if (!view.IsVisible || !view.isActiveAndEnabled)
                    throw new OperationCanceledException("MessageBox disabled.");
                await Awaitable.NextFrameAsync(linked.Token);
            }
            view.Disable();
            await view.WaitForTransition(linked.Token);
        }
        finally
        {
            waiting = false;
            if (view != null)
            {
                view.SetAdvanceHandler(null);
                view.Disable();
            }
        }
    }
}
