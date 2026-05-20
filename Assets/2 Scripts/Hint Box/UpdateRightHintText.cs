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
    [SerializeField] private GameObject destroyOjb_ToKitchen_Dia;
    [SerializeField] private GameObject destroyOjb_AfterGetInfos;

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
    public string text_picture; // 拿起客廳壁爐上的合照後
    public string text_afterGhost1; // 鬼視野+對話完全結束後的3個通知
    public string text_afterGhost2;
    public string text_afterGhost3;
    public string text_endToKitchen; // 剛進到廚房對話結束後
    public string text_endKRInfo; // 拿完第二輪的紙條後
    [Tooltip("這個不要打字")] public string id_voiceItem; // 給聲音物品通用
    public string text_vDB; // 拿完飯廳子彈後 (對應 "0")
    public string text_vKB; // 拿完廚房子彈後 (對應 "1")
    public string text_vRod; // 拿完曬衣桿後 (對應 "2")
    public string text_krInfo_1; // 拿取安眠藥紙條1後
    public string text_krInfo_2; // 拿取毒藥紙條2後
    public string text_krInfo_3; // 拿取芒果製品紙條3後
    public string text_goToB1; // 一樓目前沒有可以獲得的東西時


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

        // [20260519 新增對話] 
        DialogueManager.Instance.TriggerDialogueByEvent("News_B");
    }

    public void AfterNews1() // 拿起樓梯旁小桌子的報紙後，獲得劉宅命案報導-1
    {
        if (hintPrefab == null || uiCanvas == null) return;
        GameObject newHint = Instantiate(hintPrefab, uiCanvas);
        SelfDestroyHint hintScript = newHint.GetComponent<SelfDestroyHint>();
        if (hintScript != null) hintScript.InitAndShow(text_news1);

        // [20260519 新增對話] 
        DialogueManager.Instance.TriggerDialogueByEvent("News_1");
    }

    public void AfterNews2() // 拿起飯廳櫃子上的報紙後，獲得劉宅命案報導-2
    {
        if (hintPrefab == null || uiCanvas == null) return;
        GameObject newHint = Instantiate(hintPrefab, uiCanvas);
        SelfDestroyHint hintScript = newHint.GetComponent<SelfDestroyHint>();
        if (hintScript != null) hintScript.InitAndShow(text_news2);

        // [20260519 新增對話] 
        DialogueManager.Instance.TriggerDialogueByEvent("News_2");
    }
    
    public void AfterNews3() // 拿起飯廳桌上的報紙後，獲得劉宅命案報導-3
    {
        if (hintPrefab == null || uiCanvas == null) return;
        GameObject newHint = Instantiate(hintPrefab, uiCanvas);
        SelfDestroyHint hintScript = newHint.GetComponent<SelfDestroyHint>();
        if (hintScript != null) hintScript.InitAndShow(text_news3);

        // [20260519 新增對話] 
        DialogueManager.Instance.TriggerDialogueByEvent("News_3");
    }

    public void AfterPicture() // 拿起客廳壁爐上的合照後，獲得案件當天的合照
    {
        if (hintPrefab == null || uiCanvas == null) return;
        GameObject newHint = Instantiate(hintPrefab, uiCanvas);
        SelfDestroyHint hintScript = newHint.GetComponent<SelfDestroyHint>();
        if (hintScript != null) hintScript.InitAndShow(text_picture);

        // [20260519 新增對話] 
        DialogueManager.Instance.TriggerDialogueByEvent("Picture");
    }

    #region = 鬼視野+對話完全結束後的3個通知，請繼續調查飯廳四周與廚房、獲得走廊盡頭人的姿勢差異 =
    public void AfterGhost() // 鬼視野+對話完全結束後的2個通知，請繼續調查飯廳四周與廚房、獲得走廊盡頭人的姿勢差異、已更新鬼視野—李春梅
    {
        // 使用協程來依序播放 3 個提示
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

        float waitTime2 = 3.0f; // 預設等待時間防呆

        if (script2 != null)
        {
            script2.InitAndShow(text_afterGhost2);
            // 計算第二個提示的總時間
            waitTime2 = script2.slideInDuration + script2.displayDuration + script2.slideOutDuration;
        }

        // 等待第二個提示框播完動畫
        yield return new WaitForSeconds(waitTime2 + 0.1f);

        // --- 播放第三個提示 ---
        GameObject hint3 = Instantiate(hintPrefab, uiCanvas);
        SelfDestroyHint script3 = hint3.GetComponent<SelfDestroyHint>();

        if (script3 != null)
        {
            script3.InitAndShow(text_afterGhost3);
        }
    }
    #endregion

    public void EndToKitchen() // 剛進到廚房對話結束後，請還原命案當天的聚餐菜單
    {
        if (hintPrefab == null || uiCanvas == null) return;
        GameObject newHint = Instantiate(hintPrefab, uiCanvas);
        SelfDestroyHint hintScript = newHint.GetComponent<SelfDestroyHint>();
        if (hintScript != null) hintScript.InitAndShow(text_endToKitchen);

        Destroy(destroyOjb_ToKitchen_Dia);
    }

    public void EndKRInfo() // 拿完第二輪的紙條後，請前往一樓的其他房間調查
    {
        if (hintPrefab == null || uiCanvas == null) return;
        GameObject newHint = Instantiate(hintPrefab, uiCanvas);
        SelfDestroyHint hintScript = newHint.GetComponent<SelfDestroyHint>();
        if (hintScript != null) hintScript.InitAndShow(text_endKRInfo);

        Destroy(destroyOjb_AfterGetInfos);
    }

    public void VoiceItem() // 給聲音物品通用，獲得 乾溼曬衣桿的不同 / 飯廳畫上的子彈 / 廚房走廊的子彈
    {
        if (hintPrefab == null || uiCanvas == null) return;

        // 1. 先準備一個空字串來裝準備要顯示的文字
        string textToShow = "";

        // 2. 根據 id_voiceItem 的值，決定 textToShow 是哪一個
        switch (id_voiceItem)
        {
            case "0":
                textToShow = text_vDB; // 獲得飯廳畫上的子彈
                break;
            case "1":
                textToShow = text_vKB; // 獲得廚房走廊的子彈
                break;
            case "2":
                textToShow = text_vRod; // 獲得多功能室的曬衣桿
                break;
            default:
                Debug.LogWarning($"[UpdateRightHintText] 收到未知的 id_voiceItem: {id_voiceItem}，無法顯示對應提示。");
                return; // 提早結束，不生成 UI
        }

        // 3. 生成 UI 並將對應的文字傳進去
        GameObject newHint = Instantiate(hintPrefab, uiCanvas);
        SelfDestroyHint hintScript = newHint.GetComponent<SelfDestroyHint>();

        if (hintScript != null)
        {
            // [修正] 原本寫死的 text_endKRInfo 改為動態決定的 textToShow
            hintScript.InitAndShow(textToShow);
        }
    }

    #region ===== 料理解謎中獲得的3個紙條 =====
    public void GetKRInfo_1() // 拿取安眠藥紙條後，獲得資訊紙條-安眠藥
    {
        if (hintPrefab == null || uiCanvas == null) return;
        GameObject newHint = Instantiate(hintPrefab, uiCanvas);
        SelfDestroyHint hintScript = newHint.GetComponent<SelfDestroyHint>();
        if (hintScript != null) hintScript.InitAndShow(text_krInfo_1);
    }

    public void GetKRInfo_2() // 拿取毒藥紙條後，獲得資訊紙條-毒藥
    {
        if (hintPrefab == null || uiCanvas == null) return;
        GameObject newHint = Instantiate(hintPrefab, uiCanvas);
        SelfDestroyHint hintScript = newHint.GetComponent<SelfDestroyHint>();
        if (hintScript != null) hintScript.InitAndShow(text_krInfo_2);
    }

    public void GetKRInfo_3() // 拿取芒果製品紙條後，獲得資訊紙條-芒果製品
    {
        if (hintPrefab == null || uiCanvas == null) return;
        GameObject newHint = Instantiate(hintPrefab, uiCanvas);
        SelfDestroyHint hintScript = newHint.GetComponent<SelfDestroyHint>();
        if (hintScript != null) hintScript.InitAndShow(text_krInfo_3);
    }

    #endregion

    public void GoToB1() // 一樓目前沒有可以獲得的東西時，請前往地下室
    {
        if (hintPrefab == null || uiCanvas == null) return;
        GameObject newHint = Instantiate(hintPrefab, uiCanvas);
        SelfDestroyHint hintScript = newHint.GetComponent<SelfDestroyHint>();
        if (hintScript != null) hintScript.InitAndShow(text_goToB1);
    }
}
