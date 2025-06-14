using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

public class DialogueNode : Node
{
    public string GUID;
    
    public string SpeakerName;

    public string DialogueText;
    public bool EntryPoint = false;

    public void SetSpeakerName(string newName)
    {
        SpeakerName = newName;
    }
    
    public void SetDialogueText(string newText)
    {
        DialogueText = newText;
    }
}