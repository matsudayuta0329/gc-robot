using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Player-owned visual effects. Only the Player supplies gameplay state.</summary>
public sealed class PlayerEffects : MonoBehaviour
{
    [Header("Vacuum")]
    [SerializeField] private Transform vacuumParticleRoot;
    private ParticleSystem vacuumParticles;

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

    private Material movementMaterial;
    private Material smokeMaterial;
    private readonly TrailRenderer[] trails = new TrailRenderer[2];
    private readonly ParticleSystem[] smoke = new ParticleSystem[2];
    private Vector3 previousPosition;
    private bool vacuumActive;
    private bool initialized;

    private void Awake()
    {
        if (vacuumParticleRoot != null)
            vacuumParticles = vacuumParticleRoot.GetComponent<ParticleSystem>();
        if (movementShader == null)
        {
            Debug.LogError("PlayerEffects: Movement shader must be assigned.", this);
            enabled = false;
            return;
        }

        if (vacuumParticles != null)
            vacuumParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        movementMaterial = new Material(movementShader) { name = "Tire Trail (Runtime)" };
        smokeMaterial = new Material(movementShader) { name = "Smoke (Runtime)" };
        smokeMaterial.SetTexture("_BaseMap", smokeTexture != null ? smokeTexture : Texture2D.whiteTexture);

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
        if (vacuuming != vacuumActive && vacuumParticles != null)
        {
            if (vacuuming)
            {
                vacuumParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                vacuumParticles.Play(true);
            }
            else vacuumParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
        vacuumActive = vacuuming;
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
        if (vacuumParticles != null)
            vacuumParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        vacuumActive = false;
        for (int i = 0; i < 2; i++)
        {
            trails[i].emitting = false;
            smoke[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private void OnDestroy()
    {
        if (movementMaterial != null) Destroy(movementMaterial);
        if (smokeMaterial != null) Destroy(smokeMaterial);
    }

}
