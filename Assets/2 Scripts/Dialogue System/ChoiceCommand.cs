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
        [Tooltip("選擇此項後要跳轉到的 Block 名稱")]
        public string TargetBlockName;
    }

    [Tooltip("要提供給玩家的選項列表")]
    public List<Choice> Choices = new List<Choice>();

    public override void Execute(DialogueRunner runner, Action onComplete)
    {
        DialogueUI dialogueUI = runner.GetDialogueUI();
        
        Action<Choice> onChoiceSelected = (selectedChoice) => 
        {
            // 當玩家做出選擇後，命令 runner 跳轉到目標 Block
            runner.JumpToBlock(selectedChoice.TargetBlockName);
            
            // 選項指令的任務在這裡就結束了，呼叫 onComplete
            // 但因為 JumpToBlock 會啟動新的執行流程，這裡的 onComplete 其實可以不用呼叫
            // 為了嚴謹，我們還是呼叫它，但 Runner 在 Jump 後會重置狀態
        };
        
        dialogueUI.ShowChoices(Choices, onChoiceSelected);
    }
}