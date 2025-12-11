// KeypadButton.cs
using UnityEngine;

/// <summary>
/// 處理單個密碼鎖按鈕的視覺回饋。
/// 這是附加在 3D 模型上的腳本。
/// </summary>
public class KeypadButton : MonoBehaviour
{
    [Tooltip("此按鈕代表的數字 (0-9)")]
    [SerializeField] private int _digit;

    [Header("位移回饋設定")]
    [Tooltip("選中時按鈕沿 Z 軸移動的距離 (負值為向下移動)")]
    [SerializeField] private float _zOffsetOnSelect = -0.1f; // 新增 Z 軸位移量

    private Vector3 _defaultLocalPosition; // 新增儲存初始局部位置

    private void Awake()
    {
        // ***** 【關鍵修正：直接引用靜態單例】 *****
        // 假設 KeypadLock 在 KeypadButton 運行時已經在場景中存在（Awake/Start 已運行）
        if (KeypadLock.Instance == null)
        {
            Debug.LogError($"KeypadButton {_digit} 無法找到 KeypadLock 實例！請確認 KeypadLock 所在的場景是否已載入。");
            enabled = false;
            return;
        }

        // 將事件訂閱到單例實例上
        KeypadLock.Instance.OnDigitStateChanged += UpdateVisualState;

        // 儲存初始局部位置
        _defaultLocalPosition = transform.localPosition;
    }

    private void OnDestroy()
    {
        // ***** 【修正：取消訂閱單例上的事件】 *****
        if (KeypadLock.Instance != null)
        {
            KeypadLock.Instance.OnDigitStateChanged -= UpdateVisualState;
        }
    }

    /// <summary>
    /// 更新按鈕的視覺狀態 (由 KeypadLock 呼叫)
    /// </summary>
    private void UpdateVisualState(int digit, bool isSelected)
    {
        if (digit == _digit)
        {
            Vector3 targetLocalPosition = _defaultLocalPosition;
            string state = isSelected ? "【按下】" : "【彈起】";

            if (isSelected)
            {
                // 計算按下時的目標位置
                targetLocalPosition.z += _zOffsetOnSelect;
            }

            // 應用新的局部位置
            transform.localPosition = targetLocalPosition;

            // ***** 新增 Debug 訊息 *****
            Debug.Log($"KeypadButton {_digit} 狀態變更為 {state}. " +
                      $"位移量: {_zOffsetOnSelect}. " +
                      $"初始 Z: {_defaultLocalPosition.z:F4}, " +
                      $"目標 Z: {targetLocalPosition.z:F4}");

            // 針對位移失敗的進一步診斷：
            if (transform.localPosition != targetLocalPosition)
            {
                Debug.LogWarning($"按鍵 {_digit} 實際位置與目標位置不符！請檢查是否有其他腳本或動畫影響 Transform。");
            }

            // 由於這是立即改變位置，如果您想要更平滑的動畫，需要使用 Coroutine 或 DOTween 等工具。
            // 目前的實現是立即改變位置，符合您「向z軸移動-0.1」的需求。
        }
    }
}