using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using UnityEngine;
using Newtonsoft.Json;
using System.Threading.Tasks;
using NativeWebSocket;
using IO.Swagger.Model;
using UnityEditor;
using UnityEngine.Events;
using Base;

public class WebSocketManagerH : Singleton<WebSocketManagerH>
{


    /// <summary>
    /// ARServer URI
    /// </summary>
    public string APIDomainWS = "";
    // Start is called before the first frame update
    /// <summary>
    /// Websocket context
    /// </summary>
    private WebSocket websocket;

    /// <summary>
    /// Requset id pool
    /// </summary>
    private int requestID = 1;

    /// <summary>
    /// Invoked when connected to server
    /// </summary>
    public event EventHandler OnConnectedEvent;
    /// <summary>
    /// Invoked when disconnected from server.
    /// </summary>
    public event EventHandler OnDisconnectEvent;

    /// <summary>
    /// Dictionary of unprocessed responses
    /// </summary>
    private Dictionary<int, string> responses = new Dictionary<int, string>();
    private Dictionary<int, Tuple<string, UnityAction<string, string>>> responsesCallback = new Dictionary<int, Tuple<string, UnityAction<string, string>>>();

    public event AREditorEventArgs.StringListEventHandler OnObjectTypeRemoved;
    public event AREditorEventArgs.ObjectTypesHandler OnObjectTypeAdded;
    public event AREditorEventArgs.ObjectTypesHandler OnObjectTypeUpdated;
    public event AREditorEventArgs.SceneStateHandler OnSceneStateEvent;

    /// <summary>
    /// Invoked when main screen should be opened. Contains info of which list (scenes, projects, packages)
    /// should be opened and which tile should be highlighted.
    /// </summary>
    public event AREditorEventArgs.ShowMainScreenEventHandler OnShowMainScreen;

    /// <summary>
    /// ARServer domain or IP address
    /// </summary>
    private string serverDomain;
    
    /// <summary>
    /// Create websocket URI from domain name and port
    /// </summary>
    /// <param name="domain">Domain name or IP address</param>
    /// <param name="port">Server port</param>
    /// <returns></returns>
    public string GetWSURI(string domain, int port) {
        return "ws://" + domain + ":" + port.ToString();
    }

    /// <summary>
    /// Callbeck when connection to the server is closed
    /// </summary>
    /// <param name="closeCode"></param>
    private void OnClose(WebSocketCloseCode closeCode) {
        Debug.Log("Connection closed!");
    //    CleanupAfterDisconnect();
        OnDisconnectEvent?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Callback when some connection error occures
    /// </summary>
    /// <param name="errorMsg"></param>
    private void OnError(string errorMsg) {
        Debug.LogError(errorMsg);
    }
    /// <summary>
    /// Callback when connected to the server
    /// </summary>
    private void OnConnected() {
        Debug.Log("On connected");
        OnConnectedEvent?.Invoke(this, EventArgs.Empty);
    }   


    /// <summary>
    /// Disconnects from server
    /// </summary>
    async public void DisconnectFromSever() {
        Debug.Log("Disconnecting");
        GameManagerH.Instance.ConnectionStatus = GameManagerH.ConnectionStatusEnum.Disconnected;
        try {
            await websocket.Close();
        } catch (WebSocketException e) {
            //already closed probably..
        }
    }
    
    /// <summary>
    /// Tries to connect to server
    /// </summary>
    /// <param name="domain">Domain name or IP address of server</param>
    /// <param name="port">Server port</param>
    public async void ConnectToServer(string domain, int port) {
        Debug.Log("connectToServer called");
     
        GameManagerH.Instance.ConnectionStatus = GameManagerH.ConnectionStatusEnum.Connecting;
        try {
            APIDomainWS = GetWSURI(domain, port);
            websocket = new WebSocket(APIDomainWS);
            serverDomain = domain;

            websocket.OnOpen += OnConnected;
            websocket.OnError += OnError;
            websocket.OnClose += OnClose;
            websocket.OnMessage += HandleReceivedData;

            await websocket.Connect();
        } catch (UriFormatException ex) {
            Debug.LogError(ex);
           
       //     Notifications.Instance.ShowNotification("Failed to parse domain", ex.Message);
            GameManagerH.Instance.ConnectionStatus = GameManagerH.ConnectionStatusEnum.Disconnected;
            GameManagerH.Instance.HideLoadingScreen(true);
        }
    }
    // Update is called once per frame
    void Update()
    {
         if (websocket != null && websocket.State == WebSocketState.Open)
                websocket.DispatchMessageQueue();
    }


    
    /// <summary>
    /// Method for parsing recieved message and invoke proper callback
    /// </summary>
    /// <param name="message">Recieved message</param>
    private async void HandleReceivedData(byte[] message) {
        string data = Encoding.Default.GetString(message);
        var dispatchType = new {
            id = 0,
            response = "",
            @event = "",
            request = ""
        };

        var dispatch = JsonConvert.DeserializeAnonymousType(data, dispatchType);

        if (dispatch?.response == null && dispatch?.request == null && dispatch?.@event == null)
            return;
        if (dispatch?.@event == null || (dispatch?.@event != "RobotEef" && dispatch?.@event != "RobotJoints"))
            Debug.Log("Recieved new data: " + data);
        if (dispatch.response != null) {

            if (responses.ContainsKey(dispatch.id)) {
                responses[dispatch.id] = data;
            } else if (responsesCallback.TryGetValue(dispatch.id, out Tuple<string, UnityAction<string, string>> callbackData)) {
                callbackData.Item2.Invoke(callbackData.Item1, data);
            }

        } else if (dispatch.@event != null) {
            switch (dispatch.@event) {
                case "ShowMainScreen":
                    HandleShowMainScreen(data);
                    break;
                case "OpenScene":
                    await HandleOpenScene(data);
                    break;
                case "SceneClosed":
                    HandleCloseScene(data);
                    break;
                case "SceneObjectChanged":
                    HandleSceneObjectChanged(data);
                    break;
                case "OpenProject":
                    HandleOpenProject(data);
                    break;
                case "ProjectClosed":
                    HandleCloseProject(data);
                    break;
                case "ObjectsLocked":
                    HandleObjectLocked(data);
                    break;
             
            }
        }
 
    }

    
    /// <summary>
    /// Register or unregister to/from subsription of robots joints or end effectors pose.
    /// </summary>
    /// <param name="robotId">ID of robot</param>
    /// <param name="send">To subscribe or to unsubscribe</param>
    /// <param name="what">Pose of end effectors or joints</param>
    /// <returns>True if request successfull, false otherwise</returns>
    public async Task<bool> RegisterForRobotEvent(string robotId, bool send, IO.Swagger.Model.RegisterForRobotEventRequestArgs.WhatEnum what) {
        int r_id = Interlocked.Increment(ref requestID);
        IO.Swagger.Model.RegisterForRobotEventRequestArgs args = new IO.Swagger.Model.RegisterForRobotEventRequestArgs(robotId: robotId, send: send, what: what);
        IO.Swagger.Model.RegisterForRobotEventRequest request = new IO.Swagger.Model.RegisterForRobotEventRequest(r_id, "RegisterForRobotEvent", args);
        SendDataToServer(request.ToJson(), r_id, true);
        IO.Swagger.Model.RegisterForRobotEventResponse response = await WaitForResult<IO.Swagger.Model.RegisterForRobotEventResponse>(r_id);

        // TODO: is this correct?
        return response == null ? false : response.Result;
    }

    /// <summary>
    /// Universal method for sending data to server
    /// </summary>
    /// <param name="data">String to send</param>
    /// <param name="key">ID of request (used to obtain result)</param>
    /// <param name="storeResult">Flag whether or not store result</param>
    /// <param name="logInfo">Flag whether or not log sended message</param>
    public void SendDataToServer(string data, int key = -1, bool storeResult = false, bool logInfo = true) {
        if (key < 0) {
            key = Interlocked.Increment(ref requestID);
        }
        

        if (storeResult) {
            responses[key] = null;
        }
        SendWebSocketMessage(data, logInfo);
    }


    /// <summary>
    /// Sends data to server
    /// </summary>
    /// <param name="data"></param>
    private async void SendWebSocketMessage(string data, bool logInfo) {
        try {
            if (websocket.State == WebSocketState.Open) {
                await websocket.SendText(data);
                if (logInfo)
                    Debug.Log("Sent data to server: " + data);
            }
        } catch (WebSocketException ex) {
            Debug.Log("socketexception in sendwebsocketmessage: " + ex.Message);
         

        }
    }

    /// <summary>
    /// Invoked when openning of scene is requested
    /// </summary>
    /// <param name="data"Message from server></param>
    /// <returns></returns>
    private async Task HandleOpenScene(string data) {
        IO.Swagger.Model.OpenScene openSceneEvent = JsonConvert.DeserializeObject<IO.Swagger.Model.OpenScene>(data);
        await GameManagerH.Instance.SceneOpened(openSceneEvent.Data.Scene);
    }

    private void HandleSceneState(string obj) {
        SceneState sceneState = JsonConvert.DeserializeObject<SceneState>(obj);
        OnSceneStateEvent?.Invoke(this, new SceneStateEventArgs(sceneState.Data));
    }

    /// <summary>
    /// Invoked when openning of project is requested
    /// </summary>
    /// <param name="data">Message from server</param>
    private async void HandleOpenProject(string data) {
        IO.Swagger.Model.OpenProject openProjectEvent = JsonConvert.DeserializeObject<IO.Swagger.Model.OpenProject>(data);
        GameManagerH.Instance.ProjectOpened(openProjectEvent.Data.Scene, openProjectEvent.Data.Project);
    }

    
    private void HandleShowMainScreen(string data) {
        IO.Swagger.Model.ShowMainScreen showMainScreenEvent = JsonConvert.DeserializeObject<IO.Swagger.Model.ShowMainScreen>(data);
        OnShowMainScreen?.Invoke(this, new ShowMainScreenEventArgs(showMainScreenEvent.Data));
    }

    /// <summary>
    /// Invoked when closing of scene is requested
    /// </summary>
    /// <param name="data">Message from server</param>
    private void HandleCloseScene(string data) {
        GameManagerH.Instance.SceneClosed();
    }

    /// <summary>
    /// Invoked when closing of project is requested
    /// </summary>
    /// <param name="data">Message from server</param>
    private void HandleCloseProject(string data) {
        GameManagerH.Instance.ProjectClosed();
    }

    /// <summary>
    /// Loads actions for selected object type. Throws RequestFailedException when request failed
    /// </summary>
    /// <param name="name">Object type</param>
    /// <returns>List of actions</returns>
    public void GetActions(string name, UnityAction<string, string> callback) {
        int id = Interlocked.Increment(ref requestID);
        responsesCallback.Add(id, Tuple.Create(name, callback));
        SendDataToServer(new IO.Swagger.Model.GetActionsRequest(id: id, request: "GetActions", args: new IO.Swagger.Model.TypeArgs(type: name)).ToJson(), id, false);
    }


    public async Task<List<string>> GetEndEffectors(string robotId, string armId = null) {
        int r_id = Interlocked.Increment(ref requestID);
        GetEndEffectorsRequestArgs args = new GetEndEffectorsRequestArgs(robotId: robotId, armId: armId);
        IO.Swagger.Model.GetEndEffectorsRequest request = new IO.Swagger.Model.GetEndEffectorsRequest(r_id, "GetEndEffectors", args: args);
        SendDataToServer(request.ToJson(), r_id, true);
        IO.Swagger.Model.GetEndEffectorsResponse response = await WaitForResult<IO.Swagger.Model.GetEndEffectorsResponse>(r_id);
        if (response == null || !response.Result) {
             HNotificationManager.Instance.ShowNotification( "Failed to get robot end effectors /n");
            throw new RequestFailedException(response == null ? new List<string>() { "Failed to get robot end effectors" } : response.Messages);
        } else {
            return response.Data;
        }
    }

    /// <summary>
    /// Asks server to remove object from scene.
    /// 
    /// </summary>
    /// <param name="id">ID of action object</param>
    /// <param name="force">Indicates whether or not it should be forced</param>
    /// <returns>Response from server</returns>
    public async Task<IO.Swagger.Model.RemoveFromSceneResponse> RemoveFromScene(string id, bool force, bool dryRun) {
        int r_id = Interlocked.Increment(ref requestID);
        IO.Swagger.Model.RemoveFromSceneRequestArgs args = new IO.Swagger.Model.RemoveFromSceneRequestArgs(id: id, force: force);
        IO.Swagger.Model.RemoveFromSceneRequest request = new IO.Swagger.Model.RemoveFromSceneRequest(id: r_id, request: "RemoveFromScene", args: args, dryRun: dryRun);
        SendDataToServer(request.ToJson(), r_id, true, logInfo: !dryRun);
        return await WaitForResult<IO.Swagger.Model.RemoveFromSceneResponse>(r_id);
    }

    public void StopScene(bool dryRun, UnityAction<string, string> callback) {
        int id = Interlocked.Increment(ref requestID);
        if (callback != null)
            responsesCallback.Add(id, Tuple.Create("", callback));
        IO.Swagger.Model.StopSceneRequest request = new IO.Swagger.Model.StopSceneRequest(id, "StopScene", dryRun: dryRun);
        SendDataToServer(request.ToJson(), id, false);
    }

    
    /// <summary>
    /// Asks server to close opened scene.
    /// Throws RequestFailedException when request failed
    /// </summary>
    /// <param name="force">Indicates whether the scene should be closed even when it has unsaved changes.</param>
    /// <param name="dryRun">If true, validates all parameters, but will not execute requested action itself.</param>
    /// <returns></returns>
    public async Task CloseScene(bool force, bool dryRun = false) {
        int r_id = Interlocked.Increment(ref requestID);
        IO.Swagger.Model.CloseSceneRequestArgs args = new IO.Swagger.Model.CloseSceneRequestArgs(force);
        IO.Swagger.Model.CloseSceneRequest request = new IO.Swagger.Model.CloseSceneRequest(r_id, "CloseScene", args, dryRun);
        SendDataToServer(request.ToJson(), r_id, true);
        IO.Swagger.Model.CloseSceneResponse response = await WaitForResult<IO.Swagger.Model.CloseSceneResponse>(r_id);
        if (response == null || !response.Result)
            throw new RequestFailedException(response == null ? "Request timed out" : response.Messages[0]);
    }

    /// <summary>
    /// Asks server to close currently opened project.
    /// Throws RequestFailedException when request failed
    /// </summary>
    /// <param name="force">Indicates if it should be closed even with unsaved changes.</param>
    /// <param name="dryRun">If true, validates all parameters, but will not execute requested action itself.</param>
    /// <returns></returns>
    public async Task CloseProject(bool force, bool dryRun = false) {
        int r_id = Interlocked.Increment(ref requestID);
        IO.Swagger.Model.CloseProjectRequestArgs args = new IO.Swagger.Model.CloseProjectRequestArgs(force);
        IO.Swagger.Model.CloseProjectRequest request = new IO.Swagger.Model.CloseProjectRequest(r_id, "CloseProject", args, dryRun: dryRun);
        SendDataToServer(request.ToJson(), r_id, true);
        IO.Swagger.Model.CloseProjectResponse response = await WaitForResult<IO.Swagger.Model.CloseProjectResponse>(r_id);

        if (response == null || !response.Result)
            throw new RequestFailedException(response == null ? "Request timed out" : response.Messages[0]);
        }
    public async Task<List<string>> GetRobotArms(string robotId) {
        int r_id = Interlocked.Increment(ref requestID);
        GetRobotArmsRequestArgs args = new GetRobotArmsRequestArgs(robotId: robotId);
        IO.Swagger.Model.GetRobotArmsRequest request = new IO.Swagger.Model.GetRobotArmsRequest(r_id, "GetRobotArms", args: args);
        SendDataToServer(request.ToJson(), r_id, true);
        IO.Swagger.Model.GetRobotArmsResponse response = await WaitForResult<IO.Swagger.Model.GetRobotArmsResponse>(r_id);
        if (response == null || !response.Result) {
            throw new RequestFailedException(response == null ? new List<string>() { "Failed to get robot arms" } : response.Messages);
        } else {
            return response.Data;
        }
    }

    public async Task WriteLock(string objId, bool lockTree) {
        int r_id = Interlocked.Increment(ref requestID);
        IO.Swagger.Model.WriteLockRequestArgs args = new WriteLockRequestArgs(lockTree: lockTree, objectId: objId);

        IO.Swagger.Model.WriteLockRequest request = new IO.Swagger.Model.WriteLockRequest(r_id, "WriteLock", args: args);
        SendDataToServer(request.ToJson(), r_id, true);
        IO.Swagger.Model.WriteLockResponse response = await WaitForResult<IO.Swagger.Model.WriteLockResponse>(r_id);
        if (response == null || !response.Result) {
            throw new RequestFailedException(response == null ? new List<string>() { "Failed to lock object" } : response.Messages);
        }
    }

    /// <summary>
    /// Invoked when an object was locked
    /// </summary>
    /// <param name="data">Message from server</param>
    private void HandleObjectLocked(string data) {
        ObjectsLocked objectsLockedEvent = JsonConvert.DeserializeObject<ObjectsLocked>(data);
        HLockingEventCache.Instance.Add(new ObjectLockingEventArgs(objectsLockedEvent.Data.ObjectIds, true, objectsLockedEvent.Data.Owner));
    }

    /// <summary>
    /// Updates position and orientation of action object.
    /// Throws RequestFailedException when request failed
    /// </summary>
    /// <param name="actionObjectId">Id of action object</param>
    /// <param name="pose">Desired pose (position and orientation)</param>
    /// <returns></returns>
    public async Task UpdateActionObjectPose(string actionObjectId, IO.Swagger.Model.Pose pose, bool dryRun = false) {
        if (dryRun)
            return;
        int r_id = Interlocked.Increment(ref requestID);
        IO.Swagger.Model.UpdateObjectPoseRequestArgs args = new IO.Swagger.Model.UpdateObjectPoseRequestArgs
            (objectId: actionObjectId, pose: pose);
        IO.Swagger.Model.UpdateObjectPoseRequest request = new IO.Swagger.Model.UpdateObjectPoseRequest
            (id: r_id, request: "UpdateObjectPose", args);
        SendDataToServer(request.ToJson(), r_id, true);
        IO.Swagger.Model.UpdateObjectPoseResponse response = await WaitForResult<IO.Swagger.Model.UpdateObjectPoseResponse>(r_id);
        if (response == null || !response.Result)
            throw new RequestFailedException(response == null ? "Request timed out" : response.Messages[0]);

    }

    /// <summary>
    /// Decodes changes on scene objects
    /// </summary>
    /// <param name="data">Message from server</param>
    /// <returns></returns>
    private void HandleSceneObjectChanged(string data) {
        IO.Swagger.Model.SceneObjectChanged sceneObjectChanged = JsonConvert.DeserializeObject<IO.Swagger.Model.SceneObjectChanged>(data);
        switch (sceneObjectChanged.ChangeType) {
            case IO.Swagger.Model.SceneObjectChanged.ChangeTypeEnum.Add:
                SceneManagerH.Instance.SceneObjectAdded(sceneObjectChanged.Data);
                break;
            case IO.Swagger.Model.SceneObjectChanged.ChangeTypeEnum.Remove:
                SceneManagerH.Instance.SceneObjectRemoved(sceneObjectChanged.Data);
                break;
            case IO.Swagger.Model.SceneObjectChanged.ChangeTypeEnum.Update:
                SceneManagerH.Instance.SceneObjectUpdated(sceneObjectChanged.Data);
                break;
            case IO.Swagger.Model.SceneObjectChanged.ChangeTypeEnum.Updatebase:
                SceneManagerH.Instance.SceneObjectBaseUpdated(sceneObjectChanged.Data);
                break;
            default:
                throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Asks server to add new logic item (actions connection).
    /// Throws RequestFailedException when request failed
    /// </summary>
    /// <param name="startActionId">UUID of first action (from)</param>
    /// <param name="endActionId">UUID of second action (to)</param>
    /// <param name="dryRun">If true, validates all parameters, but will not execute requested action itself.</param>
    /// <returns></returns>
    public async Task AddLogicItem(string startActionId, string endActionId, IO.Swagger.Model.ProjectLogicIf condition, bool dryRun) {
        int r_id = Interlocked.Increment(ref requestID);
        IO.Swagger.Model.AddLogicItemRequestArgs args = new IO.Swagger.Model.AddLogicItemRequestArgs(start: startActionId, end: endActionId, condition: condition);
        IO.Swagger.Model.AddLogicItemRequest request = new IO.Swagger.Model.AddLogicItemRequest(r_id, "AddLogicItem", args, dryRun: dryRun);
        SendDataToServer(request.ToJson(), r_id, true);
        IO.Swagger.Model.AddLogicItemResponse response = await WaitForResult<IO.Swagger.Model.AddLogicItemResponse>(r_id);

        if (response == null || !response.Result)
            throw new RequestFailedException(response == null ? "Request timed out" : response.Messages[0]);
    }


    public async Task WriteUnlock(string objId) {
        int r_id = Interlocked.Increment(ref requestID);
        IO.Swagger.Model.WriteUnlockRequestArgs args = new WriteUnlockRequestArgs(objectId: objId);

        IO.Swagger.Model.WriteUnlockRequest request = new IO.Swagger.Model.WriteUnlockRequest(r_id, "WriteUnlock", args: args);
        SendDataToServer(request.ToJson(), r_id, true);
        IO.Swagger.Model.WriteUnlockResponse response = await WaitForResult<IO.Swagger.Model.WriteUnlockResponse>(r_id);
        if (response == null || !response.Result) {
            throw new RequestFailedException(response == null ? new List<string>() { "Failed to unlock object" } : response.Messages);
        }
    }
    /// <summary>
    /// If connected to server, returns domain name or IP addr of server
    /// </summary>
    /// <returns>Domain name or IP addr of server</returns>
    public string GetServerDomain() {
        if (websocket.State != WebSocketState.Open) {
            return null;
        }
        return serverDomain;
    }

    
    /// <summary>
    /// Gets available values for selected parameter.
    /// </summary>
    /// <param name="actionProviderId">ID of action provider (only action object at the moment"</param>
    /// <param name="param_id">ID of parameter</param>
    /// <param name="parent_params">List of parent parameters (e.g. to obtain list of available end effectors, robot_id has to be provided"</param>
    /// <returns>List of available options or empty list when request failed</returns>
    public async Task<List<string>> GetActionParamValues(string actionProviderId, string param_id, List<IO.Swagger.Model.IdValue> parent_params) {
        int r_id = Interlocked.Increment(ref requestID);
        IO.Swagger.Model.ActionParamValuesRequestArgs args = new IO.Swagger.Model.ActionParamValuesRequestArgs(id: actionProviderId, paramId: param_id, parentParams: parent_params);
        IO.Swagger.Model.ActionParamValuesRequest request = new IO.Swagger.Model.ActionParamValuesRequest(id: r_id, request: "ActionParamValues", args);
        SendDataToServer(request.ToJson(), r_id, true);
        IO.Swagger.Model.ActionParamValuesResponse response = await WaitForResult<IO.Swagger.Model.ActionParamValuesResponse>(r_id);
        if (response != null && response.Result)
            return response.Data;
        else
            return new List<string>();
    }

    /// <summary>
    /// Waits until response with selected ID is recieved.
    /// </summary>
    /// <typeparam name="T">Type of reposnse</typeparam>
    /// <param name="key">ID of response</param>
    /// <param name="timeout">Time [ms] after which timeout exception is thrown</param>
    /// <returns>Decoded response</returns>
    private Task<T> WaitForResult<T>(int key, int timeout = 15000) {
        return Task.Run(() => {
            if (responses.TryGetValue(key, out string value)) {
                if (value == null) {
                    Task<string> result = WaitForResponseReady(key, timeout);
                    if (!result.Wait(timeout)) {
                        Debug.LogError("The timeout interval elapsed.");
                        //TODO: throw an exception and handle it properly
                        //throw new TimeoutException();
                        return default;
                    } else {
                        value = result.Result;
                    }
                }
                return JsonConvert.DeserializeObject<T>(value);
            } else {
                return default;
            }
        });
    }



    /// <summary>
    /// Loads all projects from server.
    /// Throws RequestFailedException when request failed
    /// </summary>
    /// <returns>List of projects metadata</returns>
    public void LoadProjects(UnityAction<string, string> callback) {
        int r_id = Interlocked.Increment(ref requestID);
        responsesCallback.Add(r_id, Tuple.Create("", callback));
        IO.Swagger.Model.ListProjectsRequest request = new IO.Swagger.Model.ListProjectsRequest(id: r_id, request: "ListProjects");
        SendDataToServer(request.ToJson(), r_id, false);

    }

    public void LoadScenes(UnityAction<string, string> callback) {
        int r_id = Interlocked.Increment(ref requestID);
        responsesCallback.Add(r_id, Tuple.Create("", callback));
        IO.Swagger.Model.ListScenesRequest request = new IO.Swagger.Model.ListScenesRequest(id: r_id, request: "ListScenes");
        SendDataToServer(request.ToJson(), r_id, false);
    }

        // TODO: add timeout!
    /// <summary>
    /// WWaits until response with selected ID is recieved.
    /// </summary>
    /// <param name="key">ID of reponse</param>
    /// <param name="timeout">Not used</param>
    /// <returns>Raw response</returns>
    private Task<string> WaitForResponseReady(int key, int timeout) {
        return Task.Run(() => {
            while (true) {
                if (responses.TryGetValue(key, out string value)) {
                    if (value != null) {
                        return value;
                    } else {
                        Thread.Sleep(10);
                    }
                }
            }
        });

    }
    
    /// <summary>
    /// Gets metadata about robots
    /// </summary>
    /// <returns>List of metadatas of robots</returns>
    public async Task<List<IO.Swagger.Model.RobotMeta>> GetRobotMeta() {
        int r_id = Interlocked.Increment(ref requestID);
        IO.Swagger.Model.GetRobotMetaRequest request = new IO.Swagger.Model.GetRobotMetaRequest(r_id, "GetRobotMeta");
        SendDataToServer(request.ToJson(), r_id, true);
        IO.Swagger.Model.GetRobotMetaResponse response = await WaitForResult<IO.Swagger.Model.GetRobotMetaResponse>(r_id);
        if (response == null || !response.Result) {
               HNotificationManager.Instance.ShowNotification("Failed to get robot meta");
            throw new RequestFailedException(response == null ? new List<string>() { "Failed to get robot meta" } : response.Messages);
        } else {
            return response.Data;
        }
    }


    /// <summary>
    /// Gets information about server (server version, api version, list of supported parameters and list of available RPCs).
    /// Throws RequestFailedException when request failed
    /// </summary>
    /// <returns>Information about server</returns>
    public async Task<IO.Swagger.Model.SystemInfoResponseData> GetSystemInfo() {
        int r_id = Interlocked.Increment(ref requestID);

        IO.Swagger.Model.SystemInfoRequest request = new IO.Swagger.Model.SystemInfoRequest(id: r_id, request: "SystemInfo");
        SendDataToServer(request.ToJson(), r_id, true);
        IO.Swagger.Model.SystemInfoResponse response = await WaitForResult<IO.Swagger.Model.SystemInfoResponse>(r_id);
        if (response == null || !response.Result)
            throw new RequestFailedException(response == null ? "Request timed out" : response.Messages[0]);
        return response.Data;
    }


    public async Task RegisterUser(string username) {
        int r_id = Interlocked.Increment(ref requestID);
        IO.Swagger.Model.RegisterUserRequestArgs args = new RegisterUserRequestArgs(username);
        IO.Swagger.Model.RegisterUserRequest request = new RegisterUserRequest(r_id, "RegisterUser", args);
        SendDataToServer(request.ToJson(), r_id, true);
        IO.Swagger.Model.RegisterUserResponse response = await WaitForResult<IO.Swagger.Model.RegisterUserResponse>(r_id);
        if (response == null || !response.Result) {
            throw new RequestFailedException(response == null ? new List<string>() { "Failed to register user" } : response.Messages);
        }
    }

    /// <summary>
    /// Loads object types from server. Throws RequestFailedException when request failed
    /// </summary>
    /// <returns>List of object types</returns>
    public async Task<List<IO.Swagger.Model.ObjectTypeMeta>> GetObjectTypes() {
        int id = Interlocked.Increment(ref requestID);
        SendDataToServer(new IO.Swagger.Model.GetObjectTypesRequest(id: id, request: "GetObjectTypes").ToJson(), id, true);
        IO.Swagger.Model.GetObjectTypesResponse response = await WaitForResult<IO.Swagger.Model.GetObjectTypesResponse>(id);
        if (response != null && response.Result)
            return response.Data;
        else {
            throw new RequestFailedException("Failed to load object types");
        }

    }

    /// <summary>
    /// Asks server to rename action object.
    /// Throws RequestFailedException when request failed
    /// </summary>
    /// <param name="id">UUID of action object</param>
    /// <param name="newName">New human readable name of action objects</param>
    /// <returns></returns>
    public async Task RenameObject(string id, string newName) {
        int r_id = Interlocked.Increment(ref requestID);
        IO.Swagger.Model.RenameArgs args = new IO.Swagger.Model.RenameArgs(id: id, newName: newName);
        IO.Swagger.Model.RenameObjectRequest request = new IO.Swagger.Model.RenameObjectRequest(r_id, "RenameObject", args);
        SendDataToServer(request.ToJson(), r_id, true);
        IO.Swagger.Model.RenameObjectResponse response = await WaitForResult<IO.Swagger.Model.RenameObjectResponse>(r_id);

        if (response == null || !response.Result)
            throw new RequestFailedException(response == null ? "Request timed out" : response.Messages[0]);
    }

    /// <summary>
    /// Asks server to open scene
    /// </summary>
    /// <param name="scene_id">Id of scene to be openned</param>
    /// <returns></returns>
    public async Task OpenScene(string scene_id) {
        int r_id = Interlocked.Increment(ref requestID);
        IO.Swagger.Model.IdArgs args = new IO.Swagger.Model.IdArgs(id: scene_id);
        IO.Swagger.Model.OpenSceneRequest request = new IO.Swagger.Model.OpenSceneRequest(id: r_id, request: "OpenScene", args: args);
        SendDataToServer(request.ToJson(), r_id, true);
        IO.Swagger.Model.OpenSceneResponse response = await WaitForResult<IO.Swagger.Model.OpenSceneResponse>(r_id, 30000);
        if (response == null || !response.Result) {
            throw new RequestFailedException(response == null ? "Request timed out" : response.Messages[0]);
        }
    }

    
    /// <summary>
    /// Asks server to open project. Throws RequestFailedException when request failed
    /// </summary>
    /// <param name="id">Id of project</param>
    /// <returns></returns>
    public async Task OpenProject(string id) {
        int r_id = Interlocked.Increment(ref requestID);
        IO.Swagger.Model.IdArgs args = new IO.Swagger.Model.IdArgs(id: id);
        IO.Swagger.Model.OpenProjectRequest request = new IO.Swagger.Model.OpenProjectRequest(id: r_id, request: "OpenProject", args);
        SendDataToServer(request.ToJson(), r_id, true);
        IO.Swagger.Model.OpenProjectResponse response = await WaitForResult<IO.Swagger.Model.OpenProjectResponse>(r_id, 30000);
        if (response == null || !response.Result)
            throw new RequestFailedException(response == null ? "Request timed out" : response.Messages[0]);
    }

    /// <summary>
    /// Asks server to update action point position.
    /// Throws RequestFailedException when request failed
    /// </summary>
    /// <param name="id">UUID of action point</param>
    /// <param name="position">New position of action point.</param>
    /// <returns></returns>
    public async Task UpdateActionPointPosition(string id, IO.Swagger.Model.Position position, bool dryRun = false) {
        int r_id = Interlocked.Increment(ref requestID);
        IO.Swagger.Model.UpdateActionPointPositionRequestArgs args = new IO.Swagger.Model.UpdateActionPointPositionRequestArgs(actionPointId: id, newPosition: position);
        IO.Swagger.Model.UpdateActionPointPositionRequest request = new IO.Swagger.Model.UpdateActionPointPositionRequest(r_id, "UpdateActionPointPosition", args, dryRun: dryRun);
        SendDataToServer(request.ToJson(), key: r_id, true);
        IO.Swagger.Model.UpdateActionPointPositionResponse response = await WaitForResult<IO.Swagger.Model.UpdateActionPointPositionResponse>(r_id);

        if (response == null || !response.Result)
            throw new RequestFailedException(response == null ? "Request timed out" : response.Messages[0]);
    }


    /// <summary>
    /// Asks server to remove action.
    /// Throws RequestFailedException when request failed
    /// </summary>
    /// <param name="actionId">UUID of action</param>
    /// <param name="dryRun">If true, validates all parameters, but will not execute requested action itself.</param>
    /// <returns></returns>
    public async Task RemoveAction(string actionId, bool dryRun) {
        int r_id = Interlocked.Increment(ref requestID);
        IO.Swagger.Model.IdArgs args = new IO.Swagger.Model.IdArgs(id: actionId);
        IO.Swagger.Model.RemoveActionRequest request = new IO.Swagger.Model.RemoveActionRequest(r_id, "RemoveAction", args, dryRun: dryRun);
        SendDataToServer(request.ToJson(), r_id, true, logInfo: !dryRun);
        IO.Swagger.Model.RemoveActionResponse response = await WaitForResult<IO.Swagger.Model.RemoveActionResponse>(r_id);

        if (response == null || !response.Result)
            throw new RequestFailedException(response == null ? "Request timed out" : response.Messages[0]);
    }

    /// <summary>
    /// Asks server to rename action.
    /// Throws RequestFailedException when request failed
    /// </summary>
    /// <param name="actionId">UUID of action</param>
    /// <param name="newName">New human readable name of action.</param>
    /// <returns></returns>
    public async Task RenameAction(string actionId, string newName) {
        int r_id = Interlocked.Increment(ref requestID);
        IO.Swagger.Model.RenameActionRequestArgs args = new IO.Swagger.Model.RenameActionRequestArgs(actionId: actionId, newName: newName);
        IO.Swagger.Model.RenameActionRequest request = new IO.Swagger.Model.RenameActionRequest(r_id, "RenameAction", args);
        SendDataToServer(request.ToJson(), r_id, true);
        IO.Swagger.Model.RenameActionResponse response = await WaitForResult<IO.Swagger.Model.RenameActionResponse>(r_id);

        if (response == null || !response.Result)
            throw new RequestFailedException(response == null ? "Request timed out" : response.Messages[0]);
    }


    /// <summary>
    /// Asks server to remove action point.
    /// Throws RequestFailedException when request failed
    /// </summary>
    /// <param name="actionPointId">UUID of aciton point</param>
    /// <param name="dryRun">If true, validates all parameters, but will not execute requested action itself.</param>
    /// <returns></returns>
    public async Task RemoveActionPoint(string actionPointId, bool dryRun = false) {
        int r_id = Interlocked.Increment(ref requestID);
        IO.Swagger.Model.IdArgs args = new IO.Swagger.Model.IdArgs(actionPointId);
        IO.Swagger.Model.RemoveActionPointRequest request = new IO.Swagger.Model.RemoveActionPointRequest(r_id, "RemoveActionPoint", args, dryRun: dryRun);
        SendDataToServer(request.ToJson(), r_id, true, logInfo: !dryRun);
        IO.Swagger.Model.RemoveActionPointResponse response = await WaitForResult<IO.Swagger.Model.RemoveActionPointResponse>(r_id);

        if (response == null || !response.Result)
            throw new RequestFailedException(response == null ? "Request timed out" : response.Messages[0]);

    }

    
    /// <summary>
    /// Asks server to rename aciton point.
    /// </summary>
    /// <param name="id">UUID of action point</param>
    /// <param name="name">New human readable name of action point</param>
    /// <returns></returns>
    public async Task RenameActionPoint(string id, string name) {
        int r_id = Interlocked.Increment(ref requestID);
        IO.Swagger.Model.RenameActionPointRequestArgs args = new IO.Swagger.Model.RenameActionPointRequestArgs(actionPointId: id, newName: name);
        IO.Swagger.Model.RenameActionPointRequest request = new IO.Swagger.Model.RenameActionPointRequest(r_id, "RenameActionPoint", args);
        SendDataToServer(request.ToJson(), r_id, true);
        IO.Swagger.Model.RenameActionPointResponse response = await WaitForResult<IO.Swagger.Model.RenameActionPointResponse>(r_id);

        if (response == null || !response.Result)
            throw new RequestFailedException(response == null ? "Request timed out" : response.Messages[0]);
    }


    /// <summary>
    /// Asks server to remove action point orientation.
    /// Throws RequestFailedException when request failed
    /// </summary>
    /// <param name="actionPointId">UUID of action point</param>
    /// <param name="orientationId">UUID of orientation</param>
    /// <returns></returns>
    public async Task RemoveActionPointOrientation(string orientationId, bool dryRun) {
        int r_id = Interlocked.Increment(ref requestID);
        IO.Swagger.Model.RemoveActionPointOrientationRequestArgs args = new IO.Swagger.Model.RemoveActionPointOrientationRequestArgs(orientationId: orientationId);
        IO.Swagger.Model.RemoveActionPointOrientationRequest request = new IO.Swagger.Model.RemoveActionPointOrientationRequest(r_id, "RemoveActionPointOrientation", args, dryRun: dryRun);
        SendDataToServer(request.ToJson(), r_id, true, logInfo: !dryRun);
        IO.Swagger.Model.RemoveActionPointOrientationResponse response = await WaitForResult<IO.Swagger.Model.RemoveActionPointOrientationResponse>(r_id);

        if (response == null || !response.Result)
            throw new RequestFailedException(response == null ? "Request timed out" : response.Messages[0]);
    }

    
    /// <summary>
    /// Asks server to rename action point orientation
    /// Throws RequestFailedException when request failed
    /// </summary>
    /// <param name="orientationId">Id of orientation</param>
    /// <param name="newName">New human-readable name</param>
    /// <returns></returns>
    public async Task RenameActionPointOrientation(string orientationId, string newName) {
        int r_id = Interlocked.Increment(ref requestID);
        IO.Swagger.Model.RenameActionPointOrientationRequestArgs args = new IO.Swagger.Model.RenameActionPointOrientationRequestArgs(newName: newName, orientationId: orientationId);
        IO.Swagger.Model.RenameActionPointOrientationRequest request = new IO.Swagger.Model.RenameActionPointOrientationRequest(r_id, "RenameActionPointOrientation", args);
        SendDataToServer(request.ToJson(), r_id, true);
        IO.Swagger.Model.RenameActionPointJointsResponse response = await WaitForResult<IO.Swagger.Model.RenameActionPointJointsResponse>(r_id);

        if (response == null || !response.Result)
            throw new RequestFailedException(response == null ? "Request timed out" : response.Messages[0]);
    }



}

