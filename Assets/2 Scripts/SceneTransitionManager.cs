using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;

public class SceneTransitionManager : MonoBehaviour
{
    [Header("場景設定")]
    [Tooltip("要載入的下一個場景的名稱")]
    public string sceneToLoad;

    [Header("條件設定")]
    [Tooltip("需要持有的物品名稱（可動態增減）")]
    public List<string> requiredItemNames = new List<string>();

    [Header("UI回饋")]
    [Tooltip("用於顯示提示訊息的文字元件")]
    public TextMeshProUGUI feedbackText;

    /// <summary>
    /// 嘗試載入下一個場景（需具備所有條件物品）
    /// </summary>
    public void AttemptToLoadNextScene()
    {
        List<string> missingItems = new List<string>();

        // 檢查每一個條件物品是否存在
        foreach (string itemName in requiredItemNames)
        {
            if (!InventoryManager.Instance.HasItem(itemName))
            {
                missingItems.Add(itemName);
            }
        }

        if (missingItems.Count == 0)
        {
            Debug.Log("條件達成！載入下一個場景...");

            if (feedbackText != null)
            {
                feedbackText.gameObject.SetActive(false);
            }

            SceneManager.LoadScene(sceneToLoad);
        }
        else
        {
            Debug.Log("缺少必要物品，無法載入下一個場景！");

            if (feedbackText != null)
            {
                feedbackText.text = "缺少物品：\n" + string.Join("\n", missingItems);
                feedbackText.gameObject.SetActive(true);
            }
        }
    }
}
