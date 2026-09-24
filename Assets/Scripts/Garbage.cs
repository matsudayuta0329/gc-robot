using UnityEngine;

public class Garbage: MonoBehaviour
{
    //ダッシュ時の回収のパラメータ
    const float collectAnimTime = 0.15f;

    //通常回収時のパラメータ
    private int collectCountMax = 1;
    const float countDownMag = 0.8f;
 
    private float collectCount = 1; 
    private bool isCollected;
    public bool IsCollected => isCollected;
    public int Score => Mathf.Clamp(collectCountMax, 1, 3);

    void Awake()
    {
        collectCount = Score;
    }

    public void Init(int size)
    {
        collectCountMax = Mathf.Clamp(size, 1, 3);
        collectCount = (float)collectCountMax;
        isCollected = false;
    }

    public bool Drain()
    {
        if(!isCollected)
        {
            this.transform.localScale = this.transform.localScale * (collectCount - Time.deltaTime * countDownMag) / collectCount;
            collectCount -= Time.deltaTime * countDownMag;

            if(collectCount / collectCountMax <= 0.5f)
            {
                return true;
            }
        }
        return false;
    }

    public async Awaitable Collect(Transform collecterTransform)
    {
        if (isCollected) return;
        isCollected = true;
        float collectedTime = Time.time;
        Vector3 collectedPos = this.transform.position;

        while(collecterTransform != null && collectedTime + collectAnimTime > Time.time)
        {
            this.transform.position = Vector3.Lerp(
                collectedPos, 
                collecterTransform.position, 
                (Time.time - collectedTime) / collectAnimTime
            );
            
            await Awaitable.FixedUpdateAsync(destroyCancellationToken);
        }

        Destroy(gameObject);
    }
}
