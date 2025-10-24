using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CameraInstruction
{
    [Tooltip("代表目標鏡位的空物件的「名字」")]
    public string targetTransformName;
    [Tooltip("攝影機移動到此位置所需的時間")]
    public float transitionDuration = 1.0f;
    [Tooltip("攝影機在此位置停留的時間")]
    public float holdDuration = 2.0f;
}
