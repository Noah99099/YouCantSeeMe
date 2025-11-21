using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CleanupOnStartMenu : MonoBehaviour
{
    // *** 定義所有需要被銷毀的跨場景物件的 Tag 列表 ***
    private readonly List<string> TagsToClean = new List<string>
    {
        "GamePersistent", // 例如：Player Controller、Game Manager
        "Player",         // 例如：您想用來標記 Player Controller 或舊 Player 物件
        "PlayerCamera",   // 主相機
        // 您可以在這裡添加更多需要清理的 Tag，例如 "GameHUD"
    };

    // 如果 A 場景是第一個場景，使用 Awake 或 Start 都可以
    void Start()
    {
        // 遍歷所有需要清理的 Tag
        foreach (string targetTag in TagsToClean)
        {
            // 1. 尋找所有帶有當前 targetTag 的物件
            // 注意：這個方法也會找到 DontDestroyOnLoad 的物件
            GameObject[] persistentObjects = GameObject.FindGameObjectsWithTag(targetTag);

            // 2. 逐一銷毀它們
            foreach (GameObject obj in persistentObjects)
            {
                // 增加一個檢查，確保物件不是 null，防止潛在錯誤
                if (obj != null)
                {
                    Debug.Log($"Cleanup: Destroying persistent object with Tag: {targetTag} - Name: {obj.name}");
                    Destroy(obj);
                }
            }
        }

        Debug.Log("Cleanup complete: All specified game persistent objects have been removed.");
    }
}