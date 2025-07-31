using UnityEditor.Experimental.GraphView;
using UnityEngine; // 需要引用 UnityEngine 來使用 Color
using UnityEngine.UIElements;

public class DialogueNode : Node
{
    public string GUID;
    public string SpeakerName;
    public string DialogueText;
    public bool EntryPoint = false;
    
    // --- 新增欄位 ---
    public SpeakerNameStyle NameStyle;
    public Color NameColor;

    // --- 新增對應的 Set 方法 ---
    public void SetNameStyle(SpeakerNameStyle newStyle)
    {
        NameStyle = newStyle;
    }
    
    public void SetNameColor(Color newColor)
    {
        NameColor = newColor;
    }

    public void SetSpeakerName(string newName)
    {
        SpeakerName = newName;
    }
    
    public void SetDialogueText(string newText)
    {
        DialogueText = newText;
    }
}