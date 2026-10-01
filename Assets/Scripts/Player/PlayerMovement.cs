using System;
using System.Threading;
using UnityEngine;

public class PlayerMovement
{
    private const float DashTime = 0.1f;

    private readonly CharacterController characterController;
    private readonly Transform playerTransform;
    private readonly Transform model;
    private readonly float maxSpeed;
    private readonly float rMaxSpeed;
    private readonly float acceleration;
    private readonly float rAcceleration;
    private readonly float dashLength;
    private readonly float dashChargeTime;
    private readonly Vector3 dashExpandedScale;
    private readonly float dashInitialSpeedMultiplier;
    private readonly float dashCollisionMargin;
    private readonly Action<int> consumeGarbage;
    private readonly RaycastHit[] dashHits = new RaycastHit[16];

    private float velocity;
    private float rVelocity;
    private bool isDashExitSliding;
    private Vector3 dashExitDirection;

    public bool IsPaused { get; private set; }
    public bool IsDashing { get; private set; }
    public bool IsDashMoving { get; private set; }
    public float Velocity => velocity;
    public Vector3 DashExitDirection => dashExitDirection;

    public PlayerMovement(
        CharacterController characterController,
        Transform playerTransform,
        Transform model,
        float maxSpeed,
        float rMaxSpeed,
        float acceleration,
        float rAcceleration,
        float dashLength,
        float dashChargeTime,
        Vector3 dashExpandedScale,
        float dashInitialSpeedMultiplier,
        float dashCollisionMargin,
        Action<int> consumeGarbage)
    {
        this.characterController = characterController;
        this.playerTransform = playerTransform;
        this.model = model;
        this.maxSpeed = maxSpeed;
        this.rMaxSpeed = rMaxSpeed;
        this.acceleration = acceleration;
        this.rAcceleration = rAcceleration;
        this.dashLength = dashLength;
        this.dashChargeTime = dashChargeTime;
        this.dashExpandedScale = dashExpandedScale;
        this.dashInitialSpeedMultiplier = dashInitialSpeedMultiplier;
        this.dashCollisionMargin = dashCollisionMargin;
        this.consumeGarbage = consumeGarbage;
        SetModelScale(Vector3.one);
    }

    public void Tick(Vector2 moveInput, float deltaTime)
    {
        float targetRVelocity = moveInput.x * rMaxSpeed;
        float diffRVelocity = targetRVelocity - rVelocity;
        if (Mathf.Abs(diffRVelocity) < rAcceleration * deltaTime)
            rVelocity = targetRVelocity;
        else
            rVelocity += Mathf.Sign(diffRVelocity) * rAcceleration * deltaTime;

        playerTransform.Rotate(0f, rVelocity * deltaTime, 0f);

        float targetVelocity = moveInput.y * maxSpeed;
        float diffVelocity = targetVelocity - velocity;
        if (Mathf.Abs(diffVelocity) < acceleration * deltaTime)
            velocity = targetVelocity;
        else
            velocity += Mathf.Sign(diffVelocity) * acceleration * deltaTime;

        UpdateDashScale();

        Vector3 direction = playerTransform.forward;
        if (isDashExitSliding)
        {
            if (Mathf.Abs(velocity) > Mathf.Abs(targetVelocity) + 0.001f)
                direction = dashExitDirection;
            else
                isDashExitSliding = false;
        }
        direction.y = 0f;
        direction = direction.normalized;
        characterController.Move(direction * velocity * deltaTime);
    }

    public async Awaitable Dash(Func<bool> isActive, CancellationToken cancellationToken)
    {
        if (!isActive() || IsPaused || IsDashing || dashLength <= 0f) return;

        IsDashing = true;
        IsDashMoving = false;
        velocity = 0f;
        rVelocity = 0f;
        consumeGarbage?.Invoke(5);

        float dashedLength = 0f;
        Vector3 direction = playerTransform.forward;
        direction.y = 0f;
        direction = direction.normalized;

        try
        {
            float chargedTime = 0f;
            while (!IsPaused && isActive() && chargedTime < dashChargeTime)
            {
                await Awaitable.FixedUpdateAsync(cancellationToken);
                chargedTime += Time.fixedDeltaTime;
                float rate = dashChargeTime <= 0f
                    ? 1f
                    : Mathf.Clamp01(chargedTime / dashChargeTime);
                SetModelScale(Vector3.Lerp(Vector3.one, dashExpandedScale, rate));
            }

            if (IsPaused || !isActive()) return;
            SetModelScale(dashExpandedScale);

            IsDashMoving = true;
            bool touchedWall = false;
            bool canGlideAfterDash = true;
            Vector3 exitDirection = direction;
            while (!IsPaused && isActive())
            {
                float remainingLength = dashLength - dashedLength;
                float requestedLength = Mathf.Min(
                    dashLength / DashTime * Time.fixedDeltaTime,
                    remainingLength);
                float movableLength = GetDashMovableLength(
                    direction,
                    requestedLength,
                    out bool blocked,
                    out Vector3 hitNormal);

                Vector3 moveAmount = Vector3.zero;
                if (movableLength > 0f) moveAmount = direction * movableLength;

                if (blocked)
                {
                    touchedWall = true;
                    Vector3 slideDirection = Vector3.ProjectOnPlane(direction, hitNormal);
                    slideDirection.y = 0f;
                    if (slideDirection.sqrMagnitude < 0.0001f)
                    {
                        canGlideAfterDash = false;
                        break;
                    }

                    Vector3 slideAmount = Vector3.ProjectOnPlane(
                        direction * (requestedLength - movableLength),
                        hitNormal);
                    slideAmount.y = 0f;
                    moveAmount += slideAmount;
                    exitDirection = slideDirection.normalized;
                }

                if (moveAmount.sqrMagnitude > 0.0001f)
                    characterController.Move(moveAmount);

                dashedLength += requestedLength;
                if (dashedLength >= dashLength) break;

                await Awaitable.FixedUpdateAsync(cancellationToken);
            }

            velocity = IsPaused || !canGlideAfterDash
                ? 0f
                : maxSpeed * dashInitialSpeedMultiplier;
            isDashExitSliding = !IsPaused && touchedWall && canGlideAfterDash;
            if (isDashExitSliding) dashExitDirection = exitDirection;
        }
        finally
        {
            IsDashMoving = false;
            IsDashing = false;
            if (IsPaused || !isActive()) SetModelScale(Vector3.one);
        }
    }

    public void SetPaused(bool paused)
    {
        IsPaused = paused;
        if (!paused) return;

        velocity = 0f;
        rVelocity = 0f;
        IsDashMoving = false;
        isDashExitSliding = false;
        SetModelScale(Vector3.one);
    }

    private float GetDashMovableLength(
        Vector3 direction,
        float requestedLength,
        out bool blocked,
        out Vector3 hitNormal)
    {
        blocked = false;
        hitNormal = Vector3.zero;
        if (requestedLength <= 0f) return 0f;

        Vector3 scale = playerTransform.lossyScale;
        float radius = characterController.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
        float height = Mathf.Max(characterController.height * Mathf.Abs(scale.y), radius * 2f);
        Vector3 center = playerTransform.TransformPoint(characterController.center);
        float segmentHalfLength = height * 0.5f - radius;
        Vector3 top = center + playerTransform.up * segmentHalfLength;
        Vector3 bottom = center - playerTransform.up * segmentHalfLength;
        int hitCount = Physics.CapsuleCastNonAlloc(
            top,
            bottom,
            radius,
            direction,
            dashHits,
            requestedLength + characterController.skinWidth,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore);

        float nearestDistance = float.PositiveInfinity;
        for (int i = 0; i < hitCount; i++)
        {
            Collider hitCollider = dashHits[i].collider;
            if (hitCollider == null || hitCollider.transform.IsChildOf(playerTransform)) continue;
            if (dashHits[i].distance < nearestDistance)
            {
                nearestDistance = dashHits[i].distance;
                hitNormal = dashHits[i].normal;
            }
        }

        if (float.IsPositiveInfinity(nearestDistance)) return requestedLength;

        blocked = true;
        return Mathf.Clamp(
            nearestDistance - characterController.skinWidth - dashCollisionMargin,
            0f,
            requestedLength);
    }

    private void UpdateDashScale()
    {
        if (model == null) return;
        float dashSpeed = maxSpeed * dashInitialSpeedMultiplier;
        float rate = dashSpeed <= maxSpeed
            ? 0f
            : Mathf.InverseLerp(maxSpeed, dashSpeed, Mathf.Abs(velocity));
        SetModelScale(Vector3.Lerp(Vector3.one, dashExpandedScale, rate));
    }

    private void SetModelScale(Vector3 scale)
    {
        if (model != null) model.localScale = scale;
    }
}
