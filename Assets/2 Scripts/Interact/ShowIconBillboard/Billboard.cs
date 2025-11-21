// Billboard.cs
using UnityEngine;

/// <summary>
/// 誰旋轉就放誰那裡。
/// 如果父-子結構，腳本放父裡，父就會旋轉。
/// 所以理論上放子裡。
/// </summary>
public class Billboard : MonoBehaviour
{
    private Transform cam; // 相機位置
    private const string CameraTag = "PlayerCamera";

    private void Start()
    {
        cam = GameObject.FindGameObjectWithTag(CameraTag).transform;
    }

    void LateUpdate()
    {
        // 僅旋轉自己以面向攝影機，不會改變父物件
        transform.LookAt(transform.position + cam.rotation * Vector3.forward,
                         cam.rotation * Vector3.up);
    }
}
