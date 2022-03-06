using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Base;
using Microsoft.MixedReality.Toolkit.UI.BoundsControl;
using Microsoft.MixedReality.Toolkit.UI;
using System.Threading.Tasks;
using Hololens;


public class HTransformMenu : Singleton<HTransformMenu> {

    HInteractiveObject InteractiveObject;
    GameObject model;

    public Transform GizmoTransform;


    void  Start() {
        GizmoTransform.gameObject.GetComponent<ObjectManipulator>().OnManipulationEnded.AddListener((s) => updatePosition());
    }


    public async Task updatePosition() {
        await WebSocketManagerH.Instance.UpdateActionObjectPose(InteractiveObject.GetId(), new IO.Swagger.Model.Pose(position: DataHelper.Vector3ToPosition(TransformConvertor.UnityToROS(GameManagerH.Instance.Scene.transform.InverseTransformPoint(model.transform.position) /*InteractiveObject.transform.localPosition + model.transform.localPosition*/)),
                                                                                                                                orientation: DataHelper.QuaternionToOrientation(TransformConvertor.UnityToROS(Quaternion.Inverse(GameManagerH.Instance.Scene.transform.rotation) * model.transform.rotation   /*InteractiveObject.transform.localRotation * model.transform.localRotation*/))));

    }


    public void deactiveTransform(){

      
        GizmoTransform.gameObject.SetActive(false);

        if (InteractiveObject != null) {
              ((RobotActionObjectH) InteractiveObject).EnableOffscreenIndicator(true);
              ((RobotActionObjectH) InteractiveObject).EnableVisual(true);
        }

        InteractiveObject = null;
        Destroy(model);
        model = null;

    }

    public void activeTransform(HInteractiveObject interactiveObject) {


        InteractiveObject = interactiveObject;
      
        GizmoTransform.transform.position = InteractiveObject.transform.position;
        GizmoTransform.transform.rotation = InteractiveObject.transform.rotation;

        model = ((RobotActionObjectH) InteractiveObject).GetModelCopy();
        model.transform.SetParent(p: GizmoTransform);
        model.transform.rotation = InteractiveObject.transform.rotation;
        model.transform.position = InteractiveObject.transform.position;

        GizmoTransform.gameObject.SetActive(true);
        ((RobotActionObjectH) InteractiveObject).setInterarction(GizmoTransform.gameObject);

        /*    BoxCollider collider = model.AddComponent<BoxCollider>();
            collider.size = ((RobotActionObjectH)  InteractiveObject).getInteractObject().transform.localScale;
            collider.center = ((RobotActionObjectH)  InteractiveObject).getInteractObject().transform.position;
            BoundsControl boundsControl = model.AddComponent<BoundsControl>();

            ObjectManipulator objectManipulator = model.AddComponent<ObjectManipulator>();*/



        /* Target target = model.AddComponent<Target>();
         target.SetTarget(Color.yellow, false, true, false);
         target.enabled = true;*/

        ((RobotActionObjectH) InteractiveObject).EnableOffscreenIndicator(false);
        ((RobotActionObjectH) InteractiveObject).EnableVisual(false);

    }

}
