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
    [SerializeField] private Button skipModeButton;
    [SerializeField] private TextMeshProUGUI autoPlayLabel;
    [SerializeField] private TextMeshProUGUI skipModeLabel;

    [Header("新增功能")]
    [SerializeField] private Image characterPortrait;
    [SerializeField] private CanvasGroup dialogueCanvasGroup;
    [SerializeField] private float fadeSpeed = 2f;
    private DialogueRunner runner;
    private void Awake()
    {
        runner = FindObjectOfType<DialogueRunner>();

        if (autoPlayButton != null)
            autoPlayButton.onClick.AddListener(() => runner.ToggleAutoPlay());

        if (skipModeButton != null)
            skipModeButton.onClick.AddListener(() => runner.ToggleSkipMode());
    }

    public void ShowNodeText(DialogueNodeData node, string textOverride = null)
    {
        dialoguePanel.SetActive(true);

        HideChoices();
        HideContinueIndicator();

        // 設置角色名稱
        SetSpeakerName(node);

        // 設置對話文本
        string textToShow = textOverride ?? node.DialogueText;
        dialogueText.text = textToShow;

        // 設置角色立繪
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
            // Sprite portraitSprite = Resources.Load<Sprite>($"Portraits/{portraitName}");
            // if (portraitSprite != null)
            // {
            //     characterPortrait.sprite = portraitSprite;
            //     characterPortrait.gameObject.SetActive(true);
            // }
        }
    }

    public void UpdateDialogueText(string text)
    {
        dialogueText.text = text;
    }

    public void ShowChoices(List<NodeLinkData> choices, Action<NodeLinkData> onChoiceSelected)
    {
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
        dialoguePanel.SetActive(false);
        HideContinueIndicator();
        HideChoices();
    }

    // === 新增：淡入淡出效果 ===
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
            autoPlayLabel.text = isAuto ? "🔁 自動播放 (開)" : "🔁 自動播放";

        if (skipModeLabel != null)
            skipModeLabel.text = isSkip ? "⏩ 跳過模式 (開)" : "⏩ 跳過模式";
    }

}