using UnityEngine;
using UnityEngine.InputSystem;

public class DialogueCharacterInteraction : MonoBehaviour
{
    [Header("角色互動設定")]
    [SerializeField] private DialogueRunner dialogueRunner;
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private LayerMask playerLayer = 1; // 玩家所在的層
    [SerializeField] private bool showInteractionPrompt = true;
    [SerializeField] private GameObject interactionPromptUI; // 顯示 "按 E 互動" 的 UI
    
    [Header("輸入設定")]
    [SerializeField] private InputActionAsset playerControls;
    
    private Transform _playerTransform;
    private InputAction _interactAction;
    private bool _playerInRange = false;
    private bool _isDialogueActive = false;
    
    private const string PLAYER_ACTION_MAP_NAME = "Player";
    private const string INTERACT_ACTION_NAME = "Interact";

    private void Awake()
    {
        SetupInputSystem();
        FindPlayer();
    }

    private void SetupInputSystem()
    {
        if (playerControls == null)
        {
            Debug.LogError("Player Controls 未設定！", this);
            return;
        }

        var playerActionMap = playerControls.FindActionMap(PLAYER_ACTION_MAP_NAME);
        if (playerActionMap == null)
        {
            Debug.LogError($"找不到 '{PLAYER_ACTION_MAP_NAME}' Action Map！", this);
            return;
        }

        _interactAction = playerActionMap.FindAction(INTERACT_ACTION_NAME);
        if (_interactAction == null)
        {
            Debug.LogError($"找不到 '{INTERACT_ACTION_NAME}' Action！", this);
            return;
        }

        _interactAction.performed += OnInteract;
    }

    private void FindPlayer()
    {
        // 尋找玩家物件（你可能需要根據你的項目調整這個邏輯）
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            _playerTransform = player.transform;
        }
        else
        {
            Debug.LogWarning("找不到標籤為 'Player' 的物件！", this);
        }
    }

    private void OnEnable()
    {
        _interactAction?.Enable();
        
        // 訂閱對話事件
        if (dialogueRunner != null)
        {
            dialogueRunner.OnDialogueStart.AddListener(OnDialogueStart);
            dialogueRunner.OnDialogueEnd.AddListener(OnDialogueEnd);
        }
    }

    private void OnDisable()
    {
        _interactAction?.Disable();
        
        // 取消訂閱對話事件
        if (dialogueRunner != null)
        {
            dialogueRunner.OnDialogueStart.RemoveListener(OnDialogueStart);
            dialogueRunner.OnDialogueEnd.RemoveListener(OnDialogueEnd);
        }
    }

    private void OnDestroy()
    {
        if (_interactAction != null)
        {
            _interactAction.performed -= OnInteract;
        }
    }

    private void Update()
    {
        CheckPlayerDistance();
    }

    private void CheckPlayerDistance()
    {
        if (_playerTransform == null) return;

        float distance = Vector3.Distance(transform.position, _playerTransform.position);
        bool wasInRange = _playerInRange;
        _playerInRange = distance <= interactionDistance;

        // 玩家進入範圍
        if (_playerInRange && !wasInRange)
        {
            ShowInteractionPrompt();
        }
        // 玩家離開範圍
        else if (!_playerInRange && wasInRange)
        {
            HideInteractionPrompt();
        }
    }

    private void OnInteract(InputAction.CallbackContext context)
    {
        if (_playerInRange && !_isDialogueActive && dialogueRunner != null)
        {
            dialogueRunner.StartDialogue();
        }
    }

    private void ShowInteractionPrompt()
    {
        if (showInteractionPrompt && interactionPromptUI != null && !_isDialogueActive)
        {
            interactionPromptUI.SetActive(true);
        }
    }

    private void HideInteractionPrompt()
    {
        if (interactionPromptUI != null)
        {
            interactionPromptUI.SetActive(false);
        }
    }

    private void OnDialogueStart()
    {
        _isDialogueActive = true;
        HideInteractionPrompt();
    }

    private void OnDialogueEnd()
    {
        _isDialogueActive = false;
        if (_playerInRange)
        {
            ShowInteractionPrompt();
        }
    }

    // 在場景視圖中顯示互動範圍
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
        
        // 在場景視圖中顯示角色名稱
        if (!string.IsNullOrEmpty(name))
        {
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2, name);
            #endif
        }
    }
}