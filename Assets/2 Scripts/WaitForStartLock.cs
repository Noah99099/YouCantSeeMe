using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaitForStartLock : MonoBehaviour
{
    [Header("時間設定")]
    [Tooltip("等待幾秒才開啟後續事件")]
    public float delayTime = 1f; // 新增：可自訂的等待時間，預設 0.5 秒

    [Header("需要等待再對話的判定")]
    public GameObject startLock;
    public DestroyMe destroyMe;

    // 這個 public void 是給 Unity 編輯器 (如按鈕或對話系統) 拖曳引用的入口
    public void WaitAndFinish()
    {
        print("[WaitingManager] 觸發 Wait_1，開始計時");
        // 啟動專屬於 Wait_1 的協程
        StartCoroutine(Wait1Coroutine());
    }

    // 真正負責等待與執行後續動作的協程，Wait_1() 用
    private IEnumerator Wait1Coroutine()
    {
        // 1. 暫停執行，等待我們設定的 delayTime 秒數
        yield return new WaitForSeconds(delayTime);

        // 2. 以下的程式碼，保證會在等待結束後才執行
        print("[WaitingManager] 等待結束");

        startLock.SetActive(true);
        print("[WaitingManager] 打開判定");

        destroyMe.DestroyObject();
        print("[WaitingManager] 刪除");
    }
}
