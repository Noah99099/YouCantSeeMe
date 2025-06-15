// 請用這份程式碼完整取代你的 DialogueNodeData.cs
using System;
using UnityEngine;

[Serializable]
public class DialogueNodeData
{
    [Tooltip("節點的唯一ID")]
    public string Guid;
    
    [Tooltip("說這句話的角色名稱")]
    public string SpeakerName;

    [Tooltip("節點上顯示的對話內容")]
    [TextArea]
    public string DialogueText;

    [Tooltip("這個節點是否為對話的進入點")]
    public bool EntryPoint;

    [Tooltip("節點在編輯器圖表中的位置")]
    public Vector2 Position;
}