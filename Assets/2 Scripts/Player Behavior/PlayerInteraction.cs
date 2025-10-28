using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.EventSystems;
using System.Linq;
using System.Collections; // 為了 Coroutine

/// <summary>
/// 與場景物件交互一律寫這裡
/// </summary>
public class PlayerInteraction : MonoBehaviour
{
    public static PlayerInteraction Instance { get; private set; } // 添加單例模式
    public InteractableObject CurrentTarget { get; set; } // 玩家正在交互的物件

    [Header("互動設定")]
    [SerializeField] private float interactionRange = 3f;
    [SerializeField] private LayerMask interactionLayer;

    [Header("UI 提示")]
    [SerializeField] private TextMeshProUGUI pickupPromptText;

    // ----- [新需求] 聲音物品 -----
    [Header("聲音物品設定")]
    [SerializeField] private Transform cornerAnchor; // 請將 Camera 的子物件 cornerAnchor 拖曳到此

    [Tooltip("請將 Main Camera (或掛載 ScreenGlitchEffect 腳本的物件) 拖曳到此")]
    [SerializeField] public ScreenGlitchEffect glitchController; // [修改] 引用特效控制器

    private GameObject currentVoiceItemModel;

    // ----- [新需求] 狀態管理 -----
    public bool IsVoiceItemActive { get; private set; } // 核心狀態：是否正在使用聲音物品
    private VoiceItemData activeVoiceItemData; // 儲存當前正在使用的聲音物品

    [Header("Debug")]
    [SerializeField] private bool showDebugRay = true;

    [Header("案件紀錄簿-物品 腳本引用")]
    [SerializeField] private InventoryPanelUIController _inventoryPanelController;

    private Camera playerCamera;
    
    private GameObject currentInteractableObject = null;
    private InteractableObject currentInteractable; // 用於存儲當前可交互物件 腳本

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

        playerCamera = Camera.main;
        IsVoiceItemActive = false; // 初始化狀態：沒有使用聲音物品
    }
    
    void Start()
    {
        pickupPromptText.gameObject.SetActive(false);

        // [修改] 初始化花屏特效
        if (glitchController != null)
        {
            glitchController.StopGlitch(); // 確保開始時是關閉的
        }
        else
        {
            Debug.LogError("[PlayerInteraction] 'Glitch Controller' 引用未設置！請將 Main Camera 拖曳到此欄位。", this);
        }
    }

    private void Update()
    {
        // [新需求] 如果正在使用聲音物品，則完全跳過交互檢測
        if (IsVoiceItemActive)
        {
            HidePrompt(); // 確保在使用聲音物品時不顯示任何提示
            return;
        }

        // [!! 解決方案 !!]
        // 如果玩家正在與一個物件交互（即 CurrentTarget != null，通常意味著物品欄已打開）
        // 則也應該隱藏提示並跳過交互檢測
        if (CurrentTarget != null)
        {
            HidePrompt(); // 確保在物品欄打開時，隱藏世界中的交互提示
            return; // 不執行 ContinuousCheck()
        }
        // [!! 解決方案結束 !!]

        ContinuousCheck();
    }
    
    private void ContinuousCheck()
    {
        // [新需求] (已在 Update() 中檢查，但雙重保險)
        if (IsVoiceItemActive) return;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        currentInteractableObject = null;
        currentInteractable = null; // 重置當前可交互物件

        if (showDebugRay)
        {
            Debug.DrawRay(ray.origin, ray.direction * interactionRange, Color.blue);
        }

        if (Physics.Raycast(ray, out RaycastHit hit, interactionRange, interactionLayer))
        {
            currentInteractableObject = hit.collider.gameObject;

            if (currentInteractableObject.TryGetComponent<InteractableItem>(out var item)) // 檢查是否是 InteractableItem（獲得物品）
            {
                if (pickupPromptText != null)
                {
                    // 如果是手把模式，更換UI文本提示
                    if (InputDeviceManager.Instance.CurrentInputType == InputDeviceManager.InputType.Gamepad) 
                    {
                        pickupPromptText.text = $"按 [叉] 拾取 {item.itemData.itemName}";
                    }
                    else //鍵鼠
                    {
                        pickupPromptText.text = $"按 [滑鼠左鍵] 拾取 {item.itemData.itemName}";
                    }    
                    pickupPromptText.gameObject.SetActive(true);
                }
                return;
            }
            else if (currentInteractableObject.TryGetComponent<InteractableVoiceItem>(out var voice)) // 檢查是否是 InteractableVoiceItem（需聲音物品的交互物件）
            {
                if (pickupPromptText != null)
                {
                    // 如果是手把模式，更換UI文本提示
                    if (InputDeviceManager.Instance.CurrentInputType == InputDeviceManager.InputType.Gamepad)
                    {
                        pickupPromptText.text = $"按 [叉] 與 {voice.voiceItemData.itemName} 交互";
                    }
                    else //鍵鼠
                    {
                        pickupPromptText.text = $"按 [滑鼠左鍵] 與 {voice.voiceItemData.itemName} 交互";
                    }
                    pickupPromptText.gameObject.SetActive(true);
                }
                return;
            }
            else if (currentInteractableObject.TryGetComponent<PasswordButton>(out var button)) // 檢查是否是 PasswordButton（密碼鎖）
            {
                if (pickupPromptText != null)
                {
                    // 如果是手把模式，更換UI文本提示
                    if (InputDeviceManager.Instance.CurrentInputType == InputDeviceManager.InputType.Gamepad)
                    {
                        pickupPromptText.text = "按 [叉] 按下按鈕";
                    }
                    else //鍵鼠
                    {
                        pickupPromptText.text = "按 [滑鼠左鍵] 按下按鈕";
                    }
                    pickupPromptText.gameObject.SetActive(true);
                }
                return;
            }
            else if (currentInteractableObject.TryGetComponent<InteractableObject>(out var interactable)) // 檢查是否是 InteractableObject（需物品的交互點）
            {
                currentInteractable = interactable;
                if (pickupPromptText != null)
                {
                    // 如果是手把模式，更換UI文本提示
                    if (InputDeviceManager.Instance.CurrentInputType == InputDeviceManager.InputType.Gamepad)
                    {
                        pickupPromptText.text = $"按 [叉] 與 {interactable.objectName} 交互";
                    }
                    else //鍵鼠
                    {
                        pickupPromptText.text = $"按 [滑鼠左鍵] 與 {interactable.objectName} 交互";
                    }
                    pickupPromptText.gameObject.SetActive(true);
                }
                return;
            }
            else if (currentInteractableObject.TryGetComponent<InteractableRole>(out var role)) // 檢查是否是 InteractableRole（解鎖 Carousel）
            {
                if (pickupPromptText != null)
                {
                    // 如果是手把模式，更換UI文本提示
                    if (InputDeviceManager.Instance.CurrentInputType == InputDeviceManager.InputType.Gamepad)
                    {
                        pickupPromptText.text = $"按 [叉] 與 {role.objectName} 交互";
                    }
                    else //鍵鼠
                    {
                        pickupPromptText.text = $"按 [滑鼠左鍵] 與 {role.objectName} 交互";
                    }
                    pickupPromptText.gameObject.SetActive(true);
                }
                return;
            }
            else if (currentInteractableObject.TryGetComponent<CaseRecordBook>(out var book)) // 檢查是否是 案件紀錄簿，後續的功能才能啟用
            {
                if (pickupPromptText != null)
                {
                    // 如果是手把模式，更換UI文本提示
                    if (InputDeviceManager.Instance.CurrentInputType == InputDeviceManager.InputType.Gamepad)
                    {
                        pickupPromptText.text = $"按 [叉] 拾取 {book.itemName}";
                    }
                    else //鍵鼠
                    {
                        pickupPromptText.text = $"按 [滑鼠左鍵] 拾取 {book.itemName}";
                    }
                    pickupPromptText.gameObject.SetActive(true);
                }
                return;
            }
            else if (currentInteractableObject.TryGetComponent<Map>(out var map)) // 檢查是否是 平面圖
            {
                if (pickupPromptText != null)
                {
                    // 如果是手把模式，更換UI文本提示
                    if (InputDeviceManager.Instance.CurrentInputType == InputDeviceManager.InputType.Gamepad)
                    {
                        pickupPromptText.text = $"按 [叉] 拾取 {map.itemName}";
                    }
                    else //鍵鼠
                    {
                        pickupPromptText.text = $"按 [滑鼠左鍵] 拾取 {map.itemName}";
                    }
                    pickupPromptText.gameObject.SetActive(true);
                }
                return;
            }
        }

        HidePrompt();
    }

    public void HandleInteraction()
    {
        // [新需求] 如果正在使用聲音物品，則完全阻止交互
        if (IsVoiceItemActive)
        {
            Debug.Log("[PlayerInteraction] 正在使用聲音物品，交互已禁用。");
            return;
        }

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        Debug.Log($"[HandleInteraction] Raycasting from {ray.origin} toward {ray.direction}, Range={interactionRange}");

        if (Physics.Raycast(ray, out RaycastHit hit, interactionRange, interactionLayer))
        {
            GameObject hitObject = hit.collider.gameObject;
            Debug.Log($"[HandleInteraction] Ray hit object: {hitObject.name}, Layer: {LayerMask.LayerToName(hitObject.layer)}");

            // 檢查物件的所有交互腳本狀態
            var comps = hitObject.GetComponents<MonoBehaviour>();
            Debug.Log($"[HandleInteraction] Components on {hitObject.name}: {string.Join(", ", comps.Select(c => c.GetType().Name))}");


            if (hitObject.TryGetComponent<InteractableItem>(out var itemToPickUp)) //獲得物件，進物品背包
            {
                Debug.Log($"Picked up: {itemToPickUp.itemData.itemName}");
                InventoryManager.Instance.AddItem(itemToPickUp.itemData);
                Destroy(hitObject);
                HidePrompt();
            }
            else if (hitObject.TryGetComponent<InteractableVoiceItem>(out var voiceItem)) //獲得物件，進聲音面板
            {
                Debug.Log($"Pressed button: {voiceItem.voiceItemData.itemName}");
                // [修改] 交互成功，觸發花屏特效
                StartCoroutine(PlayGlitchEffectOnce());

                VoiceItemManager.Instance.AddItem(voiceItem.voiceItemData);
                Destroy(hitObject);
                HidePrompt();
            }
            else if (hitObject.TryGetComponent<InteractableRole>(out var roleUnlock)) //獲得個別Role的Carousel
            {
                Debug.Log($"與 {roleUnlock.objectName} 交互 → 解鎖 Carousel");
                roleUnlock.Interact();
                HidePrompt();
            }
            else if (hitObject.TryGetComponent<PasswordButton>(out var button)) //密碼鎖
            {
                Debug.Log($"Pressed button: {button.Value}");
                button.OnPress();
                HidePrompt();
            }
            else if (hitObject.TryGetComponent<CaseRecordBook>(out var book)) //案件紀錄簿
            {
                Debug.Log($"拾取了關鍵物品: {book.itemName}！");

                book.Collect();

                // 銷毀物件並隱藏提示
                Destroy(hitObject);
                HidePrompt();
            }
            else if (hitObject.TryGetComponent<Map>(out var map)) //平面圖
            {
                Debug.Log($"拾取了關鍵物品: {map.itemName}！");

                map.Collect();

                // 銷毀物件並隱藏提示
                Destroy(hitObject);
                HidePrompt();
            }
            else if (hitObject.TryGetComponent<InteractableObject>(out var interactable)) //使用物件
            {
                Debug.Log($"Interacting with: {interactable.objectName}"); //這裡沒有打開案件紀錄簿
                // 設定交互目標
                CurrentTarget = interactable;
                // 打開背包（交互模式）
                if (_inventoryPanelController != null)
                {
                    // true 代表是交互模式
                    _inventoryPanelController.OpenPanel(true);
                }
                HidePrompt();
            }
            else if (hitObject.TryGetComponent<InteractionTrigger>(out var trigger)) // 交互後執行對話(單視野)
            {
                Debug.Log($"[HandleInteraction] InteractionTrigger detected on {hitObject.name}");
                if (trigger.dialogueGraph == null)
                    Debug.LogWarning($"[HandleInteraction] InteractionTrigger on {hitObject.name} has NO DialogueGraph assigned!");
                else
                    Debug.Log($"[HandleInteraction] DialogueGraph assigned: {trigger.dialogueGraph.name}");

                if (DialogueManager.Instance == null)
                {
                    Debug.LogError("[HandleInteraction] DialogueManager.Instance is NULL — cannot start dialogue!");
                }
                else
                {
                    Debug.Log("[HandleInteraction] Calling DialogueManager.Instance.StartConversation()");
                    trigger.Interact();
                }
                HidePrompt();
                return;
            }
            else if (hitObject.TryGetComponent<BothViewInteractionTrigger>(out var bothTrigger)) // 交互後執行對話(雙視野)
            {
                Debug.Log($"[HandleInteraction] InteractionTrigger detected on {hitObject.name}");

                if (DialogueManager.Instance == null)
                {
                    Debug.LogError("[HandleInteraction] DialogueManager.Instance is NULL — cannot start dialogue!");
                }
                else
                {
                    Debug.Log("[HandleInteraction] Calling DialogueManager.Instance.StartConversation()");
                    bothTrigger.Interact();
                }
                HidePrompt();
                return;
            }

            // ------------------ 未命中任何類型 ------------------
            Debug.LogWarning($"[HandleInteraction] Hit {hitObject.name}, but it has no recognized Interactable component!");
        }
        else
        {
            Debug.Log("No interactable object detected upon interaction press.");
        }
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

        if (CurrentTarget == null)
        {
            Debug.LogWarning("[PlayerInteraction] 沒有交互目標，無法使用物品");
            CloseInventoryAndExitInteraction();
            return;
        }

        bool success = CurrentTarget.UseItem(item); // 呼叫 InteractableObject（交互點） 的邏輯

        if (success)
        {
            // 使用成功才消耗物品
            InventoryManager.Instance.RemoveItem(item);
            Debug.Log($"[PlayerInteraction] {item.itemName} 使用成功並消耗");
        }
        else
        {
            Debug.Log($"[PlayerInteraction] {item.itemName} 使用失敗，未消耗");
        }

        // 無論成功或失敗都關閉背包 + 退出交互模式
        CloseInventoryAndExitInteraction();
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
        CurrentTarget = null;

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

        // 2. 在 cornerAnchor 顯示模型
        if (voiceItem.voiceItem != null && cornerAnchor != null)
        {
            // 清理舊的模型 (保險)
            if (currentVoiceItemModel != null) Destroy(currentVoiceItemModel);

            currentVoiceItemModel = Instantiate(voiceItem.voiceItem, cornerAnchor);
            currentVoiceItemModel.transform.localPosition = Vector3.zero;
            currentVoiceItemModel.transform.localRotation = Quaternion.identity;
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
        VoiceItemManager.Instance.MarkItemAsUsed(voiceItem);

        // [修改] 確保花屏特效停止
        if (glitchController != null)
        {
            glitchController.StopGlitch();
        }
    }

    /// <summary>
    /// [新] 拾取聲音物品時觸發一次花屏
    /// </summary>
    private IEnumerator PlayGlitchEffectOnce()
    {
        if (glitchController == null) yield break;

        Debug.Log("播放花屏特效 (1秒)");
        glitchController.PlayOneShotGlitch(); // <--- 呼叫新方法

        yield return new WaitForSeconds(1.0f);

        glitchController.StopGlitch(); // <--- 呼叫新方法
        Debug.Log("花屏特效結束");
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