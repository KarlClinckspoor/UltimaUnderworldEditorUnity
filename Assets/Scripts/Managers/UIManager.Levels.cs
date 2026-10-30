using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Linq;

/// <summary>UIManager: levels (split from UIManager.cs by region, review package 10).</summary>
public partial class UIManager
{
	#region Levels

	// The level picker. UW1: the slider picks the level (1 to 9). UW2: a dropdown picks the
	// world (eight level slots each, (level - 1) / 8 as in the game) and the slider the
	// level inside it; the slots after the ninth world are a group of their own. The panel
	// right of the mode panel shows the level's name, notes and the button for empty slots.
	RectTransform levelPanel;
	Dropdown worldPicker;
	Text levelLabel;
	Button useSlotButton;
	bool syncingLevelPicker;

	/// <summary>UW2: a level slider's description names the level it is on ("Target level:
	/// Talorus 1 (33)"), since the bare number says little with 80 slots.</summary>
	public static void NameLevelSlider(Slider slider, string what)
	{
		if (!slider || LevArk.Game.Kind != UltimateEditor.Games.GameKind.UW2) return;
		Text descr = null;
		for (Transform t = slider.transform; t != null && descr == null; t = t.parent)
		{
			var d = t.Find("Description");
			if (d) descr = d.GetComponent<Text>();
		}
		if (!descr) return;
		Action<float> show = f => descr.text = what + ": " + LevArk.Game.LevelName(Mathf.Max(1, (int)f));
		show(slider.value);
		slider.onValueChanged.AddListener(f => show(f));
	}

	void BuildLevelPicker()
	{
		if (levelPanel)
			Destroy(levelPanel.gameObject);
		worldPicker = null;
		var game = LevArk.Game;
		bool uw2 = game.Kind == UltimateEditor.Games.GameKind.UW2;
		Slider slider = LevelSlider.GetComponent<Slider>();
		// a new range can clamp the value, which would call ChangeLevelView mid-build
		syncingLevelPicker = true;
		try
		{
			slider.wholeNumbers = true;
			slider.minValue = 1;
			slider.maxValue = uw2 ? game.LevelsPerWorld : Math.Max(1, MapCreator.GetLevelCount());
		}
		finally { syncingLevelPicker = false; }

		var parent = ModePanel ? ModePanel.transform : LevelSlider.transform.parent;
		levelPanel = UltimateEditor.Uw1.UI.UiKit.Rect("LevelPicker", parent);
		levelPanel.anchorMin = levelPanel.anchorMax = new Vector2(1, 0);
		levelPanel.pivot = new Vector2(0, 0);
		levelPanel.anchoredPosition = new Vector2(6, 4);
		levelPanel.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.55f);
		var v = levelPanel.gameObject.AddComponent<VerticalLayoutGroup>();
		v.padding = new RectOffset(5, 5, 4, 4);
		v.spacing = 3;
		v.childControlWidth = true; v.childControlHeight = true;
		v.childForceExpandWidth = false; v.childForceExpandHeight = false;
		var fit = levelPanel.gameObject.AddComponent<ContentSizeFitter>();
		fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
		fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

		if (uw2)
		{
			var options = new List<string>();
			for (int w = 0; w < game.WorldCount; w++)
				options.Add(worldOption(w));
			worldPicker = UltimateEditor.Uw1.UI.UiKit.Dropdown(levelPanel, options, 0, onWorldPicked, 250, 20);
			UltimateEditor.Uw1.UI.UiKit.Tooltip(worldPicker.gameObject, "UW2 keeps eight level slots per world. Worlds with fewer levels leave the rest of their slots empty; slots 73 to 80 belong to no world (map scraps keep their maps there).");
		}
		levelLabel = UltimateEditor.Uw1.UI.UiKit.Label(levelPanel, "", 12, FontStyle.Bold, Color.white, 250, -1);
		useSlotButton = UltimateEditor.Uw1.UI.UiKit.Button(levelPanel, "Make this slot a level", () =>
		{
			MapCreator.LevelData[CurrentLevel - 1].Present = true;
			SpawnPopupMessage(LevArk.Game.LevelName(CurrentLevel) + " will be saved as a level (with its texture block).");
			if (worldPicker)
			{
				int wi = LevArk.Game.WorldOf(CurrentLevel);
				worldPicker.options[wi].text = worldOption(wi);
				worldPicker.RefreshShownValue();
			}
			ShowLevelName();
		}, 170, 20);
	}

	/// <summary>A world's entry in the dropdown: its name and how many of its slots hold a level.</summary>
	string worldOption(int w)
	{
		var game = LevArk.Game;
		int used = 0;
		for (int p = 1; p <= game.LevelsPerWorld; p++)
		{
			int l = game.LevelAt(w, p);
			if (l <= MapCreator.LevelData.Count && MapCreator.LevelData[l - 1].Present) used++;
		}
		return string.Format("{0} ({1} of {2} used)", game.WorldName(w), used, game.LevelsPerWorld);
	}

	/// <summary>Puts the world dropdown and the slider on a level without changing the view.</summary>
	void syncLevelPicker(int level)
	{
		var game = LevArk.Game;
		Slider slider = LevelSlider.GetComponent<Slider>();
		syncingLevelPicker = true;
		try
		{
			if (worldPicker)
			{
				worldPicker.SetValueWithoutNotify(game.WorldOf(level));
				worldPicker.RefreshShownValue();
			}
			slider.SetValueWithoutNotify(game.PlaceInWorld(level));
			slider.handleRect.GetComponentInChildren<Text>().text = game.PlaceInWorld(level).ToString();
		}
		finally { syncingLevelPicker = false; }
	}

	void onWorldPicked(int world)
	{
		if (!MapCreator.IsInitialized || syncingLevelPicker)
			return;
		Slider slider = LevelSlider.GetComponent<Slider>();
		showLevel(LevArk.Game.LevelAt(world, (int)slider.value));
	}

	/// <summary>The scene's level slider calls this.</summary>
	public void ChangeLevelView(Slider slider)
	{
		if (!MapCreator.IsInitialized || syncingLevelPicker)
			return;
		int world = worldPicker ? worldPicker.value : 0;
		showLevel(LevArk.Game.LevelAt(world, (int)slider.value));
	}

	/// <summary>Shows a level (1-based) and moves the picker to it.</summary>
	public void GoToLevel(int level)
	{
		if (!MapCreator.IsInitialized)
			return;
		showLevel(level);
	}

	void showLevel(int level)
	{
		level = Mathf.Clamp(level, 1, levels.Length);
		InputManager.DeselectObject();
		ObjectProperties.SetActive(false);
		TilePropertiesObject.SetActive(false);
		levels[CurrentLevel - 1].SetActive(false);
		CurrentLevel = level;
		MapCreator.EnsureLevelBuilt(CurrentLevel);
		levels[CurrentLevel - 1].SetActive(true);
		if (CurrentLevel == 1)
			StartGO.SetActive(true);
		else
			StartGO.SetActive(false);
		changeTemplateFloorButton(MapCreator.GetFloorTextureFromIndex(0, CurrentLevel));
		changeTemplateWallButton(MapCreator.GetWallTextureFromIndex(0, CurrentLevel));
		syncLevelPicker(CurrentLevel);
		ShowLevelName();
		updateSectorDropdown(CurrentLevel);
	}

	/// <summary>The level's name in the picker panel ("Talorus 1 (33)" in UW2), the open save,
	/// what an empty or extra UW2 slot is, and the button that makes an empty slot a level.</summary>
	public void ShowLevelName()
	{
		if (!levelLabel || !MapCreator.IsInitialized)
			return;
		var game = LevArk.Game;
		var ld = MapCreator.LevelData[CurrentLevel - 1];
		bool uw2 = game.Kind == UltimateEditor.Games.GameKind.UW2;
		string text = (GameSession.IsSave ? "[" + GameSession.SaveName + "] " : "") + game.LevelName(CurrentLevel);
		if (uw2 && !ld.Present)
			text += "\nEmpty slot: not saved unless you make it a level.";
		string note = game.SlotNote(CurrentLevel);
		if (note != null)
			text += "\n" + char.ToUpper(note[0]) + note.Substring(1) + ".";
		levelLabel.text = text;
		useSlotButton.gameObject.SetActive(uw2 && !ld.Present);
	}

	public void CreateClearLevelsMenu()
	{
		createMenuGO = Instantiate(CreateObjectPrefab, transform);
		createMenuGO.transform.Find("TopBar/Title").gameObject.GetComponent<Text>().text = "Clear level data";
		GameObject mainPanelGO = createMenuGO.transform.Find("MainPanel").gameObject;

		Toggle clearConvsTog = addToggleToCreateMenu("Clear conversations", mainPanelGO.transform, true);
		Action clear = () =>
		{
			CurrentLevel = 1;
			foreach (var level in levels)
				Destroy(level);
			MapCreator.ClearLevels();
			InputManager.Commands = new CommandList<Command>(30);   // the undo steps were for the levels just cleared
			if (clearConvsTog.isOn)
				MapCreator.ClearConversations();
			levels = MapCreator.CreateLevels();
			levels[0].SetActive(true);

			BuildLevelPicker();
			syncLevelPicker(1);
			ShowLevelName();

			changeTemplateFloorButton();
			changeTemplateWallButton();

			StartGO.SetActive(true);
			Destroy(createMenuGO);
		};
		addButtonToCreateMenu("Clear", mainPanelGO.transform, clear);
	}

	public void ClearLevels()
	{

	}

	#endregion
}
