using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-10)] //更早初始化此腳本
public class UIInputManager : MonoBehaviour
{
    // +++ 新增的程式碼 +++
    // 建立一個公開的靜態 Instance 變數，讓其他腳本可以存取
    public static UIInputManager Instance { get; private set; }

    [Header("輸入資源")]
    public InputActionAsset playerControls;

    private const string PLAYER_ACTION_MAP_NAME = "Player";
    private const string UI_ACTION_MAP_NAME = "UI";
    private const string INVENTORY_ACTION_MAP_NAME = "Inventory";


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
    // +++ 結束新增 +++

    void Start()
    {
        if (playerControls == null)
        {
            Debug.LogError("[UIInputManager] Player Controls 未設定！請在 Inspector 中指派。", this);
            return;
        }

        // 取得 action map references（只做一次）
        playerMap = playerControls.FindActionMap(PLAYER_ACTION_MAP_NAME);
        uiMap = playerControls.FindActionMap(UI_ACTION_MAP_NAME);
        inventoryMap = playerControls.FindActionMap(INVENTORY_ACTION_MAP_NAME);

        if (playerMap == null) Debug.LogError($"[UIInputManager] 找不到 ActionMap: {PLAYER_ACTION_MAP_NAME}");
        if (uiMap == null) Debug.LogError($"[UIInputManager] 找不到 ActionMap: {UI_ACTION_MAP_NAME}");
        if (inventoryMap == null) Debug.LogError($"[UIInputManager] 找不到 ActionMap: {INVENTORY_ACTION_MAP_NAME}");

        // 初始狀態 — 保證只有 Player Map 啟用
        DisableAllMaps();
        if (playerMap != null) playerMap.Enable();

        IsInPlayerMode = true;
        IsInUIMode = false;
        IsInInventoryMode = false;

        Debug.Log("[UIInputManager] 遊戲初始化完成，當前模式：Gameplay");
    }

    private void DisableAllMaps()
    {
        playerMap?.Disable();
        uiMap?.Disable();
        inventoryMap?.Disable();
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
}