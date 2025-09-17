using UnityEngine;
using UnityEngine.EventSystems;

public class InventorySelection : MonoBehaviour
{
    public static InventorySelection Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// 設定當前選中的 UI 物件，但會根據輸入類型判斷。
    /// </summary>
    public void SetSelected(GameObject target)
    {
        if (target == null) return;
        if (InputDeviceManager.Instance != null &&
            InputDeviceManager.Instance.CurrentInputType == InputDeviceManager.InputType.Gamepad)
        {
            EventSystem.current.SetSelectedGameObject(target);
        }
        else
        {
            // 鍵鼠模式 → 不要強制鎖定
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    /// <summary>
    /// 清除當前選中的 UI 物件。
    /// </summary>
    public void ClearSelection()
    {
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }
}
