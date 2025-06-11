using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems; // 加這行才能用 EventSystem

public class UIInputManager : MonoBehaviour
{
    private bool isInUIMode = false;

    void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (!isInUIMode)
            {
                CursorManager.EnterUIMode();
                isInUIMode = true;
            }
            else
            {
                CursorManager.EnterGameplayMode();
                isInUIMode = false;
            }
        }

        // ✅ 修正：只有當滑鼠沒點到 UI 元素時，才切回準心模式
        if (isInUIMode && Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (!EventSystem.current.IsPointerOverGameObject())
            {
                CursorManager.EnterGameplayMode();
                isInUIMode = false;
            }
        }
    }

    // 讓 Resume 按鈕也可以呼叫這個方法
    public void SetGameplayMode()
    {
        isInUIMode = false;
        CursorManager.EnterGameplayMode();
    }
}
