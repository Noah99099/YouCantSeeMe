using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class DialogueLine
{
    [Header("角色與表情")]
    public string characterID;    // 說話者的 ID
    public string expression;     // 想要的表情關鍵字 (e.g., "happy", "sad")
    public CharacterPosition position;  //指定角色位置
    public CharacterAnimationType animation;   //角色動畫

    [Header("對話內容")]
    public bool overrideName;     // 是否要覆寫 Profile 中的預設名字？
    public string speakerName;    // 如果 overrideName 為 true，則使用此名字
    [Tooltip("如果勾選此項，將作為旁白顯示，忽略角色ID、立繪和名字。")]
    public bool isNarration;

    [TextArea(3, 10)]
    public string content;

    [Header("本地化 (自動生成)")]
    public string contentKey; // 我們的工具將會把生成的 Key 填寫在這裡

    [Header("聲音")] // <--- 新增區塊 ---
    public AudioClip voiceClip;      // 語音
    public AudioClip soundEffect;    // 伴隨音效 (例如：驚嘆、開門聲)

    [Header("觸發事件")]
    public UnityEvent onShowLine;
}

// 可以在 DialogueLine.cs 檔案內或外部定義
public enum CharacterEffect
{
    None,
    Shake,
    FadeIn,
    SlideIn
}