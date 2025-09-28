using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;

public class DialogueUI : MonoBehaviour
{
    [Header("對話 UI 元件")]
    [SerializeField] private GameObject dialogueBox;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI contentText;

    [Header("角色立繪位置")]
    [SerializeField] private Image characterSpriteLeft;
    [SerializeField] private Image characterSpriteRight;
    [SerializeField] private Color speakerHighlightColor = Color.white;
    [SerializeField] private Color speakerDimColor = Color.gray;

    [Header("動畫參數")]
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float shakeDuration = 0.3f;
    [SerializeField] private float shakeIntensity = 10f;

    [Header("選項 UI")]
    [SerializeField] private GameObject choiceContainer;
    [SerializeField] private Button choiceButtonPrefab;

    [Header("角色資料庫")]
    [SerializeField] private CharacterProfile[] characters;  // 在 Inspector 中預先載入所有角色 Profile

    // 內部變數
    private Coroutine typeWriterCoroutine;
    private List<Button> spawnedButtons = new List<Button>();
    private Dictionary<string, Coroutine> runningAnimations = new Dictionary<string, Coroutine>();

    // 角色資料庫
    private Dictionary<string, CharacterProfile> characterDatabase = new Dictionary<string, CharacterProfile>();
    private string currentLeftCharacterID = "";
    private string currentRightCharacterID = "";


    private void Awake()
    {
        // 將所有角色 Profile 載入到字典中，方便快速查找
        foreach (var character in characters)
        {
            if (!characterDatabase.ContainsKey(character.characterID))
            {
                characterDatabase.Add(character.characterID, character);
            }
        }

        // 預設隱藏所有立繪
        characterSpriteLeft.gameObject.SetActive(false);
        characterSpriteRight.gameObject.SetActive(false);
        if(dialogueBox != null) dialogueBox.SetActive(false);
    }

    void Start()
    {
        dialogueBox.SetActive(false);
    }

    public void ShowDialogueBox()
    {
        dialogueBox.SetActive(true);
    }

    public void HideDialogueBox()
    {
        dialogueBox.SetActive(false);
    }

    public void SetDialogue(DialogueLine line, float typeSpeed)
    {
        if (line.isNarration)
        {
            // 如果是旁白，隱藏名字和所有立繪
            nameText.gameObject.SetActive(false);
            characterSpriteLeft.gameObject.SetActive(false);
            characterSpriteRight.gameObject.SetActive(false);
            
            // 直接開始打字效果
            StartTypewriter(line.content, typeSpeed);
            // 提前結束方法，不執行後面的角色邏輯
            return; 
        }
        string localizedContent = LocalizationManager.Instance.GetLocalizedText(line.contentKey);
        nameText.gameObject.SetActive(true);
        CharacterProfile speakerProfile;
        if (!characterDatabase.TryGetValue(line.characterID, out speakerProfile))
        {
            nameText.text = line.speakerName;
            characterSpriteLeft.gameObject.SetActive(false);
            characterSpriteRight.gameObject.SetActive(false);
            StartTypewriter(localizedContent, typeSpeed);
            return;
        }

        nameText.text = line.overrideName ? line.speakerName : speakerProfile.characterName;
        UpdateCharacterSprite(line.characterID, line.expression, line.position, line.animation);
        HighlightSpeaker(line.position);
        StartTypewriter(localizedContent, typeSpeed);
    }

    private void UpdateCharacterSprite(string charID, string expression, CharacterPosition position, CharacterAnimationType animation)
    {
        CharacterProfile profile = characterDatabase[charID];
        Sprite sprite = profile.GetSprite(expression);
        Image targetImage = (position == CharacterPosition.Left) ? characterSpriteLeft : characterSpriteRight;
        string currentID = (position == CharacterPosition.Left) ? currentLeftCharacterID : currentRightCharacterID;

        // 更新立繪圖片
        targetImage.sprite = sprite;
        
        // 播放動畫
        PlayCharacterAnimation(targetImage, animation);

        // 如果目標位置不是這個角色，或者角色尚未顯示，則更新 ID
        if (currentID != charID || !targetImage.gameObject.activeInHierarchy)
        {
            targetImage.gameObject.SetActive(true);
            if (position == CharacterPosition.Left)
                currentLeftCharacterID = charID;
            else
                currentRightCharacterID = charID;
        }
    }

    private void PlayCharacterAnimation(Image targetImage, CharacterAnimationType animationType)
    {
        // 停止該位置上一個正在播放的動畫
        if (runningAnimations.ContainsKey(targetImage.name) && runningAnimations[targetImage.name] != null)
        {
            StopCoroutine(runningAnimations[targetImage.name]);
        }

        // 根據類型啟動新的動畫協程
        switch (animationType)
        {
            case CharacterAnimationType.FadeIn:
                runningAnimations[targetImage.name] = StartCoroutine(FadeInEffect(targetImage));
                break;
            case CharacterAnimationType.Shake:
                runningAnimations[targetImage.name] = StartCoroutine(ShakeEffect(targetImage));
                break;
            case CharacterAnimationType.None:
                // 如果沒有動畫，確保 Alpha 是 1 (完全不透明)
                targetImage.color = new Color(targetImage.color.r, targetImage.color.g, targetImage.color.b, 1);
                targetImage.gameObject.SetActive(true); // 順便確保物件是顯示的
                break;
        }
    }

    private void HighlightSpeaker(CharacterPosition speakerPosition)
    {
        if (speakerPosition == CharacterPosition.Left)
        {
            characterSpriteLeft.color = speakerHighlightColor;
            characterSpriteRight.color = speakerDimColor;
        }
        else
        {
            characterSpriteLeft.color = speakerDimColor;
            characterSpriteRight.color = speakerHighlightColor;
        }
    }

    private void StartTypewriter(string content, float speed)
    {
        if (typeWriterCoroutine != null)
        {
            StopCoroutine(typeWriterCoroutine);
        }
        typeWriterCoroutine = StartCoroutine(TypeWriterEffect(content, speed));
    }

    public Coroutine GetTypeWriterCoroutine()
    {
        return typeWriterCoroutine;
    }

    // 需求 #6: RPG Maker 的文本延遲效果
    private IEnumerator TypeWriterEffect(string content, float speed)
    {
        contentText.text = "";
        // 為了處理富文本標籤，我們需要一個更聰明的方法，但這裡先用基礎版
        bool isTag = false;
        string currentTag = "";

        foreach (char c in content)
        {
            if (c == '<')
            {
                isTag = true;
                currentTag += c;
            }
            else if (c == '>')
            {
                isTag = false;
                currentTag += c;
                contentText.text += currentTag;
                currentTag = "";
            }
            else if (isTag)
            {
                currentTag += c;
            }
            else
            {
                contentText.text += c;
                yield return new WaitForSeconds(speed);
            }
        }
        typeWriterCoroutine = null;
    }

    // 強制完成打字效果
    public void CompleteText(string fullContent)
    {
        if (typeWriterCoroutine != null)
        {
            StopCoroutine(typeWriterCoroutine);
            typeWriterCoroutine = null;
        }
        contentText.text = fullContent;
    }

    public void ShowChoices(List<string> choiceKeys, Action<int> onChoiceSelected)
    {
        ClearChoices();
        choiceContainer.SetActive(true);

        for (int i = 0; i < choiceKeys.Count; i++)
        {
            Button newButton = Instantiate(choiceButtonPrefab, choiceContainer.transform);
        
        // 直接將選項文字設定到按鈕上
        string localizedChoice = LocalizationManager.Instance.GetLocalizedText(choiceKeys[i]);
        newButton.GetComponentInChildren<TextMeshProUGUI>().text = localizedChoice;

        int choiceIndex = i;
        newButton.onClick.AddListener(() => onChoiceSelected(choiceIndex));
        
        spawnedButtons.Add(newButton);
        }
    }

    public void ClearChoices()
    {
        choiceContainer.SetActive(false);
        foreach (Button button in spawnedButtons)
        {
            Destroy(button.gameObject);
        }
        spawnedButtons.Clear();
    }
    //搖晃效果
    private IEnumerator ShakeEffect(Image targetImage)
    {
        Vector3 originalPos = targetImage.rectTransform.anchoredPosition;
        float timer = 0f;

        while (timer < shakeDuration)
        {
            timer += Time.deltaTime;
            float x = originalPos.x + UnityEngine.Random.Range(-shakeIntensity, shakeIntensity);
            float y = originalPos.y + UnityEngine.Random.Range(-shakeIntensity, shakeIntensity);
            targetImage.rectTransform.anchoredPosition = new Vector2(x, y);
            yield return null;
        }

        targetImage.rectTransform.anchoredPosition = originalPos;
        runningAnimations[targetImage.name] = null;
    }
    //漸入效果
    private IEnumerator FadeInEffect(Image targetImage)
    {
        float timer = 0f;
        Color originalColor = targetImage.color;
        
        // 如果物件是新出現的，從完全透明開始
        if (!targetImage.gameObject.activeInHierarchy)
        {
            targetImage.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0);
        }

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(0, 1, timer / fadeDuration);
            targetImage.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            yield return null;
        }
        targetImage.color = new Color(originalColor.r, originalColor.g, originalColor.b, 1);
        runningAnimations[targetImage.name] = null;
    }
}