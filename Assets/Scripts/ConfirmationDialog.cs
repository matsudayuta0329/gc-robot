using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// はい／いいえの確認だけを扱う。
public class ConfirmationDialog : AnimatedUIElement
{
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Button submitButton;
    private ConfirmationDialogPresenter presenter;
    public ConfirmationDialogPresenter Presenter => presenter ??= new ConfirmationDialogPresenter(this);

    public void Init(string message, Action cancelHandler, Action submitHandler)
    {
        if (messageText != null) messageText.text = message ?? string.Empty;
        Bind(cancelButton, cancelHandler);
        Bind(submitButton, submitHandler);
    }

    public void ClearHandlers() { Bind(cancelButton, null); Bind(submitButton, null); }

    private static void Bind(Button button, Action handler)
    {
        if (button == null) return;
        button.onClick = new Button.ButtonClickedEvent();
        if (handler != null) button.onClick.AddListener(() => handler());
        button.gameObject.SetActive(handler != null);
    }
}
