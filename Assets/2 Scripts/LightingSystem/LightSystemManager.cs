using System;
using System.Collections.Generic;
using UnityEngine;
using KanWu.Environment; // 確保引用燈光控制器所在的命名空間

namespace KanWu.Systems
{
    /// <summary>
    /// 燈光系統管理器：提供給其他系統呼叫的通用燈光控制介面
    /// </summary>
    public class LightSystemManager : MonoBehaviour
    {
        [Serializable]
        public class LightTriggerMapping
        {
            [Tooltip("燈光群組的唯一識別碼 (讓組員呼叫用的 ID)")]
            public string GroupID;
            [Tooltip("對應要開啟的燈光群組")]
            public LightGroupController TargetLightGroup; 
            [Tooltip("此燈光是否已被觸發")]
            public bool IsTriggered;
        }

        [Serializable]
        public class ThresholdTriggerMapping
        {
            [Tooltip("需要完成多少個謎題才會觸發？")]
            public int RequiredPuzzleCount;
            [Tooltip("觸發時要同時開啟哪些燈光群組 ID？")]
            public List<string> TargetGroupIDs;
            [HideInInspector] public bool IsTriggered;
        }

        [Header("基礎映射設定")]
        [SerializeField] private List<LightTriggerMapping> LightMappings;

        [Header("門檻觸發設定 (解開 N 個謎題後開燈)")]
        [SerializeField] private List<ThresholdTriggerMapping> ThresholdMappings;

        [Header("當前進度")]
        [SerializeField] private int CompletedPuzzleCount = 0;

        public static LightSystemManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        /// <summary>
        /// 提供給組員呼叫的 API：當任何謎題完成時，請呼叫此方法
        /// </summary>
        public void NotifyPuzzleSolved()
        {
            CompletedPuzzleCount++;
            Debug.Log($"[LightSystem] 謎題完成進度：{CompletedPuzzleCount}");

            CheckThresholds();
        }

        /// <summary>
        /// 檢查是否達到任何開燈門檻
        /// </summary>
        private void CheckThresholds()
        {
            foreach (var threshold in ThresholdMappings)
            {
                if (!threshold.IsTriggered && CompletedPuzzleCount >= threshold.RequiredPuzzleCount)
                {
                    threshold.IsTriggered = true;
                    Debug.Log($"[LightSystem] 達到門檻 {threshold.RequiredPuzzleCount}！開啟多個燈光組。");
                    
                    foreach (string groupID in threshold.TargetGroupIDs)
                    {
                        ActivateLightGroup(groupID);
                    }
                }
            }
        }

        /// <summary>
        /// 基礎開燈方法 (由內部或組員直接呼叫 ID)
        /// </summary>
        public void ActivateLightGroup(string groupID)
        {
            var mapping = LightMappings.Find(x => x.GroupID == groupID);
            if (mapping != null && !mapping.IsTriggered)
            {
                mapping.IsTriggered = true;
                if (mapping.TargetLightGroup != null)
                {
                    mapping.TargetLightGroup.TurnOnLights();
                }
            }
        }
    }
}