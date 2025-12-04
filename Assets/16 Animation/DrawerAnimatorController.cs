using UnityEngine;

// 為了讓其他腳本知道這是可以交互的物件，我們可以讓它繼承一個空介面或 MonoBehaviour
public class DrawerAnimatorController : MonoBehaviour
{
    private Animator animator;

    // 參數名稱必須與您在 Animator Controller 中創建的 Trigger 名稱一致！
    private const string TOGGLE_TRIGGER_NAME = "Toggle";

    void Awake()
    {
        // 獲取附加在同一個 GameObject 上的 Animator 組件
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError($"[DrawerAnimatorController] 物件 {gameObject.name} 缺少 Animator 組件，抽屜動畫無法運行！");
        }
    }

    /// <summary>
    /// 公共互動方法，用於觸發抽屜的開/關動畫。
    /// </summary>
    public void Interact()
    {
        if (animator != null)
        {
            // 呼叫 Animator Controller 中的 Trigger，
            // 該 Trigger 會根據當前狀態 (開著或關著) 決定播放 開啟 或 關閉 動畫。
            animator.SetTrigger(TOGGLE_TRIGGER_NAME);
            Debug.Log($"[DrawerAnimatorController] 觸發 {gameObject.name} 的 '{TOGGLE_TRIGGER_NAME}' 動畫。");
        }
    }
}