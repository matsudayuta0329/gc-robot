using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[Serializable, VolumeComponentMenu("Post-processing/TV Post Effect")]
[SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
public sealed class TVPostEffectVolume : VolumeComponent, IPostProcessComponent
{
    public ClampedFloatParameter scanlineIntensity = new ClampedFloatParameter(0f, 0f, 1f);
    public ClampedFloatParameter scanlineSpacing = new ClampedFloatParameter(4f, 2f, 32f);
    public ClampedFloatParameter chromaticAberration = new ClampedFloatParameter(0f, 0f, 10f);
    public bool IsActive() => scanlineIntensity.value > 0f || chromaticAberration.value > 0f;
}
