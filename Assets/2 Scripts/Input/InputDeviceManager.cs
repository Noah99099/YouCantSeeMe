using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement; // 引用場景管理
using System;

public class InputDeviceManager : MonoBehaviour
{
    public static InputDeviceManager Instance { get; private set; }
    public enum InputType { KeyboardMouse, Gamepad }
    public InputType CurrentInputType { get; private set; } = InputType.KeyboardMouse;
    public event Action<InputType> OnInputTypeChanged; //新增事件

    private PlayerInput playerInput; // 用於事件驅動模式

    // 防止頻繁切換的計時器
    private float lastSwitchTime;
    private const float switchCooldown = 0.5f;

    #region --- 初始化與場景管理 ---
    private void Awake()
    {
        Debug.Log("InputDeviceManager Awake 被調用");

        if (Instance != null && Instance != this)
        {
            Debug.Log("發現重複的 InputDeviceManager，銷毀新實例");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 始終以鍵鼠模式開始，即使連接了手柄
        //SwitchInput(InputType.KeyboardMouse);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 每次載入場景都嘗試尋找 PlayerInput
        TryToFindAndSubscribePlayerInput();
    }
    #endregion

    private void OnDestroy()
    {
        Debug.Log("InputDeviceManager 被銷毀");
        if (Instance == this)
        {
            Instance = null;
            Debug.Log("InputDeviceManager Instance 已清空");
        }
    }

    #region --- 核心邏輯切換 ---
    private void Update()
    {
        // ***** 核心切換邏輯 *****
        // 如果 playerInput 不是 null，代表我們處於高效的事件模式，
        // Update 函式不應該做任何事，直接返回。
        if (playerInput != null)
        {
            return;
        }

        // --- 如果 playerInput 是 null，則執行下面的降級輪詢模式 ---
        if (Time.unscaledTime - lastSwitchTime < switchCooldown)
            return;

        // 鍵盤滑鼠輸入偵測
        if (Keyboard.current != null && (Keyboard.current.anyKey.wasPressedThisFrame))
        {
            SetInputType(InputType.KeyboardMouse);
            return;
        }
        if (Mouse.current != null && (Mouse.current.delta.ReadValue().sqrMagnitude > 0.01f || Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame))
        {
            SetInputType(InputType.KeyboardMouse);
            return;
        }

        // 手柄輸入偵測
        if (HasGamepadInput())
        {
            SetInputType(InputType.Gamepad);
        }
    }
 
    private void SetInputType(InputType newType) // 統一的狀態設定方法，無論是事件還是輪詢都調用它
    {
        if (CurrentInputType != newType)
        {
            CurrentInputType = newType;
            lastSwitchTime = Time.unscaledTime;
            Debug.Log($"輸入裝置切換為: {CurrentInputType} (模式: {(playerInput == null ? "輪詢" : "事件")})");
            OnInputTypeChanged?.Invoke(newType);
        }
    }
    #endregion

    #region --- 事件驅動模式 (方法二) ---
    private void TryToFindAndSubscribePlayerInput()
    {
        // 如果之前有綁定，先取消訂閱
        if (playerInput != null)
        {
            playerInput.onControlsChanged -= OnControlsChanged;
        }

        playerInput = FindObjectOfType<PlayerInput>();

        if (playerInput != null)
        {
            Debug.Log("模式切換: 找到 PlayerInput，進入 [事件驅動] 模式。");
            playerInput.onControlsChanged += OnControlsChanged;
            OnControlsChanged(playerInput); // 立即同步一次初始狀態
        }
        else
        {
            Debug.Log("模式切換: 未找到 PlayerInput，進入 [輪詢] 模式。");
        }
    }

    private void OnControlsChanged(PlayerInput input)
    {
        string scheme = input.currentControlScheme;
        if (scheme.Contains("Keyboard")) // 稍微泛化，避免名稱寫死
        {
            SetInputType(InputType.KeyboardMouse);
        }
        else if (scheme.Contains("Gamepad"))
        {
            SetInputType(InputType.Gamepad);
        }
    }

    #endregion

    #region --- 輪詢模式輔助方法 (來自您的原始腳本) ---
    private bool HasGamepadInput()
    {
        if (Gamepad.current == null) return false;

        // 遍歷所有按鈕，只要有任何一個被按下就返回 true
        foreach (var control in Gamepad.current.allControls)
        {
            if (control is UnityEngine.InputSystem.Controls.ButtonControl button && button.wasPressedThisFrame)
            {
                return true;
            }
        }

        // 檢查搖桿和扳機鍵的閾值
        if (Gamepad.current.leftStick.ReadValue().magnitude > 0.2f ||
            Gamepad.current.rightStick.ReadValue().magnitude > 0.2f ||
            Gamepad.current.leftTrigger.ReadValue() > 0.2f ||
            Gamepad.current.rightTrigger.ReadValue() > 0.2f)
        {
            return true;
        }

        return false;
    }
    #endregion

    //private void SwitchInput(InputType newType)
    //{
    //    // 恢復條件檢查，避免頻繁觸發
    //    if (CurrentInputType != newType)
    //    {
    //        CurrentInputType = newType;
    //        lastSwitchTime = Time.unscaledTime;
    //        Debug.Log($"輸入裝置切換為: {CurrentInputType}");

    //        // 觸發事件
    //        OnInputTypeChanged?.Invoke(newType);
    //    }
    //    else
    //    {
    //        // 即使類型相同，也不觸發事件，避免頻繁觸發
    //        Debug.Log($"輸入裝置保持為: {CurrentInputType}，不觸發事件");
    //    }
    //}
}
