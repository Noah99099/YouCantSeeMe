using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events; // 重新加回 UnityEvent

public class DialogueRunner : MonoBehaviour
{
    [Header("對話資料")]
    [SerializeField] private DialogueContainerSO dialogueContainer;

    [Header("執行元件")]
    private DialogueUI dialogueUI;
    // 稍後我們可以把 AutoPlay 也做成一個 Command
    // [SerializeField] private bool autoPlayEnabled = false; 

    [Header("事件")]
    public UnityEvent OnDialogueStart = new UnityEvent();
    public UnityEvent OnDialogueEnd = new UnityEvent();

    // Runner 執行狀態
    private int _currentBlockIndex = 0;
    private int _currentCommandIndex = 0;
    private bool _isRunning = false;

    public void SetDialogueUI(DialogueUI ui) { dialogueUI = ui; }
    public void SetDialogue(DialogueContainerSO container) { dialogueContainer = container; }
    public DialogueUI GetDialogueUI() => dialogueUI;
    /// <summary>
    /// 從指定的 Block 開始執行對話。
    /// 如果找不到，則預設從第一個 Block 開始。
    /// </summary>
    public void StartDialogue(string blockName = "")
    {
        if (dialogueContainer == null)
        {
            Debug.LogError("DialogueContainer 未設定！", this);
            return;
        }

        if (dialogueContainer.Blocks.Count == 0)
        {
            Debug.LogError("這個 DialogueContainer 沒有任何 Blocks！", this);
            return;
        }

        // 觸發對話開始事件
        OnDialogueStart?.Invoke();

        // 如果 blockName 是空的或找不到，就從第一個 Block 開始
        _currentBlockIndex = string.IsNullOrEmpty(blockName) ? 0 : dialogueContainer.Blocks.FindIndex(b => b.BlockName == blockName);

        if (_currentBlockIndex == -1)
        {
            Debug.LogWarning($"找不到名為 '{blockName}' 的 Block！將從第一個 Block 開始。");
            _currentBlockIndex = 0;
        }

        _isRunning = true;
        _currentCommandIndex = 0;
        ExecuteNextCommand();
    }

    private void ExecuteNextCommand()
    {
        if (!_isRunning) return;

        var currentBlock = dialogueContainer.Blocks[_currentBlockIndex];

        // 檢查目前 Block 的所有指令是否已執行完畢
        if (_currentCommandIndex >= currentBlock.Commands.Count)
        {
            Debug.Log($"Block '{currentBlock.BlockName}' 執行完畢。");
            _isRunning = false;

            // 觸發對話結束事件
            OnDialogueEnd?.Invoke();
            return;
        }

        // 取得下一個要執行的指令
        var command = currentBlock.Commands[_currentCommandIndex];

        // 將索引指向下一個指令，為回呼做準備
        _currentCommandIndex++;

        // 執行指令！並將 ExecuteNextCommand 自己作為 onComplete 回呼傳入
        // 這樣當前指令完成後，就會自動來執行下一個指令
        command.Execute(this, ExecuteNextCommand);
    }
    
    public void JumpToBlock(string blockName)
    {
        int targetBlockIndex = dialogueContainer.Blocks.FindIndex(b => b.BlockName == blockName);
        if (targetBlockIndex == -1)
        {
            Debug.LogError($"跳轉失敗：找不到名為 '{blockName}' 的 Block！", this);
            // 在找不到區塊時，可以選擇結束對話或跳到預設區塊
            _isRunning = false;
            OnDialogueEnd?.Invoke();
            return;
        }

        // 更新當前的 block 索引，並將 command 索引重設為 0
        _currentBlockIndex = targetBlockIndex;
        _currentCommandIndex = 0;
        
        Debug.Log($"已跳轉到 Block: '{blockName}'");

        // 立刻開始執行新 Block 的第一個指令
        ExecuteNextCommand();
    }
}