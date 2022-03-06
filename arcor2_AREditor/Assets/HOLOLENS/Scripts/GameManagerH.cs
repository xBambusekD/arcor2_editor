using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Threading.Tasks;
using UnityEngine.UI;
using IO.Swagger.Model;
using UnityEngine.XR.ARFoundation;
using UnityEngine.Events;
using System.Collections;
using Newtonsoft.Json;
using MiniJSON;
using Base;

public class GameManagerH : Singleton<GameManagerH>
{

    private bool updatingPackageState;


 /*   /// <summary>
    /// Loading screen with animation
    /// </summary>
    public LoadingScreen LoadingScreen;*/

  /*  /// <summary>
    /// Canvas for headUp info (notifications, tooltip, loading screen etc.
    /// </summary>
    [SerializeField]
    private Canvas headUpCanvas;*/


    private Boolean calibrationSet = false; 

    /// <summary>
    /// Called when editor is trying to connect to server. Contains server URI
    /// </summary>
    public event AREditorEventArgs.StringEventHandler OnConnectingToServer;

    /// <summary>
    /// Temp storage for delayed scene
    /// </summary>
    private IO.Swagger.Model.Scene newScene;

    /// <summary>
    /// Api version
    /// </summary>        
    public const string ApiVersion = "0.19.0";

    /// <summary>
    /// GameObject of scene
    /// </summary>
    public GameObject Scene;

    /// <summary>
    /// Holds current editor state
    /// </summary>
    private EditorStateEnum editorState;

    /// <summary>
    /// Indicates that scene should be opened with delay (waiting for action objects)
    /// </summary>
    private bool openScene = false;

    /// <summary>
    /// Indicates that project should be opened with delay (waiting for scene or action objects)
    /// </summary>
    private bool openProject = false;

    /// <summary>
    /// Holds ID of currently executing action. Null if there is no such action
    /// </summary>
    public string ExecutingAction = null;

    /// <summary>
    /// Holds info of connection status
    /// </summary>
    private ConnectionStatusEnum connectionStatus;

    /// <summary>
    /// Called when editor connected to server. Contains server URI
    /// </summary>
    public event AREditorEventArgs.StringEventHandler OnConnectedToServer;

    /// <summary>
    /// Invoked when scene editor is opened
    /// </summary>
    public event EventHandler OnOpenSceneEditor;

    /// <summary>
    /// Callback to be invoked when requested object is selected and potentionally validated
    /// </summary>
    private Action<object> ObjectCallback;

    /// <summary>
    /// Enum specifying connection states
    /// </summary>
    public enum ConnectionStatusEnum {
        Connected, Disconnected, Connecting
    }

    /// <summary>
    /// Called when list of scenes is changed (new, removed, renamed)
    /// </summary>
    public event EventHandler OnScenesListChanged;

    /// <summary>
    /// Callback to be invoked when requested object is selected
    /// </summary>
    private Func<object, Task<RequestResult>> ObjectValidationCallback;

    /// <summary>
    /// Holds current application state (opened screen)
    /// </summary>
    private GameStateEnum gameState;

    /// <summary>
    /// Holds info about server (version, supported RPCs, supported parameters etc.)
    /// </summary>
    public IO.Swagger.Model.SystemInfoResponseData SystemInfo;

    /// <summary>
    /// Temp storage for delayed project
    /// </summary>
    private IO.Swagger.Model.Project newProject;

    public List<IO.Swagger.Model.ListScenesResponseData> Scenes = new List<IO.Swagger.Model.ListScenesResponseData>();

     /// <summary>
    /// List of projects metadata
    /// </summary>
    public List<IO.Swagger.Model.ListProjectsResponseData> Projects = new List<IO.Swagger.Model.ListProjectsResponseData>();


    /// <summary>
    /// Invoked when in SceneEditor or ProjectEditor state and no menus are opened
    /// </summary>
    public event EventHandler OnSceneInteractable;

    /// <summary>
    /// Called when list of projects is changed (new project, removed project, renamed project)
    /// </summary>
    public event EventHandler OnProjectsListChanged;

    /// <summary>
    /// Called when scene is closed
    /// </summary>
    public event EventHandler OnCloseScene;

    /// <summary>
    /// Invoked when project editor is opened
    /// </summary>
    public event EventHandler OnOpenProjectEditor;

    /// <summary>
    /// Called when project is closed
    /// </summary>
    public event EventHandler OnCloseProject;

    /// <summary>
    /// Holds whether delayed openning of main screen is requested
    /// </summary>
    private bool openMainScreenRequest = false;

    /// <summary>
    /// Holds info about what part of main screen should be displayd
    /// </summary>
    private ShowMainScreenData openMainScreenData;

    /// <summary>
    /// Enum specifying editor states
    ///
    /// For selecting states - other interaction than selecting of requeste object is disabled
    /// </summary>
    public enum EditorStateEnum {
        /// <summary>
        /// No editor (scene or project) opened
        /// </summary>
        Closed,
        /// <summary>
        /// Normal state
        /// </summary>
        Normal,
        /// <summary>
        /// Indicates that user should select action object
        /// </summary>
        SelectingActionObject,
        /// <summary>
        /// Indicates that user should select action point
        /// </summary>
        SelectingActionPoint,
        /// <summary>
        /// Indicates that user should select action 
        /// </summary>
        SelectingAction,
        /// <summary>
        /// Indicates that user should select action input
        /// </summary>
        SelectingActionInput,
        /// <summary>
        /// Indicates that user should select action output
        /// </summary>
        SelectingActionOutput,
        /// <summary>
        /// Indicates that user should select action object or another action point
        /// </summary>
        SelectingActionPointParent,
        /// <summary>
        /// Indicates that user should select orientation of action point
        /// </summary>
        SelectingAPOrientation,
        /// <summary>
        /// Indicates that user should select end effector
        /// </summary>
        SelectingEndEffector,
        /// <summary>
        /// Indicates that all interaction is disabled
        /// </summary>
        InteractionDisabled
    }

    /// <summary>
    /// Enum specifying aplication states
    /// </summary>
    public enum GameStateEnum {
        /// <summary>
        /// Not connected to server
        /// </summary>
        Disconnected,
        /// <summary>
        /// Screen with list of scenes, projects and packages
        /// </summary>
        MainScreen,
        /// <summary>
        /// Scene editor
        /// </summary>
        SceneEditor,
        /// <summary>
        /// Project editor
        /// </summary>
        ProjectEditor,
        /// <summary>
        /// Visualisation of running package
        /// </summary>
        PackageRunning,
        LoadingScene,
        LoadingProject,
        LoadingPackage,
        ClosingScene,
        ClosingProject,
        ClosingPackage,
        None
    }

    /// <summary>
    /// Change game state and invoke coresponding event
    /// </summary>
    /// <param name="value">New game state</param>
    public void SetGameState(GameStateEnum value) {
        gameState = value;            
      //  OnGameStateChanged?.Invoke(this, new GameStateEventArgs(gameState));            
    }

    /// <summary>
    /// Invoked when editor state changed. Contains new state
    /// </summary>
    public event AREditorEventArgs.EditorStateEventHandler OnEditorStateChanged;

    /// <summary>
    /// Holds connection status and invokes callback when status changed
    /// </summary>
    public ConnectionStatusEnum ConnectionStatus {
        get => connectionStatus; set {
            if (connectionStatus != value) {
                OnConnectionStatusChanged(value);
            }
        }
    }

    
    /// TODO!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! is this still neccassarry? How to use it when there is no menumanager anymore?
    /// <summary>
    /// Checks whether scene is interactable
    /// </summary>
    public bool SceneInteractable {
        get => true;
    }



    /// <summary>
    /// Binds events and sets initial state of app
    /// </summary>
    void Start()
    {
       
        Scene.SetActive(false);

        if (Application.isEditor || Debug.isDebugBuild) {
            TrilleonAutomation.AutomationMaster.Initialize();
        }

        WebSocketManagerH.Instance.OnConnectedEvent += OnConnected;
        WebSocketManagerH.Instance.OnDisconnectEvent += OnDisconnected;
        ActionsManagerH.Instance.OnActionsLoaded += OnActionsLoaded;

        WebSocketManagerH.Instance.OnShowMainScreen += OnShowMainScreen;
     /*   WebsocketManager.Instance.OnProjectRemoved += OnProjectRemoved;
        WebsocketManager.Instance.OnProjectBaseUpdated += OnProjectBaseUpdated;
        WebsocketManager.Instance.OnSceneRemoved += OnSceneRemoved;
        WebsocketManager.Instance.OnSceneBaseUpdated += OnSceneBaseUpdated;*/
    //    ConnectToSever("192.168.104.111", 6789);
    }


    /// <summary>
    /// When connected to server, checks for requests for delayd scene, project, package or main screen openning
    /// </summary>
    private async Task Update() {
        // Only when connected to server
        if (ConnectionStatus != ConnectionStatusEnum.Connected)
            return;
       
        // request for delayed openning of scene to allow loading of action objects and their actions
        if (openScene) {
            openScene = false;
            if (newScene != null) {
                Scene scene = newScene;
                newScene = null;
                await SceneOpened(scene);
            }
            // request for delayed openning of project to allow loading of action objects and their actions
        } else if (openProject) {
            openProject = false;
            if (newProject != null && newScene != null) {
                Scene scene = newScene;
                Project project = newProject;
                newScene = null;
                newProject = null;
                ProjectOpened(scene, project);
            }
            // request for delayed openning of package to allow loading of action objects and their actions
        }/* else if (openPackage) {
            openPackage = false;
            updatingPackageState = true;
            UpdatePackageState(newPackageState);
        }*/
  /*      if (nextPackageState != null && !updatingPackageState && (GameManager.Instance.GetGameState() == GameStateEnum.PackageRunning || GameManager.Instance.GetGameState() == GameStateEnum.LoadingPackage || GameManager.Instance.GetGameState() == GameStateEnum.ClosingPackage)) {
            updatingPackageState = true;
            UpdatePackageState(nextPackageState);
            nextPackageState = null;
        }*/
        // request for delayed openning of main screen to allow loading of action objects and their actions
        if (openMainScreenRequest && ActionsManagerH.Instance.ActionsReady) {
            openMainScreenRequest = false;
            await OpenMainScreen(openMainScreenData.What, openMainScreenData.Highlight);
        }
   /*     if (openPackageRunningScreenFlag && GetGameState() != GameStateEnum.PackageRunning) {
            openPackageRunningScreenFlag = false;
            OpenPackageRunningScreen();
        }*/

    }

    /// <summary>
    /// When actions are loaded, enables all menus
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnActionsLoaded(object sender, EventArgs e) {
        Debug.Log("OnActionLoaded");
        //MainMenu.Instance.gameObject.SetActive(true);
    }


    /// <summary>
    /// Shows loading screen
    /// </summary>
    /// <param name="text">Optional text for user</param>
    /// <param name="forceToHide">Sets if HideLoadingScreen needs to be run with force flag to
    /// hide loading screen. Used to avoid flickering when several actions with own loading
    /// screen management are chained.</param>
    public void ShowLoadingScreen(string text = "Loading...", bool forceToHide = false) {
      /*  Debug.Assert(LoadingScreen != null);
        // HACK to make loading screen in foreground
        // TODO - find better way
        headUpCanvas.enabled = false;
        headUpCanvas.enabled = true;
        LoadingScreen.Show(text, forceToHide);*/
    }

    /// <summary>
    /// Connects to server
    /// </summary>
    /// <param name="domain">hostname or IP address</param>
    /// <param name="port">Port of ARServer</param>
    public async void ConnectToSever(string domain, int port) {
       // ShowLoadingScreen("Connecting to server");
       
        HNotificationManager.Instance.ShowNotification("Connectig to server");
        OnConnectingToServer?.Invoke(this, new StringEventArgs(WebSocketManagerH.Instance.GetWSURI(domain, port)));
        WebSocketManagerH.Instance.ConnectToServer(domain, port);
    }

    /// <summary>
    /// Asks server to open scene
    /// </summary>
    /// <param name="id">Scene id</param>
    public async Task OpenScene(string id) {
      //  ShowLoadingScreen();
        try {
            await WebSocketManagerH.Instance.OpenScene(id);
        } catch (RequestFailedException e) {
            HNotificationManager.Instance.ShowNotification("Open scene failed " + e);
        //    HideLoadingScreen();
            return;
        }    
        try {
            await Task.Run(() => WaitForSceneReady(5000));
            return;
        } catch (TimeoutException e) {
             HNotificationManager.Instance.ShowNotification("Open scene failed");
        //    HideLoadingScreen();
        }
        
    }

            
    /// <summary>
    /// Asks server to open project
    /// </summary>
    /// <param name="id">Project id</param>
    public async void OpenProject(string id) {
        ShowLoadingScreen();
        try {
            await WebSocketManagerH.Instance.OpenProject(id);
            await Task.Run(() => WaitForProjectReady(5000));
        } catch (RequestFailedException ex) {
            HNotificationManager.Instance.ShowNotification("Failed to open project  :" + ex.Message);
            HideLoadingScreen();
        } catch (TimeoutException e) {
            HNotificationManager.Instance.ShowNotification("Open project failed - Failed to load project");
            HideLoadingScreen();
        } 
    }

    
    /// <summary>
    /// Waits until scene is loaded
    /// </summary>
    /// <param name="timeout">TimeoutException is thrown after timeout ms when scene is not loaded</param>
    public void WaitForSceneReady(int timeout) {
        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        sw.Start();
        while (SceneManagerH.Instance.SceneMeta == null) {
            if (sw.ElapsedMilliseconds > timeout)
                throw new TimeoutException();
            System.Threading.Thread.Sleep(100);
        }
        return;
    }

    /// <summary>
    /// Waits until project is loaded
    /// </summary>
    /// <param name="timeout">TimeoutException is thrown after timeout ms when project is not loaded</param>
    public void WaitForProjectReady(int timeout) {
        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        sw.Start();
        while (HProjectManager.Instance.ProjectMeta == null) {
            if (sw.ElapsedMilliseconds > timeout)
                throw new TimeoutException();
            System.Threading.Thread.Sleep(100);
        }
        return;
    }


    
    /// <summary>
    /// Hides loading screen
    /// </summary>
    /// <param name="force">Specify if hiding has to be forced. More details in ShowLoadingScreen</param>
    public void HideLoadingScreen(bool force = false) {
     /*   Debug.Assert(LoadingScreen != null);
        LoadingScreen.Hide(force);*/
    }


    /// <summary>
    /// Sets initial state of app
    /// </summary>
    private void Awake() {
        ConnectionStatus = ConnectionStatusEnum.Disconnected;
       // OpenDisconnectedScreen();
    }

    /// <summary>
    /// Disconnects from server
    /// </summary>
    public void DisconnectFromSever() {
        ConnectionStatus = ConnectionStatusEnum.Disconnected;
        WebSocketManagerH.Instance.DisconnectFromSever();
    }


    /// <summary>
    /// Event called when disconnected from server
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    private void OnDisconnected(object sender, EventArgs e) {
        
    }

    /// <summary>
    /// Event called when connected to server
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    private void OnConnected(object sender, EventArgs args) {
        // initialize when connected to the server
        ExecutingAction = null;
        ConnectionStatus = ConnectionStatusEnum.Connected;
        HNotificationManager.Instance.ShowNotification("Connected");
        //Debug.LogError("onConnected triggered");
    }


    /// <summary>
    /// Event called when connections status chanched
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    private async void OnConnectionStatusChanged(ConnectionStatusEnum newState) {
        switch (newState) {
            case ConnectionStatusEnum.Connected:
                IO.Swagger.Model.SystemInfoResponseData systemInfo;
                try {
                    systemInfo = await WebSocketManagerH.Instance.GetSystemInfo();
                    await WebSocketManagerH.Instance.RegisterUser(HLandingManager.Instance.GetUsername());
                } catch (RequestFailedException ex) {
                    DisconnectFromSever();
                    HNotificationManager.Instance.ShowNotification("Connection failed " + ex.Message);
                    return;
                }
                if (!CheckApiVersion(systemInfo)) {
                    DisconnectFromSever();
                    return;
                }

                SystemInfo = systemInfo;
                Debug.Log(systemInfo.ToJson());
            /*    ServerVersion.text = "Editor version: " + Application.version +
                    "\nServer version: " + systemInfo.Version;*/
         //       ConnectionInfo.text = WebSocketManagerH.Instance.APIDomainWS;
           //     MainMenu.Instance.gameObject.SetActive(false);


                OnConnectedToServer?.Invoke(this, new StringEventArgs(WebSocketManagerH.Instance.APIDomainWS));

                await UpdateActionObjects();
                // await UpdateServices();
                await UpdateRobotsMeta();

                try {
                    await Task.Run(() => ActionsManagerH.Instance.WaitUntilActionsReady(15000));
                } catch (TimeoutException e) {
                    HNotificationManager.Instance.ShowNotification("Connection failed Some actions were not loaded within timeout");
                    DisconnectFromSever();
                    return;
                }

                connectionStatus = newState;
                break;
            case ConnectionStatusEnum.Disconnected:
                connectionStatus = ConnectionStatusEnum.Disconnected;
               // OpenDisconnectedScreen();
              //  OnDisconnectedFromServer?.Invoke(this, EventArgs.Empty);
              //  Projects = new List<IO.Swagger.Model.ListProjectsResponseData>();
            //    Scenes = new List<IO.Swagger.Model.ListScenesResponseData>();

            //    ProjectManager.Instance.DestroyProject();
                SceneManagerH.Instance.DestroyScene();
                Scene.SetActive(false);
                break;
        }
    }


    /// <summary>
    /// Updates action objects and their actions from server
    /// </summary>
    /// <param name="highlightedObject">When set, object with this ID will gets highlighted for a few seconds in menu
    /// to inform user about it</param>
    /// <returns></returns>
    public async Task UpdateActionObjects() {
        try {
            List<IO.Swagger.Model.ObjectTypeMeta> objectTypeMetas = await WebSocketManagerH.Instance.GetObjectTypes();
            ActionsManagerH.Instance.UpdateObjects(objectTypeMetas);
        } catch (RequestFailedException ex) {
            Debug.LogError(ex);
            HNotificationManager.Instance.ShowNotification("Failed to update action objects");
            GameManagerH.Instance.DisconnectFromSever();
        }
        
    }

    
    /// <summary>
    /// Updates robot metadata from server
    /// </summary>
    /// <returns></returns>
    private async Task UpdateRobotsMeta() {
        ActionsManagerH.Instance.UpdateRobotsMetadata(await WebSocketManagerH.Instance.GetRobotMeta());
    }

    /// <summary>
    /// Checks if api version of the connected server is compatibile with editor
    /// </summary>
    /// <param name="systemInfo">Version string in format 0.0.0 (major.minor.patch)</param>
    /// <returns>True if versions are compatibile</returns>
    public bool CheckApiVersion(IO.Swagger.Model.SystemInfoResponseData systemInfo) {
        
        if (systemInfo.ApiVersion == ApiVersion)
            return true;

        if (GetMajorVersion(systemInfo.ApiVersion) != GetMajorVersion(ApiVersion) ||
            (GetMajorVersion(systemInfo.ApiVersion) == 0 && (GetMinorVersion(systemInfo.ApiVersion) != GetMinorVersion(ApiVersion)))) {
            HNotificationManager.Instance.ShowNotification("Incompatibile api versions Editor API version: " + ApiVersion + ", server API version: " + systemInfo.ApiVersion);
            return false;
        }
    //    Notifications.Instance.ShowNotification("Different api versions", "Editor API version: " + ApiVersion + ", server API version: " + systemInfo.ApiVersion + ". It can cause problems, you have been warned.");

        return true;
    }

    /// <summary>
    /// Parses version string and returns major version
    /// </summary>
    /// <param name="versionString">Version string in format 0.0.0 (major, minor, patch)</param>
    /// <returns>First number (major version)</returns>
    public int GetMajorVersion(string versionString) {
        return int.Parse(SplitVersionString(versionString)[0]);
    }

    
    /// <summary>
    /// Parses version string and returns minor version
    /// </summary>
    /// <param name="versionString">Version string in format 0.0.0 (major, minor, patch)</param>
    /// <returns>Second number (minor version)</returns>
    public int GetMinorVersion(string versionString) {
        return int.Parse(SplitVersionString(versionString)[1]);
    }


    /// <summary>
    /// Splits version string and returns list of components
    /// </summary>
    /// <param name="versionString">Version string in format 0.0.0 (major.minor.patch)</param>
    /// <returns>List of components of the version string</returns>
    public List<string> SplitVersionString(string versionString) {
        List<string> version = versionString.Split('.').ToList<string>();
        Debug.Assert(version.Count == 3, versionString);
        return version;
    }


    
    /// <summary>
    /// Opens scene editor
    /// </summary>
    public void OpenSceneEditor() {
#if !UNITY_EDITOR
             
        if (CalibrationManagerH.Instance.Calibrated) {
            Scene.SetActive(true);
        }
#else
   /*    GameObject qrCodePrefab = GameObject.Find("QRCode");
        SceneSetParent(qrCodePrefab.transform);*/
      //               GameManagerH.Instance.SceneSetActive(true);
        Scene.SetActive(true);
        
#endif
     //   AREditorResources.Instance.LeftMenuScene.DeactivateAllSubmenus();
     //   MainMenu.Instance.Close();
         SetGameState(GameStateEnum.SceneEditor);
        OnOpenSceneEditor?.Invoke(this, EventArgs.Empty);
        SetEditorState(EditorStateEnum.Normal);
      //  HideLoadingScreen(true);
    }

    
    /// <summary>
    /// Opens project editor
    /// </summary>
    public void OpenProjectEditor() {
#if !UNITY_EDITOR
      if (CalibrationManagerH.Instance.Calibrated) {
            Scene.SetActive(true);
        }
#else
        Scene.SetActive(true);
#endif
       // AREditorResources.Instance.LeftMenuProject.DeactivateAllSubmenus();
       // MainMenu.Instance.Close();
        SetGameState(GameStateEnum.ProjectEditor);
        OnOpenProjectEditor?.Invoke(this, EventArgs.Empty);
        SetEditorState(EditorStateEnum.Normal);
      //  HideLoadingScreen(true);
    }

    /// <summary>
    /// The object which was selected calls this method to inform game manager about it.
    /// Validation and potentionally selection callbacks are called and editor is set to normal state.
    /// </summary>
    /// <param name="selectedObject"></param>
    public async void ObjectSelected(object selectedObject) {
        // if validation callbeck is specified, check if this object is valid
        if (ObjectValidationCallback != null) {
            RequestResult result = await ObjectValidationCallback.Invoke(selectedObject);
            if (!result.Success) {
              //  Notifications.Instance.ShowNotification(result.Message, "");
                return;
            }
            
        }
    /*    SelectorMenu.Instance.PointsToggle.SetInteractivity(true);
        SelectorMenu.Instance.ActionsToggle.SetInteractivity(true);
        SelectorMenu.Instance.IOToggle.SetInteractivity(true);
        SelectorMenu.Instance.ObjectsToggle.SetInteractivity(true);
        SelectorMenu.Instance.OthersToggle.SetInteractivity(true);
        SelectorMenu.Instance.RobotsToggle.SetInteractivity(true);*/
        SetEditorState(EditorStateEnum.Normal);
        // hide selection info 
  //      SelectObjectInfo.gameObject.SetActive(false);
      //  RestoreFilters();
        // invoke selection callback
     /*   if (ObjectCallback != null)
            ObjectCallback.Invoke(selectedObject);*/
        ObjectCallback = null;
    }

    /// <summary>
    /// Returns editor state
    /// </summary>
    /// <returns>Editor state</returns>
    public EditorStateEnum GetEditorState() {
        return editorState;
    }

  /// <summary>
        /// Switch editor to one of selecting modes (based on request type) and promts user
        /// to select object / AP / etc. 
        /// </summary>
        /// <param name="requestType">Determines what the user should select</param>
        /// <param name="callback">Action which is called when object is selected and (optionaly) validated</param>
        /// <param name="message">Message displayed to the user</param>
        /// <param name="validationCallback">Action to be called when user selects object. If returns true, callback is called,
        /// otherwise waits for selection of another object</param>
        public async Task RequestObject(EditorStateEnum requestType, Action<object> callback, string message, Func<object, Task<RequestResult>> validationCallback = null, UnityAction onCancelCallback = null) {
            // only for "selection" requests
            Debug.Assert(requestType != EditorStateEnum.Closed &&
                requestType != EditorStateEnum.Normal &&
                requestType != EditorStateEnum.InteractionDisabled);
            SetEditorState(requestType);

    /*        SelectorMenu.Instance.PointsToggle.SetInteractivity(false);
            SelectorMenu.Instance.ActionsToggle.SetInteractivity(false);
            SelectorMenu.Instance.IOToggle.SetInteractivity(false);
            SelectorMenu.Instance.ObjectsToggle.SetInteractivity(false);
            SelectorMenu.Instance.OthersToggle.SetInteractivity(false);
            SelectorMenu.Instance.RobotsToggle.SetInteractivity(false);*/

            // "disable" non-relevant elements to simplify process for the user
         /*   switch (requestType) {
                case EditorStateEnum.SelectingActionObject:
                    SelectorMenu.Instance.RobotsToggle.SetInteractivity(true);
                    SelectorMenu.Instance.ObjectsToggle.SetInteractivity(true);
                    SceneManager.Instance.EnableAllActionObjects(true, true);
                    HProjectManager.Instance.EnableAllActionPoints(false);
                    ProjectManager.Instance.EnableAllActions(false);
                    ProjectManager.Instance.EnableAllActionOutputs(false);
                    ProjectManager.Instance.EnableAllActionInputs(false);
                    ProjectManager.Instance.EnableAllOrientations(false);
                    if (SceneManager.Instance.SceneStarted)
                        await ProjectManager.Instance.EnableAllRobotsEE(false);
                    break;
                case EditorStateEnum.SelectingActionOutput:
                    ProjectManager.Instance.EnableAllActionPoints(true);
                    ProjectManager.Instance.EnableAllActionInputs(false);
                    ProjectManager.Instance.EnableAllActions(true);
                    SceneManager.Instance.EnableAllActionObjects(false);
                    ProjectManager.Instance.EnableAllOrientations(false);
                    if (SceneManager.Instance.SceneStarted)
                        await ProjectManager.Instance.EnableAllRobotsEE(false);
                    ProjectManager.Instance.EnableAllActionOutputs(true);
                    break;
                case EditorStateEnum.SelectingActionInput:
                    ProjectManager.Instance.EnableAllActionPoints(true);
                    ProjectManager.Instance.EnableAllActionOutputs(false);
                    ProjectManager.Instance.EnableAllActions(true);
                    SceneManager.Instance.EnableAllActionObjects(false);
                    ProjectManager.Instance.EnableAllOrientations(false);
                    if (SceneManager.Instance.SceneStarted)
                        await ProjectManager.Instance.EnableAllRobotsEE(false);
                    ProjectManager.Instance.EnableAllActionInputs(true);
                    break;
                case EditorStateEnum.SelectingActionPointParent:
                    SelectorMenu.Instance.RobotsToggle.SetInteractivity(true);
                    SelectorMenu.Instance.ObjectsToggle.SetInteractivity(true);
                    SelectorMenu.Instance.PointsToggle.SetInteractivity(true);
                    ProjectManager.Instance.EnableAllActions(false);
                    ProjectManager.Instance.EnableAllOrientations(false);
                    if (SceneManager.Instance.SceneStarted)
                        await ProjectManager.Instance.EnableAllRobotsEE(false);
                    ProjectManager.Instance.EnableAllActionOutputs(false);
                    ProjectManager.Instance.EnableAllActionInputs(false);
                    SceneManager.Instance.EnableAllActionObjects(true, true);
                    ProjectManager.Instance.EnableAllActionPoints(true);
                    break;
                case EditorStateEnum.SelectingAPOrientation:
                    ProjectManager.Instance.EnableAllActions(false);
                    if (SceneManager.Instance.SceneStarted)
                        await ProjectManager.Instance.EnableAllRobotsEE(false);
                    ProjectManager.Instance.EnableAllActionOutputs(false);
                    ProjectManager.Instance.EnableAllActionInputs(false);
                    SceneManager.Instance.EnableAllActionObjects(true, true);
                    ProjectManager.Instance.EnableAllActionPoints(true);
                    ProjectManager.Instance.EnableAllOrientations(true);
                    break;
                case EditorStateEnum.SelectingEndEffector:
                    ProjectManager.Instance.EnableAllActions(false);
                    if (SceneManager.Instance.SceneStarted)
                        ProjectManager.Instance.EnableAllActionOutputs(false);
                    ProjectManager.Instance.EnableAllActionInputs(false);
                    SceneManager.Instance.EnableAllActionObjects(false, false);
                    SceneManager.Instance.EnableAllRobots(true);
                    await ProjectManager.Instance.EnableAllRobotsEE(true);
                    ProjectManager.Instance.EnableAllActionPoints(false);
                    ProjectManager.Instance.EnableAllOrientations(false);
                    break;
            }*/
            ObjectCallback = callback;
            ObjectValidationCallback = validationCallback;
            // display info for user and bind cancel callback,


      /*      if (onCancelCallback == null) {
                SelectObjectInfo.Show(message, () => CancelSelection());
            } else {

                SelectObjectInfo.Show(message,
                    () => {
                        onCancelCallback();
                        CancelSelection();
                    });
            }*/
        }


    /// <summary>
    /// Change editor state and enable / disable UI elements based on the new state
    /// and invoke corresponding event
    /// </summary>
    /// <param name="newState">New state</param>
    public void SetEditorState(EditorStateEnum newState) {
        editorState = newState;
   //     OnEditorStateChanged?.Invoke(this, new EditorStateEventArgs(newState));
    /*    switch (newState) {
            // when normal state, enable main menu button and status panel
            case EditorStateEnum.Normal:
                EditorHelper.EnableCanvasGroup(MainMenuBtnCG, true);
                break;
            // otherwise, disable main menu button and status panel
            default:
                EditorHelper.EnableCanvasGroup(MainMenuBtnCG, false);
                break;
        }*/
    }


    /// <summary>
    /// Opens main screen
    /// </summary>
    /// <param name="what">Defines what list should be displayed (scenes/projects/packages)</param>
    /// <param name="highlight">ID of element to highlight (e.g. when scene is closed, it is highlighted for a few seconds</param>
    /// <returns></returns>
    public async Task OpenMainScreen(ShowMainScreenData.WhatEnum what, string highlight) {

        Scene.SetActive(false);
        SetGameState(GameStateEnum.MainScreen);
        HMainMenuManager.Instance.ShowMainMenuScreen();
      //  OnOpenMainScreen?.Invoke(this, EventArgs.Empty);
        SetEditorState(EditorStateEnum.Closed);
        HideLoadingScreen();
    }


    /// <summary>
    /// Create visual elements of opened scene and open scene editor
    /// </summary>
    /// <param name="scene">Scene desription from the server</param>
    /// <returns></returns>
    internal async Task SceneOpened(Scene scene) {
      //  SetGameState(GameStateEnum.LoadingScene);
        if (!ActionsManagerH.Instance.ActionsReady) {
            newScene = scene;
            openScene = true;
            return;
        }
        try {
            HNotificationManager.Instance.ShowNotification("TRY SCENE");

            if (await SceneManagerH.Instance.CreateScene(scene, true)) {   
                OpenSceneEditor();                    
            } else {
                Debug.LogError("Failed to initialize scene");
             HNotificationManager.Instance.ShowNotification("Failed to initialize scene SceneOpened");
        //         HideLoadingScreen();
            }
        } catch (TimeoutException ex) {
            Debug.LogError(ex);
             HNotificationManager.Instance.ShowNotification("Failed to initialize scene SceneOpened TimeOut");
       //     HideLoadingScreen();
        } 
        

    }

    /// <summary>
    /// Create visual elements of opened scene and project and open project editor
    /// </summary>
    /// <param name="project">Project desription from the server</param>
    /// <returns></returns>
    internal async void ProjectOpened(Scene scene, Project project) {
        var state = GetGameState();
        if (!ActionsManagerH.Instance.ActionsReady) {
            newProject = project;
            newScene = scene;
            openProject = true;
            return;
        }
        if (GetGameState() == GameStateEnum.SceneEditor) {
            SetEditorState(EditorStateEnum.InteractionDisabled);
            SceneManagerH.Instance.DestroyScene();
        }
    //    SetGameState(GameStateEnum.LoadingProject);
        try {
             HNotificationManager.Instance.ShowNotification("TRY SCENE");
            if (!await SceneManagerH.Instance.CreateScene(scene, true)) {
                HNotificationManager.Instance.ShowNotification("Failed to initialize scene Project CreateScene");
                Debug.LogError("wft");
             //   HideLoadingScreen();
                return;
            }
            if (await HProjectManager.Instance.CreateProject(project, true)) {
                OpenProjectEditor();
            } else {
                HNotificationManager.Instance.ShowNotification("Failed to initialize scene Project CreateProject");

               // Notifications.Instance.SaveLogs(scene, project, "Failed to initialize project");
             //   HideLoadingScreen();
            }
        } catch (TimeoutException ex) {
            Debug.LogError(ex);
            HNotificationManager.Instance.ShowNotification("Failed to initialize scene Project TimeOut");
         //   HideLoadingScreen();
        }
    }




    /// <summary>
    /// Gets name of scene based on its ID
    /// </summary>
    /// <param name="sceneId">ID of scene</param>
    /// <returns>Name of scene</returns>
    public string GetSceneName(string sceneId) {
        foreach (ListScenesResponseData scene in Scenes) {
            if (scene.Id == sceneId)
                return scene.Name;
        }
        throw new ItemNotFoundException("Scene with id: " + sceneId + " not found");
    }



    /// <summary>
    /// Event called when request to open main screen come from server
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    private async void OnShowMainScreen(object sender, ShowMainScreenEventArgs args) {
        if (ActionsManagerH.Instance.ActionsReady){
             await OpenMainScreen(args.Data.What, args.Data.Highlight);
        }
           
        else {
            openMainScreenRequest = true;
            openMainScreenData = args.Data;
        }
            
    }

    
    /// <summary>
    /// Callback when project was closed
    /// </summary>
    internal void ProjectClosed() {
        HNotificationManager.Instance.ShowNotification("CLOSE PROJECT");
        SetGameState(GameStateEnum.ClosingProject);
      //  ShowLoadingScreen();
        HProjectManager.Instance.DestroyProject();
        SceneManagerH.Instance.DestroyScene();
        OnCloseProject?.Invoke(this, EventArgs.Empty);
        SetGameState(GameStateEnum.None);
    }

    
    /// <summary>
    /// Callback when scene was closed
    /// </summary>
    internal void SceneClosed() {
         HNotificationManager.Instance.ShowNotification("CLOSE SCENE");
        SetGameState(GameStateEnum.ClosingScene);
      //  ShowLoadingScreen();
        SceneManagerH.Instance.DestroyScene();
        OnCloseScene?.Invoke(this, EventArgs.Empty);
        SetGameState(GameStateEnum.None);
    }

    /// <summary>
    /// Asks server to close currently opened scene
    /// </summary>
    /// <param name="force">True if the server should close scene with unsaved changes</param>
    /// <param name="dryRun">Only check if the scene could be closed without forcing</param>
    /// <returns>True if request was successfull. If not, message describing error is attached</returns>
    public async Task<RequestResult> CloseScene(bool force, bool dryRun = false) {
       
        try {
            await WebSocketManagerH.Instance.CloseScene(force, dryRun);
            return (true, "");
        } catch (RequestFailedException ex) {
            if (!dryRun && force) {
                HNotificationManager.Instance.ShowNotification("Failed to close scene");
             //   HideLoadingScreen();                   
            }
            return (false, ex.Message);
        }          
        
    }

    /// <summary>
    /// Asks server to close currently opened project
    /// </summary>
    /// <param name="force">True if the server should close project with unsaved changes</param>
    /// <param name="dryRun">Only check if the project could be closed without forcing</param>
    /// <returns></returns>
    public async Task<RequestResult> CloseProject(bool force, bool dryRun = false) {
    
        try {
            await WebSocketManagerH.Instance.CloseProject(force, dryRun: dryRun);
            return (true, "");
        } catch (RequestFailedException ex) {
            if (!dryRun && force) {
                HNotificationManager.Instance.ShowNotification("Failed to close project - " + ex.Message);
              //  HideLoadingScreen();
            }                
            return (false, ex.Message);
        }           
        
    }

    /// <summary>
    /// Returns current game state
    /// </summary>
    /// <returns>Current game state</returns>
    public GameStateEnum GetGameState() {
        return gameState;
    }

    /// <summary>
    /// Activates/Disactivates the Scene and calls all necessary methods (Selector menu update).
    /// </summary>
    /// <param name="active"></param>
    public void SceneSetActive(bool active) {
        Scene.SetActive(active);
    }

    public void SceneSetParent(Transform transform){
        Scene.transform.parent = transform;
    //    Scene.transform.Rotate(90.0f, 0.0f, 0.0f, Space.Self);
    }


    public void InvokeScenesListChanged() {
        OnScenesListChanged?.Invoke(this, EventArgs.Empty);
    }

    public void InvokeProjectsListChanged() {
        OnProjectsListChanged?.Invoke(this, EventArgs.Empty);
    }
}
