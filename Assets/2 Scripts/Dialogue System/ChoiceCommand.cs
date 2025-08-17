using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ChoiceCommand : Command
{
    // 我們用一個內部類別來更好地組織每個選項的資料
    [Serializable]
    public class Choice
    {
        [Tooltip("顯示在按鈕上的文字")]
        public string ChoiceText;
        [Tooltip("選擇此項後要跳轉到的 Block 的 GUID")]
        public string TargetBlockGuid;
    }

    [Tooltip("要提供給玩家的選項列表")]
    public List<Choice> Choices = new List<Choice>();

    public override void Execute(DialogueRunner runner, Action onComplete)
    {
        DialogueUI dialogueUI = runner.GetDialogueUI();
        
        Action<Choice> onChoiceSelected = (selectedChoice) => 
        {
            // 當玩家做出選擇後，命令 runner 跳轉到目標 Block
            // 【核心修改】使用 GUID 進行跳轉
            if (!string.IsNullOrEmpty(selectedChoice.TargetBlockGuid))
            {
                runner.JumpToBlock(selectedChoice.TargetBlockGuid);
            }
            else
            {
                // 如果選項沒有連接到任何地方，就直接結束對話
                // 這是為了防止流程卡住
                runner.EndDialogue(); 
            }
            
            // 因為 JumpToBlock 會啟動新的執行流程，這裡不需要呼叫 onComplete
        };
        
        dialogueUI.ShowChoices(Choices, onChoiceSelected);
    }
}