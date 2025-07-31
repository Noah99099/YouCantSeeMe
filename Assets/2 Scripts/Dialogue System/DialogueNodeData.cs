using System;
using UnityEngine;

// --- 新增: 定義字體樣式的列舉 ---
public enum SpeakerNameStyle
{
    Normal,
    Bold,
    Italic
}

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
    
    // --- 新增欄位 ---
    [Tooltip("角色名稱的字體樣式")]
    public SpeakerNameStyle NameStyle;
    
    [Tooltip("角色名稱的顏色")]
    public Color NameColor = Color.white; // 給一個預設值為白色

    [Tooltip("節點在編輯器圖表中的位置")]
    public Vector2 Position;
}