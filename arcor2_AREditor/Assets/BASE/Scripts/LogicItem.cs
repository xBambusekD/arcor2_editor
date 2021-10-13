using System;
using Base;
using UnityEngine;

public class LogicItem 
{
    public IO.Swagger.Model.LogicItem Data;

    private ConnectionLine connection;

    private InputOutput input;
    private PuckOutput output;

    public InputOutput Input {
        get => input;
        set => input = value;
    }
    public PuckOutput Output {
        get => output;
        set => output = value;
    }

    public LogicItem(IO.Swagger.Model.LogicItem logicItem) {
        Data = logicItem;
        UpdateConnection(logicItem);
    }

    public void Remove() {
        input.RemoveLogicItem(Data.Id);
        output.RemoveLogicItem(Data.Id);
        ConnectionManagerArcoro.Instance.DestroyConnection(connection);
        connection = null;
    }

    public void UpdateConnection(IO.Swagger.Model.LogicItem logicItem) {
        if (connection != null) {
            Remove();
        }
        input = ProjectManager.Instance.GetAction(logicItem.End).Input;
        output = ProjectManager.Instance.GetAction(logicItem.Start).Output;
        input.AddLogicItem(Data.Id);
        output.AddLogicItem(Data.Id);        
        connection = ConnectionManagerArcoro.Instance.CreateConnection(input.gameObject, output.gameObject);
        output.Action.UpdateRotation(input.transform);
        connection.InitConnection(Data.Id, Output.Action.GetName() + " => " + Input.Action.GetName());
        if (Input.LineToConnection != null)
            GameObject.Destroy(Input.LineToConnection.gameObject);
        if (Output.LineToConnection != null)
            GameObject.Destroy(Output.LineToConnection.gameObject);
        Input.LineToConnection = GameObject.Instantiate(ConnectionManagerArcoro.Instance.ConnectionPrefab, Input.Action.transform).GetComponent<ConnectionLine>();
        foreach (Collider c in Input.LineToConnection.Colliders)
            c.enabled = false;
        Input.LineToConnection.SetTargets(Input.transform.GetComponent<RectTransform>(), Input.Action.Rear);
        Output.LineToConnection = GameObject.Instantiate(ConnectionManagerArcoro.Instance.ConnectionPrefab, Output.Action.transform).GetComponent<ConnectionLine>();
        foreach (Collider c in Output.LineToConnection.Colliders)
            c.enabled = false;
        Output.LineToConnection.SetTargets(Output.Action.Front, Output.transform.GetComponent<RectTransform>());
    }

    public ConnectionLine GetConnection() {
        return connection;
    }

}
