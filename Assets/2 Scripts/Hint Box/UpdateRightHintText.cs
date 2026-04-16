using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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


    // 開放一個方法給 Button 的 OnClick 呼叫

    public void StartDialouge() // 拿完玄關結束
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

    public void GetTwoThings() //拿完2個道具對話結束後的提示
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
}
