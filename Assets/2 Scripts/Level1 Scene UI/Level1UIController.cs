// Level1UIController.cs
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 管理 Level1 的 Action Map：Player。
/// 其他map還不確定，之前是寫分開管理。
/// 如果沒記錯，eating希望用esc搞定一切關面板，那麼遊戲設定面板不能放在 Player 以外的 Map，否則會功能衝突。
/// </summary>
public class Level1UIController : MonoBehaviour
{
    // ***** 新增 *****
    [Header("案件紀錄簿-物品 控制器引用")]
    [SerializeField] private InventoryPanelUIController _inventoryPanelController;

    [Header("背包panel: 背包-物品/死者/聲音/線索組合。目前共4個")]
    public GameObject[] mainPanels;
    [Header("第二層panel: 背包-物品 -> 物品模型預覽。")]
    public GameObject modelPreviewPanel;
    [Header("至高panel: 遊戲設定面板，目前只能在 Player Map 中打開該面板。")]
    public GameObject settingPanel;

    [Header("右下角的提示視野圖標")]
    public GameObject titleUI;
    [Header("準心")]
    public GameObject crossHair;


    public Vector2 MoveInput { get; private set; }  // 讀取移動的值
    //10/10新增
    public Vector2 LookInput { get; private set; } // 讀取相機的值
    public bool IsMouseDevice { get; private set; } // 用來判斷Look輸入是否來自滑鼠

    void Start()
    {
        // 遊戲開始，初始化為Player Map
        InputStackManager.Instance.Init(InputActionMaps._Player);

        //默認所有面板關閉
        for (int i=0 ; i < mainPanels.Length; i++) //背包panel
        {
            mainPanels[i].SetActive(false); 
        }
        modelPreviewPanel.SetActive(false); //物品模型預覽panel
        settingPanel.SetActive(false); //遊戲設定panel

        // ***** 新增 *****
        // 在遊戲開始時，訂閱“獲得案件紀錄簿”事件
        CaseRecordBook.OnCollected += EnableInventoryOpening;

        Debug.Log("初始化 [Level1UIController] 成功");
    }

    private void OnEnable()
    {
        // *** 關鍵修改: 移除 inputActions.Player.Enable(); ***
        // *** InputStackManager 會幫我們處理！我們只管註冊事件。***

        // 確保 InputActions 已經被 PlayerInputRegistrar 初始化
        if (InputProvider.InputActions == null)
        {
            Debug.LogError("Level1UIController: InputProvider.InputActions 尚未初始化！請檢查 Script Execution Order。");
            return;
        }

        // --- 註冊 Move 事件 ---
        InputProvider.InputActions.Player.Move.performed += OnMovePerformed;
        InputProvider.InputActions.Player.Move.canceled += OnMoveCanceled;
        // --- 註冊 Look 事件 ---
        InputProvider.InputActions.Player.Look.performed += OnLookPerformed;
        InputProvider.InputActions.Player.Look.canceled += OnLookCanceled;
        // --- 註冊 交互 事件 ---
        InputProvider.InputActions.Player.Interaction.performed += OnInteractionAction;
        // --- 註冊 切換陰陽視野 事件 ---
        InputProvider.InputActions.Player.View.performed += OnViewAction;
        // --- 註冊 打開遊戲設置 事件 ---
        InputProvider.InputActions.Player.OpenSetting.performed += OnOpenSettingAction;
    }

    private void OnDisable()
    {
        // *** 關鍵修改: 移除 inputActions.Player.Disable(); ***

        // 如果 InputActions 為 null (例如在遊戲關閉時)，就不執行取消註冊
        if (InputProvider.InputActions == null) return;

        // 關閉 Player Action Map
        // --- 取消註冊 Move 事件 ---
        InputProvider.InputActions.Player.Move.performed -= OnMovePerformed;
        InputProvider.InputActions.Player.Move.canceled -= OnMoveCanceled;

        // --- 取消註冊 Look 事件 ---
        InputProvider.InputActions.Player.Look.performed -= OnLookPerformed;
        InputProvider.InputActions.Player.Look.canceled -= OnLookCanceled;

        InputProvider.InputActions.Player.Interaction.performed -= OnInteractionAction;
        InputProvider.InputActions.Player.View.performed -= OnViewAction;
        InputProvider.InputActions.Player.OpenSetting.performed -= OnOpenSettingAction;
        InputProvider.InputActions.Player.OpenInventory.performed -= OnOpenInventoryAction;
    }

    private void OnDestroy()
    {
        // ***** 新增 *****
        // 在物件銷毀時，取消訂閱事件以防止記憶體洩漏
        CaseRecordBook.OnCollected -= EnableInventoryOpening;
    }

    private void OnMovePerformed(InputAction.CallbackContext context)
    {
        MoveInput = context.ReadValue<Vector2>();
    }

    private void OnMoveCanceled(InputAction.CallbackContext context)
    {
        MoveInput = Vector2.zero;
    }

    private void OnLookPerformed(InputAction.CallbackContext context)
    {
        LookInput = context.ReadValue<Vector2>();
        // 檢查輸入的裝置是否為滑鼠
        IsMouseDevice = context.control.device is Mouse;
    }

    private void OnLookCanceled(InputAction.CallbackContext context)
    {
        LookInput = Vector2.zero;
    }
    
    private void OnInteractionAction(InputAction.CallbackContext context) //和場景物件、交互點、人物交互
    {
        // 呼叫 PlayerInteraction 的方法
        if (PlayerInteraction.Instance != null)
        {
            PlayerInteraction.Instance.HandleInteraction();
        }
        else
        {
            Debug.LogWarning("[Level1UIController] PlayerInteraction.Instance 尚未初始化！");
        }
    }
    private void OnViewAction(InputAction.CallbackContext context) //可以切換陰陽視野
    {
        // 呼叫 ViewManager 的方法
        if (ViewManager.Instance != null)
        {
            ViewManager.Instance.ToggleView();
        }
        else
        {
            Debug.LogWarning("[Level1UIController] ViewManager.Instance 尚未初始化！");
        }
    }
    private void OnOpenSettingAction(InputAction.CallbackContext context) //打開遊戲設定面板
    {
        settingPanel.SetActive(true);
        titleUI.SetActive(false);
        crossHair.SetActive(false);
        Debug.Log($"[{this.name}] 遊戲設置已打開。");

        // 將 UI map 推入棧，此時 Player map 會被自動禁用
        InputStackManager.Instance.PushMap(InputActionMaps._Setting);
    }

    /// <summary>
    /// 當 CaseRecordBook 觸發 OnCaseRecordBookCollected 事件時，此方法會被呼叫。
    /// </summary>
    private void EnableInventoryOpening()
    {
        Debug.Log("[Level1UIController] 收到通知，啟用背包功能！");

        // 在這裡才註冊 OpenInventory Action
        if (InputProvider.InputActions != null)
        {
            InputProvider.InputActions.Player.OpenInventory.performed += OnOpenInventoryAction;
        }

        // 因為這個事件只會觸發一次，我們可以在註冊後立即取消訂閱，保持程式碼乾淨
        CaseRecordBook.OnCollected -= EnableInventoryOpening;
    }

    private void OnOpenInventoryAction(InputAction.CallbackContext context) 
    {
        // 如果沒有獲得紀錄簿，就不能打開該面板
        // 改成用呼叫 InventoryPanelUIController腳本 裡的方法
        if (_inventoryPanelController != null)
        {
            _inventoryPanelController.OpenPanel(false); // false = 非使用物品模式
            Debug.Log($"[{this.name}] 已請求打開案件紀錄簿。");

            // 將 Inventory map 推入棧，此時 Player map 會被自動禁用
            // ***** 移除：將這個呼叫移到 OpenPanel() 內部 *****
            //InputStackManager.Instance.PushMap(InputActionMaps._Inventory);
        }
        else
        {
            Debug.LogError("InventoryPanelController 的引用尚未設定！");
        }
    }
}
