using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Threading.Tasks;
using System.IO;


namespace Base {
    public class WebsocketManager : Singleton<WebsocketManager> {
        public string APIDomainWS = "";

        private ClientWebSocket clientWebSocket;

        private Queue<KeyValuePair<int, string>> sendingQueue = new Queue<KeyValuePair<int, string>>();

        private bool waitingForMessage = false;

        private string receivedData;

        private bool readyToSend, ignoreProjectChanged, connecting;

        private Dictionary<int, string> responses = new Dictionary<int, string>();

        private int requestID = 1;
        
        private bool projectArrived = false, sceneArrived = false, projectStateArrived = false;

        private void Awake() {
            waitingForMessage = false;
            readyToSend = true;
            ignoreProjectChanged = false;
            connecting = false;            
            receivedData = "";
        }


        public async Task<bool> ConnectToServer(string domain, int port) {
            GameManager.Instance.ConnectionStatus = GameManager.ConnectionStatusEnum.Connecting;
            projectArrived = false;
            sceneArrived = false;
            projectStateArrived = false;
            connecting = true;
            APIDomainWS = GetWSURI(domain, port);
            clientWebSocket = new ClientWebSocket();
            Debug.Log("[WS]:Attempting connection.");
            try {
                Uri uri = new Uri(APIDomainWS);
                await clientWebSocket.ConnectAsync(uri, CancellationToken.None);

                Debug.Log("[WS][connect]:" + "Connected");
            } catch (Exception e) {
                Debug.Log("[WS][exception]:" + e.Message);
                if (e.InnerException != null) {
                    Debug.Log("[WS][inner exception]:" + e.InnerException.Message);
                }
            }

            connecting = false;
            
            return clientWebSocket.State == WebSocketState.Open;
        }

        async public void DisconnectFromSever() {
            Debug.Log("Disconnecting");
            GameManager.Instance.ConnectionStatus = GameManager.ConnectionStatusEnum.Disconnected;
            try {
                await clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Done", CancellationToken.None);
            } catch (WebSocketException e) {
                //already closed probably..
            }
            clientWebSocket = null;
        }

        /// <summary>
        /// Waits until all post-connection data arrived from server or until timeout exprires
        /// </summary>
        /// <param name="timeout">Timeout in ms</param>
        public async Task WaitForInitData(int timeout) {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            while (!CheckInitData()) {
                if (sw.ElapsedMilliseconds > timeout)
                    throw new TimeoutException();
                Thread.Sleep(100);
            }
            return;
        }

        public bool CheckInitData() {
            return projectArrived && sceneArrived && projectStateArrived;
        }

        // Update is called once per frame
        private async void Update() {
            if (clientWebSocket == null)
                return;
            if (clientWebSocket.State != WebSocketState.Open && GameManager.Instance.ConnectionStatus == GameManager.ConnectionStatusEnum.Connected) {
                GameManager.Instance.ConnectionStatus = GameManager.ConnectionStatusEnum.Disconnected;
            }

            if (!waitingForMessage && clientWebSocket.State == WebSocketState.Open) {
                WebSocketReceiveResult result = null;
                waitingForMessage = true;
                ArraySegment<byte> bytesReceived = WebSocket.CreateClientBuffer(8192, 8192);
                MemoryStream ms = new MemoryStream();
                try {
                    do {
                        result = await clientWebSocket.ReceiveAsync(
                            bytesReceived,
                            CancellationToken.None
                        );

                        if (bytesReceived.Array != null)
                            ms.Write(bytesReceived.Array, bytesReceived.Offset, result.Count);

                    } while (!result.EndOfMessage);
                } catch (WebSocketException e) {
                    DisconnectFromSever();
                    return;
                }
                
                receivedData = Encoding.Default.GetString(ms.ToArray());
                HandleReceivedData(receivedData);
                receivedData = "";
                waitingForMessage = false;

            }

            if (sendingQueue.Count > 0 && readyToSend) {
                SendDataToServer();
            }

        }

        public string GetWSURI(string domain, int port) {
            return "ws://" + domain + ":" + port.ToString();
        }

        void OnApplicationQuit() {
            DisconnectFromSever();
        }

        public void SendDataToServer(string data, int key = -1, bool storeResult = false) {
            if (key < 0) {
                key = Interlocked.Increment(ref requestID);
            }
            Debug.Log("Sending data to server: " + data);

            if (storeResult) {
                responses[key] = null;
            }
            sendingQueue.Enqueue(new KeyValuePair<int, string>(key, data));
        }

        async public void SendDataToServer() {
            if (sendingQueue.Count == 0)
                return;
            KeyValuePair<int, string> keyVal = sendingQueue.Dequeue();
            readyToSend = false;
            if (clientWebSocket.State != WebSocketState.Open)
                return;

            ArraySegment<byte> bytesToSend = new ArraySegment<byte>(
                         Encoding.UTF8.GetBytes(keyVal.Value)
                     );
            await clientWebSocket.SendAsync(
                bytesToSend,
                WebSocketMessageType.Text,
                true,
                CancellationToken.None
            );
            readyToSend = true;
        }

        public void UpdateObjectTypes() {
            SendDataToServer(new IO.Swagger.Model.GetObjectTypesRequest(request: "GetObjectTypes").ToJson());
        }

        public void UpdateObjectActions(string ObjectId) {
            SendDataToServer(new IO.Swagger.Model.GetActionsRequest(request: "GetActions", args: new IO.Swagger.Model.TypeArgs(type: ObjectId)).ToJson());
        }


        private void HandleReceivedData(string data) {
            var dispatchType = new {
                id = 0,
                response = "",
                @event = "",
                request = ""
            };
            
            var dispatch = JsonConvert.DeserializeAnonymousType(data, dispatchType);

            if (dispatch?.response == null && dispatch?.request == null && dispatch?.@event == null)
                return;
            //if (dispatch?.@event != null && dispatch.@event != "ActionState" && dispatch.@event != "CurrentAction")
            Debug.Log("Recieved new data: " + data);
            if (dispatch.response != null) {

                if (responses.ContainsKey(dispatch.id)) {
                    responses[dispatch.id] = data;
                } else {
                    // TODO: response to unknown request
                }
                   
            } else if (dispatch.@event != null) {
                switch (dispatch.@event) {
                    case "SceneChanged":
                        HandleSceneChanged(data);
                        break;
                    case "SceneObjectChanged":
                        HandleSceneObjectChanged(data);
                        break;
                    case "SceneServiceChanged":
                        HandleSceneServiceChanged(data);
                        break;
                    case "ActionPointChanged":
                        HandleActionPointChanged(data);
                        break;
                    case "ActionChanged":
                        HandleActionChanged(data);
                        break;
                    case "OrientationChanged":
                        HandleOrientationChanged(data);
                        break;
                    case "JointsChanged":
                        HandleJointsChanged(data);
                        break;
                    case "CurrentAction":
                        HandleCurrentAction(data);
                        break;
                    case "ProjectState":
                        HandleProjectState(data);
                        break;
                    case "ProjectSaved":
                        HandleProjectSaved(data);
                        break;
                    case "ProjectException":
                        HandleProjectException(data);
                        break;
                    case "ActionResult":
                        HandleActionResult(data);
                        break;
                    case "ProjectChanged":
                        if (ignoreProjectChanged)
                            ignoreProjectChanged = false;
                        else
                            HandleProjectChanged(data);
                        break;
                }
            }

        }

        private void HandleProjectSaved(string data) {
            GameManager.Instance.ProjectSaved();
        }

        private async Task<T> WaitForResult<T>(int key) {
            if (responses.TryGetValue(key, out string value)) {
                if (value == null) {
                    value = await WaitForResponseReady(key);
                }
                return JsonConvert.DeserializeObject<T>(value);
            } else {
                return default;
            }
        }

        // TODO: add timeout!
        private Task<string> WaitForResponseReady(int key) {
            return Task.Run(() => {
                while (true) {
                    if (responses.TryGetValue(key, out string value)) {
                        if (value != null) {
                            return value;
                        } else {
                            Thread.Sleep(100);
                        }
                    }
                }
            });
        }

        private async void HandleProjectChanged(string obj) {

            try {
                GameManager.Instance.ProjectChanged = true;
                IO.Swagger.Model.ProjectChanged eventProjectChanged = JsonConvert.DeserializeObject<IO.Swagger.Model.ProjectChanged>(obj);
                switch (eventProjectChanged.ChangeType) {
                    case IO.Swagger.Model.ProjectChanged.ChangeTypeEnum.Add:
                        GameManager.Instance.ProjectAdded(eventProjectChanged.Data);
                        break;
                    case IO.Swagger.Model.ProjectChanged.ChangeTypeEnum.Remove:
                        await GameManager.Instance.LoadProjects();
                        break;
                    case IO.Swagger.Model.ProjectChanged.ChangeTypeEnum.Update:
                        GameManager.Instance.ProjectUpdated(eventProjectChanged.Data);
                        break;
                    case IO.Swagger.Model.ProjectChanged.ChangeTypeEnum.Updatebase:
                        GameManager.Instance.ProjectBaseUpdated(eventProjectChanged.Data);
                        break;
                    default:
                        throw new NotImplementedException();
                }
                projectArrived = true;
            } catch (NullReferenceException e) {
                Debug.Log("Parse error in HandleProjectChanged()");
                GameManager.Instance.ProjectUpdated(null);

            }

        }

        private void HandleCurrentAction(string obj) {
            string puck_id;
            try {
                
                IO.Swagger.Model.CurrentActionEvent currentActionEvent = JsonConvert.DeserializeObject<IO.Swagger.Model.CurrentActionEvent>(obj);

                puck_id = currentActionEvent.Data.ActionId;



            } catch (NullReferenceException e) {
                Debug.Log("Parse error in HandleCurrentAction()");
                return;
            }

            Action puck = Scene.Instance.GetAction(puck_id);
            if (puck == null)
                return;

            // Stop previously running action (change its color to default)
            if(ActionsManager.Instance.CurrentlyRunningAction != null)
                ActionsManager.Instance.CurrentlyRunningAction.StopAction();

            ActionsManager.Instance.CurrentlyRunningAction = puck;
            // Run current action (set its color to running)
            puck.RunAction();
        }

        private void HandleActionResult(string data) {
            IO.Swagger.Model.ActionResultEvent actionResult = JsonConvert.DeserializeObject<IO.Swagger.Model.ActionResultEvent>(data);
            GameManager.Instance.HandleActionResult(actionResult.Data);
        }

        private void HandleProjectException(string data) {
            IO.Swagger.Model.ProjectExceptionEvent projectException = JsonConvert.DeserializeObject<IO.Swagger.Model.ProjectExceptionEvent>(data);
            GameManager.Instance.HandleProjectException(projectException.Data);
        }

        private void HandleProjectState(string obj) {
            IO.Swagger.Model.ProjectStateEvent projectState = JsonConvert.DeserializeObject<IO.Swagger.Model.ProjectStateEvent>(obj);
            GameManager.Instance.SetProjectState(projectState.Data);
            projectStateArrived = true;
        }

        private async void HandleSceneChanged(string obj) {
            IO.Swagger.Model.SceneChanged sceneChangedEvent = JsonConvert.DeserializeObject<IO.Swagger.Model.SceneChanged>(obj);
            switch (sceneChangedEvent.ChangeType) {
                case IO.Swagger.Model.SceneChanged.ChangeTypeEnum.Add:
                    GameManager.Instance.SceneAdded(sceneChangedEvent.Data);
                    break;
                case IO.Swagger.Model.SceneChanged.ChangeTypeEnum.Remove:
                    await GameManager.Instance.LoadScenes();
                    break;
                case IO.Swagger.Model.SceneChanged.ChangeTypeEnum.Update:
                    GameManager.Instance.SceneUpdated(sceneChangedEvent.Data);
                    break;
                case IO.Swagger.Model.SceneChanged.ChangeTypeEnum.Updatebase:
                    GameManager.Instance.SceneBaseUpdated(sceneChangedEvent.Data);
                    break;
                default:
                    throw new NotImplementedException();
            }
            sceneArrived = true;
        }

        private void HandleActionChanged(string data) {
            IO.Swagger.Model.ActionChanged actionChanged = JsonConvert.DeserializeObject<IO.Swagger.Model.ActionChanged>(data);
            GameManager.Instance.ProjectChanged = true;
            switch (actionChanged.ChangeType) {
                case IO.Swagger.Model.ActionChanged.ChangeTypeEnum.Add:
                    GameManager.Instance.ActionAdded(actionChanged.Data, actionChanged.ParentId);
                    break;
                case IO.Swagger.Model.ActionChanged.ChangeTypeEnum.Remove:
                    GameManager.Instance.ActionRemoved(actionChanged.Data);
                    break;
                case IO.Swagger.Model.ActionChanged.ChangeTypeEnum.Update:
                    GameManager.Instance.ActionUpdated(actionChanged.Data);
                    break;
                case IO.Swagger.Model.ActionChanged.ChangeTypeEnum.Updatebase:
                    GameManager.Instance.ActionBaseUpdated(actionChanged.Data);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private void HandleActionPointChanged(string data) {
            GameManager.Instance.ProjectChanged = true;
            IO.Swagger.Model.ActionPointChanged actionPointChangedEvent = JsonConvert.DeserializeObject<IO.Swagger.Model.ActionPointChanged>(data);
            switch (actionPointChangedEvent.ChangeType) {
                case IO.Swagger.Model.ActionPointChanged.ChangeTypeEnum.Add:
                    GameManager.Instance.ActionPointAdded(actionPointChangedEvent.Data);
                    break;
                case IO.Swagger.Model.ActionPointChanged.ChangeTypeEnum.Remove:
                    GameManager.Instance.ActionPointRemoved(actionPointChangedEvent.Data);
                    break;
                case IO.Swagger.Model.ActionPointChanged.ChangeTypeEnum.Update:
                    GameManager.Instance.ActionPointUpdated(actionPointChangedEvent.Data);
                    break;
                case IO.Swagger.Model.ActionPointChanged.ChangeTypeEnum.Updatebase:
                    GameManager.Instance.ActionPointBaseUpdated(actionPointChangedEvent.Data);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private void HandleOrientationChanged(string data) {
            GameManager.Instance.ProjectChanged = true;
            IO.Swagger.Model.OrientationChanged orientationChanged = JsonConvert.DeserializeObject<IO.Swagger.Model.OrientationChanged>(data);
            switch (orientationChanged.ChangeType) {
                case IO.Swagger.Model.OrientationChanged.ChangeTypeEnum.Add:
                    GameManager.Instance.ActionPointOrientationAdded(orientationChanged.Data, orientationChanged.ParentId);
                    break;
                case IO.Swagger.Model.OrientationChanged.ChangeTypeEnum.Remove:
                    GameManager.Instance.ActionPointOrientationRemoved(orientationChanged.Data);
                    break;
                case IO.Swagger.Model.OrientationChanged.ChangeTypeEnum.Update:
                    GameManager.Instance.ActionPointOrientationUpdated(orientationChanged.Data);
                    break;
                case IO.Swagger.Model.OrientationChanged.ChangeTypeEnum.Updatebase:
                    GameManager.Instance.ActionPointOrientationBaseUpdated(orientationChanged.Data);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private void HandleJointsChanged(string data) {
            GameManager.Instance.ProjectChanged = true;
            IO.Swagger.Model.JointsChanged jointsChanged = JsonConvert.DeserializeObject<IO.Swagger.Model.JointsChanged>(data);
            switch (jointsChanged.ChangeType) {
                case IO.Swagger.Model.JointsChanged.ChangeTypeEnum.Add:
                    GameManager.Instance.ActionPointJointsAdded(jointsChanged.Data, jointsChanged.ParentId);
                    break;
                case IO.Swagger.Model.JointsChanged.ChangeTypeEnum.Remove:
                    GameManager.Instance.ActionPointJointsRemoved(jointsChanged.Data);
                    break;
                case IO.Swagger.Model.JointsChanged.ChangeTypeEnum.Update:
                    GameManager.Instance.ActionPointJointsUpdated(jointsChanged.Data);
                    break;
                case IO.Swagger.Model.JointsChanged.ChangeTypeEnum.Updatebase:
                    GameManager.Instance.ActionPointJointsBaseUpdated(jointsChanged.Data);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private void HandleSceneObjectChanged(string data) {
            IO.Swagger.Model.SceneObjectChanged sceneObjectChanged = JsonConvert.DeserializeObject<IO.Swagger.Model.SceneObjectChanged>(data);
            switch (sceneObjectChanged.ChangeType) {
                case IO.Swagger.Model.SceneObjectChanged.ChangeTypeEnum.Add:
                    GameManager.Instance.SceneObjectAdded(sceneObjectChanged.Data);
                    break;
                case IO.Swagger.Model.SceneObjectChanged.ChangeTypeEnum.Remove:
                    GameManager.Instance.SceneObjectRemoved(sceneObjectChanged.Data);
                    break;
                case IO.Swagger.Model.SceneObjectChanged.ChangeTypeEnum.Update:
                    GameManager.Instance.SceneObjectUpdated(sceneObjectChanged.Data);
                    break;
                case IO.Swagger.Model.SceneObjectChanged.ChangeTypeEnum.Updatebase:
                    GameManager.Instance.SceneObjectBaseUpdated(sceneObjectChanged.Data);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }


        private void HandleSceneServiceChanged(string data) {
            IO.Swagger.Model.SceneServiceChanged sceneObjectChanged = JsonConvert.DeserializeObject<IO.Swagger.Model.SceneServiceChanged>(data);

            switch (sceneObjectChanged.ChangeType) {
                case IO.Swagger.Model.SceneServiceChanged.ChangeTypeEnum.Add:
                    ActionsManager.Instance.AddService(sceneObjectChanged.Data);
                    break;
                case IO.Swagger.Model.SceneServiceChanged.ChangeTypeEnum.Remove:
                    ActionsManager.Instance.RemoveService(sceneObjectChanged.Data.Type);
                    break;
                case IO.Swagger.Model.SceneServiceChanged.ChangeTypeEnum.Update:
                    ActionsManager.Instance.UpdateService(sceneObjectChanged.Data);
                    break;
                case IO.Swagger.Model.SceneServiceChanged.ChangeTypeEnum.Updatebase:
                    ActionsManager.Instance.UpdateService(sceneObjectChanged.Data);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }



        private void HandleOpenProject(string data) {
            IO.Swagger.Model.OpenProjectResponse response = JsonConvert.DeserializeObject<IO.Swagger.Model.OpenProjectResponse>(data);
        }

        public async Task<List<IO.Swagger.Model.ObjectTypeMeta>> GetObjectTypes() {
            int id = Interlocked.Increment(ref requestID);
            SendDataToServer(new IO.Swagger.Model.GetObjectTypesRequest(id: id, request: "GetObjectTypes").ToJson(), id, true);
            IO.Swagger.Model.GetObjectTypesResponse response = await WaitForResult<IO.Swagger.Model.GetObjectTypesResponse>(id);
            if (response.Result)
                return response.Data;
            else {
                throw new RequestFailedException("Failed to load object types");
            }

        }

        public async Task<List<IO.Swagger.Model.ObjectAction>> GetActions(string name) {
            int id = Interlocked.Increment(ref requestID);
            SendDataToServer(new IO.Swagger.Model.GetActionsRequest(id: id, request: "GetActions", args: new IO.Swagger.Model.TypeArgs(type: name)).ToJson(), id, true);
            IO.Swagger.Model.GetActionsResponse response = await WaitForResult<IO.Swagger.Model.GetActionsResponse>(id);
            if (response.Result)
                return response.Data;
            else
                throw new RequestFailedException("Failed to load actions for object/service " + name);
        }

        public async Task<IO.Swagger.Model.SaveSceneResponse> SaveScene() {
            int id = Interlocked.Increment(ref requestID);
            SendDataToServer(new IO.Swagger.Model.SaveSceneRequest(id: id, request: "SaveScene").ToJson(), id, true);
            return await WaitForResult<IO.Swagger.Model.SaveSceneResponse>(id);
        }

        public async Task<IO.Swagger.Model.SaveProjectResponse> SaveProject() {
            int id = Interlocked.Increment(ref requestID);
            SendDataToServer(new IO.Swagger.Model.SaveProjectRequest(id: id, request: "SaveProject").ToJson(), id, true);
            return await WaitForResult<IO.Swagger.Model.SaveProjectResponse>(id);
        }

        public async Task OpenProject(string id) {
            int r_id = Interlocked.Increment(ref requestID);
            IO.Swagger.Model.IdArgs args = new IO.Swagger.Model.IdArgs(id: id);
            IO.Swagger.Model.OpenProjectRequest request = new IO.Swagger.Model.OpenProjectRequest(id: r_id, request: "OpenProject", args);
            SendDataToServer(request.ToJson(), r_id, true);
            IO.Swagger.Model.OpenProjectResponse response = await WaitForResult<IO.Swagger.Model.OpenProjectResponse>(r_id);
            if (!response.Result)
                throw new RequestFailedException(response.Messages);
        }

        public async Task RunProject(string projectId) {
            int r_id = Interlocked.Increment(ref requestID);
            IO.Swagger.Model.IdArgs args = new IO.Swagger.Model.IdArgs(id: projectId);
            IO.Swagger.Model.RunProjectRequest request = new IO.Swagger.Model.RunProjectRequest(id: r_id, request: "RunProject", args);
            SendDataToServer(request.ToJson(), r_id, true);
            IO.Swagger.Model.RunProjectResponse response = await WaitForResult<IO.Swagger.Model.RunProjectResponse>(r_id);
            if (!response.Result)
                throw new RequestFailedException(response.Messages);
        }

        public async Task StopProject() {
            int r_id = Interlocked.Increment(ref requestID);
            IO.Swagger.Model.StopProjectRequest request = new IO.Swagger.Model.StopProjectRequest(id: r_id, request: "StopProject");
            SendDataToServer(request.ToJson(), r_id, true);
            IO.Swagger.Model.StopProjectResponse response = await WaitForResult<IO.Swagger.Model.StopProjectResponse>(r_id);
            if (!response.Result)
                throw new RequestFailedException(response.Messages);
        }

        public async Task PauseProject() {
            int r_id = Interlocked.Increment(ref requestID);
            IO.Swagger.Model.PauseProjectRequest request = new IO.Swagger.Model.PauseProjectRequest(id: r_id, request: "PauseProject");
            SendDataToServer(request.ToJson(), r_id, true);
            IO.Swagger.Model.PauseProjectResponse response = await WaitForResult<IO.Swagger.Model.PauseProjectResponse>(r_id);
            if (!response.Result)
                throw new RequestFailedException(response.Messages);
        }

        public async Task ResumeProject() {
            int r_id = Interlocked.Increment(ref requestID);
            IO.Swagger.Model.ResumeProjectRequest request = new IO.Swagger.Model.ResumeProjectRequest(id: r_id, request: "ResumeProject");
            SendDataToServer(request.ToJson(), r_id, true);
            IO.Swagger.Model.ResumeProjectResponse response = await WaitForResult<IO.Swagger.Model.ResumeProjectResponse>(r_id);
            if (!response.Result)
                throw new RequestFailedException(response.Messages);
        }
        
        public async Task UpdateActionPointUsingRobot(string actionPointId, string robotId, string endEffectorId) {
            
            int r_id = Interlocked.Increment(ref requestID);
            IO.Swagger.Model.RobotArg robotArg = new IO.Swagger.Model.RobotArg(robotId: robotId, endEffector: endEffectorId);
            IO.Swagger.Model.UpdateActionPointUsingRobotRequestArgs args = new IO.Swagger.Model.UpdateActionPointUsingRobotRequestArgs(actionPointId: actionPointId,
                robot: robotArg);
            IO.Swagger.Model.UpdateActionPointUsingRobotRequest request = new IO.Swagger.Model.UpdateActionPointUsingRobotRequest(id: r_id, request: "UpdateActionPointUsingRobot", args);
            SendDataToServer(request.ToJson(), r_id, true);
            IO.Swagger.Model.UpdateActionPointUsingRobotResponse response = await WaitForResult<IO.Swagger.Model.UpdateActionPointUsingRobotResponse>(r_id);
            if (!response.Result)
                throw new RequestFailedException(response.Messages);
        }

        public async Task UpdateActionObjectPose(string actionObjectId, IO.Swagger.Model.Pose pose) {

            int r_id = Interlocked.Increment(ref requestID);
            IO.Swagger.Model.UpdateObjectPoseRequestArgs args = new IO.Swagger.Model.UpdateObjectPoseRequestArgs
                (objectId: actionObjectId, pose: pose);
            IO.Swagger.Model.UpdateObjectPoseRequest request = new IO.Swagger.Model.UpdateObjectPoseRequest
                (id: r_id, request: "UpdateObjectPose", args);
            SendDataToServer(request.ToJson(), r_id, true);
            IO.Swagger.Model.UpdateObjectPoseResponse response = await WaitForResult<IO.Swagger.Model.UpdateObjectPoseResponse>(r_id);
            if (!response.Result)
                throw new RequestFailedException(response.Messages);

        }

        public async Task UpdateActionObjectPoseUsingRobot(string actionObjectId, string robotId, string endEffectorId) {
            
            int r_id = Interlocked.Increment(ref requestID);
            IO.Swagger.Model.RobotArg robotArg = new IO.Swagger.Model.RobotArg(robotId: robotId, endEffector: endEffectorId);
            IO.Swagger.Model.UpdateObjectPoseUsingRobotArgs args = new IO.Swagger.Model.UpdateObjectPoseUsingRobotArgs
                (id: actionObjectId, robot: robotArg);
            IO.Swagger.Model.UpdateObjectPoseUsingRobotRequest request = new IO.Swagger.Model.UpdateObjectPoseUsingRobotRequest
                (id: r_id, request: "UpdateObjectPoseUsingRobot", args);
            SendDataToServer(request.ToJson(), r_id, true);
            IO.Swagger.Model.UpdateObjectPoseUsingRobotResponse response = await WaitForResult<IO.Swagger.Model.UpdateObjectPoseUsingRobotResponse>(r_id);
            if (!response.Result)
                throw new RequestFailedException(response.Messages);
     
        }
        
        public async Task CreateNewObjectType(IO.Swagger.Model.ObjectTypeMeta objectType) {
            int r_id = Interlocked.Increment(ref requestID);
            IO.Swagger.Model.NewObjectTypeRequest request = new IO.Swagger.Model.NewObjectTypeRequest(id: r_id, request: "NewObjectType", args: objectType);
            SendDataToServer(request.ToJson(), r_id, true);
            IO.Swagger.Model.NewObjectTypeResponse response = await WaitForResult<IO.Swagger.Model.NewObjectTypeResponse>(r_id);
            if (!response.Result)
                throw new RequestFailedException(response.Messages);
        }

        public async Task StartObjectFocusing(string objectId, string robotId, string endEffector) {
            int r_id = Interlocked.Increment(ref requestID);
            IO.Swagger.Model.RobotArg robotArg = new IO.Swagger.Model.RobotArg(endEffector, robotId);
            IO.Swagger.Model.FocusObjectStartRequestArgs args = new IO.Swagger.Model.FocusObjectStartRequestArgs(objectId: objectId, robot: robotArg);
            IO.Swagger.Model.FocusObjectStartRequest request = new IO.Swagger.Model.FocusObjectStartRequest(id: r_id, request: "FocusObjectStart", args: args);
            SendDataToServer(request.ToJson(), r_id, true);
            IO.Swagger.Model.FocusObjectStartResponse response = await WaitForResult<IO.Swagger.Model.FocusObjectStartResponse>(r_id);
            if (!response.Result)
                throw new RequestFailedException(response.Messages);
        }

        public async Task SavePosition(string objectId, int pointIdx) {
            int r_id = Interlocked.Increment(ref requestID);
            IO.Swagger.Model.FocusObjectRequestArgs args = new IO.Swagger.Model.FocusObjectRequestArgs(objectId: objectId, pointIdx: pointIdx);
            IO.Swagger.Model.FocusObjectRequest request = new IO.Swagger.Model.FocusObjectRequest(id: r_id, request: "FocusObject", args: args);
            SendDataToServer(request.ToJson(), r_id, true);
            IO.Swagger.Model.FocusObjectResponse response = await WaitForResult<IO.Swagger.Model.FocusObjectResponse>(r_id);
            if (!response.Result)
                throw new RequestFailedException(response.Messages);
        }

        public async Task FocusObjectDone(string objectId) {
            int r_id = Interlocked.Increment(ref requestID);
            IO.Swagger.Model.IdArgs args = new IO.Swagger.Model.IdArgs(id: objectId);
            IO.Swagger.Model.FocusObjectDoneRequest request = new IO.Swagger.Model.FocusObjectDoneRequest(id: r_id, request: "FocusObjectDone", args: args);
            SendDataToServer(request.ToJson(), r_id, true);
            IO.Swagger.Model.FocusObjectDoneResponse response = await WaitForResult<IO.Swagger.Model.FocusObjectDoneResponse>(r_id);
            if (!response.Result)
                throw new RequestFailedException(response.Messages);
        }

        public async Task<List<IO.Swagger.Model.IdDesc>> LoadScenes() {
            int id = Interlocked.Increment(ref requestID);
            IO.Swagger.Model.ListScenesRequest request = new IO.Swagger.Model.ListScenesRequest(id: id, request: "ListScenes");
            SendDataToServer(request.ToJson(), id, true);
            IO.Swagger.Model.ListScenesResponse response = await WaitForResult<IO.Swagger.Model.ListScenesResponse>(id);
            return response.Data;
        }

        public async Task<List<IO.Swagger.Model.ListProjectsResponseData>> LoadProjects() {
            int r_id = Interlocked.Increment(ref requestID);
            IO.Swagger.Model.ListProjectsRequest request = new IO.Swagger.Model.ListProjectsRequest(id: r_id, request: "ListProjects");
            SendDataToServer(request.ToJson(), r_id, true);
            IO.Swagger.Model.ListProjectsResponse response = await WaitForResult<IO.Swagger.Model.ListProjectsResponse>(r_id);
            return response.Data;
        }

        public async Task<IO.Swagger.Model.AddObjectToSceneResponse> AddObjectToScene(string name, string type, IO.Swagger.Model.Pose pose) {
            int r_id = Interlocked.Increment(ref requestID);
            IO.Swagger.Model.AddObjectToSceneRequestArgs args = new IO.Swagger.Model.AddObjectToSceneRequestArgs(pose: pose, type: type, name: name);
            IO.Swagger.Model.AddObjectToSceneRequest request = new IO.Swagger.Model.AddObjectToSceneRequest(id: r_id, request: "AddObjectToScene", args: args);
            SendDataToServer(request.ToJson(), r_id, true);
            return await WaitForResult<IO.Swagger.Model.AddObjectToSceneResponse>(r_id);
        }

        public async Task AddServiceToScene(IO.Swagger.Model.SceneService sceneService) {
            int r_id = Interlocked.Increment(ref requestID);
            IO.Swagger.Model.AddServiceToSceneRequest request = new IO.Swagger.Model.AddServiceToSceneRequest(id: r_id, request: "AddServiceToScene", args: sceneService);
            SendDataToServer(request.ToJson(), r_id, true);
            var response = await WaitForResult<IO.Swagger.Model.AddServiceToSceneResponse>(r_id);
            if (!response.Result) {
                throw new RequestFailedException(response.Messages);
            }
        }
        public async Task<IO.Swagger.Model.AutoAddObjectToSceneResponse> AutoAddObjectToScene(string objectType) {
            int r_id = Interlocked.Increment(ref requestID);
            IO.Swagger.Model.TypeArgs args = new IO.Swagger.Model.TypeArgs(type: objectType);
            IO.Swagger.Model.AutoAddObjectToSceneRequest request = new IO.Swagger.Model.AutoAddObjectToSceneRequest(id: r_id, request: "AutoAddObjectToScene", args: args);
            SendDataToServer(request.ToJson(), r_id, true);
            return await WaitForResult<IO.Swagger.Model.AutoAddObjectToSceneResponse>(r_id);
            
        }

        public async Task<bool> AddServiceToScene(string configId, string serviceType) {
            int r_id = Interlocked.Increment(ref requestID);
            IO.Swagger.Model.SceneService sceneService = new IO.Swagger.Model.SceneService(configurationId: configId, type: serviceType);
            IO.Swagger.Model.AddServiceToSceneRequest request = new IO.Swagger.Model.AddServiceToSceneRequest(id: r_id, request: "AddServiceToScene", args: sceneService);
            SendDataToServer(request.ToJson(), r_id, true);
            IO.Swagger.Model.AddServiceToSceneResponse response = await WaitForResult<IO.Swagger.Model.AddServiceToSceneResponse>(r_id);
            return response.Result;
        }

        public async Task<IO.Swagger.Model.RemoveFromSceneResponse> RemoveFromScene(string id, bool force) {
            int r_id = Interlocked.Increment(ref requestID);
            IO.Swagger.Model.RemoveFromSceneArgs args = new IO.Swagger.Model.RemoveFromSceneArgs(id: id, force: force);
            IO.Swagger.Model.RemoveFromSceneRequest request = new IO.Swagger.Model.RemoveFromSceneRequest(id: r_id, request: "RemoveFromScene", args: args);
            SendDataToServer(request.ToJson(), r_id, true);
            return await WaitForResult<IO.Swagger.Model.RemoveFromSceneResponse>(r_id);            
        }

        public async Task<List<IO.Swagger.Model.ServiceTypeMeta>> GetServices() {
            int r_id = Interlocked.Increment(ref requestID);
            IO.Swagger.Model.GetServicesRequest request = new IO.Swagger.Model.GetServicesRequest(id: r_id, request: "GetServices");
            SendDataToServer(request.ToJson(), r_id, true);
            IO.Swagger.Model.GetServicesResponse response = await WaitForResult<IO.Swagger.Model.GetServicesResponse>(r_id);
            if (response.Result)
                return response.Data;
            else
                return new List<IO.Swagger.Model.ServiceTypeMeta>();
        }

        public async Task OpenScene(string scene_id) {
            int r_id = Interlocked.Increment(ref requestID);
            IO.Swagger.Model.IdArgs args = new IO.Swagger.Model.IdArgs(id: scene_id);
            IO.Swagger.Model.OpenSceneRequest request = new IO.Swagger.Model.OpenSceneRequest(id: r_id, request: "OpenScene", args: args);
            SendDataToServer(request.ToJson(), r_id, true);
            IO.Swagger.Model.OpenSceneResponse response = await WaitForResult<IO.Swagger.Model.OpenSceneResponse>(r_id);
            if (!response.Result) {
                throw new RequestFailedException(response.Messages);
            }
        }

        public async Task<List<string>> GetActionParamValues(string actionProviderId, string param_id, List<IO.Swagger.Model.IdValue> parent_params) {
            int r_id = Interlocked.Increment(ref requestID);
            IO.Swagger.Model.ActionParamValuesArgs args = new IO.Swagger.Model.ActionParamValuesArgs(id: actionProviderId, paramId: param_id, parentParams: parent_params);
            IO.Swagger.Model.ActionParamValuesRequest request = new IO.Swagger.Model.ActionParamValuesRequest(id: r_id, request: "ActionParamValues", args);
            SendDataToServer(request.ToJson(), r_id, true);
            IO.Swagger.Model.ActionParamValuesResponse response = await WaitForResult<IO.Swagger.Model.ActionParamValuesResponse>(r_id);
            if (response.Result)
                return response.Data;
            else
                return new List<string>();
        }

        public async Task ExecuteAction(string actionId) {
            Debug.Assert(actionId != null);
            Debug.Assert(actionId != "");

            int r_id = Interlocked.Increment(ref requestID);
            IO.Swagger.Model.ExecuteActionArgs args = new IO.Swagger.Model.ExecuteActionArgs(actionId: actionId);
            IO.Swagger.Model.ExecuteActionRequest request = new IO.Swagger.Model.ExecuteActionRequest(id: r_id, request: "ExecuteAction", args: args);
            SendDataToServer(request.ToJson(), r_id, true);
            IO.Swagger.Model.ExecuteActionResponse response = await WaitForResult<IO.Swagger.Model.ExecuteActionResponse>(r_id);
            if (!response.Result)
                throw new RequestFailedException(response.Messages);
        }

        public async Task<IO.Swagger.Model.SystemInfoData> GetSystemInfo() {
             int r_id = Interlocked.Increment(ref requestID);

             IO.Swagger.Model.SystemInfoRequest request = new IO.Swagger.Model.SystemInfoRequest(id: r_id, request: "SystemInfo");
             SendDataToServer(request.ToJson(), r_id, true);
             IO.Swagger.Model.SystemInfoResponse response = await WaitForResult<IO.Swagger.Model.SystemInfoResponse>(r_id);
             if (!response.Result)
                 throw new RequestFailedException(response.Messages);
             return response.Data;
         }

        public async Task BuildProject(string project_id) {
            int r_id = Interlocked.Increment(ref requestID);
            IO.Swagger.Model.IdArgs args = new IO.Swagger.Model.IdArgs(id: project_id);
            IO.Swagger.Model.BuildProjectRequest request = new IO.Swagger.Model.BuildProjectRequest(id: r_id, request: "BuildProject", args);
            SendDataToServer(request.ToJson(), r_id, true);
            IO.Swagger.Model.ExecuteActionResponse response = await WaitForResult<IO.Swagger.Model.ExecuteActionResponse>(r_id);
            
            if (!response.Result)
                throw new RequestFailedException(response.Messages);
        }

        public async Task CreateScene(string name, string description) {
            int r_id = Interlocked.Increment(ref requestID);
            IO.Swagger.Model.NewSceneRequestArgs args = new IO.Swagger.Model.NewSceneRequestArgs(name: name, desc: description);
            IO.Swagger.Model.NewSceneRequest request = new IO.Swagger.Model.NewSceneRequest(r_id, "NewScene", args);
            SendDataToServer(request.ToJson(), r_id, true);
            IO.Swagger.Model.NewSceneResponse response = await WaitForResult<IO.Swagger.Model.NewSceneResponse>(r_id);

            if (!response.Result)
                throw new RequestFailedException(response.Messages);
        }
        internal async Task RemoveScene(string id) {
            int r_id = Interlocked.Increment(ref requestID);
            IO.Swagger.Model.IdArgs args = new IO.Swagger.Model.IdArgs(id: id);
            IO.Swagger.Model.DeleteSceneRequest request = new IO.Swagger.Model.DeleteSceneRequest(r_id, "DeleteScene", args);
            SendDataToServer(request.ToJson(), r_id, true);
            IO.Swagger.Model.DeleteSceneResponse response = await WaitForResult<IO.Swagger.Model.DeleteSceneResponse>(r_id);

            if (!response.Result)
                throw new RequestFailedException(response.Messages);
        }

        public async Task RenameScene(string id, string newName) {
            int r_id = Interlocked.Increment(ref requestID);
            IO.Swagger.Model.RenameArgs args = new IO.Swagger.Model.RenameArgs(id: id, newName: newName);
            IO.Swagger.Model.RenameSceneRequest request = new IO.Swagger.Model.RenameSceneRequest(r_id, "RenameScene", args);
            SendDataToServer(request.ToJson(), r_id, true);
            IO.Swagger.Model.RenameSceneResponse response = await WaitForResult<IO.Swagger.Model.RenameSceneResponse>(r_id);

            if (!response.Result)
                throw new RequestFailedException(response.Messages);
        }

        public async Task RenameObject(string id, string newName) {
            int r_id = Interlocked.Increment(ref requestID);
            IO.Swagger.Model.RenameArgs args = new IO.Swagger.Model.RenameArgs(id: id, newName: newName);
            IO.Swagger.Model.RenameObjectRequest request = new IO.Swagger.Model.RenameObjectRequest(r_id, "RenameObject", args);
            SendDataToServer(request.ToJson(), r_id, true);
            IO.Swagger.Model.RenameObjectResponse response = await WaitForResult<IO.Swagger.Model.RenameObjectResponse>(r_id);

            if (!response.Result)
                throw new RequestFailedException(response.Messages);
        }

        public async Task UpdateObjectPose(string id, IO.Swagger.Model.Pose pose) {
            int r_id = Interlocked.Increment(ref requestID);
            IO.Swagger.Model.UpdateObjectPoseRequestArgs args = new IO.Swagger.Model.UpdateObjectPoseRequestArgs(objectId: id, pose: pose);
            IO.Swagger.Model.UpdateObjectPoseRequest request = new IO.Swagger.Model.UpdateObjectPoseRequest(r_id, "UpdateObjectPose", args);
            SendDataToServer(request.ToJson(), r_id, true);
            IO.Swagger.Model.UpdateObjectPoseResponse response = await WaitForResult<IO.Swagger.Model.UpdateObjectPoseResponse>(r_id);

            if (!response.Result)
                throw new RequestFailedException(response.Messages);
        }

        public async Task<bool> CloseScene(bool force) {
            int r_id = Interlocked.Increment(ref requestID);
            IO.Swagger.Model.CloseSceneRequestArgs args = new IO.Swagger.Model.CloseSceneRequestArgs(force);
            IO.Swagger.Model.CloseSceneRequest request = new IO.Swagger.Model.CloseSceneRequest(r_id, "CloseScene", args);
            SendDataToServer(request.ToJson(), r_id, true);
            IO.Swagger.Model.CloseSceneResponse response = await WaitForResult<IO.Swagger.Model.CloseSceneResponse>(r_id);
            // TODO: is this correct?
            return response.Result;
        }

        public async Task<List<string>> GetProjectsWithScene(string sceneId) {
            int r_id = Interlocked.Increment(ref requestID);
            IO.Swagger.Model.IdArgs args = new IO.Swagger.Model.IdArgs(sceneId);
            IO.Swagger.Model.ProjectsWithSceneRequest request = new IO.Swagger.Model.ProjectsWithSceneRequest(r_id, "ProjectsWithScene", args);
            SendDataToServer(request.ToJson(), r_id, true);
            IO.Swagger.Model.ProjectsWithSceneResponse response = await WaitForResult<IO.Swagger.Model.ProjectsWithSceneResponse>(r_id);
            if (!response.Result)
                throw new RequestFailedException(response.Messages);
            return response.Data;
        }

       public async Task CreateProject(string name, string sceneId, string description, bool hasLogic) {
            int r_id = Interlocked.Increment(ref requestID);
            IO.Swagger.Model.NewProjectRequestArgs args = new IO.Swagger.Model.NewProjectRequestArgs(name: name, sceneId: sceneId, desc: description, hasLogic: hasLogic);
            IO.Swagger.Model.NewProjectRequest request = new IO.Swagger.Model.NewProjectRequest(r_id, "NewProject", args);
            SendDataToServer(request.ToJson(), r_id, true);
            IO.Swagger.Model.NewProjectResponse response = await WaitForResult<IO.Swagger.Model.NewProjectResponse>(r_id);

            if (!response.Result)
                throw new RequestFailedException(response.Messages);
        }

        internal async Task RemoveProject(string id) {
            int r_id = Interlocked.Increment(ref requestID);
            IO.Swagger.Model.IdArgs args = new IO.Swagger.Model.IdArgs(id: id);
            IO.Swagger.Model.DeleteProjectRequest request = new IO.Swagger.Model.DeleteProjectRequest(r_id, "DeleteProject", args);
            SendDataToServer(request.ToJson(), r_id, true);
            IO.Swagger.Model.DeleteProjectResponse response = await WaitForResult<IO.Swagger.Model.DeleteProjectResponse>(r_id);

            if (!response.Result)
                throw new RequestFailedException(response.Messages);
        }


        public async Task AddActionPoint(string name, string parent, IO.Swagger.Model.Position position) {
            int r_id = Interlocked.Increment(ref requestID);
            IO.Swagger.Model.AddActionPointArgs args = new IO.Swagger.Model.AddActionPointArgs(parent: parent, position: position, name: name);
            IO.Swagger.Model.AddActionPointRequest request = new IO.Swagger.Model.AddActionPointRequest(r_id, "AddActionPoint", args);
            SendDataToServer(request.ToJson(), r_id, true);
            IO.Swagger.Model.AddActionPointResponse response = await WaitForResult<IO.Swagger.Model.AddActionPointResponse>(r_id);

            if (!response.Result)   
                throw new RequestFailedException(response.Messages);
        }



        public async Task UpdateActionPointPosition(string id, IO.Swagger.Model.Position position) {
            int r_id = Interlocked.Increment(ref requestID);
            IO.Swagger.Model.UpdateActionPointPositionRequestArgs args = new IO.Swagger.Model.UpdateActionPointPositionRequestArgs(actionPointId: id, newPosition: position);
            IO.Swagger.Model.UpdateActionPointPositionRequest request = new IO.Swagger.Model.UpdateActionPointPositionRequest(r_id, "UpdateActionPointPosition", args);
            SendDataToServer(request.ToJson(), r_id, true);
            IO.Swagger.Model.UpdateActionPointPositionResponse response = await WaitForResult<IO.Swagger.Model.UpdateActionPointPositionResponse>(r_id);

            if (!response.Result)
                throw new RequestFailedException(response.Messages);
        }

        public async Task UpdateActionPointParent(string id, string parentId) {
            int r_id = Interlocked.Increment(ref requestID);
            IO.Swagger.Model.UpdateActionPointParentRequestArgs args = new IO.Swagger.Model.UpdateActionPointParentRequestArgs(actionPointId: id, newParentId: parentId);
            IO.Swagger.Model.UpdateActionPointParentRequest request = new IO.Swagger.Model.UpdateActionPointParentRequest(r_id, "UpdateActionPointParent", args);
            SendDataToServer(request.ToJson(), r_id, true);
            IO.Swagger.Model.UpdateActionPointParentResponse response = await WaitForResult<IO.Swagger.Model.UpdateActionPointParentResponse>(r_id);

            if (!response.Result)
                throw new RequestFailedException(response.Messages);
        }

        public async Task RenameActionPoint(string id, string name) {
            int r_id = Interlocked.Increment(ref requestID);
            IO.Swagger.Model.RenameActionPointRequestArgs args = new IO.Swagger.Model.RenameActionPointRequestArgs(actionPointId: id, newName: name);
            IO.Swagger.Model.RenameActionPointRequest request = new IO.Swagger.Model.RenameActionPointRequest(r_id, "RenameActionPoint", args);
            SendDataToServer(request.ToJson(), r_id, true);
            IO.Swagger.Model.RenameActionPointResponse response = await WaitForResult<IO.Swagger.Model.RenameActionPointResponse>(r_id);

            if (!response.Result)
                throw new RequestFailedException(response.Messages);
        }

        public async Task AddActionPointOrientation(string id, IO.Swagger.Model.Orientation orientation, string name) {
            int r_id = Interlocked.Increment(ref requestID);
            IO.Swagger.Model.AddActionPointOrientationRequestArgs args = new IO.Swagger.Model.AddActionPointOrientationRequestArgs(actionPointId: id, orientation: orientation, name: name);
            IO.Swagger.Model.AddActionPointOrientationRequest request = new IO.Swagger.Model.AddActionPointOrientationRequest(r_id, "AddActionPointOrientation", args);
            SendDataToServer(request.ToJson(), r_id, true);
            IO.Swagger.Model.AddActionPointOrientationResponse response = await WaitForResult<IO.Swagger.Model.AddActionPointOrientationResponse>(r_id);

            if (!response.Result)
                throw new RequestFailedException(response.Messages);
        }


        public async Task UpdateActionPointOrientation(IO.Swagger.Model.Orientation orientation, string orientationId) {
            int r_id = Interlocked.Increment(ref requestID);
            IO.Swagger.Model.UpdateActionPointOrientationRequestArgs args = new IO.Swagger.Model.UpdateActionPointOrientationRequestArgs(orientation: orientation, orientationId: orientationId);
            IO.Swagger.Model.UpdateActionPointOrientationRequest request = new IO.Swagger.Model.UpdateActionPointOrientationRequest(r_id, "UpdateActionPointOrientation", args);
            SendDataToServer(request.ToJson(), r_id, true);
            IO.Swagger.Model.UpdateActionPointOrientationResponse response = await WaitForResult<IO.Swagger.Model.UpdateActionPointOrientationResponse>(r_id);

            if (!response.Result)
                throw new RequestFailedException(response.Messages);
        }


         public async Task AddActionPointOrientationUsingRobot(string id, string robotId, string endEffector, string name) {
            int r_id = Interlocked.Increment(ref requestID);
            IO.Swagger.Model.RobotArg robotArg = new IO.Swagger.Model.RobotArg(robotId, endEffector);
            IO.Swagger.Model.AddActionPointOrientationUsingRobotRequestArgs args = new IO.Swagger.Model.AddActionPointOrientationUsingRobotRequestArgs(actionPointId: id, robot: robotArg, name: name);
            IO.Swagger.Model.AddActionPointOrientationUsingRobotRequest request = new IO.Swagger.Model.AddActionPointOrientationUsingRobotRequest(r_id, "AddActionPointOrientationUsingRobot", args);
            SendDataToServer(request.ToJson(), r_id, true);
            IO.Swagger.Model.AddActionPointOrientationUsingRobotResponse response = await WaitForResult<IO.Swagger.Model.AddActionPointOrientationUsingRobotResponse>(r_id);

            if (!response.Result)
                throw new RequestFailedException(response.Messages);
        }

        
        public async Task UpdateActionPointOrientationUsingRobot(string id, string robotId, string endEffector, string orientationId) {
            int r_id = Interlocked.Increment(ref requestID);
            IO.Swagger.Model.RobotArg robotArg = new IO.Swagger.Model.RobotArg(robotId: robotId, endEffector: endEffector);
            IO.Swagger.Model.UpdateActionPointOrientationUsingRobotRequestArgs args = new IO.Swagger.Model.UpdateActionPointOrientationUsingRobotRequestArgs(actionPointId: id, robot: robotArg, orientationId: orientationId);
            IO.Swagger.Model.UpdateActionPointOrientationUsingRobotRequest request = new IO.Swagger.Model.UpdateActionPointOrientationUsingRobotRequest(r_id, "UpdateActionPointOrientationUsingRobot", args);
            SendDataToServer(request.ToJson(), r_id, true);
            IO.Swagger.Model.UpdateActionPointOrientationUsingRobotResponse response = await WaitForResult<IO.Swagger.Model.UpdateActionPointOrientationUsingRobotResponse>(r_id);

            if (!response.Result)
                throw new RequestFailedException(response.Messages);
        }

        
        public async Task AddActionPointJoints(string id, string robotId, string name) {
            int r_id = Interlocked.Increment(ref requestID);
            IO.Swagger.Model.AddActionPointJointsRequestArgs args = new IO.Swagger.Model.AddActionPointJointsRequestArgs(actionPointId: id, robotId: robotId, name: name);
            IO.Swagger.Model.AddActionPointJointsRequest request = new IO.Swagger.Model.AddActionPointJointsRequest(r_id, "AddActionPointJoints", args);
            SendDataToServer(request.ToJson(), r_id, true);
            IO.Swagger.Model.AddActionPointJointsResponse response = await WaitForResult<IO.Swagger.Model.AddActionPointJointsResponse>(r_id);

            if (!response.Result)
                throw new RequestFailedException(response.Messages);
        }

        public async Task UpdateActionPointJoints(string robotId, string name) {
            int r_id = Interlocked.Increment(ref requestID);
            IO.Swagger.Model.UpdateActionPointJointsRequestArgs args = new IO.Swagger.Model.UpdateActionPointJointsRequestArgs(robotId: robotId, jointsId: name);
            IO.Swagger.Model.UpdateActionPointJointsRequest request = new IO.Swagger.Model.UpdateActionPointJointsRequest(r_id, "UpdateActionPointJoints", args);
            SendDataToServer(request.ToJson(), r_id, true);
            IO.Swagger.Model.UpdateActionPointJointsResponse response = await WaitForResult<IO.Swagger.Model.UpdateActionPointJointsResponse>(r_id);

            if (!response.Result)
                throw new RequestFailedException(response.Messages);
        }

        public async Task RemoveActionPointOrientation(string id, string orientationId) {
            int r_id = Interlocked.Increment(ref requestID);
            IO.Swagger.Model.RemoveActionPointOrientationRequestArgs args = new IO.Swagger.Model.RemoveActionPointOrientationRequestArgs(actionPointId: id, orientationId: orientationId);
            IO.Swagger.Model.RemoveActionPointOrientationRequest request = new IO.Swagger.Model.RemoveActionPointOrientationRequest(r_id, "RemoveActionPointOrientation", args);
            SendDataToServer(request.ToJson(), r_id, true);
            IO.Swagger.Model.RemoveActionPointOrientationResponse response = await WaitForResult<IO.Swagger.Model.RemoveActionPointOrientationResponse>(r_id);

            if (!response.Result)
                throw new RequestFailedException(response.Messages);
        }

         public async Task RemoveActionPointJoints(string jointsId) {
            int r_id = Interlocked.Increment(ref requestID);
            IO.Swagger.Model.RemoveActionPointJointsRequestArgs args = new IO.Swagger.Model.RemoveActionPointJointsRequestArgs(jointsId: jointsId);
            IO.Swagger.Model.RemoveActionPointJointsRequest request = new IO.Swagger.Model.RemoveActionPointJointsRequest(r_id, "RemoveActionPointJoints", args);
            SendDataToServer(request.ToJson(), r_id, true);
            IO.Swagger.Model.RemoveActionPointJointsResponse response = await WaitForResult<IO.Swagger.Model.RemoveActionPointJointsResponse>(r_id);

            if (!response.Result)
                throw new RequestFailedException(response.Messages);
        }

        public async Task AddAction(string actionPointId, List<IO.Swagger.Model.ActionParameter> actionParameters, string type, string name) {
            int r_id = Interlocked.Increment(ref requestID);
            IO.Swagger.Model.AddActionRequestArgs args = new IO.Swagger.Model.AddActionRequestArgs(actionPointId: actionPointId, parameters: actionParameters, type: type, name: name);
            IO.Swagger.Model.AddActionRequest request = new IO.Swagger.Model.AddActionRequest(r_id, "AddAction", args);
            SendDataToServer(request.ToJson(), r_id, true);
            IO.Swagger.Model.AddActionResponse response = await WaitForResult<IO.Swagger.Model.AddActionResponse>(r_id);

            if (!response.Result)
                throw new RequestFailedException(response.Messages);
        }

        public async Task UpdateAction(string actionId, List<IO.Swagger.Model.ActionParameter> actionParameters) {
            int r_id = Interlocked.Increment(ref requestID);
            IO.Swagger.Model.UpdateActionRequestArgs args = new IO.Swagger.Model.UpdateActionRequestArgs(actionId: actionId, parameters: actionParameters);
            IO.Swagger.Model.UpdateActionRequest request = new IO.Swagger.Model.UpdateActionRequest(r_id, "UpdateAction", args);
            SendDataToServer(request.ToJson(), r_id, true);
            IO.Swagger.Model.UpdateActionResponse response = await WaitForResult<IO.Swagger.Model.UpdateActionResponse>(r_id);

            if (!response.Result)
                throw new RequestFailedException(response.Messages);
        }

        public async Task RemoveAction(string actionId) {
            int r_id = Interlocked.Increment(ref requestID);
            IO.Swagger.Model.IdArgs args = new IO.Swagger.Model.IdArgs(id: actionId);
            IO.Swagger.Model.RemoveActionRequest request = new IO.Swagger.Model.RemoveActionRequest(r_id, "RemoveAction", args);
            SendDataToServer(request.ToJson(), r_id, true);
            IO.Swagger.Model.RemoveActionResponse response = await WaitForResult<IO.Swagger.Model.RemoveActionResponse>(r_id);

            if (!response.Result)
                throw new RequestFailedException(response.Messages);
        }

        public async Task RenameAction(string actionId, string newName) {
            int r_id = Interlocked.Increment(ref requestID);
            IO.Swagger.Model.RenameActionRequestArgs args = new IO.Swagger.Model.RenameActionRequestArgs(actionId: actionId, newName: newName);
            IO.Swagger.Model.RenameActionRequest request = new IO.Swagger.Model.RenameActionRequest(r_id, "RenameAction", args);
            SendDataToServer(request.ToJson(), r_id, true);
            IO.Swagger.Model.RenameActionResponse response = await WaitForResult<IO.Swagger.Model.RenameActionResponse>(r_id);

            if (!response.Result)
                throw new RequestFailedException(response.Messages);
        }


        public async Task RenameAction(string actionId, List<IO.Swagger.Model.ActionParameter> parameters) {
            int r_id = Interlocked.Increment(ref requestID);
            IO.Swagger.Model.UpdateActionRequestArgs args = new IO.Swagger.Model.UpdateActionRequestArgs(actionId: actionId, parameters: parameters);
            IO.Swagger.Model.UpdateActionRequest request = new IO.Swagger.Model.UpdateActionRequest(r_id, "UpdateAction", args);
            SendDataToServer(request.ToJson(), r_id, true);
            IO.Swagger.Model.UpdateActionResponse response = await WaitForResult<IO.Swagger.Model.UpdateActionResponse>(r_id);

            if (!response.Result)
                throw new RequestFailedException(response.Messages);
        }




        public async Task UpdateActionLogic(string actionId, List<IO.Swagger.Model.ActionIO> inputs, List<IO.Swagger.Model.ActionIO> outputs) {
            int r_id = Interlocked.Increment(ref requestID);
            IO.Swagger.Model.UpdateActionLogicArgs args = new IO.Swagger.Model.UpdateActionLogicArgs(actionId: actionId, inputs: inputs, outputs: outputs);
            IO.Swagger.Model.UpdateActionLogicRequest request = new IO.Swagger.Model.UpdateActionLogicRequest(r_id, "UpdateActionLogic", args);
            SendDataToServer(request.ToJson(), r_id, true);
            IO.Swagger.Model.UpdateActionLogicResponse response = await WaitForResult<IO.Swagger.Model.UpdateActionLogicResponse>(r_id);

            if (!response.Result)
                throw new RequestFailedException(response.Messages);
        }
                
        public async Task RenameProject(string projectId, string newName) {
            int r_id = Interlocked.Increment(ref requestID);
            IO.Swagger.Model.RenameProjectRequestArgs args = new IO.Swagger.Model.RenameProjectRequestArgs(projectId: projectId, newName: newName);
            IO.Swagger.Model.RenameProjectRequest request = new IO.Swagger.Model.RenameProjectRequest(r_id, "RenameProject", args);
            SendDataToServer(request.ToJson(), r_id, true);
            IO.Swagger.Model.RenameProjectResponse response = await WaitForResult<IO.Swagger.Model.RenameProjectResponse>(r_id);

            if (!response.Result)
                throw new RequestFailedException(response.Messages);

        }

        public async Task RemoveActionPoint(string actionPointId) {
            int r_id = Interlocked.Increment(ref requestID);
            IO.Swagger.Model.IdArgs args = new IO.Swagger.Model.IdArgs(actionPointId);
            IO.Swagger.Model.RemoveActionPointRequest request = new IO.Swagger.Model.RemoveActionPointRequest(r_id, "RemoveActionPoint", args);
            SendDataToServer(request.ToJson(), r_id, true);
            IO.Swagger.Model.RemoveActionPointResponse response = await WaitForResult<IO.Swagger.Model.RemoveActionPointResponse>(r_id);

            if (!response.Result)
                throw new RequestFailedException(response.Messages);

        }

        public async Task<bool> CloseProject(bool force) {
            int r_id = Interlocked.Increment(ref requestID);
            IO.Swagger.Model.CloseProjectRequestArgs args = new IO.Swagger.Model.CloseProjectRequestArgs(force);
            IO.Swagger.Model.CloseProjectRequest request = new IO.Swagger.Model.CloseProjectRequest(r_id, "CloseProject", args);
            SendDataToServer(request.ToJson(), r_id, true);
            IO.Swagger.Model.CloseProjectResponse response = await WaitForResult<IO.Swagger.Model.CloseProjectResponse>(r_id);

            // TODO: is this correct?
            return response.Result;
        }


    }
}
