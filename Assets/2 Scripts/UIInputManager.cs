using UnityEngine;
using UnityEngine.InputSystem;

public class UIInputManager : MonoBehaviour
{
    public InputActionAsset playerControls;
    private const string PLAYER_ACTION_MAP_NAME = "Player";
    private const string UI_ACTION_MAP_NAME = "UI";
    
    // 將其設為公共屬性，以便其他腳本讀取
    public bool IsInUIMode { get; private set; } = false;

    void Start()
    {
        if (playerControls == null)
        {
            Debug.LogError("Player Controls 未設定！請在 Inspector 中指派。", this);
            return;
        }

        EnterGameplayMode();
    }

    public void EnterUIMode()
    {
        if (IsInUIMode) return;
        playerControls.FindActionMap(PLAYER_ACTION_MAP_NAME).Disable();
        playerControls.FindActionMap(UI_ACTION_MAP_NAME).Enable();
        CursorManager.EnterUIMode();
        IsInUIMode = true;
        Debug.Log("遊戲模式切換為：UI 模式");
    }

    public void EnterGameplayMode()
    {
        if (!IsInUIMode) return;
        playerControls.FindActionMap(UI_ACTION_MAP_NAME).Disable();
        playerControls.FindActionMap(PLAYER_ACTION_MAP_NAME).Enable();
        CursorManager.EnterGameplayMode();
        IsInUIMode = false;
        Debug.Log("遊戲模式切換為：準心模式");
    }
}