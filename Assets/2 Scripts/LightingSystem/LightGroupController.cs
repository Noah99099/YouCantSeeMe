using System.Collections.Generic;
using UnityEngine;
using DG.Tweening; // 引入專案使用的 DOTween

namespace KanWu.Environment
{
    /// <summary>
    /// 燈光群組控制器：負責處理特定區域內燈光與發光材質的漸變開啟/關閉
    /// </summary>
    public class LightGroupController : MonoBehaviour
    {
        [Header("燈光設定")]
        [Tooltip("該區域包含的燈光組件")]
        [SerializeField] private List<Light> AreaLights;
        [Tooltip("目標亮度")]
        [SerializeField] private float TargetIntensity = 1.5f;
        [Tooltip("燈光漸變持續時間")]
        [SerializeField] private float FadeDuration = 2.0f;

        [Header("URP 發光材質設定 (選填)")]
        [Tooltip("需要同步發光的物件 (如：90年代的日光燈管模型)")]
        [SerializeField] private List<Renderer> EmissiveRenderers;
        [Tooltip("發光顏色與強度 (HDR)")]
        [ColorUsage(true, true)]
        [SerializeField] private Color EmissionColor = Color.white;

        private void Awake()
        {
            // 初始化：遊戲開始時將此區域燈光預設為關閉
            SetLightsState(false);
        }

        /// <summary>
        /// 平滑開啟該區域的燈光
        /// </summary>
        public void TurnOnLights()
        {
            // 1. 處理 Light 組件的亮度漸變
            foreach (var light in AreaLights)
            {
                if (light != null)
                {
                    light.enabled = true;
                    light.DOIntensity(TargetIntensity, FadeDuration).SetEase(Ease.InOutSine);
                }
            }

            // 2. 處理 URP 發光材質的漸變
            foreach (var renderer in EmissiveRenderers)
            {
                if (renderer != null)
                {
                    Material mat = renderer.material;
                    mat.EnableKeyword("_EMISSION");
                    mat.DOColor(EmissionColor, "_EmissionColor", FadeDuration).SetEase(Ease.InOutSine);
                }
            }
        }

        /// <summary>
        /// 強制設定燈光狀態 (無漸變，用於初始化)
        /// </summary>
        private void SetLightsState(bool isOn)
        {
            float intensity = isOn ? TargetIntensity : 0f;
            Color emColor = isOn ? EmissionColor : Color.black;

            foreach (var light in AreaLights)
            {
                if (light != null)
                {
                    light.intensity = intensity;
                    light.enabled = isOn;
                }
            }

            foreach (var renderer in EmissiveRenderers)
            {
                if (renderer != null)
                {
                    Material mat = renderer.material;
                    mat.SetColor("_EmissionColor", emColor);
                    if (isOn) mat.EnableKeyword("_EMISSION");
                    else mat.DisableKeyword("_EMISSION");
                }
            }
        }
    }
}