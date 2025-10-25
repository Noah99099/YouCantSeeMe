using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.EventSystems;
using System.Linq;

/// <summary>
/// UIInputManager 相關的內容不用
/// 要重寫，代替UIInputManager
/// 處理好了
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
    }
    
    void Start()
    {
        pickupPromptText.gameObject.SetActive(false);
    }

    private void Update()
    {
        ContinuousCheck();
    }
    
    private void ContinuousCheck()
    {
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
            else if (currentInteractableObject.TryGetComponent<InteractableObject>(out var interactable)) // 檢查是否是 InteractableObject（需物品的交互物件）
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
            else if (currentInteractableObject.TryGetComponent<InteractableVoice>(out var voice)) // 檢查是否是 InteractableVoice（需聲音物品的交互物件）
            {
                //currentInteractable = interactable;
                if (pickupPromptText != null)
                {
                    // 如果是手把模式，更換UI文本提示
                    if (InputDeviceManager.Instance.CurrentInputType == InputDeviceManager.InputType.Gamepad)
                    {
                        pickupPromptText.text = $"按 [叉] 與 {voice.objectName} 交互";
                    }
                    else //鍵鼠
                    {
                        pickupPromptText.text = $"按 [滑鼠左鍵] 與 {voice.objectName} 交互";
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
        }

        HidePrompt();
    }

    public void HandleInteraction()
    {
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
            else if (hitObject.TryGetComponent<InteractableVoice>(out var voiceItem)) //獲得物件，進聲音背包
            {
                Debug.Log($"Pressed button: {voiceItem.objectName}");
                voiceItem.Interact();
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

                    // 推入 Map 的邏輯最好也由 Level1UIController 或 Panel Controller 統一處理
                    // 這裡暫時假設 OpenPanel 內部還沒有 PushMap
                    // ***** 移除：將這個呼叫移到 OpenPanel() 內部 *****
                    //InputStackManager.Instance.PushMap(InputActionMaps._Inventory);
                }
                HidePrompt();
            }
            else if (hitObject.TryGetComponent<InteractionTrigger>(out var trigger)) // 交互後執行對話
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

            // ------------------ 未命中任何類型 ------------------
            Debug.LogWarning($"[HandleInteraction] Hit {hitObject.name}, but it has no recognized Interactable component!");
        }
        else
        {
            Debug.Log("No interactable object detected upon interaction press.");
        }
    }

    #region ===== 使用物品的方法 =====
    /// <summary>
    /// 從背包使用物品按鈕呼叫
    /// </summary>
    /// <param name="item">被使用的物品</param>
    // 從背包使用物品按鈕呼叫
    public void OnItemUsed(ItemData item)
    {
        if (CurrentTarget == null)
        {
            Debug.LogWarning("[PlayerInteraction] 沒有交互目標，無法使用物品");
            CloseInventoryAndExitInteraction();
            return;
        }

        bool success = CurrentTarget.UseItem(item); // 呼叫 InteractableObject 的邏輯

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
    private void CloseInventoryAndExitInteraction()
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

    private void HidePrompt() 
    {
        if (pickupPromptText != null && pickupPromptText.gameObject.activeSelf)
        {
            pickupPromptText.gameObject.SetActive(false);
        }
    }
}