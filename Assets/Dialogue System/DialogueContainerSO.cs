using System.Collections.Generic;
using UnityEngine;

// 這個屬性讓我們可以在 Project 視窗中右鍵 Create -> Dialogue -> New Dialogue
[CreateAssetMenu(fileName = "NewDialogue", menuName = "Dialogue/New Dialogue")]
public class DialogueContainerSO : ScriptableObject
{
    [Header("對話資訊")]
    [Tooltip("這段對話的名稱")]
    public string DialogueName = "新的對話";
    
    [Header("節點資料")]
    [Tooltip("所有節點之間的連接資訊")]
    public List<NodeLinkData> NodeLinks = new List<NodeLinkData>();
    
    [Tooltip("所有對話節點的詳細資料")]
    public List<DialogueNodeData> DialogueNodes = new List<DialogueNodeData>();
}