using UnityEngine;

public class GameManagers : MonoBehaviour
{
    [SerializeField] private GlobalVariableDatabase globalDatabase;
    private static GameManagers _instance;
    private void Awake()
    {
        if (globalDatabase != null)
        {
            globalDatabase.ResetVariables();
        }

        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }
}