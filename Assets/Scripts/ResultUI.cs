using System;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResultUI : AnimatedUIElement
{
    [SerializeField] private TMP_Text countText;
    [SerializeField] private Button continueButton;
    private bool closed;
    private int session;
    private ResultUIPresenter presenter;
    public ResultUIPresenter Presenter => presenter ??= new ResultUIPresenter(this);
    public bool IsConfirmed => closed;

    internal void SetCount(int count)
    {
        if (countText != null) countText.text = $"回収したごみ: {count}個";
    }

    public override void Enable()
    {
        if (IsVisible) return;
        closed = false;
        session++;
        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(Close);
            continueButton.onClick.AddListener(Close);
        }
        base.Enable();
    }

    public void Close()
    {
        if (IsVisible) closed = true;
    }

    // 既存のButtonイベントとの互換性を維持する。
    public void Confirm() => Close();

    public async Awaitable WaitForClose(CancellationToken cancellationToken = default)
    {
        int currentSession = session;
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, destroyCancellationToken);
        while (!closed)
        {
            linked.Token.ThrowIfCancellationRequested();
            if (!IsVisible || !isActiveAndEnabled || currentSession != session)
                throw new OperationCanceledException("ResultUI closed externally.");
            await Awaitable.NextFrameAsync(linked.Token);
        }
        linked.Token.ThrowIfCancellationRequested();
    }
}
