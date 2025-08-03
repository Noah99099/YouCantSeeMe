using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class GhostWall : MonoBehaviour
{
    [Header("鬼打牆的鏡像設置")]
    public Transform playerTransform;
    [Tooltip("左右")]public bool mirrorX = true;
    //[Tooltip("前後")] public bool mirrorZ = true; //勾了後傳送點就跟空氣牆不同面了 

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) 
        {
            Vector3 wallCenter = transform.position; //空氣牆的中心點
            Vector3 offsetToWall = playerTransform.position - wallCenter; //玩家進到空氣牆的位置 - 空氣牆的中心點

            //如果有開鏡像設置就翻轉
            if (mirrorX) offsetToWall.x *= -1; //往左走變往右走
            //if (mirrorZ) offsetToWall.z *= -1; //結果發現不能用

            offsetToWall.y = 0; //y軸歸零，不然y軸會算到2次(初始+偏移)

            Vector3 mirrorPosition = playerTransform.position + offsetToWall; //最終位置

            playerTransform.position = mirrorPosition; //賦值，重新定位玩家位置

            //旋轉方向反射（旋轉 180 度）
            Vector3 euler = playerTransform.eulerAngles;
            euler.y = (euler.y + 180f) % 360f;
            playerTransform.eulerAngles = euler;
        }
    }
}
