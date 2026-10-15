using System;
using System.Threading;
using UnityEngine;

public sealed class MessageWindowPresenter
{
    private readonly MessageWindow view;
    private bool asking;
    public MessageWindowPresenter(MessageWindow view) => this.view = view;
    public bool IsTyping => view.IsTyping;
    public void DisplayFullText() => view.DisplayFullText();
    public void Hide() => view.Disable();
    public async Awaitable ShowMessage(string message, Action advance)
    {
        view.Init(string.Empty, null, advance);
        view.Enable();
        await view.SetText(message);
    }

    // asyncメソッドはoutを使えないため、回答をAwaitable<bool>で返す。
    public async Awaitable<bool> ShowAndWaitAsync(string message, CancellationToken cancellationToken = default)
    {
        if (asking) throw new InvalidOperationException("MessageWindow is already waiting for an answer.");
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
                if (!view.IsVisible || !view.isActiveAndEnabled)
                    throw new OperationCanceledException("MessageWindow disabled.");
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
