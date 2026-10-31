using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TileProperties : MonoBehaviour {

	public StaticObject SelectedTile { get; private set; }

	public Dropdown TileType;
	//public InputField Height;
	public Slider Height;
	public Toggle Door;
	public Toggle Antimagic;
	public Image FloorTex;
	public Image WallTex;



	/// <summary>The square last selected on the map (a tile in tile mode, or the square of an
	/// object selected in object mode) and its level; Tools windows read it. The tile under the
	/// mouse is TilePanel.LastTile.</summary>
	public static MapTile LastSelected;
	public static int LastSelectedLevel;

	Toggle lightFlag;
	GameObject lightFlagLabel;
	bool settingUp;

	/// <summary>UW2's light flag (MapTile.LightFlag): a copy of the Door toggle and its label,
	/// on the free right half of the Door row. UW1 has no use for the bit, so it is hidden there.</summary>
	void ensureLightFlagToggle()
	{
		if (lightFlag)
			return;
		Transform label = Door.transform.parent.Find("DoorT");
		if (label)
		{
			lightFlagLabel = Instantiate(label.gameObject, label.parent);
			lightFlagLabel.name = "LightFlagT";
			var lrt = (RectTransform)lightFlagLabel.transform;
			lrt.anchoredPosition += new Vector2(112f, 0f);
			var text = lightFlagLabel.GetComponentInChildren<Text>();
			if (text) text.text = "Light flag";
			UltimateEditor.Uw1.UI.UiKit.Tooltip(lightFlagLabel, "UW2: with the level's DL.DAT entry, decides whether the level's minimum light applies on this square (Tools > Level light).");
		}
		GameObject copy = Instantiate(Door.gameObject, Door.transform.parent);
		copy.name = "LightFlag";	// UIManager.ChangeTileFlag knows it by this name
		var rt = (RectTransform)copy.transform;
		rt.anchoredPosition = new Vector2(-18.5f, rt.anchoredPosition.y);
		lightFlag = copy.GetComponent<Toggle>();
		lightFlag.onValueChanged.AddListener(v =>
		{
			if (settingUp || LastSelected == null || LastSelected.LightFlag == v)
				return;
			LastSelected.LightFlag = v;
			UltimateEditor.Uw1.UI.LightWindow.Changed();
		});
	}

	public void SetTile(MapTile tile)
	{
		LastSelected = tile;
		LastSelectedLevel = tile.Level;
		UltimateEditor.Uw1.UI.LightWindow.OnTileSelected(tile);
		bool uw2 = LevArk.Game.Kind == UltimateEditor.Games.GameKind.UW2;
		if (uw2)
			ensureLightFlagToggle();
		if (lightFlag)
		{
			lightFlag.gameObject.SetActive(uw2);
			if (lightFlagLabel) lightFlagLabel.SetActive(uw2);
			settingUp = true;
			try { lightFlag.isOn = tile.LightFlag; }
			finally { settingUp = false; }
		}

		TileType.value = (int)tile.TileType;

		Height.value = tile.FloorHeight;
		Height.handleRect.GetComponentInChildren<Text>().text = tile.FloorHeight.ToString();
		Door.isOn = tile.IsDoor;
		Antimagic.isOn = tile.IsAntimagic;
		FloorTex.sprite = Sprite.Create(MapCreator.GetFloorTextureFromIndex(tile.FloorTexture, tile.Level), new Rect(0, 0, 32, 32), Vector2.zero);
		WallTex.sprite = Sprite.Create(MapCreator.GetWallTextureFromIndex(tile.WallTexture, tile.Level), new Rect(0, 0, 64, 64), Vector2.zero);
	}
}
