using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class DialogueRunner : MonoBehaviour
{
    [SerializeField] private DialogueContainerSO dialogueContainer;
    private DialogueUI dialogueUI;
    public UnityEvent OnDialogueStart = new UnityEvent();
    public UnityEvent OnDialogueEnd = new UnityEvent();

    private DialogueBlock _currentBlock;
    private int _currentCommandIndex = 0;
    private bool _isRunning = false;

    public void SetDialogueUI(DialogueUI ui) { dialogueUI = ui; }
    public void SetDialogue(DialogueContainerSO container) { dialogueContainer = container; }
    public DialogueUI GetDialogueUI() => dialogueUI;

    public void StartDialogue()
    {
        if (dialogueContainer == null || dialogueContainer.Blocks.Count == 0)
        {
            Debug.LogError("對話資料未設定或為空，無法啟動對話。", this);
            return;
        }
        OnDialogueStart?.Invoke();
        _isRunning = true;
        
        _currentBlock = dialogueContainer.Blocks[0];
        _currentCommandIndex = 0;
        ExecuteNextCommand();
    }

    private void ExecuteNextCommand()
    {
        if (!_isRunning) return;
        
        if (_currentCommandIndex >= _currentBlock.Commands.Count)
        {
            FollowNextLink();
            return;
        }

        var command = _currentBlock.Commands[_currentCommandIndex];
        _currentCommandIndex++;
        command.Execute(this, ExecuteNextCommand);
    }
    
    private void FollowNextLink()
    {
        Debug.Log($"--- FollowNextLink: 正在從區塊 '{_currentBlock.BlockName}' (GUID: {_currentBlock.GUID}) 尋找下一條連線...");
        string nextGuid = _currentBlock.NextBlockGuid;
        if (string.IsNullOrEmpty(nextGuid))
        {
            Debug.Log("<color=lime>CHAIN_STEP_1: 找不到後續連線，準備呼叫 EndDialogue()。</color>");
            EndDialogue();
            return;
        }
        
        Debug.Log($"找到了連線，準備跳轉到 GUID: {nextGuid}");
        JumpToBlock(nextGuid);
    }
    
    public void JumpToBlock(string targetBlockGuid)
    {
        var targetBlock = dialogueContainer.Blocks.FirstOrDefault(b => b.GUID == targetBlockGuid);
        if (targetBlock == null)
        {
            Debug.LogError($"跳轉失敗：在資料中找不到 GUID 為 '{targetBlockGuid}' 的區塊！對話將終止。", this);
            EndDialogue();
            return;
        }
        _currentBlock = targetBlock;
        _currentCommandIndex = 0;
        
        Debug.Log($"已跳轉到區塊: '{targetBlock.BlockName}'");
        ExecuteNextCommand();
    }

    private void EndDialogue()
    {
        Debug.Log("<color=yellow>--- EndDialogue() 方法已被呼叫！正在觸發 OnDialogueEnd 事件... ---</color>");
        if (!_isRunning) return; 
        
        _isRunning = false;
        OnDialogueEnd?.Invoke(); 
    }
}