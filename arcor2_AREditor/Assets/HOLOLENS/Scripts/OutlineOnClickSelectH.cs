using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Hololens;
using UnityEngine.EventSystems;
using System;
using Base;
using Hololens;

public class OutlineOnClickSelectH : OutlineOnClick
{
 private bool objSelected = false;
    private bool forceSelected = false;

    private void OnEnable() {
        GameManagerH.Instance.OnSceneInteractable += OnDeselect;
    }

    private void OnDisable() {
        if (GameManagerH.Instance != null) {
            GameManagerH.Instance.OnSceneInteractable -= OnDeselect;
        }
    }

    public override void OnClick(Click type) {
        // HANDLE MOUSE
        if (type == Click.MOUSE_RIGHT_BUTTON) {
            Select();
        }
        // HANDLE TOUCH
        else if (type == Click.TOUCH) {
            Select();
        }
    }    

    private void OnDeselect(object sender, EventArgs e) {
        if (objSelected && !forceSelected) {
            objSelected = false;
            SceneManagerH.Instance.SetSelectedObject(null);
            Deselect();
        }
        forceSelected = false;
    }

    public override void Select(bool force = false) {
        forceSelected = force;
        objSelected = true;
        SceneManagerH.Instance.SetSelectedObject(gameObject);
        base.Select();
    }
}
