using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-10)] //更早初始化此腳本
public class UIInputManager : MonoBehaviour
{
    // 建立一個公開的靜態 Instance 變數，讓其他腳本可以存取
    public static UIInputManager Instance { get; private set; }

    [Header("輸入資源")]
    public InputActionAsset playerControls;

    private const string PLAYER_ACTION_MAP_NAME = "Player";
    private const string UI_ACTION_MAP_NAME = "UI";
    private const string INVENTORY_ACTION_MAP_NAME = "Inventory";
    [Header("對話系統參考")]
    [Tooltip("請將場景中的 DialogueUI 物件拖曳到此處")]
    [SerializeField] private DialogueUI dialogueUI;


    private const string DIALOGUE_ACTION_MAP_NAME = "Dialogue"; // 對話 Action Map 的名稱

    private InputActionMap dialogueMap; // 對話 Action Map 的引用
    private InputAction advanceDialogueAction; // "推進對話" Action 的引用

    public bool IsInDialogueMode { get; private set; } = false;

    private InputActionMap playerMap;
    private InputActionMap uiMap;
    private InputActionMap inventoryMap;

    public bool IsInPlayerMode { get; private set; } = false;
    public bool IsInUIMode { get; private set; } = false;
    public bool IsInInventoryMode { get; private set; } = false;

    private void Awake()
    {
        // 實現單例模式
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"發現重複的 UIInputManager 實例，將銷毀 {gameObject.name}");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        Debug.Log("UIInputManager 實例化完成");

        // 如果你的輸入管理器需要在不同場景之間持續存在，可以取消註解下一行
        // DontDestroyOnLoad(gameObject);
    }

    private void OnDisable()
    {
        // 取消訂閱事件，避免記憶體洩漏
        if (advanceDialogueAction != null)
        {
            advanceDialogueAction.performed -= OnAdvanceDialogue;
        }
    }

    void Start()
    {
        if (playerControls == null)
        {
            Debug.LogError("[UIInputManager] Player Controls 未設定！請在 Inspector 中指派。", this);
            return;
        }

        // 取得 action map references
        playerMap = playerControls.FindActionMap(PLAYER_ACTION_MAP_NAME);
        uiMap = playerControls.FindActionMap(UI_ACTION_MAP_NAME);
        inventoryMap = playerControls.FindActionMap(INVENTORY_ACTION_MAP_NAME);
        dialogueMap = playerControls.FindActionMap(DIALOGUE_ACTION_MAP_NAME);

        // 取得 action reference
        advanceDialogueAction = dialogueMap.FindAction("AdvanceDialogue");
        
        // 【關鍵修正】在這裡訂閱事件，確保 advanceDialogueAction 已經被找到
        if (advanceDialogueAction != null)
        {
            advanceDialogueAction.performed += OnAdvanceDialogue;
        }
        else
        {
            Debug.LogError($"[UIInputManager] 在 '{DIALOGUE_ACTION_MAP_NAME}' 中找不到 Action: AdvanceDialogue");
        }

        // 檢查 map 是否存在
        if (playerMap == null) Debug.LogError($"[UIInputManager] 找不到 ActionMap: {PLAYER_ACTION_MAP_NAME}");
        if (uiMap == null) Debug.LogError($"[UIInputManager] 找不到 ActionMap: {UI_ACTION_MAP_NAME}");
        if (inventoryMap == null) Debug.LogError($"[UIInputManager] 找不到 ActionMap: {INVENTORY_ACTION_MAP_NAME}");
        if (dialogueMap == null) Debug.LogError($"[UIInputManager] 找不到 ActionMap: {DIALOGUE_ACTION_MAP_NAME}");

        // 初始狀態
        EnterGameplayMode(); // 直接呼叫 EnterGameplayMode 來設定初始狀態，更簡潔
    }

    private void DisableAllMaps()
    {
        playerMap?.Disable();
        uiMap?.Disable();
        inventoryMap?.Disable();
        dialogueMap?.Disable();
    }

    public void EnterUIMode()
    {
        if (IsInUIMode)
        {
            Debug.Log("[UIInputManager腳本] 已在 UI 模式，跳過切換");
            return;
        }

        DisableAllMaps();
        uiMap?.Enable();

        CursorManager.EnterUIMode();

        IsInUIMode = true;
        IsInPlayerMode = false;
        IsInInventoryMode = false;

        Debug.Log("[UIInputManager腳本] 遊戲模式切換為：UI 模式");
    }

    public void EnterGameplayMode()
    {
        if (IsInPlayerMode)
        {
            Debug.Log("[UIInputManager腳本] 已處於 Gameplay 模式，跳過切換");
            return;
        }

        DisableAllMaps();
        playerMap?.Enable();

        CursorManager.EnterGameplayMode();

        IsInPlayerMode = true;
        IsInUIMode = false;
        IsInInventoryMode = false;

        Debug.Log("[UIInputManager腳本] 遊戲模式切換為：Gameplay 模式");
    }

    public void EnterInventoryModeNoCursor()
    {
        if (IsInInventoryMode)
        {
            Debug.Log("[UIInputManager腳本] 已在 Inventory 模式，跳過切換");
            return;
        }

        DisableAllMaps();
        inventoryMap?.Enable();

        IsInInventoryMode = true;
        IsInUIMode = false;
        IsInPlayerMode = false;

        Debug.Log("[UIInputManager腳本] 遊戲模式切換為：Inventory（無滑鼠）模式");
    }

    public void EnterDialogueMode()
    {
        if (IsInDialogueMode)
        {
            Debug.Log("[UIInputManager腳本] 已在 Dialogue 模式，跳過切換");
            return;
        }

        DisableAllMaps();
        dialogueMap?.Enable();

        // 對話時通常也需要滑鼠，所以我們呼叫與 UI 模式相同的滑鼠管理器
        CursorManager.EnterUIMode();

        IsInDialogueMode = true;
        IsInUIMode = false;
        IsInPlayerMode = false;
        IsInInventoryMode = false;

        Debug.Log("[UIInputManager腳本] 遊戲模式切換為：Dialogue 模式");
    }
    
    private void OnAdvanceDialogue(InputAction.CallbackContext context)
    {
        // 只有在對話模式下才執行
        if (!IsInDialogueMode) return;

        if (dialogueUI != null && dialogueUI.gameObject.activeInHierarchy)
        {
            // 這就是魔法發生的地方：呼叫 DialogueUI 的方法來推進對話！
            dialogueUI.OnContinueClicked();
        }
    }
}