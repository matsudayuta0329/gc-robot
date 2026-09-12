using UnityEngine;

public class TutorialUI : AnimatedUIElement
{
    [SerializeField] private MessageWindow messageWindow;
    [SerializeField] private TrashCount trashCount;
    [SerializeField] private OperationGuideUI operationGuide;
    public MessageWindow MessageWindow => messageWindow;
    public TrashCount TrashCount => trashCount;
    public OperationGuideUI OperationGuide => operationGuide;
}
