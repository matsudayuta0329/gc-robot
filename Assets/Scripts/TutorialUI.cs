using TMPro;
using UnityEngine;

public class TutorialUI : AnimatedUIElement
{
    private TutorialUIPresenter presenter;
    public TutorialUIPresenter Presenter => presenter ??= new TutorialUIPresenter(this);
    [SerializeField] private MessageBox messageBox;
    [SerializeField] private TMP_Text trashCountText;
    [SerializeField] private OperationGuideUI operationGuide;
    public MessageBox MessageBox => messageBox;
    public TMP_Text TrashCountText => trashCountText;
    public OperationGuideUI OperationGuide => operationGuide;

    public void SetTrashNum(int count)
    {
        if (trashCountText != null) trashCountText.text = count.ToString();
    }
}
