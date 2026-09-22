using TMPro;
using UnityEngine;

public class TutorialUI : AnimatedUIElement
{
    [SerializeField] private MessageWindow messageWindow;
    [SerializeField] private TMP_Text trashCountText;
    [SerializeField] private OperationGuideUI operationGuide;
    public MessageWindow MessageWindow => messageWindow;
    public TMP_Text TrashCountText => trashCountText;
    public OperationGuideUI OperationGuide => operationGuide;

    public void SetTrashNum(int count)
    {
        if (trashCountText != null) trashCountText.text = count.ToString();
    }
}
