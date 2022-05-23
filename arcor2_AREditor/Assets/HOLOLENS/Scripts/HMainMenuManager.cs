using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Hololens;
using Base;
using UnityEngine.UI;
using Newtonsoft.Json;
using IO.Swagger.Model;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.MixedReality.Toolkit.UI;



public class HMainMenuManager : Singleton<HMainMenuManager>
{

    public GameObject mainMenuScreen;
    public GameObject scenesScreen;
    public GameObject projectsScreen;
    public Interactable showScenesButton;

    public Interactable showProjectsButton;

    public Interactable showNotificationsButton;

    public List<GameObject> SceneTiles => sceneTiles;

    public List<GameObject> ProjectTiles => projectTiles;

    private List<GameObject> sceneTiles = new List<GameObject>();
    private List<GameObject> projectTiles = new List<GameObject>();
    public GameObject sceneOpenPrefabButton;
    public GameObject projectOpenPrefabButton;
    public GameObject sceneParent;
    public GameObject projectParent;

    private bool scenesLoaded, projectsLoaded, packagesLoaded, scenesUpdating, projectsUpdating, packagesUpdating;
    // Start is called before the first frame update
    private void Awake() {
        scenesLoaded = projectsLoaded = scenesUpdating = projectsUpdating = packagesLoaded = packagesUpdating = false;
    }
    void Start()
    {
        mainMenuScreen.SetActive(false);
        scenesScreen.SetActive(false);
      //  GameManagerH.Instance.OnOpenSceneEditor += OnShowEditorScreen;
       // GameManagerH.Instance.OnOpenProjectEditor += OnShowEditorScreen;
      //  GameManagerH.Instance.OnConnectedToServer += ConnectedToServer;
      //  GameManagerH.Instance.OnScenesListChanged += UpdateScenes;
       // GameManagerH.Instance.OnProjectsListChanged += UpdateProjects;
        showScenesButton.OnClick.AddListener(() => OpenScenes());
        showProjectsButton.OnClick.AddListener(() => OpenProjects());
        showNotificationsButton.OnClick.AddListener(() => HNotificationManager.Instance.ShowNotificationScreen());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnShowEditorScreen(object sender, EventArgs args){
        HideMainMenuScreen();
    }


    public void HideMainMenuScreen(){
         mainMenuScreen.SetActive(false);
         scenesScreen.SetActive(false);
         projectsScreen.SetActive(false);
    }

    public void ShowMainMenuScreen(){
         mainMenuScreen.SetActive(true);
    }

    private void ConnectedToServer(object sender, EventArgs args) {
     //   ShowMainMenuScreen();
    }

    public void LoadScenesCb(string id, string responseData) {
        IO.Swagger.Model.ListScenesResponse response = JsonConvert.DeserializeObject<IO.Swagger.Model.ListScenesResponse>(responseData);
        
        if (response == null || !response.Result) {
             HNotificationManager.Instance.ShowNotification("Failed to load scenes");
            scenesUpdating = false;
            return;
        }
        GameManagerH.Instance.Scenes = response.Data;
        GameManagerH.Instance.Scenes.Sort(delegate (ListScenesResponseData x, ListScenesResponseData y) {
            return y.Modified.CompareTo(x.Modified);
        });
        scenesUpdating = false;
        scenesLoaded = true;
        GameManagerH.Instance.InvokeScenesListChanged();
    }

    
    public void LoadProjectsCb(string id, string responseData) {
        IO.Swagger.Model.ListProjectsResponse response = JsonConvert.DeserializeObject<IO.Swagger.Model.ListProjectsResponse>(responseData);
        if (response == null) {
            HNotificationManager.Instance.ShowNotification("Failed to load projects");
            return;
        }
        GameManagerH.Instance.Projects = response.Data;
        GameManagerH.Instance.Projects.Sort(delegate (ListProjectsResponseData x, ListProjectsResponseData y) {
            return y.Modified.CompareTo(x.Modified);
        });
        projectsUpdating = false;
        projectsLoaded = true;
        GameManagerH.Instance.InvokeProjectsListChanged();
    }

    public async void OpenScenes() {

         SceneTiles.Clear();

        if (!scenesUpdating) {
            scenesUpdating = true;
            scenesLoaded = false;
            WebSocketManagerH.Instance.LoadScenes(LoadScenesCb);
        }
        try {
            await WaitUntilScenesLoaded();

           // scenesScreen.SetActive(true);

     //       AddNewBtn.SetDescription("Add scene");

        } catch (TimeoutException ex) {            
            HNotificationManager.Instance.ShowNotification("Failed to switch to scenes");
        } finally {
          //  GameManager.Instance.HideLoadingScreen(true);
        }
    }

    public async void OpenProjects() {
      /*  if (!scenesUpdating) {
            scenesUpdating = true;
            scenesLoaded = false;
            WebSocketManagerH.Instance.LoadScenes(LoadScenesCb);
        }*/
        try {
           // await WaitUntilScenesLoaded();
            if (!projectsUpdating) {
                projectsUpdating = true;
                projectsLoaded = false;
                WebSocketManagerH.Instance.LoadProjects(LoadProjectsCb);
            }
            await WaitUntilProjectsLoaded();
        
          //  projectsScreen.SetActive(true);
           
        } catch (TimeoutException ex) {
            HNotificationManager.Instance.ShowNotification("Failed to switch to projects");
        } finally {
           // GameManager.Instance.HideLoadingScreen(true);
        }
        
    }

    private async Task WaitUntilScenesLoaded() {
        await Task.Run(() => {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            while (true) {
                if (sw.ElapsedMilliseconds > 5000)
                    throw new TimeoutException("Failed to load scenes");
                if (scenesLoaded) {
                    return true;
                } else {
                    Thread.Sleep(10);
                }
            }
        });
    }

    private async Task WaitUntilProjectsLoaded() {
        await Task.Run(() => {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            while (true) {
                if (sw.ElapsedMilliseconds > 5000)
                    throw new TimeoutException("Failed to load projects");
                if (projectsLoaded) {
                    return true;
                } else {
                    Thread.Sleep(10);
                }
            }
        });

    }

    public void UpdateProjects(object sender, EventArgs eventArgs) {
        ProjectTiles.Clear();

        foreach(Transform t in projectParent.transform){
            if (t.gameObject.GetComponentInChildren<Text>().text == "temp") continue;
            else {
                Destroy(t.gameObject);
            }
        }

        foreach (IO.Swagger.Model.ListProjectsResponseData project in GameManagerH.Instance.Projects) {
            GameObject projectOpenButton = Instantiate(projectOpenPrefabButton);
            bool starred = PlayerPrefsHelper.LoadBool("project/" + project.Id + "/starred", false);
            if (project.Problems == null) {                
             
            projectOpenButton.GetComponent<Button>().onClick.AddListener(() => GameManagerH.Instance.OpenProject(project.Id));//.onClick()
            projectOpenButton.GetComponentInChildren<Text>().text = project.Name;
            projectOpenButton.transform.SetParent(projectParent.transform, false);
            projectOpenButton.SetActive(true);
                    string sceneName = GameManagerH.Instance.GetSceneName(project.SceneId);
            } else {
                string sceneName = "unknown";
                try {
                    sceneName = GameManagerH.Instance.GetSceneName(project.SceneId);
                } catch (ItemNotFoundException) { }
           //     tile.InitInvalidProject(project.Id, project.Name, project.Created, project.Modified, starred, project.Problems.FirstOrDefault(), sceneName);
            }
            ProjectTiles.Add(projectOpenButton);
        }
        // Button button = Instantiate(TileNewPrefab, ProjectsDynamicContent.transform).GetComponent<Button>();
        // TODO new scene
        // button.onClick.AddListener(() => NewProjectDialog.Open());
    }
    public void UpdateScenes(object sender, EventArgs eventArgs) {

        sceneTiles.Clear();

        foreach(Transform t in sceneParent.transform){
            if (t.gameObject.GetComponentInChildren<Text>().text == "temp") continue;
            else {
                Destroy(t.gameObject);
            }
        }
        foreach (IO.Swagger.Model.ListScenesResponseData scene in GameManagerH.Instance.Scenes) {
            Debug.Log("--- Scene --- /n" + scene);
            GameObject sceneOpenButton = Instantiate(sceneOpenPrefabButton);
           
            sceneOpenButton.GetComponent<Button>().onClick.AddListener(() => GameManagerH.Instance.OpenScene(scene.Id));//.onClick()
            sceneOpenButton.GetComponentInChildren<Text>().text = scene.Name;
            bool starred = PlayerPrefsHelper.LoadBool("scene/" + scene.Id + "/starred", false);
            sceneOpenButton.transform.SetParent(sceneParent.transform, false);
            sceneOpenButton.SetActive(true);
            SceneTiles.Add(sceneOpenButton);
        }
    }
       

}
