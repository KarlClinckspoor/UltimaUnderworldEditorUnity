using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DebugPanel : MonoBehaviour {

	public Text Text1;
	public Text Text2;

	private UIManager uiManager;
	private PropertiesPanel pp;

	void Start () {
		uiManager = GameObject.FindGameObjectWithTag("UIManager").GetComponent<UIManager>();
		pp = uiManager.ObjectProperties.GetComponent<PropertiesPanel>();
	}
	
	void Update () {
		if (pp.SelectedObject)
			Text1.text = pp.SelectedObject.Name;
		else
			Text1.text = " - ";
		if (uiManager.InputManager.SelectedObject)
			Text2.text = uiManager.InputManager.SelectedObject.name;
		else
			Text2.text = " - ";
	}
}
