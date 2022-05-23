using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Hololens;
using System.Threading.Tasks;
using System;
using Base;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.UI.BoundsControl;
using Microsoft.MixedReality.Toolkit.UI;



public abstract class HStartEndAction : HAction
{
    public Renderer Visual;
    public GameObject VisualRoot;

    protected string playerPrefsKey;
    [SerializeField]
  //  protected OutlineOnClick outlineOnClick;
    public GameObject ModelPrefab;
    public GameObject interactObject;


    public override void OnClick() {
       /* if (type == Click.MOUSE_LEFT_BUTTON || type == Click.LONG_TOUCH) {
            // We have clicked with left mouse and started manipulation with object
            StartManipulation();
        }*/
    }

    public virtual void Init(IO.Swagger.Model.Action projectAction, ActionMetadataH metadata, HActionPoint ap, IActionProviderH actionProvider, string actionType) {
        base.Init(projectAction, metadata, ap, actionProvider);
        interactObject.GetComponentInChildren<Interactable>().OnClick.AddListener(() => HSelectorManager.Instance.OnSelectObject(this) );


        if (!HProjectManager.Instance.ProjectMeta.HasLogic) {
            Destroy(gameObject);
            return;
        }
        playerPrefsKey = "project/" + HProjectManager.Instance.ProjectMeta.Id + "/" + actionType;

    }

    public void SavePosition() {
        PlayerPrefsHelper.SaveVector3(playerPrefsKey, transform.localPosition);
    }

    public override void OnHoverStart() {
        if (GameManagerH.Instance.GetEditorState() != GameManagerH.EditorStateEnum.Normal &&
            GameManagerH.Instance.GetEditorState() != GameManagerH.EditorStateEnum.SelectingAction) {
            if (GameManagerH.Instance.GetEditorState() == GameManagerH.EditorStateEnum.InteractionDisabled) {
                if (GameManagerH.Instance.GetGameState() != GameManagerH.GameStateEnum.PackageRunning)
                    return;
            } else {
                return;
            }
        }
        if (GameManagerH.Instance.GetGameState() != GameManagerH.GameStateEnum.ProjectEditor &&
            GameManagerH.Instance.GetGameState() != GameManagerH.GameStateEnum.PackageRunning) {
            return;
        }
     //   outlineOnClick.Highlight();
        NameText.gameObject.SetActive(true);
      /*  if (SelectorMenu.Instance.ManuallySelected) {
            DisplayOffscreenIndicator(true);
        }*/
    }

    public override void OnHoverEnd() {
     //   outlineOnClick.UnHighlight();
        NameText.gameObject.SetActive(false);
        DisplayOffscreenIndicator(false);
    }

    public async override Task<RequestResult> Movable() {
        return new RequestResult(true);
    }

    public override bool HasMenu() {
        return false;
    }

    public override void StartManipulation() {
        throw new NotImplementedException();
    }

    public override async Task<bool> WriteUnlock() {
        return true;
    }

 /*   public override async Task<bool> WriteLock(bool lockTree) {
        return true;
    }*/

    protected override void OnObjectLockingEvent(object sender, ObjectLockingEventArgs args) {
        return;
    }

    public override void OnObjectLocked(string owner) {
        return;
    }

    public override void OnObjectUnlocked() {
        return;
    }

    public override string GetName() {
        return Data.Name;
    }    

    public override void OpenMenu() {
        throw new NotImplementedException();
    }

    public async override Task<RequestResult> Removable() {
        return new RequestResult(false, GetObjectTypeName() + " could not be removed");
    }

    public override void Remove() {
        throw new NotImplementedException();
    }

    public override Task Rename(string name) {
        throw new NotImplementedException();
    }

    public GameObject GetModelCopy() {
        return Instantiate(ModelPrefab);
    }

    public override void EnableVisual(bool enable) {
        VisualRoot.SetActive(enable);
        interactObject.SetActive(enable);
    }

     public void setInterarction(GameObject interactComponents){

        BoxCollider collider = interactComponents.GetComponent<BoxCollider>();
        collider.size = interactObject.transform.localScale;
        collider.center = interactObject.transform.localPosition;

        BoundsControl boundsControl = interactComponents.GetComponent<BoundsControl>();
        ObjectManipulator objectManipulator = interactComponents.GetComponent<ObjectManipulator>();
        boundsControl.BoundsOverride = collider;

             
        boundsControl.ScaleLerpTime = 1L;
        boundsControl.RotateLerpTime = 1L;
        objectManipulator.ScaleLerpTime = 1L;
        objectManipulator.RotateLerpTime = 1L;

        boundsControl.ScaleHandlesConfig.ShowScaleHandles = false;
        boundsControl.RotationHandlesConfig.ShowHandleForX = false;
        boundsControl.RotationHandlesConfig.ShowHandleForY = false;
        boundsControl.RotationHandlesConfig.ShowHandleForZ = false;

   
        boundsControl.UpdateBounds();
    }
}
