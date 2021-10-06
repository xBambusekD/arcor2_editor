using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Base;
using UnityEngine;

[RequireComponent(typeof(OutlineOnClick))]
public class ConnectionLine : InteractiveObject {
    public string LogicItemId = "", Name = "connection";

    public OnClickCollider OnClickCollider;
    public OutlineOnClick OutlineOnClick;
    public MeshCollider MeshCollider;
    public RectTransform[] Target = new RectTransform[2];
    public GameObject Cone;

    public void InitConnection(string logicItemId, string name) {
        LogicItemId = logicItemId;
        Name = name;

    }



    private void Awake() {
        //OutlineOnClick = GetComponent<OutlineOnClick>();
        //OnClickCollider.Target = gameObject;
        //lineRenderer = gameObject.GetComponent<LineRenderer>();
        //meshCollider = gameObject.GetComponent<MeshCollider>();
        //lineRenderer.rayTracingMode = UnityEngine.Experimental.Rendering.RayTracingMode.DynamicGeometry;
    }

    public void SetTargets(RectTransform transform1, RectTransform transform2) {
        Target[0] = transform1;
        Target[1] = transform2;
    }

    private void FixedUpdate() {
        
    }

    private void Start() {
        
    }

    public void UpdateConnection() {
        if (Target[0] != null && Target[1] != null) {
            transform.position = Target[0].position;
            if ((Target[1].position - Target[0].position).magnitude > 0.0001)
                transform.rotation = Quaternion.LookRotation(Target[1].position - Target[0].position);
            transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y, (Target[1].position - Target[0].position).magnitude);
        }  
    }


    public override string GetId() {
        return LogicItemId;
    }

    public override string GetName() {
        return Name;
    }

    public override bool HasMenu() {
        return false;
    }

    public override async Task<RequestResult> Movable() {
        return new RequestResult(false);
    }

    public override void OnClick(Click type) {
        throw new System.NotImplementedException();
    }

    public override void OnHoverEnd() {
        OutlineOnClick.UnHighlight();
    }

    public override void OnHoverStart() {
        
        OutlineOnClick.Highlight();
    }

    public override void OpenMenu() {
        throw new System.NotImplementedException();
    }

    public override async Task<RequestResult> Removable() {
        return new RequestResult(false);
    }

    public async override void Remove() {
        await Base.WebsocketManager.Instance.RemoveLogicItem(LogicItemId);
    }


    public override void StartManipulation() {
        throw new System.NotImplementedException();
    }


    private void OnDestroy() {
        
    }

    public override string GetObjectTypeName() {
        return "Connection line";
    }

    public override void CloseMenu() {
        throw new System.NotImplementedException();
    }

    public override void UpdateColor() {
        
    }

    public override Task Rename(string name) {
        throw new System.NotImplementedException();
    }

    public override void EnableVisual(bool enable) {
        
    }
}
