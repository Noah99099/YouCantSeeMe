using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

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

    private UIInputManager inputManager;
    private InputAction interactionAction;
    private Camera playerCamera;
    
    private GameObject currentInteractableObject = null;
    private InteractableObject currentInteractable; // 用於存儲當前可交互物件 腳本

    private void Awake()
    {
        // 實現單例模式
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        inputManager = FindObjectOfType<UIInputManager>();
        if (inputManager == null)
        {
            Debug.LogError("找不到 UIInputManager 實例！請確認場景中存在一個。", this);
            this.enabled = false;
            return;
        }

        playerCamera = Camera.main;

        // 【核心修正】
        // 1. 使用大寫的 'PlayerControls'
        // 2. 直接存取 Player Action Map 和 Interaction Action，更簡潔安全
        interactionAction = inputManager.PlayerControls.Player.Interaction;
        interactionAction.performed += HandleInteraction;
    }
    
    void Start()
    {
        pickupPromptText.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (interactionAction != null)
        {
            interactionAction.performed -= HandleInteraction;
        }
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (!UIInputManager.Instance.IsGameStarted) return; //新增

        if (!inputManager.IsInPlayerMode)
        {
            HidePrompt();
            return;
        }
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
                    pickupPromptText.text = $"按 [滑鼠左鍵] 拾取 {item.itemData.itemName}";
                    pickupPromptText.gameObject.SetActive(true);
                }
                return;
            }
            else if (currentInteractableObject.TryGetComponent<PasswordButton>(out var button)) // 檢查是否是 PasswordButton（密碼鎖）
            {
                if (pickupPromptText != null)
                {
                    pickupPromptText.text = "按 [滑鼠左鍵] 按下按鈕";
                    pickupPromptText.gameObject.SetActive(true);
                }
                return;
            }
            else if (currentInteractableObject.TryGetComponent<InteractableObject>(out var interactable)) // 檢查是否是 InteractableObject（需物品的交互物件）
            {
                currentInteractable = interactable;
                if (pickupPromptText != null)
                {
                    pickupPromptText.text = $"按 [滑鼠左鍵] 與 {interactable.objectName} 交互";
                    pickupPromptText.gameObject.SetActive(true);
                }
                return;
            }
            else if (currentInteractableObject.TryGetComponent<InteractableVoice>(out var voice)) // 檢查是否是 InteractableVoice（需聲音物品的交互物件）
            {
                //currentInteractable = interactable;
                if (pickupPromptText != null)
                {
                    pickupPromptText.text = $"按 [滑鼠左鍵] 與 {voice.objectName} 交互";
                    pickupPromptText.gameObject.SetActive(true);
                }
                return;
            }
        }

        HidePrompt();
    }

    private void HandleInteraction(InputAction.CallbackContext context)
    {
        if (!inputManager.IsInPlayerMode)
        {
            return;
        }
        
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactionRange, interactionLayer))
        {
            GameObject hitObject = hit.collider.gameObject;

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
            else if (hitObject.TryGetComponent<PasswordButton>(out var button)) //密碼鎖
            {
                Debug.Log($"Pressed button: {button.Value}");
                button.OnPress();
                HidePrompt();
            }
            else if (hitObject.TryGetComponent<InteractableObject>(out var interactable)) //使用物件
            {
                Debug.Log($"Interacting with: {interactable.objectName}");
                // 設定交互目標
                CurrentTarget = interactable;
                // 打開背包（交互模式）
                if (InventoryUI.Instance != null)
                {
                    InventoryUI.Instance.ToggleInventory(true);
                    InventoryUI.Instance.isInteractionMode = true;
                }
                HidePrompt();
            }
        }
        else
        {
            Debug.Log("No interactable object detected upon interaction press.");
        }
    }

    #region ===== 使用物品 =====
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
        Debug.Log($"[PlayerInteraction] CloseInventoryAndExitInteraction start. CurrentTarget={(CurrentTarget != null ? CurrentTarget.name : "null")}, isInventoryVisible={InventoryUI.Instance.isInventoryVisible}");

        InventoryUI.Instance.CloseInventory();
        InventoryUI.Instance.isInteractionMode = false;

        // 清空選中物品
        InventoryUI.Instance.SetCurrentSelectedItem(null);

        // 清空交互目標
        CurrentTarget = null;

        // 最後再刷新 UI（此時面板已關閉，就不會觸發 modelPreview）
        InventoryUI.Instance.UpdateUI();

        Debug.Log($"[PlayerInteraction] CloseInventoryAndExitInteraction end. isInventoryVisible={InventoryUI.Instance.isInventoryVisible}, currentSelectedItem={(InventoryUI.Instance.CurrentSelectedItem != null ? InventoryUI.Instance.CurrentSelectedItem.itemName : "null")}");
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