using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResultUI : AnimatedUIElement
{
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text countText;
    [SerializeField] private Button continueButton;
    public bool IsConfirmed { get; private set; }

    public void Init(int score, int count)
    {
        IsConfirmed = false;
        if (scoreText != null) scoreText.text = score.ToString();
        if (countText != null) countText.text = count.ToString();
        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(Confirm);
            continueButton.onClick.AddListener(Confirm);
        }
    }

    public void Confirm() => IsConfirmed = true;
}
