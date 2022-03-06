using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Base;
using Microsoft.MixedReality.Toolkit.UI;
using System.Threading.Tasks;


public class HSelectorManager : Singleton<HSelectorManager>
{


    public GameObject ObjectManupulationScreen;
    public GameObject transfomButton;
    public GameObject closeMenuButton;
    public GameObject closeDetailButton;

    private HInteractiveObject selectedObject;

    GameObject lastClicked;

    protected List<HInteractiveObject> lockedObjects = new List<HInteractiveObject>();

    public HInteractiveObject getSelecetedObject(){
        return selectedObject;
    }
    // Start is called before the first frame update
    void Start()
    {
        lastClicked = null;
        transfomButton.GetComponent<Interactable>().OnClick.AddListener(() => transformClicked());
        closeMenuButton.GetComponent<Interactable>().OnClick.AddListener(() => closeMenuClicked());
        closeDetailButton.GetComponent<Interactable>().OnClick.AddListener(() => closeDetailClicked());



    }

    public void setLastClicked(GameObject lastClicked){
        this.lastClicked = lastClicked;

    }

    public void closeMenuClicked(){
        if(lastClicked.name.Equals("Transform")){
              unlockObject();
        }
      
        setLastClicked(closeMenuButton);
    }

    public void closeDetailClicked(){
        if(lastClicked.name.Equals("Transform")){
              unlockObject();
        }
      
        setLastClicked(closeDetailButton);
    }

    public void transformClicked(){
       
        transformObject();
        setLastClicked(transfomButton);
        
    }

     public async Task<bool> LockObject(HInteractiveObject interactiveObject, bool lockTree) {
        if (await interactiveObject.WriteLock(lockTree)) {
            lockedObjects.Add(interactiveObject);
            return true;
        }
        return false;
    }

      public async Task<RequestResult> UnlockAllObjects() {
        if (GameManagerH.Instance.ConnectionStatus == GameManagerH.ConnectionStatusEnum.Disconnected) {
            lockedObjects.Clear();
            return new RequestResult(true);
        }
        for (int i = lockedObjects.Count - 1; i >= 0; --i) {
            if (lockedObjects[i].IsLockedByMe) {
                if (!await lockedObjects[i].WriteUnlock()) {
                    return new RequestResult(false, $"Failed to unlock {lockedObjects[i].GetName()}");
                }
                if (lockedObjects[i] is CollisionObjectH co) {
                    await co.WriteUnlockObjectType();
                }
                lockedObjects.RemoveAt(i);
            }
        }
        return new RequestResult(true);
    }


    public async void transformObject(){


        if (!await LockObject(getSelecetedObject(), true)) {
            return;
        }

        HTransformMenu.Instance.activeTransform(getSelecetedObject());
    }

    public async void  unlockObject(){

        if (lastClicked.name.Equals("Transform")){
           await HTransformMenu.Instance.updatePosition();
           HTransformMenu.Instance.deactiveTransform();
        }



        if (lockedObjects.Count > 0) {
            Debug.Log("UNLOCK");
            await UnlockAllObjects();
        }
    //  await  HTransformMenu.Instance.

    }

 /*   // Update is called once per frame
    void Update()
    {
        
    }
*/

    public void OnSelectObject(HInteractiveObject selectedObject){
        lastClicked = null;
        this.selectedObject = selectedObject;
        ObjectManupulationScreen.transform.parent = this.selectedObject.transform;
        ObjectManupulationScreen.transform.localPosition = new Vector3(0,1,0);
        ObjectManupulationScreen.SetActive(true);
    }
}
