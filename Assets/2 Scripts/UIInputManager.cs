using UnityEngine;
using UnityEngine.InputSystem;

public class UIInputManager : MonoBehaviour
{
    [Header("輸入資源")]
    public InputActionAsset playerControls;

    private const string PLAYER_ACTION_MAP_NAME = "Player";
    private const string UI_ACTION_MAP_NAME = "UI";
    
    public bool IsInUIMode { get; private set; } = false;

    // +++ 新增的程式碼 +++
    // 建立一個公開的靜態 Instance 變數，讓其他腳本可以存取
    public static UIInputManager Instance { get; private set; }

    // +++ 新增的程式碼 +++
    private void Awake()
    {
        // 實現單例模式
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        // 如果你的輸入管理器需要在不同場景之間持續存在，可以取消註解下一行
        // DontDestroyOnLoad(gameObject);
    }
    // +++ 結束新增 +++

    void Start()
    {
        if (playerControls == null)
        {
            Debug.LogError("Player Controls 未設定！請在 Inspector 中指派。", this);
            return;
        }

        // 遊戲一開始，預設進入遊戲模式
        playerControls.FindActionMap(UI_ACTION_MAP_NAME).Disable();
        playerControls.FindActionMap(PLAYER_ACTION_MAP_NAME).Enable();
        CursorManager.EnterGameplayMode();
        IsInUIMode = false; // 確保初始狀態正確
    }

    public void EnterUIMode()
    {
        // if (IsInUIMode) return; // 這個判斷可以移除，讓呼叫更具確定性
        playerControls.FindActionMap(PLAYER_ACTION_MAP_NAME).Disable();
        playerControls.FindActionMap(UI_ACTION_MAP_NAME).Enable();
        CursorManager.EnterUIMode();
        IsInUIMode = true;
        Debug.Log("遊戲模式切換為：UI 模式");
    }

    public void EnterGameplayMode()
    {
        // if (!IsInUIMode) return; // 這個判斷可以移除，讓呼叫更具確定性
        playerControls.FindActionMap(UI_ACTION_MAP_NAME).Disable();
        playerControls.FindActionMap(PLAYER_ACTION_MAP_NAME).Enable();
        CursorManager.EnterGameplayMode();
        IsInUIMode = false;
        Debug.Log("遊戲模式切換為：Gameplay 模式");
    }
}