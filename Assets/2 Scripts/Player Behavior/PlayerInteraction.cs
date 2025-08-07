using UnityEngine;
using UnityEngine.InputSystem;
using TMPro; // 引用 TextMeshPro 的命名空間

public class PlayerInteraction : MonoBehaviour
{
    [Header("互動設定")]
    [SerializeField]
    [Tooltip("互動距離")]
    private float interactionRange = 3f;

    [SerializeField]
    [Tooltip("可互動圖層")]
    private LayerMask interactionLayer;

    [Header("UI 提示")]
    [Tooltip("顯示拾取提示的UI文字元件")]
    // 將變數類型從 Text 改為 TextMeshProUGUI
    public TextMeshProUGUI pickupPromptText; 

    [Header("Debug")]
    [SerializeField]
    private bool showDebugRay = true;

    //private PlayerControls controls;
    private UIInputManager inputManager;
    private InputAction interactionAction;
    private Camera playerCamera;
    
    private GameObject currentInteractableObject = null;

    void Start()
    {
        CursorManager.EnterGameplayMode();
    }

    private void Awake()
    {
        inputManager = FindObjectOfType<UIInputManager>();
        if (inputManager == null)
        {
            Debug.LogError("找不到 UIInputManager 實例！請確認場景中存在一個。", this);
            return;
        }

        playerCamera = Camera.main;

        // --- 修正點 3: 從 UIInputManager 取得共用的 InputActionAsset ---
        var playerMap = inputManager.playerControls.FindActionMap("Player");
        interactionAction = playerMap.FindAction("Interaction");
        
        // --- 修正點 4: 將事件訂閱從 OnEnable/OnDisable 移動到 Awake 或 Start ---
        // 這是因為 Action Map 的啟用和禁用將由 UIInputManager 負責
        interactionAction.performed += HandleInteraction;
    }

    private void OnDestroy()
    {
        // 在腳本銷毀時取消訂閱，防止內存洩漏
        if (interactionAction != null)
        {
            interactionAction.performed -= HandleInteraction;
        }
    }

    //private void OnEnable()
    //{
    //controls.Player.Enable();
    //controls.Player.Interaction.performed += HandleInteraction;
    //}

    //private void OnDisable()
    //{
    //controls.Player.Disable();
    //controls.Player.Interaction.performed -= HandleInteraction;
    //}

    private void Update()
    {
        // 只有在非 UI 模式下才進行連續的射線檢測
        if (inputManager.IsInUIMode)
        {
            HidePrompt(); // 在 UI 模式下隱藏提示
            return;
        }
        ContinuousCheck();
    }
    
    private void ContinuousCheck()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        currentInteractableObject = null;

        if (showDebugRay)
        {
            Debug.DrawRay(ray.origin, ray.direction * interactionRange, Color.blue);
        }

        if (Physics.Raycast(ray, out RaycastHit hit, interactionRange, interactionLayer))
        {
            currentInteractableObject = hit.collider.gameObject;

            if (currentInteractableObject.TryGetComponent<InteractableItem>(out var item))
            {
                if (currentInteractableObject.TryGetComponent<IViewInteractable>(out var viewObj))
                {
                    if (!viewObj.IsInteractiveIn(ViewManager.Instance.CurrentView))
                    {
                        HidePrompt();
                        return;
                    }
                }

                if (pickupPromptText != null)
                {
                    pickupPromptText.text = $"按 [滑鼠左鍵] 拾取 {item.itemData.itemName}";
                    pickupPromptText.color = Color.white;
                    pickupPromptText.gameObject.SetActive(true);
                }
                return;
            }
            else if (currentInteractableObject.TryGetComponent<PasswordButton>(out var button))
            {
                if (currentInteractableObject.TryGetComponent<IViewInteractable>(out var viewObj))
                {
                    if (!viewObj.IsInteractiveIn(ViewManager.Instance.CurrentView))
                    {
                        pickupPromptText.text = "切換到陽視野才能操作密碼鎖";
                        pickupPromptText.color = Color.red;
                        pickupPromptText.gameObject.SetActive(true);
                        return;
                    }
                }

                if (pickupPromptText != null)
                {
                    pickupPromptText.text = "按 [滑鼠左鍵] 按下按鈕";
                    pickupPromptText.color = Color.white;
                    pickupPromptText.gameObject.SetActive(true);
                }
                return;
            }
        }

        HidePrompt();
    }

    private void HandleInteraction(InputAction.CallbackContext context)
    {
        // 確保在 UI 模式下不執行任何互動邏輯
        if (inputManager.IsInUIMode)
        {
            return;
        }
        
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactionRange, interactionLayer))
        {
            GameObject hitObject = hit.collider.gameObject;

            if (hitObject.TryGetComponent<IViewInteractable>(out var viewObj))
            {
                if (!viewObj.IsInteractiveIn(ViewManager.Instance.CurrentView))
                {
                    ShowViewModeError();
                    return;
                }
            }

            if (hitObject.TryGetComponent<InteractableItem>(out var itemToPickUp))
            {
                Debug.Log($"Picked up: {itemToPickUp.itemData.itemName}");
                InventoryManager.Instance.AddItem(itemToPickUp.itemData);
                Destroy(hitObject);
                HidePrompt();
            }
            else if (hitObject.TryGetComponent<PasswordButton>(out var button))
            {
                Debug.Log($"Pressed button: {button.Value}");
                button.OnPress();
                HidePrompt();
            }
        }
        else
        {
            Debug.Log("No interactable object detected upon interaction press.");
        }
    }

    private void ShowViewModeError() 
    {
        if(pickupPromptText != null) 
        {
            pickupPromptText.text = "切換到陽視野才能操作密碼鎖";
            pickupPromptText.color = Color.red;
            pickupPromptText.gameObject.SetActive(true);

            //2秒後隱藏提示
            Invoke(nameof(HidePrompt), 2f);
        }
    }

    private void HidePrompt() 
    {
        if (pickupPromptText != null)
        {
            pickupPromptText.gameObject.SetActive(false);
        }
    }

}