using System.Collections;
using System.Collections.Generic;
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

    /// <summary>
    /// 設定當前選中的 UI 物件，但會根據輸入類型判斷。
    /// </summary>
    public void SetSelected(GameObject target)
    {
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
        EventSystem.current.SetSelectedGameObject(null);
    }
}
