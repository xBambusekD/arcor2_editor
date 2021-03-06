using UnityEngine;
using UnityEngine.UI;
using Base;
using UnityEngine.EventSystems;

public class ActionObjectMenuProjectEditor : MonoBehaviour, IMenu {
    public Base.ActionObject CurrentObject;
    [SerializeField]
    private TMPro.TMP_Text objectName;
    public Slider VisibilitySlider;
    public GameObject DynamicContent;

    [SerializeField]
    private InputDialog inputDialog;

    
    public async void CreateNewAP(string name) {
        Debug.Assert(CurrentObject != null);
        IO.Swagger.Model.Position offset = new IO.Swagger.Model.Position();
        if (CurrentObject.ActionObjectMetadata.ObjectModel != null) {
            switch (CurrentObject.ActionObjectMetadata.ObjectModel.Type) {
                case IO.Swagger.Model.ObjectModel.TypeEnum.Box:
                    offset.Z = CurrentObject.ActionObjectMetadata.ObjectModel.Box.SizeY / 2m + 0.1m;
                    break;
                case IO.Swagger.Model.ObjectModel.TypeEnum.Cylinder:
                    offset.Z = CurrentObject.ActionObjectMetadata.ObjectModel.Cylinder.Height / 2m + 0.1m;
                    break;
                case IO.Swagger.Model.ObjectModel.TypeEnum.Mesh:
                    //TODO: how to handle meshes? do i know dimensions?
                    break;
                case IO.Swagger.Model.ObjectModel.TypeEnum.Sphere:
                    offset.Z = CurrentObject.ActionObjectMetadata.ObjectModel.Sphere.Radius / 2m + 0.1m;
                    break;
                default:
                    offset.Z = 0.15m;
                    break;
            }
        }
        bool result = await GameManager.Instance.AddActionPoint(name, CurrentObject.Data.Id, offset);
        //Base.Scene.Instance.SpawnActionPoint(CurrentObject.GetComponent<Base.ActionObject>(), null);
        if (result)
            inputDialog.Close();
        UpdateMenu();
    }

    public void ShowAddActionPointDialog() {
        inputDialog.Open("Create action point",
                         "Type action point name",
                         "Name",
                         Scene.Instance.GetFreeAPName(CurrentObject.Data.Name),
                         () => CreateNewAP(inputDialog.GetValue()),
                         () => inputDialog.Close());
    }

    public void UpdateMenu() {
        objectName.text = CurrentObject.Data.Name;
        VisibilitySlider.value = CurrentObject.GetVisibility()*100;
        foreach (Transform t in DynamicContent.transform) {
            Destroy(t.gameObject);
        }
        foreach (ActionPoint actionPoint in CurrentObject.GetActionPoints()) {
            Button button = GameManager.Instance.CreateButton(DynamicContent.transform, actionPoint.Data.Name);
            button.onClick.AddListener(() => ShowActionPoint(actionPoint));

            // Add EventTrigger OnPointerEnter and OnPointerExit - to be able to highlight corresponding AP when hovering over button
            OutlineOnClick APoutline = actionPoint.GetComponent<OutlineOnClick>();
            EventTrigger eventTrigger = button.gameObject.AddComponent<EventTrigger>();
            // Create OnPointerEnter entry
            EventTrigger.Entry OnPointerEnter = new EventTrigger.Entry {
                eventID = EventTriggerType.PointerEnter
            };
            OnPointerEnter.callback.AddListener((eventData) => APoutline.Highlight());
            eventTrigger.triggers.Add(OnPointerEnter);

            // Create OnPointerExit entry
            EventTrigger.Entry OnPointerExit = new EventTrigger.Entry {
                eventID = EventTriggerType.PointerExit
            };
            OnPointerExit.callback.AddListener((eventData) => APoutline.UnHighlight());
            eventTrigger.triggers.Add(OnPointerExit);
        }
    }

    public void OnVisibilityChange(float value) {
        CurrentObject.SetVisibility(value/100f); 
    }

    public void ShowNextAO() {
        ActionObject nextAO = Scene.Instance.GetNextActionObject(CurrentObject.Data.Id);
        ShowActionObject(nextAO);
    }

    public void ShowPreviousAO() {
        ActionObject previousAO = Scene.Instance.GetNextActionObject(CurrentObject.Data.Id);
        ShowActionObject(previousAO);
    }

    private static void ShowActionObject(ActionObject actionObject) {
        actionObject.ShowMenu();
        Scene.Instance.SetSelectedObject(actionObject.gameObject);
        actionObject.SendMessage("Select");
    }

    private static void ShowActionPoint(ActionPoint actionPoint) {
        MenuManager.Instance.ActionObjectMenuProjectEditor.Close();
        actionPoint.ShowMenu(true);
        
        Scene.Instance.SetSelectedObject(actionPoint.gameObject);
        actionPoint.SendMessage("Select");
    }
}
