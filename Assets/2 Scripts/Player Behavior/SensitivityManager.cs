using UnityEngine;

public class SensitivityManager : MonoBehaviour
{
    public static SensitivityManager Instance { get; private set; }

    public float minSensitivity = 10f;
    public float maxSensitivity = 100f;

    [Header("靈敏度設定")]
    public float mouseSensitivity = 50f; //滑鼠
    public float gamepadSensitivity = 30f;   // 手柄

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetMouseSensitivity(float value)
    {
        mouseSensitivity = value;
        Debug.Log($"滑鼠靈敏度設為：{mouseSensitivity}");
    }

    public void SetGamepadSensitivity(float value)
    {
        gamepadSensitivity = value;
        Debug.Log($"手柄靈敏度設為：{gamepadSensitivity}");
    }

    public float GetMouseSensitivity()
    {
        return mouseSensitivity;
    }

    public float GetGamepadSensitivity()
    {
        return gamepadSensitivity;
    }
}
