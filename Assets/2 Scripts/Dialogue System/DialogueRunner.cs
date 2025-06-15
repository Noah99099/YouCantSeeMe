using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem; 

public class DialogueRunner : MonoBehaviour
{
    [Header("對話資料")]
    [SerializeField] private DialogueContainerSO dialogueContainer;

    [Header("執行元件")]
    [SerializeField] private DialogueUI dialogueUI;

    // --- 狀態變數 ---
    private DialogueNodeData _currentNode;
    private bool _isWaitingForContinue = false; // 是否在等待玩家點擊繼續
    private NodeLinkData _pendingContinueLink;  // 儲存待處理的「繼續」連結

    private void Start()
    {
        var entryPointNode = dialogueContainer.DialogueNodes.Find(node => node.EntryPoint);
        if (entryPointNode != null)
        {
            ShowNode(entryPointNode);
        }
        else
        {
            Debug.LogError("找不到對話進入點 (Entry Point)！");
            dialogueUI.Hide();
        }
    }

    // --- 新增: Update 函式來監聽輸入 ---
    private void Update()
    {
        // 如果不是在等待「繼續」的狀態，就直接返回，不做任何事
        if (!_isWaitingForContinue)
        {
            return;
        }

        // 檢查空白鍵或滑鼠左鍵是否被按下
        // 使用 Input System 的 GetKeyDown
        if (Keyboard.current.spaceKey.wasPressedThisFrame || Mouse.current.leftButton.wasPressedThisFrame)
        {
            // 停止等待，避免重複觸發
            _isWaitingForContinue = false;
            
            // 執行「繼續」的邏輯
            GoToNextNode(_pendingContinueLink);
        }
    }

    private void ShowNode(DialogueNodeData node)
    {
        _currentNode = node;
        _isWaitingForContinue = false; // 每次顯示新節點時，重置等待狀態

        dialogueUI.ShowNodeText(node);

        var allLinks = dialogueContainer.NodeLinks.Where(link => link.BaseNodeGuid == _currentNode.Guid).ToList();
        var choiceLinks = allLinks.Where(link => link.PortName != "繼續").ToList();

        // 優先顯示選項
        if (choiceLinks.Count > 0)
        {
            dialogueUI.ShowChoices(choiceLinks, OnChoiceSelected);
        }
        // 如果沒有選項，但有「繼續」連線
        else
        {
            var continueLink = allLinks.FirstOrDefault(link => link.PortName == "繼續");
            if (continueLink != null)
            {
                // 進入「等待繼續」狀態
                _isWaitingForContinue = true;
                _pendingContinueLink = continueLink;
                dialogueUI.ShowContinueIndicator(); // 顯示提示圖示
            }
            // 如果沒有任何出口，對話結束
            else
            {
                dialogueUI.Hide();
            }
        }
    }

    private void OnChoiceSelected(NodeLinkData selectedLink)
    {
        GoToNextNode(selectedLink);
    }
    
    private void GoToNextNode(NodeLinkData link)
    {
        var nextNode = dialogueContainer.DialogueNodes.Find(node => node.Guid == link.TargetNodeGuid);
        if (nextNode != null)
        {
            ShowNode(nextNode);
        }
        else
        {
            Debug.Log("找不到目標節點！對話結束。");
            dialogueUI.Hide();
        }
    }
}