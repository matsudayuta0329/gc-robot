using System;
using UnityEngine;
using UnityEngine.Audio;

[Serializable]
public sealed class SceneBgm
{
    [SerializeField] private AudioClip clip;
    [SerializeField, Range(0f, 1f)] private float volume = 0.5f;
    [SerializeField] private AudioMixerGroup output;

    public void Play(GameObject owner)
    {
        if (owner == null || clip == null) return;

        var source = owner.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.loop = true;
        source.clip = clip;
        source.volume = 0f;
        source.outputAudioMixerGroup = output;
        UIParent sceneUI = null;
        foreach (var candidate in UnityEngine.Object.FindObjectsByType<UIParent>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (candidate.gameObject.scene == owner.scene) { sceneUI = candidate; break; }
        owner.AddComponent<SceneBgmFade>().Initialize(source, volume, sceneUI);
        source.Play();
    }
}
