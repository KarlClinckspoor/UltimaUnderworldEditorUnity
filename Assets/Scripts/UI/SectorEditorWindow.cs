using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SectorEditorWindow : MonoBehaviour
{
	private Sector sector;
	private static GameObject sectorEditorWindowPrefab => Resources.Load<GameObject>("UI/SectorEditorPrefab");
	private static GameObject flexibleColorPickerPrefab => Resources.Load<GameObject>("UI/FlexibleColorPicker");


	public InputField NameField;
	public Button ColorButton;

	public static SectorEditorWindow Create(Sector sec, Transform parent)
	{
		GameObject sectorEditorWindowGO = Instantiate(sectorEditorWindowPrefab, parent);
		SectorEditorWindow sectorEditorWindow = sectorEditorWindowGO.GetComponent<SectorEditorWindow>();
		sectorEditorWindow.sector = sec;
		sectorEditorWindow.NameField.text = sec.Name;
		sectorEditorWindow.ColorButton.image.color = sec.SectorColor;
		return sectorEditorWindow;
	}


	public void ChangeName(string str) => sector.SetName(str);
	
	public void ChangeColor()
	{
		GameObject flexibleColorPickerGO = Instantiate(flexibleColorPickerPrefab, transform.parent);
		FlexibleColorPicker flexibleColorPicker = flexibleColorPickerGO.GetComponent<FlexibleColorPicker>();
		flexibleColorPicker.OKButton.onClick.AddListener(() => changeColor(flexibleColorPicker.GetColor()));		
	}

	private void changeColor(Color col)
	{
		sector.SetColor(col);
		ColorButton.image.color = col;
	}
}
