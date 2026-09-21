using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class GameAreaTests
{
    private readonly List<GameObject> objects = new List<GameObject>();
    private GameObject Create(string name, bool active = true)
    {
        var go = new GameObject(name);
        go.SetActive(active);
        objects.Add(go);
        return go;
    }
    private static void Set(object instance, string name, object value)
    {
        var type = instance.GetType();
        while (type != null)
        {
            var field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null) { field.SetValue(instance, value); return; }
            type = type.BaseType;
        }
        throw new MissingFieldException(name);
    }
    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        foreach (var go in objects) if (go != null) UnityEngine.Object.Destroy(go);
        objects.Clear();
        yield return null;
    }
    [Test]
    public void InputMaskSupportsNullEmptyAndMovementWithSpin()
    {
        var input = Create("input").AddComponent<InputReader>();
        input.SetEnableAction(null);
        Assert.IsTrue(input.IsActionEnabled(ActionType.Dash));
        input.SetEnableAction(Array.Empty<ActionType>());
        Assert.IsEmpty(input.GetEnabledActions());
        input.SetEnableAction(new[] { ActionType.Move });
        CollectionAssert.AreEquivalent(new[] { ActionType.Move }, input.GetEnabledActions());
        Assert.AreEqual(Vector2.zero, input.GetMoveInput());
        input.gameObject.SetActive(false);
        Assert.IsEmpty(input.GetEnabledActions());
        input.gameObject.SetActive(true);
        CollectionAssert.AreEquivalent(new[] { ActionType.Move }, input.GetEnabledActions());
    }
    [Test]
    public void ScoreAndTotalSurviveDashConsumption()
    {
        var counter = Create("counter").AddComponent<GarbageCounter>();
        counter.RecordCollection(1);
        counter.RecordCollection(3);
        counter.CountDown(5);
        Assert.AreEqual(4, counter.Score);
        Assert.AreEqual(2, counter.CollectedCount);
        Assert.AreEqual(-3, counter.count); // Existing dash cost behavior is retained.
        counter.ResetCount();
        Assert.AreEqual(0, counter.Score);
        Assert.AreEqual(0, counter.CollectedCount);
        Assert.AreEqual(0, counter.count);
    }
    [UnityTest]
    public IEnumerator DrainHonorsSizeIntervalAndOnlyFinishesOnce()
    {
        var garbage = Create("garbage").AddComponent<Garbage>();
        garbage.Init(3);
        Assert.IsFalse(garbage.Drain());
        Assert.IsFalse(garbage.Drain()); // Same-frame drain must not count twice.
        Set(garbage, "lastDrainTime", float.NegativeInfinity);
        Assert.IsFalse(garbage.Drain());
        Set(garbage, "lastDrainTime", float.NegativeInfinity);
        Assert.IsTrue(garbage.Drain());
        Assert.IsFalse(garbage.Drain());
        Assert.AreEqual(3, garbage.Score);
        yield return null;
        Assert.IsTrue(garbage == null);
    }
    [UnityTest]
    public IEnumerator CollectionClaimsGarbageBeforeAnimationCompletes()
    {
        var garbage = Create("garbage").AddComponent<Garbage>();
        var target = Create("target");
        bool finished = false;
        Collect();
        async void Collect() { await garbage.Collect(target.transform); finished = true; }
        Assert.IsTrue(garbage.IsCollected);
        Assert.IsFalse(garbage.Drain());
        yield return new WaitForSeconds(0.25f);
        Assert.IsTrue(finished);
        Assert.IsTrue(garbage == null);
    }
    [UnityTest]
    public IEnumerator EnablingCancelsPendingHide()
    {
        var ui = Create("ui").AddComponent<GameUI>();
        Set(ui, "disableDuration", 0.05f);
        ui.Disable();
        ui.Enable();
        yield return new WaitForSecondsRealtime(0.1f);
        Assert.IsTrue(ui.gameObject.activeSelf);
    }
    [UnityTest]
    public IEnumerator TimerSpawnsTrashThenStopsPlayerAndShowsResult()
    {
        var input = Create("input").AddComponent<InputReader>();
        var playerObject = Create("player", false);
        var player = playerObject.AddComponent<Player>();
        Set(player, "inputReader", input);
        Set(player, "pickupPivot", player.transform);
        Set(player, "suctionPortPivot", player.transform);
        playerObject.SetActive(true);
        var ui = Create("gameUI").AddComponent<GameUI>();
        var result = Create("result").AddComponent<ResultUI>();
        var prefab = Create("trashPrefab").AddComponent<Garbage>();
        var parent = Create("trashParent");
        var minSpawnPoint = Create("minSpawnPoint").transform;
        var maxSpawnPoint = Create("maxSpawnPoint").transform;
        minSpawnPoint.position = new Vector3(-1f, 0f, -1f);
        maxSpawnPoint.position = new Vector3(1f, 0f, 1f);
        var managerObject = Create("manager", false);
        var manager = managerObject.AddComponent<GameManager>();
        Set(manager, "player", player);
        Set(manager, "inputReader", input);
        Set(manager, "gameUI", ui);
        Set(manager, "resultUI", result);
        Set(manager, "limit", 0.08f);
        Set(manager, "spawnInterval", 0.02f);
        Set(manager, "trashParent", parent.transform);
        Set(manager, "minSpawnPoint", minSpawnPoint);
        Set(manager, "maxSpawnPoint", maxSpawnPoint);
        Set(manager, "trashObjects", new List<GameManager.TrashEntry> {
            new GameManager.TrashEntry { trashObject = prefab.gameObject, Size = 3 }
        });
        managerObject.SetActive(true);
        Assert.IsTrue(player.IsPaused);
        yield return new WaitForSeconds(0.2f);
        Assert.Greater(parent.transform.childCount, 0);
        Assert.AreEqual(3, parent.transform.GetChild(0).GetComponent<Garbage>().Score);
        Assert.IsFalse(manager.IsPlaying);
        Assert.AreEqual(0, manager.RemainingTime);
        Assert.IsTrue(player.IsPaused);
        Assert.IsEmpty(input.GetEnabledActions());
        Assert.IsFalse(ui.gameObject.activeSelf);
        Assert.IsTrue(result.gameObject.activeSelf);
        result.Confirm();
        yield return null;
    }

    [UnityTest]
    public IEnumerator DashChargesAtRestThenShrinksWithSpeed()
    {
        var input = Create("input").AddComponent<InputReader>();
        var playerObject = Create("player", false);
        var model = Create("model").transform;
        model.SetParent(playerObject.transform);
        var player = playerObject.AddComponent<Player>();
        Set(player, "inputReader", input);
        Set(player, "model", model);
        Set(player, "pickupPivot", playerObject.transform);
        Set(player, "suctionPortPivot", playerObject.transform);
        Set(player, "dashLength", 3f);
        Set(player, "dashChargeTime", 0.2f);
        Set(player, "dashExpandedScale", new Vector3(10.5f, 1.7f, 1.5f));
        playerObject.SetActive(true);
        var dash = typeof(Player).GetMethod("Dash", BindingFlags.Instance | BindingFlags.NonPublic);
        dash.Invoke(player, null);

        Vector3 startPosition = playerObject.transform.position;
        yield return new WaitForSeconds(0.1f);
        Assert.AreEqual(startPosition, playerObject.transform.position);
        Assert.Greater(model.localScale.x, 1f);
        Assert.Less(model.localScale.x, 10.5f);

        yield return new WaitForSeconds(0.16f);
        Assert.Greater(playerObject.transform.position.z, startPosition.z);
        Assert.That(model.localScale.x, Is.EqualTo(10.5f).Within(0.01f));

        yield return new WaitForSeconds(0.12f);
        Assert.Less(model.localScale.x, 10.5f);
        Assert.Greater(model.localScale.x, 1f);

        yield return new WaitForSeconds(0.2f);
        Assert.Less(Vector3.Distance(model.localScale, Vector3.one), 0.01f);
    }

    [UnityTest]
    public IEnumerator DashStopsBeforeWallWithoutSlidingOrExitSpeed()
    {
        var input = Create("input").AddComponent<InputReader>();
        var playerObject = Create("player", false);
        var model = Create("model").transform;
        model.SetParent(playerObject.transform);
        var player = playerObject.AddComponent<Player>();
        Set(player, "inputReader", input);
        Set(player, "model", model);
        Set(player, "pickupPivot", playerObject.transform);
        Set(player, "suctionPortPivot", playerObject.transform);
        Set(player, "dashLength", 15f);
        Set(player, "dashChargeTime", 0f);
        playerObject.SetActive(true);

        var wall = Create("wall");
        wall.transform.position = new Vector3(0f, 0.5f, 1.5f);
        wall.transform.localScale = new Vector3(10f, 2f, 0.2f);
        wall.AddComponent<BoxCollider>();
        Physics.SyncTransforms();

        var dash = typeof(Player).GetMethod("Dash", BindingFlags.Instance | BindingFlags.NonPublic);
        dash.Invoke(player, null);
        yield return new WaitForSeconds(0.15f);

        Assert.Less(playerObject.transform.position.z, 1f);
        Assert.Less(Mathf.Abs(playerObject.transform.position.x), 0.01f);
        var velocity = typeof(Player).GetField("velocity", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That((float)velocity.GetValue(player), Is.EqualTo(0f).Within(0.001f));
    }

    [UnityTest]
    public IEnumerator AngledDashUsesRemainingDistanceAlongWallWithoutRebound()
    {
        var input = Create("input").AddComponent<InputReader>();
        var playerObject = Create("player", false);
        playerObject.transform.rotation = Quaternion.Euler(0f, 45f, 0f);
        var model = Create("model").transform;
        model.SetParent(playerObject.transform);
        var player = playerObject.AddComponent<Player>();
        Set(player, "inputReader", input);
        Set(player, "model", model);
        Set(player, "pickupPivot", playerObject.transform);
        Set(player, "suctionPortPivot", playerObject.transform);
        Set(player, "dashLength", 15f);
        Set(player, "dashChargeTime", 0f);
        playerObject.SetActive(true);

        var wall = Create("wall");
        wall.transform.position = new Vector3(0f, 0.5f, 1.5f);
        wall.transform.localScale = new Vector3(50f, 2f, 0.2f);
        wall.AddComponent<BoxCollider>();
        Physics.SyncTransforms();

        var dash = typeof(Player).GetMethod("Dash", BindingFlags.Instance | BindingFlags.NonPublic);
        dash.Invoke(player, null);
        var isDash = typeof(Player).GetField("isDash", BindingFlags.Instance | BindingFlags.NonPublic);
        yield return new WaitUntil(() => !(bool)isDash.GetValue(player));

        float positionAtDashEnd = playerObject.transform.position.x;
        float distanceAtDashEnd = playerObject.transform.position.magnitude;
        var velocity = typeof(Player).GetField("velocity", BindingFlags.Instance | BindingFlags.NonPublic);
        var exitDirection = typeof(Player).GetField("dashExitDirection", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.Greater((float)velocity.GetValue(player), 0f);
        Assert.Less(Mathf.Abs(((Vector3)exitDirection.GetValue(player)).z), 0.01f);
        // 壁へ向かった未移動分もダッシュ距離として消費するため、実移動は15未満になる。
        Assert.That(distanceAtDashEnd, Is.GreaterThan(9f));
        Assert.That(distanceAtDashEnd, Is.LessThan(15f));

        yield return new WaitForSeconds(0.1f);

        Assert.Less(playerObject.transform.position.z, 1f);
        Assert.Greater(playerObject.transform.position.x, 10f);
        Assert.Greater(playerObject.transform.position.x, positionAtDashEnd);
    }

    [UnityTest]
    public IEnumerator DashKeepsExitSlideAtTwentyDegreeWallImpact()
    {
        var input = Create("input").AddComponent<InputReader>();
        var playerObject = Create("player", false);
        playerObject.transform.rotation = Quaternion.Euler(0f, 20f, 0f);
        var model = Create("model").transform;
        model.SetParent(playerObject.transform);
        var player = playerObject.AddComponent<Player>();
        Set(player, "inputReader", input);
        Set(player, "model", model);
        Set(player, "pickupPivot", playerObject.transform);
        Set(player, "suctionPortPivot", playerObject.transform);
        Set(player, "dashLength", 15f);
        Set(player, "dashChargeTime", 0f);
        playerObject.SetActive(true);

        var wall = Create("wall");
        wall.transform.position = new Vector3(0f, 0.5f, 1.5f);
        wall.transform.localScale = new Vector3(50f, 2f, 0.2f);
        wall.AddComponent<BoxCollider>();
        Physics.SyncTransforms();

        var dash = typeof(Player).GetMethod("Dash", BindingFlags.Instance | BindingFlags.NonPublic);
        var isDash = typeof(Player).GetField("isDash", BindingFlags.Instance | BindingFlags.NonPublic);
        var exitDirection = typeof(Player).GetField("dashExitDirection", BindingFlags.Instance | BindingFlags.NonPublic);
        dash.Invoke(player, null);
        yield return new WaitUntil(() => !(bool)isDash.GetValue(player));

        Vector3 storedDirection = (Vector3)exitDirection.GetValue(player);
        Assert.Greater(storedDirection.x, 0.99f);
        Assert.Less(Mathf.Abs(storedDirection.z), 0.01f);
        float positionAtDashEnd = playerObject.transform.position.x;

        yield return new WaitForSeconds(0.1f);

        Assert.Greater(playerObject.transform.position.x, positionAtDashEnd);
        Assert.Less(playerObject.transform.position.z, 1f);
    }
}
