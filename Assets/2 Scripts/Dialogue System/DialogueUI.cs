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
    [SerializeField] private GameObject continueIndicator;

    [Header("模式控制")]
    [SerializeField] private Button autoPlayButton;
    [SerializeField] private Button skipModeButton; // 現在是「跳到下個重點」按鈕
    [SerializeField] private TextMeshProUGUI autoPlayLabel;
    [SerializeField] private TextMeshProUGUI skipModeLabel;

    [Header("新增功能")]
    [SerializeField] private Image characterPortrait;
    [SerializeField] private CanvasGroup dialogueCanvasGroup;
    [SerializeField] private float fadeSpeed = 2f;
    private DialogueRunner activeRunner;

    // Awake 方法保持空白即可
    private void Awake()
    {
    }

    // SetActiveRunner 是唯一的入口點，用來設定UI與當前Runner的關聯
    public void SetActiveRunner(DialogueRunner runner)
    {
        activeRunner = runner;

        // 動態設定「自動播放」按鈕的監聽事件
        if (autoPlayButton != null)
        {
            autoPlayButton.onClick.RemoveAllListeners(); 
            autoPlayButton.onClick.AddListener(() => activeRunner.ToggleAutoPlay());
        }

        // 【*** 關鍵修改 ***】
        // 動態設定「跳到下個重點」按鈕的監聽事件，呼叫新的方法
        if (skipModeButton != null)
        {
            skipModeButton.onClick.RemoveAllListeners(); 
            skipModeButton.onClick.AddListener(() => activeRunner.SkipToNextImportantNode());
        }
        
        // 初始更新一次按鈕文字
        UpdateModeButtons(activeRunner.IsAutoPlayEnabled, false);
    }

    public void ShowNodeText(DialogueNodeData node, string textOverride = null)
    {
        dialoguePanel.SetActive(true);

        HideChoices();
        HideContinueIndicator();

        SetSpeakerName(node);

        string textToShow = textOverride ?? node.DialogueText;
        dialogueText.text = textToShow;

        SetCharacterPortrait(node.CharacterPortrait);
    }

    private void SetSpeakerName(DialogueNodeData node)
    {
        string nameText = node.SpeakerName;
        switch (node.NameStyle)
        {
            case SpeakerNameStyle.Bold:
                nameText = $"<b>{nameText}</b>";
                break;
            case SpeakerNameStyle.Italic:
                nameText = $"<i>{nameText}</i>";
                break;
        }
        string colorHex = ColorUtility.ToHtmlStringRGB(node.NameColor);
        speakerNameText.text = $"<color=#{colorHex}>{nameText}</color>";
    }

    private void SetCharacterPortrait(string portraitName)
    {
        if (characterPortrait == null) return;

        if (string.IsNullOrEmpty(portraitName))
        {
            characterPortrait.gameObject.SetActive(false);
        }
        else
        {
            // 載入立繪圖片的邏輯
        }
    }

    public void UpdateDialogueText(string text)
    {
        dialogueText.text = text;
    }

    public void ShowChoices(List<NodeLinkData> choices, Action<NodeLinkData> onChoiceSelected)
    {
        if (choiceButtonPrefab == null)
        {
            Debug.LogError("錯誤：'Choice Button Prefab' 沒有在 DialogueUI 的 Inspector 中設定！", this.gameObject);
            return;
        }
        if (choiceButtonContainer == null)
        {
            Debug.LogError("錯誤：'Choice Button Container' 沒有在 DialogueUI 的 Inspector 中設定！", this.gameObject);
            return;
        }
        
        HideChoices();

        foreach (var choice in choices)
        {
            GameObject buttonGO = Instantiate(choiceButtonPrefab, choiceButtonContainer);
            var buttonText = buttonGO.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = choice.PortName;
            }
            var button = buttonGO.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(() =>
                {
                    onChoiceSelected(choice);
                    HideChoices();
                });
            }
        }
    }

    public void HideChoices()
    {
        if (choiceButtonContainer == null) return;
        foreach (Transform child in choiceButtonContainer)
        {
            Destroy(child.gameObject);
        }
    }

    public void ShowContinueIndicator()
    {
        if (continueIndicator != null)
        {
            continueIndicator.SetActive(true);
        }
    }

    public void HideContinueIndicator()
    {
        if (continueIndicator != null)
        {
            continueIndicator.SetActive(false);
        }
    }

    public void Hide()
    {
        // --- 新增的除錯日誌 ---
        Debug.Log($"[DialogueUI] Hide() 方法被呼叫。 dialoguePanel 物件是 {(dialoguePanel == null ? "NULL" : "已指派")}", this.gameObject);
        // --- 結束新增 ---

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
            
            // --- 新增的除錯日誌 ---
            // 確認 panel 在被關閉後，在層級中的狀態是什麼
            Debug.Log($"[DialogueUI] dialoguePanel.SetActive(false) 已執行。 Panel 的 activeInHierarchy 狀態為: {dialoguePanel.activeInHierarchy}");
            // --- 結束新增 ---
        }
        
        HideContinueIndicator();
        HideChoices();
    }
    public void FadeIn()
    {
        if (dialogueCanvasGroup != null)
        {
            StartCoroutine(FadeCanvasGroup(dialogueCanvasGroup, dialogueCanvasGroup.alpha, 1f));
        }
    }

    public void FadeOut()
    {
        if (dialogueCanvasGroup != null)
        {
            StartCoroutine(FadeCanvasGroup(dialogueCanvasGroup, dialogueCanvasGroup.alpha, 0f));
        }
    }

    private System.Collections.IEnumerator FadeCanvasGroup(CanvasGroup canvasGroup, float startAlpha, float targetAlpha)
    {
        float elapsedTime = 0f;
        float duration = 1f / fadeSpeed;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsedTime / duration);
            canvasGroup.alpha = alpha;
            yield return null;
        }
        canvasGroup.alpha = targetAlpha;
    }

    public void UpdateModeButtons(bool isAuto, bool isSkip)
    {
        if (autoPlayLabel != null)
            autoPlayLabel.text = isAuto ? "自動播放中..." : "自動播放";
        
        if (skipModeLabel != null)
            skipModeLabel.text = "跳到下個重點";
    }
}