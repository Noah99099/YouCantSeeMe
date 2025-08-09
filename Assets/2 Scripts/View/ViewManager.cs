using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEditor.ShaderGraph.Internal.KeywordDependentCollection;
public enum ViewType { Yang, Yin }

public class ViewManager : MonoBehaviour
{
    [Header("視野UI提示")]
    public GameObject yangUI;
    public GameObject yinUI;
    public static ViewManager Instance { get; private set; } //�}����ҹ��
    public static event Action<ViewType> OnViewChanged;
    public ViewType CurrentView { get; private set; } = ViewType.Yang;
    //private PlayerControls controls;
    private InputAction viewAction;

    void Awake()
    {
        if (Instance != null && Instance != this) //�Y Instasce������ �B Instasce�����Ӹ}��
        {
            Destroy(gameObject); //�R���ӹC������
            return; //�����o��if���P�_
        }
        Instance = this; //�Ӹ}����ȵ�Instasce
        DontDestroyOnLoad(gameObject); //�ӹC�����󤣾P��
        //controls = new PlayerControls();

        yangUI.SetActive(true); //�q�{������
        yinUI.SetActive(false);
    }

    void Start()
    {
        // 從場景中唯一的 UIInputManager 獲取共享的 controls
        UIInputManager inputManager = FindObjectOfType<UIInputManager>();
        if (inputManager != null && inputManager.playerControls != null)
        {
            viewAction = inputManager.playerControls.FindActionMap("Player").FindAction("View");
            if (viewAction != null)
            {
                viewAction.performed += OnViewPerformed;
            }
        }
        else
        {
            Debug.LogError("在 ViewManager 中找不到 UIInputManager 或其 playerControls！", this);
        }
    }

    //void OnEnable()
    //{
        //controls.Player.Enable();
        //controls.Player.View.performed += OnViewPerformed;
    //} 
    //void OnDisable()
    //{
        //controls.Player.View.performed -= OnViewPerformed;
        //controls.Player.Disable();
    //}
    private void OnViewPerformed(InputAction.CallbackContext context)
    {
        ToggleView();
    }
    /// <summary>
    /// ��������
    /// </summary>
    void ToggleView()
    {
        CurrentView = (CurrentView == ViewType.Yang) ? ViewType.Yin : ViewType.Yang;
        
        //�s���ƥ�
        OnViewChanged?.Invoke(CurrentView);

        yangUI.SetActive(CurrentView == ViewType.Yang);
        yinUI.SetActive(CurrentView == ViewType.Yin);

        Debug.Log($"Switched to view: {CurrentView}");
    }
}
