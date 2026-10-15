using System;

public sealed class GameUIPresenter : IDisposable
{
    private readonly GameUI view;
    private GarbageCounter counter;
    public GameUIPresenter(GameUI view) => this.view = view;
    public void Bind(GarbageCounter source)
    {
        Dispose();
        counter = source;
        if (counter == null) return;
        counter.OnUpdateCount += SetCount;
        SetCount(counter.count);
    }
    public void SetTime(float seconds) => view.SetTime(seconds);
    private void SetCount(int count) => view.SetTrashNum(count);
    public void Dispose()
    {
        if (counter != null) counter.OnUpdateCount -= SetCount;
        counter = null;
    }
}
