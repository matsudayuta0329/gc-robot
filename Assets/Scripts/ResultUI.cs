using System;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResultUI : AnimatedUIElement
{
    [SerializeField] private TMP_Text countText;
    [SerializeField] private Button continueButton;
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private TMP_Text rankText;
    private int pendingCount;
    private bool submitted;
    private bool closed;
    private int session;
    private ResultUIPresenter presenter;
    public ResultUIPresenter Presenter => presenter ??= new ResultUIPresenter(this);
    public bool IsConfirmed => closed;

    internal void SetCount(int count)
    {
        pendingCount = count;
        if (countText != null) countText.text = $"回収したごみ: {count}個";
        EnsureEntryFields();
        if (nameInput != null) nameInput.text = "Player";
        if (rankText != null) rankText.text = $"予想順位: {ScoreRanking.GetRank(count)}位";
    }

    public override void Enable()
    {
        if (IsVisible) return;
        closed = false;
        submitted = false;
        session++;
        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(Close);
            continueButton.onClick.AddListener(Close);
        }
        base.Enable();
    }

    public void Close()
    {
        if (!IsVisible || closed) return;
        if (!submitted)
        {
            int rank = ScoreRanking.Submit(nameInput != null ? nameInput.text : "Player", pendingCount);
            if (rankText != null) rankText.text = $"今回の順位: {rank}位";
            submitted = true;
        }
        closed = true;
    }

    private void EnsureEntryFields()
    {
        if (nameInput != null && rankText != null) return;
        var parent = countText != null ? countText.transform.parent as RectTransform : transform as RectTransform;
        if (parent == null) return;
        // 自動生成時のみ、既存の結果パネル内に各入力を縦に配置する。
        parent.sizeDelta = new Vector2(Mathf.Max(parent.sizeDelta.x, 560), 340);
        if (countText != null) countText.rectTransform.anchoredPosition = new Vector2(0, 110);
        if (continueButton != null)
            ((RectTransform)continueButton.transform).anchoredPosition = new Vector2(0, -110);
        if (rankText == null) rankText = CreateLabel(parent, "今回の順位: --位", new Vector2(0, 55), new Vector2(420, 40));
        if (nameInput == null)
        {
            var go = new GameObject("PlayerNameInput", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image), typeof(TMP_InputField));
            var rect = go.GetComponent<RectTransform>(); rect.SetParent(parent, false); rect.sizeDelta = new Vector2(360, 42); rect.anchoredPosition = new Vector2(0, -5);
            go.GetComponent<UnityEngine.UI.Image>().color = Color.white;
            var text = CreateLabel(rect, "Player", Vector2.zero, new Vector2(340, 38));
            text.color = Color.black; text.alignment = TextAlignmentOptions.MidlineLeft;
            var input = go.GetComponent<TMP_InputField>(); input.textViewport = text.rectTransform; input.textComponent = text; input.lineType = TMP_InputField.LineType.SingleLine; input.text = "Player"; input.characterLimit = 16;
            nameInput = input;
        }
    }

    private TMP_Text CreateLabel(RectTransform parent, string value, Vector2 position, Vector2 size)
    {
        var go = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        var rect = go.GetComponent<RectTransform>(); rect.SetParent(parent, false); rect.sizeDelta = size; rect.anchoredPosition = position;
        var text = go.GetComponent<TextMeshProUGUI>(); text.text = value; text.fontSize = 26; text.alignment = TextAlignmentOptions.Center;
        if (countText != null) text.font = countText.font;
        return text;
    }

    // 既存のButtonイベントとの互換性を維持する。
    public void Confirm() => Close();

    public async Awaitable WaitForClose(CancellationToken cancellationToken = default)
    {
        int currentSession = session;
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, destroyCancellationToken);
        while (!closed)
        {
            linked.Token.ThrowIfCancellationRequested();
            if (!IsVisible || !isActiveAndEnabled || currentSession != session)
                throw new OperationCanceledException("ResultUI closed externally.");
            await Awaitable.NextFrameAsync(linked.Token);
        }
        linked.Token.ThrowIfCancellationRequested();
    }
}
