using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class TestShow : MonoBehaviour
{
    public void crossScene() 
    {
        SceneLoader.Instance.LoadScene("Level1");
    }

}
