using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Hololens;
using Base;
using Microsoft.MixedReality.Toolkit.UI;
using UnityEngine.XR.WSA;

    public class CalibrationManagerH : Base.Singleton<CalibrationManagerH>
    {
    //public GameObject qrCodePrefab;

    private bool isEditor = false;
  

    
    [HideInInspector]
    public bool Calibrated {
        private set;
        get;
    }

    //    public GameObject scene;
    void Start()
    {
        #if UNITY_EDITOR
            GameManagerH.Instance.Scene.transform.localPosition = new Vector3(0.0f,0.0f,0.5f);
            isEditor = true;
        #endif

        GameManagerH.Instance.OnCloseScene += SetCalibration;

    }

    public void SetCalibration(object sender, EventArgs e){
        Calibrated = false;
    }

    private void Update() {
        
        if (!isEditor){
            //TO DO: doplnit neskor aj ak je scena/projekt open
            if(!Calibrated){
                GameObject qrCodePrefab = GameObject.Find("QRCode(Clone)");
                if (qrCodePrefab != null){
                     GameManagerH.Instance.SceneSetParent(qrCodePrefab.transform);
                     GameManagerH.Instance.SceneSetActive(true);
                     Calibrated = true;
                }
         }
             
        }
    }


 /*   public void StartTracking(){
        transform.GetComponent<QRCodesManager>().StartQRTracking();
    }

    public void StopTracking(){
            transform.GetComponent<QRCodesManager>().StopQRTracking();
    }*/
//        GameObject qrCodeObject = Instantiate(qrCodePrefab, new Vector3(0, 0, 0), Quaternion.identity);



}
