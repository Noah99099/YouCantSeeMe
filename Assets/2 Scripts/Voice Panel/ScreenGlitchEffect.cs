// 檔案名稱: ScreenGlitchEffect.cs
// (請將此腳本掛載到您的 Main Camera 上，與 Volume 元件放在一起)

using UnityEngine;
using UnityEngine.Rendering; // 引用 Rendering 核心
using UnityEngine.Rendering.Universal; // 引用 URP

[RequireComponent(typeof(Volume))]
public class ScreenGlitchEffect : MonoBehaviour
{
    private Volume glitchVolume;
    private ChromaticAberration chromaticAberration; // 我們要控制的 "色差" 效果

    void Awake()
    {
        glitchVolume = GetComponent<Volume>();
        if (glitchVolume == null || glitchVolume.profile == null)
        {
            Debug.LogError("[ScreenGlitchEffect] 找不到 Volume 或 Volume Profile！", this);
            return;
        }

        // 嘗試從 Profile 中獲取 "色差" 效果
        if (!glitchVolume.profile.TryGet(out chromaticAberration))
        {
            Debug.LogError("[ScreenGlitchEffect] 在 Volume Profile 中找不到 Chromatic Aberration (色差) 效果！請確保您已 'Add Override'。", this);
            return;
        }

        // 遊戲開始時確保特效是關閉的
        StopGlitch();
    }

    /// <summary>
    /// 設置花屏強度 (0.0 到 1.0)
    /// </summary>
    public void SetGlitchIntensity(float intensity)
    {
        if (chromaticAberration == null) return;

        float clampedIntensity = Mathf.Clamp01(intensity);

        // URP 中，參數需要使用 .value 來設置
        chromaticAberration.intensity.value = clampedIntensity;
    }

    /// <summary>
    /// [新] 專門用於 "拾取" 時播放一次性特效
    /// </summary>
    public void PlayOneShotGlitch()
    {
        // PlayerInteraction 中的 Coroutine 會處理 1 秒的持續時間
        SetGlitchIntensity(1.0f); // 設置為最大強度 (您可以調整 1.0)
    }

    /// <summary>
    /// [新] 專門用於停止所有特效
    /// </summary>
    public void StopGlitch()
    {
        SetGlitchIntensity(0.0f);
    }
}