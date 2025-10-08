using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// 管理 Level1 的 Action Map：Player。
/// 其他map還不確定，之前是寫分開管理。
/// 如果沒記錯，eating希望用esc搞定一切關面板，那麼遊戲設定面板不能放在 Player 以外的 Map，否則會功能衝突。
/// </summary>
public class Level1UIController : MonoBehaviour
{
    [Header("背包panel: 背包-物品/死者/聲音/線索組合。目前共4個")]
    public GameObject[] mainPanels;
    [Header("第二層panel: 背包-物品 -> 物品模型預覽。")]
    public GameObject modelPreviewPanel;
    [Header("至高panel: 遊戲設定面板，目前只能在 Player Map 中打開該面板。")]
    public GameObject settingPanel;

    private PlayerControls inputActions;
    private PlayerView playerView; //腳本
    public Vector2 MoveInput { get; private set; }  // 給 PlayerMovement 讀取移動的值
    private void Awake()
    {
        // 初始化 Input Actions，若未初始化，OnEnable中會報錯。
        inputActions = new PlayerControls();

        playerView = GetComponent<PlayerView>(); //獲取 PlayerView 腳本
    }

    void Start()
    {
        // 遊戲開始，初始化為Player Map
        InputStackManager.Instance.Init(InputActionMaps._Player);

        //默認所有面板關閉
        for(int i=0 ; i < mainPanels.Length; i++) //背包panel
        {
            mainPanels[i].SetActive(false); 
        }
        modelPreviewPanel.SetActive(false); //物品模型預覽panel
        settingPanel.SetActive(false); //遊戲設定panel

        Debug.Log("初始化 [Level1UIController] 成功");

        // 啟動時，根據當前模式立即設定一次焦點
        //SetFocusForCurrentDevice(InputDeviceManager.Instance.CurrentInputType);
    }

    private void OnEnable()
    {
        // 啟用Player Action Map
        inputActions.Player.Enable();
        //inputActions.Player.Look.performed += OnLookAction;
        inputActions.Player.Move.performed += OnMovePerformed;
        inputActions.Player.Move.canceled += OnMoveCanceled;
        inputActions.Player.Interaction.performed += OnInteractionAction;
        inputActions.Player.View.performed += OnViewAction;
        inputActions.Player.OpenSetting.performed += OnOpenSettingAction;
        //一開始沒有打開背包
    }
    private void OnDisable()
    {
        // 關閉 Player Action Map
        //inputActions.Player.Look.performed -= OnLookAction;
        inputActions.Player.Move.performed -= OnMovePerformed;
        inputActions.Player.Move.canceled -= OnMoveCanceled;
        inputActions.Player.Interaction.performed -= OnInteractionAction;
        inputActions.Player.View.performed -= OnViewAction;
        inputActions.Player.OpenSetting.performed -= OnOpenSettingAction;
        inputActions.Player.Disable();
    }

    private void Update()
    {
        //Debug.Log($"[Update] MoveInput={inputActions.Player.Move.ReadValue<Vector2>()}, ActionEnabled={inputActions.Player.Move.enabled}");

        // 每幀更新 MoveInput
        //MoveInput = inputActions.Player.Move.ReadValue<Vector2>();

        // --- Look 處理 ---
        if (inputActions.Player.Look != null && playerView != null)
        {
            Vector2 lookInput = inputActions.Player.Look.ReadValue<Vector2>();
            bool usingGamepad = inputActions.Player.Look.activeControl?.device is Gamepad;
            playerView.SetLookInput(lookInput, usingGamepad); // 使用 playerView 腳本的 SetLookInput方法
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
    }
}
