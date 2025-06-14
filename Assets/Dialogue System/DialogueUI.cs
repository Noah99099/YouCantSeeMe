using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // 引用 TextMeshPro

public class DialogueUI : MonoBehaviour
{
    [Header("UI 元件")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI speakerNameText;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private Transform choiceButtonContainer;
    [SerializeField] private GameObject choiceButtonPrefab;

    /// <summary>
    /// 顯示對話節點內容，包含多個選項
    /// </summary>
    /// <param name="node">要顯示的節點資料</param>
    /// <param name="choices">可用的選項連結</param>
    /// <param name="onChoiceSelected">當玩家選擇一個選項時要呼叫的回呼函式</param>
    public void ShowNode(DialogueNodeData node, List<NodeLinkData> choices, Action<NodeLinkData> onChoiceSelected)
    {
        dialoguePanel.SetActive(true);
        speakerNameText.text = node.SpeakerName;
        dialogueText.text = node.DialogueText;

        // 清空舊的按鈕
        foreach (Transform child in choiceButtonContainer)
        {
            Destroy(child.gameObject);
        }

        // 如果有選項，就為每個選項建立按鈕
        if (choices != null && choices.Count > 0)
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
    }
    
    /// <summary>
    /// 隱藏對話 UI
    /// </summary>
    public void Hide()
    {
        dialoguePanel.SetActive(false);
    }
}