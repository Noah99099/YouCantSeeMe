using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.EventSystems;
using System.Linq;
using System.Collections; // 為了 Coroutine
using UnityEngine.Rendering.Universal;
// 【新增此行：讓您可以直接使用 _Player, _Keypad 等常量】
using static InputActionMaps;

/// <summary>
/// 與場景物件交互一律寫這裡
/// </summary>
public class PlayerInteraction : MonoBehaviour
{
    public static PlayerInteraction Instance { get; private set; } // 添加單例模式
    public InteractableObject CurrentTarget { get; set; } // 玩家正在交互的物件。單一物品放置判定點
    // ***** [修正 1: 新增變數] *****
    // 因為 CurrentTarget 是 InteractableObject 類型，無法存儲新的 ItemPlacementSpot
    // 所以我們需要一個專門存儲 "多物品放置點" 的變數
    public ItemPlacementSpot CurrentPlacementSpot { get; set; }

    [Header("互動設定")]
    [SerializeField] private float interactionRange = 3f;
    [SerializeField] private LayerMask interactionLayer;

    [Header("UI 提示")]
    [SerializeField] private TextMeshProUGUI pickupPromptText;

    [Header("提供接口的UI提示")]
    [Tooltip("它們不會在該腳本中使用")]
    public GameObject crossHair;
    public GameObject titleUI;

    // ----- [新需求] 聲音物品 -----
    [Header("聲音物品設定")]
    [SerializeField] private Transform cornerAnchor; // 請將 Camera 的子物件 cornerAnchor 拖曳到此[Header("特效與聲音 (可選)")]
    [Tooltip("獲得聲音物品的雜音 AudioSource")]
    [SerializeField] private AudioSource staticNoiseSource;

    [Tooltip("請將 Main Camera (或掛載 ScreenGlitchEffect 腳本的物件) 拖曳到此")]
    [SerializeField] public ScreenGlitchEffect glitchController; // [修改] 引用特效控制器

    [Tooltip("UI 波形圖腳本")]
    public WaveformVisualizer waveformUI;

    private GameObject currentVoiceItemModel;

    // ----- [新需求] 狀態管理 -----
    public bool IsVoiceItemActive { get; private set; } // 核心狀態：是否正在使用聲音物品
    private VoiceItemData activeVoiceItemData; // 儲存當前正在使用的聲音物品

    [Header("Debug")]
    [SerializeField] private bool showDebugRay = true;

    [Header("案件紀錄簿-物品 腳本引用")]
    [SerializeField] private InventoryPanelUIController _inventoryPanelController;

    // 使用 [SerializeField] 更為專業，它在 Inspector 可見，但保持 private
    [SerializeField] private Camera playerCamera;

    private GameObject currentInteractableObject = null;
    private InteractableObject currentInteractable; // 用於存儲當前可交互物件 腳本，基本沒用到

    // ----- [!! 新增 !!] -----
    // 我們需要快取 (Cache) 這兩個效果參數
    private FilmGrain filmGrainEffect;
    private ChromaticAberration chromaticAberrationEffect;
    // ----- [!! 結束新增 !!] -----

    private void Awake()
    {
        // 實現單例模式，但不跨場景
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 確保攝影機已經在 Inspector 中拖曳賦值
        if (playerCamera == null)
        {
            Debug.LogError("Player Camera is not assigned in the Inspector!");
        }
        IsVoiceItemActive = false; // 初始化狀態：沒有使用聲音物品

        if (staticNoiseSource != null) // 初始不播放「獲得聲音物品雜音」
        {
            staticNoiseSource.volume = 0;
            staticNoiseSource.Stop();
        }
    }
    
    void Start()
    {
        pickupPromptText.gameObject.SetActive(false);

        // [修改] 初始化花屏特效
        if (glitchController != null)
        {
            glitchController.StopGlitch(); // 確保開始時是關閉的
            // ----- [!! 新增 !!] -----
            // 獲取 Glitch Volume Profile 中的參數，以便手動控制
            if (glitchController.glitchVolume != null && glitchController.glitchVolume.profile != null)
            {
                // 1. 抓取 FilmGrain
                if (!glitchController.glitchVolume.profile.TryGet(out filmGrainEffect))
                {
                    Debug.LogWarning($"[PlayerInteraction]  {glitchController.glitchVolume.name}  Profile 䤣 FilmGrainI");
                }
                
                // 2. 抓取 ChromaticAberration
                if (!glitchController.glitchVolume.profile.TryGet(out chromaticAberrationEffect))
                {
                    Debug.LogWarning($"[PlayerInteraction]  {glitchController.glitchVolume.name}  Profile 䤣 ChromaticAberrationI");
                }
            }
            else
            {
                Debug.LogError("[PlayerInteraction] Glitch Controller  'glitchVolume' wBoO Profile wI", this);
            }
            // ----- [!! 結束新增 !!] -----
        }
        else
        {
            Debug.LogError("[PlayerInteraction] 'Glitch Controller' 引用未設置！請將 Main Camera 拖曳到此欄位。", this);
        }
    }

    private void Update()
    {
        // 1. 如果正在使用聲音物品，跳過交互檢測
        if (IsVoiceItemActive)
        {
            HidePrompt(); // 確保在使用聲音物品時不顯示任何提示
            return;
        }

        // 2. 【關鍵修正】：如果玩家不在正常遊玩模式 (例如：打開了密碼鎖、案件紀錄簿、組合線索等)
        // 只要 CurrentMap 不是 _Player，就代表玩家正在操作介面，此時不應該顯示世界提示
        if (InputStackManager.Instance != null && InputStackManager.Instance.CurrentMap != _Player)
        {
            HidePrompt();
            return;
        }

        // 3. 【關鍵修正】：雙重保險。明確檢查背包面板是否為開啟狀態，或是正在與特定物件交互
        bool isInventoryOpen = _inventoryPanelController != null && _inventoryPanelController.IsInventoryPanelOpen;
        bool isInteractingWithTarget = CurrentTarget != null || CurrentPlacementSpot != null;

        if (isInventoryOpen || isInteractingWithTarget)
        {
            HidePrompt();
            return;
        }

        // ***** 【新增：檢查 Keypad 交互狀態】 *****
        // 假設 InputStackManager.Instance.CurrentMap 可以取得當前的 Action Map 名稱
        // 並且在 Keypad 模式下，它會是 InputActionMaps._Keypad
        if (InputStackManager.Instance != null &&
            InputStackManager.Instance.CurrentMap == _Keypad)
        {
            HidePrompt(); // 確保在密碼鎖交互期間不顯示世界中的交互提示
            return; // 不執行 ContinuousCheck()
        }

        // [!! 解決方案 !!]
        // 如果玩家正在與一個物件交互（即 CurrentTarget != null，通常意味著物品欄已打開）
        // 則也應該隱藏提示並跳過交互檢測
        // 【修改這裡】：同時檢查 CurrentTarget 或 CurrentPlacementSpot
        // 只要其中一個有值（代表正在交互/打開背包中），就隱藏提示文字並退出
        if (CurrentTarget != null || CurrentPlacementSpot != null)
        {
            HidePrompt(); // 確保在物品欄打開時，隱藏世界中的交互提示
            return; // 不執行 ContinuousCheck()
        }
        // [!! 解決方案結束 !!]

        ContinuousCheck();
    }

    // ==========================================
    // 瘦身後的 ContinuousCheck (處理UI提示)
    // ==========================================
    private void ContinuousCheck()
    {
        // [新需求] (已在 Update() 中檢查，但雙重保險)
        if (IsVoiceItemActive) return;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        currentInteractableObject = null;
        //currentInteractable = null; // 重置當前可交互物件

        if (showDebugRay)
        {
            Debug.DrawRay(ray.origin, ray.direction * interactionRange, Color.blue);
        }

        if (Physics.Raycast(ray, out RaycastHit hit, interactionRange, interactionLayer))
        {
            currentInteractableObject = hit.collider.gameObject;

            // 【關鍵魔法】：不管你是抽屜還是密碼鎖，只要你有實作 IInteractable，我就理你！
            if (currentInteractableObject.TryGetComponent<IInteractable>(out var interactable))
            {
                if (pickupPromptText != null)
                {
                    bool isGamepad = InputDeviceManager.Instance.CurrentInputType == InputDeviceManager.InputType.Gamepad;
                    // 直接跟物件要提示文字
                    pickupPromptText.text = interactable.GetInteractPrompt(isGamepad);
                    pickupPromptText.gameObject.SetActive(true);
                }
                return;
            }
        }

        HidePrompt();
    }


    // ==========================================
    // 瘦身後的 HandleInteraction (處理實際點擊)
    // ==========================================
    public void HandleInteraction()
    {
        // [新需求] 如果正在使用聲音物品，則完全阻止交互
        if (IsVoiceItemActive) return;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactionRange, interactionLayer)) // Raycast 檢測所有相關圖層 (Obstacle, Interactable)
        {
            GameObject hitObject = hit.collider.gameObject;

            // 1. 擋住射線的障礙物
            if (hitObject.layer == LayerMask.NameToLayer("Obstacle")) return;

            // 2. 【關鍵魔法】：呼叫目標自己的 Interact 邏輯
            if (hitObject.TryGetComponent<IInteractable>(out var interactable))
            {
                // 把自己(PlayerInteraction)傳過去，這樣物件才知道是誰觸發的
                interactable.Interact(this);
                HidePrompt();
            }
            else
            {
                Debug.LogWarning($"[HandleInteraction] {hitObject.name} 在交互層，但沒有實作 IInteractable 介面！");
            }
        }
    }

    // ==========================================
    // 新增：提供給「需要打開背包的物件」呼叫的公開方法
    // ==========================================
    public void OpenInventoryForTarget(InteractableObject target)
    {
        CurrentTarget = target;
        CurrentPlacementSpot = null;
        if (_inventoryPanelController != null) _inventoryPanelController.OpenPanel(true);
    }

    public void OpenInventoryForSpot(ItemPlacementSpot spot)
    {
        CurrentPlacementSpot = spot;
        CurrentTarget = null;
        if (_inventoryPanelController != null) _inventoryPanelController.OpenPanel(true);
    }

    #region ===== 使用案件紀錄簿-物品的方法 =====
    /// <summary>
    /// 從背包使用物品按鈕呼叫(一般物品)
    /// </summary>
    /// <param name="item">被使用的物品</param>
    // 從背包使用物品按鈕呼叫
    public void OnItemUsed(ItemData item)
    {
        // [新需求] 檢查是否正在使用聲音物品
        if (IsVoiceItemActive)
        {
            Debug.LogWarning("[PlayerInteraction] 正在使用聲音物品，無法使用一般物品。");
            CloseInventoryAndExitInteraction();
            return;
        }

        // 1. 【關鍵修改】先將目標暫存起來 (Cache references)
        // 因為 CloseInventoryAndExitInteraction() 會把 CurrentTarget 清空，
        // 所以我們必須在清空前先把引用抓出來。
        var targetInteractable = CurrentTarget;
        var targetSpot = CurrentPlacementSpot;

        // 2. 【關鍵修改】先關閉背包與交互狀態
        // 這會將 Input State 重置回 "Gameplay"
        // 無論成功或失敗都關閉背包 + 退出交互模式
        CloseInventoryAndExitInteraction();

        // 3. 【關鍵修改】使用暫存的變數執行邏輯
        // 這樣如果這裡觸發了對話 (Input set to Dialogue)，就不會再被上面的 Close 覆蓋回去

        // 情況 A: 目標是舊腳本 (InteractableObject)
        if (targetInteractable != null)
        {
            bool success = targetInteractable.UseItem(item);
            if (success)
            {
                InventoryManager.Instance.RemoveItem(item);
                Debug.Log($"[PlayerInteraction] 舊腳本判定：{item.itemName} 使用成功");
            }
            else
            {
                Debug.Log($"[PlayerInteraction] 舊腳本判定：{item.itemName} 錯誤");
                // 此時觸發 onWrongItemUsed -> 對話開啟 -> Input 鎖定為 Dialogue -> 成功！
            }
        }
        // 情況 B: 目標是新腳本 (ItemPlacementSpot)
        else if (targetSpot != null)
        {
            // 呼叫 ItemPlacementSpot 的 TryPlaceItem
            bool success = targetSpot.TryPlaceItem(item);
            if (success)
            {
                InventoryManager.Instance.RemoveItem(item);
                Debug.Log($"[PlayerInteraction] 新腳本判定：{item.itemName} 放置成功");
            }
            else
            {
                Debug.Log($"[PlayerInteraction] 新腳本判定：{item.itemName} 不可放置於此");
            }
        }
        else
        {
            Debug.LogWarning("[PlayerInteraction] 沒有任何交互目標，無法使用物品");
        }
    }

    /// <summary>
    /// 關閉背包並退出交互模式
    /// </summary>
    public void CloseInventoryAndExitInteraction()
    {
        Debug.Log($"[PlayerInteraction] CloseInventoryAndExitInteraction start. CurrentTarget={(CurrentTarget != null ? CurrentTarget.name : "null")}");

        // 呼叫新的 InventoryPanelUIController 來關閉面板
        if (_inventoryPanelController != null)
        {
            _inventoryPanelController.ClosePanel(); // <--- 正確的關閉指令
        }
        else
        {
            Debug.LogError("[PlayerInteraction] _inventoryPanelController 的引用是 null！無法關閉面板。", this.gameObject);
        }

        // 清空交互目標仍然是 PlayerInteraction 的職責
        // 清空所有類型的交互目標
        CurrentTarget = null;
        CurrentPlacementSpot = null; // [新增] 也要清空這個

        Debug.Log($"[PlayerInteraction] CloseInventoryAndExitInteraction end. CurrentTarget={(CurrentTarget != null ? CurrentTarget.name : "null")}");
    }
    #endregion

    #region ===== [新需求] 聲音物品使用流程 =====
    /// <summary>
    /// [新] 1. 從 VoicePanelUIController 呼叫，開始使用聲音物品
    /// </summary>
    public void UseVoiceItem(VoiceItemData voiceItem)
    {
        if (IsVoiceItemActive)
        {
            Debug.LogError($"[PlayerInteraction] 試圖使用 {voiceItem.itemName}，但 {activeVoiceItemData.itemName} 已經在使用了！");
            return;
        }

        Debug.Log($"[PlayerInteraction] 開始使用聲音物品: {voiceItem.itemName}");

        // 1. 進入激活狀態
        IsVoiceItemActive = true;
        activeVoiceItemData = voiceItem;

        // ----- [!! 解決方案 步驟 1 (已修改) !!] -----
        // 立刻將 Glitch Volume 的權重設為 1，
        // 這樣它就能以 P=20 優先級 "覆蓋" 陰視野 (P=10)
        if (glitchController != null)
        {
            glitchController.PlayOneShotGlitch(); 
        }

        // [!! 在這裡添加 !!] 
        // 呼叫 "瞬間音效" (Sound 1) 的協程
        StartCoroutine(PlayNoiseForDuration(1f));
        // ----- [!! 添加結束 !!] -----

        // 2. 在 cornerAnchor 顯示模型
        if (voiceItem.voiceItem != null && cornerAnchor != null)
        {
            // 清理舊的模型 (保險)
            if (currentVoiceItemModel != null) Destroy(currentVoiceItemModel);

            currentVoiceItemModel = Instantiate(voiceItem.voiceItem, cornerAnchor);
            currentVoiceItemModel.transform.localPosition = Vector3.zero;
            currentVoiceItemModel.transform.localRotation = Quaternion.identity;

            // 根據 ScriptableObject 中定義的 itemScale 來設定模型的本地縮放
            currentVoiceItemModel.transform.localScale = Vector3.one * voiceItem.itemScale;
        }
        else
        {
            Debug.LogWarning($"[PlayerInteraction] {voiceItem.itemName} 的 'voiceItem' Prefab 或 'cornerAnchor' 未設置！");
        }

        // 3. 激活場景中對應的判定點
        // (我們使用 FindObjectsOfType，因為判定點可能在任何地方)
        var detectionPoints = FindObjectsOfType<VoiceItemDetectionPoint>();
        bool foundPoint = false;
        foreach (var point in detectionPoints)
        {
            if (point.ActivatePoint(voiceItem))
            {
                foundPoint = true;
                Debug.Log($"[PlayerInteraction] 已激活判定點: {point.gameObject.name}");
            }
        }
        if (!foundPoint)
        {
            Debug.LogWarning($"[PlayerInteraction] 使用了 {voiceItem.itemName}，但在場景中沒有找到對應的 VoiceItemDetectionPoint！");
        }
    }

    /// <summary>
    /// [新] 2. 由 VoiceItemDetectionPoint 呼叫，完成使用
    /// </summary>
    public void CompleteVoiceItemUsage(VoiceItemData voiceItem)
    {
        if (!IsVoiceItemActive || voiceItem != activeVoiceItemData)
        {
            Debug.LogWarning($"[PlayerInteraction] CompleteVoiceItemUsage 被呼叫，但物品 {voiceItem.itemName} 與當前激活的 {activeVoiceItemData.itemName} 不符。");
            return;
        }

        Debug.Log($"[PlayerInteraction] 成功使用聲音物品: {voiceItem.itemName}");

        // 1. 退出激活狀態
        IsVoiceItemActive = false;
        activeVoiceItemData = null;

        // 2. 刪除 cornerAnchor 中的模型
        if (currentVoiceItemModel != null)
        {
            Destroy(currentVoiceItemModel);
            currentVoiceItemModel = null;
        }

        // 3. 標記物品為「已使用」
        if (VoiceItemManager.Instance != null)
        {
            // 標記為「已使用」(這樣它才會成為一個線索)
            VoiceItemManager.Instance.MarkItemAsUsed(voiceItem);

            // [!!] 通知 CCM，並告知類型是「Sound」[!!]
            ClueCombinationManager.Instance?.CheckForNewPuzzleUnlocks(false, EClueType.Sound);
        }

        // [修改] 確保花屏特效停止
        if (glitchController != null)
        {
            glitchController.StopGlitch();
        }
    }

    /// <summary>
    /// [新] 拾取聲音物品時觸發一次花屏
    /// </summary>
    public IEnumerator PlayGlitchEffectOnce()
    {
        if (glitchController == null) yield break;

        Debug.Log("播放花屏特效 (1秒)");
        // ----- [!! 修改 !!] -----
        // 1. 手動將強度設為 1.0 (或你想要的最大值)
        if (filmGrainEffect != null) filmGrainEffect.intensity.value = 1.0f;
        if (chromaticAberrationEffect != null) chromaticAberrationEffect.intensity.value = 1.0f;
        
        // 2. 開啟 Volume (Weight = 1)
        glitchController.PlayOneShotGlitch(); 
        
        // 3. 播放聲音
        StartCoroutine(PlayNoiseForDuration(1f));
        
        // 4. 等待
        yield return new WaitForSeconds(1.0f);
        
        // 5. 關閉 Volume (Weight = 0)
        glitchController.StopGlitch(); 
        
        // 6. 手動將強度歸零 (清理狀態)
        if (filmGrainEffect != null) filmGrainEffect.intensity.value = 0.0f;
        if (chromaticAberrationEffect != null) chromaticAberrationEffect.intensity.value = 0.0f;
        // ----- [!! 結束修改 !!] -----
        Debug.Log("花屏特效結束");
    }

    /// <summary>
    /// [新] 拾取聲音物品時觸發一次雜音
    /// </summary>
    /// <param name="duration">時長</param>
    /// <returns></returns>
    private IEnumerator PlayNoiseForDuration(float duration)
    {
        staticNoiseSource.volume = 1;
        staticNoiseSource.Play();
        yield return new WaitForSeconds(duration);
        staticNoiseSource.Stop();
        staticNoiseSource.volume = 0;
    }

    #endregion

    private void HidePrompt() 
    {
        if (pickupPromptText != null && pickupPromptText.gameObject.activeSelf)
        {
            pickupPromptText.gameObject.SetActive(false);
        }
    }
}