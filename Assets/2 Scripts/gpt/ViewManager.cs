using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
public enum ViewType { Yang, Yin }

public class ViewManager : MonoBehaviour
{
    [Header("視野UI提示")]
    public GameObject yangUI;
    public GameObject yinUI;
    public static ViewManager Instance { get; private set; }
    public static event Action<ViewType> OnViewChanged;
    public ViewType CurrentView { get; private set; } = ViewType.Yang;

    private PlayerControls controls;

    void Awake()
    {
        Instance = this;
        controls = new PlayerControls();

        yangUI.SetActive(true);
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
