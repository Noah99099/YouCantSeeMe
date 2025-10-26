using UnityEngine;

public class GetTwoThings : MonoBehaviour
{
    [Header("要打開的判定點")]
    public GameObject two;
    private int num = 0;  

    private void OnEnable()
    {
        // 訂閱事件
        Map.GetMap += OnMapCollected;
        CaseRecordBook.OnCollected += OnCaseRecordBookCollected;
    }

    private void OnDisable()
    {
        // 取消訂閱事件，防止記憶體洩漏或重複觸發
        Map.GetMap -= OnMapCollected;
        CaseRecordBook.OnCollected -= OnCaseRecordBookCollected;
    }

    private void OnMapCollected()
    {
        Debug.Log("ItemEventReceiver: 收到 GetMap 事件。");
        // TODO: 在這裡撰寫收到地圖後的行為
        num += 1;
        CheckTwoThings();
    }

    private void OnCaseRecordBookCollected()
    {
        Debug.Log("ItemEventReceiver: 收到 OnCollected 事件。");
        // TODO: 在這裡撰寫收到案件紀錄簿後的行為
        num += 1;
        CheckTwoThings();
    }

    public void CheckTwoThings() 
    {
        if(num == 2) 
        {
            two.SetActive(true);
        }
    }
}
