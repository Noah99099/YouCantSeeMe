using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 在宅邸用腳本，大門不用
/// 跳出右上提示框
/// </summary>
public class UpdateRightHintText : MonoBehaviour
{
    [Header("基本生成設定")]
    [SerializeField] private GameObject hintPrefab; // 放入掛有 SelfDestroyHint 的 Prefab
    [SerializeField] private Transform uiCanvas;    // 必須生在 Canvas 底下才能顯示

    [Header("其他要呼叫的內容")]
    //[SerializeField] private GameObject destroyOjb_StartDialouge; //不能刪 會卡住完全走不了、移動。十分詭異
    [SerializeField] private GameObject destroyOjb_GetTwoThings;

    [Header("提示一覽")]
    public string text_1; // 拿完玄關結束
    public string text_2; // 拿完2個道具對話結束後的提示
    public string text_3; // 第一次往飯廳走
    public string text_4; // 與5個證物立牌交互後
    public string text_5; // 證物立牌調查對話結束
    public string text_newsBuild; // 拿起客廳桌子上的報紙後
    public string text_news1; // 拿起樓梯旁小桌子的報紙後
    public string text_news2; // 拿起飯廳櫃子上的報紙後
    public string text_news3; // 拿起飯廳桌上的報紙後
    public string text_afterGhost1; // 鬼視野+對話完全結束後的2個通知
    public string text_afterGhost2;


    // 開放一個方法給 Button 的 OnClick 呼叫

    public void StartDialouge() // 拿完玄關結束，請拿取平面圖和案件紀錄簿
    {
        if (hintPrefab == null || uiCanvas == null)
        {
            Debug.LogError("HintSpawner: Prefab 或 Canvas 尚未綁定！");
            return;
        }

        // 1. 在指定的 Canvas 下生成 Prefab 複製品
        GameObject newHint = Instantiate(hintPrefab, uiCanvas);

        // 2. 獲取它身上的 SelfDestroyHint 腳本
        SelfDestroyHint hintScript = newHint.GetComponent<SelfDestroyHint>();

        if (hintScript != null)
        {
            // 3. 傳入文字並啟動動畫與銷毀流程 (預設 2 秒)
            hintScript.InitAndShow(text_1);
        }

        // 不能刪 會卡住完全走不了、移動。十分詭異
        //Destroy(destroyOjb_StartDialouge); // 刪除:StartDialouge，一進玄關的對話管理器
    }

    public void GetTwoThings() //拿完2個道具對話結束後的提示，請前往案發現場—飯廳
    {
        if (hintPrefab == null || uiCanvas == null)
        {
            Debug.LogError("HintSpawner: Prefab 或 Canvas 尚未綁定！");
            return;
        }

        // 1. 在指定的 Canvas 下生成 Prefab 複製品
        GameObject newHint = Instantiate(hintPrefab, uiCanvas);

        // 2. 獲取它身上的 SelfDestroyHint 腳本
        SelfDestroyHint hintScript = newHint.GetComponent<SelfDestroyHint>();

        if (hintScript != null)
        {
            // 3. 傳入文字並啟動動畫與銷毀流程 (預設 2 秒)
            hintScript.InitAndShow(text_2);
        }

        Destroy(destroyOjb_GetTwoThings); // 刪除:GetTwoThings，獲得平面圖和紀錄簿對話的管理器和對話
    }

    public void InvestigationDining() //第一次往飯廳走，調查死者死因
    {
        if (hintPrefab == null || uiCanvas == null)
        {
            Debug.LogError("HintSpawner: Prefab 或 Canvas 尚未綁定！");
            return;
        }

        // 1. 在指定的 Canvas 下生成 Prefab 複製品
        GameObject newHint = Instantiate(hintPrefab, uiCanvas);

        // 2. 獲取它身上的 SelfDestroyHint 腳本
        SelfDestroyHint hintScript = newHint.GetComponent<SelfDestroyHint>();

        if (hintScript != null)
        {
            // 3. 傳入文字並啟動動畫與銷毀流程 (預設 2 秒)
            hintScript.InitAndShow(text_3);
        }
    }

    public void AfterEvidenceMarker() // 與5個證物立牌交互後，獲得死因調查結果
    {
        if (hintPrefab == null || uiCanvas == null) return;
        GameObject newHint = Instantiate(hintPrefab, uiCanvas);
        SelfDestroyHint hintScript = newHint.GetComponent<SelfDestroyHint>();
        if (hintScript != null) hintScript.InitAndShow(text_4);
    }

    public void AfterMarkerConclusion() // 證物立牌調查對話結束，解開飯廳的法陣
    {
        if (hintPrefab == null || uiCanvas == null) return;
        GameObject newHint = Instantiate(hintPrefab, uiCanvas);
        SelfDestroyHint hintScript = newHint.GetComponent<SelfDestroyHint>();
        if (hintScript != null) hintScript.InitAndShow(text_5);
    }

    public void AfterNewsBuild() // 拿起客廳桌子上的報紙後，獲得劉氏集團建案完工報導
    {
        if (hintPrefab == null || uiCanvas == null) return;
        GameObject newHint = Instantiate(hintPrefab, uiCanvas);
        SelfDestroyHint hintScript = newHint.GetComponent<SelfDestroyHint>();
        if (hintScript != null) hintScript.InitAndShow(text_newsBuild);
    }

    public void AfterNews1() // 拿起樓梯旁小桌子的報紙後，獲得劉宅命案報導-1
    {
        if (hintPrefab == null || uiCanvas == null) return;
        GameObject newHint = Instantiate(hintPrefab, uiCanvas);
        SelfDestroyHint hintScript = newHint.GetComponent<SelfDestroyHint>();
        if (hintScript != null) hintScript.InitAndShow(text_news1);
    }

    public void AfterNews2() // 拿起飯廳櫃子上的報紙後，獲得劉宅命案報導-2
    {
        if (hintPrefab == null || uiCanvas == null) return;
        GameObject newHint = Instantiate(hintPrefab, uiCanvas);
        SelfDestroyHint hintScript = newHint.GetComponent<SelfDestroyHint>();
        if (hintScript != null) hintScript.InitAndShow(text_news2);
    }
    
    public void AfterNews3() // 拿起飯廳桌上的報紙後，獲得劉宅命案報導-3
    {
        if (hintPrefab == null || uiCanvas == null) return;
        GameObject newHint = Instantiate(hintPrefab, uiCanvas);
        SelfDestroyHint hintScript = newHint.GetComponent<SelfDestroyHint>();
        if (hintScript != null) hintScript.InitAndShow(text_news3);
    }

    #region = 鬼視野+對話完全結束後的2個通知，請繼續調查飯廳四周與廚房、獲得走廊盡頭人的姿勢差異 =
    public void AfterGhost() // 鬼視野+對話完全結束後的2個通知，請繼續調查飯廳四周與廚房、獲得走廊盡頭人的姿勢差異
    {
        // 使用協程來依序播放兩個提示
        StartCoroutine(ShowGhostHintsCoroutine());
    }
    private IEnumerator ShowGhostHintsCoroutine()
    {
        if (hintPrefab == null || uiCanvas == null)
        {
            Debug.LogError("HintSpawner: Prefab 或 Canvas 尚未綁定！");
            yield break;
        }

        // --- 播放第一個提示 ---
        GameObject hint1 = Instantiate(hintPrefab, uiCanvas);
        SelfDestroyHint script1 = hint1.GetComponent<SelfDestroyHint>();

        float waitTime = 3.0f; // 預設等待時間防呆

        if (script1 != null)
        {
            script1.InitAndShow(text_afterGhost1);
            // 動態取得第一個提示框的總生命週期時間 (滑入時間 + 停留時間 + 滑出時間)
            waitTime = script1.slideInDuration + script1.displayDuration + script1.slideOutDuration;
        }

        // 等待第一個提示框播完動畫並自動銷毀
        // 額外加 0.1 秒緩衝，確保畫面順暢交接
        yield return new WaitForSeconds(waitTime + 0.1f);

        // --- 播放第二個提示 ---
        GameObject hint2 = Instantiate(hintPrefab, uiCanvas);
        SelfDestroyHint script2 = hint2.GetComponent<SelfDestroyHint>();

        if (script2 != null)
        {
            script2.InitAndShow(text_afterGhost2);
        }
    }
    #endregion
}
