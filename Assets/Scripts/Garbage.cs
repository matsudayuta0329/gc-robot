using UnityEngine;

public class Garbage: MonoBehaviour
{
    public void OnCollect()
    {
        Destroy(gameObject);
    }
}