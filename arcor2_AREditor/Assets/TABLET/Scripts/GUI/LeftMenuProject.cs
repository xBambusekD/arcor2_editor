using System;
using UnityEngine.UI;
using Base;
using System.Collections;
using IO.Swagger.Model;
using UnityEngine;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using static Base.GameManager;
using UnityEngine.Events;

public class LeftMenuProject : LeftMenu
{

    public ButtonWithTooltip SetActionPointParentButton, AddActionButton, AddActionButton2, RunButton, RunButton2,
        AddConnectionButton, AddConnectionButton2, BuildPackageButton, AddActionPointUsingRobotButton, AddActionPointButton,
        AddActionPointButton2, AddNewObjectButton;

    public InputDialog InputDialog;
    public AddNewActionDialog AddNewActionDialog;

    private string selectAPNameWhenCreated = "";
    public UnityAction<ActionPoint3D> ActionCb;

    public ActionPoint3D APToRemoveOnCancel;

    public string ApToAddActionName = null;

    public GameObject ActionPicker, MeshPicker;
    protected override void Update() {
        base.Update();
        if (ProjectManager.Instance.ProjectMeta != null)
            EditorInfo.text = "Project: \n" + ProjectManager.Instance.ProjectMeta.Name;
    }


    public enum Mode {
        Normal,
        AddAction,
        Remove,
        Move,
        Run,
        Connections,
    }

    private Mode currentMode = Mode.Normal;

    public Mode CurrentMode {
        get => currentMode;
        private set {
            currentMode = value;
            //SelectorMenuButton.interactable = currentMode == Mode.Normal;
            /*switch (value) {
                case Mode.
            }*/

        }
    }


    private const string SET_ACTION_POINT_PARENT_LABEL = "Upravit hierarchii";
    private const string ADD_ACTION_LABEL = "Add action";
    private const string ADD_CONNECTION_LABEL = "Add connection";
    private const string EDIT_CONNECTION_LABEL = "Edit connection";
    private const string RUN_ACTION_LABEL = "Spustit akci";
    private const string RUN_ACTION_POINT_LABEL = "Přesunout robota na tuto pozici";
    private const string RUN_ACTION_OR_PACKAGE_LABEL = "Spustit akci nebo program";
    private const string RUN_TEMP_PACKAGE_LABEL = "Spustit program";
    private const string ADD_ACTION_POINT_GLOBAL_LABEL = "Add global action point";
    private const string ADD_ACTION_POINT_LABEL = "Add action point";
    private const string ACTION_POINT_AIMING_LABEL = "Open action point aiming menu";
    private const string ADD_ACTION_POINT_USING_ROBOT_LABEL = "Add action point using robot";
    private const string ADD_NEW_OBJECT_LABEL = "Přidat nový objekt";
    

    protected override void Start() {
#if !AR_ON
        AddActionPointButton.SetInteractivity(true);
        AddActionPointButton2.SetInteractivity(true);
#endif
        Base.ProjectManager.Instance.OnProjectSavedSatusChanged += OnProjectSavedStatusChanged;
        Base.GameManager.Instance.OnOpenProjectEditor += OnOpenProjectEditor;

        SceneManager.Instance.OnSceneStateEvent += OnSceneStateEvent;

        GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
        GameManager.Instance.OnEditorStateChanged += OnEditorStateChanged;
        SelectorMenu.Instance.OnObjectSelectedChangedEvent += OnObjectSelectedChangedEvent;

        GameManager.Instance.OnActionExecution += OnActionExecutionEvent;
        GameManager.Instance.OnActionExecutionCanceled += OnActionExecutionEvent;
        GameManager.Instance.OnActionExecutionFinished += OnActionExecutionEvent;


        CurrentMode = Mode.AddAction;
        RightButtonsMenu.Instance.SetActionMode();
        SetActiveSubmenu(LeftMenuSelection.None);
    }


    private void OnActionExecutionEvent(object sender, EventArgs args) {
        UpdateBtns();
    }

    protected override void Awake() {
        base.Awake();
       
    }

    protected override void OnSceneStateEvent(object sender, SceneStateEventArgs args) {
        if (GameManager.Instance.GetGameState() == GameManager.GameStateEnum.ProjectEditor) {
            base.OnSceneStateEvent(sender, args);
            UpdateBtns();
        }

    }

    protected void OnEnable() {
        ProjectManager.Instance.OnActionPointAddedToScene += OnActionPointAddedToScene;

    }

    protected void OnDisable() {
        ProjectManager.Instance.OnActionPointAddedToScene -= OnActionPointAddedToScene;
    }

    private void OnActionPointAddedToScene(object sender, ActionPointEventArgs args) {
        /*if (!string.IsNullOrEmpty(selectAPNameWhenCreated) && args.ActionPoint.GetName().Contains(selectAPNameWhenCreated)) {
            SelectorMenu.Instance.SetSelectedObject(args.ActionPoint, true);
            selectAPNameWhenCreated = "";
            RenameClick(true);
        }*/
        /*
        if (ApToAddActionName != null && ActionCb != null && args.ActionPoint.GetName() == ApToAddActionName) {
            ApToAddActionName = null;
            ActionCb.Invoke((ActionPoint3D) args.ActionPoint);
            ActionCb = null;
        }*/
    }

    public void StartAddActionMode() {

        if (!ActionModeButton.GetComponent<Image>().enabled) { //other menu/dialog opened
                                                               //close all other opened menus/dialogs and takes care of red background of buttons
            SetActiveSubmenu(LeftMenuSelection.None, true, true);
        }
        //if (ActionModeButton.GetComponent<Image>().enabled) {
            //AddActionModeBtn.GetComponent<Image>().enabled = false;
            //RestoreSelector();
            //ActionPicker.SetActive(false);

            //CurrentMode = Mode.Normal;
            //SelectorMenu.Instance.Active = true;
            //SetActiveSubmenu(LeftMenuSelection.None, true, false);
        //} else {
            ActionModeButton.GetComponent<Image>().enabled = true;
            RightButtonsMenu.Instance.gameObject.SetActive(true);
            SelectorMenu.Instance.gameObject.SetActive(false);
            RightButtonsMenu.Instance.SetActionMode();
            if (currentMode == Mode.Normal)

                CurrentMode = Mode.AddAction;
            SelectorMenuButton.GetComponent<Image>().enabled = false;
        //}

    }

    public void StartRemoveMode() {

        if (!RemoveModeButton.GetComponent<Image>().enabled) { //other menu/dialog opened
            SetActiveSubmenu(LeftMenuSelection.None, true, true); //close all other opened menus/dialogs and takes care of red background of buttons
        }

        /*if (RemoveModeButton.GetComponent<Image>().enabled) {
            //RemoveModeBtn.GetComponent<Image>().enabled = false;
            //RestoreSelector();

            //CurrentMode = Mode.Normal;
            SelectorMenu.Instance.Active = true;
            SetActiveSubmenu(LeftMenuSelection.None, true, false);
        } else {*/
            RemoveModeButton.GetComponent<Image>().enabled = true;
            RightButtonsMenu.Instance.gameObject.SetActive(true);
            SelectorMenu.Instance.gameObject.SetActive(false);
            RightButtonsMenu.Instance.SetRemoveMode();
            CurrentMode = Mode.Remove;
            SelectorMenuButton.GetComponent<Image>().enabled = false;
        //}

    }

    public void StartMoveMode() {
       SetActiveSubmenu(LeftMenuSelection.None, true, true); //close all other opened menus/dialogs and takes care of red background of buttons
       
        /*if (MoveModeButton.GetComponent<Image>().enabled) {
            //MoveModeButton.GetComponent<Image>().enabled = false;
            //RestoreSelector();

            //CurrentMode = Mode.Normal;
            SelectorMenu.Instance.Active = true;
            SetActiveSubmenu(LeftMenuSelection.None, true, false);
        } else {*/
            MoveModeButton.GetComponent<Image>().enabled = true;
            RightButtonsMenu.Instance.gameObject.SetActive(true);
            SelectorMenu.Instance.gameObject.SetActive(false);
            RightButtonsMenu.Instance.SetMoveMode();
            CurrentMode = Mode.Move;
            SelectorMenuButton.GetComponent<Image>().enabled = false;
        //}

    }

    public void StartRunMode() {
        SetActiveSubmenu(LeftMenuSelection.None, true, true); //close all other opened menus/dialogs and takes care of red background of buttons
        
        /*if (RunModeButton.GetComponent<Image>().enabled) {
            //RunModeButton.GetComponent<Image>().enabled = false;
            //RestoreSelector();

            //CurrentMode = Mode.Normal;
            SelectorMenu.Instance.Active = true;
            SetActiveSubmenu(LeftMenuSelection.None, true, false);
        } else {*/
            RunModeButton.GetComponent<Image>().enabled = true;
            RightButtonsMenu.Instance.gameObject.SetActive(true);
            SelectorMenu.Instance.gameObject.SetActive(false);
            RightButtonsMenu.Instance.SetRunMode();
            CurrentMode = Mode.Run;
            SelectorMenuButton.GetComponent<Image>().enabled = false;
        //}

    }

    public void StartConnectionsMode() {
        SetActiveSubmenu(LeftMenuSelection.None, true, true); //close all other opened menus/dialogs and takes care of red background of buttons
        
        /*if (ConnectionModeButton.GetComponent<Image>().enabled) {
            ConnectionModeButton.GetComponent<Image>().enabled = false;
            //RestoreSelector();

            SelectorMenu.Instance.Active = true;
            SetActiveSubmenu(LeftMenuSelection.None, true, false);
        } else {*/
            ConnectionModeButton.GetComponent<Image>().enabled = true;
            RightButtonsMenu.Instance.gameObject.SetActive(true);
            SelectorMenu.Instance.gameObject.SetActive(false);
            RightButtonsMenu.Instance.SetConnectionsMode();
            CurrentMode = Mode.Connections;
            SelectorMenuButton.GetComponent<Image>().enabled = false;
       // }

    }

    public void SelectorMenuClick() {
       //SetActiveSubmenu(CurrentSubmenuOpened, true, true); //close all other opened menus/dialogs and takes care of red background of buttons
        


        //if (!SelectorMenuButton.GetComponent<Image>().enabled) {
            SelectorMenu.Instance.Active = true;
            SetActiveSubmenu(LeftMenuSelection.Utility, true, true);
            SelectorMenu.Instance.gameObject.SetActive(true);
            SelectorMenuButton.GetComponent<Image>().enabled = true;
        RightButtonsMenu.Instance.gameObject.SetActive(false);
            /*
            if (TransformMenu.Instance.CanvasGroup.alpha > 0) {
                TransformMenu.Instance.Hide();
            } else {
                SelectorMenuButton.GetComponent<Image>().enabled = false;
                RightButtonsMenu.Instance.gameObject.SetActive(true);
                SelectorMenu.Instance.gameObject.SetActive(false);
                SetActiveSubmenu(LeftMenuSelection.None);
                SelectorMenu.Instance.DeselectObject();
            }*/
        /*} else {
            SelectorMenuButton.GetComponent<Image>().enabled = true;
            RightButtonsMenu.Instance.gameObject.SetActive(false);
            SelectorMenu.Instance.gameObject.SetActive(true);
            SetActiveSubmenu(LeftMenuSelection.Utility);
        }*/
    }


    protected async override Task UpdateBtns(InteractiveObject obj) {
        try {
            if (CanvasGroup.alpha == 0) {
                previousUpdateDone = true;
                return;
            }
        
            await base.UpdateBtns(obj);
#if UNITY_ANDROID && AR_ON
            if (!CalibrationManager.Instance.Calibrated && !TrackingManager.Instance.IsDeviceTracking()) {
                SetActionPointParentButton.SetInteractivity(false, $"{SET_ACTION_POINT_PARENT_LABEL}\n(AR not calibrated)");
                AddActionButton.SetInteractivity(false, $"{ADD_ACTION_LABEL}\n(AR not calibrated)");
                AddActionButton2.SetInteractivity(false, $"{ADD_ACTION_LABEL}\n(AR not calibrated)");
                AddConnectionButton.SetInteractivity(false, $"{ADD_CONNECTION_LABEL}\n(AR not calibrated)");
                AddConnectionButton2.SetInteractivity(false, $"{ADD_CONNECTION_LABEL}\n(AR not calibrated)");
                RunButton.SetInteractivity(false, $"{RUN_ACTION_OR_PACKAGE_LABEL}\n(AR not calibrated)");
                RunButton2.SetInteractivity(false, $"{RUN_ACTION_OR_PACKAGE_LABEL}\n(AR not calibrated)");
                AddActionPointButton.SetInteractivity(false, $"{ADD_ACTION_POINT_LABEL}\n(AR not calibrated)");
                AddActionPointButton2.SetInteractivity(false, $"{ADD_ACTION_POINT_LABEL}\n(AR not calibrated)");
                CopyButton.SetInteractivity(false, $"{COPY_LABEL}\n(AR not calibrated");
                //ActionPointAimingMenuButton.SetInteractivity(false, $"{ACTION_POINT_AIMING_LABEL}\n(AR not calibrated)");
                AddNewObjectButton.SetInteractivity(false, $"{ADD_NEW_OBJECT_LABEL}\n(AR not calibrated)");
            }
            else
#endif
            AddNewObjectButton.SetInteractivity(false, $"{ADD_NEW_OBJECT_LABEL}\n(no object could be selected)");
            if (requestingObject || obj == null) {
                SetActionPointParentButton.SetInteractivity(false, $"{SET_ACTION_POINT_PARENT_LABEL}\n(není vybraný žádný akční bod)");
                AddActionButton.SetInteractivity(false, $"{ADD_ACTION_LABEL}\n(no action point is selected)");
                AddActionButton2.SetInteractivity(false, $"{ADD_ACTION_LABEL}\n(no action point is selected)");
                AddConnectionButton.SetInteractivity(false, $"{ADD_CONNECTION_LABEL}\n(no input / output is selected)");
                AddConnectionButton2.SetInteractivity(false, $"{ADD_CONNECTION_LABEL}\n(no input / output is selected)");
                RunButton.SetInteractivity(false, $"{RUN_ACTION_OR_PACKAGE_LABEL}\n(vyberte akci, akční bod nebo START)");
                RunButton2.SetInteractivity(false, RunButton.GetAlternativeDescription());
                AddActionPointButton.SetInteractivity(true);
                AddActionPointButton2.SetInteractivity(true);
                AddActionPointButton.SetDescription(ADD_ACTION_POINT_GLOBAL_LABEL);
                AddActionPointButton2.SetDescription(ADD_ACTION_POINT_GLOBAL_LABEL);
                CopyButton.SetInteractivity(false, $"{COPY_LABEL}\n(vybraný objekt nemůže být duplikován)");
                AddNewObjectButton.SetInteractivity(true);
                //ActionPointAimingMenuButton.SetInteractivity(false, $"{ACTION_POINT_AIMING_LABEL}\n(no action point selected)");
            } else if (obj.IsLocked && obj.LockOwner != LandingScreen.Instance.GetUsername()) {
                SetActionPointParentButton.SetInteractivity(false, $"{SET_ACTION_POINT_PARENT_LABEL}\n(object is used by {obj.LockOwner})");
                AddConnectionButton.SetInteractivity(false, $"{ADD_CONNECTION_LABEL}\n(object is used by {obj.LockOwner})");
                AddConnectionButton2.SetInteractivity(false, $"{ADD_CONNECTION_LABEL}\n(object is used by {obj.LockOwner})");
                RunButton.SetInteractivity(false, $"{RUN_ACTION_OR_PACKAGE_LABEL}\n(object is used by {obj.LockOwner})");
                RunButton2.SetInteractivity(false, $"{RUN_ACTION_OR_PACKAGE_LABEL}\n(object is used by {obj.LockOwner})");
                AddActionButton.SetInteractivity(false, $"{ADD_ACTION_POINT_LABEL}\n(object is used by {obj.LockOwner})");
                AddActionButton2.SetInteractivity(false, $"{ADD_ACTION_POINT_LABEL}\n(object is used by {obj.LockOwner})");
                CopyButton.SetInteractivity(false, $"{COPY_LABEL}\n(object is used by {obj.LockOwner})");
                //ActionPointAimingMenuButton.SetInteractivity(false, $"{ACTION_POINT_AIMING_LABEL}\n(object is used by {obj.LockOwner})");
            } else {
                SetActionPointParentButton.SetInteractivity(obj is ActionPoint3D, $"{SET_ACTION_POINT_PARENT_LABEL}\n(vybraný objekt není akční bod)");
                if (obj is ActionPoint3D) {
                    AddActionButton.SetInteractivity(ProjectManager.Instance.AnyAvailableAction, $"{ADD_ACTION_LABEL}\n(no actions available)");
                    AddActionButton2.SetInteractivity(ProjectManager.Instance.AnyAvailableAction, $"{ADD_ACTION_LABEL}\n(no actions available)");
                } else {
                    AddActionButton.SetInteractivity(false, $"{ADD_ACTION_LABEL}\n(selected object is not action point)");
                    AddActionButton2.SetInteractivity(false, $"{ADD_ACTION_LABEL}\n(selected object is not action point)");
                }
               // ActionPointAimingMenuButton.SetInteractivity(obj is ActionPoint3D || obj is APOrientation, $"{ACTION_POINT_AIMING_LABEL}\n(selected object is not action point or orientation)");
                if (obj is IActionPointParent) {
                    AddActionPointButton.SetDescription($"Add AP relative to {obj.GetName()}");
                    AddActionPointButton.SetInteractivity(true);
                } else {
                    AddActionPointButton.SetInteractivity(false, $"{ADD_ACTION_POINT_LABEL}\n(vybraný objekt nemůže být předek akčního bodu");
                }
                AddActionPointButton2.SetInteractivity(AddActionPointButton.IsInteractive(), $"{ADD_ACTION_POINT_LABEL}\n({AddActionPointButton.GetAlternativeDescription()})");
                AddActionPointButton2.SetDescription(AddActionPointButton.GetDescription());
                if (obj is ActionPoint3D) {
                    CopyButton.SetInteractivity(false, $"{COPY_LABEL}\n(kontroluji...)");
                    WebsocketManager.Instance.CopyActionPoint(obj.GetId(), null, obj.GetName(), CopyActionPointDryRunCallback, true);
                } else {
                    CopyButton.SetInteractivity(obj is Base.Action && !(obj is StartEndAction), $"{COPY_LABEL}\n(vybraný objekt nemůže být duplikován)");
                }
                if (!MainSettingsMenu.Instance.ConnectionsSwitch.IsOn()) {
                    AddConnectionButton.SetInteractivity(false, $"{ADD_CONNECTION_LABEL}\n(connections are hidden)");
                    AddConnectionButton2.SetInteractivity(false, $"{ADD_CONNECTION_LABEL}\n(connections are hidden)");
                } else {
                    if (obj is Base.Action) {
                        if (obj is EndAction) {
                            AddConnectionButton.SetInteractivity(false, $"{ADD_CONNECTION_LABEL}\n(poslední akce už nemůže být k ničemu připojena)");
                        } else {
                            AddConnectionButton.SetInteractivity(true);
                        }
                        AddConnectionButton2.SetInteractivity(AddConnectionButton.IsInteractive(), AddConnectionButton.GetAlternativeDescription());
                    }
                    

                }
                string runBtnInteractivity = null;

                if (obj.GetType() == typeof(Action3D)) {
                    if (!SceneManager.Instance.SceneStarted)
                        runBtnInteractivity = "offline";
                    else if (!string.IsNullOrEmpty(GameManager.Instance.ExecutingAction)) {
                        string actionName = ProjectManager.Instance.GetAction(GameManager.Instance.ExecutingAction).GetName();
                        runBtnInteractivity = $"akce '{actionName}' právě běží";
                    }
                    RunButton.SetDescription(RUN_ACTION_LABEL);
                    RunButton2.SetDescription(RUN_ACTION_LABEL);
                    RunButton.SetInteractivity(string.IsNullOrEmpty(runBtnInteractivity), $"{RUN_ACTION_LABEL}\n({runBtnInteractivity})");
                    RunButton2.SetInteractivity(string.IsNullOrEmpty(runBtnInteractivity), $"{RUN_ACTION_LABEL}\n({runBtnInteractivity})");
                } else if (obj.GetType() == typeof(StartAction)) {
                    if (!ProjectManager.Instance.ProjectMeta.HasLogic) {
                        runBtnInteractivity = "project without logic could not be started from editor";
                    } 
                    RunButton.SetDescription(RUN_TEMP_PACKAGE_LABEL);
                    RunButton2.SetDescription(RUN_TEMP_PACKAGE_LABEL);
                    RunButton.SetInteractivity(string.IsNullOrEmpty(runBtnInteractivity), $"{RUN_TEMP_PACKAGE_LABEL}\n({runBtnInteractivity})");
                    RunButton2.SetInteractivity(string.IsNullOrEmpty(runBtnInteractivity), $"{RUN_TEMP_PACKAGE_LABEL}\n({runBtnInteractivity})");
                } else if (obj is ActionPoint3D) {                    
                    RunButton.SetDescription(RUN_ACTION_POINT_LABEL);
                } else {
                    runBtnInteractivity = "vyberte akci, akční bod nebo START";
                    RunButton.SetInteractivity(false, $"{RUN_ACTION_OR_PACKAGE_LABEL}\n({runBtnInteractivity})");
                    RunButton2.SetInteractivity(false, $"{RUN_ACTION_OR_PACKAGE_LABEL}\n({runBtnInteractivity})");
                }

                
            }

            if (!SceneManager.Instance.SceneStarted) {
                AddActionPointUsingRobotButton.SetInteractivity(false, $"{ADD_ACTION_POINT_USING_ROBOT_LABEL}\n(offline");
            } else {
                AddActionPointUsingRobotButton.SetInteractivity(true);
            }
        } finally {
            previousUpdateDone = true;
        }
    }

    public override void DeactivateAllSubmenus(bool unlock = true) {
        base.DeactivateAllSubmenus(unlock);

        AddActionButton.GetComponent<Image>().enabled = false;
        AddActionButton2.GetComponent<Image>().enabled = false;
        //ActionPointAimingMenuButton.GetComponent<Image>().enabled = false;
        if (ActionPickerMenu.Instance.IsVisible())
            ActionPickerMenu.Instance.Hide(unlock);
        if (ActionParametersMenu.Instance.IsVisible())
            ActionParametersMenu.Instance.Hide();
        if (ActionPointAimingMenu.Instance.IsVisible())
            _ = ActionPointAimingMenu.Instance.Hide(unlock);
    }

    private void OnOpenProjectEditor(object sender, EventArgs eventArgs) {
        UpdateBtns();
    }

    

    public void SaveProject() {
        SaveButton.SetInteractivity(false, "Ukládám projekt...");
        Base.GameManager.Instance.SaveProject();        
    }

    public async void BuildPackage(string name) {
        try {
            await Base.GameManager.Instance.BuildPackage(name);
            InputDialog.Close();
            Notifications.Instance.ShowToastMessage("Package was built sucessfully.");
        } catch (Base.RequestFailedException ex) {

        }

    }


    public async void RunProject() {
        GameManager.Instance.ShowLoadingScreen("Spouštím program...", true);
        try {
            await Base.WebsocketManager.Instance.TemporaryPackage();
            MainMenu.Instance.Close();
        } catch (RequestFailedException ex) {
            Base.Notifications.Instance.ShowNotification("Failed to run temporary package", "");
            Debug.LogError(ex);
            GameManager.Instance.HideLoadingScreen(true);
        }
    }

    public void ShowBuildPackageDialog() {
        InputDialog.Open("Build package",
                         "",
                         "Package name",
                         Base.ProjectManager.Instance.ProjectMeta.Name + DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss"),
                         () => BuildPackage(InputDialog.GetValue()),
                         () => InputDialog.Close());
    }


    private void OnProjectSavedStatusChanged(object sender, EventArgs e) {
       UpdateBuildAndSaveBtns();
    }
    

    public override async void UpdateBuildAndSaveBtns() {
        if (GameManager.Instance.GetGameState() != GameManager.GameStateEnum.ProjectEditor)
            return;
        if (CurrentSubmenuOpened != LeftMenuSelection.Home)
            return;
        
        BuildPackageButton.SetInteractivity(false, $"Build package\n(checking...)");
        SaveButton.SetInteractivity(false, "Save project\n(checking...)");
        CloseButton.SetInteractivity(false, "Close project\n(checking...)");
        if (SceneManager.Instance.SceneStarted) {
            WebsocketManager.Instance.StopScene(true, StopSceneCallback);
        } else {
            CloseButton.SetInteractivity(true);
        }

        if (!ProjectManager.Instance.ProjectChanged) {
            BuildPackageButton.SetInteractivity(true);            
            SaveButton.SetInteractivity(false, "Save project\n(there are no unsaved changes)");
        } else {
            WebsocketManager.Instance.SaveProject(true, SaveProjectCallback);
            BuildPackageButton.SetInteractivity(false, "Build package\n(there are unsaved changes on project)");
            //RunButton.SetInteractivity(false, "There are unsaved changes on project");
            //RunButton2.SetInteractivity(false, "There are unsaved changes on project");
        }
    }

    private void StopSceneCallback(string _, string data) {
        CloseProjectResponse response = JsonConvert.DeserializeObject<CloseProjectResponse>(data);
        if (response.Messages != null) {
            CloseButton.SetInteractivity(response.Result, response.Messages.FirstOrDefault());
        } else {
            CloseButton.SetInteractivity(response.Result);
        }
    }

    protected void SaveProjectCallback(string _, string data) {
        SaveProjectResponse response = JsonConvert.DeserializeObject<SaveProjectResponse>(data);
        if (response.Messages != null) {
            SaveButton.SetInteractivity(response.Result, response.Messages.FirstOrDefault());
        } else {
            SaveButton.SetInteractivity(response.Result);
        }
    }
    private void CopyActionPointDryRunCallback(string _, string data) {
        CopyActionPointResponse response = JsonConvert.DeserializeObject<CopyActionPointResponse>(data);
        if (response.Result) {
            CopyButton.SetInteractivity(true);
        } else {
            CopyButton.SetInteractivity(false, response.Messages.FirstOrDefault());
        }
    }

    /*
    protected void CloseProjectCallback(string nothing, string data) {
        CloseProjectResponse response = JsonConvert.DeserializeObject<CloseProjectResponse>(data);
        if (response.Messages != null) {
            CloseButton.SetInteractivity(response.Result, response.Messages.FirstOrDefault());
        } else {
            CloseButton.SetInteractivity(response.Result);
        }
    }*/

    public override async void CopyObjectClick() {
        InteractiveObject selectedObject = SelectorMenu.Instance.GetSelectedObject();
        if (selectedObject is null)
            return;
        if (selectedObject.GetType() == typeof(ActionPoint3D)) {
            selectAPNameWhenCreated = selectedObject.GetName() + "_copy";            
            WebsocketManager.Instance.CopyActionPoint(selectedObject.GetId(), Sight.Instance.CreatePoseInTheView().Position, selectedObject.GetName(), CopyActionPointCallback);            
        } else if (selectedObject is Base.Action action) {
            ActionPickerMenu.Instance.DuplicateAction(action);
        }
    }

    private void CopyActionPointCallback(string actionPointName, string data) {
        CopyActionPointResponse response = JsonConvert.DeserializeObject<CopyActionPointResponse>(data);
        Base.ActionPoint ap = ProjectManager.Instance.GetactionpointByName(actionPointName);
        if (response.Result) {
            Notifications.Instance.ShowToastMessage($"Action point {actionPointName} was duplicated");
        } else {
            Notifications.Instance.ShowNotification("Failed to duplicate action point", response.Messages.FirstOrDefault());
        }
    }

    public async void AddConnectionClick() {
        if (SelectorMenu.Instance.GetSelectedObject() is Base.Action action) {
            action.AddConnection();            
        }
    }


    public async void AddActionClick() {
        //was clicked the button in favorites or settings submenu?
        Button clickedButton = AddActionButton.Button;
        if (CurrentSubmenuOpened == LeftMenuSelection.Favorites) {
            clickedButton = AddActionButton2.Button;
        }

        if (!SelectorMenu.Instance.gameObject.activeSelf && !clickedButton.GetComponent<Image>().enabled) { //other menu/dialog opened
            SetActiveSubmenu(CurrentSubmenuOpened, unlock: false); //close all other opened menus/dialogs and takes care of red background of buttons
        }

        if (clickedButton.GetComponent<Image>().enabled) {
            clickedButton.GetComponent<Image>().enabled = false;
            SelectorMenu.Instance.gameObject.SetActive(true);
            ActionPickerMenu.Instance.Hide();
        } else {
            if (await ActionPickerMenu.Instance.Show((Base.ActionPoint) selectedObject)) {
                clickedButton.GetComponent<Image>().enabled = true;
                SelectorMenu.Instance.gameObject.SetActive(false);
            } else {
                Notifications.Instance.ShowNotification("Failed to open action picker", "Could not lock action point");
            }
            
        }
    }

    public void AddActionPointClick() {
        SetActiveSubmenu(CurrentSubmenuOpened);
        if (selectedObject is IActionPointParent parent) {
            CreateActionPoint(ProjectManager.Instance.GetFreeAPName(parent.GetName()), parent);
        } else {
            CreateActionPoint(ProjectManager.Instance.GetFreeAPName("global"), default);
        }
    }

    public void AddActionPointUsingRobotClick() {
        string armId = null;
        if (!SceneManager.Instance.IsRobotAndEESelected()) {
            OpenRobotSelector(AddActionPointUsingRobotClick);
            return;
        }
        if (SceneManager.Instance.SelectedRobot.MultiArm())
            armId = SceneManager.Instance.SelectedArmId;
        CreateGlobalActionPointUsingRobot(ProjectManager.Instance.GetFreeAPName("global"),
            SceneManager.Instance.SelectedRobot.GetId(),
            SceneManager.Instance.SelectedEndEffector.EEId,
            armId);
    }

    /// <summary>
    /// Creates new action point
    /// </summary>
    /// <param name="name">Name of the new action point</param>
    /// <param name="parentId">Id of AP parent. Global if null </param>
    private async void CreateActionPoint(string name, IActionPointParent parentId = null) {
        Debug.Assert(!string.IsNullOrEmpty(name));
        Debug.Assert(parentId != null);
        selectAPNameWhenCreated = name;
        bool result = await GameManager.Instance.AddActionPoint(name, parentId);
        if (result)
            InputDialog.Close();
        else
            selectAPNameWhenCreated = "";
    }


    private void CreateGlobalActionPointUsingRobot(string name, string robotId, string eeId, string armId) {
        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(robotId) || string.IsNullOrEmpty(eeId)) {
            Notifications.Instance.ShowNotification("Failed to create new AP", "Some required parameter is missing");
            return;
        }

        GameManager.Instance.ShowLoadingScreen("Adding AP...");

        WebsocketManager.Instance.AddActionPointUsingRobot(name, eeId, robotId, false, AddActionPointUsingRobotCallback, armId);
        selectAPNameWhenCreated = name;
    }


    protected void AddActionPointUsingRobotCallback(string nothing, string data) {
        AddApUsingRobotResponse response = JsonConvert.DeserializeObject<AddApUsingRobotResponse>(data);
        GameManager.Instance.HideLoadingScreen();
        if (response.Result) {
            Notifications.Instance.ShowToastMessage("Action point created");
        } else {
            Notifications.Instance.ShowNotification("Failed to add action point", response.Messages.FirstOrDefault());
        }
    }
    /*
    public async void ActionPointAimingClick() {
        if (!SelectorMenu.Instance.gameObject.activeSelf && !ActionPointAimingMenuButton.GetComponent<Image>().enabled) { //other menu/dialog opened
            SetActiveSubmenu(CurrentSubmenuOpened, unlock: false); //close all other opened menus/dialogs and takes care of red background of buttons
        }

        if (ActionPointAimingMenuButton.GetComponent<Image>().enabled) {
            ActionPointAimingMenuButton.GetComponent<Image>().enabled = false;
            SelectorMenu.Instance.gameObject.SetActive(true);
            _ = ActionPointAimingMenu.Instance.Hide(true);
        } else {
            bool opened = false;

            if (selectedObject is ActionPoint3D actionPoint) {
                opened = await ActionPointAimingMenu.Instance.Show(actionPoint);
            } else if (selectedObject is APOrientation orientation) {
                opened = await orientation.OpenDetailMenu();     
            }
            if (opened) {
                ActionPointAimingMenuButton.GetComponent<Image>().enabled = true;
                SelectorMenu.Instance.gameObject.SetActive(false);
            } else {
                Notifications.Instance.ShowNotification("Failed to open action picker", "Could not lock action point");
            }
            

        }
    }*/


    public override void UpdateVisibility() {
        if (GameManager.Instance.GetGameState() == GameManager.GameStateEnum.ProjectEditor &&
            MainMenu.Instance.CurrentState() == DanielLochner.Assets.SimpleSideMenu.SimpleSideMenu.State.Closed) {
            UpdateVisibility(true);            
        } else {
            UpdateVisibility(false);
        }
    }

    public override void UpdateVisibility(bool visible, bool force = false) {
        base.UpdateVisibility(visible, force);
        if (GameManager.Instance.GetGameState() == GameStateEnum.ProjectEditor)
            AREditorResources.Instance.StartStopSceneBtn.gameObject.SetActive(visible);
    }

    public async void ShowCloseProjectDialog() {
        (bool success, _) = await Base.GameManager.Instance.CloseProject(false);
        if (!success) {
            string message = "Are you sure you want to close current project? ";
            if (ProjectManager.Instance.ProjectChanged) {
                message += "Unsaved changes will be lost";
                if (SceneManager.Instance.SceneStarted) {
                    message += " and system will go offline";
                }
                message += ".";
            } else if (SceneManager.Instance.SceneStarted) {
                message += "System will go offline.";
            }
            GameManager.Instance.HideLoadingScreen();
            ConfirmationDialog.Open("Close project",
                         message,
                         () => CloseProject(),
                         () => ConfirmationDialog.Close());
        }

    }

    public async void CloseProject() {
        if (SceneManager.Instance.SceneStarted)
            WebsocketManager.Instance.StopScene(false, null);
        GameManager.Instance.ShowLoadingScreen("Closing project..");
        _ = await GameManager.Instance.CloseProject(true);
        ConfirmationDialog.Close();
        MainMenu.Instance.Close();
        GameManager.Instance.HideLoadingScreen();
    }

    public async void RunClicked() {
        try {
            InteractiveObject selectedObject = SelectorMenu.Instance.GetSelectedObject();
            if (selectedObject is null)
                return;
            if (selectedObject is StartAction) {
                Debug.LogError("START");
                RunProject();
            } else if (selectedObject is Action3D action) {
                action.ActionBeingExecuted = true;
                await WebsocketManager.Instance.ExecuteAction(selectedObject.GetId(), false);
                // TODO: enable stop execution (_ = GameManager.Instance.CancelExecution();)
                action.ActionBeingExecuted = false;
            } else if (selectedObject.GetType() == typeof(APOrientation)) {
                
                //await WebsocketManager.Instance.MoveToActionPointOrientation(SceneManager.Instance.SelectedRobot.GetId(), SceneManager.Instance.SelectedEndEffector.GetId(), 0.5m, selectedObject.GetId(), false);
            } 
        } catch (RequestFailedException ex) {
            Notifications.Instance.ShowNotification("Failed to execute action", ex.Message);
            return;
        }
        
    }

    public async void CloseActionPicker(bool setActionMode = true) {
        if (CurrentMode == Mode.AddAction) {
            if (APToRemoveOnCancel != null) {
                await WebsocketManager.Instance.RemoveActionPoint(APToRemoveOnCancel.GetId());
            }
            if (ProjectManager.Instance.PrevAction != null &&
                ProjectManager.Instance.NextAction != null) {
                try {
                    await WebsocketManager.Instance.AddLogicItem(ProjectManager.Instance.PrevAction,
                        ProjectManager.Instance.NextAction, null, false);
                } catch (RequestFailedException) {
                } finally {
                    ProjectManager.Instance.PrevAction = null;
                    ProjectManager.Instance.NextAction = null;
                }
            }
            APToRemoveOnCancel = null;
            ActionPicker.SetActive(false);
            SelectorMenu.Instance.Active = true;
            if (setActionMode)
                RightButtonsMenu.Instance.SetActionMode();
        } else {
            APToRemoveOnCancel = null;
            //RestoreSelector();
            ActionPicker.SetActive(false);
        }
    }

    public async void ActionMoveToClick() {
        InteractiveObject selectedObject = SelectorMenu.Instance.GetSelectedObject();
        if (selectedObject is ActionPoint3D actionPoint) {
            ActionMoveToClick(actionPoint);
        } else if (selectedObject is Action3D action) {
            ActionMoveToClick((ActionPoint3D) action.ActionPoint);
        }
    }

    public void AddNewObject() {
        MeshPicker.transform.position = Camera.main.transform.position + Camera.main.transform.forward * 0.2f;
        MeshPicker.GetComponent<FaceCamera>().Update();
        SelectorMenu.Instance.gameObject.SetActive(false);
        RightButtonsMenu.Instance.gameObject.SetActive(true);
        RightButtonsMenu.Instance.SetMenuTriggerMode();
        SelectorMenu.Instance.Active = false;
        MeshPicker.SetActive(true);
    }

    public void ActionMoveToClick(ActionPoint3D actionPoint) {
        string robotId = "";
        foreach (IRobot r in SceneManager.Instance.GetRobots()) {
            robotId = r.GetId();
        }
        string name = ProjectManager.Instance.GetFreeActionName("MoveTo");
        NamedOrientation o = actionPoint.GetFirstOrientation();
        List<ActionParameter> parameters = new List<ActionParameter> {
            new ActionParameter(name: "pose", type: "pose", value: "\"" + o.Id + "\""),
            new ActionParameter(name: "move_type", type: "string_enum", value: "\"JOINTS\""),
            new ActionParameter(name: "velocity", type: "double", value: "30.0"),
            new ActionParameter(name: "acceleration", type: "double", value: "50.0")
        };
        IActionProvider robot = SceneManager.Instance.GetActionObject(robotId);
        //ProjectManager.Instance.ActionToSelect = name;
        WebsocketManager.Instance.AddAction(actionPoint.GetId(), parameters, robotId + "/move", name, robot.GetActionMetadata("move").GetFlows(name));
        //RestoreSelector();
        ActionPicker.SetActive(false);
        //if (CurrentMode == Mode.AddAction) {
        SelectorMenu.Instance.Active = true;
        RightButtonsMenu.Instance.SetActionMode();
        if (APToRemoveOnCancel != null)
            RightButtonsMenu.Instance.MoveClick();
        //} else {
        // RestoreSelector();
        //    AddActionButton.GetComponent<Image>().enabled = false;
        //}
        APToRemoveOnCancel = null;
    }
    public async void ActionPickClick() {
        InteractiveObject selectedObject = SelectorMenu.Instance.GetSelectedObject();
        if (selectedObject is ActionPoint3D actionPoint) {
            ActionPickClick(actionPoint);
        } else if (selectedObject is Action3D action) {
            ActionPickClick((ActionPoint3D) action.ActionPoint);
        }
    }


    public void ActionPickClick(ActionPoint3D actionPoint) {

        string robotId = "";
        foreach (IRobot r in SceneManager.Instance.GetRobots()) {
            robotId = r.GetId();
        }
        string name = ProjectManager.Instance.GetFreeActionName("Pick");
        NamedOrientation o = ((ActionPoint3D) actionPoint).GetFirstOrientation();
        List<ActionParameter> parameters = new List<ActionParameter> {
            new ActionParameter(name: "pick_pose", type: "pose", value: "\"" + o.Id + "\""),
            new ActionParameter(name: "vertical_offset", type: "double", value: "0.0")
        };
        IActionProvider robot = SceneManager.Instance.GetActionObject(robotId);

        //ProjectManager.Instance.ActionToSelect = name;
        WebsocketManager.Instance.AddAction(actionPoint.GetId(), parameters, robotId + "/pick", name, robot.GetActionMetadata("pick").GetFlows(name));
        //RestoreSelector();
        ActionPicker.SetActive(false);
        //if (CurrentMode == Mode.AddAction) {
        SelectorMenu.Instance.Active = true;
        RightButtonsMenu.Instance.SetActionMode();
        if (APToRemoveOnCancel != null)
            RightButtonsMenu.Instance.MoveClick();
        /*} else {
            //RestoreSelector();
            AddActionButton.GetComponent<Image>().enabled = false;
        }*/
        APToRemoveOnCancel = null;


    }

    public async void ActionReleaseClick() {
        InteractiveObject selectedObject = SelectorMenu.Instance.GetSelectedObject();
        if (selectedObject is ActionPoint3D actionPoint) {
            ActionReleaseClick(actionPoint);
        } else if (selectedObject is Action3D action) {
            ActionReleaseClick((ActionPoint3D) action.ActionPoint);
        }
    }

    public void ActionReleaseClick(ActionPoint3D actionPoint) {
        string robotId = "";
        foreach (IRobot r in SceneManager.Instance.GetRobots()) {
            robotId = r.GetId();
        }
        string name = ProjectManager.Instance.GetFreeActionName("Release");
        NamedOrientation o = ((ActionPoint3D) actionPoint).GetFirstOrientation();
        List<ActionParameter> parameters = new List<ActionParameter> {
            new ActionParameter(name: "place_pose", type: "pose", value: "\"" + o.Id + "\""),
            new ActionParameter(name: "vertical_offset", type: "double", value: "0.0")
        };
        IActionProvider robot = SceneManager.Instance.GetActionObject(robotId);

        //ProjectManager.Instance.ActionToSelect = name;
        WebsocketManager.Instance.AddAction(actionPoint.GetId(), parameters, robotId + "/place", name, robot.GetActionMetadata("place").GetFlows(name));
        //RestoreSelector();
        ActionPicker.SetActive(false);
        //if (CurrentMode == Mode.AddAction) {
        SelectorMenu.Instance.Active = true;
        RightButtonsMenu.Instance.SetActionMode();
        if (APToRemoveOnCancel != null)
            RightButtonsMenu.Instance.MoveClick();
        /*} else {
            //RestoreSelector();
            AddActionButton.GetComponent<Image>().enabled = false;
        }*/
        APToRemoveOnCancel = null;

    }

    public void CloseMeshPicker() {
        SelectorMenu.Instance.gameObject.SetActive(true);
        RightButtonsMenu.Instance.gameObject.SetActive(false);
        SelectorMenuClick();
        MeshPicker.SetActive(false);
        SelectorMenu.Instance.Active = true;
    }

    public void AddCubeCb() {
        ActionObjectPickerMenu.Instance.CreateCube();
        CloseMeshPicker();
    }
    public async void AddBlueBoxCb() {
        string name = SceneManager.Instance.GetFreeAOName("ModraKrabice");
        await WebsocketManager.Instance.AddObjectToScene(name, "BlueBox", Sight.Instance.CreatePoseInTheView(), new List<IO.Swagger.Model.Parameter>());
        CloseMeshPicker();
    }
    public async void AddTesterCb() {
        string name = SceneManager.Instance.GetFreeAOName("Tester");
        await WebsocketManager.Instance.AddObjectToScene(name, "Tester", Sight.Instance.CreatePoseInTheView(), new List<IO.Swagger.Model.Parameter>());
        CloseMeshPicker();
    }
}
