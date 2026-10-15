public sealed class ResultUIPresenter
{
    private readonly ResultUI view;
    public ResultUIPresenter(ResultUI view) => this.view = view;
    public void SetResult(int collectedCount) => view.SetCount(collectedCount);
}
