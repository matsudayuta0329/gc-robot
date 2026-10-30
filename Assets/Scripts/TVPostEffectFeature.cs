using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;

public sealed class TVPostEffectFeature : ScriptableRendererFeature
{
    [SerializeField] private Shader shader;
    private Material material;
    private TVPass pass;

    public override void Create()
    {
        CoreUtils.Destroy(material);
        material = shader != null ? CoreUtils.CreateEngineMaterial(shader) : null;
        pass = new TVPass { renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        var data = renderingData.cameraData;
        if (material == null || !data.postProcessEnabled || !data.resolveFinalTarget
            || data.cameraType == CameraType.Preview || data.cameraType == CameraType.Reflection) return;
        var cameraSettings = data.camera.GetComponent<TVPostEffect>();
        Vector4 parameters;
        // 無効化したカメラコンポーネントがある場合はVolumeにもフォールバックしない。
        if (cameraSettings != null)
        {
            if (!cameraSettings.isActiveAndEnabled) return;
            parameters = new Vector4(Mathf.Clamp01(cameraSettings.scanlineIntensity),
                Mathf.Clamp(cameraSettings.scanlineSpacing, 2f, 32f),
                Mathf.Clamp(cameraSettings.chromaticAberration, 0f, 10f), 0f);
        }
        else
        {
            var settings = VolumeManager.instance.stack.GetComponent<TVPostEffectVolume>();
            if (settings == null || !settings.active || !settings.IsActive()) return;
            parameters = new Vector4(settings.scanlineIntensity.value, settings.scanlineSpacing.value,
                settings.chromaticAberration.value, 0f);
        }
        if (parameters.x <= 0 && parameters.z <= 0) return;
        pass.Setup(material, parameters);
        renderer.EnqueuePass(pass);
    }

    protected override void Dispose(bool disposing) => CoreUtils.Destroy(material);

    private sealed class TVPass : ScriptableRenderPass
    {
        private static readonly int Parameters = Shader.PropertyToID("_TVParameters");
        private Material material;
        private Vector4 parameters;

        public void Setup(Material effectMaterial, Vector4 values)
        {
            material = effectMaterial;
            parameters = values;
            requiresIntermediateTexture = true;
        }

        public override void RecordRenderGraph(RenderGraph graph, ContextContainer frameData)
        {
            var resources = frameData.Get<UniversalResourceData>();
            if (resources.isActiveTargetBackBuffer) return;
            var source = resources.activeColorTexture;
            var descriptor = graph.GetTextureDesc(source);
            descriptor.name = "TV Post Effect Color";
            descriptor.clearBuffer = false;
            var destination = graph.CreateTexture(descriptor);
            // カメラごとの値をRenderGraph実行まで独立して保持する。
            var properties = new MaterialPropertyBlock();
            properties.SetVector(Parameters, parameters);
            var blit = new RenderGraphUtils.BlitMaterialParameters(source, destination,
                material, 0, properties, 0, 0);
            graph.AddBlitPass(blit, passName: "TV Post Effect");
            resources.cameraColor = destination;
        }
    }
}
