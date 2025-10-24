using UnityEditor;
using XNodeEditor;
using System; // 需要引用 System 來使用 Type

[CustomNodeGraphEditor(typeof(DialogueGraph))]
public class DialogueGraphEditor : NodeGraphEditor 
{
    public override string GetNodeMenuName(System.Type type) 
    {
        // --- 核心修正：加入對 CommentNode 的例外允許 ---
        if (typeof(BaseNode).IsAssignableFrom(type) || type == typeof(CommentNode))
        {
            // 將節點路徑美化，例如 "Dialogue/LineNode" -> "Line"
            return base.GetNodeMenuName(type).Replace("Node", "");
        }
        return null;
    }
}