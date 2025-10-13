using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement; // 引用場景管理
using System;

/// <summary>
/// 此腳本的職責：
/// 檢測當前是"鍵鼠模式"還是"手柄模式"。
/// </summary>
public class InputDeviceManager : MonoBehaviour
{
    public static InputDeviceManager Instance { get; private set; }
    public enum InputType { KeyboardMouse, Gamepad }
    public InputType CurrentInputType { get; private set; } = InputType.KeyboardMouse;

    // 當輸入設備類型改變時觸發的事件
    public event Action<InputType> OnInputTypeChanged;

    // 防止頻繁切換的計時器
    private float lastSwitchTime;
    private const float switchCooldown = 0.5f;

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

        // 遊戲啟動時，強制設為鍵鼠模式
        CurrentInputType = InputType.KeyboardMouse;
        Debug.Log("InputDeviceManager 初始化，預設為鍵鼠模式。");
    }

    private void Update()
    {
        // 如果還在冷卻時間內，則不進行偵測，防止模式快速來回切換
        if (Time.unscaledTime - lastSwitchTime < switchCooldown)
            return;

        // 優先偵測鍵鼠輸入
        if (HasKeyboardMouseInput())
        {
            // 如果當前不是鍵鼠模式，則切換過去
            SetInputType(InputType.KeyboardMouse);
        }
        // 否則，偵測手把輸入
        else if (HasGamepadInput())
        {
            // 如果當前不是手把模式，則切換過去
            SetInputType(InputType.Gamepad);
        }
    }

    private void OnDestroy()
    {
        Debug.Log("InputDeviceManager 被銷毀");
        if (Instance == this)
        {
            Instance = null;
            Debug.Log("InputDeviceManager Instance 已清空");
        }
    }
 
    private void SetInputType(InputType newType) // 統一的狀態設定方法，無論是事件還是輪詢都調用它
    {
        // 只有在輸入類型真正改變時，才執行後續邏輯
        if (CurrentInputType != newType)
        {
            CurrentInputType = newType;
            lastSwitchTime = Time.unscaledTime;
            Debug.Log($"輸入裝置切換為: {CurrentInputType}");
            // 觸發事件，通知所有訂閱者
            OnInputTypeChanged?.Invoke(newType);
        }
    }

    #region --- 輪詢模式輔助方法 ---
    /// <summary>
    /// 檢查是否有任何鍵盤或滑鼠的有效輸入。
    /// </summary>
    private bool HasKeyboardMouseInput()
    {
        // 檢查鍵盤是否有任何按鍵被按下
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
        {
            return true;
        }

        // 檢查滑鼠是否移動，或是否有按鍵被按下
        if (Mouse.current != null &&
            (Mouse.current.delta.ReadValue().sqrMagnitude > 0.1f || // 使用 sqrMagnitude 效率更高
             Mouse.current.leftButton.wasPressedThisFrame ||
             Mouse.current.rightButton.wasPressedThisFrame ||
             Mouse.current.middleButton.wasPressedThisFrame ||
             Mouse.current.scroll.ReadValue().sqrMagnitude > 0.1f))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// 檢查是否有任何手把的有效輸入。
    /// </summary>
    private bool HasGamepadInput()
    {
        if (Gamepad.current == null) return false;

        // 檢查搖桿和扳機鍵的閾值
        if (Gamepad.current.leftStick.ReadValue().magnitude > 0.2f ||
            Gamepad.current.rightStick.ReadValue().magnitude > 0.2f ||
            Gamepad.current.leftTrigger.ReadValue() > 0.2f ||
            Gamepad.current.rightTrigger.ReadValue() > 0.2f)
        {
            return true;
        }

        // 遍歷所有按鈕，只要有任何一個被按下就返回 true
        foreach (var control in Gamepad.current.allControls)
        {
            // 我們只關心 ButtonControl 類型的控件
            if (control is UnityEngine.InputSystem.Controls.ButtonControl button && button.wasPressedThisFrame)
            {
                // 有些手把的搖桿也被視為按鈕（按下時），但我們上面已經檢查過了，這裡可以忽略
                // 為了避免重複偵測，我們可以加上一個簡單的判斷
                if (control != Gamepad.current.leftStickButton && control != Gamepad.current.rightStickButton)
                {
                    return true;
                }
            }
        }

        return false;
    }
    #endregion
}
