// TimedChoiceNode.cs (升級後)
using UnityEngine;
using XNode;
using System.Collections.Generic;

[NodeTint(0.8f, 0.4f, 0.2f)]
public class TimedChoiceNode : BaseNode
{
    [Input(connectionType = ConnectionType.Override)] public BaseNode entry;

    [Header("選項 (有時限)")]
    [Output(dynamicPortList = true)]
    public List<string> choices = new List<string>(); // 創作者在此寫作

    [Header("本地化 (自動生成)")]
    public List<string> choiceKeys = new List<string>(); // 工具將 Key 填寫在這裡

    [Header("時間設定")]
    [Tooltip("玩家必須在此時間內做出選擇 (秒)")]
    public float timeLimit = 10.0f;

    [Header("超時出口")]
    [Tooltip("如果時間到了玩家還沒選擇，流程將從這裡繼續")]
    [Output(connectionType = ConnectionType.Override)] public BaseNode timeoutExit;

    protected override void Init()
    {
        base.Init();
        name = "Timed Choice";
    }

    public BaseNode GetNextNodeForChoice(int choiceIndex)
    {
        if (choiceIndex < 0 || choiceIndex >= choices.Count) return null;
        NodePort choicePort = GetOutputPort("choices " + choiceIndex);
        if (choicePort == null || !choicePort.IsConnected) return null;
        return choicePort.Connection.node as BaseNode;
    }
    
    public BaseNode GetNextNodeOnTimeout()
    {
        NodePort port = GetOutputPort("timeoutExit");
        if (port == null || !port.IsConnected) return null;
        return port.Connection.node as BaseNode;
    }
}