using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class PlayerInteraction : MonoBehaviour
{
    public static PlayerInteraction Instance { get; private set; } // 添加單例模式

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
    private InteractableObject currentInteractable; // 用於存儲當前可交互物件

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
        //CursorManager.EnterGameplayMode();
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

            if (hitObject.TryGetComponent<InteractableItem>(out var itemToPickUp)) //獲得物件
            {
                Debug.Log($"Picked up: {itemToPickUp.itemData.itemName}");
                InventoryManager.Instance.AddItem(itemToPickUp.itemData);
                Destroy(hitObject);
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
                // 打開背包（交互模式）
                if (InventoryUI.Instance != null)
                {
                    InventoryUI.Instance.ToggleInventory(true);
                }
                HidePrompt();
            }
        }
        else
        {
            Debug.Log("No interactable object detected upon interaction press.");
        }
    }

    /// <summary>
    /// 當物品被使用時調用（從InventoryUI調用）
    /// </summary>
    /// <param name="item">被使用的物品</param>
    public void OnItemUsed(ItemData item)
    {
        if (currentInteractable != null)
        {
            // 檢查物品是否正確
            if (currentInteractable.requiredItem == item)
            {
                Debug.Log($"正確使用物品: {item.itemName} 於 {currentInteractable.objectName}");
                // 觸發正確使用物品的事件
                currentInteractable.OnCorrectItemUsed();
            }
            else
            {
                Debug.Log($"錯誤使用物品: {item.itemName} 於 {currentInteractable.objectName}");
                // 觸發錯誤使用物品的事件
                currentInteractable.OnWrongItemUsed();
            }
        }
        else
        {
            Debug.LogWarning("沒有可交互物件，無法使用物品");
        }
    }

    private void HidePrompt() 
    {
        if (pickupPromptText != null && pickupPromptText.gameObject.activeSelf)
        {
            pickupPromptText.gameObject.SetActive(false);
        }
    }
}