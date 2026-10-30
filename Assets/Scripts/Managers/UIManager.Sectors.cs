using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Linq;

/// <summary>UIManager: sectors (split from UIManager.cs by region, review package 10).</summary>
public partial class UIManager
{
	#region Sectors

	public void AddSector()
	{
		createMenuGO = Instantiate(CreateObjectPrefab, transform);
		createMenuGO.transform.Find("TopBar/Title").gameObject.GetComponent<Text>().text = "Sector name";
		GameObject mainPanelGO = createMenuGO.transform.Find("MainPanel").gameObject;
		InputField sectorNameIF = addStringInputToCreateMenu("Name", mainPanelGO.transform);
		addButtonToCreateMenu("Create", mainPanelGO.transform, () => addSector(sectorNameIF.text));
	}

	private void addSector(string name)
	{
		if (string.IsNullOrEmpty(name))
			name = "Unnamed";
		Sector sec = MapCreator.CreateNewSector(CurrentLevel - 1, name);
		if (sec == null)
			return;
		sec.OnNameChanged += s => updateSectorDropdown(CurrentLevel);
	}

	private Sector getSector()
	{
		if (SectorDropdown.options.Count == 0)
			return null;
		string name = SectorDropdown.options[SectorDropdown.value].text;
		if (string.IsNullOrEmpty(name))
			return null;
		Sector sec = MapCreator.LevelData[CurrentLevel - 1].GetSector(name);
		return sec;
	}

	public void EditSector()
	{
		Sector sec = getSector();
		if (sec == null)
			return;
		SectorEditorWindow sew = SectorEditorWindow.Create(sec, transform);
	}

	private void updateSectorDropdown(int levelNumber)
	{
		if (levelNumber == CurrentLevel)
		{
			SectorDropdown.ClearOptions();
			MapCreator.LevelData[CurrentLevel - 1].Sectors.ForEach(s => SectorDropdown.options.Add(new Dropdown.OptionData(s.Name)));			
			SectorDropdown.value = 0;			
		}
	}

	#endregion
}
