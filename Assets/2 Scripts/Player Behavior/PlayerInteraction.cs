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

    private PlayerControls controls;
    private Camera playerCamera;
    
    private GameObject currentInteractableObject = null;

    void Start()
    {
        CursorManager.EnterGameplayMode();
    }

    private void Awake()
    {
        controls = new PlayerControls();
        playerCamera = Camera.main;
    }

    private void OnEnable()
    {
        controls.Player.Enable();
        controls.Player.Interaction.performed += HandleInteraction;
    }

    private void OnDisable()
    {
        controls.Player.Disable();
        controls.Player.Interaction.performed -= HandleInteraction;
    }

    private void Update()
    {
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
                PasswordLockManager.Instance?.HandleButtonPress(button);
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