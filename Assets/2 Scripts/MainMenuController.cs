using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;
using UnityEngine.SceneManagement;
using Unity.VisualScripting;

public class MainMenuController : MonoBehaviour
{
    [Header("主菜單按鈕")]
    public Button startButton;
    public Button optionButton;
    public Button memberListButton;
    public Button quitButton;

    [Header("遊戲設定滑軌")]
    public Slider masterMusicSlider;
    public Slider seSlider;
    public Slider sensitivitySlider;

    [Header("Panels")]
    [Tooltip("Setting 面板")] public GameObject settingPanel;
    [Tooltip("Member List 面板")] public GameObject memberPanel;

    [Header("退出Panel按鈕")]
    [Tooltip("退出遊戲設定")] public Button settingPanelExitButton;
    [Tooltip("退出人員表")] public Button memberPanelExitButton;


    // 輸入系統
    private PlayerControls controls;
    private InputAction navigateAction; //方向?導航?指示?
    //private InputAction submitAction; //選擇
    private InputAction cancelAction; //退出

    private void Awake() //獲取輸入動作
    {
        controls = new PlayerControls();
        navigateAction = controls.UI.Navigate;
        // = controls.UI.Submit;
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

        //加上退出settingPanel和memberPanel的事件綁定   
    }

    private void OnEnable()
    {
        controls.Enable();
        cancelAction.performed += OnCancelAction;
        cancelAction.Enable();
        Debug.Log($"Cancel enabled: {cancelAction.enabled}, bindings: {cancelAction.bindings.Count}");
    }
    private void OnDisable()
    {
        controls.Disable();
        cancelAction.performed -= OnCancelAction;
        cancelAction.Disable();
    }

    private void Update()
    {
        if (settingPanel.activeSelf || memberPanel.activeSelf) return;

        if (EventSystem.current.currentSelectedGameObject == null)
        {
            if (AnyInputPressed() || navigateAction.ReadValue<Vector2>().y != 0)
            {
                EventSystem.current.SetSelectedGameObject(startButton.gameObject);
            }
        }
    }

    private bool AnyInputPressed() //是否偵測到按下鍵盤或手柄
    {
        return Keyboard.current.anyKey.wasPressedThisFrame || Gamepad.current != null;
    }

    private void SetMainMenuButtonsInteractable(bool isActive)
    {
        startButton.gameObject.SetActive(isActive);
        optionButton.gameObject.SetActive(isActive);
        memberListButton.gameObject.SetActive(isActive);
        quitButton.gameObject.SetActive(isActive);
    }

    private void StartGame()
    {
        SceneLoader loader = FindObjectOfType<SceneLoader>();
        if (loader != null)
        {
            loader.LoadScene();
        }
        else
        {
            Debug.LogError("SceneLoader not found in scene!");
        }
    }
    #region 遊戲設定按鈕
    private void OpenSettings() //打開遊戲設定panel，選中退出遊戲設定panel按鈕
    {
        settingPanel.SetActive(true);
        SetMainMenuButtonsInteractable(false); //禁用主菜單按鈕
        StartCoroutine(SetSelectedNextFrame(masterMusicSlider.gameObject)); // 延遲 1 幀再選中 masterMusicSlider
    }
    #endregion
    private IEnumerator SetSelectedNextFrame(GameObject go) 
    {
        yield return null; // 等待一幀
        EventSystem.current.SetSelectedGameObject(go);
    }
    private void OpenMemberPanel() //打開人員表panel，選中退出人員表panel按鈕
    {
        memberPanel.SetActive(true);
        SetMainMenuButtonsInteractable(false); //禁用主菜單按鈕
        //EventSystem.current.SetSelectedGameObject(memberPanelExitButton.gameObject);
    }
    private void QuitGame()
    {
        Application.Quit();
    }
    #region 關掉Panels功能
    private void CloseSettings() //關掉遊戲設定面板，選中遊戲設定按鈕
    {
        settingPanel.SetActive(false);
        SetMainMenuButtonsInteractable(true); //開啟主菜單按鈕
        EventSystem.current.SetSelectedGameObject(optionButton.gameObject);
    }
    private void CloseMemberPanel() //關掉人員表面板，選中人員表按鈕
    {
        memberPanel.SetActive(false);
        SetMainMenuButtonsInteractable(true); //開啟主菜單按鈕
        EventSystem.current.SetSelectedGameObject(memberListButton.gameObject);
    }
    #endregion

    private void OnCancelAction(InputAction.CallbackContext context) //用退出鍵退出 settingPanel 和 memberPanel
    {
        Debug.Log("Cancel pressed!");

        if (settingPanel.activeSelf)
            CloseSettings();
        else if (memberPanel.activeSelf)
            CloseMemberPanel();
        // 否則讓按鈕照自己正常邏輯處理（交給 Unity 自己執行 Button.onClick）
    }
}