using System;
using UnityEngine;

public class AnimatedUIElement : MonoBehaviour, IUIElement
{
    [SerializeField] private GameObject root;
    [SerializeField] private Animator animator;
    [SerializeField] private string enableTrigger = "Enable";
    [SerializeField] private string disableTrigger = "Disable";
    [SerializeField, Min(0)] private float enableDuration;
    [SerializeField, Min(0)] private float disableDuration;
    private int transition;

    public float EnableDuration => enableDuration;
    public float DisableDuration => disableDuration;
    private GameObject Root => root != null ? root : gameObject;

    public virtual void Enable()
    {
        transition++;
        Root.SetActive(true);
        Trigger(enableTrigger, disableTrigger);
    }

    public virtual void Disable()
    {
        int version = ++transition;
        if (!Root.activeSelf) return;
        Trigger(disableTrigger, enableTrigger);
        HideAfterAnimation(version);
    }

    private void Trigger(string trigger, string reset)
    {
        if (animator == null || !animator.isActiveAndEnabled) return;
        if (!string.IsNullOrEmpty(reset)) animator.ResetTrigger(reset);
        if (!string.IsNullOrEmpty(trigger)) animator.SetTrigger(trigger);
    }

    private async void HideAfterAnimation(int version)
    {
        try
        {
            float end = Time.unscaledTime + disableDuration;
            while (Time.unscaledTime < end)
                await Awaitable.NextFrameAsync(destroyCancellationToken);
            if (version == transition) Root.SetActive(false);
        }
        catch (OperationCanceledException) { }
    }
}
