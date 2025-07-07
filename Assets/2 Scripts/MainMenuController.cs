using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("主菜單按鈕")]
    public Button startButton;
    public Button optionButton;
    public Button memberListButton;
    public Button quitButton;

    [Header("Panels")]
    [Tooltip("Setting 面板")] public GameObject settingPanel;
    [Tooltip("Member List 面板")] public GameObject memberPanel;

    [Header("退出Panel按鈕")]
    [Tooltip("退出遊戲設定")] public Button settingPanelExitButton;
    [Tooltip("退出人員表")] public Button memberPanelExitButton;

    [Header("場景設定")]
    [Tooltip("要加載的場景名稱（必須已加入 Build Settings）")]
    public string sceneToLoad;

    // 輸入系統
    private PlayerControls controls;
    private InputAction navigateAction; //方向?導航?指示?
    private InputAction submitAction; //選擇
    private InputAction cancelAction; //退出

    private void Awake() //獲取輸入動作
    {
        controls = new PlayerControls();
        navigateAction = controls.UI.Navigate;
        submitAction = controls.UI.Submit;
        cancelAction = controls.UI.Cancel;
    }
    private void Start() //初始設置
    {
        EventSystem.current.SetSelectedGameObject(null); //無按鈕標示
        settingPanel.SetActive(false); //不顯示遊戲設定
        memberPanel.SetActive(false); //不顯示人員表

        //綁定按鈕事件，像是直接在inspector手動綁事件那樣
        startButton.onClick.AddListener(StartGame);
        optionButton.onClick.AddListener(OpenSettings);
        memberListButton.onClick.AddListener(OpenMemberPanel);
        quitButton.onClick.AddListener(QuitGame);

        settingPanelExitButton.onClick.AddListener(CloseSettings);
        memberPanelExitButton.onClick.AddListener(CloseMemberPanel);
    }

    private void Update()
    {
        //無法做到初始沒標誌按下鍵盤或滑鼠後才有標誌的功能，但是沒辦法刪代碼所以保留
        //好像又可以?反正是可以留的代碼
        if (EventSystem.current.currentSelectedGameObject == null && AnyInputPressed()) //
        {
            EventSystem.current.SetSelectedGameObject(startButton.gameObject);
        } //

        if (memberPanel.activeSelf || settingPanel.activeSelf) return;

        Vector2 navigation = navigateAction.ReadValue<Vector2>();
        if(navigation.y != 0) 
        {
            if (EventSystem.current.currentSelectedGameObject == null) 
            {
                EventSystem.current.SetSelectedGameObject(startButton.gameObject);
            }
        }
    }

    private bool AnyInputPressed() //是否偵測到按下鍵盤或手柄
    {
        return Keyboard.current.anyKey.wasPressedThisFrame || Gamepad.current != null;
    }

    #region 主菜單按鈕功能
    private void StartGame()
    {
        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            SceneManager.LoadScene(sceneToLoad);
        }
        else
        {
            Debug.LogWarning("請在 Inspector 中指定 sceneToLoad 的場景名稱！");
        }
    }
    private void OpenSettings() //打開遊戲設定panel，選中退出遊戲設定panel按鈕
    {
        settingPanel.SetActive(true);
        EventSystem.current.SetSelectedGameObject(settingPanelExitButton.gameObject);
    }
    private void OpenMemberPanel() //打開人員表panel，選中退出人員表panel按鈕
    {
        memberPanel.SetActive(true);
        EventSystem.current.SetSelectedGameObject(memberPanelExitButton.gameObject);
    }
    private void QuitGame()
    {
        Application.Quit();
    }
    #endregion
    #region 關掉Panels功能
    private void CloseSettings() //關掉遊戲設定面板，選中遊戲設定按鈕
    {
        settingPanel.SetActive(false);
        EventSystem.current.SetSelectedGameObject(optionButton.gameObject);
    }
    private void CloseMemberPanel() //關掉人員表面板，選中人員表按鈕
    {
        memberPanel.SetActive(false);
        EventSystem.current.SetSelectedGameObject(memberListButton.gameObject);
    }
    #endregion

    //輸入系統事件處理(不知道是不是真的有用)
    private void OnSubmit(InputAction.CallbackContext context) { } //目前選取的按鈕會自動處理點擊事件
    //private void OnCancel(InputAction.CallbackContext context) 
    //{
    //    //關閉打開的panel，這個是真的沒用到，
    //    if (settingPanel.activeSelf)
    //        CloseSettings();
    //    else if (memberPanel.activeSelf)
    //        CloseMemberPanel();
    //    else
    //        EventSystem.current.SetSelectedGameObject(null);
    //}
}