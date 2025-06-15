using System;
using UnityEngine;

[Serializable]
public class NodeLinkData
{
    [Tooltip("來源節點的ID")]
    public string BaseNodeGuid;

    [Tooltip("目標節點的ID")]
    public string TargetNodeGuid;

    [Tooltip("選項接口的文字 (如果有的話)")]
    public string PortName; // 這將會是玩家看到的選項文字，例如「好的」或「我拒絕」
}