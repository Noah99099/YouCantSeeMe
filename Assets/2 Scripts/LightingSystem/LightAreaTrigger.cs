using System.Collections.Generic;
using UnityEngine;
using KanWu.Systems; // 引用系統管理器以便呼叫開燈 API

namespace KanWu.Environment
{
    /// <summary>
    /// 區域燈光觸發器：偵測玩家進入特定區域後，觸發一個或多個燈光群組
    /// </summary>
    [RequireComponent(typeof(Collider))] // 防呆機制：強制要求掛載此腳本的物件必須要有 Collider
    public class LightAreaTrigger : MonoBehaviour
    {
        [Header("觸發設定")]
        [Tooltip("進入此區域時要同時開啟的燈光群組 ID 列表")]
        [SerializeField] private List<string> TargetGroupIDs;

        [Tooltip("是否只允許觸發一次？(恐怖遊戲通常為 true)")]
        [SerializeField] private bool TriggerOnce = true;

        [Tooltip("觸發對象的標籤 (通常是 Player)")]
        [SerializeField] private string TargetTag = "Player";

        private bool _hasTriggered = false;

        private void Awake()
        {
            // 確保掛載的 Collider 已經勾選 IsTrigger，避免變成物理實體阻擋玩家
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }
            
            // 如果場景中不需要看到這個物件，可以將 MeshRenderer 關閉
            MeshRenderer mesh = GetComponent<MeshRenderer>();
            if (mesh != null)
            {
                mesh.enabled = false;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // 如果已經觸發過且設定為只觸發一次，則直接跳出
            if (_hasTriggered && TriggerOnce) return;

            // 檢查進入區域的是否為目標物件 (例如玩家)
            if (other.CompareTag(TargetTag))
            {
                _hasTriggered = true;
                
                // 遍歷所有設定好的群組 ID，並呼叫 Manager 開燈
                foreach (string groupID in TargetGroupIDs)
                {
                    if (LightSystemManager.Instance != null)
                    {
                        LightSystemManager.Instance.ActivateLightGroup(groupID);
                    }
                    else
                    {
                        Debug.LogError("[LightAreaTrigger] 場景中找不到 LightSystemManager 實例！");
                    }
                }
            }
        }
    }
}