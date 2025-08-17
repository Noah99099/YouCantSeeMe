using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public enum ViewType { Yang, Yin }

public class ViewManager : MonoBehaviour
{
    [Header("視野UI提示")]
    public GameObject yangUI;
    public GameObject yinUI;
    public static ViewManager Instance { get; private set; }
    public static event Action<ViewType> OnViewChanged;
    public ViewType CurrentView { get; private set; } = ViewType.Yang;
    
    private InputAction viewAction;

    void Awake()
    {
        if (Instance != null && Instance != this) // 如果 Instance 已存在且不是自己
        {
            Destroy(gameObject); // 則銷毀這個重複的物件
            return; // 結束 Awake
        }
        Instance = this; // 將自己設為唯一的 Instance
        DontDestroyOnLoad(gameObject); // 確保切換場景時物件不被銷毀

        yangUI.SetActive(true);
        yinUI.SetActive(false);
    }

    void Start()
    {
        UIInputManager inputManager = FindObjectOfType<UIInputManager>();
        if (inputManager != null && inputManager.PlayerControls != null) // 【核心修正 #1】使用大寫的 'PlayerControls'
        {
            // 【核心修正 #2】直接存取 Player Action Map 和 View Action
            viewAction = inputManager.PlayerControls.Player.View;
            
            if (viewAction != null)
            {
                viewAction.performed += OnViewPerformed;
            }
        }
        else
        {
            Debug.LogError("在 ViewManager 中找不到 UIInputManager 或其 PlayerControls！", this);
        }
    }

    private void OnDestroy() // 【新增】當物件被銷毀時，取消訂閱
    {
        if (viewAction != null)
        {
            viewAction.performed -= OnViewPerformed;
        }
    }

    private void OnViewPerformed(InputAction.CallbackContext context)
    {
        // 【新增】確保只有在玩家模式下才能切換視野
        if(UIInputManager.Instance.IsInPlayerMode)
        {
            ToggleView();
        }
    }

    void ToggleView()
    {
        CurrentView = (CurrentView == ViewType.Yang) ? ViewType.Yin : ViewType.Yang;
        
        OnViewChanged?.Invoke(CurrentView);

        yangUI.SetActive(CurrentView == ViewType.Yang);
        yinUI.SetActive(CurrentView == ViewType.Yin);

        Debug.Log($"Switched to view: {CurrentView}");
    }
}