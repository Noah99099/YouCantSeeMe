using Unity.Burst.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-10)] // 確保此腳本優先初始化
public class UIInputManager : MonoBehaviour
{
    public static UIInputManager Instance { get; private set; }

    // 我們將使用由 Input Action Asset 生成的 C# 類別來管理所有控制
    // 這是最安全且最高效的方式
    public PlayerControls PlayerControls { get; private set; }

    // 用布林值來追蹤目前的遊戲狀態
    public bool IsInPlayerMode { get; private set; } = false;
    public bool IsInUIMode { get; private set; } = false;
    public bool IsInInventoryMode { get; private set; } = false;
    public bool IsInDialogueMode { get; private set; } = false;

    // 新增: 遊戲開始狀態
    public bool IsGameStarted { get; private set; } = false;

    // 新增: 輸入設備類型引用
    private InputDeviceManager inputDeviceManager;

    [Tooltip("提示按下視野按鈕才能開始遊戲")] public GameObject hintUI;

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
    }

    private void OnEnable()
    {
        // 在 OnEnable 中訂閱所有需要的事件
        PlayerControls.Dialogue.AdvanceDialogue.performed += OnAdvanceDialoguePerformed;
    }

    private void OnDisable()
    {
        // 在 OnDisable 中取消訂閱，防止記憶體洩漏
        PlayerControls.Dialogue.AdvanceDialogue.performed -= OnAdvanceDialoguePerformed;
    }
 
    void Start()
    {
        // 遊戲一開始，禁用所有操作，等待玩家按下開始按鈕
        DisableAllMaps();

        // 只啟用 StartGame 操作
        PlayerControls.Player.StartGame.Enable();

        // 設置初始游標狀態
        UpdateCursorState();

        hintUI.SetActive(true); //新增

        Debug.Log("[UIInputManager] 等待玩家按下開始按鈕");
    }

    private void DisableAllMaps()
    {
        // 透過 PlayerControls 實例來停用所有的 Action Map
        PlayerControls.Player.Disable();
        PlayerControls.UI.Disable();
        PlayerControls.Inventory.Disable();
        PlayerControls.Dialogue.Disable();
    }

    // 新增: 開始遊戲的方法
    public void StartGame()
    {
        if (IsGameStarted) return;

        IsGameStarted = true;

        // 先禁用所有操作，然後重新啟用 Player Action Map
        DisableAllMaps();
        PlayerControls.Player.Enable();

        SetModeFlags(isPlayer: true);
        UpdateCursorState();

        hintUI.SetActive(false); //新增

        Debug.Log("[UIInputManager] 遊戲開始，啟用玩家控制");
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
                if (inputDeviceManager.CurrentInputType == InputDeviceManager.InputType.KeyboardMouse)
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

    public void EnterUIMode()
    {
        if (IsInUIMode) return;
        DisableAllMaps();
        PlayerControls.UI.Enable();
        SetModeFlags(isUI: true);
        UpdateCursorState();
        Debug.Log("[UIInputManager] 遊戲模式切換為：UI 模式");
    }

    public void EnterGameplayMode()
    {
        if (IsInPlayerMode) return;
        DisableAllMaps();
        PlayerControls.Player.Enable();
        SetModeFlags(isPlayer: true);
        UpdateCursorState();
        Debug.Log("[UIInputManager] 遊戲模式切換為：Gameplay 模式");
    }

    public void EnterInventoryMode()
    {
        if (IsInInventoryMode) return;
        DisableAllMaps();
        PlayerControls.Inventory.Enable();
        SetModeFlags(isInventory: true);
        UpdateCursorState();
        Debug.Log("[UIInputManager] 遊戲模式切換為：Inventory（無滑鼠）模式");
    }

    public void EnterDialogueMode()
    {
        if (IsInDialogueMode) return;
        DisableAllMaps();
        PlayerControls.Dialogue.Enable();
        SetModeFlags(isDialogue: true);
        UpdateCursorState();
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

    // 輔助方法，用來集中設定模式旗標，讓程式碼更乾淨
    private void SetModeFlags(bool isPlayer = false, bool isUI = false, bool isInventory = false, bool isDialogue = false)
    {
        IsInPlayerMode = isPlayer;
        IsInUIMode = isUI;
        IsInInventoryMode = isInventory;
        IsInDialogueMode = isDialogue;
    }
}