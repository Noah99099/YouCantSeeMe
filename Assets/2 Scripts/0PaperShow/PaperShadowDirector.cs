using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Spine.Unity; // 記得加上這一行
using UnityEngine.InputSystem; // 用於支援新的 Input System

public class PaperShadowDirector : MonoBehaviour
{
    [Header("基礎配置")]
    public CanvasGroup mainCanvas;
    public GameObject otherUIObject; // 裝 UI 的空物件
    public PaperShadowUIHelper helper;

    [Header("場景邏輯腳本")]
    [Tooltip("演出結束後需要重新開啟的場景對話腳本")]
    public GameObject targetSceneObject;   // 結束後要開啟的場景空物件

    [Header("玩家控制設定")]
    [Tooltip("自動獲取，不需手動拖曳")]
    public SimpleFirstPersonController playerController;
    private float originalRotationSpeed = 1.0f; // 儲存玩家原本的旋轉速度

    [Header("時間控制項目 - 1. 圖片1、2")]
    public float img12HoldTime = 2.0f;
    public float img12FadeTime = 1.0f;

    [Header("時間控制項目 - 2. 圖片3~6 (分鏡8-11)")]
    public float img36HoldTime = 2.0f;
    public float img36FadeTime = 1.0f;

    [Header("時間控制項目 - 3. 圖片7、8 (配合蓋章音效)")]
    public float img78HoldTime = 2.0f;
    public float img78FadeTime = 1.0f;

    [Header("時間控制項目 - 4. 圖片9 (過場黑幕)")]
    public float img9HoldTime = 2.0f;
    public float img9FadeTime = 1.0f;

    [Header("音樂控制")]
    [Tooltip("演出專用的 AudioSource，用來播放演出背景音")]
    public AudioSource performanceAudioSource;
    [Tooltip("演出專用的背景音樂")]
    public AudioClip performanceBGM;
    [Tooltip("音樂淡入淡出的時間")]
    public float audioFadeDuration = 1.0f;

    [Tooltip("演出專用的 AudioSource，用來播放音效 (SE)")]
    public AudioSource seAudioSource;
    [Tooltip("圖片7蓋上去的音效")]
    public AudioClip stampSE;

    [Header("圖片控制 (請照索引 1-10 放入)")]
    public CanvasGroup[] images; // 索引 0 對應圖片1

    // --- 修改這裡 ---
    [Header("Spine 動畫設定")]
    [Tooltip("僅用於讓下方的陣列能在 Inspector 抓到動畫選單，請拖入同一個 Spine UI 物件")]
    public SkeletonGraphic referenceSpineGraphic;

    [Tooltip("請設定 30 個大小，並依序選取對應的動畫 (索引 0 = 動畫 1)")]
    [SpineAnimation(dataField: "referenceSpineGraphic")]
    public string[] animNames;
    // --------------

    private bool isDialogueActive = false;
    private bool isSkipping = false;
    private Coroutine performanceCoroutine;

    private void Start()
    {
        performanceCoroutine = StartCoroutine(PerformanceSequence());
    }

    private void Update()
    {
        // 如果已經在跳過流程中，就不再重複執行
        if (isSkipping) return;

        bool skipPressed = false;

        // 兼容舊版與新版 Input System 的數字鍵 2
        if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
        {
            skipPressed = true;
        }
        else if (Keyboard.current != null && Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            skipPressed = true;
        }

        if (skipPressed)
        {
            SkipSequence();
        }
    }

    private void SkipSequence()
    {
        isSkipping = true;

        // 停止正常演出排程與漸變
        if (performanceCoroutine != null) StopCoroutine(performanceCoroutine);
        helper.StopAllCoroutines();
        StopAllCoroutines();

        StartCoroutine(SkipCoroutine());
    }

    private IEnumerator SkipCoroutine()
    {
        // 1. 如果玩家正在對話中按下跳過，我們快速把當前對話跑到結束以釋放狀態
        if (DialogueManager.Instance != null)
        {
            while (DialogueManager.Instance.IsDialogueActive())
            {
                DialogueManager.Instance.TriggerSkip();
                yield return null;
            }
        }
        isDialogueActive = false;

        // 2. 先把 圖片9 打開且透明度調1
        if (images[8] != null)
        {
            images[8].gameObject.SetActive(true);
            images[8].alpha = 1f;
        }

        // 3. 其餘圖片和 Spine動畫(UI) 直接刪除
        for (int i = 0; i < images.Length; i++)
        {
            if (i != 8 && images[i] != null)
            {
                Destroy(images[i].gameObject);
            }
        }
        if (referenceSpineGraphic != null)
        {
            Destroy(referenceSpineGraphic.gameObject);
        }

        // 【新增音樂恢復】：在畫面淡出期間，同時淡出演出音樂並恢復場景音樂
        if (performanceAudioSource != null) StartCoroutine(helper.FadeAudioSource(performanceAudioSource, 0f, 2f));
        if (AudioManager.Instance != null) AudioManager.Instance.SetVideoMute(false, 2f);

        // 4. 最後再執行 MainCanvas 消失
        if (mainCanvas != null)
        {
            yield return helper.FadeCanvasGroup(mainCanvas, 0, 2f);
        }

        // 5. 觸發遊戲正式開始
        otherUIObject.SetActive(true);
        if (targetSceneObject != null) targetSceneObject.SetActive(true);

        yield return StartDialogue("StartGame");

        // 【新增】恢復玩家視角旋轉
        if (playerController != null)
        {
            playerController.RotationSpeed = originalRotationSpeed;
        }

        if (mainCanvas != null) Destroy(mainCanvas.gameObject);
        Debug.Log("已成功跳過演出並清理資源");
    }

    private IEnumerator PerformanceSequence()
    {
        // ================== 初始配置 ==================
        InitSetup();

        // ================== 分鏡 0 ==================
        InputStackManager.Instance.PushMap(InputActionMaps._Loading);
        otherUIObject.SetActive(false);

        // 【新增音樂切換】：讓主場景音樂暫時靜音，並開始播放演出的 BGM
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetVideoMute(true, audioFadeDuration);
        }
        if (performanceAudioSource != null && performanceBGM != null)
        {
            performanceAudioSource.clip = performanceBGM;
            performanceAudioSource.loop = true;
            performanceAudioSource.volume = 0f;
            performanceAudioSource.Play();
            StartCoroutine(helper.FadeAudioSource(performanceAudioSource, 1f, audioFadeDuration));
        }

        // 使用 圖片1、2 的專屬時間控制
        yield return helper.Wait(img12HoldTime);
        yield return helper.FadeCanvasGroup(images[0], 0, img12FadeTime);
        yield return helper.Wait(img12HoldTime);
        yield return helper.FadeCanvasGroup(images[1], 0, img12FadeTime);

        // [修改] 關掉後直接刪除
        Destroy(images[0].gameObject);
        Destroy(images[1].gameObject);

        yield return helper.PlaySpineAndWait(animNames[0]); // 動畫1
        yield return helper.PlaySpineAndWait(animNames[1]); // 動畫2
        helper.PlaySpine(animNames[2], true); // 動畫3 (過渡)

        // ================== 分鏡 1 ==================
        helper.PlaySpine(animNames[3], true); // 動畫4 (不用播完，先循環放著)
        yield return StartDialogue("TheShow1");
        otherUIObject.SetActive(false);

        // 【修正】只要"TheShow1"結束 -> 動畫4 播放到底，暫停
        helper.StopCurrentLoopAndClearQueue();
        yield return helper.WaitForCurrentAnimation();

        // 播放 動畫5(過渡)
        yield return helper.PlaySpineAndWait(animNames[4]); // 動畫5
        // 播放 動畫6 到底，暫停
        yield return helper.PlaySpineAndWait(animNames[5]); // 動畫6
        // 播放 動畫7(過渡)
        yield return helper.PlaySpineAndWait(animNames[6]); // 動畫7

        // ================== 分鏡 2 ==================
        yield return StartDialogue("TheShow2");
        otherUIObject.SetActive(false);
        yield return helper.PlaySpineAndWait(animNames[7]); // 動畫8
        yield return helper.PlaySpineAndWait(animNames[8]); // 動畫9(過渡)

        // ================== 分鏡 3 ==================
        yield return StartDialogue("TheShow3");
        otherUIObject.SetActive(false);
        yield return helper.PlaySpineAndWait(animNames[9]); // 動畫10
        yield return helper.PlaySpineAndWait(animNames[10]); // 動畫11(過渡)

        // ================== 分鏡 4 ==================
        yield return StartDialogue("TheShow4");
        otherUIObject.SetActive(false);
        yield return helper.PlaySpineAndWait(animNames[11]); // 動畫12
        yield return helper.PlaySpineAndWait(animNames[12]); // 動畫13(過渡)

        // ================== 分鏡 5 ==================
        yield return helper.PlaySpineAndWait(animNames[13]); // 動畫14 到底
        yield return helper.PlaySpineAndWait(animNames[14]); // 動畫15(過渡)

        // 播放 動畫16 -> 只要動畫16播放到底，自動接動畫17循環
        helper.PlaySpine(animNames[15], false);
        helper.AddAnimation(animNames[16], true);

        yield return StartDialogue("TheShow5");
        otherUIObject.SetActive(false);

        // 【修正】只要"TheShow5"結束 -> 動畫17 播放到底，暫停
        helper.StopCurrentLoopAndClearQueue();
        yield return helper.WaitForCurrentAnimation();

        // 播放 動畫18(過渡)
        yield return helper.PlaySpineAndWait(animNames[17]); // 動畫18
        // 播放 動畫19 到底，暫停
        yield return helper.PlaySpineAndWait(animNames[18]); // 動畫19
        // 播放 動畫20(過渡)
        helper.PlaySpine(animNames[19], true);               // 動畫20

        // ================== 分鏡 6 ==================
        yield return StartDialogue("TheShow6");
        otherUIObject.SetActive(false);
        yield return helper.PlaySpineAndWait(animNames[20]); // 動畫21
        yield return helper.PlaySpineAndWait(animNames[21]); // 動畫22(過渡)

        // ================== 分鏡 7 ==================
        yield return StartDialogue("TheShow7");
        otherUIObject.SetActive(false);
        yield return helper.FadeCanvasGroup(images[2], 1, img36FadeTime);
        images[3].gameObject.SetActive(true); // 圖片4
        images[4].gameObject.SetActive(true); // 圖片5
        images[5].gameObject.SetActive(true); // 圖片6
        yield return helper.PlaySpineAndWait(animNames[22]); // 動畫23
        helper.PlaySpine(animNames[23], true); // 動畫24(循環)

        // ================== 分鏡 8 - 11 ==================
        yield return StartDialogue("TheShow8");
        otherUIObject.SetActive(false);
        yield return helper.Wait(img36HoldTime);
        yield return helper.FadeCanvasGroup(images[2], 0, img36FadeTime);
        Destroy(images[2].gameObject); // [修改] 刪除圖片3

        yield return StartDialogue("TheShow9");
        otherUIObject.SetActive(false);
        yield return helper.Wait(img36HoldTime);
        yield return helper.FadeCanvasGroup(images[3], 0, img36FadeTime);
        Destroy(images[3].gameObject); // [修改] 刪除圖片4

        yield return StartDialogue("TheShow10");
        otherUIObject.SetActive(false);
        yield return helper.Wait(img36HoldTime);
        yield return helper.FadeCanvasGroup(images[4], 0, img36FadeTime);
        Destroy(images[4].gameObject); // [修改] 刪除圖片5

        yield return StartDialogue("TheShow11");
        otherUIObject.SetActive(false);
        yield return helper.Wait(img36HoldTime);
        yield return helper.FadeCanvasGroup(images[5], 0, img36FadeTime);
        Destroy(images[5].gameObject); // [修改] 刪除圖片6

        yield return helper.PlaySpineAndWait(animNames[24]); // 動畫25(過渡)

        // ================== 分鏡 12 ==================
        // 1. 播放 26 -> 預約 27 -> 預約 28(循環)
        helper.PlaySpine(animNames[25], false);       // 26
        helper.AddAnimation(animNames[26], false);    // 27(過渡)
        helper.AddAnimation(animNames[27], true);     // 28

        // 2. 啟動對話並等待其結束
        yield return StartDialogue("TheShow12");
        otherUIObject.SetActive(false);

        // 3. 對話結束，告知 Spine 當前動畫停止循環，並清空尚未輪到的排隊
        helper.StopCurrentLoopAndClearQueue();

        // 4. 等待當前正在播放的動畫 (不論卡在 26、27 還是 28) 完整播完這圈
        yield return helper.WaitForCurrentAnimation();

        // 5. 接續您的邏輯，直接接 29 播完 -> 30 過渡
        yield return helper.PlaySpineAndWait(animNames[28]); // 動畫 29
        yield return helper.PlaySpineAndWait(animNames[29]); // 動畫 30(過渡)

        // ================== 分鏡 13 ==================
        yield return StartDialogue("TheShow13");
        otherUIObject.SetActive(false);

        yield return helper.Wait(img9HoldTime);
        yield return helper.FadeCanvasGroup(images[8], 1, img9FadeTime); // 圖片9 打開

        yield return helper.Wait(img78HoldTime);
        yield return helper.FadeCanvasGroup(images[6], 1, img12FadeTime); // 圖片7 打開

        // --- 【新增音效播放】 ---
        if (seAudioSource != null && stampSE != null)
        {
            seAudioSource.PlayOneShot(stampSE);
        }

        images[7].gameObject.SetActive(true); // 圖片8 打開

        yield return helper.Wait(img78HoldTime);
        yield return helper.FadeCanvasGroup(images[6], 0, img78FadeTime); // 圖片7 透明度歸0

        yield return helper.Wait(img9HoldTime);

        // [修改] 在 MainCanvas 透明度歸 0 以前，將其餘圖片與 Spine 刪除，只留圖片 9 (index 8)
        if (images[6] != null) Destroy(images[6].gameObject); // 圖片7
        if (images[7] != null) Destroy(images[7].gameObject); // 圖片8
        if (images[9] != null) Destroy(images[9].gameObject); // 圖片10 (底圖)
        if (referenceSpineGraphic != null) Destroy(referenceSpineGraphic.gameObject);

        // 【新增音樂恢復】：在畫面淡出期間，同時淡出演出音樂並恢復場景音樂
        if (performanceAudioSource != null) StartCoroutine(helper.FadeAudioSource(performanceAudioSource, 0f, 2f));
        if (AudioManager.Instance != null) AudioManager.Instance.SetVideoMute(false, 2f);

        // 執行最終淡出
        yield return helper.FadeCanvasGroup(mainCanvas, 0, 2f);

        otherUIObject.SetActive(true);
        if (targetSceneObject != null) targetSceneObject.SetActive(true); // 打開指定場景物件
        yield return StartDialogue("StartGame"); // 呼叫初始對話

        // 【新增】恢復玩家視角旋轉
        if (playerController != null)
        {
            playerController.RotationSpeed = originalRotationSpeed;
        }

        Destroy(mainCanvas.gameObject);
        Debug.Log("演出正式結束並清理資源");
    }

    private void InitSetup()
    {
        if (targetSceneObject != null) targetSceneObject.SetActive(false); // 初始關閉

        // 【新增】尋找玩家並儲存視角旋轉速度，然後歸零
        if (playerController == null)
        {
            playerController = FindObjectOfType<SimpleFirstPersonController>();
        }

        if (playerController != null)
        {
            originalRotationSpeed = playerController.RotationSpeed;
            playerController.RotationSpeed = 0f;
            Debug.Log($"[PaperShadowDirector] 已鎖定玩家視角旋轉 (原始速度: {originalRotationSpeed})");
        }

        // 初始圖片狀態 (Index 0=圖1, 9=圖10)
        for (int i = 0; i < images.Length; i++)
        {
            images[i].alpha = 1;
            images[i].gameObject.SetActive(false);
        }

        // 圖1,2,3,7,9,10 打開
        int[] startActive = { 0, 1, 2, 6, 8, 9 };
        foreach (int i in startActive) images[i].gameObject.SetActive(true);

        // 圖3,7,9 透明度 0
        images[2].alpha = 0;
        images[2].alpha = 0; // 圖片3
        images[6].alpha = 0; // 圖片7
        images[8].alpha = 0; // 圖片9

        // 監聽對話結束
        DialogueManager.Instance.OnConversationEnd += () => { isDialogueActive = false; };
    }

    private IEnumerator StartDialogue(string eventID)
    {
        isDialogueActive = true;
        DialogueManager.Instance.TriggerDialogueByEvent(eventID);
        yield return new WaitUntil(() => !isDialogueActive);
        InputStackManager.Instance.PushMap(InputActionMaps._Loading); // 確保對話完回到 Loading
    }
}