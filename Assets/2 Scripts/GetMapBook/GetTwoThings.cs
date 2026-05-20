using UnityEngine;

public class GetTwoThings : MonoBehaviour
{
    [Header("要打開的判定點")]
    public GameObject two;

    private void Start()
    {
        // 進入場景時，主動檢查進度
        CheckWithManager();
    }

    public void CheckWithManager()
    {
        if (GetMapBookManager.Instance != null)
        {
            // 如果從管理器確認地圖和書都拿到了
            if (GetMapBookManager.Instance.hasMap && GetMapBookManager.Instance.hasBook)
            {
                // 執行開啟邏輯
                ExecuteActivation(GetMapBookManager.Instance.uiElement1, GetMapBookManager.Instance.uiElement2);
            }
        }
    }

    public void ExecuteActivation(GameObject ui1, GameObject ui2)
    {
        // 1. 打開 Scene 2 的判定點
        if (two != null)
        {
            two.SetActive(true);
            Debug.Log("GetTwoThings: Scene 2 判定點 'two' 已打開。");
        }

        // 2. 打開跨場景過來的兩個 UI
        if (ui1 != null) ui1.SetActive(true);
        if (ui2 != null) ui2.SetActive(true);

        Debug.Log("GetTwoThings: 跨場景 UI 已同步打開。");
    }
}
