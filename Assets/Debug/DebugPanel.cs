using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DebugPanel : MonoBehaviour
{
    public Toggle inspectorPanel;
    public GameObject runtimeInspector;
    public GameObject runtimeHierarchy;

    public void ShowInspectorPanel()
    {
        runtimeInspector.SetActive(inspectorPanel.isOn);
        runtimeHierarchy.SetActive(inspectorPanel.isOn);
    }
}
