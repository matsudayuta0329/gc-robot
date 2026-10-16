using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class UIAnimationSetup
{
    private const string Folder = "Assets/UIAnimations";

    [MenuItem("GC Robot/UI/Create Animation Templates")]
    public static void CreateTemplates()
    {
        Directory.CreateDirectory(Folder);
        AssetDatabase.Refresh();
        CreateElement();
        CreateParent();
        AssetDatabase.SaveAssets();
    }

    private static AnimationClip Clip(string name, string path, float from, float to,
        params AnimationEvent[] events)
    {
        string assetPath = Folder + "/" + name + ".anim";
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
        if (clip != null) return clip;
        clip = new AnimationClip { name = name };
        clip.SetCurve(path, typeof(CanvasGroup), "m_Alpha", AnimationCurve.Linear(0f, from, 0.5f, to));
        AnimationUtility.SetAnimationEvents(clip, events);
        AssetDatabase.CreateAsset(clip, assetPath);
        return clip;
    }

    private static AnimationEvent Event(float time, string method) =>
        new AnimationEvent { time = time, functionName = method };

    private static AnimatorState State(AnimatorStateMachine machine, string name, Motion motion = null)
    {
        var state = machine.AddState(name);
        state.motion = motion;
        // Motionに含まれないプロパティの既定値書き戻しを避ける。
        state.writeDefaultValues = false;
        return state;
    }

    private static void Transition(AnimatorState source, AnimatorState target, string trigger = null)
    {
        var transition = source.AddTransition(target);
        transition.duration = 0f;
        transition.offset = 0f;
        transition.hasExitTime = trigger == null;
        transition.exitTime = 1f;
        if (trigger != null) transition.AddCondition(AnimatorConditionMode.If, 0f, trigger);
    }

    private static void CreateElement()
    {
        string path = Folder + "/UIElement.controller";
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(path) != null) return;
        var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        controller.AddParameter("enable", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("disable", AnimatorControllerParameterType.Trigger);
        var machine = controller.layers[0].stateMachine;
        var enableClip = Clip("Enable", "", 0f, 1f, Event(0.5f, "CompleteEnable"));
        var before = State(machine, "BedoreEnable", enableClip);
        before.speed = 0f;
        var enable = State(machine, "Enable", enableClip);
        var idle = State(machine, "Idle");
        var disable = State(machine, "Disable",
            Clip("Disable", "", 1f, 0f, Event(0.5f, "CompleteDisable")));
        machine.defaultState = before;
        Transition(before, enable, "enable");
        Transition(enable, idle);
        Transition(idle, disable, "disable");
        Transition(enable, disable, "disable");
        Transition(before, disable, "disable");
        Transition(disable, before);
    }

    private static void CreateParent()
    {
        string path = Folder + "/UIParent.controller";
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(path) != null) return;
        var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        foreach (string trigger in new[] { "startGame", "endGame", "nextScene" })
            controller.AddParameter(trigger, AnimatorControllerParameterType.Trigger);
        var machine = controller.layers[0].stateMachine;
        var idleClip = Clip("IdleTransition", "Transition", 0f, 0f);
        var inactive = new Keyframe(0f, 0f)
            { inTangent = float.PositiveInfinity, outTangent = float.PositiveInfinity };
        var inactiveEnd = new Keyframe(0.5f, 0f)
            { inTangent = float.PositiveInfinity, outTangent = float.PositiveInfinity };
        idleClip.SetCurve("Transition", typeof(GameObject), "m_IsActive",
            new AnimationCurve(inactive, inactiveEnd));
        var idle = State(machine, "Idle", idleClip);
        machine.defaultState = idle;
        var startClip = Clip("StartGame", "Transition", 1f, 0f,
            Event(0.1f, "EnableElements"), Event(0.5f, "CompleteStart"));
        // 終了直前にオーバーレイを非アクティブへ戻し、IdleTransitionでも維持する。
        startClip.SetCurve("Transition", typeof(CanvasGroup), "m_Alpha",
            AnimationCurve.Linear(0f, 1f, 0.45f, 0f));
        var show = new Keyframe(0f, 1f) { inTangent = float.PositiveInfinity, outTangent = float.PositiveInfinity };
        var hide = new Keyframe(29f / 60f, 0f) { inTangent = float.PositiveInfinity, outTangent = float.PositiveInfinity };
        startClip.SetCurve("Transition", typeof(GameObject), "m_IsActive", new AnimationCurve(show, hide));
        var start = State(machine, "StartGame", startClip);
        var end = State(machine, "EndGame", Clip("EndGame", "Transition", 0f, 0f,
            Event(0.1f, "DisableElements"), Event(0.3f, "EnableResult"), Event(0.5f, "CompleteEnd")));
        var next = State(machine, "NextScene", Clip("NextScene", "Transition", 0f, 1f,
            Event(0.1f, "DisableElements"), Event(0.1f, "DisableResult"), Event(0.5f, "CompleteNextScene")));
        Transition(idle, start, "startGame");
        Transition(start, idle);
        Transition(idle, end, "endGame");
        Transition(end, idle);
        Transition(idle, next, "nextScene");
        Transition(next, idle);
    }

    [MenuItem("GC Robot/UI/Assign Templates To Selected Hierarchy")]
    public static void AssignSelected()
    {
        CreateTemplates();
        foreach (var selected in Selection.gameObjects)
        {
            foreach (var element in selected.GetComponentsInChildren<AnimatedUIElement>(true))
                Assign(element, "UIElement");
            foreach (var parent in selected.GetComponentsInChildren<UIParent>(true))
                Assign(parent, "UIParent");
        }
    }

    private static void Assign(Component component, string controllerName)
    {
        var animator = component.GetComponent<Animator>();
        if (animator == null) animator = Undo.AddComponent<Animator>(component.gameObject);
        Undo.RecordObject(animator, "Assign UI controller");
        animator.runtimeAnimatorController =
            AssetDatabase.LoadAssetAtPath<AnimatorController>(Folder + "/" + controllerName + ".controller");
        animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        var serialized = new SerializedObject(component);
        serialized.FindProperty("animator").objectReferenceValue = animator;
        if (component is AnimatedUIElement)
        {
            var group = component.GetComponent<CanvasGroup>();
            if (group == null) group = Undo.AddComponent<CanvasGroup>(component.gameObject);
            serialized.FindProperty("canvasGroup").objectReferenceValue = group;
            AssignClipIfEmpty(serialized, "enableAnimation", "Enable");
            AssignClipIfEmpty(serialized, "disableAnimation", "Disable");
        }
        else if (component is UIParent)
        {
            AssignClipIfEmpty(serialized, "startGameAnimation", "StartGame");
            AssignClipIfEmpty(serialized, "endGameAnimation", "EndGame");
            AssignClipIfEmpty(serialized, "nextSceneAnimation", "NextScene");
        }
        serialized.ApplyModifiedProperties();
    }

    private static void AssignClipIfEmpty(SerializedObject target, string field, string clipName)
    {
        var property = target.FindProperty(field);
        if (property.objectReferenceValue == null)
            property.objectReferenceValue = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                Folder + "/" + clipName + ".anim");
    }
}
