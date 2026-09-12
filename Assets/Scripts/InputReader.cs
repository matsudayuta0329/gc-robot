using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections.Generic;

public class InputReader : MonoBehaviour, PlayerInputAction.IPlayerActions
{
    [Serializable]
    public class ActionEntry
    {
        public ActionType actionType;
        public InputAction action;
    }

    [SerializeField] private List<ActionEntry> actions = new List<ActionEntry>();
    private readonly Dictionary<ActionType, List<InputAction>> lookup = new Dictionary<ActionType, List<InputAction>>();
    private PlayerInputAction inputAction;
    private ActionType[] enabledTypes;
    private InputAction interactAction;
    public event Action Dash;
    public event Action Interact;
    public event Action<ActionType> ActionPerformed;
    private Vector2 moveInput;
    private Vector2 lookDiff;
    private bool isCollect;

    private void Awake() => Initialize();
    private void OnEnable() => SetEnableAction(enabledTypes);
    private void OnDisable()
    {
        foreach (var list in lookup.Values)
            foreach (var action in list) action.Disable();
        moveInput = Vector2.zero;
        lookDiff = Vector2.zero;
        isCollect = false;
    }

    private void Initialize()
    {
        if (inputAction != null) return;
        inputAction = new PlayerInputAction();
        foreach (var entry in actions)
        {
            if (entry == null || entry.action == null) continue;
            Add(entry.actionType, entry.action);
            var type = entry.actionType;
            entry.action.performed += context => OnCustomAction(type, context);
            entry.action.canceled += context => OnCustomAction(type, context);
        }
        // 未設定の操作は既存の生成済みInput Actionsを使う。
        inputAction.Player.SetCallbacks(this);
        if (!lookup.ContainsKey(ActionType.Move))
        {
            Add(ActionType.Move, inputAction.Player.Move);
            Add(ActionType.Move, inputAction.Player.Spin);
        }
        if (!lookup.ContainsKey(ActionType.Dash)) Add(ActionType.Dash, inputAction.Player.Dash);
        if (!lookup.ContainsKey(ActionType.Vacuum)) Add(ActionType.Vacuum, inputAction.Player.Collect);
        if (!lookup.ContainsKey(ActionType.Look)) Add(ActionType.Look, inputAction.Player.Look);
        if (!lookup.ContainsKey(ActionType.Interact))
        {
            interactAction = new InputAction("Interact", InputActionType.Button, "<Keyboard>/e");
            interactAction.AddBinding("<Gamepad>/buttonSouth");
            interactAction.performed += context => OnCustomAction(ActionType.Interact, context);
            Add(ActionType.Interact, interactAction);
        }
    }

    private void Add(ActionType type, InputAction action)
    {
        if (!lookup.TryGetValue(type, out var list)) lookup[type] = list = new List<InputAction>();
        list.Add(action);
    }

    public void SetEnableAction(ActionType[] types)
    {
        Initialize();
        enabledTypes = types == null ? null : (ActionType[])types.Clone();
        foreach (var pair in lookup)
            foreach (var action in pair.Value)
            {
                if (isActiveAndEnabled && (types == null || Array.IndexOf(types, pair.Key) >= 0)) action.Enable();
                else action.Disable();
            }
        if (!IsActionEnabled(ActionType.Move)) moveInput = Vector2.zero;
        if (!IsActionEnabled(ActionType.Vacuum)) isCollect = false;
        if (!IsActionEnabled(ActionType.Look)) lookDiff = Vector2.zero;
    }

    public ActionType[] GetEnabledActions()
    {
        Initialize();
        var result = new List<ActionType>();
        foreach (var pair in lookup)
            if (pair.Value.Exists(action => action.enabled)) result.Add(pair.Key);
        return result.ToArray();
    }

    public bool IsActionEnabled(ActionType type)
    {
        Initialize();
        return lookup.TryGetValue(type, out var list) && list.Exists(action => action.enabled);
    }

    public Vector2 GetMoveInput() => moveInput;
    public Vector2 GetLookDiff() => lookDiff;
    public bool GetIsCollect() => isCollect;

    private void OnCustomAction(ActionType type, InputAction.CallbackContext context)
    {
        switch (type)
        {
            case ActionType.Move: moveInput = context.ReadValue<Vector2>(); break;
            case ActionType.Look: lookDiff = context.ReadValue<Vector2>(); break;
            case ActionType.Vacuum: isCollect = !context.canceled && context.ReadValue<float>() > 0; break;
            case ActionType.Dash: if (context.performed) Dash?.Invoke(); break;
            case ActionType.Interact: if (context.performed) Interact?.Invoke(); break;
        }
        if (context.performed) ActionPerformed?.Invoke(type);
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput.y = context.ReadValue<float>();
        if (context.performed) ActionPerformed?.Invoke(ActionType.Move);
    }
    public void OnSpin(InputAction.CallbackContext context)
    {
        moveInput.x = context.ReadValue<float>();
        if (context.performed) ActionPerformed?.Invoke(ActionType.Move);
    }
    public void OnLook(InputAction.CallbackContext context) => lookDiff = context.ReadValue<Vector2>();
    public void OnCollect(InputAction.CallbackContext context)
    {
        if (context.started) isCollect = true;
        else if (context.canceled) isCollect = false;
        if (context.performed) ActionPerformed?.Invoke(ActionType.Vacuum);
    }
    public void OnDash(InputAction.CallbackContext context)
    {
        if (context.started) Dash?.Invoke();
        if (context.performed) ActionPerformed?.Invoke(ActionType.Dash);
    }
    private void OnDestroy()
    {
        OnDisable();
        inputAction?.Dispose();
        interactAction?.Dispose();
        foreach (var entry in actions) entry?.action?.Dispose();
    }

    public enum InputPattern { Pause, GamePlay, UIPlay }
}
