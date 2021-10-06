using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Base;
using IO.Swagger.Model;
using RosSharp.Urdf;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RightButtonsMenu : Singleton<RightButtonsMenu> {
    public ButtonWithTooltip SelectBtn, CollapseBtn, MenuTriggerBtn, AddActionBtn, RemoveBtn, MoveBtn, ExecuteBtn, ConnectionBtn;
    public Sprite CollapseIcon, UncollapseIcon;

    public Image RobotHandBtn;

    private InteractiveObject selectedObject;
    private ButtonInteractiveObject selectedButton;

    public GameObject ActionPicker;

    public bool Connecting;
    private RobotEE robotEE;

    private void Awake() {
        SelectorMenu.Instance.OnObjectSelectedChangedEvent += OnObjectSelectedChangedEvent;
        Sight.Instance.OnObjectSelectedChangedEvent += OnButtonObjectSelectedChangedEvent;
        Connecting = false;
    }

    private void OnButtonObjectSelectedChangedEvent(object sender, ButtonInteractiveObjectEventArgs args) {
        selectedButton = args.InteractiveObject;
        MenuTriggerBtn.SetInteractivity(selectedButton != null, "No item selected");
    }

    private void OnObjectSelectedChangedEvent(object sender, InteractiveObjectEventArgs args) {
        selectedObject = args.InteractiveObject;
        if (SelectorMenu.Instance.ManuallySelected)
            SelectBtn.GetComponent<Image>().enabled = true;
        else
            SelectBtn.GetComponent<Image>().enabled = false;
        if (args.InteractiveObject != null) {
            SelectBtn.SetInteractivity(true);
            CollapseBtn.SetInteractivity(true);
            Base.ActionPoint actionPoint;
            if (args.InteractiveObject is ActionPoint3D || args.InteractiveObject.GetType() == typeof(Action3D)) {
                if (args.InteractiveObject is Action3D action)
                    if (action.ActionPoint != null)
                        actionPoint = action.ActionPoint;
                    else {
                        return;
                    }
                else {
                    actionPoint = (Base.ActionPoint) args.InteractiveObject;
                }
                if (actionPoint.ActionsCollapsed) {
                    CollapseBtn.GetComponent<IconButton>().Icon.sprite = UncollapseIcon;
                    CollapseBtn.SetDescription("Show actions");
                } else {
                    CollapseBtn.GetComponent<IconButton>().Icon.sprite = CollapseIcon;
                    CollapseBtn.SetDescription("Hide actions");
                }
            } else {
                CollapseBtn.SetInteractivity(false, "Selected object is not action point");
            }
            Task<RequestResult> tMove = Task.Run(() => selectedObject.Movable());
            Task<RequestResult> tRemove = Task.Run(() => selectedObject.Removable());
            UpdateMoveAndRemoveBtns(selectedObject.GetId(), tMove, tRemove);
            
            ExecuteBtn.SetInteractivity(selectedObject.GetType() == typeof(StartAction) || selectedObject.GetType() == typeof(Action3D) || selectedObject.GetType() == typeof(ActionPoint3D));
            AddActionBtn.SetInteractivity(selectedObject.GetType() == typeof(Action3D) ||
                selectedObject.GetType() == typeof(ActionPoint3D) ||
                selectedObject.GetType() == typeof(ConnectionLine) ||
                selectedObject is RobotEE || selectedObject is RobotActionObject);
            if (selectedObject is RobotEE)
                RobotHandBtn.color = Color.white;
            else
                RobotHandBtn.color = new Color(1, 1, 1, 0.4f);
        } else {
            CollapseBtn.SetInteractivity(false, "No object selected");
            SelectBtn.SetInteractivity(false, "No object selected");
            RemoveBtn.SetInteractivity(false, "No object selected");
            MoveBtn.SetInteractivity(false, "No object selected");
            ExecuteBtn.SetInteractivity(false, "No object selected");
            AddActionBtn.SetInteractivity(true);
            RobotHandBtn.color = new Color(1, 1, 1, 0.4f);
        }

    }

    private async void UpdateMoveAndRemoveBtns(string objId, Task<RequestResult> movable, Task<RequestResult> removable) {
        RequestResult move = await movable;
        RequestResult remove = await removable;

        if (selectedObject != null && objId != selectedObject.GetId()) // selected object was updated in the meantime
            return;
        MoveBtn.SetInteractivity(move.Success, $"Move object\n({move.Message})");
        RemoveBtn.SetInteractivity(remove.Success, $"Remove object\n({remove.Message})");
    }

    public void SelectClick() {
        if (selectedObject != null) {
            SelectorMenu.Instance.SetSelectedObject(selectedObject, true);
        }
    }

    public void CollapseClick() {
        if (selectedObject != null) {
            string apId = null;
            if (selectedObject is ActionPoint3D actionPoint3D)
                apId = actionPoint3D.GetId();
            else if (selectedObject is Action3D action && action.ActionPoint != null)
                apId = action.ActionPoint.GetId();
            else
                return;
            if (SelectorMenu.Instance.SelectorItems.TryGetValue(apId, out SelectorItem selectorItem)) {
                selectorItem.CollapseBtnCb();
                OnObjectSelectedChangedEvent(this, new InteractiveObjectEventArgs(selectedObject));
            }
        }
    }

    public void TriggerClick() {
        selectedButton.OnClick(Clickable.Click.MOUSE_LEFT_BUTTON);
    }

    public void AddAction() {
        if (selectedObject != null && (selectedObject is Action3D || selectedObject is ActionPoint3D)) {

            ActionPicker.transform.position = Camera.main.transform.position + Camera.main.transform.forward * 0.2f;
            ActionPicker.GetComponent<FaceCamera>().Update();
            ActionPicker.SetActive(true);
            if (selectedObject is Action3D action) {
                action.ActionPoint.SetApCollapsed(false);
            } else if (selectedObject is ActionPoint3D actionPoint) {
                actionPoint.SetApCollapsed(false);
            } else {
                return;
            }


        } else {
            string name = ProjectManager.Instance.GetFreeAPName("ap");
            AREditorResources.Instance.LeftMenuProject.ApToAddActionName = name;
            AREditorResources.Instance.LeftMenuProject.ActionCb = SelectAP;
            if (selectedObject != null) {
                if (selectedObject is ConnectionLine connectionLine) {
                    if (ProjectManager.Instance.LogicItems.TryGetValue(connectionLine.LogicItemId, out LogicItem logicItem)) {
                        logicItem.Input.Action.ActionPoint?.SetApCollapsed(false);
                        logicItem.Output.Action.ActionPoint?.SetApCollapsed(false);
                        ProjectManager.Instance.PrevAction = logicItem.Output.Action.GetId();
                        ProjectManager.Instance.NextAction = logicItem.Input.Action.GetId();
                        connectionLine.Remove();
                        GameManager.Instance.AddActionPointExperiment(name, false);
                    }
                } else if (selectedObject is RobotEE robotEE) {
                    GameManager.Instance.AddActionPointExperiment(name, false, robotEE);
                } else {
                    GameManager.Instance.AddActionPointExperiment(name, false);
                }

            } else {
                GameManager.Instance.AddActionPointExperiment(name, false);
            }

        }
        SelectorMenu.Instance.Active = false;
        SetMenuTriggerMode();
    }

    public void SelectAP(ActionPoint3D actionPoint) {

        actionPoint.SetApCollapsed(false);
        //SelectorMenu.Instance.SetSelectedObject(actionPoint, true);
        AREditorResources.Instance.LeftMenuProject.APToRemoveOnCancel = actionPoint;
        ActionPicker.transform.position = actionPoint.transform.position - Camera.main.transform.forward * 0.05f;
        ActionPicker.SetActive(true);
    }

    public void SetMenuTriggerMode() {
        MenuTriggerBtn.gameObject.SetActive(true);
        SelectBtn.gameObject.SetActive(false);
        CollapseBtn.gameObject.SetActive(true);
        AddActionBtn.gameObject.SetActive(false);
        MoveBtn.gameObject.SetActive(false);
        RemoveBtn.gameObject.SetActive(false);
        ExecuteBtn.gameObject.SetActive(false);
        ConnectionBtn.gameObject.SetActive(false);
    }

    public void SetSelectorMode() {
        SelectBtn.gameObject.SetActive(true);
        CollapseBtn.gameObject.SetActive(true);
        MenuTriggerBtn.gameObject.SetActive(false);
        AddActionBtn.gameObject.SetActive(false);
        MoveBtn.gameObject.SetActive(false);
        RemoveBtn.gameObject.SetActive(false);
        ExecuteBtn.gameObject.SetActive(false);
        ConnectionBtn.gameObject.SetActive(false);
    }

    public void SetActionMode() {
        SelectorMenu.Instance.DeselectObject();
        SelectBtn.gameObject.SetActive(false);
        CollapseBtn.gameObject.SetActive(true);
        MenuTriggerBtn.gameObject.SetActive(false);
        AddActionBtn.gameObject.SetActive(true);
        MoveBtn.gameObject.SetActive(false);
        RemoveBtn.gameObject.SetActive(false);
        ExecuteBtn.gameObject.SetActive(false);
        ConnectionBtn.gameObject.SetActive(false);
    }

    public void SetMoveMode() {
        SelectorMenu.Instance.DeselectObject();
        SelectBtn.gameObject.SetActive(false);
        CollapseBtn.gameObject.SetActive(true);
        MenuTriggerBtn.gameObject.SetActive(false);
        AddActionBtn.gameObject.SetActive(false);
        MoveBtn.gameObject.SetActive(true);
        RemoveBtn.gameObject.SetActive(false);
        ExecuteBtn.gameObject.SetActive(false);
        ConnectionBtn.gameObject.SetActive(false);
    }

    public void SetRemoveMode() {
        SelectorMenu.Instance.DeselectObject();
        SelectBtn.gameObject.SetActive(false);
        CollapseBtn.gameObject.SetActive(true);
        MenuTriggerBtn.gameObject.SetActive(false);
        AddActionBtn.gameObject.SetActive(false);
        MoveBtn.gameObject.SetActive(false);
        RemoveBtn.gameObject.SetActive(true);
        ExecuteBtn.gameObject.SetActive(false);
        ConnectionBtn.gameObject.SetActive(false);
    }

    public void SetRunMode() {
        SelectorMenu.Instance.DeselectObject();
        SelectBtn.gameObject.SetActive(false);
        CollapseBtn.gameObject.SetActive(true);
        MenuTriggerBtn.gameObject.SetActive(false);
        AddActionBtn.gameObject.SetActive(false);
        MoveBtn.gameObject.SetActive(false);
        RemoveBtn.gameObject.SetActive(false);
        ExecuteBtn.gameObject.SetActive(true);
        ConnectionBtn.gameObject.SetActive(false);

    }

    public void SetConnectionsMode() {
        SelectorMenu.Instance.DeselectObject();
        SelectBtn.gameObject.SetActive(false);
        CollapseBtn.gameObject.SetActive(true);
        MenuTriggerBtn.gameObject.SetActive(false);
        AddActionBtn.gameObject.SetActive(false);
        MoveBtn.gameObject.SetActive(false);
        RemoveBtn.gameObject.SetActive(false);
        ExecuteBtn.gameObject.SetActive(false);
        ConnectionBtn.gameObject.SetActive(true);
    }

    public void MoveClick() {

        if (selectedObject is null)
            return;

        
        //SelectorMenu.Instance.Active = false;
        gameObject.SetActive(false);

        InteractiveObject obj = selectedObject;
        if (selectedObject is Action3D action)
            obj = action.ActionPoint;
        //TransformMenu.Instance.Show(obj, selectedObject.GetType() == typeof(DummyAimBox) || selectedObject.GetType() == typeof(DummyAimBoxTester), selectedObject.GetType() == typeof(DummyAimBoxTester));
        TransformMenu.Instance.Show(obj);
    }

    public void RemoveClick() {
        if (selectedObject is null)
            return;

        SelectorMenu.Instance.Active = false;
        //gameObject.SetActive(false);
        AREditorResources.Instance.LeftMenuProject.ConfirmationDialog.Open("Remove object",
                         "Are you sure you want to remove " + selectedObject.GetName() + "?",
                         () => {
                             selectedObject.Remove();
                             SetRemoveMode();
                             SelectorMenu.Instance.Active = true;
                         },
                         () => {
                             SetRemoveMode();
                             SelectorMenu.Instance.Active = true;
                             Debug.LogError("fasdhfjkkjyxchv");
                         });
    }

    public async void RunClick() {
        if (selectedObject is null)
            return;

        if (selectedObject.GetType() == typeof(StartAction)) {
            GameManager.Instance.SaveProject();
            GameManager.Instance.ShowLoadingScreen("Running project", true);
            try {
                await Base.WebsocketManager.Instance.TemporaryPackage();
                //MenuManager.Instance.MainMenu.Close();
            } catch (RequestFailedException ex) {
                Base.Notifications.Instance.ShowNotification("Failed to run temporary package", "");
                Debug.LogError(ex);
                GameManager.Instance.HideLoadingScreen(true);
            }
        } else if (selectedObject.GetType() == typeof(Action3D)) {
            try {
                await WebsocketManager.Instance.ExecuteAction(selectedObject.GetId(), false);
            } catch (RequestFailedException ex) {
                Notifications.Instance.ShowNotification("Failed to execute action", ex.Message);
                return;
            }
        } else if (selectedObject.GetType() == typeof(ActionPoint3D)) {
            string robotId = "";
            foreach (IRobot r in SceneManager.Instance.GetRobots()) {
                robotId = r.GetId();
            }
            NamedOrientation o = ((ActionPoint3D) selectedObject).GetFirstOrientation();
            IRobot robot = SceneManager.Instance.GetRobot(robotId);
            await WebsocketManager.Instance.MoveToActionPointOrientation(robot.GetId(), (await robot.GetEndEffectorIds())[0], 0.5m, o.Id, false);
        }
    }

    public async void ConnectionClick() {
        if (selectedObject != null && selectedObject is Action3D action) {

            if (Connecting) {
                if (action.Input.AnyConnection()) {
                    Notifications.Instance.ShowNotification("Failed to create connection", "There is already existing connection to this action");
                    ConnectionManagerArcoro.Instance.DestroyConnectionToMouse();
                    Connecting = false;
                    return;
                }
                Base.Action from = ConnectionManagerArcoro.Instance.GetActionConnectedToPointer();
                try {
                    await WebsocketManager.Instance.AddLogicItem(from.GetId(), action.GetId(), from.GetProjectLogicIf(), false);                
                } catch (RequestFailedException ex) {
                    Notifications.Instance.ShowNotification("Failed to create connection", ex.Message);
                    return;
                } finally {
                    ConnectionManagerArcoro.Instance.DestroyConnectionToMouse();
                    Connecting = false;
                }
            } else {
                if (action.Output.AnyConnection()) {
                    Notifications.Instance.ShowNotification("Failed to create connection", "There is already existing connection from this action");
                    return;
                }
                Connecting = true;
                ConnectionManagerArcoro.Instance.CreateConnectionToPointer(action.Output.gameObject);

            }
        } else {
            if (Connecting) {
                Connecting = false;
                ConnectionManagerArcoro.Instance.DestroyConnectionToMouse();
            }
        }
    }

    public void ResetConnectionMode() {
        if (Connecting) {
            ConnectionManagerArcoro.Instance.DestroyConnectionToMouse();
            Connecting = false;
        }
    }

    public void RobotHandTeachingPush() {
        if (selectedObject is RobotEE ee) {
            robotEE = ee;
        } else {
            return;
        }
        if (SceneManager.Instance.SelectedRobot == null)
            AREditorResources.Instance.LeftMenuProject.OpenRobotSelector();
        _ = WebsocketManager.Instance.HandTeachingMode(robotId: SceneManager.Instance.SelectedRobot.GetId(), enable: true);
    }

    public async void RobotHandTeachingRelease() {
        if (robotEE == null)
            return;
        if (SceneManager.Instance.SelectedRobot == null)
            AREditorResources.Instance.LeftMenuProject.OpenRobotSelector();
        await WebsocketManager.Instance.HandTeachingMode(robotId: SceneManager.Instance.SelectedRobot.GetId(), enable: false);
        IO.Swagger.Model.Position position = DataHelper.Vector3ToPosition(TransformConvertor.UnityToROS(GameManager.Instance.Scene.transform.InverseTransformPoint(robotEE.transform.position)));
        await WebsocketManager.Instance.MoveToPose(SceneManager.Instance.SelectedRobot.GetId(), SceneManager.Instance.SelectedEndEffector.GetId(), 1, position, DataHelper.QuaternionToOrientation(Quaternion.Euler(180, 0, 0)));
    }

    public void AddConnectionPush() {
        ConnectionClick();
    }

    public void AddConnectionRelease() {
        ConnectionClick();
    }



}
