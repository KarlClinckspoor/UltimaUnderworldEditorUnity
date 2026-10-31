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

	public void SetTile(MapTile tile)
	{
		LastSelected = tile;
		LastSelectedLevel = tile.Level;
		UltimateEditor.Uw1.UI.LightWindow.OnTileSelected(tile);

		TileType.value = (int)tile.TileType;

		Height.value = tile.FloorHeight;
		Height.handleRect.GetComponentInChildren<Text>().text = tile.FloorHeight.ToString();
		Door.isOn = tile.IsDoor;
		Antimagic.isOn = tile.IsAntimagic;
		FloorTex.sprite = Sprite.Create(MapCreator.GetFloorTextureFromIndex(tile.FloorTexture, tile.Level), new Rect(0, 0, 32, 32), Vector2.zero);
		WallTex.sprite = Sprite.Create(MapCreator.GetWallTextureFromIndex(tile.WallTexture, tile.Level), new Rect(0, 0, 64, 64), Vector2.zero);
	}
}
