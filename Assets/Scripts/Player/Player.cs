using UnityEngine;
using System;

[RequireComponent(typeof(CharacterController), typeof(GarbageCounter))]
public class Player : MonoBehaviour
{
    [SerializeField]InputReader inputReader;
    [SerializeField]private Transform model;
    [SerializeField]private float maxSpeed = 15f;
    [SerializeField]private float rMaxSpeed = 270f;
    [SerializeField]private float acceleration = 45f;
    [SerializeField]private float rAcceleration = 1080f;

    [SerializeField]private Transform suctionPortPivot;
    [SerializeField]private Transform pickupPivot;
    [SerializeField]private float pickupRadius;
    [SerializeField]private LayerMask garbageLayer;

    [SerializeField]private float dashLength;
    [SerializeField, Min(0f)] private float dashChargeTime = 0.2f;
    [SerializeField] private Vector3 dashExpandedScale = new Vector3(10.5f, 1.7f, 1.5f);
    [SerializeField, Min(1f)] private float dashInitialSpeedMultiplier = 1.5f;
    [SerializeField, Min(0f)] private float dashCollisionMargin = 0.02f;
    [SerializeField, Min(0)] private float npcRadius = 2f;
    [SerializeField] private LayerMask npcLayer;
    private NPC nearbyNPC;
    private bool isTalking;
    public bool IsPaused => isPause;
    const float dashTime = 0.1f;

    CharacterController charConn;
    GarbageCounter garbages;

    private float speed;
    private float rSpeed;
    private float velocity = 0f;
    private float rVelocity = 0f;

    //プレイヤーの演出待機用
    private bool isPause = false;

    //プレイヤーの操作制御奪取用
    private bool isDash = false;
    private bool isDashMoving;
    private readonly RaycastHit[] dashHits = new RaycastHit[16];

    void Awake()
    {
        TryGetComponent(out charConn);
        TryGetComponent(out garbages);

        inputReader.Dash += HandleDash;
        inputReader.Interact += HandleInteract;

        speed = maxSpeed;
        rSpeed = rMaxSpeed;
        SetModelScale(Vector3.one);
    }

    void FixedUpdate()
    {
        if (isPause) return;
        if(!isDash)
        {
            // 演出中はFixedUpdateの先頭で停止する。
            Vector2 moveInput = inputReader.GetMoveInput();

            //目標角速度と目標値との差を算出
            float targetRVelocity = moveInput.x * rSpeed;
            float diffRVelocity = targetRVelocity - rVelocity;

            //角速度を計算
            if(Mathf.Abs(diffRVelocity) < rAcceleration * Time.fixedDeltaTime)
            {
                rVelocity = targetRVelocity;
            }else{
                rVelocity += Mathf.Sign(diffRVelocity) * rAcceleration * Time.fixedDeltaTime;
            }

            //回転の適用
            this.transform.Rotate(0f, rVelocity * Time.fixedDeltaTime, 0f);

            //目標速度と目標値との差を計算
            float targetVelocity = moveInput.y * speed;
            float diffVelocity = targetVelocity - velocity;

            //速度を計算
            if(Mathf.Abs(diffVelocity) < acceleration * Time.fixedDeltaTime)
            {
                velocity = targetVelocity;
            }else{
                velocity += Mathf.Sign(diffVelocity) * acceleration * Time.fixedDeltaTime;
            }

            UpdateDashScale();

            //移動方向への方向ベクトルを計算
            Vector3 direction = this.transform.forward;
            direction.y = 0;
            direction = direction.normalized;

            //移動の適用
            charConn.Move(direction * velocity * Time.fixedDeltaTime);

            //正面のごみを拾得
            if(inputReader.GetIsCollect())
            {
                DrainGarbage();
            }
        }else if(isDashMoving){
            //周囲のごみを拾得
            CollectGarbage();
        }
    }

    void DrainGarbage()
    {
        //付近のごみオブジェクトを取得
        Collider[] results = Physics.OverlapSphere(pickupPivot.position, pickupRadius, garbageLayer);

        //ごみオブジェクトに取得されたことを通知しカウントアップ
        //プレイ感によっては正面にある一つだけを回収する方向も視野に
        for(int i = 0; i < results.Length; i++)
        {
            Garbage result;
            if(results[i].TryGetComponent(out result))
            {
                if (result.Drain()) garbages.RecordCollection(result.Score);
            }
        }
    }

    void CollectGarbage()
    {
        //付近のごみオブジェクトを取得
        Collider[] results = Physics.OverlapSphere(pickupPivot.position, pickupRadius, garbageLayer);

        //ごみオブジェクトに取得されたことを通知しカウントアップ
        //プレイ感によっては正面にある一つだけを回収する方向も視野に
        for(int i = 0; i < results.Length; i++)
        {
            Garbage result;
            if(results[i].TryGetComponent(out result))
            {
                if (!result.IsCollected)
                {
                    CollectAsync(result);
                    garbages.RecordCollection(result.Score);
                }
            }
        }
    }

    async Awaitable Dash()
    {
        if (!isActiveAndEnabled || isPause || isDash || dashLength <= 0) return;
        //ダッシュ状態設定
        isDash = true;
        isDashMoving = false;
        velocity = 0f;
        rVelocity = 0f;
        garbages.CountDown(5);

        float dashedLength = 0f;

        //移動方向への方向ベクトルを計算
        Vector3 direction = this.transform.forward;
        direction.y = 0;
        direction = direction.normalized;

        try
        {
            // 約0.2秒停止しながら、モデルをダッシュ用の大きさまで拡大する。
            float chargedTime = 0f;
            while (!isPause && isActiveAndEnabled && chargedTime < dashChargeTime)
            {
                await Awaitable.FixedUpdateAsync(destroyCancellationToken);
                chargedTime += Time.fixedDeltaTime;
                float rate = dashChargeTime <= 0f ? 1f : Mathf.Clamp01(chargedTime / dashChargeTime);
                SetModelScale(Vector3.Lerp(Vector3.one, dashExpandedScale, rate));
            }

            if (isPause || !isActiveAndEnabled) return;
            SetModelScale(dashExpandedScale);

            //ダッシュ
            isDashMoving = true;
            bool wasBlocked = false;
            while(!isPause && isActiveAndEnabled)
            {
                float remainingLength = dashLength - dashedLength;
                float requestedLength = Mathf.Min(
                    dashLength / dashTime * Time.fixedDeltaTime,
                    remainingLength);
                float movableLength = GetDashMovableLength(direction, requestedLength, out wasBlocked);

                if (movableLength > 0f)
                {
                    CollisionFlags collision = charConn.Move(direction * movableLength);
                    dashedLength += movableLength;
                    wasBlocked |= (collision & CollisionFlags.Sides) != 0;
                }

                // 壁への衝突時は、CharacterControllerによる大きな壁面スライドを続けない。
                if(wasBlocked || dashedLength >= dashLength)
                {
                    break;
                }

                await Awaitable.FixedUpdateAsync(destroyCancellationToken);
            }

            //ダッシュ状態解除
            velocity = isPause || wasBlocked ? 0 : maxSpeed * dashInitialSpeedMultiplier;
        }
        finally
        {
            isDashMoving = false;
            isDash = false;
            if (isPause || !isActiveAndEnabled) SetModelScale(Vector3.one);
        }
    }

    private float GetDashMovableLength(Vector3 direction, float requestedLength, out bool blocked)
    {
        blocked = false;
        if (requestedLength <= 0f) return 0f;

        Vector3 scale = transform.lossyScale;
        float radius = charConn.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
        float height = Mathf.Max(charConn.height * Mathf.Abs(scale.y), radius * 2f);
        Vector3 center = transform.TransformPoint(charConn.center);
        float segmentHalfLength = height * 0.5f - radius;
        Vector3 top = center + transform.up * segmentHalfLength;
        Vector3 bottom = center - transform.up * segmentHalfLength;
        int hitCount = Physics.CapsuleCastNonAlloc(
            top,
            bottom,
            radius,
            direction,
            dashHits,
            requestedLength + charConn.skinWidth,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore);

        float nearestDistance = float.PositiveInfinity;
        for (int i = 0; i < hitCount; i++)
        {
            Collider hitCollider = dashHits[i].collider;
            if (hitCollider == null || hitCollider.transform.IsChildOf(transform)) continue;
            nearestDistance = Mathf.Min(nearestDistance, dashHits[i].distance);
        }

        if (float.IsPositiveInfinity(nearestDistance)) return requestedLength;

        blocked = true;
        return Mathf.Clamp(
            nearestDistance - charConn.skinWidth - dashCollisionMargin,
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

    public void SetPaused(bool paused)
    {
        isPause = paused;
        if (paused)
        {
            velocity = 0;
            rVelocity = 0;
            isDashMoving = false;
            SetModelScale(Vector3.one);
            if (nearbyNPC != null) nearbyNPC.SetGuideEnable(false);
        }
    }

    private async void HandleDash()
    {
        try { await Dash(); }
        catch (OperationCanceledException) { }
    }

    private async void CollectAsync(Garbage garbage)
    {
        try { await garbage.Collect(suctionPortPivot); }
        catch (OperationCanceledException) { }
    }

    private void Update()
    {
        if (isPause || isTalking) return;
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
        if (!isActiveAndEnabled || isPause || isTalking || isDash || nearbyNPC == null) return;
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
