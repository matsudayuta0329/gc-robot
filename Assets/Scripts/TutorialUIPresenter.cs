using System;

public sealed class TutorialUIPresenter : IDisposable
{
    private readonly TutorialUI view;
    private GarbageCounter counter;
    public TutorialUIPresenter(TutorialUI view) => this.view = view;
    public MessageWindowPresenter Message => view.MessageWindow.Presenter;
    public OperationGuidePresenter Guide => view.OperationGuide.Presenter;
    public void Bind(GarbageCounter source)
    {
        Dispose();
        counter = source;
        if (counter == null) return;
        counter.OnUpdateCount += SetCount;
        SetCount(counter.count);
    }
    private void SetCount(int value) => view.SetTrashNum(value);
    public void Dispose()
    {
        if (counter != null) counter.OnUpdateCount -= SetCount;
        counter = null;
    }
}
