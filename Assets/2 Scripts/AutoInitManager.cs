using UnityEngine;

public class AutoInitManager : MonoBehaviour
{
    private void Awake()
    {
        if (SensitivityManager.Instance == null)
        {
            GameObject prefab = Resources.Load<GameObject>("SensitivityManager");
            if (prefab != null)
            {
                Instantiate(prefab);
            }
            else
            {
                Debug.LogError("無法在 Resources 中找到 SensitivityManager.prefab！");
            }
        }
    }
}