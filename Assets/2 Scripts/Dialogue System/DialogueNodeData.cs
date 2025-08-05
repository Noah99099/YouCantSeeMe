using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DialogueNodeData
{
    [Tooltip("節點的唯一ID")]
    public string Guid;

    [Tooltip("節點類型")]
    public DialogueNodeType NodeType = DialogueNodeType.Normal;

    [Tooltip("說這句話的角色名稱")]
    public string SpeakerName;

    [Tooltip("節點上顯示的對話內容")]
    [TextArea(3, 10)]
    public string DialogueText;

    [Tooltip("這個節點是否為對話的進入點")]
    public bool EntryPoint;

    [Tooltip("角色名稱的字體樣式")]
    public SpeakerNameStyle NameStyle;

    [Tooltip("角色名稱的顏色")]
    public Color NameColor = Color.white;

    [Tooltip("節點在編輯器圖表中的位置")]
    public Vector2 Position;

    [Header("高級功能")]
    [Tooltip("執行此節點的條件")]
    public List<DialogueCondition> Conditions = new List<DialogueCondition>();

    [Tooltip("執行此節點時觸發的動作")]
    public List<DialogueAction> Actions = new List<DialogueAction>();

    [Tooltip("音效檔案名稱")]
    public string AudioClipName;

    [Tooltip("角色立繪名稱")]
    public string CharacterPortrait;

    [Tooltip("文字顯示速度 (字符/秒，0表示瞬間顯示)")]
    [Range(0, 100)]
    public float TextSpeed = 20f;

    [Tooltip("是否等待音效播放完畢")]
    public bool WaitForAudio = false;

    [Tooltip("節點標籤 (用於跳轉)")]
    public string NodeLabel;
    
    [Header("跳過模式設定")]
    [Tooltip("勾選此項，在使用「跳到下個重點」功能時，會在此節點停留。")]
    public bool IsImportant = false;
}