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
    private enum TextEffectType { None, Shake, Wave, Rainbow, Color }
    private class TextEffectInfo
    {
        public TextEffectType type;
        public int startIndex;
        public int length;
        public string parameter;
    }
    [Header("對話 UI 元件")]
    [SerializeField] private GameObject dialogueBox;
    [SerializeField] private Image dialogueBoxImage;
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
    [SerializeField] private float rainbowSpeed = 1f;

    [Header("選項 UI")]
    [SerializeField] private GameObject choiceContainer;
    [SerializeField] private Button choiceButtonPrefab;

    [Header("角色資料庫")]
    [SerializeField] private CharacterProfile[] characters;  // 在 Inspector 中預先載入所有角色 Profile

    [Header("計時器 UI")]
    [SerializeField] private Slider timerSlider;

    [Header("對話框樣式")] // <--- 【新增】
    [SerializeField] private DialogueBoxStyle defaultBoxStyle;
    // 內部變數
    private Coroutine typeWriterCoroutine;
    private List<Button> spawnedButtons = new List<Button>();
    private Dictionary<string, Coroutine> runningAnimations = new Dictionary<string, Coroutine>();
    private Coroutine timerCoroutine;
    private List<TextEffectInfo> textEffects = new List<TextEffectInfo>();
    private bool isAnimatingText = false;
    // 新增：用來快取 TMP 解析完的原始頂點與顏色資料
    private TMP_MeshInfo[] cachedMeshInfo;

    // 角色資料庫
    private Dictionary<string, CharacterProfile> characterDatabase = new Dictionary<string, CharacterProfile>();
    private string currentLeftCharacterID = "";
    private string currentRightCharacterID = "";

    // 將 Update 改為 LateUpdate，確保在 TMP 自身排版完成後才介入修改網格
    void LateUpdate()
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

    /// <summary>
    /// 【新方法】
    /// 根據傳入的角色 Profile 來套用對話框樣式
    /// </summary>
    /// <param name="profile">傳入 null 可套用預設(旁白)樣式</param>
    private void ApplyStyle(CharacterProfile profile)
    {
        // 1. 決定要套用哪個樣式
        //    - 如果 profile 和 profile.boxStyle 都存在 -> 用角色的樣式
        //    - 否則 (例如是旁白，或角色沒有設定樣式) -> 用預設樣式
        DialogueBoxStyle styleToApply = (profile != null && profile.boxStyle != null) 
                                        ? profile.boxStyle 
                                        : defaultBoxStyle;

        // 2. 如果連 defaultBoxStyle 都沒有，就直接返回
        if (styleToApply == null)
        {
            // (可選) 第一次使用時，將當前設定儲存為預設值
            // if (defaultBoxStyle == null)
            // {
            //     Debug.LogWarning("DialogueUI 缺少 defaultBoxStyle。");
            // }
            return;
        }

        // 3. 套用樣式
        if (dialogueBoxImage != null && styleToApply.boxSprite != null)
        {
            dialogueBoxImage.sprite = styleToApply.boxSprite;
        }

        if (nameText != null)
        {
            nameText.color = styleToApply.nameColor;
            if (styleToApply.nameFont != null)
            {
                nameText.font = styleToApply.nameFont;
            }
        }

        if (contentText != null)
        {
            contentText.color = styleToApply.contentColor;
            if (styleToApply.contentFont != null)
            {
                contentText.font = styleToApply.contentFont;
            }
        }
    }

    public void SetDialogue(DialogueLine line, float typeSpeed)
    {
        // --- 核心修正：無論如何，先確保 Name 和 Content 物件都啟用 ---
        if (contentText != null) contentText.gameObject.SetActive(true);
        if (nameText != null) nameText.gameObject.SetActive(true);

        if (line.isNarration)
        {
            // 【修改】如果是旁白，套用預設樣式 (null 會觸發 defaultBoxStyle)
            ApplyStyle(null); 

            if (nameText != null) nameText.gameObject.SetActive(false); 
            if (characterSpriteLeft != null) characterSpriteLeft.gameObject.SetActive(false); 
            if (characterSpriteRight != null) characterSpriteRight.gameObject.SetActive(false); 

            ProcessTextForEffects(line.content, typeSpeed); 
            return; 
        }

        // --- 這是非旁白情況 ---
        string localizedContent = line.content; 
        
        CharacterProfile speakerProfile;
        if (!characterDatabase.TryGetValue(line.characterID, out speakerProfile))
        {
            // 找不到角色 Profile 的備用邏輯
            
            // 【修改】找不到角色，也套用預設樣式
            ApplyStyle(null); 

            nameText.text = line.speakerName;
            if (characterSpriteLeft != null) characterSpriteLeft.gameObject.SetActive(false);
            if (characterSpriteRight != null) characterSpriteRight.gameObject.SetActive(false);
            
            ProcessTextForEffects(localizedContent, typeSpeed); 
            return;
        }

        // 【修改】成功找到角色，套用 "該角色" 的樣式
        ApplyStyle(speakerProfile); 

        nameText.text = line.overrideName ? line.speakerName : speakerProfile.characterName; 
        UpdateCharacterSprite(line.characterID, line.expression, line.position, line.animation); 
        HighlightSpeaker(line.position); 
        
        ProcessTextForEffects(localizedContent, typeSpeed); 
    }

    private void ProcessTextForEffects(string fullText, float typeSpeed)
    {
        textEffects.Clear();
        string cleanText = fullText;

        string pattern = @"<(shake|wave|rainbow|color)(?:=(.*?))?>(.*?)<\/\1>"; 
        MatchCollection matches = Regex.Matches(fullText, pattern, RegexOptions.IgnoreCase);

        foreach (Match match in matches.Cast<Match>().Reverse())
        {
            string tag = match.Groups[1].Value;       
            string parameter = match.Groups[2].Value; 
            string content = match.Groups[3].Value;   

            TextEffectType type;
            if (!Enum.TryParse<TextEffectType>(tag, true, out type)) continue;

            // 【修正】Color 類型不需要自訂動畫系統，TMP 本身就支援 <color=xxx> 標籤。
            // 直接保留在 cleanText 中讓 TMP 原生渲染，不剝離標籤、不加入 textEffects。
            // 這樣可避免：
            //   1. 打字機過程中 AnimateText 上色造成的閃爍
            //   2. 打字結束後 isAnimatingText=false 導致顏色消失（變回黑色）
            if (type == TextEffectType.Color)
            {
                // cleanText 不做任何修改，TMP 自行解析 <color> 標籤
                continue;
            }

            // 其他自訂特效（Shake / Wave / Rainbow）才需要動畫系統處理
            textEffects.Add(new TextEffectInfo
            {
                type = type,
                startIndex = match.Index, // 這裡記錄的是「字串 (String)」的索引
                length = content.Length,
                parameter = parameter
            });
            cleanText = cleanText.Remove(match.Index, match.Length).Insert(match.Index, content);
        }

        contentText.text = cleanText;

        // 【修正核心 1】先強制更新一次網格，讓 TMP 解析所有 <b>, <color> 等原生標籤
        contentText.ForceMeshUpdate();

        // 【修正核心 2】快取一份乾淨的頂點與顏色資料，供 Update 動畫使用
        cachedMeshInfo = contentText.textInfo.CopyMeshInfoVertexData();

        // 【修正核心 3】取得真實的「可見字元總數」，而不是字串長度
        int totalVisibleChars = contentText.textInfo.characterCount;

        contentText.maxVisibleCharacters = 0;
        // 【修正】只有當「真的有特效標籤」時，才開啟每幀動畫更新
        isAnimatingText = textEffects.Count > 0;

        if (typeWriterCoroutine != null) StopCoroutine(typeWriterCoroutine);
        
        // 傳入 totalVisibleChars 避免打字機停頓
        typeWriterCoroutine = StartCoroutine(TypeWriterEffect(totalVisibleChars, typeSpeed));
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
        // 檢查是否含有需要持續每幀變動的「動態特效」
        bool hasDynamicEffects = textEffects.Any(e => e.type == TextEffectType.Shake || 
                                                      e.type == TextEffectType.Wave || 
                                                      e.type == TextEffectType.Rainbow);

        if (speed <= 0)
        {
            contentText.maxVisibleCharacters = totalChars;
            
            // 瞬間顯示時，若沒有動態特效，立刻關閉動畫更新以策安全
            if (!hasDynamicEffects) isAnimatingText = false;
            
            typeWriterCoroutine = null;
            yield break; 
        }

        for (int i = 0; i <= totalChars; i++)
        {
            contentText.maxVisibleCharacters = i;
            yield return new WaitForSeconds(speed);
        }

        // 【修正】打字完全結束後，若沒有動態持續型特效（如只有Color或根本沒特效）
        // 立即關閉 isAnimatingText，讓網格回歸靜態，確保絕對不會閃爍與切字
        if (!hasDynamicEffects)
        {
            isAnimatingText = false;
        }

        typeWriterCoroutine = null;
    }

    /// <summary>
    /// 【新輔助函式】
    /// 嘗試將字串 (如 "red", "yellow" 或 "#FF0000") 解析為 Color
    /// </summary>
    private Color ParseColor(string colorString)
    {
        Color parsedColor;
        
        // ColorUtility.TryParseHtmlString 非常強大，
        // 它可以處理 "red", "blue" 等顏色名稱，
        // 也可以處理 "#FF0000" (有#) 和 "FF0000" (無#) 的 16 進位碼
        if (ColorUtility.TryParseHtmlString(colorString, out parsedColor))
        {
            return parsedColor;
        }

        // 如果解析失敗，返回預設顏色並顯示警告
        Debug.LogWarning($"[DialogueUI] 無法解析顏色標籤: '{colorString}'。將使用預設白色。");
        return Color.white;
    }
    
    private void AnimateText()
    {
        // 【修正核心 4】絕對不要在這裡呼叫 contentText.ForceMeshUpdate(); 

        var textInfo = contentText.textInfo;
        if (textInfo.characterCount == 0 || cachedMeshInfo == null) return;

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            var charInfo = textInfo.characterInfo[i];
            if (!charInfo.isVisible) continue;

            int matIndex = charInfo.materialReferenceIndex;
            Vector3[] vertices = textInfo.meshInfo[matIndex].vertices;
            Color32[] colors = textInfo.meshInfo[matIndex].colors32;
            
            // 讀取快取的原始資料，確保我們的位移不會無限累加
            Vector3[] sourceVertices = cachedMeshInfo[matIndex].vertices;
            Color32[] sourceColors = cachedMeshInfo[matIndex].colors32;

            Vector3 offset = Vector3.zero;
            bool applyCustomColor = false;
            Color32 customColor = Color.white;

            foreach (var effect in textEffects)
            {
                // 【修正核心 5】使用 charInfo.index (該字元在原字串中的位置) 
                // 來取代原本的 i (可見字元索引)。這樣不管有幾個 <b>，特效位置都不會跑掉！
                if (charInfo.index >= effect.startIndex && charInfo.index < effect.startIndex + effect.length)
                {
                    if (effect.type == TextEffectType.Shake)
                    {
                        offset += new Vector3(UnityEngine.Random.Range(-textShakeIntensity, textShakeIntensity), UnityEngine.Random.Range(-textShakeIntensity, textShakeIntensity), 0);
                    }
                    else if (effect.type == TextEffectType.Wave)
                    {
                        offset += new Vector3(0, Mathf.Sin(Time.time * waveSpeed + i * 0.5f) * waveAmplitude, 0);
                    }
                    else if (effect.type == TextEffectType.Rainbow)
                    {
                        float hue = Mathf.Repeat(Time.time * rainbowSpeed + i * 0.1f, 1f);
                        customColor = Color.HSVToRGB(hue, 1, 1);
                        applyCustomColor = true;
                    }
                    else if (effect.type == TextEffectType.Color)
                    {
                        customColor = ParseColor(effect.parameter);
                        applyCustomColor = true;
                    }
                }
            }

            // 統一將位移與顏色覆蓋回 Mesh
            for (int j = 0; j < 4; j++)
            {
                vertices[charInfo.vertexIndex + j] = sourceVertices[charInfo.vertexIndex + j] + offset;
                
                // 【修正】在覆蓋顏色前，先保留 TMP 動態管理的 alpha 值。
                // 打字機效果中，TMP 會將超出 maxVisibleCharacters 的字元 alpha 設為 0，
                // 若直接使用 cachedMeshInfo 的 alpha（全部為 255），會讓不可見字元閃爍出現。
                byte dynamicAlpha = colors[charInfo.vertexIndex + j].a;
                
                // 如果有套用自訂顏色特效就覆蓋，否則保留 TMP 標籤原有的顏色
                Color32 baseColor = applyCustomColor ? customColor : sourceColors[charInfo.vertexIndex + j];
                colors[charInfo.vertexIndex + j] = new Color32(baseColor.r, baseColor.g, baseColor.b, dynamicAlpha);
            }
        }

        // 推送更新至 TMP 渲染組件
        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            var meshInfo = textInfo.meshInfo[i];
            meshInfo.mesh.vertices = meshInfo.vertices;
            meshInfo.mesh.colors32 = meshInfo.colors32; 
            contentText.UpdateGeometry(meshInfo.mesh, i);
        }
    }

    // 強制完成打字效果
    public void CompleteText(string fullText)
    {
        if (typeWriterCoroutine != null)
        {
            StopCoroutine(typeWriterCoroutine);
            typeWriterCoroutine = null;
        }

        // 傳入 0f，TypeWriterEffect 的保護機制會讓它瞬間印出全部文字，
        // 同時確保快取機制與特效重新正確綁定。
        ProcessTextForEffects(fullText, 0f); 
    }

    public void ShowChoices(List<string> readyToDisplayStrings, Action<int> onChoiceSelected)
    {
        ClearChoices();
        choiceContainer.SetActive(true);

        for (int i = 0; i < readyToDisplayStrings.Count; i++)
        {
            Button newButton = Instantiate(choiceButtonPrefab, choiceContainer.transform);

            // --- 【核心修正】 ---
            // 列表中的字串 "已經" 是最終文字，直接顯示它
            newButton.GetComponentInChildren<TextMeshProUGUI>().text = readyToDisplayStrings[i];
            // --- 修正結束 ---

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