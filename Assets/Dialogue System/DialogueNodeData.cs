using System;
using UnityEngine;

// [Serializable] 屬性讓這個 class 的實例可以被 Unity 序列化，
// 這樣我們才能將它儲存到 ScriptableObject 中。
[Serializable]
public class DialogueNodeData
{
    [Tooltip("節點的唯一ID")]
    public string Guid; // 我們用 GUID 來確保每個節點都有一個絕對不會重複的 ID

    [Tooltip("節點上顯示的對話內容")]
    [TextArea]
    public string DialogueText;

    [Tooltip("節點在編輯器圖表中的位置")]
    public Vector2 Position;
}