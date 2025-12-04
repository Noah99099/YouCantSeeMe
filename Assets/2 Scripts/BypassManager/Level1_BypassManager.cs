using UnityEngine;

public class Level1_BypassManager : MonoBehaviour
{
    // --- Phase 1: 流程跳過 - 核心目標 ---
    [Header("Phase 1: 流程跳過 - 核心目標")]
    [Tooltip("將場景中 PrepareToYinView 腳本所在的物件拖入")]
    public PrepareToYinView prepareToYinViewInstance;

    [Tooltip("將控制大門開關的 DoorController 腳本拖入")]
    public DoorController doorController;

    // --- Phase 2: Layer 變更與清理目標 ---
    // *** 關鍵變更：現在需要直接引用門片和門牌上的 ChangeObjectLayer 實例 ***
    [Header("Phase 2: Layer 變更與清理目標")]
    [Tooltip("門片上掛載的 ChangeObjectLayer 腳本")]
    public ChangeObjectLayer gateLayerChangerL;
    public ChangeObjectLayer gateLayerChangerR;
    [Tooltip("門牌上掛載的 ChangeObjectLayer 腳本")]
    public ChangeObjectLayer hNumLayerChanger;
    [Tooltip("密碼上掛載的 ChangeObjectLayer 腳本，限跳過才觸發，正常輸入密碼不觸發")]
    public ChangeObjectLayer num0;
    public ChangeObjectLayer num1;
    public ChangeObjectLayer num2;
    public ChangeObjectLayer num3;
    public ChangeObjectLayer num4;
    public ChangeObjectLayer num5;
    public ChangeObjectLayer num6;
    public ChangeObjectLayer num7;
    public ChangeObjectLayer num8;
    public ChangeObjectLayer num9;

    [Tooltip("流程結束後需要刪除的 Manager 或 Collider 物件")]
    public GameObject[] objectsToDestroy;

    private void Update()
    {
        // 偵測按下數字 1 鍵
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Debug.Log("【測試捷徑已啟用】 - 正在跳過 Level 1 進入流程...");
            PerformBypass();
        }
    }

    private void PerformBypass()
    {
        // 執行前置檢查
        if (prepareToYinViewInstance == null || doorController == null || 
            gateLayerChangerL == null || gateLayerChangerR == null || hNumLayerChanger == null || 
            num0 == null || num1 == null || num2 == null || num3 == null || num4 == null || 
            num5 == null || num6 == null || num7 == null || num8 == null || num9 == null)
        {
            Debug.LogError("BypassManager: 有關鍵參考物件遺失，請檢查 Inspector 設定！");
            return;
        }

        // --- 1. 模擬前三個交互，觸發 PrepareToYinView 的 Check() ---
        // 觸發整個事件鏈：計數 -> Check() -> finishYangCollider.SetActive(true) -> CanChangeView 事件
        prepareToYinViewInstance.InvokeYangAction_Lock();
        prepareToYinViewInstance.InvokeYangAction_Gate();
        prepareToYinViewInstance.InvokeYangAction_HNum();

        // --- 2. 模擬密碼鎖正確輸入，直接開門 ---
        doorController.OpenDoor();

        // --- 3. 模擬物件 Layer 改變 (避免再次交互) ---
        // 直接呼叫門片和門牌各自的 ChangeObjectLayer 腳本上的 ChangeLayer() 方法。
        // 每個實例會使用它們自己的 targetLayerName (您在 Inspector 中設定的 "Default")。
        gateLayerChangerL.ChangeLayer();
        gateLayerChangerR.ChangeLayer();
        hNumLayerChanger.ChangeLayer();
        num0.ChangeLayer();
        num1.ChangeLayer();
        num2.ChangeLayer();
        num3.ChangeLayer();
        num4.ChangeLayer();
        num5.ChangeLayer();
        num6.ChangeLayer();
        num7.ChangeLayer();
        num8.ChangeLayer();
        num9.ChangeLayer();

        // --- 4. 模擬刪除 Manager/Collider 物件 (場景減負) ---
        foreach (GameObject obj in objectsToDestroy)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }

        Debug.Log("【測試捷徑已完成】 - 流程已跳過，大門已開啟，場景已清理。");

        // 禁用此 Manager，避免再次意外觸發
        this.enabled = false;
    }
}
