using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Linq;

/// <summary>UIManager: textures (split from UIManager.cs by region, review package 10).</summary>
public partial class UIManager
{
	#region Textures
	
	public void CreateTileFloorExplorer()
	{
		CreateTextureExplorer(TextureExplorerType.Picker, TextureType.Floor, "Textures", "Select floor texture", "", "");
	}
	public void CreateTileWallExplorer()
	{
		CreateTextureExplorer(TextureExplorerType.Picker, TextureType.Wall, "Textures", "Select wall texture", "", "");
	}
	public void CreateTemplateFloorExplorer()
	{
		Action<int> getTex = (i) =>
		{
			templateFloor = i;
			Texture2D tex = MapCreator.GetFloorTextureFromIndex(i, CurrentLevel);
			changeTemplateFloorButton(tex);
			Destroy(TexturePicker);
		};
		CreateTexturePicker(GetCurrentLevelTextures(TextureType.Floor, false), getTex, "Textures", "Select floor texture");
	}
	private void changeTemplateFloorButton() => changeTemplateFloorButton(MapCreator.GetFloorTextureFromIndex(templateFloor, CurrentLevel));
	private void changeTemplateFloorButton(Texture2D tex)
	{
		TemplateFloorImage.sprite = Sprite.Create(tex, new Rect(0, 0, 32.0f, 32.0f), Vector2.zero);
		TextureTemplateFloorImage.sprite = Sprite.Create(tex, new Rect(0, 0, 32.0f, 32.0f), Vector2.zero);
	}
	public void CreateTemplateWallExplorer()
	{
		Action<int> getTex = (i) =>
		{
			templateWall = i;
			Texture2D tex = MapCreator.GetWallTextureFromIndex(i, CurrentLevel);
			changeTemplateWallButton(tex);
			Destroy(TexturePicker);
		};
		CreateTexturePicker(GetCurrentLevelTextures(TextureType.Wall, false), getTex, "Textures", "Select wall texture");
	}
	private void changeTemplateWallButton() => changeTemplateWallButton(MapCreator.GetWallTextureFromIndex(templateWall, CurrentLevel));
	private void changeTemplateWallButton(Texture2D tex)
	{
		TemplateWallImage.sprite = Sprite.Create(tex, new Rect(0, 0, 64.0f, 64.0f), Vector2.zero);
		TextureTemplateWallImage.sprite = Sprite.Create(tex, new Rect(0, 0, 64.0f, 64.0f), Vector2.zero);
	}
	private void updateTemplateButtons(TextureType texType)
	{
		if (texType == TextureType.Floor)
			changeTemplateFloorButton();
		else if (texType == TextureType.Wall)
			changeTemplateWallButton();
	}

	public void CreateTextureExplorer(TextureExplorerType expType, TextureType texType, string firstTitle, string firstDescr, string secondTitle, string secondDescr)
	{
		if (!MapCreator.IsInitialized)
			return;
		if (textureExplorer)
			Destroy(textureExplorer);

		textureExplorer = Instantiate(TextureExplorerPrefab, transform);
		TextureExplorer texExp = textureExplorer.GetComponent<TextureExplorer>();
		if(expType == TextureExplorerType.Level)
		{
			List<Texture2D> list = GetCurrentLevelTextures(texType, false);			
			texExp.Title.text = firstTitle;
			textureExplorer.transform.Find("Description").GetComponent<Text>().text = firstDescr;
			if (texType == TextureType.Door)
			{
				//texExp.Title.text += " Last door is always massive.";
				GridLayoutGroup glg = texExp.Content.GetComponent<GridLayoutGroup>();
				glg.cellSize = new Vector2(64.0f, 128.0f);
			}
			for (int i = 0; i < list.Count; i++)
			{
				int new_i = i;
				GameObject texItem = null;
				Action<int> act = (x) =>
				{
					MapCreator.SwapTexture(texType, CurrentLevel, new_i, x);
					Texture2D newTex = MapCreator.GetTexture(texType, MapCreator.GetLevelTextureIndex(texType, CurrentLevel, new_i));
					texItem.GetComponent<Image>().sprite = Sprite.Create(newTex, new Rect(0, 0, newTex.width, newTex.height), Vector2.zero);
					texItem.GetComponentInChildren<Text>().text = MapCreator.GetLevelTextureIndex(texType, CurrentLevel, new_i).ToString();
					updateTemplateButtons(texType);
					Destroy(TexturePicker);
					//Destroy(textureExplorer);
				};
				//texItem = CreateTextureItem(list[i], MapCreator.GetLevelTextureIndex(texType, CurrentLevel, i), texExp.Content.transform, (x) => CreateTexturePicker(texType, false, act, CreateLoadedTextureExplorer));
				texItem = CreateTextureItem(list[i], MapCreator.GetLevelTextureIndex(texType, CurrentLevel, i), texExp.Content.transform, (x) => CreateTexturePicker(GetTextures(texType, false), act, secondTitle, secondDescr));
			}
		}
		else if(expType == TextureExplorerType.Loaded)
		{
			texExp.Title.text = firstTitle;
			textureExplorer.transform.Find("Description").GetComponent<Text>().text = firstDescr;
			List<Texture2D> list = GetTextures(texType, false);
			Vector2 size = default;
			Func<int, string> changeItemDescr = null;
			int fontSize = 0;

			if (texType == TextureType.Door)
			{
				GridLayoutGroup glg = texExp.Content.GetComponent<GridLayoutGroup>();
				size = new Vector2(64.0f, 128.0f);
				glg.cellSize = size;
			}
			else if (texType == TextureType.GenericHead)
			{
				changeItemDescr = (i) => StaticObject.GetMonsterName(i);
				fontSize = 10;
			}
			else if (texType == TextureType.NPCHead)
				changeItemDescr = (i) => (i + 1).ToString();

			Action<GameObject, Texture2D> changeSpriteAct = (go, newTex) =>
			{
				if(go)
					go.GetComponent<Image>().sprite = Sprite.Create(newTex, new Rect(0, 0, newTex.width, newTex.height), Vector2.zero);
			};
			for (int i = 0; i < list.Count; i++)
			{
				int new_i = i;
				GameObject texItem = null;
				//texItem = CreateTextureItem(list[i], i, texExp.Content.transform, (x) => CreateTexturePicker(texType, true, (y) => LoadTextureAction(texType, y, new_i, texItem), CreateLoadedTextureExplorer));
				Action<int> loadTexAct = (index) =>
				{
					LoadTextureAction(texType, index, new_i, (newTex) => changeSpriteAct(texItem, newTex));
					updateTemplateButtons(texType);
				};
				//texItem = CreateTextureItem(list[i], i, texExp.Content.transform, (x) => CreateTexturePicker(GetTextures(texType, true), (y) => LoadTextureAction(texType, y, new_i, (newTex) => changeSpriteAct(texItem, newTex)), secondTitle, secondDescr, size), changeItemDescr, fontSize);
				texItem = CreateTextureItem(list[i], i, texExp.Content.transform, (x) => CreateTexturePicker(GetTextures(texType, true), loadTexAct, secondTitle, secondDescr, size), changeItemDescr, fontSize);
			}
			// adding and removing is for UW1 only: UW2's picture files hold fixed lists (MapCreator.AddTexture)
			if (LevArk.Game.Kind == UltimateEditor.Games.GameKind.UW1 && (texType == TextureType.Floor || texType == TextureType.Wall || texType == TextureType.Door || texType == TextureType.NPCHead))
			{
				GameObject addItem = null;
				Action<int> add = (x) =>
				{
					Texture2D loaded = DataReader.GetResourceTexture(texType, x);
					if(loaded)
					{
						bool added = MapCreator.AddTexture(texType, loaded);
						if(added)
						{
							GameObject texItem = null;
							//texItem = CreateTextureItem(loaded, MapCreator.GetTextureCount(texType) - 1, texExp.Content.transform, (y) => CreateTexturePicker(texType, true, (z) => LoadTextureAction(texType, z, list.Count, texItem), CreateLoadedTextureExplorer));
							texItem = CreateTextureItem(loaded, MapCreator.GetTextureCount(texType) - 1, texExp.Content.transform, (y) => CreateTexturePicker(GetTextures(texType, true), (z) => LoadTextureAction(texType, z, list.Count, (newTex) => changeSpriteAct(texItem, newTex)), secondTitle, secondDescr, default, changeItemDescr));
							addItem.transform.SetAsLastSibling();
							Destroy(TexturePicker);
						}
						else
							SpawnPopupMessage("Maximum texture limit reached (256)");
					}
				};
				Action<int> rem = (x) =>
				{
					bool removed = MapCreator.RemoveTexture(texType);
					if (removed)
					{
						GameObject lastTex = texExp.Content.transform.GetChild(texExp.Content.transform.childCount - 2).gameObject;
						Destroy(lastTex);
					}
					else
						SpawnPopupMessage("Minimum texture count reached, can't remove more.");
				};
				//addItem = CreateAddNewTextureItem(texExp.Content.transform, list.Count, (x) => CreateTexturePicker(texType, true, add, CreateLoadedTextureExplorer), rem);
				addItem = CreateAddNewTextureItem(texExp.Content.transform, list.Count, (x) => CreateTexturePicker(GetTextures(texType, true), add, secondTitle, secondDescr), rem);
			}
		}
		//FIXME : when you change selected tile while this picker is active, will change previous tile.
		else if(expType == TextureExplorerType.Picker)
		{
			//FIXME : should use properties instead?
			MapTileScript mts = InputManager.SelectedObject.GetComponent<MapTileScript>();
			if (!mts)
				return;
			MapTile selectedTile = mts.MapTile;
			List<Texture2D> list = GetCurrentLevelTextures(texType, false);
			texExp.Title.text = "Pick a new texture for this tile.";
			for (int i = 0; i < list.Count; i++)
			{
				Action<int> act = (x) =>
				{
					selectedTile.SetNewTexture(texType, x);
					TilePropertiesObject.GetComponent<TileProperties>().SetTile(selectedTile);
					MapCreator.UpdateGOTexture(selectedTile, CurrentLevel, texType);
					Destroy(textureExplorer);
				};
				CreateTextureItem(list[i], i, texExp.Content.transform, act);
			}
		}
	}

	public void CreateTexturePicker(List<Texture2D> textures, Action<int> pickAction, string title, string descr, Vector2 size = default, Func<int, string> itemDescr = null)
	{
		if (TexturePicker)
			Destroy(TexturePicker);
		TexturePicker = Instantiate(TextureExplorerPrefab, transform);
		TexturePicker.transform.position += new Vector3(12.0f, -12.0f);
		TextureExplorer texExp = TexturePicker.GetComponent<TextureExplorer>();
		//List<Texture2D> list = CreateLoadedTextureExplorer(type, fromResources);
		//texExp.Title.text = "Pick a texture to swap with.";
		if (size != default)
		{
			GridLayoutGroup glg = texExp.Content.GetComponent<GridLayoutGroup>();
			glg.cellSize = size;
		}
		texExp.Title.text = title;
		TexturePicker.transform.Find("Description").GetComponent<Text>().text = descr;
		for (int i = 0; i < textures.Count; i++)
			CreateTextureItem(textures[i], i, texExp.Content.transform, pickAction, itemDescr);
	}

	//public void LoadTextureAction(TextureType texType, int newIndex, int oldIndex, GameObject texItem)
	public void LoadTextureAction(TextureType texType, int newIndex, int oldIndex, Action<Texture2D> newTextureAct)
	{
		//Debug.LogFormat("LoadTextureAction, texType : {0}, x : {1}, new_i : {2}, texItem.name : {3}", texType, x, new_i, texItem.name);
		Texture2D loaded = DataReader.GetResourceTexture(texType, newIndex);
		if (loaded)
		{
			if (texType == TextureType.Object)
			{
				Action<Texture2D, int, int[]> changeTex = (tex, i, rawTex) =>
				{
					bool result = MapCreator.UpdateTexture(texType, oldIndex, tex);
					if (!result)
					{
						SpawnPopupMessage("Failed to swap textures - invalid dimensions.");
						return;
					}
					MapCreator.TextureData.Objects.AuxPalettes[oldIndex] = i;
					MapCreator.TextureData.Objects.RawTextures[oldIndex] = rawTex;
					//if(texItem)
					//	texItem.GetComponent<Image>().sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.zero);
				};
				CreateAuxPaletteConverter(loaded, newIndex, changeTex, newTextureAct);
			}
			else
			{
				bool result = MapCreator.UpdateTexture(texType, oldIndex, loaded);
				if (!result)
				{
					SpawnPopupMessage("Failed to swap textures - invalid dimensions.");
					return;
				}
				else
				{
					Texture2D newTex = MapCreator.GetTexture(texType, oldIndex);
					newTextureAct(newTex);
					//if(texItem)
					//	texItem.GetComponent<Image>().sprite = Sprite.Create(newTex, new Rect(0, 0, newTex.width, newTex.height), Vector2.zero);
				}
			}
		}
		else
			SpawnPopupMessage("Failed to load texture from resources.");
		Destroy(TexturePicker);
	}

	public void CreateAuxPaletteConverter(Texture2D before, int index, Action<Texture2D, int, int[]> onAccept, Action<Texture2D> newTextureAct)
	{
		GameObject auxConvGO = Instantiate(AuxConverterPrefab, transform);
		auxConvGO.transform.Find("TopBar/Title").GetComponent<Text>().text = "Graphic converter";
		Dropdown drop = auxConvGO.GetComponentInChildren<Dropdown>();
		List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();
		for (int i = 0; i < MapCreator.TextureData.AuxPalettes.Count; i++)
			options.Add(new Dropdown.OptionData("Palette " + i));		
		drop.options = options;
		if(index < 461)
			drop.value = MapCreator.TextureData.Objects.AuxPalettes[index];
		Texture2D after = before;
		Image imageA = auxConvGO.transform.Find("MainPanel/Panel/PanelA/SpriteA").GetComponent<Image>();
		Image imageB = auxConvGO.transform.Find("MainPanel/Panel/PanelB/SpriteB").GetComponent<Image>();
		Image pal = auxConvGO.transform.Find("MainPanel/Panel/PanelC/Pal").GetComponent<Image>();
		imageA.sprite = Sprite.Create(before, new Rect(0, 0, before.width, before.height), new Vector2(0.5f, 0.5f));
		//imageB.sprite = Sprite.Create(after, new Rect(0, 0, after.width, after.height), new Vector2(0.5f, 0.5f));
		int[] rawTex = new int[after.width * after.height];
		Action<int> changePal = (i) =>
		{
			Color[] auxpal = MapCreator.TextureData.AuxPalettes[i];
			after = DataWriter.ConvertToAuxPalette(before, auxpal, rawTex, true);
			imageB.sprite = Sprite.Create(after, new Rect(0, 0, after.width, after.height), new Vector2(0.5f, 0.5f));
			Texture2D auxpalTex = new Texture2D(16, 1);
			auxpalTex.SetPixels(auxpal);
			auxpalTex.filterMode = FilterMode.Point;
			auxpalTex.Apply();
			pal.sprite = Sprite.Create(auxpalTex, new Rect(0, 0, 16, 1), new Vector2(0.5f, 0.5f));

		};
		changePal(drop.value);
		drop.onValueChanged.AddListener((i) => changePal(i));
		Button but = auxConvGO.transform.Find("MainPanel/DropdownPanel/ButtonPanel/Button").GetComponent<Button>();
		but.onClick.AddListener(() => onAccept(after, drop.value, rawTex));
		but.onClick.AddListener(() => newTextureAct(after));
		but.onClick.AddListener(() => Destroy(auxConvGO));
	}

	//public List<Texture2D> CreateLevelTextureExplorer(TextureExplorer texExp, TextureType type, bool fromResources)
	public List<Texture2D> GetCurrentLevelTextures(TextureType type, bool fromResources)
	{
		List<Texture2D> list = null;
		if (type == TextureType.Floor)
			list = DataReader.GetLevelFloors(MapCreator.LevelData[CurrentLevel - 1]);
		else if (type == TextureType.Wall)
			list = DataReader.GetLevelWalls(MapCreator.LevelData[CurrentLevel - 1]);
		else if (type == TextureType.Door)
			list = DataReader.GetLevelDoors(MapCreator.LevelData[CurrentLevel - 1]);
		return list;
	}

	//public List<Texture2D> CreateLoadedTextureExplorer(TextureExplorer texExp, TextureType type, bool fromResources)
	public List<Texture2D> GetTextures(TextureType type, bool fromResources)
	{
		//texExp.Title.text = "Pick a texture be replaced.";
		List<Texture2D> list = new List<Texture2D>();
		if (type == TextureType.Floor)
		{
			if (!fromResources)
				for (int i = 0; i < MapCreator.TextureData.Floors.Count; i++)
					list.Add(MapCreator.TextureData.Floors.Textures[i]);
			else
				list = DataReader.FloorTextures;
		}
		else if (type == TextureType.Wall)
		{
			if (!fromResources)
				for (int i = 0; i < MapCreator.TextureData.Walls.Count; i++)
					list.Add(MapCreator.TextureData.Walls.Textures[i]);
			else
				list = DataReader.WallTextures;
		}
		else if (type == TextureType.Door)
		{
			//GridLayoutGroup glg = texExp.Content.GetComponent<GridLayoutGroup>();
			//glg.cellSize = new Vector2(64.0f, 128.0f);
			if (!fromResources)
				for (int i = 0; i < MapCreator.TextureData.Doors.Count; i++)
					list.Add(MapCreator.TextureData.Doors.Textures[i]);
			else
				list = DataReader.DoorTextures;
		}
		else if(type == TextureType.Lever)
		{
			if (!fromResources)
				for (int i = 0; i < MapCreator.TextureData.Levers.Count; i++)
					list.Add(MapCreator.TextureData.Levers.Textures[i]);
			else
				list = DataReader.LeverTextures;
		}
		else if(type == TextureType.Other)
		{
			if (!fromResources)
				for (int i = 0; i < MapCreator.TextureData.Other.Count; i++)
					list.Add(MapCreator.TextureData.Other.Textures[i]);
			else
				list = DataReader.OtherTextures;
		}
		else if(type == TextureType.GenericHead)
		{
			if (!fromResources)
				for (int i = 0; i < MapCreator.TextureData.GenHeads.Count; i++)
					list.Add(MapCreator.TextureData.GenHeads.Textures[i]);
			else
				list = DataReader.Portraits;
			
		}
		else if (type == TextureType.NPCHead)
		{
			if (!fromResources)
				for (int i = 0; i < MapCreator.TextureData.NPCHeads.Count; i++)
					list.Add(MapCreator.TextureData.NPCHeads.Textures[i]);
			else
				list = DataReader.Portraits;
		}
		else if (type == TextureType.Object)
		{
			if (!fromResources)
				for (int i = 0; i < MapCreator.TextureData.Objects.Count; i++)
					list.Add(MapCreator.TextureData.Objects.Textures[i]);
			else
				list = DataReader.ObjectGraphics;
		}
		return list;
	}
	public List<Texture2D> GetWritingTextures()
	{
		List<Texture2D> writTex = new List<Texture2D>();
		for (int i = 20; i < 28; i++)
			writTex.Add(MapCreator.TextureData.Other.Textures[i]);
		return writTex;
	}
	public List<Texture2D> GetGraveTextures()
	{
		List<Texture2D> gravTex = new List<Texture2D>();
		for (int i = 28; i < 30; i++)		
			gravTex.Add(MapCreator.TextureData.Other.Textures[i]);
		return gravTex;
	}
	public List<Texture2D> GetBridgeTextures(int level)
	{
		List<Texture2D> bridgeTex = new List<Texture2D>();
		for (int i = 0; i < 8; i++)
		{
			if (i < 2)
				bridgeTex.Add(MapCreator.TextureData.Other.Textures[30 + i]);
			else
				bridgeTex.Add(MapCreator.GetFloorTextureFromIndex(i - 2, level));
		}
		return bridgeTex;
	}
	public List<Texture2D> GetPillarTextures()
	{
		List<Texture2D> pillartex = new List<Texture2D>();
		for (int i = 0; i < 4; i++)
			pillartex.Add(MapCreator.TextureData.Other.Textures[i]);
		return pillartex;
	}

	public GameObject CreateTextureItem(Texture2D tex, int index, Transform parent, Action<int> act, Func<int, string> descr = null, int size = 0)
	{
		GameObject texItem = Instantiate(TextureItemPrefab, parent);
		texItem.name = tex.name;
		Image img = texItem.GetComponent<Image>();
		//Debug.LogFormat("tex {1} filter mode  : {0}", tex.filterMode, index);
		img.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.zero);
		
		Button button = texItem.GetComponent<Button>();
		if(act != null)
			button.onClick.AddListener(() => act(index));
		Text text = texItem.GetComponentInChildren<Text>();
		if (descr != null)
			text.text = descr(index);
		else
			text.text = index.ToString();
		if (size != 0)
			text.fontSize = size;
		return texItem;
	}

	public GameObject CreateAddNewTextureItem(Transform parent, int index, Action<int> add, Action<int> rem)
	{
		GameObject addItem = Instantiate(AddTextureItemPrefab, parent);
		Button[] button = addItem.GetComponentsInChildren<Button>();
		if (add != null)
			button[0].onClick.AddListener(() => add(index));
		if (rem != null)
			button[1].onClick.AddListener(() => rem(index));
		return addItem;
	}
	public void CreateGenericHeadExplorer()
	{
		CreateTextureExplorer(TextureExplorerType.Loaded, TextureType.GenericHead, "Generic portraits", "Pick a portrait to swap\nIf an NPC has got no portrait, it will use this.", "Loaded portraits", "Pick a new portrait");
	}
	public void CreateNPCHeadExplorer()
	{
		CreateTextureExplorer(TextureExplorerType.Loaded, TextureType.NPCHead, "NPC portraits", "Pick a portrait to swap or add a new portrait\nPortrait ID matches NPC ID.", "Loaded portraits", "Pick a new portrait");
	}
	public void CreateObjectGraphicExplorer()
	{
		CreateTextureExplorer(TextureExplorerType.Loaded, TextureType.Object, "Object graphics", "Pick a graphic to swap", "Loaded graphics", "Pick a new graphic and then apply palette");
	}
	public void ShowPalette()
	{
		if (!MapCreator.IsInitialized)
			return;
		GameObject palObj = new GameObject("Palette");
		palObj.transform.SetParent(transform);
		palObj.transform.localPosition = Vector3.zero;
		palObj.transform.localScale = new Vector3(2.0f, 2.0f);
		Image img = palObj.AddComponent<Image>();
		Texture2D tex = new Texture2D(16, 16);
		for (int i = 0; i < 256; i++)
		{
			Color col = MapCreator.TextureData.Palettes[0][i];
			tex.SetPixel(i % 16, i / 16, col);
		}
		tex.filterMode = FilterMode.Point;
		tex.Apply();
		img.sprite = Sprite.Create(tex, new Rect(0, 0, 16, 16), Vector2.zero);
		

		Button button = palObj.AddComponent<Button>();
		button.onClick.AddListener(() => Destroy(palObj));
	}
	public void ShowAuxPalettes()
	{

	}

	#endregion
}
