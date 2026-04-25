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

        [Header("燈光識別碼與物件映射")]
        [SerializeField] private List<LightTriggerMapping> LightMappings;

        // 單例模式：方便組員直接全域呼叫
        public static LightSystemManager Instance { get; private set; }

        private void Awake()
        {
            // 標準的單例防呆機制
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Debug.LogWarning("[LightSystem] 場景中存在多個 LightSystemManager，已銷毀重複物件。");
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 提供給組員呼叫的開燈 API
        /// </summary>
        /// <param name="groupID">你在 Inspector 中設定的 GroupID</param>
        public void ActivateLightGroup(string groupID)
        {
            LightTriggerMapping mapping = LightMappings.Find(x => x.GroupID == groupID);

            if (mapping != null && !mapping.IsTriggered)
            {
                mapping.IsTriggered = true;
                
                if (mapping.TargetLightGroup != null)
                {
                    Debug.Log($"[LightSystem] 收到指令！正在開啟燈光群組：{groupID}。");
                    mapping.TargetLightGroup.TurnOnLights();
                }
                else
                {
                    Debug.LogWarning($"[LightSystem] 群組 {groupID} 缺少對應的 LightGroupController 組件！");
                }
            }
            else if (mapping == null)
            {
                Debug.LogError($"[LightSystem] 呼叫失敗：找不到 ID 為 '{groupID}' 的燈光群組，請檢查拼寫。");
            }
        }
    }
}