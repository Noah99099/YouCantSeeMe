using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-10)] // 確保此腳本優先初始化
public class UIInputManager : MonoBehaviour
{
    public static UIInputManager Instance { get; private set; }
    public PlayerControls PlayerControls { get; private set; }

    // 用布林值來追蹤目前的遊戲狀態
    public bool IsInPlayerMode { get; private set; } = false; //遊玩
    public bool IsInUIMode { get; private set; } = false; //主選單 或 菜單?
    public bool IsInInventoryMode { get; private set; } = false; //背包（不包含3D）
    public bool IsInModelPreviewMode { get; private set; } = false; //3D模型預覽
    public bool IsInDialogueMode { get; private set; } = false; //對話系統
    public bool IsGameStarted { get; private set; } = false; //遊戲開始狀態

    // 新增: 輸入設備類型引用
    private InputDeviceManager inputDeviceManager;
    // 添加对 InventoryInputToUI腳本 的引用
    private InventoryInputToUI inventoryInput;

    [Header("功能：全局輸入模式管理和Action Map切換")]
    [Tooltip("提示按下視野按鈕才能開始遊戲")] public GameObject hintUI; //畫面至中下的提示，不是右下的看無

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 在 Awake 中就建立 PlayerControls 的實例
        PlayerControls = new PlayerControls();

        // 獲取 InputDeviceManager 引用
        inputDeviceManager = FindObjectOfType<InputDeviceManager>();
        if (inputDeviceManager == null)
        {
            Debug.LogWarning("找不到 InputDeviceManager，將使用默認游標設置");
        }
        // 獲取 InventoryInputToUI 引用
        inventoryInput = FindObjectOfType<InventoryInputToUI>();
        if (inventoryInput == null)
        {
            Debug.LogWarning("找不到 InventoryInputToUI 組件");
        }
    }

    private void OnEnable()
    {
        // 在 OnEnable 中訂閱所有需要的事件
        PlayerControls.Dialogue.AdvanceDialogue.performed += OnAdvanceDialoguePerformed;

        // 新增：在這裡訂閱 Startup/StartGame 按鍵
        PlayerControls.Startup.StartGame.performed += OnStartupStartGamePerformed;
    }

    private void OnDisable()
    {
        // 在 OnDisable 中取消訂閱，防止記憶體洩漏
        PlayerControls.Dialogue.AdvanceDialogue.performed -= OnAdvanceDialoguePerformed;

        // 新增：取消訂閱
        PlayerControls.Startup.StartGame.performed -= OnStartupStartGamePerformed;
    }
 
    void Start()
    {
        // 遊戲一開始，禁用所有操作，等待玩家按下切換視野按鈕（左shift、R1）
        DisableAllMaps();

        // 啟用 Startup Map（只有 StartGame 可用）
        PlayerControls.Startup.Enable();

        // 特別確保 Player Action Map 中的 OpenInventory 被禁用
        PlayerControls.Player.OpenInventory.Disable();

        // 設置初始游標狀態
        UpdateCursorState();

        hintUI.SetActive(true); //新增

        Debug.Log("[UIInputManager] 等待玩家按下開始按鈕");
    }

    #region ===== 關閉所有的 Action Map：目前有5個 =====
    private void DisableAllMaps()
    {
        // 透過 PlayerControls 實例來停用所有的 Action Map
        PlayerControls.Player.Disable();
        PlayerControls.UI.Disable();
        PlayerControls.Inventory.Disable();
        PlayerControls.Dialogue.Disable();
        PlayerControls.Startup.Disable();
    }
    #endregion

    #region ===== 開始遊戲 =====
    // 新增：啟動流程入口（由 Startup/StartGame 的 performed 事件觸發）
    private void OnStartupStartGamePerformed(InputAction.CallbackContext ctx)
    {
        //按下 切換視野按鈕（左shift、R1） 後執行的方法
        // 避免重複觸發
        if (IsGameStarted) return;
        StartGame();
    }

    // 新增: 開始遊戲的方法
    public void StartGame()
    {
        if (IsGameStarted) return;

        IsGameStarted = true;

        // 先禁用所有操作，然後重新啟用 Player Action Map
        DisableAllMaps();

        // 特別啟用 Player Action Map 中的 OpenInventory
        //PlayerControls.Player.OpenInventory.Enable();

        // 正式啟用 Player Map（之後 OpenInventory 就在這裡正常運作）
        EnterGameplayMode();

        SetModeFlags(isPlayer: true);

        hintUI.SetActive(false); //新增: 關閉提示按下按鈕

        // 新增: 通知 ViewManager 遊戲已開始
        if (ViewManager.Instance != null)
        {
            ViewManager.Instance.OnGameStarted();
        }

        Debug.Log("[UIInputManager] 遊戲開始：Startup → Player。現已啟用玩家控制（OpenInventory 第一時間可用）");
    }
    #endregion

    #region ===== 模式切換方法 =====
    // 沒補完的：3D預覽、組合物件線索
    public void EnterUIMode() //菜單模式
    {
        if (IsInUIMode) return;
        DisableAllMaps();
        PlayerControls.UI.Enable();
        SetModeFlags(isUI: true);
        UpdateCursorState();
        if (inventoryInput != null)
        {
            inventoryInput.BindOpenInventory(false); // UI 模式不允許開背包
        }
        Debug.Log("[UIInputManager] 遊戲模式切換為：UI 模式");
    }

    public void EnterGameplayMode() //玩家模式
    {
        if (IsInPlayerMode) return;
        DisableAllMaps();
        PlayerControls.Player.Enable();
        SetModeFlags(isPlayer: true);
        UpdateCursorState();
        // 集中管理：在玩家模式下，綁定 openInventory
        if (inventoryInput != null)
        {
            inventoryInput.BindOpenInventory(true);
        }
        Debug.Log("[UIInputManager] 遊戲模式切換為：Gameplay 模式");
    }

    public void EnterInventoryMode() //背包模式
    {
        if (IsInInventoryMode) return;
        DisableAllMaps();
        PlayerControls.Inventory.Enable();
        SetModeFlags(isInventory: true);
        UpdateCursorState();
        // 離開玩家模式，解除 openInventory
        if (inventoryInput != null)
        {
            inventoryInput.BindOpenInventory(false);
        }
        Debug.Log("[UIInputManager] 遊戲模式切換為：Inventory 模式");
    }

    public void EnterModelPreviewMode() //模型預覽模式
    {
        if (IsInModelPreviewMode) return;
        DisableAllMaps();
        PlayerControls.ModelPreview.Enable();
        SetModeFlags(isModelPreview: true);
        UpdateCursorState();
        if (inventoryInput != null)
        {
            inventoryInput.BindOpenInventory(false); // 模型預覽模式不允許開背包
        }
        Debug.Log("[UIInputManager] 遊戲模式切換為：ModelPreview 模式");
    }

    public void EnterDialogueMode() //對話模式
    {
        if (IsInDialogueMode) return;
        DisableAllMaps();
        PlayerControls.Dialogue.Enable();
        SetModeFlags(isDialogue: true);
        UpdateCursorState();
        if (inventoryInput != null)
        {
            inventoryInput.BindOpenInventory(false); // 對話模式不允許開背包
        }
        Debug.Log("[UIInputManager] 遊戲模式切換為：Dialogue 模式");
    }
    
    // 【核心修正】這裡的函式名稱必須與 OnEnable/OnDisable 中的訂閱名稱一致
    private void OnAdvanceDialoguePerformed(InputAction.CallbackContext context)
    {
        Debug.Log("<color=magenta>--- OnAdvanceDialogue: 點擊訊號已收到！ ---</color>");
        if (!IsInDialogueMode) return;

        if (DialogueUI.Instance != null && DialogueUI.Instance.gameObject.activeInHierarchy)
        {
            DialogueUI.Instance.OnContinueClicked();
        }
    }
    #endregion

    #region ===== 輔助 =====
    // 輔助方法，用來集中設定模式旗標，讓程式碼更乾淨
    private void SetModeFlags(bool isPlayer = false, bool isUI = false, bool isInventory = false, bool isDialogue = false, bool isModelPreview = false)
    {
        IsInPlayerMode = isPlayer;
        IsInUIMode = isUI;
        IsInInventoryMode = isInventory;
        IsInDialogueMode = isDialogue;
        IsInModelPreviewMode = isModelPreview;
    }

    // 新增: 更新游標狀態的方法
    private void UpdateCursorState()
    {
        if (inputDeviceManager != null)
        {
            // 根據輸入設備類型和當前模式設置游標
            if (IsInPlayerMode)
            {
                // 玩家模式下，無論使用什麼設備都隱藏游標
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else if (IsInInventoryMode)
            {
                // 背包模式下，根據設備類型設置游標
                if (inputDeviceManager.CurrentInputType == InputDeviceManager.InputType.KeyboardMouse) // 鍵鼠
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                else // 手柄
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }
            else if (IsInModelPreviewMode)
            {
                // 3D預覽模式下，根據設備類型設置游標
                if (inputDeviceManager.CurrentInputType == InputDeviceManager.InputType.KeyboardMouse) // 鍵鼠
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                else // 手柄
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }// 沒補完的：菜單、組合物件線索
            else // UI模式或對話模式
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
        else
        {
            // 沒有 InputDeviceManager 時的默認行為
            if (IsInPlayerMode)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        Debug.Log($"[UIInputManager] 游標狀態: LockState={Cursor.lockState}, Visible={Cursor.visible}");
    }
    #endregion
}