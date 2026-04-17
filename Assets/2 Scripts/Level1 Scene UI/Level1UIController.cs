// Level1UIController.cs
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 管理 Level1 的 Action Map：Player。
/// 其他map還不確定，之前是寫分開管理。
/// 如果沒記錯，eating希望用esc搞定一切關面板，那麼遊戲設定面板不能放在 Player 以外的 Map，否則會功能衝突。
/// </summary>
[DefaultExecutionOrder(50)]
public class Level1UIController : MonoBehaviour
{
    // ***** 新增 *****
    [Header("案件紀錄簿-物品 控制器引用")]
    [SerializeField] private InventoryPanelUIController _inventoryPanelController;
    [Header("平面圖 控制器引用")]
    [SerializeField] private MapPanelUIController _mapPanelUIController;

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
    public Vector2 LookInput { get; private set; } // 讀取相機的值
    public bool IsMouseDevice { get; private set; } // 用來判斷Look輸入是否來自滑鼠

    // 新增：一開始不能使用切換視野
    private bool canUseViewAction = false;
    // ***** 需求修改: 新增 *****
    private bool _sceneTransitionFinished = false;
    private bool _playerMapInitialized = false; // 確保 Init 只執行一次

    void Start()
    {
        // ***** 需求修改: 移除 *****
        // 遊戲開始，不再強行初始化為Player Map
        // InputStackManager.Instance.Init(InputActionMaps._Player); 
        // ***** 需求修改: 結束 *****

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
        // 在遊戲開始時，訂閱“獲得平面圖”事件
        Map.GetMap += EnableMapOpening;

        PrepareToYinView.CanChangeView += EnableViewAction; // 新增：接收允許切換視野事件

        Debug.Log("初始化 [Level1UIController] 成功");

        // ***** 需求修改: 新增 *****
        // 檢查是否是從編輯器直接啟動 (沒有 SceneLoader)
        if (SceneLoader.Instance == null)
        {
            // 如果沒有 SceneLoader (例如在編輯器中啟動)
            Debug.LogWarning("[Level1UIController] 未偵測到 SceneLoader，視為轉場已完成。");
            _sceneTransitionFinished = true;
            // 立即嘗試初始化
            TryInitializePlayerMap();
        }
    }

    private void OnEnable()
    {
        // ***** 解決方案: 先移除，再添加 *****
        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.OnSceneTransitionComplete -= HandleSceneTransitionComplete; // 先移除
            SceneLoader.Instance.OnSceneTransitionComplete += HandleSceneTransitionComplete; // 再添加

            // ***** 新增 *****
            SceneLoader.Instance.OnSceneTransitionStart -= HandleSceneTransitionStart; // 先移除
            SceneLoader.Instance.OnSceneTransitionStart += HandleSceneTransitionStart; // 再添加
        }

        if (InputProvider.InputActions == null)
        {
            Debug.LogError("Level1UIController: InputProvider.InputActions 尚未初始化！");
            return;
        }

        // ***** 解決方案: 對所有 Input Actions 應用 "先移除再添加" 模式 *****

        // --- 註冊 Move 事件 ---
        InputProvider.InputActions.Player.Move.performed -= OnMovePerformed;
        InputProvider.InputActions.Player.Move.performed += OnMovePerformed;
        InputProvider.InputActions.Player.Move.canceled -= OnMoveCanceled;
        InputProvider.InputActions.Player.Move.canceled += OnMoveCanceled;

        // --- 註冊 Look 事件 ---
        InputProvider.InputActions.Player.Look.performed -= OnLookPerformed;
        InputProvider.InputActions.Player.Look.performed += OnLookPerformed;
        InputProvider.InputActions.Player.Look.canceled -= OnLookCanceled;
        InputProvider.InputActions.Player.Look.canceled += OnLookCanceled;

        // --- 註冊 交互 事件 ---
        InputProvider.InputActions.Player.Interaction.performed -= OnInteractionAction;
        InputProvider.InputActions.Player.Interaction.performed += OnInteractionAction;
        // --- 註冊 切換陰陽視野 事件 ---
        InputProvider.InputActions.Player.View.performed -= OnViewAction;
        InputProvider.InputActions.Player.View.performed += OnViewAction;
        // --- 註冊 打開遊戲設置 事件 ---
        InputProvider.InputActions.Player.OpenSetting.performed -= OnOpenSettingAction;
        InputProvider.InputActions.Player.OpenSetting.performed += OnOpenSettingAction;
    }

    private void OnDisable()
    {
        // ***** 需求修改: 新增 *****
        // 取消訂閱 SceneLoader 事件
        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.OnSceneTransitionComplete -= HandleSceneTransitionComplete;
            SceneLoader.Instance.OnSceneTransitionStart -= HandleSceneTransitionStart; // <--- 新增
        }
        // ***** 需求修改: 結束 *****

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
        InputProvider.InputActions.Player.OpenMap.performed -= OnOpenMapAction;
    }

    private void OnDestroy()
    {
        // ***** 新增 *****
        // 在物件銷毀時，取消訂閱事件以防止記憶體洩漏
        CaseRecordBook.OnCollected -= EnableInventoryOpening;
        Map.GetMap -= EnableMapOpening;
        PrepareToYinView.CanChangeView -= EnableViewAction;

        // ***** 需求修改: 新增 (雖然 OnDisable 應該已經處理了，但多一層保險) *****
        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.OnSceneTransitionComplete -= HandleSceneTransitionComplete;
        }
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
        // 新增：一開始禁止使用
        if (!canUseViewAction)
        {
            Debug.Log("[Level1UIController] 尚未解鎖切換視野功能！");
            return;
        }

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
        crossHair.SetActive(false);
        titleUI.SetActive(false);
        Debug.Log($"[{this.name}] 遊戲設置已打開。");

        // 將 UI map 推入棧，此時 Player map 會被自動禁用
        InputStackManager.Instance.PushMap(InputActionMaps._Setting);
    }

    #region === 案件紀錄簿和平面圖 ===
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

    /// <summary>
    /// 當 Map 觸發事件時，此方法會被呼叫。
    /// </summary>
    private void EnableMapOpening()
    {
        Debug.Log("[Level1UIController] 收到通知，啟用平面圖功能！");

        // 在這裡才註冊 OpenInventory Action
        if (InputProvider.InputActions != null)
        {
            InputProvider.InputActions.Player.OpenMap.performed += OnOpenMapAction;
        }

        // 因為這個事件只會觸發一次，我們可以在註冊後立即取消訂閱，保持程式碼乾淨
        Map.GetMap -= EnableMapOpening;
    }

    private void OnOpenMapAction(InputAction.CallbackContext context)
    {
        // 如果沒有獲得平面圖，就不能打開該面板
        // 改成用呼叫 MapPanelUIController 裡的方法
        if (_mapPanelUIController != null && _mapPanelUIController.gameObject != null)
        {
            _mapPanelUIController.OpenMap(); // 打開平面圖
            Debug.Log($"[{this.name}] 已請求打開平面圖。");
            
            // 透過單例呼叫 BigMapController 的置中方法
            if (BigMapController.Instance != null)
            {
                BigMapController.Instance.CenterMapOnPlayer();
            }
            else
            {
                Debug.LogWarning("找不到 BigMapController 單例，無法執行地圖置中！");
            }
            // ==================================
        }
        else
        {
            Debug.LogError("MapPanelUIController 的引用尚未設定！");
        }
    }
    #endregion

    // 新增：啟用切換視野功能的方法
    private void EnableViewAction()
    {
        Debug.Log("[Level1UIController] 收到通知，可以使用切換視野功能！");
        canUseViewAction = true;
        PrepareToYinView.CanChangeView -= EnableViewAction; // 僅需觸發一次
    }

    // ***** 需求修改: 以下為新增的完整邏輯 *****
    /// <summary>
    /// 處理來自 SceneLoader 的場景轉場「開始」事件
    /// </summary>
    private void HandleSceneTransitionStart()
    {
        // 重置這兩個標記，以便為下一次「轉場完成」做準備
        _sceneTransitionFinished = false;
        _playerMapInitialized = false;
    }

    /// <summary>
    /// 處理來自 SceneLoader 的場景轉場完成事件。
    /// </summary>
    private void HandleSceneTransitionComplete()
    {
        _sceneTransitionFinished = true;
        TryInitializePlayerMap();
    }

    /// <summary>
    /// 嘗試初始化 Player Map (會被 Start() 或 HandleSceneTransitionComplete() 呼叫)
    /// </summary>
    private void TryInitializePlayerMap()
    {
        // 必須轉場完成，且尚未初始化過
        if (!_sceneTransitionFinished || _playerMapInitialized)
        {
            return;
        }
        _playerMapInitialized = true; // 標記為已初始化，防止重複執行

        // 我們使用 Coroutine 並等待一幀 (yield return null)
        // 這是為了確保 SceneDialogueController (ExecutionOrder 10)
        // 的 Start() 或 HandleSceneTransitionComplete() 已經被執行，
        // 並且有機會 Push "Dialogue" Map (如果有的話)。
        StartCoroutine(InitializePlayerMapAfterDelay());
    }

    /// <summary>
    /// 延遲一幀後檢查並初始化 Player Map
    /// </summary>
    private System.Collections.IEnumerator InitializePlayerMapAfterDelay()
    {
        // 等待一幀，讓其他 [DefaultExecutionOrder(10)] 的腳本 (SceneDialogueController) 先執行完畢
        yield return null;

        if (InputStackManager.Instance == null)
        {
            Debug.LogError("[Level1UIController] InputStackManager.Instance 為 null，無法初始化 Player Map！");
            yield break;
        }

        // ***** 需求修改: 檢查靜態標記 *****
        if (SceneDialogueController.IsSceneDialoguePlaying)
        {
            // 棧是 [Loading, Dialogue]。我們什麼都不做。
            // SceneDialogueController 會在對話結束後呼叫 Init(Player)
            Debug.Log("[Level1UIController] 偵測到場景對話正在播放。Player Map 將在對話結束後初始化。");
        }
        else
        {
            // 棧是 [Loading]。我們必須手動切換到 Player
            Debug.Log("[Level1UIController] 未偵測到場景對話。立即初始化 Player Map。");
            InputStackManager.Instance.Init(InputActionMaps._Player);
        }
    }
}
