using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class WaveformVisualizer : MonoBehaviour
{
    [Header("線條設定")]
    public int pointsCount = 100; // 線上的點數量，越多越平滑但越耗效能
    public float width = 300f;    // 波形圖在 UI 上的總寬度
    public float height = 100f;   // 波形圖的最大高度

    [Header("端點固定設定")]
    [Tooltip("兩端有多少個點要逐漸歸零？數值越大邊緣越平滑")]
    public int fadeRange = 5; // [!! 新增 !!] 兩端漸變的點數

    [Header("波形參數 (基礎)")]
    public float baseFreq = 5f;   // 基礎頻率
    public float baseAmp = 5f;    // 基礎振幅 (沒訊號時的微幅波動)
    public float speed = 5f;      // 波動速度

    [Header("波形參數 (受強度影響)")]
    public float maxAmpMultiplier = 10f; // 強度最強時，振幅放大幾倍
    public float noiseFactor = 20f;      // 強度最強時的雜訊(Glitch)程度

    private LineRenderer lr;
    private float currentIntensity = 0f; // 0 = 遠/無訊號, 1 = 超近
    private Vector3[] positions;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
        lr.positionCount = pointsCount;
        lr.useWorldSpace = false; // 重要：讓線條跟隨 UI 物件移動
        positions = new Vector3[pointsCount];
    }

    void Update()
    {
        DrawWaveform();
    }

    // 由外部 (DetectionPoint) 呼叫此方法來設定強度
    public void SetIntensity(float intensity)
    {
        currentIntensity = Mathf.Clamp01(intensity);
    }

    // 重置波形 (關閉時呼叫)
    public void ResetWave()
    {
        currentIntensity = 0f;
    }

    void DrawWaveform()
    {
        float xStart = -width / 2f;
        float step = width / (pointsCount - 1);

        float timeFactor = Time.time * speed;
        float noiseBase = noiseFactor * currentIntensity;
        float ampTotal = baseAmp + (baseAmp * maxAmpMultiplier * currentIntensity);

        for (int i = 0; i < pointsCount; i++)
        {
            float x = xStart + (step * i);

            // 1. 原始波形計算 (和之前一樣)
            float sineValue = Mathf.Sin(timeFactor + x * 20f * (baseFreq + currentIntensity * 5f)); // 這裡我有微調參數適配小尺寸

            float noise = 0f;
            if (currentIntensity > 0.01f)
            {
                noise = UnityEngine.Random.Range(-1f, 1f) * noiseBase;
            }

            float rawY = (sineValue * ampTotal * currentIntensity) + noise;

            // ---------------------------------------------------------
            // [!! 核心修改 !!] 計算邊緣權重 (Edge Weight)
            // ---------------------------------------------------------
            float edgeWeight = 1f; // 預設為 1 (不影響中間)

            // 處理左邊端點
            if (i < fadeRange)
            {
                edgeWeight = (float)i / fadeRange;
            }
            // 處理右邊端點
            else if (i >= pointsCount - fadeRange)
            {
                edgeWeight = (float)(pointsCount - 1 - i) / fadeRange;
            }

            // 將原始 Y 乘上權重
            // 如果是第 0 點，權重是 0 -> Y 就會強制變成 0
            // 如果是中間點，權重是 1 -> Y 保持原樣
            float finalY = rawY * edgeWeight;

            // ---------------------------------------------------------

            // 限制高度
            finalY = Mathf.Clamp(finalY, -height / 2f, height / 2f);

            positions[i].x = x;
            positions[i].y = finalY;
            positions[i].z = 0;
        }

        lr.SetPositions(positions);
    }
}