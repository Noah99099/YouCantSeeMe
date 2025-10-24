using UnityEngine;
using XNode;

// 我們將這個節點設定為不可連接，並給它一個中性的顏色
[NodeTint(0.4f, 0.4f, 0.4f)]
public class CommentNode : Node
{
    [TextArea(10, 20)] // 讓文字輸入框在 Inspector 中更大
    public string text;

    // 我們不需要 xNode 的 GetValue 方法，所以可以留空
    public override object GetValue(NodePort port) {
        return null;
    }
}