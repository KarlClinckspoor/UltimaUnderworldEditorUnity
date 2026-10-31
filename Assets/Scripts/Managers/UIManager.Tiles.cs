using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Linq;

/// <summary>UIManager: tiles (split from UIManager.cs by region, review package 10).</summary>
public partial class UIManager
{
	#region Tiles

	public void ChangeTileType(Dropdown dropdown)
	{
		if (!InputManager.SelectedObject)	//Dislike
			return;
		if (!EventSystem.current.currentSelectedGameObject)
			return;

		MapTileScript mts = InputManager.SelectedObject.GetComponent<MapTileScript>();
		MapTile tile = mts.MapTile;
		tile.TileType = (TileType)dropdown.value;
		MapCreator.UpdateTileAndNeighbours(CurrentLevel, mts, false);
		UltimateEditor.Uw1.UI.ZoneTools.TileEdited(mts.MapTile);
	}

	public void ChangeTemplateType(Dropdown dropdown)
	{
		templateType = (TileType)dropdown.value;
	}
	public void ChangeTemplateHeight(Slider slider)
	{
		templateHeight = (int)slider.value;
		slider.handleRect.GetComponentInChildren<Text>().text = templateHeight.ToString();
	}

	public void ChangeTileFlag(Toggle tog)
	{
		if (!InputManager.SelectedObject)
			return;
		if (!EventSystem.current.currentSelectedGameObject)
			return;

		MapTile tile = InputManager.SelectedObject.GetComponent<MapTileScript>().MapTile;
		if (tog.gameObject.name == "Door")
			tile.IsDoor = tog.isOn;
		else if (tog.gameObject.name == "Antimagic")
			tile.IsAntimagic = tog.isOn;
		else if (tog.gameObject.name == "LightFlag")
		{
			tile.LightFlag = tog.isOn;
			UltimateEditor.Uw1.UI.LightWindow.Changed();
		}
	}

	public void ChangeTileProperty(InputField input)
	{
		if (!InputManager.SelectedObject)
			return;
		if (!EventSystem.current.currentSelectedGameObject)
			return;

		MapTileScript mts = InputManager.SelectedObject.GetComponent<MapTileScript>();
		MapTile tile = mts.MapTile;
		int value = 0;
		bool result = int.TryParse(input.text, out value);
		if (!result)
		{
			SpawnPopupMessage("Invalid value at " + input.name);
			return;
		}
		if (input.gameObject.name == "Height")
		{
			setTileHeight(mts, Mathf.Clamp(value, 0, 15));
		}
	}
	public void ChangeTileProperty(Slider slider)
	{
		if (!InputManager.SelectedObject)
			return;
		if (!EventSystem.current.currentSelectedGameObject)
			return;

		MapTileScript mts = InputManager.SelectedObject.GetComponent<MapTileScript>();
		MapTile tile = mts.MapTile;

		int value = (int)slider.value;
		slider.handleRect.GetComponentInChildren<Text>().text = value.ToString();
		if (slider.gameObject.name == "Height")
		{
			setTileHeight(mts, value);
		}
	}

	/// <summary>
	/// This function is for TileMode 'painting' tiles
	/// </summary>	
	public void ChangeTileType(GameObject tileObj, TileType newType)
	{
		MapTileScript mts = tileObj.GetComponent<MapTileScript>();
		MapTile tile = mts.MapTile;
		if (tile)
		{
			// painting runs every frame the button is held: a tile already like the template is left alone
			if (tile.TileType == templateType && tile.FloorHeight == templateHeight && tile.FloorTexture == templateFloor && tile.WallTexture == templateWall)
				return;
			tile.TileType = templateType;
			tile.FloorTexture = templateFloor;
			tile.WallTexture = templateWall;
			if (tile.FloorHeight != templateHeight)
				setTileHeight(mts, templateHeight);   // moves the tile's objects with the floor
			MapCreator.UpdateTileAndNeighbours(CurrentLevel, mts, false);
		UltimateEditor.Uw1.UI.ZoneTools.TileEdited(mts.MapTile);

			if (InputManager.SelectedObject)
			{
				MapTileScript otherMts = InputManager.SelectedObject.GetComponent<MapTileScript>();
				MapTile other = otherMts.MapTile;
				if (other == tile)
					TilePropertiesObject.GetComponent<TileProperties>().SetTile(tile);
			}
		}
	}
	public void ChangeTileTexture(GameObject tileObj)
	{
		MapTileScript mts = tileObj.GetComponent<MapTileScript>();
		MapTile tile = mts.MapTile;
		if (tile)
		{
			bool floor = TextureTemplateFloorToggle.isOn && tile.FloorTexture != templateFloor;
			bool wall = TextureTemplateWallToggle.isOn && tile.WallTexture != templateWall;
			if (!floor && !wall)
				return;   // painting runs every frame the button is held
			if (floor)
				tile.FloorTexture = templateFloor;
			if (wall)
				tile.WallTexture = templateWall;
			MapCreator.UpdateTileAndNeighbours(CurrentLevel, mts, false);
		UltimateEditor.Uw1.UI.ZoneTools.TileEdited(mts.MapTile);
			if (InputManager.SelectedObject)
			{
				MapTileScript otherMts = InputManager.SelectedObject.GetComponent<MapTileScript>();
				MapTile other = otherMts.MapTile;
				if (other == tile)
					TilePropertiesObject.GetComponent<TileProperties>().SetTile(tile);
			}
		}
	}

	public void ChangeTileSector(GameObject tileObj, bool erase = false)
	{
		MapTileScript mts = tileObj.GetComponent<MapTileScript>();
		MapTile tile = mts ? mts.MapTile : null;
		if (!tile)
			return;
		// Zoning (Tools > Zones): left button paints the selected zone, right button unassigns.
		if (UltimateEditor.Uw1.UI.ZoneTools.PaintEnabled && UltimateEditor.Uw1.UI.ZoneTools.Zones != null)
		{
			UltimateEditor.Uw1.UI.ZoneTools.Paint(tile, erase);
			return;
		}
		Sector sec = getSector();
		if (sec == null)
			return;
		mts.SetOverlay(true);
		mts.SetOverlayColor(sec.SectorColor);
	}

	/// <summary>The one way a tile's floor height changes (the wheel, the properties panel, the
	/// template brush): its objects move with the floor. One standing on the floor stays on it;
	/// a raised one (a lever, a bridge, something on a wall) keeps its height above it; traps
	/// and triggers keep their height, which is data. The wheel flattened every object to the
	/// floor and the panel moved only doors.</summary>
	void setTileHeight(MapTileScript mts, int height)
	{
		MapTile tile = mts.MapTile;
		height = Mathf.Clamp(height, 0, 15);
		int delta = height - tile.FloorHeight;
		if (delta == 0)
			return;
		var objects = tile.GetTileObjects(so => !MapCreator.IsTrapOrTrigger(so)) ?? new List<StaticObject>();
		var onFloor = new HashSet<StaticObject>();
		foreach (var so in objects)
			if (so.ZPos == MapCreator.FloorZ(so, tile)) onFloor.Add(so);
		tile.FloorHeight = height;
		foreach (var so in objects)
		{
			so.ZPos = onFloor.Contains(so) ? MapCreator.FloorZ(so, tile) : Mathf.Clamp(so.ZPos + delta * 8, 0, 127);
			MapCreator.SetGOHeight(so);
			if (so.IsDoor())
				MapCreator.SetDoorGOPosition(so);
		}
		MapCreator.UpdateTileAndNeighbours(CurrentLevel, mts, false);
		UltimateEditor.Uw1.UI.ZoneTools.TileEdited(tile);
	}

	public void ChangeTileHeight(GameObject tileObj, float val)
	{
		MapTileScript mts = tileObj.GetComponent<MapTileScript>();
		MapTile tile = mts.MapTile;
		int height = tile.FloorHeight;
		if (val > 0)
			height++;
		else if (val < 0)
			height--;
		height = Mathf.Clamp(height, 0, 15);
		if (height == tile.FloorHeight)
			return;
		setTileHeight(mts, height);

	
		if (InputManager.SelectedObject)
		{
			MapTileScript otherMts = InputManager.SelectedObject.GetComponent<MapTileScript>();
			MapTile other = otherMts.MapTile;
			if (other == tile)
				TilePropertiesObject.GetComponent<TileProperties>().SetTile(tile);
		}
	}

	#endregion
}
