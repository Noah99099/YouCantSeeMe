using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DialogueUI : MonoBehaviour
{
    [Header("UI 元件")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI speakerNameText;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private Transform choiceButtonContainer;
    [SerializeField] private GameObject choiceButtonPrefab;
    
    // --- 新增: 繼續提示圖示 (可選) ---
    [Tooltip("用於提示玩家可以繼續對話的圖示")]
    [SerializeField] private GameObject continueIndicator;


    /// <summary>
    /// 顯示節點的文字內容，並清空舊的選項
    /// </summary>
    public void ShowNodeText(DialogueNodeData node)
    {
        dialoguePanel.SetActive(true);
        speakerNameText.text = node.SpeakerName;
        dialogueText.text = node.DialogueText;

        // 清空舊的按鈕
        foreach (Transform child in choiceButtonContainer)
        {
            Destroy(child.gameObject);
        }
        
        // 預設隱藏繼續提示
        if (continueIndicator != null)
        {
            continueIndicator.SetActive(false);
        }
    }

    /// <summary>
    /// 顯示多個選項按鈕
    /// </summary>
    public void ShowChoices(List<NodeLinkData> choices, Action<NodeLinkData> onChoiceSelected)
    {
        foreach (var choice in choices)
        {
            GameObject buttonInstance = Instantiate(choiceButtonPrefab, choiceButtonContainer);
            buttonInstance.GetComponentInChildren<TextMeshProUGUI>().text = choice.PortName;
            buttonInstance.GetComponent<Button>().onClick.AddListener(() =>
            {
                onChoiceSelected?.Invoke(choice);
            });
        }
    }

    /// <summary>
    /// 顯示「可以繼續」的提示圖示
    /// </summary>
    public void ShowContinueIndicator()
    {
        if (continueIndicator != null)
        {
            continueIndicator.SetActive(true);
        }
    }

    /// <summary>
    /// 隱藏對話 UI
    /// </summary>
    public void Hide()
    {
        dialoguePanel.SetActive(false);
    }
}