using UnityEngine;
using XNode;
using System.Collections.Generic;

[NodeTint(0.7f, 0.2f, 0.2f)] // 使用選項節點應有的顏色
public class ChoiceNode : BaseNode
{
    [Input(connectionType = ConnectionType.Override)] public BaseNode entry;

    [Header("選項")]
    [Output(dynamicPortList = true)]
    public List<string> choices = new List<string>();

    [Header("本地化 (自動生成)")]
    public List<string> choiceKeys = new List<string>(); // 工具將 Key 填寫在這裡

    protected override void Init()
    {
        base.Init();
        name = "Choice";
    }

    public BaseNode GetNextNodeForChoice(int choiceIndex)
    {
        if (choiceIndex < 0 || choiceIndex >= choices.Count) return null;
        
        NodePort choicePort = GetOutputPort("choices " + choiceIndex);
        if (choicePort == null || !choicePort.IsConnected) return null;
        
        // --- 附帶修正：這裡的變數名稱是 choicePort ---
        return choicePort.Connection.node as BaseNode;
    }
}