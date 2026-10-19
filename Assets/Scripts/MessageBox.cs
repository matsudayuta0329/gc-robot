using System;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 会話文の文字送りと「次へ」だけを扱う。
public class MessageBox : AnimatedUIElement
{
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button nextButton;
    [SerializeField, Min(0)] private float characterInterval = 0.04f;
    private MessageBoxPresenter presenter;
    private int textVersion;
    public MessageBoxPresenter Presenter => presenter ??= new MessageBoxPresenter(this);
    public bool IsTyping { get; private set; }

    public void SetAdvanceHandler(Action handler)
    {
        if (nextButton == null) return;
        nextButton.onClick = new Button.ButtonClickedEvent();
        if (handler != null) nextButton.onClick.AddListener(() => handler());
    }

    public async Awaitable SetText(string text, CancellationToken cancellationToken = default)
    {
        int version = ++textVersion;
        if (messageText == null) return;
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, destroyCancellationToken);
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
                    await Awaitable.NextFrameAsync(linked.Token);
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

    public override void Disable()
    {
        DisplayFullText();
        base.Disable();
    }
}
