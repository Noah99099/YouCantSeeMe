// PrepareToYinView.cs
using System;
using UnityEngine;

public class PrepareToYinView : MonoBehaviour
{
    // 統一事件
    public static event Action YangAction;
    public GameObject finishYangCollider;
    private int num = 0; //門牌、密碼鎖、門片

    public static event Action CanChangeView; // 給 Level1UIController.cs 接收用

    public static void InvokeYangAction()
    {
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
        num += 1;
        Check();
    }

    private void Check() 
    {       
        if (num == 3) //門牌、密碼鎖、門片
        {
            Debug.Log("[PrepareToYinView] 打開判定點");
            finishYangCollider.SetActive(true);

            CanChangeView?.Invoke(); // 給 Level1UIController.cs 接收用，因為一開始不能切換視野
        }
    }
}
