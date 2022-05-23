using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.UI;
using Base;

public class HDeleteActionManager : Singleton<HDeleteActionManager>
{

   // public Interactable inputButton;
    public Interactable outputButton;
    public Interactable actionButton;

    private HAction action;
    // Start is called before the first frame update
    void Start()
    {
        actionButton.OnClick.AddListener(() => HSelectorManager.Instance.deleteObject());
        outputButton.OnClick.AddListener(async () => {await  WebSocketManagerH.Instance.RemoveLogicItem(action.Output.GetLogicItems()[0].Data.Id);
                                                              HSelectorManager.Instance.deleteActionManager.SetActive(false);  });

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Show(HAction action){
        this.action = action;
        resetButtons();
     

    }

    public void resetButtons(){
      //  inputButton.gameObject.SetActive(false);
        outputButton.gameObject.SetActive(true);
        actionButton.gameObject.SetActive(true);
    }

    public void setActiveActionButton(bool active){
        actionButton.gameObject.SetActive(active);

    }

  /*   public void setActiveInputButton(bool active){
        inputButton.gameObject.SetActive(active);

    }*/

 public void setActiveOutputButton(bool active){
        outputButton.gameObject.SetActive(active);

    }


  
}
