using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Hololens;
using Base;
using IO.Swagger.Model;
using Newtonsoft.Json;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Utilities;
using TMPro;

public class HelpSCRIPT : MonoBehaviour
{
  

     private void OnRobotModelLoaded(object sender, RobotUrdfModelArgs args) {
            Debug.Log("URDF:" + args.RobotType + " robot is fully loaded");

            // check if the robot of the type we need was loaded
          
                // if so, lets ask UrdfManagerH for the robot model
               RobotModelH RobotModel = UrdfManagerH.Instance.GetRobotModelInstance(args.RobotType);
               Rigidbody[] bodies = RobotModel.RobotModelGameObject.GetComponentsInChildren<Rigidbody>();
               foreach(Rigidbody body in bodies){
                 ObjectManipulator manipulator =   body.gameObject.AddComponent<ObjectManipulator>();
                 //manipulator.
               }
            

              RobotModel.SetActiveAllVisuals(true);
                
                // if robot is loaded, unsubscribe from UrdfManagerH event
                UrdfManagerH.Instance.OnRobotUrdfModelLoaded -= OnRobotModelLoaded;
            
        }

  public void add(){
          foreach (ActionObjectMetadataH actionObject in ActionsManagerH.Instance.ActionObjectsMetadata.Values.OrderBy(x => x.Type)) {
              if(actionObject.Robot){
                       if (ActionsManagerH.Instance.RobotsMeta.TryGetValue(actionObject.Type, out RobotMeta robotMeta)) {
                        
                        if (!string.IsNullOrEmpty(robotMeta.UrdfPackageFilename)) {
                       
                                // Get the robot model, if it returns null, the robot will be loading itself
                            RobotModelH RobotModel = UrdfManagerH.Instance.GetRobotModelInstance(robotMeta.Type, robotMeta.UrdfPackageFilename);
                            
                                if (RobotModel != null) {
                                 
                                 //   frontPlate.GetComponent<Interactable>().OnClick.AddListener(() => AddObjectToScene(actionObject.Type));
                                if (!RobotModel.RobotModelGameObject.name.Equals("Eddie")){
                                       Rigidbody[] bodies = RobotModel.RobotModelGameObject.GetComponentsInChildren<Rigidbody>();
               foreach(Rigidbody body in bodies){
                 ObjectManipulator manipulator =   body.gameObject.AddComponent<ObjectManipulator>();
                 //manipulator.
               }
                                }
                                
                                    RobotModel.SetActiveAllVisuals(true);
                                } else {
                                    // Robot is not loaded yet, let's wait for it to be loaded
                                
                                    UrdfManagerH.Instance.OnRobotUrdfModelLoaded += OnRobotModelLoaded;
                                }
                            }
                        }

                }
          }
  }
}
