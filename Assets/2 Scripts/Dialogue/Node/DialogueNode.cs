using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Dialogue Node", menuName = "Dialogue/Dialogue Node")]
public class DialogueNode : ScriptableObject
{
    public bool isImportantNode = false; // 用於跳過功能
    public List<DialogueLine> lines;
    [Header("節點結束後的選項")]
    public List<Choice> choices;
}