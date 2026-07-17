using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField]InputReader inputReader;
    [SerializeField]private Transform model;
    [SerializeField]private float maxSpeed = 10f;
    [SerializeField]private float rMaxSpeed = 720f;
    [SerializeField]private float acceleration;
    [SerializeField]private float rAcceleration;

    CharacterController charConn;

    private float velocity = 0f;
    private float rVelocity = 0f;

    void Awake()
    {
        TryGetComponent(out charConn);
    }

    void FixedUpdate()
    {
        Vector2 moveInput = inputReader.GetMoveInput();

        //目標角速度と目標値との差を算出
        float targetRVelocity = moveInput.x * rMaxSpeed;
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
        float targetVelocity = moveInput.y * maxSpeed;
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
    }
}