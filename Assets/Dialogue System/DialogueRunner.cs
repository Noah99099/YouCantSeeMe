using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DialogueRunner : MonoBehaviour
{
    [Header("對話資料")]
    [SerializeField] private DialogueContainerSO dialogueContainer;

    [Header("執行元件")]
    [SerializeField] private DialogueUI dialogueUI;

    private DialogueNodeData _currentNode;

    // 遊戲開始時可以自動觸發，或由其他腳本呼叫
    private void Start()
    {
        // 找到對話的進入點
        var entryPointNode = dialogueContainer.DialogueNodes.Find(node => node.EntryPoint);
        if (entryPointNode != null)
        {
            _currentNode = entryPointNode;
            ShowNode(_currentNode);
        }
        else
        {
            Debug.LogError("找不到對話進入點 (Entry Point)！");
            dialogueUI.Hide();
        }
    }
    
    /// <summary>
    /// 顯示指定的節點
    /// </summary>
    private void ShowNode(DialogueNodeData node)
    {
        _currentNode = node;
        
        // 找到所有從當前節點出發的連接
        var allLinks = dialogueContainer.NodeLinks.Where(link => link.BaseNodeGuid == _currentNode.Guid).ToList();
        
        // 分離出「繼續」連線和「選項」連線
        var continueLink = allLinks.FirstOrDefault(link => link.PortName == "繼續");
        var choiceLinks = allLinks.Where(link => link.PortName != "繼續").ToList();

        // 優先顯示選項
        if (choiceLinks.Count > 0)
        {
            dialogueUI.ShowNode(node, choiceLinks, OnChoiceSelected);
        }
        // 如果沒有選項，但有「繼續」連線
        else if (continueLink != null)
        {
            // 這裡我們也用按鈕來代表「繼續」，但只有一個
            dialogueUI.ShowNode(node, new List<NodeLinkData> { continueLink }, OnChoiceSelected);
        }
        // 如果沒有任何出口，代表對話結束
        else
        {
            dialogueUI.Hide();
        }
    }

    /// <summary>
    /// 當玩家點擊一個選項按鈕時被呼叫
    /// </summary>
    private void OnChoiceSelected(NodeLinkData selectedLink)
    {
        // 找到選擇的目標節點
        var nextNode = dialogueContainer.DialogueNodes.Find(node => node.Guid == selectedLink.TargetNodeGuid);
        if (nextNode != null)
        {
            ShowNode(nextNode);
        }
        else
        {
            Debug.LogError("找不到目標節點！對話結束。");
            dialogueUI.Hide();
        }
    }
}