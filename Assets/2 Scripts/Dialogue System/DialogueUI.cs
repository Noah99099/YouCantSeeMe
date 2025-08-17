using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DialogueUI : MonoBehaviour
{
    public static DialogueUI Instance { get; private set; }//宣告單例
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

    [Header("打字機效果")]
    [Tooltip("每秒顯示多少個字元。設為 0 表示立即顯示。")]
    [SerializeField] private float charactersPerSecond = 25f;
    private DialogueRunner activeRunner;
    private Action _onDialogueComplete;
    private Coroutine _typewriterCoroutine; // 用來保存打字機協程的引用，方便中斷
    private bool _isTyping = false;        // 追蹤目前是否正在打字
    private string _fullTextToType;        // 儲存當前需要顯示的完整文字

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            // 如果發現場景中已經有一個 Instance，就發出錯誤日誌並銷毀自己
            Debug.LogError("場景中發現了多個 DialogueUI 實例！請刪除多餘的物件。", this.gameObject);
            Destroy(this.gameObject);
        }
    }

    public void ShowDialogue(SayCommand sayCommand, Action onComplete)
    {
        _onDialogueComplete = onComplete;
        Debug.Log($"<color=yellow>[DialogueUI.ShowDialogue] 回呼函式已設定. 是否為 null? : {_onDialogueComplete == null}</color>");

        dialoguePanel.SetActive(true);
        HideChoices();
        ShowContinueIndicator();

        // 設定說話者名稱 (這部分不變)
        if (speakerNameText != null)
        {
            speakerNameText.text = sayCommand.SpeakerName;
            speakerNameText.color = sayCommand.NameColor;
        }

        // 【核心修改】從直接設定文字，改為啟動打字機效果
        if (dialogueText != null)
        {
            _fullTextToType = sayCommand.DialogueText; // 儲存完整文字

            // 如果上一個打字機還在跑，先停掉它
            if (_typewriterCoroutine != null)
            {
                StopCoroutine(_typewriterCoroutine);
            }

            // 如果速度設為 0 或更低，則立即顯示
            if (charactersPerSecond <= 0)
            {
                dialogueText.text = _fullTextToType;
                _isTyping = false;
            }
            else
            {
                // 否則，啟動打字機協程
                _typewriterCoroutine = StartCoroutine(TypewriterEffect(_fullTextToType));
            }
        }
    }

    public void ShowChoices(List<ChoiceCommand.Choice> choices, Action<ChoiceCommand.Choice> onChoiceSelected)
    {
        // 確保先清空舊的選項按鈕
        HideChoices();
        HideContinueIndicator(); // 顯示選項時，不應顯示「繼續」提示

        foreach (var choice in choices)
        {
            // 實例化按鈕 Prefab
            GameObject buttonGO = Instantiate(choiceButtonPrefab, choiceButtonContainer);

            // 設定按鈕文字
            var buttonText = buttonGO.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = choice.ChoiceText;
            }

            // 設定按鈕的點擊事件
            var button = buttonGO.GetComponent<Button>();
            if (button != null)
            {
                // 當按鈕被點擊時，呼叫傳入的 onChoiceSelected 回呼，並傳回被選擇的 choice
                button.onClick.AddListener(() =>
                {
                    onChoiceSelected(choice);
                    // 選擇後，再次清空所有選項按鈕
                    HideChoices();
                });
            }
        }
    }

    // 假設你有一個處理「繼續」按鈕的方法，或是由 DialogueRunner 呼叫
    // 我們需要確保它能呼叫回呼
    public void OnContinueClicked()
    {
        Debug.Log($"[DialogueUI] OnContinueClicked 被呼叫。目前是否正在打字 (_isTyping): {_isTyping}");
        // 【情況 1】如果打字機還在跑，則立即完成它
        if (_isTyping)
        {
            Debug.Log("[DialogueUI] 正在打字中，準備快轉...");
            // 停止協程
            if (_typewriterCoroutine != null)
            {
                StopCoroutine(_typewriterCoroutine);
                _typewriterCoroutine = null;
            }

            // 直接顯示完整文字
            dialogueText.text = _fullTextToType;
            _isTyping = false;

            // 注意：這裡就結束了，不呼叫 onComplete，等待玩家下一次點擊
            return;
        }
        Debug.Log($"[DialogueUI] 打字已結束。檢查回呼函式 (_onDialogueComplete) 是否存在: {(_onDialogueComplete != null)}");

        // 【情況 2】如果打字機已經結束，則推進到下一個指令
        _onDialogueComplete?.Invoke();
        _onDialogueComplete = null;
    }

    // SetActiveRunner 是唯一的入口點，用來設定UI與當前Runner的關聯
    public void SetActiveRunner(DialogueRunner runner)
    {
        activeRunner = runner;

        // 動態設定「自動播放」按鈕的監聽事件
        if (autoPlayButton != null)
        {
            autoPlayButton.onClick.RemoveAllListeners();
            //autoPlayButton.onClick.AddListener(() => activeRunner.ToggleAutoPlay());
        }

        // 動態設定「跳到下個重點」按鈕的監聽事件，呼叫新的方法
        if (skipModeButton != null)
        {
            skipModeButton.onClick.RemoveAllListeners();
            //skipModeButton.onClick.AddListener(() => activeRunner.SkipToNextImportantNode());
        }

        // 初始更新一次按鈕文字
        //UpdateModeButtons(activeRunner.IsAutoPlayEnabled, false);
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

    private System.Collections.IEnumerator TypewriterEffect(string text)
    {
        Debug.Log("[DialogueUI] TypewriterEffect 協程開始。");
        _isTyping = true;
        dialogueText.text = ""; // 先清空文字框

        // 如果速度設為 0 或更低，直接顯示完整文字並結束
        if (charactersPerSecond <= 0)
        {
            dialogueText.text = text;
            _isTyping = false;
            Debug.Log("[DialogueUI] TypewriterEffect 因速度為0，立即完成。");
            yield break; // 結束協程
        }

        float delay = 1f / charactersPerSecond;

        foreach (char c in text)
        {
            dialogueText.text += c;
            // 【核心修正】改用 WaitForSecondsRealtime，它不受 Time.timeScale 影響
            yield return new WaitForSecondsRealtime(delay);
        }

        _typewriterCoroutine = null;
        _isTyping = false;
        Debug.Log("<color=lime>[DialogueUI] TypewriterEffect 協程正常結束。</color>");
    }
}