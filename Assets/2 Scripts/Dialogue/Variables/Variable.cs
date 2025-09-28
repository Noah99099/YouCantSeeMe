using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Variable
{
    public string name; // 變數名稱 (e.g., "player_gold")
    public float value; // 變數的值 (我們先用 float 來涵蓋整數和浮點數)
}