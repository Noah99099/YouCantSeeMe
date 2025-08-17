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
        // 遊戲一開始，預設進入玩家模式
        EnterGameplayMode();
    }

    private void DisableAllMaps()
    {
        // 透過 PlayerControls 實例來停用所有的 Action Map
        PlayerControls.Player.Disable();
        PlayerControls.UI.Disable();
        PlayerControls.Inventory.Disable();
        PlayerControls.Dialogue.Disable();
    }

    public void EnterUIMode()
    {
        if (IsInUIMode) return;
        DisableAllMaps();
        PlayerControls.UI.Enable();
        CursorManager.EnterUIMode();
        SetModeFlags(isUI: true);
        Debug.Log("[UIInputManager] 遊戲模式切換為：UI 模式");
    }

    public void EnterGameplayMode()
    {
        if (IsInPlayerMode) return;
        DisableAllMaps();
        PlayerControls.Player.Enable();
        CursorManager.EnterGameplayMode();
        SetModeFlags(isPlayer: true);
        Debug.Log("[UIInputManager] 遊戲模式切換為：Gameplay 模式");
    }

    public void EnterInventoryModeNoCursor()
    {
        if (IsInInventoryMode) return;
        DisableAllMaps();
        PlayerControls.Inventory.Enable();
        SetModeFlags(isInventory: true);
        Debug.Log("[UIInputManager] 遊戲模式切換為：Inventory（無滑鼠）模式");
    }

    public void EnterDialogueMode()
    {
        if (IsInDialogueMode) return;
        DisableAllMaps();
        PlayerControls.Dialogue.Enable();
        CursorManager.EnterUIMode();
        SetModeFlags(isDialogue: true);
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