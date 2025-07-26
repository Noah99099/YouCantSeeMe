using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEditor.ShaderGraph.Internal.KeywordDependentCollection;
public enum ViewType { Yang, Yin }

public class ViewManager : MonoBehaviour
{
    [Header("視野UI提示")]
    public GameObject yangUI;
    public GameObject yinUI;
    public static ViewManager Instance { get; private set; } //腳本單例實例
    public static event Action<ViewType> OnViewChanged;
    public ViewType CurrentView { get; private set; } = ViewType.Yang;
    private PlayerControls controls;

    void Awake()
    {
        if (Instance != null && Instance != this) //若 Instasce不為空 且 Instasce不為該腳本
        {
            Destroy(gameObject); //刪除該遊戲物件
            return; //結束這個if的判斷
        }
        Instance = this; //該腳本賦值給Instasce
        DontDestroyOnLoad(gameObject); //該遊戲物件不銷毀
        controls = new PlayerControls();

        yangUI.SetActive(true); //默認陽視野
        yinUI.SetActive(false);
    }

    void OnEnable()
    {
        controls.Player.Enable();
        controls.Player.View.performed += _ => ToggleView();
    }

    void OnDisable()
    {
        controls.Player.View.performed -= _ => ToggleView();
        controls.Player.Disable();
    }

    /// <summary>
    /// 切換視野
    /// </summary>
    void ToggleView()
    {
        CurrentView = (CurrentView == ViewType.Yang) ? ViewType.Yin : ViewType.Yang;
        
        //廣播事件
        OnViewChanged?.Invoke(CurrentView);

        yangUI.SetActive(CurrentView == ViewType.Yang);
        yinUI.SetActive(CurrentView == ViewType.Yin);

        Debug.Log($"Switched to view: {CurrentView}");
    }
}
