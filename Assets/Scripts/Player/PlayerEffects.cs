using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Player-owned visual effects. Only the Player supplies gameplay state.</summary>
public sealed class PlayerEffects : MonoBehaviour
{
    [Header("Vacuum")]
    [SerializeField] private Transform suctionPort;
    [SerializeField] private Shader vacuumShader;
    [SerializeField] private Texture2D noiseTexture;
    [SerializeField, Min(0.1f)] private float vacuumLength = 2.2f;
    [SerializeField, Min(0.01f)] private float portRadius = 0.45f;
    [SerializeField, Min(0.001f)] private float tipRadius = 0.04f;
    [SerializeField, Range(0f, 0.5f)] private float vacuumMaxAlpha = 0.35f;

    [Header("Movement")]
    [SerializeField] private Shader movementShader;
    [SerializeField] private Texture2D smokeTexture;
    [SerializeField, Min(0.01f)] private float wheelHalfSpacing = 0.75f;
    [SerializeField, Min(0.01f)] private float trailWidth = 0.25f;
    [SerializeField, Min(0f)] private float trailDuration = 1.2f;
    [SerializeField, Range(0f, 1f)] private float trailAlpha = 0.3f;
    [SerializeField, Min(0.01f)] private float smokeSize = 0.4f;
    [SerializeField, Range(0f, 1f)] private float smokeAlpha = 0.18f;
    [SerializeField, Min(0f)] private float smokeRate = 18f;

    private GameObject vacuumObject;
    private Material vacuumMaterial;
    private Material movementMaterial;
    private Material smokeMaterial;
    private Texture2D generatedNoise;
    private Mesh vacuumMesh;
    private readonly TrailRenderer[] trails = new TrailRenderer[2];
    private readonly ParticleSystem[] smoke = new ParticleSystem[2];
    private Vector3 previousPosition;
    private bool initialized;

    private void Awake()
    {
        if (vacuumShader == null || movementShader == null)
        {
            Debug.LogError("PlayerEffects: Vacuum and movement shaders must be assigned.", this);
            enabled = false;
            return;
        }

        generatedNoise = noiseTexture == null ? CreateRepeatNoise() : null;
        vacuumMaterial = new Material(vacuumShader) { name = "Vacuum Field (Runtime)" };
        vacuumMaterial.SetTexture("_NoiseTex", noiseTexture != null ? noiseTexture : generatedNoise);
        vacuumMaterial.SetFloat("_MaxAlpha", vacuumMaxAlpha);
        movementMaterial = new Material(movementShader) { name = "Tire Trail (Runtime)" };
        smokeMaterial = new Material(movementShader) { name = "Smoke (Runtime)" };
        if (smokeTexture != null) smokeMaterial.SetTexture("_BaseMap", smokeTexture);

        vacuumMesh = CreateOpenTaperedCylinder(20, 8);
        vacuumObject = new GameObject("Vacuum Field");
        vacuumObject.transform.SetParent(transform, false);
        vacuumObject.AddComponent<MeshFilter>().sharedMesh = vacuumMesh;
        var vacuumRenderer = vacuumObject.AddComponent<MeshRenderer>();
        vacuumRenderer.sharedMaterial = vacuumMaterial;
        vacuumRenderer.shadowCastingMode = ShadowCastingMode.Off;
        vacuumRenderer.receiveShadows = false;
        vacuumObject.SetActive(false);

        for (int i = 0; i < 2; i++)
        {
            float x = i == 0 ? -wheelHalfSpacing : wheelHalfSpacing;
            var wheel = new GameObject(i == 0 ? "Left Wheel Effects" : "Right Wheel Effects");
            wheel.transform.SetParent(transform, false);
            wheel.transform.localPosition = new Vector3(x, 0.04f, 0f);
            wheel.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

            var trail = wheel.AddComponent<TrailRenderer>();
            trail.sharedMaterial = movementMaterial;
            trail.alignment = LineAlignment.TransformZ;
            trail.time = trailDuration;
            trail.startWidth = trailWidth;
            trail.endWidth = trailWidth;
            trail.minVertexDistance = 0.04f;
            trail.numCornerVertices = 2;
            trail.startColor = new Color(0f, 0f, 0f, trailAlpha);
            trail.endColor = new Color(0f, 0f, 0f, 0f);
            trail.shadowCastingMode = ShadowCastingMode.Off;
            trail.emitting = false;
            trails[i] = trail;

            var smokeObject = new GameObject("Smoke");
            smokeObject.transform.SetParent(wheel.transform, false);
            var particles = smokeObject.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.playOnAwake = false;
            main.loop = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = 0.45f;
            main.startSpeed = 0.3f;
            main.startSize = smokeSize;
            main.startColor = new Color(0.22f, 0.22f, 0.22f, smokeAlpha);
            main.maxParticles = 60;
            var emission = particles.emission;
            emission.rateOverTime = smokeRate;
            var shape = particles.shape;
            shape.enabled = false;
            var colorOverLifetime = particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            colorOverLifetime.color = fade;
            var particleRenderer = particles.GetComponent<ParticleSystemRenderer>();
            particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            particleRenderer.sharedMaterial = smokeMaterial;
            particleRenderer.shadowCastingMode = ShadowCastingMode.Off;
            smoke[i] = particles;
        }
        previousPosition = transform.position;
        initialized = true;
    }

    public void Tick(bool vacuuming, bool movingAllowed)
    {
        if (!initialized) return;
        bool moving = movingAllowed && (transform.position - previousPosition).sqrMagnitude > 0.000001f;
        previousPosition = transform.position;
        vacuumObject.SetActive(vacuuming);
        if (vacuuming)
        {
            vacuumObject.transform.SetPositionAndRotation(
                suctionPort != null ? suctionPort.position : transform.position + transform.forward,
                transform.rotation);
            vacuumMaterial.SetFloat("_MaxAlpha", vacuumMaxAlpha);
        }
        for (int i = 0; i < 2; i++)
        {
            trails[i].emitting = moving;
            if (moving && !smoke[i].isPlaying) smoke[i].Play();
            else if (!moving && smoke[i].isPlaying) smoke[i].Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    private void OnDisable()
    {
        if (!initialized) return;
        vacuumObject.SetActive(false);
        for (int i = 0; i < 2; i++)
        {
            trails[i].emitting = false;
            smoke[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private void OnDestroy()
    {
        if (vacuumMaterial != null) Destroy(vacuumMaterial);
        if (movementMaterial != null) Destroy(movementMaterial);
        if (smokeMaterial != null) Destroy(smokeMaterial);
        if (generatedNoise != null) Destroy(generatedNoise);
        if (vacuumMesh != null) Destroy(vacuumMesh);
    }

    private Mesh CreateOpenTaperedCylinder(int sides, int rings)
    {
        var vertices = new Vector3[(sides + 1) * (rings + 1)];
        var normals = new Vector3[vertices.Length];
        var uvs = new Vector2[vertices.Length];
        var triangles = new int[sides * rings * 6];
        for (int ring = 0; ring <= rings; ring++)
        {
            float t = (float)ring / rings;
            float radius = Mathf.Lerp(portRadius, tipRadius, t);
            for (int side = 0; side <= sides; side++)
            {
                float u = (float)side / sides;
                float angle = u * Mathf.PI * 2f;
                var radial = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                int index = ring * (sides + 1) + side;
                vertices[index] = radial * radius + Vector3.forward * (t * vacuumLength);
                normals[index] = new Vector3(radial.x, radial.y, (portRadius - tipRadius) / vacuumLength).normalized;
                uvs[index] = new Vector2(u, t);
                if (ring == rings || side == sides) continue;
                int next = index + sides + 1;
                int tri = (ring * sides + side) * 6;
                triangles[tri] = index;
                triangles[tri + 1] = next;
                triangles[tri + 2] = index + 1;
                triangles[tri + 3] = index + 1;
                triangles[tri + 4] = next;
                triangles[tri + 5] = next + 1;
            }
        }
        var mesh = new Mesh { name = "Open Vacuum Field" };
        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        return mesh;
    }

    private static Texture2D CreateRepeatNoise()
    {
        const int size = 64;
        var texture = new Texture2D(size, size, TextureFormat.R8, false) { name = "Vacuum Noise (Runtime)", wrapMode = TextureWrapMode.Repeat };
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float u = (float)x / size;
            float v = (float)y / size;
            float a = Mathf.PerlinNoise(u * 4f, v * 4f);
            float b = Mathf.PerlinNoise((u - 1f) * 4f, v * 4f);
            float c = Mathf.PerlinNoise(u * 4f, (v - 1f) * 4f);
            float d = Mathf.PerlinNoise((u - 1f) * 4f, (v - 1f) * 4f);
            float value = Mathf.Lerp(Mathf.Lerp(a, b, u), Mathf.Lerp(c, d, u), v);
            pixels[y * size + x] = new Color(value, value, value, 1f);
        }
        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }
}
