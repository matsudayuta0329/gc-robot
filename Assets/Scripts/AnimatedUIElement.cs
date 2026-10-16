using System;
using System.Threading;
using UnityEngine;

public class AnimatedUIElement : MonoBehaviour, IUIElement
{
    [SerializeField] private GameObject root;
    [SerializeField] private Animator animator;
    [SerializeField] private AnimationClip enableAnimation;
    [SerializeField] private AnimationClip disableAnimation;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField, Min(1)] private float animationTimeout = 30f;
    private int version;
    private bool enabling;
    private bool disabling;
    private AnimatorOverrideController overrideController;
    public bool IsVisible { get; private set; }
    protected GameObject Root => root != null ? root : gameObject;
    private bool HasAnimator => animator != null && animator.runtimeAnimatorController != null;

    protected virtual void Awake() => ConfigureAnimator();

    private void ConfigureAnimator()
    {
        if (overrideController != null || !HasAnimator) return;
        overrideController = UIAnimatorOverrides.Create(animator,
            ("Enable", enableAnimation), ("Disable", disableAnimation));
    }

    protected virtual void OnDestroy()
    {
        if (overrideController != null) Destroy(overrideController);
    }

    public virtual void PrepareEnable()
    {
        version++;
        enabling = disabling = false;
        IsVisible = false;
        Root.SetActive(true);
        ConfigureAnimator();
        SetInteraction(false);
        if (HasAnimator)
        {
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            animator.ResetTrigger("enable");
            animator.ResetTrigger("disable");
            animator.Play("Base Layer.BedoreEnable", 0, 0f);
            // 初回描画より前にEnableの先頭姿勢を評価する。
            animator.Update(0f);
        }
        else if (canvasGroup != null) canvasGroup.alpha = 0f;
    }

    public virtual void Enable()
    {
        if (IsVisible && !disabling) return;
        PrepareEnable();
        IsVisible = true;
        enabling = true;
        if (HasAnimator) animator.SetTrigger("enable");
        else
        {
            if (canvasGroup != null) canvasGroup.alpha = 1f;
            CompleteEnable();
        }
    }

    public virtual void Disable()
    {
        if (disabling) return;
        version++;
        enabling = false;
        IsVisible = false;
        SetInteraction(false);
        if (!Root.activeSelf) return;
        disabling = true;
        if (HasAnimator)
        {
            animator.ResetTrigger("enable");
            animator.SetTrigger("disable");
        }
        else CompleteDisable();
    }

    // Animatorと同じGameObjectにこのコンポーネントを配置する。
    public void CompleteEnable()
    {
        if (!enabling) return;
        enabling = false;
        SetInteraction(true);
    }

    public void CompleteDisable()
    {
        if (!disabling) return;
        disabling = false;
        Root.SetActive(false);
    }

    public async Awaitable WaitForTransition(CancellationToken cancellationToken = default)
    {
        int expectedVersion = version;
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, destroyCancellationToken);
        float deadline = Time.unscaledTime + animationTimeout;
        while (enabling || disabling)
        {
            linked.Token.ThrowIfCancellationRequested();
            if (expectedVersion != version) throw new OperationCanceledException("UI transition replaced.");
            if (Time.unscaledTime >= deadline)
                throw new InvalidOperationException(name + ": Animation Eventの完了通知がありません。");
            await Awaitable.NextFrameAsync(linked.Token);
        }
        linked.Token.ThrowIfCancellationRequested();
    }

    private void SetInteraction(bool enabled)
    {
        if (canvasGroup == null) return;
        canvasGroup.interactable = enabled;
        canvasGroup.blocksRaycasts = enabled;
    }
}
