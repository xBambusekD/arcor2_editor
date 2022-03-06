using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Base;
using Hololens;

using System.Threading.Tasks;
using System;

using IO.Swagger.Model;
using System.Linq;
public class HProjectManager : Base.Singleton<HProjectManager>
{

        /// <summary>
        /// Opened project metadata
        /// </summary>
        public IO.Swagger.Model.Project ProjectMeta = null;
        /// <summary>
        /// All action points in scene
        /// </summary>
        public Dictionary<string, HActionPoint> ActionPoints = new Dictionary<string, HActionPoint>();
        /// <summary>
        /// All logic items (i.e. connections of actions) in project
        /// </summary>
        public Dictionary<string, HLogicItem> LogicItems = new Dictionary<string, HLogicItem>();
        /// <summary>
        /// All Project parameters <id, instance>
        /// </summary>
        public List<IO.Swagger.Model.ProjectParameter> ProjectParameters = new List<ProjectParameter>();
        /// <summary>
        /// Spawn point for global action points
        /// </summary>
        public GameObject ActionPointsOrigin;
        /// <summary>
        /// Prefab for project elements
        /// </summary>
        public GameObject ConnectionPrefab, ActionPointPrefab, PuckPrefab, StartPrefab, EndPrefab;
        /// <summary>
        /// Action representing start of program
        /// </summary>
        public HStartAction StartAction;
        /// <summary>
        /// Action representing end of program
        /// </summary>
        public HEndAction EndAction;
        /// <summary>
        /// ??? Dan?
        /// </summary>
        private bool projectActive = true;
        /// <summary>
        /// Indicates if action point orientations should be visible for given project
        /// </summary>
        public bool APOrientationsVisible;
        /// <summary>
        /// Holds current diameter of action points
        /// </summary>
        public float APSize = 0.2f;
        /// <summary>
        /// Indicates if project is loaded
        /// </summary>
        public bool Valid = false;
        /// <summary>
        /// Indicates if editation of project is allowed.
        /// </summary>
        public bool AllowEdit = false;
        /// <summary>
        /// Indicates if project was changed since last save
        /// </summary>
        private bool projectChanged;
        /// <summary>
        /// Flag which indicates whether project changed event should be trigered during Update()
        /// </summary>
        private bool updateProject = false;
        /// <summary>
        /// Public setter for project changed property. Invokes OnProjectChanged event with each change and
        /// OnProjectSavedSatusChanged when projectChanged value differs from original value (i.e. when project
        /// was not changed and now it is and vice versa) 
        /// </summary>
        public bool ProjectChanged {
            get => projectChanged;
            set {
                bool origVal = projectChanged;
                projectChanged = value;
                if (!Valid)
                    return;
                OnProjectChanged?.Invoke(this, EventArgs.Empty);
                if (origVal != value) {
                    OnProjectSavedSatusChanged?.Invoke(this, EventArgs.Empty);
                }
            } 
        }


        /// <summary>
        /// Invoked when some of the action point weas updated. Contains action point description
        /// </summary>
        //public event AREditorEventArgs.ActionPointUpdatedEventHandler OnActionPointUpdated; 
        /// <summary>
        /// Invoked when project loaded
        /// </summary>
        public event EventHandler OnLoadProject;
        /// <summary>
        /// Invoked when project changed
        /// </summary>
        public event EventHandler OnProjectChanged;
        /// <summary>
        /// Invoked when project saved
        /// </summary>
        public event EventHandler OnProjectSaved;
        /// <summary>
        /// Invoked when projectChanged value differs from original value (i.e. when project
        /// was not changed and now it is and vice versa) 
        /// </summary>
        public event EventHandler OnProjectSavedSatusChanged;
        /// <summary>
        /// Indicates whether there is any object with available action in the scene
        /// </summary>
        public bool AnyAvailableAction;

        public event AREditorEventArgs.ActionPointEventHandler OnActionPointAddedToScene;
        public event AREditorEventArgs.ActionEventHandler OnActionAddedToScene;

        public event AREditorEventArgs.ActionPointOrientationEventHandler OnActionPointOrientationAdded;
        public event AREditorEventArgs.ActionPointOrientationEventHandler OnActionPointOrientationUpdated;
        public event AREditorEventArgs.ActionPointOrientationEventHandler OnActionPointOrientationBaseUpdated;
        public event AREditorEventArgs.StringEventHandler OnActionPointOrientationRemoved;
    // Start is called before the first frame update
    void Start()
    {
           /* WebSocketManagerH.Instance.OnLogicItemAdded += OnLogicItemAdded;
            WebSocketManagerH.Instance.OnLogicItemRemoved += OnLogicItemRemoved;
            WebSocketManagerH.Instance.OnLogicItemUpdated += OnLogicItemUpdated;
            WebSocketManagerH.Instance.OnProjectBaseUpdated += OnProjectBaseUpdated;

            WebSocketManagerH.Instance.OnActionPointAdded += OnActionPointAdded;
            WebSocketManagerH.Instance.OnActionPointRemoved += OnActionPointRemoved;
            WebSocketManagerH.Instance.OnActionPointUpdated += OnActionPointUpdated;
            WebSocketManagerH.Instance.OnActionPointBaseUpdated += OnActionPointBaseUpdated;

            WebSocketManagerH.Instance.OnActionPointOrientationAdded += OnActionPointOrientationAddedCallback;
            WebSocketManagerH.Instance.OnActionPointOrientationUpdated += OnActionPointOrientationUpdatedCallback;
            WebSocketManagerH.Instance.OnActionPointOrientationBaseUpdated += OnActionPointOrientationBaseUpdatedCallback;
            WebSocketManagerH.Instance.OnActionPointOrientationRemoved += OnActionPointOrientationRemovedCallback;

            WebSocketManagerH.Instance.OnActionPointJointsAdded += OnActionPointJointsAdded;
            WebSocketManagerH.Instance.OnActionPointJointsUpdated += OnActionPointJointsUpdated;
            WebSocketManagerH.Instance.OnActionPointJointsBaseUpdated += OnActionPointJointsBaseUpdated;
            WebSocketManagerH.Instance.OnActionPointJointsRemoved += OnActionPointJointsRemoved;

            WebSocketManagerH.Instance.OnProjectParameterAdded += OnProjectParameterAdded;
            WebSocketManagerH.Instance.OnProjectParameterUpdated += OnProjectParameterUpdated;
            WebSocketManagerH.Instance.OnProjectParameterRemoved += OnProjectParameterRemoved;*/
  
    }

    // Update is called once per frame
    void Update()
    {
        if (updateProject) {
            ProjectChanged = true;
            updateProject = false;
        }
    }

    public async Task<bool> CreateProject(IO.Swagger.Model.Project project, bool allowEdit) {
        Debug.Assert(ActionsManagerH.Instance.ActionsReady);
        if (ProjectMeta != null)
            return false;

        SetProjectMeta(DataHelper.ProjectToBareProject(project));
        AllowEdit = allowEdit;
        LoadSettings();
        AnyAvailableAction = false;
        foreach (ActionObjectH obj in SceneManagerH.Instance.ActionObjects.Values)
            if (obj.ActionObjectMetadata.ActionsMetadata.Count > 0) {
                AnyAvailableAction = true;
                break;
            }
    Debug.Log(StartPrefab);
        StartAction = Instantiate(StartPrefab,  SceneManagerH.Instance.SceneOrigin.transform).GetComponent<HStartAction>();
        StartAction.Init(null, null, null, null, "START");
        EndAction = Instantiate(EndPrefab, SceneManagerH.Instance.SceneOrigin.transform).GetComponent<HEndAction>();
        EndAction.Init(null, null, null, null, "END");

        foreach (SceneObjectOverride objectOverrides in project.ObjectOverrides) {
            ActionObjectH actionObject = SceneManagerH.Instance.GetActionObject(objectOverrides.Id);
            foreach (IO.Swagger.Model.Parameter p in objectOverrides.Parameters) {
                if (actionObject.TryGetParameterMetadata(p.Name, out ParameterMeta meta)) {
                    Base.Parameter parameter = new Base.Parameter(meta, p.Value);
                    actionObject.Overrides[p.Name] = parameter;
                }
                
            }
        }

        UpdateActionPoints(project);
        UpdateProjectParameters(project.Parameters);
        if (project.HasLogic) {
            UpdateLogicItems(project.Logic);
        }
        // update orientation of all actions
        /*foreach (Action action in GetAllActions()) {
            action.UpdateRotation();
        }*/
        if (project.Modified == System.DateTime.MinValue) { //new project, never saved
            projectChanged = true;
        } else if (project.IntModified == System.DateTime.MinValue) {
            ProjectChanged = false;
        } else {
            ProjectChanged = project.IntModified > project.Modified;
        }
        Valid = true;
        OnLoadProject?.Invoke(this, EventArgs.Empty);
//        SetActionInputOutputVisibility(MainSettingsMenu.Instance.ConnectionsSwitch.IsOn());
        return true;
    }    

    public void SetActionInputOutputVisibility(bool visible) {
        if (!Valid || !ProjectMeta.HasLogic)
            return;
        /*foreach (Action action in GetAllActions()) {
            action.EnableInputOutput(visible);
        }
        StartAction.EnableInputOutput(visible);
        EndAction.EnableInputOutput(visible);*/
        //SelectorMenu.Instance.ShowIO(visible);
  /*      if (SelectorMenu.Instance.IOToggle.Toggled != visible)
            SelectorMenu.Instance.IOToggle.SwitchToggle();
        SelectorMenu.Instance.IOToggle.SetInteractivity(visible, "Connections are hidden");*/
    }

    /// <summary>
    /// Updates logic items
    /// </summary>
    /// <param name="logic">List of logic items</param>
    private void UpdateLogicItems(List<IO.Swagger.Model.LogicItem> logic) {
        foreach (IO.Swagger.Model.LogicItem projectLogicItem in logic) {
            if (!LogicItems.TryGetValue(projectLogicItem.Id, out HLogicItem logicItem)) {
                logicItem = new HLogicItem(projectLogicItem);
                LogicItems.Add(logicItem.Data.Id, logicItem);
            } else {
                logicItem.UpdateConnection(projectLogicItem);
            }
        }
    }
    private void UpdateProjectParameters(List<ProjectParameter> projectParameters) {
        ProjectParameters.Clear();
        if (projectParameters == null)
            return;
        ProjectParameters.AddRange(projectParameters);
    }  

    /// <summary>
    /// Loads project settings from persistant storage
    /// </summary>
    public void LoadSettings() {
        APOrientationsVisible = PlayerPrefsHelper.LoadBool("project/" + ProjectMeta.Id + "/APOrientationsVisibility", true);
        APSize = PlayerPrefsHelper.LoadFloat("project/" + ProjectMeta.Id + "/APSize", 0.2f);
    }


    /// <summary>
    /// Updates action point GameObject in ActionObjects.ActionPoints dict based on the data present in IO.Swagger.Model.ActionPoint Data.
    /// </summary>
    /// <param name="project"></param>
    public void UpdateActionPoints(Project project) {
        List<string> currentAP = new List<string>();
        List<string> currentActions = new List<string>();
        // key = parentId, value = list of APs with given parent
        Dictionary<string, List<IO.Swagger.Model.ActionPoint>> actionPointsWithParents = new Dictionary<string, List<IO.Swagger.Model.ActionPoint>>();
        // ordered list of already processed parents. This ensure that global APs are processed first,
        // then APs with action objects as a parents and then APs with already processed AP parents
        List<string> processedParents = new List<string> {
            "global"
        };
        foreach (IO.Swagger.Model.ActionPoint projectActionPoint in project.ActionPoints) {
            string parent = projectActionPoint.Parent;
            if (string.IsNullOrEmpty(parent)) {
                parent = "global";
            }
            if (actionPointsWithParents.TryGetValue(parent, out List<IO.Swagger.Model.ActionPoint> projectActionPoints)) {
                projectActionPoints.Add(projectActionPoint);
            } else {
                List<IO.Swagger.Model.ActionPoint> aps = new List<IO.Swagger.Model.ActionPoint> {
                    projectActionPoint
                };
                actionPointsWithParents[parent] = aps;
            }
            // if parent is action object, we dont need to process it
            if (SceneManagerH.Instance.ActionObjects.ContainsKey(parent)) {
                processedParents.Add(parent);
            }
        }

        for (int i = 0; i < processedParents.Count; ++i) {
            if (actionPointsWithParents.TryGetValue(processedParents[i], out List<IO.Swagger.Model.ActionPoint> projectActionPoints)) {
                foreach (IO.Swagger.Model.ActionPoint projectActionPoint in projectActionPoints) {
                    // if action point exist, just update it
                    if (ActionPoints.TryGetValue(projectActionPoint.Id, out HActionPoint actionPoint)) {
                        actionPoint.ActionPointBaseUpdate(DataHelper.ActionPointToBareActionPoint(projectActionPoint));
                    }
                    // if action point doesn't exist, create new one
                    else {
                        IActionPointParentH parent = null;
                        if (!string.IsNullOrEmpty(projectActionPoint.Parent)) {
                            parent = GetActionPointParent(projectActionPoint.Parent);
                        }


                        actionPoint = SpawnActionPoint(projectActionPoint, parent);

                    }

                    // update actions in current action point 
                    (List<string>, Dictionary<string, string>) updateActionsResult = actionPoint.UpdateActionPoint(projectActionPoint);
                    currentActions.AddRange(updateActionsResult.Item1);
                    // merge dictionaries
                    //connections = connections.Concat(updateActionsResult.Item2).GroupBy(i => i.Key).ToDictionary(i => i.Key, i => i.First().Value);

                    actionPoint.UpdatePositionsOfPucks();

                    currentAP.Add(actionPoint.Data.Id);

                    processedParents.Add(projectActionPoint.Id);
                }
            }
            
        }

        
        

        //UpdateActionConnections(project.ActionPoints, connections);

        // Remove deleted actions
        foreach (string actionId in GetAllActionsDict().Keys.ToList<string>()) {
            if (!currentActions.Contains(actionId)) {
                RemoveAction(actionId);
            }
        }

        // Remove deleted action points
        foreach (string actionPointId in GetAllActionPointsDict().Keys.ToList<string>()) {
            if (!currentAP.Contains(actionPointId)) {
                RemoveActionPoint(actionPointId);
            }
        }
    }

    
    /// <summary>
    /// Destroys current project
    /// </summary>
    /// <returns>True if project successfully destroyed</returns>
    public bool DestroyProject() {
        Valid = false;
        ProjectMeta = null;
        foreach (HActionPoint ap in ActionPoints.Values) {
            ap.DeleteAP(false);
        }
        if (StartAction != null) {
            Destroy(StartAction.gameObject);
            StartAction = null;
        }               
        if (EndAction != null) {
            Destroy(EndAction.gameObject);
            EndAction = null;
        }
        ActionPoints.Clear();
        HConnectionManagerArcoro.Instance.Clear();
        LogicItems.Clear();
        ProjectParameters.Clear();
        return true;
    }


    /// <summary>
    /// Destroys and removes references to action point of given Id.
    /// </summary>
    /// <param name="Id"></param>
    public void RemoveActionPoint(string Id) {
        if (ActionPoints.TryGetValue(Id, out HActionPoint actionPoint)) {

            // If deleted AP is selected in SelectorMenu (which most of the times should be),
            // deselect it, in order to update buttons, references, etc.
         /*   if (actionPoint == SelectorMenu.Instance.GetSelectedObject()) {
                SelectorMenu.Instance.DeselectObject();
            }*/

            // Call function in corresponding action point that will delete it and properly remove all references and connections.
            // We don't want to update project, because we are calling this method only upon received update from server.
            actionPoint.DeleteAP();
        }
    }
    
    /// <summary>
    /// Removes all action points in project
    /// </summary>
    public void RemoveActionPoints() {
        List<HActionPoint> actionPoints = ActionPoints.Values.ToList();
        foreach (HActionPoint actionPoint in actionPoints) {
            actionPoint.DeleteAP();
        }
    }

    /// <summary>
    /// Returns all action points in the scene in a dictionary [action_point_Id, ActionPoint_object]
    /// </summary>
    /// <returns></returns>
    public Dictionary<string, HActionPoint> GetAllActionPointsDict() {
        return ActionPoints;
    }

    /// <summary>
    /// Destroys and removes references to action of given Id.
    /// </summary>
    /// <param name="Id"></param>
    public void RemoveAction(string Id) {
        HAction aToRemove = GetAction(Id);
        string apIdToRemove = aToRemove.ActionPoint.Data.Id;
        // Call function in corresponding action that will delete it and properly remove all references and connections.
        // We don't want to update project, because we are calling this method only upon received update from server.
        ActionPoints[apIdToRemove].Actions[Id].DeleteAction();
    }


    /// <summary>
    /// Returns action of given Id.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public HAction GetAction(string id) {
        if (id == "START")
            return StartAction;
        else if (id == "END")
            return EndAction;
        foreach (HActionPoint actionPoint in ActionPoints.Values) {
            if (actionPoint.Actions.TryGetValue(id, out HAction action)) {
                return action;
            }
        }

        //Debug.LogError("Action " + Id + " not found!");
        throw new ItemNotFoundException("Action with ID " + id + " not found");
    }

    public bool ActionsContainsName(string name) {
        foreach (HAction action in GetAllActions()) {
            if (action.Data.Name == name)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Returns all actions in the scene in a list [Action_object]
    /// </summary>
    /// <returns></returns>
    public List<HAction> GetAllActions() {
        List<HAction> actions = new List<HAction>();
        foreach (HActionPoint actionPoint in ActionPoints.Values) {
            foreach (HAction action in actionPoint.Actions.Values) {
                actions.Add(action);
            }
        }

        return actions;
    }

    public HAction SpawnAction(IO.Swagger.Model.Action projectAction, HActionPoint ap) {
        //string action_id, string action_name, string action_type, 
        Debug.Assert(!ActionsContainsName(projectAction.Name));
        ActionMetadataH actionMetadata;
        string providerName = projectAction.Type.Split('/').First();
        string actionType = projectAction.Type.Split('/').Last();
        IActionProviderH actionProvider;
        try {
            actionProvider = SceneManagerH.Instance.GetActionObject(providerName);
        } catch (KeyNotFoundException ex) {
            throw new RequestFailedException("PROVIDER NOT FOUND EXCEPTION: " + providerName + " " + actionType);                
        }

        try {
            actionMetadata = actionProvider.GetActionMetadata(actionType);
        } catch (ItemNotFoundException ex) {
            Debug.LogError(ex);
            return null; //TODO: throw exception
        }

        if (actionMetadata == null) {
            Debug.LogError("Actions not ready");
            return null; //TODO: throw exception
        }
        GameObject puck = Instantiate(PuckPrefab, ap.ActionsSpawn.transform);
        puck.SetActive(false);

        puck.GetComponent<HAction>().Init(projectAction, actionMetadata, ap, actionProvider);

        puck.transform.localScale = new Vector3(1f, 1f, 1f);

        HAction action = puck.GetComponent<HAction>();

        // Add new action into scene reference
        ActionPoints[ap.Data.Id].Actions.Add(action.Data.Id, action);
        ap.UpdatePositionsOfPucks();
        puck.SetActive(true);

        return action;
    }

    /// <summary>
    /// Removes actino from project
    /// </summary>
    /// <param name="action"></param>
    public void ActionRemoved(IO.Swagger.Model.BareAction action) {
        ProjectManager.Instance.RemoveAction(action.Id);
        updateProject = true;
    }

    
    /// <summary>
    /// Returns all actions in the scene in a dictionary [action_Id, Action_object]
    /// </summary>
    /// <returns></returns>
    public Dictionary<string, HAction> GetAllActionsDict() {
        Dictionary<string, HAction> actions = new Dictionary<string, HAction>();
        foreach (HActionPoint actionPoint in ActionPoints.Values) {
            foreach (HAction action in actionPoint.Actions.Values) {
                actions.Add(action.Data.Id, action);
            }
        }

        return actions;
    }


    
        /// <summary>
        /// Finds parent of action point based on its ID
        /// </summary>
        /// <param name="parentId">ID of parent object</param>
        /// <returns></returns>
        public IActionPointParentH GetActionPointParent(string parentId) {
            if (parentId == null || parentId == "")
                throw new KeyNotFoundException("Action point parent " + parentId + " not found");
            if (SceneManagerH.Instance.ActionObjects.TryGetValue(parentId, out ActionObjectH actionObject)) {
                return actionObject;
            }
            if (ActionPoints.TryGetValue(parentId, out HActionPoint actionPoint)) {
                return actionPoint;
            }

            throw new KeyNotFoundException("Action point parent " + parentId + " not found");
        }

        
        /// <summary>
        /// Returns action point of given Id.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public HActionPoint GetActionPoint(string id) {
            if (ActionPoints.TryGetValue(id, out HActionPoint actionPoint)) {
                return actionPoint;
            }

            throw new KeyNotFoundException("ActionPoint \"" + id + "\" not found!");
        }



        /// <summary>
        /// Spawn action point into the project
        /// </summary>
        /// <param name="apData">Json describing action point</param>
        /// <param name="actionPointParent">Parent of action point. If null, AP is spawned as global.</param>
        /// <returns></returns>
        public HActionPoint SpawnActionPoint(IO.Swagger.Model.ActionPoint apData, IActionPointParentH actionPointParent) {
            Debug.Assert(apData != null);
            GameObject AP;
            if (actionPointParent == null) {               
                AP = Instantiate(ActionPointPrefab, ActionPointsOrigin.transform);
            } else {
                AP = Instantiate(ActionPointPrefab, actionPointParent.GetSpawnPoint());
            }
            AP.transform.localScale = new Vector3(1f, 1f, 1f);
            HActionPoint actionPoint = AP.GetComponent<HActionPoint>();
            actionPoint.InitAP(apData, APSize, actionPointParent);
            ActionPoints.Add(actionPoint.Data.Id, actionPoint);
         //   OnActionPointAddedToScene?.Invoke(this, new ActionPointEventArgs(actionPoint));
            return actionPoint;
        }

        
        /// <summary>
        /// Sets project metadata
        /// </summary>
        /// <param name="project"></param>
        public void SetProjectMeta(BareProject project) {
            if (ProjectMeta == null) {
                ProjectMeta = new Project(sceneId: "", id: "", name: "");
            }
            ProjectMeta.Id = project.Id;
            ProjectMeta.SceneId = project.SceneId;
            ProjectMeta.HasLogic = project.HasLogic;
            ProjectMeta.Description = project.Description;
            ProjectMeta.IntModified = project.IntModified;
            ProjectMeta.Modified = project.Modified;
            ProjectMeta.Name = project.Name;
            
        }

}
