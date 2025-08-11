using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewDialogue", menuName = "Dialogue/New Dialogue")]
public class DialogueContainerSO : ScriptableObject
{
    [Header("對話資訊")]
    public string DialogueName = "新的對話";

    [Header("對話區塊")]
    // 【修改】現在只剩下 Blocks 列表
    public List<DialogueBlock> Blocks = new List<DialogueBlock>();
}