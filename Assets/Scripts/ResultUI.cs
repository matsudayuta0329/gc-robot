using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResultUI : AnimatedUIElement
{
    [SerializeField] private TMP_Text countText;
    [SerializeField] private Button continueButton;
    public bool IsConfirmed { get; private set; }

    public void Init(int count)
    {
        IsConfirmed = false;
        if (countText != null) countText.text = count.ToString();
        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(Confirm);
            continueButton.onClick.AddListener(Confirm);
        }
    }

    public void Confirm() => IsConfirmed = true;
}
