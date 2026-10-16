using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class UIFlowTests
{
    private readonly List<GameObject> objects = new List<GameObject>();
    private GameObject Create(string name, bool active = true)
    {
        var go = new GameObject(name);
        go.SetActive(active);
        objects.Add(go);
        return go;
    }
    private static void Set(object target, string name, object value)
    {
        for (var type = target.GetType(); type != null; type = type.BaseType)
        {
            var field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null) { field.SetValue(target, value); return; }
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

    [UnityTest]
    public IEnumerator ResultWaitHandlesEarlyCloseAndResetsOnReuse()
    {
        var result = Create("Result").AddComponent<ResultUI>();
        result.Enable();
        result.Close();
        result.Close();
        bool completed = false;
        async void Wait() { await result.WaitForClose(); completed = true; }
        Wait();
        Assert.IsTrue(completed);
        result.Disable();
        result.Enable();
        completed = false;
        Wait();
        yield return null;
        Assert.IsFalse(completed);
        result.Close();
        yield return null;
        Assert.IsTrue(completed);
    }

    [UnityTest]
    public IEnumerator PopupReturnsAnswerOnlyAfterHidingAndCanBeReused()
    {
        var popup = Create("Popup").AddComponent<MessageWindow>();
        var yes = Create("Yes").AddComponent<Button>();
        var no = Create("No").AddComponent<Button>();
        Set(popup, "submitButton", yes);
        Set(popup, "cancelButton", no);
        bool? answer = null;
        async void Ask() { answer = await popup.Presenter.ShowAndWaitAsync("Continue?"); }
        Ask();
        no.onClick.Invoke();
        yield return null;
        Assert.AreEqual(false, answer);
        Assert.IsFalse(popup.gameObject.activeSelf);
        answer = null;
        Ask();
        yes.onClick.Invoke();
        yield return null;
        Assert.AreEqual(true, answer);
        Assert.IsFalse(popup.gameObject.activeSelf);
    }

    [UnityTest]
    public IEnumerator CancelledPopupReleasesWaitAndHides()
    {
        var popup = Create("Popup").AddComponent<MessageWindow>();
        using var cancellation = new CancellationTokenSource();
        bool cancelled = false;
        async void Ask()
        {
            try { await popup.Presenter.ShowAndWaitAsync("Continue?", cancellation.Token); }
            catch (OperationCanceledException) { cancelled = true; }
        }
        Ask();
        cancellation.Cancel();
        yield return null;
        Assert.IsTrue(cancelled);
        Assert.IsFalse(popup.IsVisible);
    }

    [UnityTest]
    public IEnumerator ParentWithResultWaitsForClose()
    {
        var parent = Create("Parent").AddComponent<UIParent>();
        var result = Create("Result").AddComponent<ResultUI>();
        Set(parent, "resultUI", result);
        bool finished = false;
        async void Finish() { await parent.FinishAsync(); finished = true; }
        Finish();
        yield return null;
        Assert.IsTrue(result.IsVisible);
        Assert.IsFalse(finished);
        result.Close();
        yield return null;
        Assert.IsTrue(finished);
        Assert.IsFalse(result.IsVisible);
    }

    [UnityTest]
    public IEnumerator RealAnimatorHoldsFirstPoseAndWaitsForChildAndExitEvents()
    {
        var go = Create("Element");
        var group = go.AddComponent<CanvasGroup>();
        var animator = go.AddComponent<Animator>();
        animator.runtimeAnimatorController = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
            "Assets/UIAnimations/UIElement.controller");
        Assert.IsNotNull(animator.runtimeAnimatorController);
        var element = go.AddComponent<GameUI>();
        Set(element, "animator", animator);
        Set(element, "canvasGroup", group);
        var enableClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AnimationClip>(
            "Assets/UIAnimations/Enable.anim");
        Set(element, "enableAnimation", enableClip);
        Set(element, "disableAnimation", UnityEditor.AssetDatabase.LoadAssetAtPath<AnimationClip>(
            "Assets/UIAnimations/Disable.anim"));
        element.PrepareEnable();
        var elementOverride = animator.runtimeAnimatorController as AnimatorOverrideController;
        Assert.IsNotNull(elementOverride);
        Assert.AreSame(enableClip, elementOverride["Enable"]);
        Assert.That(group.alpha, Is.EqualTo(0f).Within(0.01f));
        yield return new WaitForSecondsRealtime(0.1f);
        Assert.That(group.alpha, Is.EqualTo(0f).Within(0.01f));

        var parentObject = Create("Parent");
        var parentAnimator = parentObject.AddComponent<Animator>();
        var overlay = Create("Transition");
        overlay.transform.SetParent(parentObject.transform);
        var overlayGroup = overlay.AddComponent<CanvasGroup>();
        overlayGroup.alpha = 1f;
        overlay.SetActive(false);
        parentAnimator.runtimeAnimatorController = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
            "Assets/UIAnimations/UIParent.controller");
        Assert.IsNotNull(parentAnimator.runtimeAnimatorController);
        var parent = parentObject.AddComponent<UIParent>();
        Set(parent, "animator", parentAnimator);
        Set(parent, "startGameAnimation", UnityEditor.AssetDatabase.LoadAssetAtPath<AnimationClip>(
            "Assets/UIAnimations/StartGame.anim"));
        Set(parent, "endGameAnimation", UnityEditor.AssetDatabase.LoadAssetAtPath<AnimationClip>(
            "Assets/UIAnimations/EndGame.anim"));
        Set(parent, "nextSceneAnimation", UnityEditor.AssetDatabase.LoadAssetAtPath<AnimationClip>(
            "Assets/UIAnimations/NextScene.anim"));
        Set(parent, "elements", new List<AnimatedUIElement> { element });
        bool started = false;
        async void StartUI() { await parent.Enable(); started = true; }
        StartUI();
        var parentOverride = parentAnimator.runtimeAnimatorController as AnimatorOverrideController;
        Assert.IsNotNull(parentOverride);
        Assert.AreSame(UnityEditor.AssetDatabase.LoadAssetAtPath<AnimationClip>(
            "Assets/UIAnimations/IdleTransition.anim"), parentOverride["IdleTransition"]);
        yield return new WaitForSecondsRealtime(0.2f);
        Assert.IsFalse(started);
        Assert.IsTrue(overlay.activeSelf);
        yield return new WaitForSecondsRealtime(0.6f);
        Assert.IsTrue(started);
        Assert.IsTrue(parentAnimator.GetCurrentAnimatorStateInfo(0).IsName("Idle"));
        Assert.IsFalse(overlay.activeSelf);
        Assert.That(overlayGroup.alpha, Is.EqualTo(0f).Within(0.01f));
        Assert.That(group.alpha, Is.EqualTo(1f).Within(0.01f));
        Assert.IsTrue(group.interactable);
        bool finished = false;
        async void FinishUI() { await parent.FinishAsync(); finished = true; }
        FinishUI();
        yield return new WaitForSecondsRealtime(0.2f);
        Assert.IsFalse(finished);
        yield return new WaitForSecondsRealtime(0.6f);
        Assert.IsTrue(finished);
        Assert.IsFalse(element.gameObject.activeSelf);
    }

    [UnityTest]
    public IEnumerator LobbyPopupLocksInputAndRejectsSecondRequest()
    {
        var input = Create("Input").AddComponent<InputReader>();
        var playerObject = Create("Player", false);
        var player = playerObject.AddComponent<Player>();
        Set(player, "inputReader", input);
        playerObject.SetActive(true);
        var popup = Create("Popup").AddComponent<MessageWindow>();
        var no = Create("No").AddComponent<Button>();
        Set(popup, "cancelButton", no);
        var managerObject = Create("Lobby", false);
        var manager = managerObject.AddComponent<LobbyManager>();
        Set(manager, "player", player);
        Set(manager, "inputReader", input);
        Set(manager, "popup", popup);
        Set(manager, "uiParent", Create("Parent").AddComponent<UIParent>());
        managerObject.SetActive(true);
        yield return null;
        bool completed = false;
        async void Ask() { await manager.ConfirmAndTravelAsync("First", ""); completed = true; }
        Ask();
        Assert.IsTrue(player.IsPaused);
        Assert.IsEmpty(input.GetEnabledActions());
        bool secondCompleted = false;
        async void AskAgain() { await manager.ConfirmAndTravelAsync("Second", ""); secondCompleted = true; }
        AskAgain();
        Assert.IsTrue(secondCompleted);
        Assert.IsFalse(completed);
        no.onClick.Invoke();
        yield return null;
        Assert.IsTrue(completed);
        Assert.IsFalse(player.IsPaused);
        Assert.IsTrue(input.IsActionEnabled(ActionType.Move));
    }
}
