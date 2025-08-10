using System;
using UnityEngine;

[Serializable]
public class SayCommand : Command
{
    [Tooltip("說話的角色名稱")]
    public string SpeakerName;

    [Tooltip("對話內容")]
    [TextArea(3, 10)]
    public string DialogueText;
    
    [Tooltip("角色名稱的顏色")]
    public Color NameColor = Color.white;
    
    // 你可以把你原本 DialogueNodeData 中的其他屬性 (如字體樣式、頭像等) 都加進來

    public override void Execute(DialogueRunner runner, Action onComplete)
    {
        // 【優化】直接從 runner 獲取 DialogueUI，不再需要 FindObjectOfType
        DialogueUI dialogueUI = runner.GetDialogueUI();
        
        if (dialogueUI != null)
        {
            dialogueUI.ShowDialogue(this, onComplete);
        }
        else
        {
            Debug.LogError("在場景中找不到 DialogueUI！");
            onComplete();
        }
    }
}