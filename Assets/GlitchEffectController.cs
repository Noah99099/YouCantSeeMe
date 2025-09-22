using UnityEngine;
using UnityEngine.UI;

public class GlitchEffectController : MonoBehaviour
{
    [Header("Glitch UI Image")]
    [SerializeField] private Image glitchImage;

    [Header("玩家與距離參數")]
    public Transform playerTransform; // 一般設為 Camera.main.transform
    [SerializeField] private float maxDistance = 5f;

    private InteractableVoice currentVoice;

    private void Awake()
    {
        if (glitchImage == null)
        {
            glitchImage = GetComponent<Image>();
        }
        SetIntensity(0f); // 初始隱藏
        glitchImage.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (currentVoice != null && playerTransform != null)
        {
            float distance = Vector3.Distance(playerTransform.position, currentVoice.transform.position);
            UpdateGlitchEffect(distance, maxDistance);
        }
        else
        {
            SetIntensity(0f); // 沒有物件就隱藏
        }
    }

    public void SetCurrentVoice(InteractableVoice voice) //VoiceItemInteractionManager 調用方法
    {
        currentVoice = voice;
        if (voice != null) 
        {
            glitchImage.gameObject.SetActive(true);
            SetIntensity(1f); // 拿到物件就立即顯示花屏
        }      
        else 
        {
            SetIntensity(0f);
            glitchImage.gameObject.SetActive(false);
        }
            
    }

    /// <summary>
    /// 0 = 沒有特效，1 = 最強特效
    /// </summary>
    public void SetIntensity(float intensity)
    {
        if (glitchImage == null) return;
        Color c = glitchImage.color;
        c.a = Mathf.Clamp01(intensity); // alpha 控制強度
        glitchImage.color = c;
    }

    /// <summary>
    /// 距離越近，特效越明顯
    /// </summary>
    public void UpdateGlitchEffect(float distance, float maxDistance)
    {
        float intensity = 1f - Mathf.Clamp01(distance / maxDistance);
        SetIntensity(intensity);
    }

    public void HideGlitch()
    {
        SetIntensity(0f);
        glitchImage.gameObject.SetActive(false);
        currentVoice = null; // 清除物件
    }
}
