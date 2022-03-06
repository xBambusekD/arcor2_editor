using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.UI;
using System;
public class HEditorMenuScreen : MonoBehaviour
{

    public GameObject editorSceneMenu;

    public PressableButtonHoloLens2 recalibrateButton;
    public PressableButtonHoloLens2 closeSceneButton;

    public PressableButtonHoloLens2 notificationButton;
    // Start is called before the first frame update
    void Start()
    {
        HideEditorSceneMenu();

        GameManagerH.Instance.OnOpenSceneEditor += OnOpenEditorSceneMenu;
        GameManagerH.Instance.OnOpenProjectEditor += OnOpenEditorSceneMenu;
    
        closeSceneButton.ButtonPressed.AddListener(() => CloseScene());
        notificationButton.ButtonPressed.AddListener(() => HNotificationManager.Instance.ShowNotificationScreen());

  //  #if !UNITY_EDITOR
      //  recalibrateButton.ButtonPressed.AddListener(() => QRTracking.QRCodesManager.Instance.StartQRTracking());
   // #else
  //      recalibrateButton.enabled = false;
  //  #endif

    }

    private void OnOpenEditorSceneMenu(object sender, EventArgs args) {
        ShowEditorSceneMenu();
    }


    public async void CloseScene() {
        if (SceneManagerH.Instance.SceneStarted)
            WebSocketManagerH.Instance.StopScene(false, null);

        bool success = false;
        string message;
        if(GameManagerH.Instance.GetGameState() == GameManagerH.GameStateEnum.SceneEditor){
            (success, message) = await GameManagerH.Instance.CloseScene(true);
        }
        else if (GameManagerH.Instance.GetGameState() == GameManagerH.GameStateEnum.ProjectEditor){
            (success, message) = await GameManagerH.Instance.CloseProject(true);
        }
      //  (bool success, string message) = await GameManagerH.Instance.CloseScene(true);
        if (success) {
            editorSceneMenu.SetActive(false);
     //   #if !UNITY_EDITOR
       //     QRTracking.QRCodesManager.Instance.StopQRTracking();
    //    #endif
            HMainMenuManager.Instance.ShowMainMenuScreen();
        }
    }

    public void ShowEditorSceneMenu(){
        editorSceneMenu.SetActive(true);
    }

    public void HideEditorSceneMenu(){
        editorSceneMenu.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
