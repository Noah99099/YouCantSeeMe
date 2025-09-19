using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

[DefaultExecutionOrder(-10)] //第二個初始化此腳本
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

        // 如果已經選中同一個物件，不重複設置
        if (EventSystem.current.currentSelectedGameObject == target) return;

        if (InputDeviceManager.Instance != null &&
            InputDeviceManager.Instance.CurrentInputType == InputDeviceManager.InputType.Gamepad)
        {
            // 使用 Coroutine 延遲一幀
            InventoryUI.Instance.StartCoroutine(SetSelectedNextFrame(target));
        }
        else
        {
            EventSystem.current.SetSelectedGameObject(null); // 鍵鼠模式 → 不要強制鎖定
        }
    }
    private IEnumerator SetSelectedNextFrame(GameObject target)
    {
        yield return null; // 等一幀
        if (EventSystem.current.currentSelectedGameObject != target)
            EventSystem.current.SetSelectedGameObject(target);
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
