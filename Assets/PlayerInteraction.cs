using UnityEngine;
using UnityEngine.InputSystem;
// 1. 引用 TextMeshPro 的命名空間
using TMPro; 

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
    // 2. 將變數類型從 Text 改為 TextMeshProUGUI
    public TextMeshProUGUI pickupPromptText; 

    [Header("Debug")]
    [SerializeField]
    private bool showDebugRay = true;

    private PlayerControls controls;
    private Camera playerCamera;
    
    private GameObject currentInteractableObject = null; 

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
                if (pickupPromptText != null)
                {
                    pickupPromptText.text = $"按 [滑鼠左鍵] 拾取 {item.itemData.itemName}";
                    pickupPromptText.gameObject.SetActive(true);
                }
                return;
            }
        }
        
        if (pickupPromptText != null && pickupPromptText.gameObject.activeSelf)
        {
            pickupPromptText.gameObject.SetActive(false);
        }
    }

        private void HandleInteraction(InputAction.CallbackContext context)
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactionRange, interactionLayer))
        {
            GameObject hitObject = hit.collider.gameObject;

            if (hitObject.TryGetComponent<InteractableItem>(out var itemToPickUp))
            {
                Debug.Log($"Picked up: {itemToPickUp.itemData.itemName}");
                InventoryManager.Instance.AddItem(itemToPickUp.itemData);
                Destroy(hitObject);
                if (pickupPromptText != null)
                {
                    pickupPromptText.gameObject.SetActive(false);
                }
            }
            else if (hitObject.TryGetComponent<PasswordButton>(out var button))
            {
                Debug.Log($"Pressed button: {button.Value}");
                PasswordLockManager.Instance?.HandleButtonPress(button);
            }
        }
        else
        {
            Debug.Log("No interactable object detected upon interaction press.");
        }
    }

}