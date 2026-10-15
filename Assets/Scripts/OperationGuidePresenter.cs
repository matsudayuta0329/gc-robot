public sealed class OperationGuidePresenter
{
    private readonly OperationGuideUI view;
    public OperationGuidePresenter(OperationGuideUI view) => this.view = view;
    public void Show(string message, ActionType[] actions)
    {
        view.SetText(message);
        view.SetGuideEnable(actions);
        view.Enable();
    }
    public void Hide()
    {
        view.SetGuideEnable(System.Array.Empty<ActionType>());
        view.Disable();
    }
}
