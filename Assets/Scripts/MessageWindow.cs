using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 会話とロビーの確認ダイアログで共有する。
public class MessageWindow : AnimatedUIElement
{
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Button submitButton;
    [SerializeField, Min(0)] private float characterInterval = 0.04f;
    private int textVersion;
    public bool IsTyping { get; private set; }

    public async Awaitable SetText(string text)
    {
        int version = ++textVersion;
        if (messageText == null) return;
        messageText.text = text ?? string.Empty;
        messageText.maxVisibleCharacters = int.MaxValue;
        messageText.ForceMeshUpdate();
        int length = messageText.textInfo.characterCount;
        messageText.maxVisibleCharacters = 0;
        IsTyping = true;
        try
        {
            for (int i = 1; i <= length && version == textVersion; i++)
            {
                messageText.maxVisibleCharacters = i;
                float end = Time.unscaledTime + characterInterval;
                while (Time.unscaledTime < end && version == textVersion)
                    await Awaitable.NextFrameAsync(destroyCancellationToken);
            }
        }
        finally
        {
            if (version == textVersion) IsTyping = false;
        }
    }

    public void DisplayFullText()
    {
        textVersion++;
        IsTyping = false;
        if (messageText != null) messageText.maxVisibleCharacters = int.MaxValue;
    }

    public void Init(string message, Action cancelHandler, Action submitHandler)
    {
        DisplayFullText();
        if (messageText != null) messageText.text = message ?? string.Empty;
        Bind(cancelButton, cancelHandler);
        Bind(submitButton, submitHandler);
    }

    private static void Bind(Button button, Action handler)
    {
        if (button == null) return;
        // 新しいイベントにしてInspectorの永続リスナーもクリアする。
        button.onClick = new Button.ButtonClickedEvent();
        if (handler != null) button.onClick.AddListener(() => handler());
        button.gameObject.SetActive(handler != null);
    }

    public override void Disable()
    {
        DisplayFullText();
        base.Disable();
    }
}
