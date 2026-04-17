// PrepareToYinView.cs
using System;
using System.Collections; // 新增這行以使用協程
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// 在陽視野，與[門牌、密碼鎖、門片]都交互過一次後，先眨眼再對話[陽視野沒有東西調查]，最後陰視野開啟
/// </summary>
public class PrepareToYinView : MonoBehaviour
{
    // 統一事件
    public static event Action YangAction;
    [Tooltip("結束陽視野調查事件的collider")]
    public GameObject finishYangCollider;

    //新增: 眨眼過渡，避免對話1接對話2生硬卡頓
    public BlinkEffect blinkEffect;

    public GameObject centerText; //中間交互文字要暫時關閉
    public GameObject waitManager; //等待管理器

    [Header("時間設定")]
    [Tooltip("眨眼後等待幾秒才開啟後續事件")]
    public float delayAfterBlink = 0.5f; // 新增：可自訂的等待時間，預設 0.5 秒

    //門牌、密碼鎖、門片
    private int num_Lock = 0; //密碼鎖
    private int num_Gate = 0; //門片
    private int num_HNum = 0; //門牌

    public static event Action CanChangeView; // 給 Level1UIController.cs 接收用

    public void InvokeYangAction_Lock() //給密碼鎖用
    {
        num_Lock += 1;
        YangAction?.Invoke();
    }

    // 試試看這樣
    public void InvokeYangAction_Gate()
    {
        num_Gate += 1;
        YangAction?.Invoke();

        waitManager.SetActive(true);
    }
    public void InvokeYangAction_HNum()
    {
        num_HNum += 1;
        YangAction?.Invoke();
    }

    // 訂閱事件
    private void OnEnable()
    {
        PrepareToYinView.YangAction += HandleYinDialouge;
    }

    private void OnDisable()
    {
        PrepareToYinView.YangAction -= HandleYinDialouge;
    }

    void HandleYinDialouge()
    {
        Debug.Log("收到 YangAction 事件！");
        Check();
    }

    private void Check() 
    {       
        if (num_Lock >= 1 && num_Gate >= 1 && num_HNum >= 1) // 門牌、門片、密碼至少一次交互
        {
            Debug.Log("[PrepareToYinView] 打開判定點");

            //新增: 先眨眼
            blinkEffect.PlayBlink();

            // 新增：啟動計時器協程
            StartCoroutine(WaitAndFinish());   
        }
    }

    // 新增的協程方法：用來等待時間-眨眼用
    private IEnumerator WaitAndFinish()
    {
        // 暫停執行，等待我們設定的 delayAfterBlink 秒數
        yield return new WaitForSeconds(delayAfterBlink);

        centerText.SetActive(false);

        finishYangCollider.SetActive(true); //打開對話範圍 [陽視野沒有東西調查]
        CanChangeView?.Invoke(); // 給 Level1UIController.cs 接收用，因為一開始不能切換視野
    }
}
