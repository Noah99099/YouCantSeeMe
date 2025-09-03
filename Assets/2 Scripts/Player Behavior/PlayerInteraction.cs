using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class PlayerInteraction : MonoBehaviour
{
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

    private void Awake()
    {
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

        if (showDebugRay)
        {
            Debug.DrawRay(ray.origin, ray.direction * interactionRange, Color.blue);
        }

        if (Physics.Raycast(ray, out RaycastHit hit, interactionRange, interactionLayer))
        {
            currentInteractableObject = hit.collider.gameObject;

            // ... (此處邏輯不變，保持原樣即可)
            if (currentInteractableObject.TryGetComponent<InteractableItem>(out var item))
            {
                if (pickupPromptText != null)
                {
                    pickupPromptText.text = $"按 [滑鼠左鍵] 拾取 {item.itemData.itemName}";
                    pickupPromptText.gameObject.SetActive(true);
                }
                return;
            }
            else if (currentInteractableObject.TryGetComponent<PasswordButton>(out var button))
            {
                if (pickupPromptText != null)
                {
                    pickupPromptText.text = "按 [滑鼠左鍵] 按下按鈕";
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

    private void HidePrompt() 
    {
        if (pickupPromptText != null && pickupPromptText.gameObject.activeSelf)
        {
            pickupPromptText.gameObject.SetActive(false);
        }
    }
}