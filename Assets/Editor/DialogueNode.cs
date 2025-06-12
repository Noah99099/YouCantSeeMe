using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

public class DialogueNode : Node
{
    // 這個節點的唯一識別碼
    public string GUID;
    
    // 這個節點的對話內容
    public string DialogueText;
    
    // 標記這個節點是否為對話的進入點
    public bool EntryPoint = false;

    // --- 新增部分 ---
    // 當我們在節點上新增一個文字輸入框後，
    // 需要一個方法來更新 DialogueText 這個變數。
    public void SetDialogueText(string newText)
    {
        DialogueText = newText;
    }
}