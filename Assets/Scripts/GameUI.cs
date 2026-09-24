using TMPro;
using UnityEngine;

public class GameUI : AnimatedUIElement
{
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private TMP_Text countText;

    public void SetTime(float seconds)
    {
        if (timeText != null) timeText.text = Mathf.CeilToInt(Mathf.Max(0, seconds)).ToString();
    }

    public void SetTrashNum(int count)
    {
        if (countText != null) countText.text = count.ToString();
    }
}
