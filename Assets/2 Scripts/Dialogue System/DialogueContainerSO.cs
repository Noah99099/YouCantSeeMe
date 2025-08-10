using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewDialogue", menuName = "Dialogue/New Dialogue")]
public class DialogueContainerSO : ScriptableObject
{
    [Header("對話資訊")]
    public string DialogueName = "新的對話";

    [Header("對話區塊")]
    public List<DialogueBlock> Blocks = new List<DialogueBlock>();
    
    public List<NodeLinkData> NodeLinks = new List<NodeLinkData>();
}