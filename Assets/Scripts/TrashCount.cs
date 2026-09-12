using TMPro;
using UnityEngine;

public class TrashCount : AnimatedUIElement
{
    [SerializeField] private TMP_Text countText;

    // 表示方法は後から実装するため、仕様どおり空関数。
    public void SetTrashNum(int count) { }
}
