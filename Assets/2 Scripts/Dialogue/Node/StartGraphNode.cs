using UnityEngine;
using XNode;

/// <summary>
/// 【新節點】
/// 一個「終點」節點，它會立即結束當前圖形，
/// 並無縫跳轉到另一個指定的對話圖形。
/// </summary>
[NodeTint(0.9f, 0.5f, 0.1f)] // 給它一個醒目的橘色
public class StartGraphNode : BaseNode
{
    [Input] public BaseNode entry;

    [Header("要跳轉到的圖形")]
    [Tooltip("當流程執行到此節點時，立即開始這個新的對話圖形。")]
    public DialogueGraph graphToStart;
    
    protected override void Init() { base.Init(); name = "Start Graph"; }

    // 這個節點沒有 "exit" 出口，因為它會立即跳轉，
    // 所以 GetNextNode() 永遠返回 null。
    public override BaseNode GetNextNode()
    {
        return null;
    }
}