using UnityEngine;
using UnityEngine.SceneManagement; // 引用場景管理命名空間
using TMPro; // 記得引用 TextMeshPro

public class SceneTransitionManager : MonoBehaviour
{
    [Header("場景設定")]
    [Tooltip("要載入的下一個場景的名稱")]
    public string sceneToLoad;

    [Header("條件設定")]
    [Tooltip("需要持有的物品1的名稱")]
    public string requiredItem1_Name = "圓球";

    [Tooltip("需要持有的物品2的名稱")]
    public string requiredItem2_Name = "方塊";

    [Header("UI回饋")]
    [Tooltip("用於顯示提示訊息的文字元件")]
    public TextMeshProUGUI feedbackText;

    /// <summary>
    /// 這個公開方法將被按鈕的 OnClick 事件呼叫
    /// </summary>
    public void AttemptToLoadNextScene()
    {
        // 檢查背包裡是否有指定的物品
        bool hasItem1 = InventoryManager.Instance.HasItem(requiredItem1_Name);
        bool hasItem2 = InventoryManager.Instance.HasItem(requiredItem2_Name);

        // 如果兩個物品都有
        if (hasItem1 && hasItem2)
        {
            Debug.Log("條件達成！載入下一個場景...");
            
            // 隱藏可能還在顯示的舊訊息
            if (feedbackText != null)
            {
                feedbackText.gameObject.SetActive(false);
            }

            // 載入場景
            SceneManager.LoadScene(sceneToLoad);
        }
        // 如果缺少任何一個物品
        else
        {
            Debug.Log("缺少必要物品，無法載入下一個場景！");
            
            // 顯示提示訊息
            if (feedbackText != null)
            {
                feedbackText.text = $"缺少物品：\n{(hasItem1 ? "" : requiredItem1_Name)}\n{(hasItem2 ? "" : requiredItem2_Name)}";
                feedbackText.gameObject.SetActive(true);
            }
        }
    }
}