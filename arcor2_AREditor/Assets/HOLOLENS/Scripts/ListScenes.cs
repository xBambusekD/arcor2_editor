using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using Base;
using IO.Swagger.Model;
using Hololens;
using TMPro;
using  RosSharp.Urdf;
using  Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.Utilities.Solvers;

using  Microsoft.MixedReality.Toolkit.UI;


public class ListScenes : Singleton<ListScenes>
{
    public GameObject SceneList;

    public GameObject SpherePrefab;
    public GameObject CubePrefab;
    public GameObject CylinderPrefab;

    public GameObject StartPrefab;

    public GameObject EndPrefab;

    public GameObject ActionPointPrefab;

    public GameObject ActionPrefab;

    public GameObject scenePrefab;
    public GameObject connectionPrefab;

    public Material lineMaterial;

    public GameObject Scrollmenu;

    public GameObject ScrollMenuContainer;

    private Dictionary<string, GameObject> scenes = new Dictionary<string, GameObject>();

      private Dictionary<string, Dictionary<string, GameObject>> actions_scenes = new Dictionary<string, Dictionary<string, GameObject>>();

      
    private Dictionary<string, List<IO.Swagger.Model.LogicItem>> project_logicItems = new Dictionary<string, List<IO.Swagger.Model.LogicItem>>();
    private string waitingSceneProject = null;


    public void setActiveMenu(bool active){
        Scrollmenu.SetActive(active);
        SceneList.SetActive(active);
        Scrollmenu.GetComponentInParent<RadialView>().enabled = !active;
    }
     void Start()
    {
        setActiveMenu(false);
        GameManagerH.Instance.OnScenesListChanged += UpdateScenes;
        GameManagerH.Instance.OnProjectsListChanged += UpdateProjects;
         GameManagerH.Instance.OnOpenSceneEditor += OnShowEditorScreen;
        GameManagerH.Instance.OnOpenProjectEditor += OnShowEditorScreen;
    }
    

    public void OnShowEditorScreen(object sender, EventArgs args){
       // SceneList.transform.parent = null;
         setActiveMenu(false);

    }

   public async void UpdateProjects(object sender, EventArgs eventArgs) {
         foreach(KeyValuePair<string, GameObject> kvp in scenes){
            Destroy(kvp.Value);
        }
        actions_scenes.Clear();
        project_logicItems.Clear();
        scenes.Clear (); 

        foreach (IO.Swagger.Model.ListProjectsResponseData project in GameManagerH.Instance.Projects) {
            createMenuProject( await WebSocketManagerH.Instance.GetScene(project.SceneId), await WebSocketManagerH.Instance.GetProject(project.Id) );
            
        }

        SceneList.GetComponent<GridObjectCollection>().UpdateCollection();
        UpdateLogicItem();
        setActiveMenu(true);

         SceneList.transform.parent = ScrollMenuContainer.transform;


   }



    public async void UpdateScenes(object sender, EventArgs eventArgs) {

        foreach(KeyValuePair<string, GameObject> kvp in scenes){
            Destroy(kvp.Value);
        }

        actions_scenes.Clear();
        project_logicItems.Clear();
        
        scenes.Clear (); 

        foreach (IO.Swagger.Model.ListScenesResponseData scene in GameManagerH.Instance.Scenes) {
            createMenuScene(await WebSocketManagerH.Instance.GetScene(scene.Id) );
        }

        SceneList.GetComponent<GridObjectCollection>().UpdateCollection();

        setActiveMenu(true);

        SceneList.transform.parent = ScrollMenuContainer.transform;
                



   }





     internal void createMenuScene(Scene scene){
 //await WebSocketManagerH.Instance.GetRobotMeta();

       GameObject newScene = Instantiate(scenePrefab, SceneList.transform);
       newScene.name = scene.Name;
       newScene.GetComponentInChildren<TextMeshPro>().text = scene.Name;
       GameObject ActionObjectsSpawn = new GameObject(scene.Name);

      Dictionary<string, GameObject> ActionObjects = new Dictionary<string, GameObject>();

        foreach (IO.Swagger.Model.SceneObject sceneObject in scene.Objects) {
            if (!ActionsManagerH.Instance.ActionObjectsMetadata.TryGetValue(sceneObject.Type, out ActionObjectMetadataH actionObject)) {
                    continue;
            }
            if(HActionObjectPickerMenu.Instance.objectsModels.TryGetValue(actionObject.Type, out GameObject objectModel)){
                Transform obj = objectModel.transform.Find("ImportedMeshObject");
                if(obj == null){
                    obj = objectModel.GetComponentInChildren<UrdfRobot>().transform;
                }
                GameObject menuObject = Instantiate(obj.gameObject, ActionObjectsSpawn.transform);
                menuObject.transform.localScale = new Vector3(1f,1f,1f);
                menuObject.transform.parent = ActionObjectsSpawn.transform;
                menuObject.transform.localPosition = TransformConvertor.ROSToUnity(DataHelper.PositionToVector3(sceneObject.Pose.Position));
                menuObject.transform.localRotation = TransformConvertor.ROSToUnity(DataHelper.OrientationToQuaternion(sceneObject.Pose.Orientation));
            }
            else if (actionObject.CollisionObject) {
                
                switch (actionObject.ObjectModel.Type) {
                    case IO.Swagger.Model.ObjectModel.TypeEnum.Box:
                        GameObject cube = Instantiate(CubePrefab, ActionObjectsSpawn.transform);
                        cube.transform.parent = ActionObjectsSpawn.transform;
                        cube.transform.localPosition = TransformConvertor.ROSToUnity(DataHelper.PositionToVector3(sceneObject.Pose.Position));
                        cube.transform.localRotation = TransformConvertor.ROSToUnity(DataHelper.OrientationToQuaternion(sceneObject.Pose.Orientation));
                        cube.transform.localScale = TransformConvertor.ROSToUnityScale(new Vector3((float) actionObject.ObjectModel.Box.SizeX, (float) actionObject.ObjectModel.Box.SizeY, (float) actionObject.ObjectModel.Box.SizeZ));
                        break;
                    case IO.Swagger.Model.ObjectModel.TypeEnum.Cylinder:
                        GameObject cylinder = Instantiate(CylinderPrefab, ActionObjectsSpawn.transform);
                        cylinder.transform.parent = ActionObjectsSpawn.transform;
                        cylinder.transform.localPosition = TransformConvertor.ROSToUnity(DataHelper.PositionToVector3(sceneObject.Pose.Position));
                        cylinder.transform.localRotation = TransformConvertor.ROSToUnity(DataHelper.OrientationToQuaternion(sceneObject.Pose.Orientation));
                        cylinder.transform.localScale = new Vector3((float) actionObject.ObjectModel.Cylinder.Radius, (float) actionObject.ObjectModel.Cylinder.Height / 2, (float) actionObject.ObjectModel.Cylinder.Radius);
                        break;
                    case IO.Swagger.Model.ObjectModel.TypeEnum.Sphere:
                         GameObject sphere = Instantiate(SpherePrefab, ActionObjectsSpawn.transform);
                        sphere.transform.parent = ActionObjectsSpawn.transform;
                        sphere.transform.localPosition = TransformConvertor.ROSToUnity(DataHelper.PositionToVector3(sceneObject.Pose.Position));
                        sphere.transform.localRotation = TransformConvertor.ROSToUnity(DataHelper.OrientationToQuaternion(sceneObject.Pose.Orientation));
                        sphere.transform.localScale = new Vector3((float) actionObject.ObjectModel.Sphere.Radius, (float) actionObject.ObjectModel.Sphere.Radius, (float) actionObject.ObjectModel.Sphere.Radius);
                    break;
                  
                    default:
                        GameObject defaultCube = Instantiate(CubePrefab, ActionObjectsSpawn.transform);
                        defaultCube.transform.parent = ActionObjectsSpawn.transform;
                        defaultCube.transform.localPosition = TransformConvertor.ROSToUnity(DataHelper.PositionToVector3(sceneObject.Pose.Position));
                        defaultCube.transform.localRotation = TransformConvertor.ROSToUnity(DataHelper.OrientationToQuaternion(sceneObject.Pose.Orientation));
                        defaultCube.transform.localScale = new Vector3(0.05f, 0.01f, 0.05f);
                        break;
                }
            }

        }

     Renderer[] meshes = ActionObjectsSpawn.GetComponentsInChildren<Renderer>();
     Bounds bounds = new Bounds(ActionObjectsSpawn.transform.position, Vector3.zero);
     foreach (Renderer mesh in meshes)
     {
         bounds.Encapsulate(mesh.bounds);
     }
     if(scene.Objects.Count == 0){
          newScene.transform.localScale = new Vector3(0.3f,0.3f,0.3f);
          newScene.transform.position  = Vector3.zero;
     }
     else{
         newScene.transform.localScale = bounds.size;
         newScene.transform.position  = bounds.center;//apply the center and size
        float maxV = Mathf.Max(Mathf.Max(newScene.transform.localScale.x, newScene.transform.localScale.y), newScene.transform.localScale.z);
        newScene.transform.localScale = new Vector3(maxV, maxV, maxV);

        ActionObjectsSpawn.transform.parent = newScene.transform;
        newScene.transform.localScale /= 10;


     }
    
    
 
    scenes.Add(scene.Name, newScene);

     newScene.GetComponent<Interactable>().OnClick.AddListener(() => openScene(scene.Id));//.onClick()
          //  sceneOpenButton.GetComponentInChildren<Text>().text = scene.Name;
     bool starred = PlayerPrefsHelper.LoadBool("scene/" + scene.Id + "/starred", false);
     }

   internal void createMenuProject(Scene scene, Project project){
 //await WebSocketManagerH.Instance.GetRobotMeta();

       GameObject newProject = Instantiate(scenePrefab, SceneList.transform);
       newProject.name = project.Name;
       newProject.GetComponentInChildren<TextMeshPro>().text = project.Name;

       GameObject ActionObjectsSpawn = new GameObject(project.Name);

      Dictionary<string, GameObject> ActionObjects = new Dictionary<string, GameObject>();

        foreach (IO.Swagger.Model.SceneObject sceneObject in scene.Objects) {
            if (!ActionsManagerH.Instance.ActionObjectsMetadata.TryGetValue(sceneObject.Type, out ActionObjectMetadataH actionObject)) {
                    continue;
            }
            if(HActionObjectPickerMenu.Instance.objectsModels.TryGetValue(actionObject.Type, out GameObject objectModel)){
                Transform obj = objectModel.transform.Find("ImportedMeshObject");
                if(obj == null){
                    obj = objectModel.GetComponentInChildren<UrdfRobot>().transform;
                }
                GameObject menuObject = Instantiate(obj.gameObject, ActionObjectsSpawn.transform);
                menuObject.transform.localScale = new Vector3(1f,1f,1f);
                menuObject.transform.parent = ActionObjectsSpawn.transform;
                menuObject.transform.localPosition = TransformConvertor.ROSToUnity(DataHelper.PositionToVector3(sceneObject.Pose.Position));
                menuObject.transform.localRotation = TransformConvertor.ROSToUnity(DataHelper.OrientationToQuaternion(sceneObject.Pose.Orientation));
                ActionObjects.Add(sceneObject.Id, menuObject);
            }
            else if (actionObject.CollisionObject) {
                
                switch (actionObject.ObjectModel.Type) {
                    case IO.Swagger.Model.ObjectModel.TypeEnum.Box:
                        GameObject cube = Instantiate(CubePrefab, ActionObjectsSpawn.transform);
                        cube.transform.parent = ActionObjectsSpawn.transform;
                        cube.transform.localPosition = TransformConvertor.ROSToUnity(DataHelper.PositionToVector3(sceneObject.Pose.Position));
                        cube.transform.localRotation = TransformConvertor.ROSToUnity(DataHelper.OrientationToQuaternion(sceneObject.Pose.Orientation));
                        cube.transform.localScale = TransformConvertor.ROSToUnityScale(new Vector3((float) actionObject.ObjectModel.Box.SizeX, (float) actionObject.ObjectModel.Box.SizeY, (float) actionObject.ObjectModel.Box.SizeZ));
                         ActionObjects.Add(sceneObject.Id, cube);
                        break;
                    case IO.Swagger.Model.ObjectModel.TypeEnum.Cylinder:
                        GameObject cylinder = Instantiate(CylinderPrefab, ActionObjectsSpawn.transform);
                        cylinder.transform.parent = ActionObjectsSpawn.transform;
                        cylinder.transform.localPosition = TransformConvertor.ROSToUnity(DataHelper.PositionToVector3(sceneObject.Pose.Position));
                        cylinder.transform.localRotation = TransformConvertor.ROSToUnity(DataHelper.OrientationToQuaternion(sceneObject.Pose.Orientation));
                        cylinder.transform.localScale = new Vector3((float) actionObject.ObjectModel.Cylinder.Radius, (float) actionObject.ObjectModel.Cylinder.Height / 2, (float) actionObject.ObjectModel.Cylinder.Radius);
                         ActionObjects.Add(sceneObject.Id, cylinder);
                        break;
                    case IO.Swagger.Model.ObjectModel.TypeEnum.Sphere:
                         GameObject sphere = Instantiate(SpherePrefab, ActionObjectsSpawn.transform);
                        sphere.transform.parent = ActionObjectsSpawn.transform;
                        sphere.transform.localPosition = TransformConvertor.ROSToUnity(DataHelper.PositionToVector3(sceneObject.Pose.Position));
                        sphere.transform.localRotation = TransformConvertor.ROSToUnity(DataHelper.OrientationToQuaternion(sceneObject.Pose.Orientation));
                        sphere.transform.localScale = new Vector3((float) actionObject.ObjectModel.Sphere.Radius, (float) actionObject.ObjectModel.Sphere.Radius, (float) actionObject.ObjectModel.Sphere.Radius);
                        ActionObjects.Add(sceneObject.Id, sphere);
                    break;
                  
                    default:
                        GameObject defaultCube = Instantiate(CubePrefab, ActionObjectsSpawn.transform);
                        defaultCube.transform.parent = ActionObjectsSpawn.transform;
                        defaultCube.transform.localPosition = TransformConvertor.ROSToUnity(DataHelper.PositionToVector3(sceneObject.Pose.Position));
                        defaultCube.transform.localRotation = TransformConvertor.ROSToUnity(DataHelper.OrientationToQuaternion(sceneObject.Pose.Orientation));
                        defaultCube.transform.localScale = new Vector3(0.05f, 0.01f, 0.05f);
                        ActionObjects.Add(sceneObject.Id, defaultCube);
                        break;
                }
            }

        }

        

    
     Dictionary<string, GameObject> actions_ID = new Dictionary<string, GameObject>();
     GameObject start = Instantiate(StartPrefab,  ActionObjectsSpawn.transform);
     start. transform.localPosition = PlayerPrefsHelper.LoadVector3("project/" + project.Id + "/" + "START", new Vector3(0, 0.15f, 0));
     actions_ID.Add("START", start );

     GameObject end = Instantiate(EndPrefab, ActionObjectsSpawn.transform);
     end. transform.localPosition = PlayerPrefsHelper.LoadVector3("project/" + project.Id + "/" + "END", new Vector3(0, 0.1f, 0));
     actions_ID.Add("END",  end );


      Dictionary<string, GameObject> ActionPoints= new Dictionary<string, GameObject>();
      
      foreach (IO.Swagger.Model.ActionPoint projectActionPoint in project.ActionPoints) {
          

            GameObject AP = Instantiate(ActionPointPrefab, ActionObjectsSpawn.transform);
            AP.transform.localScale = new Vector3(1f, 1f, 1f);
            if(projectActionPoint.Parent != null){
                if(ActionObjects.TryGetValue(projectActionPoint.Parent, out GameObject parent)){
                      AP.transform.parent = parent.transform;
                }
                else {
                     AP.transform.parent = ActionObjectsSpawn.transform;
                }
            }
            else {
                 AP.transform.parent = ActionObjectsSpawn.transform;
            }

           
            AP.transform.localPosition = TransformConvertor.ROSToUnity(DataHelper.PositionToVector3(projectActionPoint.Position));
            int i = 1;
            ActionPoints.Add(projectActionPoint.Id, AP);
            foreach(IO.Swagger.Model.Action action in  projectActionPoint.Actions){
                GameObject nAction = Instantiate(ActionPrefab, AP.transform);
                nAction.transform.localPosition = new Vector3(0, i * 0.015f + 0.015f, 0);
                ++i;
                nAction.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                actions_ID.Add(action.Id, nAction);
            }
                         
         
        }
         foreach (IO.Swagger.Model.ActionPoint projectActionPoint in project.ActionPoints) {

            if(projectActionPoint.Parent != null){
                if(ActionPoints.TryGetValue(projectActionPoint.Parent, out GameObject parent)){
                    if(ActionPoints.TryGetValue(projectActionPoint.Id, out GameObject child)){
                        child.transform.parent = parent.transform;
                        child.transform.localPosition = TransformConvertor.ROSToUnity(DataHelper.PositionToVector3(projectActionPoint.Position));
                    }
                }
            }
         }
               

           
           
    

        project_logicItems.Add(project.Name, project.Logic);
  

     Renderer[] meshes = ActionObjectsSpawn.GetComponentsInChildren<Renderer>();
     Bounds bounds = new Bounds(ActionObjectsSpawn.transform.position, Vector3.zero);
     foreach (Renderer mesh in meshes)
     {
         bounds.Encapsulate(mesh.bounds);
     }
    newProject.transform.localScale = bounds.size;
    Vector3 vec = newProject.transform.position;
    newProject.transform.position  = bounds.center;//apply the center and size
    float maxV = Mathf.Max(Mathf.Max(newProject.transform.localScale.x, newProject.transform.localScale.y), newProject.transform.localScale.z);
    newProject.transform.localScale = new Vector3(maxV, maxV, maxV);

    ActionObjectsSpawn.transform.parent = newProject.transform;
    newProject.transform.localScale /= 10;

    scenes.Add(project.Name, newProject);
    actions_scenes.Add(project.Name, actions_ID);

                newProject.GetComponent<Interactable>().OnClick.AddListener(() => openProject(project.Id));//.onClick()



   }

 
    public void waitOpenProject(object sender, EventArgs args){
        if(waitingSceneProject != null){
            GameManagerH.Instance.OpenProject(waitingSceneProject);
            waitingSceneProject = null;
            GameManagerH.Instance.OnCloseProject -= waitOpenProject;
            GameManagerH.Instance.OnCloseScene -= waitOpenProject;
        }
        
    }
   public void openProject(String projectID){
       if(GameManagerH.Instance.GetGameState().Equals( GameManagerH.GameStateEnum.ProjectEditor) || GameManagerH.Instance.GetGameState().Equals( GameManagerH.GameStateEnum.SceneEditor)){
           HHandMenuManager.Instance.onSeletectedChanged(HHandMenuManager.AllClickedEnum.Close);
           waitingSceneProject = projectID;
           GameManagerH.Instance.OnCloseProject += waitOpenProject;
           GameManagerH.Instance.OnCloseScene += waitOpenProject;

       }
       else {
            GameManagerH.Instance.OpenProject(projectID);
       }
   }

     public async void waitOpenScene(object sender, EventArgs args){
        if(waitingSceneProject != null){
           await GameManagerH.Instance.OpenScene(waitingSceneProject);
            waitingSceneProject = null;
            GameManagerH.Instance.OnCloseScene -= waitOpenScene;
             GameManagerH.Instance.OnCloseProject -= waitOpenScene;
        }
        
    }
   public async void openScene(String sceneID){
       if(GameManagerH.Instance.GetGameState().Equals( GameManagerH.GameStateEnum.ProjectEditor) || GameManagerH.Instance.GetGameState().Equals( GameManagerH.GameStateEnum.SceneEditor)){
           HHandMenuManager.Instance.onSeletectedChanged(HHandMenuManager.AllClickedEnum.Close);
           waitingSceneProject = sceneID;
           GameManagerH.Instance.OnCloseProject += waitOpenScene;

           GameManagerH.Instance.OnCloseScene += waitOpenScene;

       }
       else {
           await GameManagerH.Instance.OpenScene(sceneID);
       }
   }


    public void UpdateLogicItem(){
        foreach(KeyValuePair<string, List<IO.Swagger.Model.LogicItem>> kvp in project_logicItems){
            if(actions_scenes.TryGetValue(kvp.Key, out Dictionary<string, GameObject> action_project)){
                foreach(IO.Swagger.Model.LogicItem items in  kvp.Value){
                    if(action_project.TryGetValue(items.Start, out GameObject start)){
                        if(action_project.TryGetValue(items.End, out GameObject end)){
                            GameObject lineG = Instantiate(connectionPrefab,scenes[kvp.Key].transform.GetChild(0) );
                            Connection c = lineG.GetComponent<Connection>();
                         /*   lineG.transform.parent = scenes[kvp.Key].transform.GetChild(0);
                            LineRenderer line = lineG.AddComponent<LineRenderer>();
                            line.material = lineMaterial;
                            line.startWidth = 0.0005f;
                            line.endWidth = 0.0005f;
                            Connection c = lineG.AddComponent<Connection>();*/
                        //      c.
                            c.target[0] = start.transform.Find("Output").GetComponent<RectTransform>();
                            c.target[1] =  end.transform.Find("Input").GetComponent<RectTransform>();

                        
                            //  line.positionCount = 20;
                            /*   RectTransform rec1 = start.transform.Find("Output").GetComponent<RectTransform>();
                            RectTransform rec2 = end.transform.Find("Input").GetComponent<RectTransform>();


                            Vector3	p1 = rec1.TransformPoint(
                                        0,
                                        rec1.rect.height/2f,
                                        0);
                            Vector3	c1 = p1 + rec1.up * 0.1f;

                            Vector3	p2 = rec2.TransformPoint(
                                        0,
                                        rec2.rect.height/2f,
                                        0);
                            Vector3	c2 = p2 + rec2.up * 0.1f;
                            for (int i = 0; i < 20; i++) {
                                line.SetPosition(i, GetBezierPoint((float)i/(float)(19), p1, p2, c1, c2));
                            }*/
                        //      lineG.transform.position = GetBezierPoint(.5f,  p1, p2, c1, c2);
                            
                        }
                    }

                }
            }
        }        
    }

      public Vector3 GetBezierPoint(float t, Vector3 p1, Vector3 p2, Vector3 c1, Vector3 c2 , int derivative = 0) {
		derivative = Mathf.Clamp(derivative, 0, 2);
		float u = (1f-t);
	//	Vector3 p1 = points[0].p, p2 = points[1].p, c1 = points[0].c, c2 = points[1].c;

		if (derivative == 0) {
			return u*u*u*p1 + 3f*u*u*t*c1 + 3f*u*t*t*c2 + t*t*t*p2;

		} else if (derivative == 1) {
			return 3f*u*u*(c1-p1) + 6f*u*t*(c2-c1) + 3f*t*t*(p2-c2);

		} else if (derivative == 2) {
			return 6f*u*(c2-2f*c1+p1) + 6f*t*(p2-2f*c2+c1);

		} else {
			return Vector3.zero;
		}
	}

}
