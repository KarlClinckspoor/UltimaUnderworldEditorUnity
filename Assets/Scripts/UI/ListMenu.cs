using System;
using UnityEngine;
using UnityEngine.UI;

public class ListMenu : MovableWindow
{
	public GameObject Content;
	public Text Title;
	public Text Information;
	public GameObject OptionsPanel;
	public GameObject TogglePrefab;
	public GameObject ButtonPrefab;

	public Toggle CreateToggleOption(string text, bool interactable = true)
	{
		GameObject togGO = Instantiate(TogglePrefab, OptionsPanel.transform);
		Text textC = togGO.GetComponentInChildren<Text>();
		textC.text = text;
		textC.horizontalOverflow = HorizontalWrapMode.Overflow;
		Toggle tog = togGO.GetComponent<Toggle>();
		tog.isOn = false;
		tog.interactable = interactable;
		return tog;
	}

	public Button CreateButtonOption(string text, Action act)
	{
		GameObject butGO = Instantiate(ButtonPrefab, OptionsPanel.transform);
		Text textC = butGO.GetComponentInChildren<Text>();
		textC.text = text;
		Button but = butGO.GetComponent<Button>();
		but.onClick.AddListener(() => act());
		return but;
	}
}
