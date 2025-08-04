using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyOnContact : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // 確保目標物件上有 Collider（其實這是必然的，因為能進 OnTriggerEnter 就表示有 Collider）
        if (other != null)
        {
            Destroy(other.gameObject);
        }
    }
}
