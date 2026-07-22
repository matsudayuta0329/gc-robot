using UnityEngine;

public class Garbage: MonoBehaviour
{
    const float collectAnimTime = 0.15f;

    public async Awaitable OnCollect(Transform collecterTransform)
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
            
            await Awaitable.NextFrameAsync();
        }

        Destroy(gameObject);
    }
}