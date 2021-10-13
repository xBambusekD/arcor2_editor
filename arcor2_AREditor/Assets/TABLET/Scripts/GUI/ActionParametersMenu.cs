using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Base;
using IO.Swagger.Model;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;

public class ActionParametersMenu : Singleton<ActionParametersMenu>
{
    public GameObject Content;
    public CanvasGroup CanvasGroup;
    private Action3D currentAction;
    private List<IParameter> actionParameters = new List<IParameter>();
    public VerticalLayoutGroup DynamicContentLayout;
    public GameObject CanvasRoot;

    public TransformWheel TransformWheel;

    [SerializeField]
    private LabeledInput VelocityInput;


    public TMPro.TMP_Text ActionName, ActionType, ActionPointName;

    public async Task<bool> Show(Action3D action) {
        if (!await action.WriteLock(false))
            return false;
        currentAction = action;
        actionParameters = await Base.Parameter.InitActionParameters(currentAction.ActionProvider.GetProviderId(), currentAction.Parameters.Values.ToList(), Content, OnChangeParameterHandler, DynamicContentLayout, CanvasRoot, false, CanvasGroup);

        ActionName.text = $"Name: {action.GetName()}";
        ActionType.text = $"Type: {action.ActionProvider.GetProviderName()}/{action.Metadata.Name}";
        ActionPointName.text = $"AP: {action.ActionPoint.GetName()}";
        VelocityInput.SetValue(action.Parameters["velocity"].Value);
        TransformWheel.SetValue(int.Parse(action.Parameters["velocity"].Value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture));
        EditorHelper.EnableCanvasGroup(CanvasGroup, true);
        return true;
    }

    private void Update() {
        if (currentAction == null)
            return;

        if (TransformWheel.GetValue() < 0)
            TransformWheel.SetValue(0);
        else if (TransformWheel.GetValue() > 100)
            TransformWheel.SetValue(100);

        VelocityInput.SetValue(TransformWheel.GetValue());
    }

    public async void Hide(bool unlock = true) {
        RectTransform[] transforms = Content.GetComponentsInChildren<RectTransform>();
        if (transforms != null) {
            foreach (RectTransform o in transforms) {
                if (o.gameObject.tag != "Persistent") {
                    Destroy(o.gameObject);
                }
            }
        }
        if (CanvasGroup.alpha > 0)
            await Confirm();
        EditorHelper.EnableCanvasGroup(CanvasGroup, false);
        if (currentAction != null) {
            if(unlock)
                await currentAction.WriteUnlock();
            currentAction = null;
        }
    }

    public bool IsVisible() {
        return CanvasGroup.alpha > 0;
    }

    public void SetVisibility(bool visible) {
        EditorHelper.EnableCanvasGroup(CanvasGroup, visible);
    }

    public void OnChangeParameterHandler(string parameterId, object newValue, string type, bool isValueValid = true) {
        if (isValueValid && currentAction.Parameters.TryGetValue(parameterId, out Base.Parameter actionParameter)) {           
            try {
                if (JsonConvert.SerializeObject(newValue) != actionParameter.Value) {
                    SaveParameters();
                }
            } catch (JsonReaderException) {

            }
        }

    }

    public async Task Confirm() {
        //List<ActionParameter> parameters = new List<ActionParameter>();
        /*foreach (KeyValuePair<string, Base.Parameter> param in action.Parameters) {
            if (param.Value.Name == "velocity")
                parameters.Add(new ActionParameter(name: "velocity", type: "double", value: JsonConvert.SerializeObject((float) TransformWheel.GetValue())));
            else
                parameters.Add(new ActionParameter(name: param.Value.Name, type: param.Value.Type, value: param.Value.Value));
        }*/
        NamedOrientation o = currentAction.ActionPoint.GetFirstOrientation();
        List<ActionParameter> parameters = new List<ActionParameter> {
            new ActionParameter(name: "pose", type: "pose", value: "\"" + o.Id + "\""),
            new ActionParameter(name: "move_type", type: "string_enum", value: "\"JOINTS\""),
            new ActionParameter(name: "velocity", type: "double", value: JsonConvert.SerializeObject((float) TransformWheel.GetValue())),
            new ActionParameter(name: "acceleration", type: "double", value: JsonConvert.SerializeObject((float) TransformWheel.GetValue()))
        };

        Debug.Assert(ProjectManager.Instance.AllowEdit);
        try {
            await WebsocketManager.Instance.UpdateAction(currentAction.GetId(), parameters, currentAction.GetFlows());
            Base.Notifications.Instance.ShowToastMessage("Parameters saved");
        } catch (RequestFailedException e) {
            Notifications.Instance.ShowNotification("Failed to update action ", e.Message);
        }

    }

    public async void SaveParameters() {
        if (Base.Parameter.CheckIfAllValuesValid(actionParameters)) {
            List<IO.Swagger.Model.ActionParameter> parameters = new List<IO.Swagger.Model.ActionParameter>();
            foreach (IParameter actionParameter in actionParameters) {
                IO.Swagger.Model.ParameterMeta metadata = currentAction.Metadata.GetParamMetadata(actionParameter.GetName());
                string value = JsonConvert.SerializeObject(actionParameter.GetValue());
                IO.Swagger.Model.ActionParameter ap = new IO.Swagger.Model.ActionParameter(name: actionParameter.GetName(), value: value, type: actionParameter.GetCurrentType());
                parameters.Add(ap);
            }
            Debug.Assert(ProjectManager.Instance.AllowEdit);
            try {
                await WebsocketManager.Instance.UpdateAction(currentAction.Data.Id, parameters, currentAction.GetFlows());
                Notifications.Instance.ShowToastMessage("Parameters saved");
            } catch (RequestFailedException e) {
                Notifications.Instance.ShowNotification("Failed to save parameters", e.Message);
            }
        }
    }


}
