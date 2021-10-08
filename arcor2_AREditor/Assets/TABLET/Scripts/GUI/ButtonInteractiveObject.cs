using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using Base;

public class ButtonInteractiveObject : InteractiveObject {

    public string Name;
    public UnityEvent Callback;
    public GameObject Border;

    public override void CloseMenu() {
        throw new NotImplementedException();
    }

    public override void EnableVisual(bool enable) {
    }

    public override string GetId() {
        return Name;
    }

    public override string GetName() {
        return Name;
    }

    public override string GetObjectTypeName() {
        return "Button";
    }

    public override bool HasMenu() {
        return false;
    }

    public async override Task<RequestResult> Movable() {
        return new RequestResult(false);
    }

    public override void OnClick(Click type) {
        Callback.Invoke();
    }

    public override void OnHoverEnd() {

        Border.SetActive(false);
    }

    public override void OnHoverStart() {
        Border.SetActive(true);
    }

    public override void OpenMenu() {
        throw new NotImplementedException();
    }

    public async override Task<RequestResult> Removable() {
        return new RequestResult(false);
    }

    public override void Remove() {
        throw new NotImplementedException();
    }

    public override Task Rename(string newName) {
        throw new NotImplementedException();
    }

    public override void StartManipulation() {
        throw new NotImplementedException();
    }

    public override void UpdateColor() {
        throw new NotImplementedException();
    }
}
