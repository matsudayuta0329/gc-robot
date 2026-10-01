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

    private PlayerMovement movement;
    private PickupGarbage pickupGarbage;
    private NPC nearbyNPC;
    private bool isPaused;
    private bool isTalking;

    public bool IsPaused => movement?.IsPaused ?? isPaused;

    private void Awake()
    {
        TryGetComponent(out CharacterController characterController);
        TryGetComponent(out GarbageCounter garbageCounter);
        movement = new PlayerMovement(
            characterController, transform, model,
            maxSpeed, rMaxSpeed, acceleration, rAcceleration,
            dashLength, dashChargeTime, dashExpandedScale,
            dashInitialSpeedMultiplier, dashCollisionMargin,
            amount => garbageCounter.CountDown(amount));
        movement.SetPaused(isPaused);
        pickupGarbage = new PickupGarbage(
            suctionPortPivot, pickupPivot, pickupRadius,
            garbageLayer, garbageMaterial, garbageCounter);

        inputReader.Dash += HandleDash;
        inputReader.Interact += HandleInteract;
    }

    private void FixedUpdate()
    {
        if (movement == null || movement.IsPaused) return;
        if (!movement.IsDashing)
        {
            movement.Tick(inputReader.GetMoveInput(), Time.fixedDeltaTime);
            pickupGarbage.TickVacuum(inputReader.GetIsCollect());
        }
        else if (movement.IsDashMoving)
        {
            pickupGarbage.CollectNearby();
        }
    }

    public void SetPaused(bool paused)
    {
        isPaused = paused;
        movement?.SetPaused(paused);
        if (!paused) return;
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
        if (IsPaused || isTalking) return;
        NPC closest = null;
        float distance = float.PositiveInfinity;
        if (inputReader.IsActionEnabled(ActionType.Interact))
        foreach (var hit in Physics.OverlapSphere(transform.position, npcRadius, npcLayer, QueryTriggerInteraction.Collide))
        {
            var npc = hit.GetComponentInParent<NPC>();
            if (npc == null || !npc.isActiveAndEnabled) continue;
            float candidate = (npc.transform.position - transform.position).sqrMagnitude;
            if (candidate < distance) { closest = npc; distance = candidate; }
        }
        if (nearbyNPC != closest && nearbyNPC != null) nearbyNPC.SetGuideEnable(false);
        nearbyNPC = closest;
        if (nearbyNPC != null) nearbyNPC.SetGuideEnable(true);
    }

    private async void HandleInteract()
    {
        if (!isActiveAndEnabled || IsPaused || isTalking || movement.IsDashing || nearbyNPC == null) return;
        isTalking = true;
        try { await nearbyNPC.Invoke(); }
        catch (OperationCanceledException) { }
        finally { isTalking = false; }
    }

    private void OnDisable()
    {
        if (nearbyNPC != null) nearbyNPC.SetGuideEnable(false);
    }

    private void OnDestroy()
    {
        if (inputReader == null) return;
        inputReader.Dash -= HandleDash;
        inputReader.Interact -= HandleInteract;
    }
}
