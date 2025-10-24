using UnityEngine;
using System.Collections.Generic;

// 這是一個非常簡化的範例管理器
public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    // <QuestID, QuestStatus>
    private Dictionary<string, QuestStatus> questStates = new Dictionary<string, QuestStatus>();

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    public void UpdateQuestStatus(string questID, QuestStatus status)
    {
        questStates[questID] = status;
        Debug.Log($"<color=purple>[QuestManager] 任務 '{questID}' 狀態已更新為: {status}</color>");
    }

    public QuestStatus GetQuestStatus(string questID)
    {
        if (questStates.ContainsKey(questID))
        {
            return questStates[questID];
        }
        return QuestStatus.NotStarted;
    }
}