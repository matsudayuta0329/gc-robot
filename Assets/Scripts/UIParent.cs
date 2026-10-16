using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

// UIの演出順のみを管理する。ゲームデータや遷移先シーンは保持しない。
public class UIParent : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private AnimationClip startGameAnimation;
    [SerializeField] private AnimationClip endGameAnimation;
    [SerializeField] private AnimationClip nextSceneAnimation;
    [SerializeField] private List<AnimatedUIElement> elements = new List<AnimatedUIElement>();
    [SerializeField] private ResultUI resultUI;
    [SerializeField, Min(1)] private float animationTimeout = 30f;
    private string phase;
    private bool animationCompleted;
    private bool resultShown;
    private bool busy;
    private AnimatorOverrideController overrideController;
    private bool HasAnimator => animator != null && animator.runtimeAnimatorController != null;

    private void Awake() => ConfigureAnimator();

    private void ConfigureAnimator()
    {
        if (overrideController != null || !HasAnimator) return;
        overrideController = UIAnimatorOverrides.Create(animator,
            ("StartGame", startGameAnimation), ("EndGame", endGameAnimation),
            ("NextScene", nextSceneAnimation));
    }

    private void OnDestroy()
    {
        if (overrideController != null) Destroy(overrideController);
    }

    public async Awaitable Enable(CancellationToken cancellationToken = default)
    {
        if (busy) throw new InvalidOperationException("UIParent is busy.");
        busy = true;
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, destroyCancellationToken);
        try
        {
            foreach (var element in elements)
                if (element != null && element != resultUI) element.PrepareEnable();
            if (resultUI != null) resultUI.PrepareEnable();
            await Play("startGame", linked.Token);
        }
        finally { phase = null; busy = false; }
    }

    public async Awaitable FinishAsync(CancellationToken cancellationToken = default)
    {
        if (busy) throw new InvalidOperationException("UIParent is busy.");
        busy = true;
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, destroyCancellationToken);
        try
        {
            if (resultUI != null)
            {
                resultShown = false;
                await Play("endGame", linked.Token);
                if (!resultShown)
                    throw new InvalidOperationException("endGameにEnableResultのAnimation Eventを設定してください。");
                await resultUI.WaitForClose(linked.Token);
            }
            // ResultUIなしの場合はendGameを経由せず、直接遷移演出へ進む。
            await Play("nextScene", linked.Token);
        }
        finally { phase = null; busy = false; }
    }

    private async Awaitable Play(string trigger, CancellationToken token)
    {
        ConfigureAnimator();
        token.ThrowIfCancellationRequested();
        phase = trigger;
        animationCompleted = false;
        if (!HasAnimator)
        {
            if (trigger == "startGame") EnableElements();
            else if (trigger == "endGame") { DisableElements(); EnableResult(); }
            else { DisableElements(); DisableResult(); }
            animationCompleted = true;
        }
        else
        {
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            animator.ResetTrigger("startGame");
            animator.ResetTrigger("endGame");
            animator.ResetTrigger("nextScene");
            animator.SetTrigger(trigger);
        }
        float deadline = Time.unscaledTime + animationTimeout;
        while (!animationCompleted)
        {
            if (!isActiveAndEnabled) throw new OperationCanceledException("UIParent disabled.");
            if (Time.unscaledTime >= deadline)
                throw new InvalidOperationException(name + ": " + trigger + "の完了イベントがありません。");
            await Awaitable.NextFrameAsync(token);
        }
        foreach (var element in elements)
            if (element != null && element != resultUI) await element.WaitForTransition(token);
        if (resultUI != null) await resultUI.WaitForTransition(token);
    }

    // 以下はParentのAnimation Eventから呼ぶ。
    public void EnableElements()
    {
        if (phase != "startGame") return;
        foreach (var element in elements)
            if (element != null && element != resultUI) element.Enable();
    }

    public void DisableElements()
    {
        if (phase != "endGame" && phase != "nextScene") return;
        foreach (var element in elements)
            if (element != null && element != resultUI) element.Disable();
    }

    public void EnableResult()
    {
        if (phase != "endGame" || resultShown || resultUI == null) return;
        resultShown = true;
        resultUI.Enable();
    }

    public void DisableResult()
    {
        if (phase == "nextScene" && resultUI != null) resultUI.Disable();
    }

    public void CompleteStart() { if (phase == "startGame") animationCompleted = true; }
    public void CompleteEnd() { if (phase == "endGame") animationCompleted = true; }
    public void CompleteNextScene() { if (phase == "nextScene") animationCompleted = true; }
}
