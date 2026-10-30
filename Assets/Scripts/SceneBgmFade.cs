using UnityEngine;

/// <summary>UIParentの遷移進行に合わせて、BGMの音量だけを変化させる。</summary>
public sealed class SceneBgmFade : MonoBehaviour
{
    private AudioSource source;
    private UIParent sceneUI;
    private float maximumVolume;
    private float fallbackProgress;

    public void Initialize(AudioSource audioSource, float volume, UIParent parent)
    {
        source = audioSource;
        maximumVolume = Mathf.Clamp01(volume);
        sceneUI = parent;
        fallbackProgress = 0f;
        if (source != null) source.volume = 0f;
    }

    private void LateUpdate()
    {
        if (source == null) return;
        fallbackProgress = Mathf.MoveTowards(fallbackProgress, 1f, Time.unscaledDeltaTime / 0.5f);
        float progress = sceneUI != null ? sceneUI.SoundEffectVolume : fallbackProgress;
        source.volume = maximumVolume * Mathf.SmoothStep(0f, 1f, progress);
    }
}
