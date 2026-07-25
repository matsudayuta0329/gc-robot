using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class InputReader: MonoBehaviour, PlayerInputAction.IPlayerActions
{
    private PlayerInputAction inputAction;

    public event Action Dash;

    private Vector2 moveInput = new Vector2();
    private Vector2 lookDiff = new Vector2();
    private bool isCollect = false;

    void Awake()
    {
        inputAction = new PlayerInputAction();
        inputAction.Player.SetCallbacks(this);

        inputAction.Player.Enable();
    }

    //外部公開用メソッド
    public Vector2 GetMoveInput()
    {
        return moveInput;
    }

    public Vector2 GetLookDiff()
    {
        return lookDiff;
    }

    public bool GetIsCollect()
    {
        return isCollect;
    }

    //入力取得用ハンドラ
    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput.y = context.ReadValue<float>();
    }

    public void OnSpin(InputAction.CallbackContext context)
    {
        moveInput.x = context.ReadValue<float>();
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        lookDiff = context.ReadValue<Vector2>();
    }

    public void OnCollect(InputAction.CallbackContext context)
    {
        if(context.started)
        {
            isCollect = true;
        }else if(context.canceled){
            isCollect = false;
        }
    }

    public void OnDash(InputAction.CallbackContext context)
    {
        if(context.started)
            Dash?.Invoke();
    }

    public enum InputPattern
    {
        Pause,
        GamePlay,
        UIPlay
    }
}