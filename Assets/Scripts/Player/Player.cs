using System;
using UnityEngine;

[RequireComponent(typeof(CharacterController), typeof(GarbageCounter))]
public class Player : MonoBehaviour
{
    [SerializeField] private InputReader inputReader;
    [SerializeField] private Transform model;
    [SerializeField] private float maxSpeed = 15f;
    [SerializeField] private float rMaxSpeed = 270f;
    [SerializeField] private float acceleration = 45f;
    [SerializeField] private float rAcceleration = 1080f;
    [SerializeField] private Transform suctionPortPivot;
    [SerializeField] private Transform pickupPivot;
    [SerializeField] private float pickupRadius;
    [SerializeField] private LayerMask garbageLayer;
    [SerializeField] private Material garbageMaterial;
    [SerializeField] private float dashLength;
    [SerializeField, Min(0f)] private float dashChargeTime = 0.2f;
    [SerializeField] private Vector3 dashExpandedScale = new Vector3(10.5f, 1.7f, 1.5f);
    [SerializeField, Min(1f)] private float dashInitialSpeedMultiplier = 1.5f;
    [SerializeField, Min(0f)] private float dashCollisionMargin = 0.02f;
    [SerializeField, Min(0)] private float npcRadius = 2f;
    [SerializeField] private LayerMask npcLayer;
    [Header("Sound Effects")]
    [SerializeField] private AudioClip moveClip;
    [SerializeField] private AudioClip vacuumClip;
    [SerializeField] private AudioClip dashClip;
    [SerializeField, Range(0f, 1f)] private float moveVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float vacuumVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float dashVolume = 1f;
    [SerializeField, Min(0.01f)] private float vacuumFadeDuration = 0.15f;

    private PlayerMovement movement;
    private PickupGarbage pickupGarbage;
    private PlayerEffects effects;
    private AudioSource moveSource;
    private AudioSource vacuumSource;
    private AudioSource dashSource;
    private bool vacuumRequested;
    private float vacuumFadeStart;
    private float vacuumFadeElapsed;
    private bool movedThisFixedStep;
    private NPC nearbyNPC;
    private RankingBoard nearbyRankingBoard;
    private bool isPaused;
    private bool isTalking;

    public bool IsPaused => movement?.IsPaused ?? isPaused;
    public bool IsDashing => movement?.IsDashing ?? false;

    private void Awake()
    {
        TryGetComponent(out CharacterController characterController);
        TryGetComponent(out GarbageCounter garbageCounter);
        movement = new PlayerMovement(
            characterController, transform, model,
            maxSpeed, rMaxSpeed, acceleration, rAcceleration,
            dashLength, dashChargeTime, dashExpandedScale,
            dashInitialSpeedMultiplier, dashCollisionMargin,
            amount => garbageCounter.TrySpend(amount));
        movement.SetPaused(isPaused);
        movement.DashStarted += PlayDashSound;
        pickupGarbage = new PickupGarbage(
            suctionPortPivot, pickupPivot, pickupRadius,
            garbageLayer, garbageMaterial, garbageCounter);
        TryGetComponent(out effects);

        moveSource = CreateAudioSource(moveClip, true, moveVolume);
        vacuumSource = CreateAudioSource(vacuumClip, true, 0f);
        dashSource = CreateAudioSource(null, false, 1f);

        inputReader.Dash += HandleDash;
        inputReader.Interact += HandleInteract;
        inputReader.Cancel += HandleCancel;
    }

    private void FixedUpdate()
    {
        if (movement == null || movement.IsPaused)
        {
            movedThisFixedStep = false;
            if (effects != null) effects.Tick(false, false);
            return;
        }
        if (!movement.IsDashing)
        {
            Vector3 previousPosition = transform.position;
            movement.Tick(inputReader.GetMoveInput(), Time.fixedDeltaTime);
            movedThisFixedStep = (transform.position - previousPosition).sqrMagnitude > 0.000001f;
            pickupGarbage.TickVacuum(inputReader.GetIsCollect());
        }
        else if (movement.IsDashMoving)
        {
            movedThisFixedStep = false;
            pickupGarbage.CollectNearby();
        }
        else movedThisFixedStep = false;
        if (effects != null)
            effects.Tick(!movement.IsDashing && inputReader.GetIsCollect(), !movement.IsDashing);
    }

    public void SetPaused(bool paused)
    {
        isPaused = paused;
        movement?.SetPaused(paused);
        if (!paused) return;
        movedThisFixedStep = false;
        if (effects != null) effects.Tick(false, false);
        if (nearbyNPC != null) nearbyNPC.SetGuideEnable(false);
    }

    private async Awaitable Dash()
    {
        if (movement == null) return;
        await movement.Dash(() => isActiveAndEnabled, destroyCancellationToken);
    }

    private async void HandleDash()
    {
        try { await Dash(); }
        catch (OperationCanceledException) { }
    }

    private void Update()
    {
        UpdateSounds();
        if (IsPaused || isTalking) return;
        NPC closest = null;
        RankingBoard closestBoard = null;
        float distance = float.PositiveInfinity;
        if (inputReader.IsActionEnabled(ActionType.Interact))
        foreach (var hit in Physics.OverlapSphere(transform.position, npcRadius, ~0, QueryTriggerInteraction.Collide))
        {
            var npc = hit.GetComponentInParent<NPC>();
            if (npc != null && npc.isActiveAndEnabled)
            {
                float candidate = (npc.transform.position - transform.position).sqrMagnitude;
                if (candidate < distance) { closest = npc; closestBoard = null; distance = candidate; }
            }
            var board = hit.GetComponentInParent<RankingBoard>();
            if (board != null && board.isActiveAndEnabled)
            {
                float candidate = (board.transform.position - transform.position).sqrMagnitude;
                if (candidate < distance) { closestBoard = board; closest = null; distance = candidate; }
            }
        }
        if (nearbyRankingBoard != null && nearbyRankingBoard != closestBoard) nearbyRankingBoard.SetGuideEnable(false);
        nearbyRankingBoard = closestBoard;
        if (nearbyRankingBoard != null) nearbyRankingBoard.SetGuideEnable(true);
        if (nearbyNPC != closest && nearbyNPC != null) nearbyNPC.SetGuideEnable(false);
        nearbyNPC = closest;
        if (nearbyNPC != null) nearbyNPC.SetGuideEnable(true);
    }

    private async void HandleInteract()
    {
        if (!isActiveAndEnabled || IsPaused || isTalking || movement.IsDashing) return;
        if (nearbyRankingBoard != null) { try { await nearbyRankingBoard.OpenAsync(); } catch (OperationCanceledException) { } return; }
        if (nearbyNPC == null) return;
        isTalking = true;
        try { await nearbyNPC.Invoke(); }
        catch (OperationCanceledException) { }
        catch (Exception exception) { Debug.LogException(exception, this); }
        finally { isTalking = false; }
    }

    private void HandleCancel() { if (nearbyRankingBoard != null && nearbyRankingBoard.IsOpen) nearbyRankingBoard.Close(); }

    private void OnDisable()
    {
        if (nearbyRankingBoard != null) { nearbyRankingBoard.Close(); nearbyRankingBoard.SetGuideEnable(false); }
        if (nearbyNPC != null) nearbyNPC.SetGuideEnable(false);
        if (moveSource != null) moveSource.Stop();
        if (vacuumSource != null) { vacuumSource.Stop(); vacuumSource.volume = 0f; }
        if (dashSource != null) dashSource.Stop();
        vacuumRequested = false;
        movedThisFixedStep = false;
    }

    private void OnDestroy()
    {
        if (movement != null) movement.DashStarted -= PlayDashSound;
        if (inputReader == null) return;
        inputReader.Dash -= HandleDash;
        inputReader.Interact -= HandleInteract;
        inputReader.Cancel -= HandleCancel;
    }

    private AudioSource CreateAudioSource(AudioClip clip, bool loop, float volume)
    {
        var source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.loop = loop;
        source.clip = clip;
        source.volume = volume;
        return source;
    }

    private void UpdateSounds()
    {
        if (movement == null) return;
        bool canPlay = !IsPaused && !isTalking && !movement.IsDashing;
        bool moving = canPlay && movedThisFixedStep;
        if (moveSource != null && moveSource.clip != null)
        {
            moveSource.volume = moveVolume;
            if (moving && !moveSource.isPlaying) moveSource.Play();
            else if (!moving && moveSource.isPlaying) moveSource.Stop();
        }

        if (vacuumSource == null || vacuumSource.clip == null) return;
        bool requested = canPlay && inputReader.GetIsCollect();
        if (requested != vacuumRequested)
        {
            vacuumRequested = requested;
            vacuumFadeStart = vacuumSource.volume;
            vacuumFadeElapsed = 0f;
            if (requested && !vacuumSource.isPlaying) vacuumSource.Play();
        }
        vacuumFadeElapsed = Mathf.Min(vacuumFadeElapsed + Time.unscaledDeltaTime, vacuumFadeDuration);
        float targetVolume = vacuumRequested ? vacuumVolume : 0f;
        vacuumSource.volume = Mathf.Lerp(vacuumFadeStart, targetVolume,
            vacuumFadeElapsed / Mathf.Max(0.01f, vacuumFadeDuration));
        if (!vacuumRequested && vacuumFadeElapsed >= vacuumFadeDuration && vacuumSource.isPlaying)
            vacuumSource.Stop();
    }

    private void PlayDashSound()
    {
        if (dashSource != null && dashClip != null) dashSource.PlayOneShot(dashClip, dashVolume);
    }
}
