// SpawnPoint.cs
using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    [Header("重生點設置")]
    public string pointID = "Default";
    public bool isDefault = false;

    // 在編輯器中可視化
    void OnDrawGizmos()
    {
        Gizmos.color = isDefault ? Color.green : Color.blue;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        Gizmos.DrawIcon(transform.position + Vector3.up, "SpawnPoint.png");
    }
}