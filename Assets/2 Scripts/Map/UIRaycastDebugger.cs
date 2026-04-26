using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class UIRaycastDebugger : MonoBehaviour
{
    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // 每次按左鍵
        {
            PointerEventData eventData = new PointerEventData(EventSystem.current);
            eventData.position = Input.mousePosition;
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            Debug.Log("--- UI 點擊穿透測試 ---");
            foreach (var result in results)
            {
                Debug.Log($"點擊到了物件：{result.gameObject.name}，層級：{result.gameObject.layer}");
            }
            if (results.Count == 0) Debug.Log("完全沒點到任何 UI 物件！");
        }
    }
}