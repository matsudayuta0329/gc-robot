using UnityEngine;

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

    [SerializeField]private float dashTime;

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

    void Awake()
    {
        TryGetComponent(out charConn);
        TryGetComponent(out garbages);

        inputReader.Dash += () => {Dash();};

        speed = maxSpeed;
        rSpeed = rMaxSpeed;
    }

    void FixedUpdate()
    {
        //インプットを取得(演出時は入力をキャンセル)
        //滑って進む程度の距離の演出中の移動は許容するようにする
        Vector2 moveInput = isPause? Vector2.zero: isDash? new Vector2(1,1): inputReader.GetMoveInput();

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

        //移動方向への方向ベクトルを計算
        Vector3 direction = this.transform.forward;
        direction.y = 0;
        direction = direction.normalized;

        //移動の適用
        charConn.Move(direction * velocity * Time.fixedDeltaTime);

        
        //周囲のごみを拾得
        CollectGarbage();
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
                result.OnCollect(suctionPortPivot);
                garbages.CountUp();
            }
        }
    }

    async Awaitable Dash()
    {
        isDash = true;

        garbages.CountDown();

        float dashedTime = Time.time;
        float acceleration_bef = acceleration;

        speed = maxSpeed * 10;
        rSpeed = 0f;
        acceleration = 3000f;

        while(dashedTime + dashTime > Time.time)
        {
            Debug.Log(velocity);
            await Awaitable.NextFrameAsync();
        }

        //スピード低減の時間
        acceleration = 1000f;
        while(velocity > maxSpeed)
        {
            Debug.Log(velocity);
            await Awaitable.NextFrameAsync();
        }

        //ダッシュ状態解除
        speed = maxSpeed;
        rSpeed = rMaxSpeed;
        acceleration = acceleration_bef;
        isDash = false;
    }
}