using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Michsky.UI.ModernUIPack;
using UnityEngine.UI;

public class Dialog : MonoBehaviour
{
    public ModalWindowManager WindowManager;

    public virtual void Start() {
        WindowManager = GetComponent<ModalWindowManager>();
    }

    protected virtual void UpdateToggleGroup(GameObject togglePrefab, GameObject toggleGroup, List<IO.Swagger.Model.IdDesc> idDescs) {
        List<string> items = new List<string>();
        foreach (IO.Swagger.Model.IdDesc idDesc in idDescs) {
            items.Add(idDesc.Name);
        }
        UpdateToggleGroup(togglePrefab, toggleGroup, items);
    }

    protected virtual void UpdateToggleGroup(GameObject togglePrefab, GameObject toggleGroup, List<IO.Swagger.Model.ListProjectsResponseData> projects) {
        List<string> items = new List<string>();
        foreach (IO.Swagger.Model.ListProjectsResponseData project in projects) {
            items.Add(project.Id);
        }
        UpdateToggleGroup(togglePrefab, toggleGroup, items);
    }

    protected virtual void UpdateToggleGroup(GameObject togglePrefab, GameObject toggleGroup, List<string> items) {
        foreach (Transform toggle in toggleGroup.transform) {
            Destroy(toggle.gameObject);
        }
        foreach (string item in items) {

            GameObject toggle = Instantiate(togglePrefab, toggleGroup.transform);
            foreach (TMPro.TextMeshProUGUI text in toggle.GetComponentsInChildren<TMPro.TextMeshProUGUI>()) {
                text.text = item;
            }
            toggle.GetComponent<Toggle>().group = toggleGroup.GetComponent<ToggleGroup>();
            toggle.transform.SetAsFirstSibling();
        }
    }

    protected virtual string GetSelectedValue(GameObject toggleGroup) {
        foreach (Toggle toggle in toggleGroup.GetComponentsInChildren<Toggle>()) {
            if (toggle.isOn) {
                return toggle.GetComponentInChildren<TMPro.TextMeshProUGUI>().text;
            }
        }
        throw new Base.ItemNotFoundException("Nothing selected");
    }
}
