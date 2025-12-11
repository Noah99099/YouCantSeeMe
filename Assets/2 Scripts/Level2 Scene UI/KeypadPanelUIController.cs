// KeypadPanelUIController.cs
using UnityEngine;
using UnityEngine.InputSystem;

public class KeypadPanelUIController : MonoBehaviour
{
    [SerializeField] private KeypadLock _keypadLock;

    private void Awake()
    {
        // 查找 KeypadLock 實例
        if (_keypadLock == null)
        {
            Debug.LogError("KeypadPanelUIController 找不到 KeypadLock!");
            enabled = false;
        }
    }

    private void OnEnable()
    {
        // 當 Keypad Map 被 InputStackManager 啟用時，此 UI 腳本的 OnEnable 被呼叫
        if (InputProvider.InputActions == null) return;

        // 訂閱 Keypad Map 中的 CloseKeypad Action (假設綁定為 ESC)
        InputProvider.InputActions.Keypad.CloseKeypad.performed += OnCloseKeypadAction;
    }

    private void OnDisable()
    {
        // 當 Keypad Map 被 PopMap 禁用時，此 UI 腳本的 OnDisable 被呼叫
        if (InputProvider.InputActions == null) return;

        InputProvider.InputActions.Keypad.CloseKeypad.performed -= OnCloseKeypadAction;
    }

    private void OnCloseKeypadAction(InputAction.CallbackContext context)
    {
        // 按下 ESC 鍵時，呼叫 KeypadLock 的退出方法
        _keypadLock.ExitInteractionState();
    }
}