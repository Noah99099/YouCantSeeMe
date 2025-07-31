using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DialogueUI : MonoBehaviour
{
    // ... UI 元件欄位保留不變 ...
    [Header("UI 元件")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI speakerNameText;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private Transform choiceButtonContainer;
    [SerializeField] private GameObject choiceButtonPrefab;
    [SerializeField] private GameObject continueIndicator;
    
    public void ShowNodeText(DialogueNodeData node)
    {
        dialoguePanel.SetActive(true);
        
        // --- 核心修改: 產生富文本 ---
        string nameText = node.SpeakerName;
        // 1. 根據樣式包裹標籤
        switch (node.NameStyle)
        {
            case SpeakerNameStyle.Bold:
                nameText = $"<b>{nameText}</b>";
                break;
            case SpeakerNameStyle.Italic:
                nameText = $"<i>{nameText}</i>";
                break;
        }
        
        // 2. 根據顏色包裹標籤
        // ColorUtility.ToHtmlStringRGB 可以將 Unity 的 Color 物件轉換為 #RRGGBB 格式的十六進位字串
        string colorHex = ColorUtility.ToHtmlStringRGB(node.NameColor);
        speakerNameText.text = $"<color=#{colorHex}>{nameText}</color>";
        
        dialogueText.text = node.DialogueText;

        // ... 後面的程式碼保留不變 ...
        foreach (Transform child in choiceButtonContainer)
        {
            Destroy(child.gameObject);
        }
        if (continueIndicator != null)
        {
            continueIndicator.SetActive(false);
        }
    }

    // ... ShowChoices, ShowContinueIndicator, Hide 方法保留不變 ...
    public void ShowChoices(List<NodeLinkData> choices, Action<NodeLinkData> onChoiceSelected) { /* ... */ }
    public void ShowContinueIndicator() { /* ... */ }
    public void Hide() { /* ... */ }
}