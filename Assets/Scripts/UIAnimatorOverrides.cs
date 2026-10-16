using System;
using System.Collections.Generic;
using UnityEngine;

// 共通ControllerのState/Transitionを維持し、各UIのClipだけを起動時に差し替える。
public static class UIAnimatorOverrides
{
    public static AnimatorOverrideController Create(Animator animator,
        params (string originalName, AnimationClip replacement)[] clips)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return null;

        bool hasReplacement = false;
        foreach (var clip in clips)
            hasReplacement |= clip.replacement != null;
        if (!hasReplacement) return null;

        var baseController = animator.runtimeAnimatorController is AnimatorOverrideController current
            ? current.runtimeAnimatorController : animator.runtimeAnimatorController;
        var overrides = new AnimatorOverrideController(baseController);
        var entries = new List<KeyValuePair<AnimationClip, AnimationClip>>(overrides.overridesCount);
        overrides.GetOverrides(entries);
        foreach (var clip in clips)
        {
            if (clip.replacement == null) continue;
            int index = entries.FindIndex(entry => entry.Key != null && entry.Key.name == clip.originalName);
            if (index < 0)
            {
                UnityEngine.Object.Destroy(overrides);
                throw new InvalidOperationException($"{animator.name}: 共通Controllerに{clip.originalName}がありません。");
            }
            entries[index] = new KeyValuePair<AnimationClip, AnimationClip>(entries[index].Key, clip.replacement);
        }
        overrides.ApplyOverrides(entries);
        animator.runtimeAnimatorController = overrides;
        return overrides;
    }
}
