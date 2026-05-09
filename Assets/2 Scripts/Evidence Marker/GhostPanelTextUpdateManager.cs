using UnityEngine;
using TMPro;

/// <summary>
/// 掛載於 Scene 1 的 Panel 上。作為跨場景 UI 更新的全局接口。
/// </summary>
public class GhostPanelTextUpdateManager : MonoBehaviour
{
    // 單例模式：讓全域都可以直接透過 GhostPanelTextUpdateManager.Instance 存取
    public static GhostPanelTextUpdateManager Instance { get; private set; }

    [Header("UI 綁定 (Scene 1 內部綁定)")]
    [Tooltip("請將 Panel 底下對應的 5 個 TextMeshPro 元件，依序填入此陣列 (Size 設為 5)")]
    [SerializeField] private TextMeshProUGUI[] targetPanelTexts;

    private void Awake()
    {
        // 初始化單例
        if (Instance == null)
        {
            Instance = this;
            // 注意：如果您的 Panel 父節點已經有 DontDestroyOnLoad，這裡就不需要再加。
        }
        else
        {
            // 避免跨場景載入時出現兩個一樣的 Manager
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 提供給 Scene 2 的物件呼叫，根據指定的編號更新對應的文本
    /// </summary>
    /// <param name="index">文本在陣列中的編號 (0~4)</param>
    /// <param name="newText">要更新的內容</param>
    public void UpdateGhostText(int index, string newText)
    {
        // 安全檢查：確保陣列有設定，且編號沒有超出範圍
        if (targetPanelTexts != null && index >= 0 && index < targetPanelTexts.Length)
        {
            if (targetPanelTexts[index] != null)
            {
                targetPanelTexts[index].text = newText;
            }
            else
            {
                Debug.LogWarning($"[GhostPanelTextUpdateManager] 陣列中編號 {index} 的文本未綁定！");
            }
        }
        else
        {
            Debug.LogError($"[GhostPanelTextUpdateManager] 更新失敗！傳入的編號 {index} 無效，或陣列未設定。");
        }
    }
}
