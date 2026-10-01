using System;
using UnityEngine;

public class PickupGarbage
{
    private static readonly int DisplacementIntensity = Shader.PropertyToID("_DisplacementIntensity");
    private static readonly int TargetPos = Shader.PropertyToID("_TargetPos");

    private readonly Transform suctionPortPivot;
    private readonly Transform pickupPivot;
    private readonly float pickupRadius;
    private readonly LayerMask garbageLayer;
    private readonly Material garbageMaterial;
    private readonly GarbageCounter garbageCounter;

    public PickupGarbage(
        Transform suctionPortPivot,
        Transform pickupPivot,
        float pickupRadius,
        LayerMask garbageLayer,
        Material garbageMaterial,
        GarbageCounter garbageCounter)
    {
        this.suctionPortPivot = suctionPortPivot;
        this.pickupPivot = pickupPivot;
        this.pickupRadius = pickupRadius;
        this.garbageLayer = garbageLayer;
        this.garbageMaterial = garbageMaterial;
        this.garbageCounter = garbageCounter;
    }

    public void TickVacuum(bool isCollecting)
    {
        if (garbageMaterial != null)
        {
            garbageMaterial.SetFloat(DisplacementIntensity, isCollecting ? 1f : 0f);
            if (isCollecting && pickupPivot != null)
                garbageMaterial.SetVector(TargetPos, pickupPivot.position);
        }

        if (isCollecting) DrainNearby();
    }

    public void CollectNearby()
    {
        foreach (var hit in FindNearby())
        {
            var garbage = hit.GetComponent<Garbage>();
            if (garbage == null || garbage.IsCollected) continue;
            CollectAsync(garbage);
            garbageCounter.RecordCollection(garbage.Score);
        }
    }

    private void DrainNearby()
    {
        foreach (var hit in FindNearby())
        {
            var garbage = hit.GetComponent<Garbage>();
            if (garbage == null || garbage.IsCollected || !garbage.Drain()) continue;
            CollectAsync(garbage);
            garbageCounter.RecordCollection(garbage.Score);
        }
    }

    private Collider[] FindNearby()
    {
        if (pickupPivot == null) return Array.Empty<Collider>();
        return Physics.OverlapSphere(pickupPivot.position, pickupRadius, garbageLayer);
    }

    private async void CollectAsync(Garbage garbage)
    {
        try { await garbage.Collect(suctionPortPivot); }
        catch (OperationCanceledException) { }
    }
}
