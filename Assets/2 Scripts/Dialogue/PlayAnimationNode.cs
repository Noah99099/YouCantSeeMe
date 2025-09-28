using UnityEngine;
using XNode;

[NodeTint(0.7f, 0.5f, 0.2f)] // 給它一個不同的顏色，例如橘色
public class PlayAnimationNode : BaseNode
{
    [Input(connectionType = ConnectionType.Override)] public BaseNode entry;
    [Output(connectionType = ConnectionType.Override)] public BaseNode exit;

    [Header("動畫目標")]
    // --- 核心修正：將 Animator 引用改為 string 名稱 ---
    [Tooltip("將場景中帶有 Animator 的物件的「名字」填寫在這裡")]
    public string targetObjectName;

    [Tooltip("要觸發的 Animator Trigger 的名稱")]
    public string triggerName;
    
    // (未來可擴展功能：bool waitForCompletion，決定是否要等待動畫播完再繼續)

    protected override void Init()
    {
        base.Init();
        name = "Play Animation";
    }

    public override BaseNode GetNextNode()
    {
        NodePort exitPort = GetOutputPort("exit");
        if (exitPort == null || !exitPort.IsConnected) return null;
        return exitPort.Connection.node as BaseNode;
    }
}