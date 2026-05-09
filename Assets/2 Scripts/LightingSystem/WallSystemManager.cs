using System;
using System.Collections.Generic;
using UnityEngine;

namespace KanWu.Systems
{
    /// <summary>
    /// 空氣牆系統管理器：負責追蹤解謎進度並在達標時關閉對應的空氣牆
    /// </summary>
    public class WallSystemManager : MonoBehaviour
    {
        [Serializable]
        public class WallTriggerMapping
        {
            [Tooltip("空氣牆群組的唯一識別碼 (給 Inspector 或 API 呼叫的 ID)")]
            public string WallID;
            [Tooltip("對應要關閉的空氣牆物件 (通常是包含 BoxCollider 的透明物件)")]
            public GameObject TargetWall;
            [Tooltip("此空氣牆是否已被關閉")]
            public bool IsTriggered;
        }

        [Serializable]
        public class ThresholdTriggerMapping
        {
            [Tooltip("需要完成多少個謎題才會觸發？")]
            public int RequiredPuzzleCount;
            [Tooltip("觸發時要同時關閉哪些空氣牆 ID？")]
            public List<string> TargetWallIDs;
            [HideInInspector] public bool IsTriggered;
        }

        [Header("空氣牆基礎映射設定")]
        [SerializeField] private List<WallTriggerMapping> WallMappings;

        [Header("門檻觸發設定 (解開 N 個謎題後關閉空氣牆)")]
        [SerializeField] private List<ThresholdTriggerMapping> ThresholdMappings;

        [Header("當前解謎進度")]
        [SerializeField] private int CompletedPuzzleCount = 0;

        public static WallSystemManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        /// <summary>
        /// 提供給解謎腳本呼叫的 API：當任何謎題完成時，請呼叫此方法
        /// </summary>
        public void NotifyPuzzleSolved()
        {
            CompletedPuzzleCount++;
            Debug.Log($"[WallSystem] 謎題完成進度：{CompletedPuzzleCount}");

            CheckThresholds();
        }

        /// <summary>
        /// 檢查是否達到任何關閉空氣牆的門檻
        /// </summary>
        private void CheckThresholds()
        {
            foreach (var threshold in ThresholdMappings)
            {
                if (!threshold.IsTriggered && CompletedPuzzleCount >= threshold.RequiredPuzzleCount)
                {
                    threshold.IsTriggered = true;
                    Debug.Log($"[WallSystem] 達到門檻 {threshold.RequiredPuzzleCount}！關閉對應空氣牆。");

                    foreach (string wallID in threshold.TargetWallIDs)
                    {
                        DeactivateWall(wallID);
                    }
                }
            }
        }

        /// <summary>
        /// 基礎關閉空氣牆方法 (由內部或直接指定 ID 呼叫)
        /// </summary>
        public void DeactivateWall(string wallID)
        {
            var mapping = WallMappings.Find(x => x.WallID == wallID);
            if (mapping != null && !mapping.IsTriggered)
            {
                mapping.IsTriggered = true;
                if (mapping.TargetWall != null)
                {
                    // 關閉空氣牆物件 (使其失去阻擋效果)
                    mapping.TargetWall.SetActive(false);
                }
            }
        }
    }
}