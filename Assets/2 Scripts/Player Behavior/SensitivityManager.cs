using UnityEngine;

public class SensitivityManager : MonoBehaviour
{
    public static SensitivityManager Instance { get; private set; }

    [Header("滑鼠靈敏度範圍")]
    public float minSensitivity = 50f;
    public float maxSensitivity = 300f;

    [Header("目前靈敏度")]
    public float mouseSensitivity = 50f; //預設值

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 場景切換不銷毀
        }
        else
        {
            Destroy(gameObject); // 保證只存在一個
        }
    }

    public void SetSensitivity(float value)
    {
        mouseSensitivity = value;
        Debug.Log($"滑鼠靈敏度設為：{mouseSensitivity}");
    }

    public float GetSensitivity()
    {
        return mouseSensitivity;
    }
}
