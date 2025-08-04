using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Spine;
using Spine.Unity;
using DG.Tweening;

public class LogoRotating : MonoBehaviour
{
    [Header("±ÛÂà¨¤«×")]
    public float rotationSpeed;

    // Update is called once per frame
    void Update()
    {
        gameObject.transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
    }
}
