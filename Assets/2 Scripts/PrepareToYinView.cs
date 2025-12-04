// PrepareToYinView.cs
using System;
using Unity.VisualScripting;
using UnityEngine;

public class PrepareToYinView : MonoBehaviour
{
    // 統一事件
    public static event Action YangAction;
    [Tooltip("結束陽視野調查事件的collider")]
    public GameObject finishYangCollider;
    //門牌、密碼鎖、門片
    private int num_Lock = 0; //門片
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
            finishYangCollider.SetActive(true);

            CanChangeView?.Invoke(); // 給 Level1UIController.cs 接收用，因為一開始不能切換視野
        }
    }
}
