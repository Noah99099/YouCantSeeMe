using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Text.RegularExpressions;

public class DialogueUI : MonoBehaviour
{
    private enum TextEffectType { None, Shake, Wave }
    private class TextEffectInfo
    {
        public TextEffectType type;
        public int startIndex;
        public int length;
    }
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

    [Header("文字特效參數")]
    [SerializeField] private float waveSpeed = 5f;
    [SerializeField] private float waveAmplitude = 10f;
    [SerializeField] private float textShakeIntensity = 2f;

    [Header("選項 UI")]
    [SerializeField] private GameObject choiceContainer;
    [SerializeField] private Button choiceButtonPrefab;

    [Header("角色資料庫")]
    [SerializeField] private CharacterProfile[] characters;  // 在 Inspector 中預先載入所有角色 Profile

    [Header("計時器 UI")]
    [SerializeField] private Slider timerSlider;
    // 內部變數
    private Coroutine typeWriterCoroutine;
    private List<Button> spawnedButtons = new List<Button>();
    private Dictionary<string, Coroutine> runningAnimations = new Dictionary<string, Coroutine>();
    private Coroutine timerCoroutine;
    private List<TextEffectInfo> textEffects = new List<TextEffectInfo>();
    private bool isAnimatingText = false;

    // 角色資料庫
    private Dictionary<string, CharacterProfile> characterDatabase = new Dictionary<string, CharacterProfile>();
    private string currentLeftCharacterID = "";
    private string currentRightCharacterID = "";

    // Update 方法是我們新的動畫心跳
    void Update()
    {
        if (isAnimatingText)
        {
            AnimateText();
        }
    }
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
        if (dialogueBox != null) dialogueBox.SetActive(false);
        if (timerSlider != null) timerSlider.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        // 當這個物件被啟用時，自動顯示 dialogueBox
        if (dialogueBox != null)
        {
            dialogueBox.SetActive(true);
        }
    }

    private void OnDisable()
    {
        // 當這個物件被停用時，自動隱藏/清理
        if (dialogueBox != null)
        {
            dialogueBox.SetActive(false);
        }

        // --- 搬移 HideDialogueBox 的所有清理邏輯 ---
        if (characterSpriteLeft != null) characterSpriteLeft.gameObject.SetActive(false);
        if (characterSpriteRight != null) characterSpriteRight.gameObject.SetActive(false);

        currentLeftCharacterID = "";
        currentRightCharacterID = "";
        StopTimer();
        ClearChoices();
    }

    public void SetDialogue(DialogueLine line, float typeSpeed)
    {
        // --- 核心修正：無論如何，先確保 Name 和 Content 物件都啟用 ---
        // (除非它們是 null)
        if (contentText != null) contentText.gameObject.SetActive(true);
        if (nameText != null) nameText.gameObject.SetActive(true);


        if (line.isNarration)
        {
            // 如果是旁白，隱藏名字和所有立繪
            if (nameText != null) nameText.gameObject.SetActive(false); //
            if (characterSpriteLeft != null) characterSpriteLeft.gameObject.SetActive(false); //
            if (characterSpriteRight != null) characterSpriteRight.gameObject.SetActive(false); //

            ProcessTextForEffects(line.content, typeSpeed); //

            // ----- 修正：刪除多餘的 StartTypewriter -----
            //StartTypewriter(line.content, typeSpeed); //
            
            // 提前結束方法，不執行後面的角色邏輯
            return; //
        }

        // --- 這是非旁白情況 ---
        string localizedContent = line.content; // 暫時繞過 LocalizationManager，使用原始文本
        //等到要做本地化再打開
        //string localizedContent = LocalizationManager.Instance.GetLocalizedText(line.contentKey);
        
        // (這行已在最上面做過，可選)
        // nameText.gameObject.SetActive(true); 

        CharacterProfile speakerProfile;
        if (!characterDatabase.TryGetValue(line.characterID, out speakerProfile))
        {
            // 找不到角色 Profile 的備用邏輯
            nameText.text = line.speakerName;
            if (characterSpriteLeft != null) characterSpriteLeft.gameObject.SetActive(false);
            if (characterSpriteRight != null) characterSpriteRight.gameObject.SetActive(false);
            
            ProcessTextForEffects(localizedContent, typeSpeed); // <-- 使用這個
            // ----- 修正：刪除多餘的 StartTypewriter -----
            // StartTypewriter(localizedContent, typeSpeed);
            return;
        }

        nameText.text = line.overrideName ? line.speakerName : speakerProfile.characterName; //
        UpdateCharacterSprite(line.characterID, line.expression, line.position, line.animation); //
        HighlightSpeaker(line.position); //
        
        ProcessTextForEffects(localizedContent, typeSpeed); //
        
        // ----- 修正：刪除多餘的 StartTypewriter -----
        // StartTypewriter(localizedContent, typeSpeed); //
    }

    private void ProcessTextForEffects(string fullText, float typeSpeed)
    {
        textEffects.Clear();
        string cleanText = fullText;
        string pattern = @"<(\w+)>(.*?)<\/\1>";
        MatchCollection matches = Regex.Matches(fullText, pattern, RegexOptions.IgnoreCase);

        foreach (Match match in matches.Cast<Match>().Reverse())
        {
            string tag = match.Groups[1].Value;
            string content = match.Groups[2].Value;
            TextEffectType type;
            if (Enum.TryParse<TextEffectType>(tag, true, out type))
            {
                textEffects.Add(new TextEffectInfo
                {
                    type = type,
                    startIndex = match.Groups[2].Index,
                    length = content.Length
                });
            }
            cleanText = cleanText.Remove(match.Index, match.Length).Insert(match.Index, content);
        }

        contentText.text = cleanText;
        contentText.maxVisibleCharacters = 0;
        isAnimatingText = true;

        if (typeWriterCoroutine != null) StopCoroutine(typeWriterCoroutine);
        typeWriterCoroutine = StartCoroutine(TypeWriterEffect(cleanText.Length, typeSpeed));
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
        // Fix: Pass content.Length instead of content string
        typeWriterCoroutine = StartCoroutine(TypeWriterEffect(content.Length, speed));
    }

    public Coroutine GetTypeWriterCoroutine()
    {
        return typeWriterCoroutine;
    }

    // 需求 #6: RPG Maker 的文本延遲效果
    private IEnumerator TypeWriterEffect(int totalChars, float speed)
{
    // 如果速度設定為 0 或負數，我們將其視為立即顯示
    if (speed <= 0)
    {
        contentText.maxVisibleCharacters = totalChars;
        typeWriterCoroutine = null;
        yield break; // 結束協程
    }

    // 正常速度的打字機效果
    for (int i = 0; i <= totalChars; i++)
    {
        contentText.maxVisibleCharacters = i;
        yield return new WaitForSeconds(speed);
    }
    typeWriterCoroutine = null;
}
    
    private void AnimateText()
    {
        contentText.ForceMeshUpdate();
        var textInfo = contentText.textInfo;
        if (textInfo.characterCount == 0) return;

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            var charInfo = textInfo.characterInfo[i];
            if (!charInfo.isVisible) continue;

            Vector3[] vertices = textInfo.meshInfo[charInfo.materialReferenceIndex].vertices;
            Vector3 offset = Vector3.zero;

            foreach (var effect in textEffects)
            {
                if (i >= effect.startIndex && i < effect.startIndex + effect.length)
                {
                    if (effect.type == TextEffectType.Shake)
                    {
                        offset += new Vector3(UnityEngine.Random.Range(-textShakeIntensity, textShakeIntensity), UnityEngine.Random.Range(-textShakeIntensity, textShakeIntensity), 0);
                    }
                    else if (effect.type == TextEffectType.Wave)
                    {
                        offset += new Vector3(0, Mathf.Sin(Time.time * waveSpeed + i * 0.5f) * waveAmplitude, 0);
                    }
                }
            }

            for (int j = 0; j < 4; j++)
            {
                vertices[charInfo.vertexIndex + j] += offset;
            }
        }

        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            var meshInfo = textInfo.meshInfo[i];
            meshInfo.mesh.vertices = meshInfo.vertices;
            contentText.UpdateGeometry(meshInfo.mesh, i);
        }
    }

    // 強制完成打字效果
    public void CompleteText(string fullText)
    {
        // 1. 停止任何正在運行的打字機協程
        if (typeWriterCoroutine != null)
        {
            StopCoroutine(typeWriterCoroutine);
            typeWriterCoroutine = null;
        }

        // 2. 處理文字中的特效標籤
        ProcessTextForEffects(fullText, 99999f); 
        
        // 3. 立即顯示所有字元
        contentText.maxVisibleCharacters = contentText.text.Length;

        // 4. 確保文字動畫可以從靜態的第一幀開始
        isAnimatingText = true;
        AnimateText(); 
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

    // --- 新增的計時器方法 ---
    public void StartTimer(float duration, Action onTimeout)
    {
        if (timerSlider == null) return;

        StopTimer(); // 先停止任何可能在執行的計時器
        timerCoroutine = StartCoroutine(TimerCoroutine(duration, onTimeout));
    }

    public void StopTimer()
    {
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
        }
        if (timerSlider != null) timerSlider.gameObject.SetActive(false);
    }

    private IEnumerator TimerCoroutine(float duration, Action onTimeout)
    {
        timerSlider.gameObject.SetActive(true);
        float timer = duration;

        while (timer > 0)
        {
            timer -= Time.deltaTime;
            timerSlider.value = timer / duration; // 更新 Slider 的進度
            yield return null;
        }

        timerSlider.gameObject.SetActive(false);
        onTimeout?.Invoke(); // 時間到，觸發超時事件
    }
}