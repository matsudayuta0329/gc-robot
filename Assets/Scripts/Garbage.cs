using UnityEngine;

public class Garbage: MonoBehaviour
{
    //ダッシュ時の回収のパラメータ
    const float collectAnimTime = 0.15f;

    //通常回収時のパラメータ
    [SerializeField]private int collectCountMax = 1;
    const float countDownInterval = 1f;
 
    private int collectCount = 1; 
    private float lastDrainTime = 0f;

    void Awake()
    {
        collectCount = collectCountMax;
    }

    public void Drain()
    {
        if(lastDrainTime + countDownInterval <= Time.time)
        {
            this.transform.localScale = this.transform.localScale * (collectCount - 1) / collectCount;
            lastDrainTime = Time.time;
            collectCount--;

            if(collectCount <= 0)
            {
                Destroy(gameObject);
            }
        }
    }

    public async Awaitable Collect(Transform collecterTransform)
    {
        float collectedTime = Time.time;
        Vector3 collectedPos = this.transform.position;

        while(collectedTime + collectAnimTime > Time.time)
        {
            this.transform.position = Vector3.Lerp(
                collectedPos, 
                collecterTransform.position, 
                (Time.time - collectedTime) / collectAnimTime
            );
            
            await Awaitable.FixedUpdateAsync();
        }

        Destroy(gameObject);
    }
}