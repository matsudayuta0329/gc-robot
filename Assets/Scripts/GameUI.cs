using TMPro;
using UnityEngine;

public class GameUI : AnimatedUIElement
{
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text countText;
    [SerializeField] private TrashCount trashCount;
    [SerializeField] private OperationGuideUI operationGuide;
    public TrashCount TrashCount => trashCount;
    public OperationGuideUI OperationGuide => operationGuide;

    public void SetTime(float seconds)
    {
        if (timeText != null) timeText.text = Mathf.CeilToInt(Mathf.Max(0, seconds)).ToString();
    }

    public void SetScore(int score)
    {
        if (scoreText != null) scoreText.text = score.ToString();
    }

    public void SetTrashNum(int count)
    {
        if (countText != null) countText.text = count.ToString();
        if (trashCount != null) trashCount.SetTrashNum(count);
    }
}
