using UnityEngine;

/// <summary>広い円盤から吸引口へ、寿命に合わせて粒子を収束させる。</summary>
[RequireComponent(typeof(ParticleSystem))]
public sealed class VacuumParticleFlow : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float length = 3f;
    [SerializeField, Min(0f)] private float radius = 1.2f;
    [SerializeField, Min(0.01f)] private float travelTime = 0.65f;
    [SerializeField, Min(0f)] private float emissionRate = 65f;
    [SerializeField, Min(0.01f)] private float particleSize = 0.12f;
    [SerializeField] private Color color = new Color(0.6f, 0.9f, 1f, 0.5f);
    [SerializeField] private Shader particleShader;
    [SerializeField] private Texture2D particleTexture;
    private ParticleSystem system;
    private ParticleSystem.Particle[] buffer;
    private Transform direction;
    private Material material;
    private Texture2D generatedTexture;

    private void Awake()
    {
        system = GetComponent<ParticleSystem>();
        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var player = GetComponentInParent<Player>();
        direction = player != null ? player.transform : transform;
        var main = system.main;
        main.playOnAwake = false;
        main.loop = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Shape;
        main.startSpeed = 0f;
        main.startLifetime = travelTime;
        main.startSize = particleSize;
        main.startColor = color;
        main.maxParticles = 256;
        main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
        var shape = system.shape;
        shape.enabled = false;
        var emission = system.emission;
        emission.rateOverTime = emissionRate;
        buffer = new ParticleSystem.Particle[main.maxParticles];
        if (particleShader != null)
        {
            material = new Material(particleShader);
            if (particleTexture == null)
            {
                generatedTexture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
                generatedTexture.wrapMode = TextureWrapMode.Clamp;
                for (int y = 0; y < 32; y++)
                    for (int x = 0; x < 32; x++)
                    {
                        float r = new Vector2((x + 0.5f) / 16f - 1f, (y + 0.5f) / 16f - 1f).magnitude;
                        generatedTexture.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(1f - r) * Mathf.Clamp01(1f - r)));
                    }
                generatedTexture.Apply(false, true);
            }
            material.SetTexture("_BaseMap", particleTexture != null ? particleTexture : generatedTexture);
            GetComponent<ParticleSystemRenderer>().sharedMaterial = material;
        }
    }

    // seedから出発点を再現するため、粒子ごとの管理オブジェクトや毎フレームの配列確保は不要。
    public static Vector3 EvaluatePosition(uint seed, float progress, float length, float radius)
    {
        uint hash = unchecked(seed * 747796405u + 2891336453u);
        float angle = (hash & 65535u) / 65536f * Mathf.PI * 2f;
        float r = Mathf.Sqrt((hash >> 16) / 65535f) * radius;
        return new Vector3(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r, length) * (1f - Mathf.Clamp01(progress));
    }

    private void LateUpdate()
    {
        if (system == null || system.particleCount == 0) return;
        int count = system.GetParticles(buffer);
        for (int i = 0; i < count; i++)
        {
            float t = 1f - buffer[i].remainingLifetime / Mathf.Max(0.001f, buffer[i].startLifetime);
            Vector3 offset = EvaluatePosition(buffer[i].randomSeed, t, length, radius);
            // Portのモデル階層の回転・倍率に左右されず、掃除機の前方から先端へ収束する。
            buffer[i].position = transform.position + direction.rotation * offset;
            buffer[i].velocity = Vector3.zero;
            buffer[i].startSize = particleSize * Mathf.Lerp(1f, 0.2f, t);
            var tint = color;
            tint.a *= Mathf.Clamp01(t / 0.12f) * Mathf.Clamp01((1f - t) / 0.15f);
            buffer[i].startColor = tint;
        }
        system.SetParticles(buffer, count);
    }

    private void OnDestroy()
    {
        if (material != null) Destroy(material);
        if (generatedTexture != null) Destroy(generatedTexture);
    }
}
