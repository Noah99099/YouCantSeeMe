using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class InputDeviceManager : MonoBehaviour
{
    public static InputDeviceManager Instance { get; private set; }
    public enum InputType { KeyboardMouse, Gamepad }
    public InputType CurrentInputType { get; private set; } = InputType.KeyboardMouse;

    // === 新增事件 ===
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

        Debug.Log("InputDeviceManager 初始化完成，Instance 已設置");

        // 始終以鍵鼠模式開始，即使連接了手柄
        SwitchInput(InputType.KeyboardMouse);
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

    private void Update()
    {
        // 防止頻繁切換
        if (Time.unscaledTime - lastSwitchTime < switchCooldown)
            return;

        // ====== 鍵盤 W / S 偵測 ======
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.wasPressedThisFrame || Keyboard.current.sKey.wasPressedThisFrame)
            {
                SwitchInput(InputType.KeyboardMouse);
                return;
            }
        }

        // ====== 滑鼠偵測 ======
        if (Mouse.current != null)
        {
            if (Mouse.current.delta.ReadValue() != Vector2.zero ||
                Mouse.current.leftButton.wasPressedThisFrame ||
                Mouse.current.rightButton.wasPressedThisFrame ||
                Mouse.current.scroll.ReadValue().sqrMagnitude > 0.001f)
            {
                SwitchInput(InputType.KeyboardMouse);
                return;
            }
        }

        // ====== 手柄輸入檢測 ======
        if (HasGamepadInput())
        {
            SwitchInput(InputType.Gamepad);
        }
    }

    private bool HasGamepadInput()
    {
        // 閾值可視需求調整
        //const float stickThresholdSqr = 0.01f; // 相當於 magnitude ~0.1
        //const float triggerThreshold = 0.1f;

        foreach (var gamepad in Gamepad.all)
        {
            if (gamepad == null) continue;

            // 檢測 D-pad
            if (gamepad.dpad.up.wasPressedThisFrame ||
                gamepad.dpad.down.wasPressedThisFrame ||
                gamepad.dpad.left.wasPressedThisFrame ||
                gamepad.dpad.right.wasPressedThisFrame)
                return true;

            // 檢測主要按鈕
            if (gamepad.aButton.wasPressedThisFrame ||
                gamepad.bButton.wasPressedThisFrame ||
                gamepad.xButton.wasPressedThisFrame ||
                gamepad.yButton.wasPressedThisFrame)
                return true;

            // 檢測肩部按鈕和菜單按鈕
            if (gamepad.leftShoulder.wasPressedThisFrame ||
                gamepad.rightShoulder.wasPressedThisFrame ||
                gamepad.startButton.wasPressedThisFrame ||
                gamepad.selectButton.wasPressedThisFrame)
                return true;

            // 檢測搖桿移動（使用較高的閾值避免漂移）
            Vector2 leftStick = gamepad.leftStick.ReadValue();
            Vector2 rightStick = gamepad.rightStick.ReadValue();
            if (leftStick.magnitude > 0.2f || rightStick.magnitude > 0.2f)
                return true;

            // 檢測搖桿點擊
            if (gamepad.leftStickButton.wasPressedThisFrame ||
                gamepad.rightStickButton.wasPressedThisFrame)
                return true;

            // 檢測扳機鍵
            if (gamepad.leftTrigger.ReadValue() > 0.2f ||
                gamepad.rightTrigger.ReadValue() > 0.2f)
                return true;
        }

        return false;
    }

    private void SwitchInput(InputType newType)
    {
        // 恢復條件檢查，避免頻繁觸發
        if (CurrentInputType != newType)
        {
            CurrentInputType = newType;
            lastSwitchTime = Time.unscaledTime;
            Debug.Log($"輸入裝置切換為: {CurrentInputType}");

            // 觸發事件
            OnInputTypeChanged?.Invoke(newType);
        }
        else
        {
            // 即使類型相同，也不觸發事件，避免頻繁觸發
            Debug.Log($"輸入裝置保持為: {CurrentInputType}，不觸發事件");
        }
    }
}
