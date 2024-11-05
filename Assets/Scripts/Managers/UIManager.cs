using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Linq;

public enum EditorMode
{
	Null,
	Object,
	Tile,
	Texture,
	Sector
}

public enum TextureExplorerType
{
	Null,
	Level,
	Loaded,
	Picker,
}

public enum TextureType
{
	Null,
	Floor,
	Wall,
	Door,
	Lever,
	Other,
	GenericHead,
	NPCHead,
	Object,
}

public enum CursorType
{
	Null,
	Normal,
	ArrowX,
	ArrowY
}

public class UIManager : MonoBehaviour {

	public MapCreator MapCreator;
	public InputManager InputManager;

	[Header("Prefabs")]
	public GameObject FileExplorerPrefab;

	public GameObject ListMenuPrefab;
	public GameObject ListItemPrefab;

	public GameObject TextureExplorerPrefab;
	public GameObject TextureItemPrefab;
	public GameObject AddTextureItemPrefab;
	public GameObject AuxConverterPrefab;

	public GameObject PopupMessagePrefab;

	public GameObject StringBlockItemPrefab;
	public GameObject StringItemPrefab;
	public GameObject SimpeInputPrefab;

	public GameObject ConversationWindowPrefab;
	public GameObject ConversationDebugWindowPrefab;
	public GameObject ConversationEditorPrefab;
	public GameObject VariableItemPrefab;

	public GameObject OptionWindowPrefab;
	public GameObject DropdownPrefab;
	
	public GameObject TooltipPrefab;

	public GameObject ItemMenuPrefab;
	public GameObject ItemButtonPrefab;
	public GameObject ItemButtonAddPrefab;

	public GameObject ContextMenuPrefab;

	public GameObject DefaultButtonPrefab;
	public GameObject TextPrefab;

	public GameObject ObjectEditorPrefab;
	public GameObject CommonPropertiesPrefab;
	public GameObject CommonPropertiesBasicPrefab;
	public GameObject WeaponEditorPrefab;
	public GameObject MonsterEditorPrefab;
	public GameObject MonsterEditorBasicPrefab;
	public GameObject WeaponEditorBasicPrefab;
	public GameObject ArmourEditorPrefab;
	public GameObject ProjectileEditorPrefab;
	public GameObject RangedEditorPrefab;
	public GameObject ContainerEditorPrefab;
	public GameObject LightEditorPrefab;
	public GameObject TerrainEditorPrefab;

	public GameObject CreateObjectPrefab;
	public GameObject CreateObjectInputPrefab;
	public GameObject CreateObjectDropdownPrefab;
	public GameObject CreateObjectSliderPrefab;
	public GameObject CreateObjectTogglePrefab;
	public GameObject CreateObjectButtonPrefab;
	public GameObject CreateObjectLargeTextPrefab;
	public GameObject CreateObjectDescriptionPrefab;
	public GameObject CreateObjectImageButtonPrefab;

	public static GameObject ItemDropdownPrefab;

	[Header("Pointers")]
	public GameObject TopBarGO;
	public GameObject InfoPanel;
	public GameObject LevelSlider;
	public GameObject ObjectProperties;
	//public GameObject PropertiesPanelObject;
	public GameObject TilePanelObject;
	public GameObject StaticObjectPanelObject;
	public GameObject TilePropertiesObject;
	public GameObject ModePanel;
	public GameObject TemplatePanel;
	public Image TemplateFloorImage;
	public Image TemplateWallImage;
	public GameObject TextureTemplatePanel;
	public Image TextureTemplateFloorImage;
	public Image TextureTemplateWallImage;
	public Toggle TextureTemplateFloorToggle;
	public Toggle TextureTemplateWallToggle;
	public GameObject SectorPanel;
	public Dropdown SectorDropdown;
	public GameObject StartGO;

	public event Action OnSelectNonGO;
	public Action<GameObject> OnSelectObject;
	public event Action<LineRenderer> OnStartLink;

	public int CurrentLevel { get; private set; }
	public static EditorMode CurrentMode { get; private set; } 

	private GameObject activeMenu;
	private GameObject[] levels;	//MapCreator
	private GameObject popup;
	private GameObject tooltip;
	private GameObject listMenu;
	private GameObject fileExplorer;
	private GameObject textureExplorer;
	public GameObject TexturePicker { get; protected set; }
	private GameObject conversationWindow;
	private GameObject conversationDebugWindow;
	private GameObject itemList;
	private GameObject itemPicker;
	private GameObject contextMenu;
	private GameObject triggerLinkGO;
	private GameObject createMenuGO;
	private StaticObject linkingObject;
	public GameObject ConversationEditorGO { get; protected set; }

	public CursorType CurrentCursorMode { get; private set; }

	[Header("Other")]
	public Texture2D NormalCursor;
	public Texture2D ArrowXCursor;
	public Texture2D ArrowYCursor;

	private TileType templateType;
	private int templateFloor;
	private int templateWall;
	private int templateHeight;

	private bool activatedMenu;
	private bool spawnedPopup;

	private StaticObject copyObject;

	#region Init

	private void Start()
	{
		listMenu = null;
		CurrentLevel = 1;
		string path = DataReader.GetPathFromFile();
		if (!string.IsNullOrEmpty(path) && DataReader.ValidateFilePath(path) && DataReader.ValidateFile(path))
			DataReader.FilePath = path;
		else
			DataReader.FilePath = Application.dataPath;
		SetCursorMode(CursorType.Normal);
	}

	private void Update()
	{
		if(Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(1))
		{
			if(activatedMenu)
				activatedMenu = false;
			else if(activeMenu)
				activeMenu.SetActive(false);

			if (popup)
				popup.transform.SetAsLastSibling();
		}
	}

	public void SetMenu(GameObject menu)
	{
		if (activeMenu == null)
			activeMenu = menu;
		if (activeMenu != menu)
		{
			activeMenu.SetActive(false);
			activeMenu = menu;
			activeMenu.SetActive(true);
		}
		else
			activeMenu.SetActive(!activeMenu.activeSelf);

		activatedMenu = activeMenu.activeSelf;
	}

	public void LoadData()
	{
		if (MapCreator.IsInitialized)
		{
			SpawnPopupMessage("Data is already loaded.");
			return;
		}
		if (string.IsNullOrEmpty(DataReader.FilePath) || !DataReader.ValidateFilePath(DataReader.FilePath))
		{
			SpawnPopupMessage("Invalid path to UW level data.");
			return;
		}
		if(!DataReader.ValidateFile(DataReader.FilePath))
		{
			SpawnPopupMessage("Invalid file, should be LEV.ARK from DATA folder.");
			return;
		}

		CurrentLevel = 1;
		CurrentMode = EditorMode.Object;
		StartCoroutine(Load());
	}

	private IEnumerator Load()
	{
		DataReader.Init(this);
		SpawnPopupMessage("Loading textures", null, false, true);
		yield return null;
		TextureData texData;
		try
		{
			texData = DataReader.LoadTextures();
		}
		catch(Exception e)
		{
			Destroy(popup);
			SpawnPopupMessage("Failed to load textures\n" + e.Message);
			yield break;
		}
		if (texData == null)
		{
			Destroy(popup);
			SpawnPopupMessage("Failed to load textures.");
			yield break;
		}
		SpawnPopupMessage("Loading levels", null, false, true);
		yield return null;
		List<LevelData> levData;
		try
		{
			levData = DataReader.LoadLevels();
		}
		catch(Exception e)
		{
			Destroy(popup);
			SpawnPopupMessage("Failed to load level data\n" + e.Message);
			yield break;
		}
		if(levData == null)
		{
			Destroy(popup);
			SpawnPopupMessage("Failed to load level data");
			yield break;
		}
		SpawnPopupMessage("Loading strings", null, false, true);
		yield return null;
		StringData strData;
		try
		{
			strData = DataReader.LoadStrings();
		}
		catch(Exception e)
		{
			Destroy(popup);
			SpawnPopupMessage("Failed to load strings\n" + e.Message);
			yield break;
		}
		if(strData == null)
		{
			Destroy(popup);
			SpawnPopupMessage("Failed to load strings.");
			yield break;
		}
		SpawnPopupMessage("Loading conversations", null, false, true);
		yield return null;
		ConversationData convData;
		try
		{
			convData = DataReader.LoadConversations();
		}
		catch(Exception e)
		{
			Destroy(popup);
			SpawnPopupMessage("Failed to load conversations\n" + e.Message);
			yield break;
		}
		if(convData == null)
		{
			Destroy(popup);
			SpawnPopupMessage("Failed to load conversations");
			yield break;
		}

		GameData gameData = new GameData("");

		SpawnPopupMessage("Loading object data", null, false, true);
		yield return null;
		ObjectData objData;
		try
		{
			objData = DataReader.LoadObjectData();
		}
		catch(Exception e)
		{
			Destroy(popup);
			SpawnPopupMessage("Failed to load object data\n" + e.Message);			
			yield break;
		}
		if(objData == null)
		{
			Destroy(popup);
			SpawnPopupMessage("Failed to load object data");
			yield break;
		}
		SpawnPopupMessage("Creating game objects", null, false, true);
		yield return null;
		foreach (var lvl in levData)
		{
			lvl.OnSectorAdded += (sec, i) => updateSectorDropdown(i);
			lvl.OnSectorRemoved += (sec, i) => updateSectorDropdown(i);
		}
		MapCreator.Initialize(levData, texData, strData, convData, gameData, objData);
		levels = MapCreator.CreateLevels();
		StartGO = MapCreator.SpawnStartGO();
		Camera.main.transform.position = new Vector3(StartGO.transform.position.x, StartGO.transform.position.y, Camera.main.transform.position.z);
		if (levels == null)
		{
			Debug.LogError("Failed to create levels");
			yield break;
		}
		InfoPanel.SetActive(true);
		LevelSlider.SetActive(true);
		Slider levelSlider = LevelSlider.GetComponent<Slider>();
		levelSlider.maxValue = MapCreator.GetLevelCount();
		levelSlider.handleRect.GetComponentInChildren<Text>().text = "1";
		ModePanel.SetActive(true);
		levels[0].SetActive(true);
		TopBarGO.transform.Find("Levels").GetComponent<Button>().interactable = true;
		TopBarGO.transform.Find("Objects").GetComponent<Button>().interactable = true;
		TopBarGO.transform.Find("Textures").GetComponent<Button>().interactable = true;
		TopBarGO.transform.Find("Strings").GetComponent<Button>().interactable = true;
		TopBarGO.transform.Find("NPCs").GetComponent<Button>().interactable = true;
		TopBarGO.transform.Find("File/FileMenu/SaveData").GetComponent<Button>().interactable = true;
		//TopBarGO.transform.Find("File/FileMenu/SaveToJSON").GetComponent<Button>().interactable = true;
		//TopBarGO.transform.Find("File/FileMenu/LoadFromJSON").GetComponent<Button>().interactable = true;

		changeTemplateFloorButton(MapCreator.GetFloorTextureFromIndex(0, CurrentLevel));
		changeTemplateWallButton(MapCreator.GetWallTextureFromIndex(0, CurrentLevel));

		ItemDropdownPrefab = CreateItemDropdown();	//For conversations only
		Destroy(popup);
	}

	#endregion

	#region Save

	public void SaveData()
	{
		if(!MapCreator.IsInitialized)
		{
			SpawnPopupMessage("Data is not loaded.");
			return;
		}
		StartCoroutine(Save());
	}

	public void SaveToJSON()
	{
		if (!MapCreator.IsInitialized)
		{
			SpawnPopupMessage("Data is not loaded.");
			return;
		}
		List<SavedLevel> toSave = MapCreator.LevelData.Select(ld => new SavedLevel(ld)).ToList();
		DataWriter.WriteToJson(toSave, Path.Combine(Application.dataPath, "uwmaps.json"));
	}

	public void LoadFromJSON()
	{
		if (!MapCreator.IsInitialized)
		{
			SpawnPopupMessage("Map is not created.");
			return;
		}
		List<SavedLevel> sls = DataReader.ReadFromJson(Path.Combine(Application.dataPath, "uwmaps.json")) as List<SavedLevel>;
		for (int i = 0; i < sls.Count; i++)
			MapCreator.LevelData[i].LoadLevel(sls[i]);
	}

	private IEnumerator Save()
	{
		SpawnPopupMessage("Saving level data", null, false, true);
		yield return null;
		try
		{
			DataWriter.SaveLevelData(MapCreator.LevelData);
		}
		catch(Exception e)
		{
			SpawnPopupMessage("Failed to save level data\n" + e.Message);
			yield break;
		}
		SpawnPopupMessage("Saving texture data", null, false, true);
		yield return null;
		try
		{
			DataWriter.SaveTextureData(MapCreator.TextureData);
		}
		catch(Exception e)
		{
			SpawnPopupMessage("Failed to save texture data\n"+e.Message);
			yield break;
		}
		SpawnPopupMessage("Saving strings", null, false, true);
		yield return null;
		try
		{
			DataWriter.SaveStringData(MapCreator.StringData);
		}
		catch(Exception e)
		{
			SpawnPopupMessage("Failed to save string data\n" + e.Message);
			yield break;
		}
		SpawnPopupMessage("Saving conversations", null, false, true);
		yield return null;
		try
		{
			DataWriter.SaveConversationData(MapCreator.ConversationData);
		}
		catch(Exception e)
		{
			SpawnPopupMessage("Failed to save conversation data\n" + e.Message);
			yield break;
		}
		SpawnPopupMessage("Saving object data", null, false, true);
		yield return null;
		try
		{
			DataWriter.SaveObjectDatas(MapCreator.ObjectData);
		}
		catch(Exception e)
		{
			SpawnPopupMessage("Failed to save object data\n" + e.Message);
			yield break;
		}
		Destroy(popup);
	}

	#endregion

	#region Objects

	public void CreateObjectListMenu()
	{
		if (!MapCreator.IsInitialized)
			return;
		if (listMenu)
			return;

		listMenu = Instantiate(ListMenuPrefab, transform);
		ListMenu list = listMenu.GetComponent<ListMenu>();
		list.Title.text = "Object list";
		int totalCount = 0;
		for (int i = 0; i < MapCreator.LevelData[CurrentLevel - 1].Objects.Length; i++)
		{
			StaticObject so = MapCreator.LevelData[CurrentLevel - 1].Objects[i];
			GameObject listObject = CreateListObject(so, i);
			if (!listObject)
				continue;
			listObject.transform.SetParent(list.Content.transform);
			totalCount++;
		}
		list.Information.text = "Object count : " + totalCount + ", active mobs : " + MapCreator.LevelData[CurrentLevel - 1].ActiveMobs + "\n";
		list.Information.text += "Static counter : " + MapCreator.LevelData[CurrentLevel - 1].StaticListStart + ", mobile counter : " + MapCreator.LevelData[CurrentLevel - 1].MobileListStart;
		Transform searchT = list.transform.Find("Search");
		if (searchT)
			searchT.gameObject.SetActive(true);
		addSearch(list);
	}

	public void SetCameraToObject(StaticObject so)
	{
		if(MapCreator.ObjectToGO.ContainsKey(so))
		{
			GameObject go = MapCreator.ObjectToGO[so];
			Camera.main.transform.position = new Vector3(go.transform.position.x, go.transform.position.y, Camera.main.transform.position.z);
		}
		else
		{
			StaticObject container = so.GetFirstContainer();
			if (MapCreator.ObjectToGO.ContainsKey(container))
			{
				GameObject go = MapCreator.ObjectToGO[container];
				Camera.main.transform.position = new Vector3(go.transform.position.x, go.transform.position.y, Camera.main.transform.position.z);
			}
		}
	}

	public GameObject CreateListObject(StaticObject so, int number)
	{
		if (!so)
			return null;

		GameObject listItemGO = Instantiate(ListItemPrefab);
		listItemGO.name = so.Name;
		ListItem listItem = listItemGO.GetComponent<ListItem>();

		listItem.Icon.sprite = MapCreator.GetObjectSpriteFromID(so);
		listItem.Name.text = "" + number + " : " + so.GetFullName() + " {" + DataReader.ToHex(so.GetFileIndex()) + "}";
		listItem.Adress.text = "Prev: " + so.PrevAdress + " " + so.GetPositionString();

		Button button = listItemGO.GetComponent<Button>();
		Action<StaticObject, GameObject> onClick = (_so, go) =>
		{
			Camera.main.transform.position = new Vector3(go.transform.position.x, go.transform.position.y, Camera.main.transform.position.z);
			SetObjectProperties(_so);
		};
		if (MapCreator.ObjectToGO.ContainsKey(so))
		{
			GameObject obj = MapCreator.ObjectToGO[so];
			button.onClick.AddListener(() => onClick(so, obj));
		}
		else
		{
			StaticObject container = so.GetFirstContainer();
			if(container && MapCreator.ObjectToGO.ContainsKey(container))
			{
				GameObject containerGO = MapCreator.ObjectToGO[container];
				button.onClick.AddListener(() => onClick(so, containerGO));
			}
			else
				button.interactable = false;
		}
		if (so.Animation != null)
			listItemGO.GetComponent<Image>().color = new Color(0.7f, 0.7f, 0.2f);
		if (so is MobileObject)
		{
			MobileObject mo = (MobileObject)so;
			if(mo.ActiveMob)
			{
				listItemGO.GetComponent<Image>().color = new Color(0.3f, 0.8f, 0.3f);
				listItem.Adress.text += ", mob list : " + mo.MobListIndex.ToString();
			}
		}
		return listItemGO;
	}

	public void AddObject()
	{
		if (!MapCreator.IsInitialized)
			return;
		Vector3 center = new Vector3(Screen.width / 2.0f, Screen.height / 2.0f, 0.0f);
		GameObject floor = MapCreator.GetObjectUnderMouse(center);
		MapTile tile = null;
		if (floor && InputManager.GetDataType(floor) == DataType.Tile)
		{
			tile = floor.transform.parent.GetComponent<MapTileScript>().MapTile;
			//Debug.LogFormat("Found floor : {0}", tile);
		}
		else
		{
			Vector3 basePos = MapCreator.GetWorldPosition(center, true, true);
			tile = MapCreator.GetTile(basePos, CurrentLevel);
		}
		if(tile == null)
		{
			//Debug.LogFormat("No tile in the center of screen");
			SpawnPopupMessage("No tile to create object in!");
			return;
		}
		Action<int> addObjectAct = (id) => CreateObject(id, CurrentLevel, new Vector2Int(3, 3), tile);
		createFullItemList(addObjectAct, "Item list", "Pick an object to spawn. Red objects - unknown");
	}

	public void RemoveFromInventory(Dropdown dropdown)
	{
		PropertiesPanel pp = ObjectProperties.GetComponent<PropertiesPanel>();
		if(pp.InventoryLinks.ContainsKey(dropdown.value))
		{
			StaticObject toRemove = pp.InventoryLinks[dropdown.value];
			StaticObject container = toRemove.GetContainer();
			if(container)
			{
				RemoveFromContainerCommand rfcc = new RemoveFromContainerCommand(toRemove, container, container.Tile);
				rfcc.Do();
				InputManager.Commands.Add(rfcc);
			}
			else
			{
				//Debug.LogErrorFormat("Trying to remove item {0} from container, but it has no container.", toRemove);
			}
		}
	}

	public void DeleteObject()
	{
		//if (!InputManager.SelectedObject)
		//	return;

		//StaticObject so = InputManager.SelectedObject.GetComponent<StaticObjectScript>().StaticObject;
		StaticObject so = ObjectProperties.GetComponent<PropertiesPanel>().SelectedObject;
		RemoveObjectCommand roc = new RemoveObjectCommand(so);
		InputManager.Commands.Add(roc);
		roc.Do();
		ObjectProperties.SetActive(false);
	}

	public void SetObjectType(Dropdown dropdown)
	{
		//if (!InputManager.SelectedObject)
		//	return;

		int id = PropertiesPanel.TypeLinks[dropdown.options[dropdown.value]];
		//int id = dropdown.value;
		//StaticObject so = InputManager.SelectedObject.GetComponent<StaticObjectScript>().StaticObject;
		StaticObject so = ObjectProperties.GetComponent<PropertiesPanel>().SelectedObject;

		so.SetID(id);
		so.Name = StaticObject.GetName(so.ObjectID);
		if (MapCreator.ObjectToGO.ContainsKey(so))
		{
			GameObject obj = MapCreator.ObjectToGO[so];
			MapCreator.SetGOSprite(so);
			MapCreator.SetGODirection(so);
			obj.name = so.Name;
		}
	}

	public void ToggleFlag(Toggle tog)
	{
		//if (!InputManager.SelectedObject)
		//	return;

		//StaticObject so = InputManager.SelectedObject.GetComponent<StaticObjectScript>().StaticObject;
		StaticObject so = ObjectProperties.GetComponent<PropertiesPanel>().SelectedObject;
		if (tog.gameObject.name == "Enchantable")
			so.IsEnchanted = tog.isOn;
		else if (tog.gameObject.name == "Invisible")
			so.IsInvisible = tog.isOn;
		else if (tog.gameObject.name == "IsQuantity")
			so.IsQuantity = tog.isOn;
		else if (tog.gameObject.name == "Door")
			so.IsDoorOpen = tog.isOn;
	}

	public void ChangeObjectProperty(InputField input)
	{
		//if (!InputManager.SelectedObject)
		//	return;

		//StaticObject so = InputManager.SelectedObject.GetComponent<StaticObjectScript>().StaticObject;
		StaticObject so = ObjectProperties.GetComponent<PropertiesPanel>().SelectedObject;
		int value = 0;
		bool result = int.TryParse(input.text, out value);
		if (!result)
		{
			SpawnPopupMessage("Invalid value at " + input.name);
			return;
		}
		if(input.gameObject.name == "XPos")
		{
			if (value > 7)
				value = 7;
			else if (value < 0)
				value = 0;
			so.XPos = value;
			MapCreator.SetGOPosition(so);
		}
		else if(input.gameObject.name == "YPos")
		{
			if (value > 7)
				value = 7;
			else if (value < 0)
				value = 0;
			so.YPos = value;
			MapCreator.SetGOPosition(so);
		}
		else if (input.gameObject.name == "Height")
		{
			so.ZPos = value;
			MapCreator.SetGOHeight(so);
		}
		else if (input.gameObject.name == "Direction")
		{
			so.Direction = value;
			MapCreator.SetGODirection(so);
		}
		else if (input.gameObject.name == "Flags")
			so.Flags = value;
		else if (input.gameObject.name == "Quality")
			so.Quality = value;
		else if (input.gameObject.name == "OwnerSpecial")
			so.Owner = value;
		else if (input.gameObject.name == "QuantityLink")
			so.Special = value;

		input.text = value.ToString();

		if(so is MobileObject)
		{
			MobileObject mo = (MobileObject)so;
			if (input.gameObject.name == "Attitude")
				mo.Attitude = value;
			else if (input.gameObject.name == "Goal")
				mo.Goal = value;
			else if (input.gameObject.name == "HomeX")
				mo.XHome = value;
			else if (input.gameObject.name == "HomeY")
				mo.YHome = value;
			else if (input.gameObject.name == "HP")
				mo.HP = value;
			else if (input.gameObject.name == "NPC")
				mo.Whoami = value;
		}
	}

	#endregion

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
			if (value > 15)
				value = 15;
			else if (value < 0)
				value = 0;
			tile.FloorHeight = value;
			MapCreator.UpdateTileAndNeighbours(CurrentLevel, mts, false);
			List<StaticObject> doors = tile.GetTileObjects((so) => { return so.IsDoor(); });
			if (doors == null)
				return;
			foreach (var door in doors)
			{
				door.ZPos = tile.FloorHeight * 8;
				MapCreator.SetGOHeight(door);
				MapCreator.SetDoorGOPosition(door);
			}
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
			tile.FloorHeight = value;
			MapCreator.UpdateTileAndNeighbours(CurrentLevel, mts, false);
			List<StaticObject> doors = tile.GetTileObjects((so) => { return so.IsDoor(); });
			if (doors == null)
				return;
			foreach (var door in doors)
			{
				door.ZPos = tile.FloorHeight * 8;
				MapCreator.SetGOHeight(door);
				MapCreator.SetDoorGOPosition(door);
			}
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
			tile.TileType = templateType;
			tile.FloorHeight = templateHeight;
			tile.FloorTexture = templateFloor;
			tile.WallTexture = templateWall;
			MapCreator.UpdateTileAndNeighbours(CurrentLevel, mts, false);

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
			if(TextureTemplateFloorToggle.isOn)
				tile.FloorTexture = templateFloor;
			if(TextureTemplateWallToggle.isOn)
				tile.WallTexture = templateWall;
			MapCreator.UpdateTileAndNeighbours(CurrentLevel, mts, false);
			if (InputManager.SelectedObject)
			{
				MapTileScript otherMts = InputManager.SelectedObject.GetComponent<MapTileScript>();
				MapTile other = otherMts.MapTile;
				if (other == tile)
					TilePropertiesObject.GetComponent<TileProperties>().SetTile(tile);
			}
		}
	}

	public void ChangeTileSector(GameObject tileObj)
	{
		Sector sec = getSector();
		if (sec == null)
			return;
		Debug.Log("C");
		MapTileScript mts = tileObj.GetComponent<MapTileScript>();
		MapTile tile = mts.MapTile;
		if (tile)
		{
			mts.SetOverlay(true);
			mts.SetOverlayColor(sec.SectorColor);
		}
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
		tile.FloorHeight = height;
		tile.UpdateObjectsHeight();
		MapCreator.UpdateTileAndNeighbours(CurrentLevel, mts, false);

	
		if (InputManager.SelectedObject)
		{
			MapTileScript otherMts = InputManager.SelectedObject.GetComponent<MapTileScript>();
			MapTile other = otherMts.MapTile;
			if (other == tile)
				TilePropertiesObject.GetComponent<TileProperties>().SetTile(tile);
		}
	}

	#endregion

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
			if (texType == TextureType.Floor || texType == TextureType.Wall || texType == TextureType.Door || texType == TextureType.NPCHead)
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

	#region Strings

	public void CreateStringBlockList()
	{
		if (!MapCreator.IsInitialized)
			return;
		if (listMenu)
			return;

		listMenu = Instantiate(ListMenuPrefab, transform);
		ListMenu list = listMenu.GetComponent<ListMenu>();
		list.Title.text = "List of string blocks";
		list.Information.text = "Block count : " + MapCreator.StringData.Blocks.Count;
		for (int i = 0; i < MapCreator.StringData.Blocks.Count; i++)
		{
			StringBlock block = MapCreator.StringData.Blocks[i];
			GameObject stringBlockItem = Instantiate(StringBlockItemPrefab, list.Content.transform);
			Text text = stringBlockItem.GetComponentInChildren<Text>();
			text.text = "String block " + block.BlockNumber + " (in strings array " + i + ")" + "\n" + block.GetDescription();
			Button button = stringBlockItem.GetComponentInChildren<Button>();
			button.onClick.AddListener(() => CreateStringList(block));
		}
	}

	public void CreateStringList(StringBlock block)
	{
		if (listMenu)
			Destroy(listMenu);

		listMenu = Instantiate(ListMenuPrefab, transform);
		ListMenu list = listMenu.GetComponent<ListMenu>();
		list.Title.text = "String list for block " + block.BlockNumber;
		list.Information.text = "String count : " + block.Strings.Count;
		for (int i = 0; i < block.Strings.Count; i++)
		{
			GameObject stringItem = Instantiate(StringItemPrefab, list.Content.transform);
			stringItem.name = "StringItem_" + i;
			Text text = stringItem.GetComponentInChildren<Text>();
			text.text = i + ":";
			InputField input = stringItem.GetComponentInChildren<InputField>();
			input.text = block.Strings[i];
			int new_i = i;
			
			input.onEndEdit.AddListener((x) =>
			{
				block.Strings[new_i] = x;
			});
		}
	}

	public void CreateKeyEditor()
	{
		if (!MapCreator.IsInitialized)
			return;
		if (listMenu)
			Destroy(listMenu);

		listMenu = Instantiate(ListMenuPrefab, transform);
		ListMenu list = listMenu.GetComponent<ListMenu>();
		list.Title.text = "Key editor";
		list.Information.text = "UW1 has a limit of 63 keys";
		StringBlock block = MapCreator.StringData.GetKeyBlock();
		for (int i = 1; i < 64; i++)
		{
			GameObject stringItem = Instantiate(StringItemPrefab, list.Content.transform);
			stringItem.name = "StringItem_" + i;
			Text text = stringItem.GetComponentInChildren<Text>();
			text.text = "Key " + i;
			InputField input = stringItem.GetComponentInChildren<InputField>();
			int index = i + 100;
			input.text = block.Strings[index];
			int new_i = index;
			input.onEndEdit.AddListener((x) => block.Strings[new_i] = x);
		}
	}
	public void CreateMantraEditor()
	{
		if (!MapCreator.IsInitialized)
			return;
		if (listMenu)
			Destroy(listMenu);

		listMenu = Instantiate(ListMenuPrefab, transform);
		ListMenu list = listMenu.GetComponent<ListMenu>();
		list.Title.text = "Mantra editor";
		list.Information.text = "";
		StringBlock block = MapCreator.StringData.GetMantraBlock();
		for (int i = 0; i < 25; i++)
		{
			GameObject inputItem = Instantiate(SimpeInputPrefab, list.Content.transform);
			inputItem.name = "InputItem_" + i;
			Text text = inputItem.GetComponentInChildren<Text>();
			text.text = MapCreator.StringData.GetMantraDescription(i);
			InputField input = inputItem.GetComponentInChildren<InputField>();
			int index = i + 51;
			input.text = block.Strings[index];
			int new_i = index;
			input.onEndEdit.AddListener((str) => block.Strings[new_i] = str);
		}
	}

	public void CreateBarterStringsList(Conversation conv)
	{
		if (listMenu)
			Destroy(listMenu);

		listMenu = Instantiate(ListMenuPrefab, transform);
		ListMenu list = listMenu.GetComponent<ListMenu>();
		list.Title.text = "Barter strings";
		list.Information.text = "";
		for (int i = 0; i < 16; i++)
		{
			GameObject stringItem = Instantiate(StringItemPrefab, list.Content.transform);
			Text text = stringItem.GetComponentInChildren<Text>();
			text.text = i + ":";
			InputField input = stringItem.GetComponentInChildren<InputField>();
			input.text = conv.BarterStrings[i];
			int new_i = i;
			input.onEndEdit.AddListener((x) =>
			{
				conv.BarterStrings[new_i] = x;
			});
		}
	}

	#endregion

	#region Conversations

	public void CreatePlayConversationList()
	{
		int[] impGlobs = ConversationManager.GetImportedGlobals();
		Toggle debugTog = null, singleTog = null, clearTog = null;
		System.Action<ListMenu> options = (list) =>
		{
			list.OptionsPanel.SetActive(true);
			debugTog = list.CreateToggleOption("Debug mode");
			singleTog = list.CreateToggleOption("Single code processing", false);
			clearTog = list.CreateToggleOption("Clean conversation (no saved progress)");
			System.Action createOptionsWindow = () =>
			{
				GameObject optWinGO = Instantiate(OptionWindowPrefab, transform);
				OptionsWindow optWin = optWinGO.GetComponent<OptionsWindow>();
				optWin.Title.text = "Conversation parameters";
				optWin.AddDefaultOptions(impGlobs);
			};
			Button optBut = list.CreateButtonOption("Parameters", createOptionsWindow);
			debugTog.onValueChanged.AddListener((x) => { if (x == false) singleTog.isOn = false; singleTog.interactable = x; });
		};
		System.Func<Conversation, bool> buttonCreationCondition = (conv) => { return (conv != null); };
		System.Func<Conversation, bool> interactableCondition = (conv) => { return (!(conv.State == ConversationState.Unconverted)); };
		System.Action<Conversation, GameObject> buttonPreferances = (conv, go) =>
		{
			Color col = Color.white;
			if (conv.State == ConversationState.Converted)
				col = new Color(0.5f, 1.0f, 0.5f);
			else if (conv.State == ConversationState.Unconverted)
				col = new Color(1.0f, 0.5f, 0.5f);
			else if (conv.State == ConversationState.Uneditable)
				col = new Color(1.0f, 1.0f, 0.5f);
			Image image = go.GetComponent<Image>();
			if (image)
				image.color = col;
		};

		System.Action<Conversation, int> onClick = (conv, i) =>
		{
			impGlobs[14] = conv.Slot;
			PlayConversation(conv, impGlobs, debugTog.isOn, singleTog.isOn, clearTog.isOn);
		};
		CreateGenericConversationList("List of conversations", "Choose conversation to play", onClick, buttonCreationCondition, interactableCondition, options, buttonPreferances);
	}

	public void CreateDeleteConversationList()
	{
		System.Action<Conversation, int> onClick = (conv, i) =>
		{
			SpawnPopupMessage("Conversation deleted");
			string fileName = ConversationEditor.GetConversationFile(conv.Slot);
			MapCreator.StringData.RemoveBlock(conv.StringBlock);
			MapCreator.ConversationData.Conversations[conv.Slot] = null;
			if (File.Exists(fileName))
				File.Delete(fileName);
			//FIXME : Update Mobiles with whoamis
		};
		System.Func<Conversation, bool> buttonCreationCondition = (conv) => { return (conv != null); };
		CreateGenericConversationList("List of conversations", "Choose conversation to delete.\nThis will also delete all strings associated.", onClick, buttonCreationCondition);
	}

	public void CreateAddConversationList()
	{
		System.Action<Conversation, int> onClick = (conv, i) =>
		{
			SpawnPopupMessage("Conversation created");
			conv = MapCreator.ConversationData.Conversations[i] = new Conversation(ConversationState.Unconverted);
			conv.Slot = i;
			conv.BarterStrings = StringBlock.GetDefaultBarterStrings();
			conv.Likes = new List<int>();
			conv.Dislikes = new List<int>();
			//StringBlock block = MapCreator.StringData.AddNewBlock(conv.Slot);
			//conv.StringBlock = block.BlockNumber;
		};
		System.Func<Conversation, bool> buttonCreationCondition = (conv) => { return (conv == null); };
		CreateGenericConversationList("List of empty slots", "Choose slot to add new conversation.", onClick, buttonCreationCondition);
	}

	public void CreateEditConversationList()
	{
		Action<Conversation, int> onClick = (conv, i) =>
		{
			if (ConversationEditorGO)
				return;
			ConversationEditorGO = Instantiate(ConversationEditorPrefab, transform);
			ConversationEditorGO.SetActive(true);
			ConversationEditor convEdit = ConversationEditorGO.GetComponent<ConversationEditor>();
			convEdit.Init(conv, this);
		};
		System.Func<Conversation, bool> buttonCreationCondition = (conv) => { if (!conv) return false;return (!(conv.State == ConversationState.Uneditable)); };
		CreateGenericConversationList("List of created conversations", "Cannot edit original Ultima conversations,\nonly those created in the editor.", onClick, buttonCreationCondition);
	}

	public GameObject CreateGenericConversationList(string title, string info, System.Action<Conversation, int> onClick, System.Func<Conversation, bool> buttonCreationCondition, System.Func<Conversation, bool> interactableCondition = null, System.Action<ListMenu> options = null, System.Action<Conversation, GameObject> buttonPreferences = null)
	{
		if (!MapCreator.IsInitialized)
			return null;
		if (listMenu)
			return null;
		if (onClick == null)
			return null;

		listMenu = Instantiate(ListMenuPrefab, transform);
		ListMenu list = listMenu.GetComponent<ListMenu>();
		list.Title.text = title;
		list.Information.text = info;
		options?.Invoke(list);

		for (int i = 0; i < MapCreator.ConversationData.Conversations.Length; i++)
		{
			if (i == 0)
				continue;
			int new_i = i;
			Conversation conv = MapCreator.ConversationData.Conversations[new_i];
			if (!buttonCreationCondition(conv))
				continue;
			
			string npcName = "";
			if (i < 256 && conv)
				npcName = MapCreator.StringData.GetNPCName(i);
			else if(i >= 256)
				npcName = StaticObject.GetMonsterName(i - 256) + "(generic)";

			GameObject stringItem = Instantiate(StringBlockItemPrefab, list.Content.transform);
			Text text = stringItem.GetComponentInChildren<Text>();
			text.text = new_i + ": " + npcName;
			Button button = stringItem.GetComponentInChildren<Button>();
			buttonPreferences?.Invoke(conv, stringItem);
			//button.onClick.AddListener(() => DataWriter.SaveStringToTxt(conv.DumpConversation(), "conv_" + new_i + ".txt"));
			System.Action act = () =>
			{
				onClick(conv, new_i);
				Destroy(listMenu);
			};
			button.onClick.AddListener(() => act());
			if (interactableCondition != null && !interactableCondition(conv))
				button.interactable = false;
		}
		return listMenu;
	}

	public void PlayConversation(Conversation conv, int[] impGlobs, bool debug, bool single, bool clean)
	{
		conversationWindow = Instantiate(ConversationWindowPrefab, transform);
		GameObject debugWin = null;
		if(debug)
			debugWin = Instantiate(ConversationDebugWindowPrefab, transform);
		ConversationWindow convWin = conversationWindow.GetComponent<ConversationWindow>();
		convWin.DebugWindow = debugWin;
		int[] savedVars = null;
		if (!clean)
			//privGlobs = conv.ConvGlobals;
			savedVars = conv.SavedVars;
		if (clean)
			conv.UnnamedVars = null;
		ConversationManager convMan = new ConversationManager(conv, conversationWindow, debugWin, savedVars, impGlobs, debug, single);
		convWin.ConvManager = convMan;
		if (!single)
		{
			convWin.NextOperation.gameObject.SetActive(false);
			convMan.ProcessConversation();
		}
	}

	public GameObject CreateItemDropdown()
	{
		GameObject dropdownGO = Instantiate(DropdownPrefab, transform);
		dropdownGO.SetActive(false);
		List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();
		Dropdown.OptionData firstOp = new Dropdown.OptionData("nothing");
		options.Add(firstOp);
		for (int i = 0; i < 320; i++)
		{
			string itemName = StaticObject.GetName(i);
			Sprite itemSprite = MapCreator.GetObjectSpriteFromID(i);
			Dropdown.OptionData option = new Dropdown.OptionData(itemName, itemSprite);
			options.Add(option);
		}
		Dropdown dropdown = dropdownGO.GetComponent<Dropdown>();
		dropdown.options = options;

		return dropdownGO;
	}

	//public GameObject CreateVariableList(int varCount, Dictionary<int, string> privateGlobals)
	public GameObject CreateVariableList(int varCount, List<string> vars, string title, string description, bool adding = false)
	{
		if (!MapCreator.IsInitialized)
			return null;
		if (listMenu)
			return null;
		listMenu = Instantiate(ListMenuPrefab, transform);
		ListMenu list = listMenu.GetComponent<ListMenu>();
		list.Title.text = title;
		list.Information.text = description;

		for (int i = 0; i < varCount; i++)
		{
			int new_i = i;
			GameObject varItem = Instantiate(VariableItemPrefab, list.Content.transform);
			//Transform idT = varItem.transform.Find("VariableID");
			//idT.GetComponent<Text>().text = "id : " + new_i;
			InputField input = varItem.GetComponentInChildren<InputField>();
			//if (privateGlobals.ContainsKey(new_i))
			input.text = vars[new_i];
			input.onEndEdit.AddListener((str) => vars[new_i] = str);
			if(adding)
			{
				Button button = varItem.GetComponentInChildren<Button>();
				System.Action act = () =>
				{
					for (int j = 0; j < list.Content.transform.childCount; j++)
					{
						int new_j = j;
						if(list.Content.transform.GetChild(new_j).gameObject == varItem)
						{
							vars.RemoveAt(new_j);
							Destroy(list.Content.transform.GetChild(new_j).gameObject);
						}
					}
				};
				button.onClick.AddListener(() => act());
			}
			else
				varItem.GetComponentInChildren<Button>().gameObject.SetActive(false);
		}
		if(adding)
		{
			GameObject addItem = Instantiate(StringBlockItemPrefab, list.Content.transform);
			addItem.GetComponentInChildren<Text>().text = "Add new local variable";
			Button button = addItem.GetComponent<Button>();
			System.Action act = () =>
			{
				int count = vars.Count;
				string varName = "newVar" + count;
				for (int j = 0; j < list.Content.transform.childCount; j++)
				{
					int new_j = j;
					InputField inputField = list.Content.transform.GetChild(new_j).GetComponentInChildren<InputField>();
					if(inputField && inputField.text == varName)
						varName += "(2)";
				}
				GameObject newVarItem = Instantiate(VariableItemPrefab, list.Content.transform);
				InputField input = newVarItem.GetComponentInChildren<InputField>();
				Button removeButton = newVarItem.GetComponentInChildren<Button>();
				System.Action removeAction = () =>
				{
					for (int j = 0; j < list.Content.transform.childCount; j++)
					{
						int new_j = j;
						if (list.Content.transform.GetChild(new_j).gameObject == newVarItem)
						{
							vars.RemoveAt(new_j);
							Destroy(list.Content.transform.GetChild(new_j).gameObject);
						}
					}
				};
				removeButton.onClick.AddListener(() => removeAction());

				input.text = varName;
				System.Action<string> changeNameAction = (str) =>
				{
					for (int j = 0; j < list.Content.transform.childCount; j++)
					{
						int new_j = j;
						if (list.Content.transform.GetChild(new_j).gameObject == newVarItem)
						{
							vars[new_j] = str;
						}
					}
				};
				input.onEndEdit.AddListener((str) => changeNameAction(str));
					
				vars.Add(varName);
				addItem.transform.SetAsLastSibling();
			};
			button.onClick.AddListener(() => act());
		}

		return listMenu;
	}

	#endregion

	#region Item lists

	public void CreateConversationItemList(List<int> items, string title, string info)
	{
		if (itemList)
			Destroy(itemList);
		itemList = Instantiate(ItemMenuPrefab, transform);
		ListMenu list = itemList.GetComponent<ListMenu>();
		list.Title.text = title;
		list.Information.text = info;
		Transform searchT = list.transform.Find("Search");
		if (searchT)
			searchT.gameObject.SetActive(false);
		for (int i = 0; i < items.Count; i++)
		{
			int new_i = i;
			GameObject itemGO = createItemButton(items[i], list.Content.transform, getItemButtonTooltip(items[i]));
			Action remove = createRemoveAction(items, items[new_i], itemGO);
			itemGO.GetComponent<Button>().onClick.AddListener(() => remove());
		}
		GameObject addGO = Instantiate(ItemButtonAddPrefab, list.Content.transform);
		Action add = () =>
		{
			CreateFullItemList(list, items, addGO, "Item picker", "Add new item / category to list.");
		};
		Button button = addGO.GetComponent<Button>();
		button.onClick.AddListener(() => add());
	}
	private Action createRemoveAction(List<int> itemList, int index, GameObject itemGO)
	{
		Action act =	() =>
		{
			Debug.LogFormat("Remove item, {0}, {1}, {2}", itemList.Count, index, itemGO);
			itemList.Remove(index);
			Destroy(itemGO);
		};
		return act;

	}
	public void CreateFullItemList(ListMenu currentList, List<int> items, GameObject addGO, string title, string info)
	{
		if (itemPicker)
			Destroy(itemPicker);

		itemPicker = Instantiate(ItemMenuPrefab, transform);
		ListMenu list = itemPicker.GetComponent<ListMenu>();
		list.Title.text = title;
		list.Information.text = info;
		addSearch(list);
		for (int i = -8; i < 320; i++)
		{
			int new_i = i;
			if (i < 0)
				new_i = getCategory(i + 8);
			
			Action onClick = null;
			onClick = () =>
			{				
				GameObject newItem = createItemButton(new_i, currentList.Content.transform, getItemButtonTooltip(new_i));
				Action remove = createRemoveAction(items, new_i, newItem);
				newItem.GetComponent<Button>().onClick.AddListener(() => remove());
				addGO.transform.SetAsLastSibling();
				items.Add(new_i);
				Destroy(itemPicker);
			};
			GameObject itemGO = createItemButton(new_i, list.Content.transform, getItemButtonTooltip(new_i));
			itemGO.GetComponent<Button>().onClick.AddListener(() => onClick());
		}
	}

	private void addSearch(ListMenu list)
	{
		Transform searchT = list.transform.Find("Search");
		if(searchT)
		{
			InputField inputField = searchT.GetComponent<InputField>();
			Action<string> search = (str) =>
			{
				if(string.IsNullOrEmpty(str))
				{
					for (int i = 0; i < list.Content.transform.childCount; i++)
					{
						list.Content.transform.GetChild(i).gameObject.SetActive(true);
					}
				}
				else
				{
					str = str.ToLower();
					for (int i = 0; i < list.Content.transform.childCount; i++)
					{
						GameObject item = list.Content.transform.GetChild(i).gameObject;
						string name = item.name.ToLower();
						if (name.Contains(str))
							item.SetActive(true);
						else
							item.SetActive(false);
					}
				}
			};
			inputField.onEndEdit.AddListener((str) => search(str));
			inputField.onValueChanged.AddListener((str) => search(str));
		}
	}

	private void createFullItemList(Action<int> onClick, string title, string info)
	{
		if (itemPicker)
			Destroy(itemPicker);

		itemPicker = Instantiate(ItemMenuPrefab, transform);
		ListMenu list = itemPicker.GetComponent<ListMenu>();
		list.Title.text = title;
		list.Information.text = info;
		addSearch(list);

		for (int i = 0; i < 459; i++)
		{
			int new_i = i;
			GameObject itemGO = createItemButton(new_i, list.Content.transform, getItemButtonTooltip(new_i));
			Button button = itemGO.GetComponent<Button>();
			button.onClick.AddListener(() => onClick(new_i));
			button.onClick.AddListener(() => Destroy(itemPicker));
			if(StaticObject.GetObjectType(i) == ObjectType.Unknown)
			{
				button.GetComponent<Image>().color = Color.red;
			}
		}
	}
	public void CreateSpecifiedItemList(Action<int> onClick, List<int> ids, string title, string info, bool dontDestroy = false)
	{
		if (itemPicker)
			Destroy(itemPicker);

		itemPicker = Instantiate(ItemMenuPrefab, transform);
		ListMenu list = itemPicker.GetComponent<ListMenu>();
		list.Title.text = title;
		list.Information.text = info;
		Transform searchT = list.transform.Find("Search");
		if (searchT)
			searchT.gameObject.SetActive(false);

		foreach (var id in ids)
		{
			int new_i = id;
			GameObject itemGO = createItemButton(new_i, list.Content.transform, getItemButtonTooltip(new_i));
			Button button = itemGO.GetComponent<Button>();
			button.onClick.AddListener(() => onClick(new_i));
			if(!dontDestroy)
				button.onClick.AddListener(() => Destroy(itemPicker));
			if (StaticObject.GetObjectType(id) == ObjectType.Unknown)
			{
				button.GetComponent<Image>().color = Color.red;
			}
		}
	}
	private void createItemList(Action<StaticObject> onClick, List<StaticObject> objects, string title, string info)
	{
		if (itemPicker)
			Destroy(itemPicker);

		itemPicker = Instantiate(ItemMenuPrefab, transform);
		itemPicker.GetComponent<RectTransform>().sizeDelta = new Vector2(377, 200);
		ListMenu list = itemPicker.GetComponent<ListMenu>();
		list.Title.text = title;
		list.Information.text = info;
		Transform searchT = list.transform.Find("Search");
		if (searchT)
			searchT.gameObject.SetActive(false);
		foreach (var so in objects)
		{
			GameObject itemGO = createItemButton(so.ObjectID, list.Content.transform, so.GetFullName());
			Button button = itemGO.GetComponent<Button>();
			button.onClick.AddListener(() => onClick(so));
			button.onClick.AddListener(() => Destroy(itemPicker));
			if (StaticObject.GetObjectType(so.ObjectID) == ObjectType.Unknown)
			{
				button.GetComponent<Image>().color = Color.red;
			}
		}
	}

	private GameObject createItemButton(int id, Transform parent, string tooltip)
	{
		GameObject itemGO = Instantiate(ItemButtonPrefab, parent);
		ItemButton ib = itemGO.GetComponent<ItemButton>();
		setItemButtonSprite(ib, id);
		ib.Tooltip = tooltip;
		itemGO.name = ib.Tooltip;
		return itemGO;
	}

	private void setItemButtonSprite(ItemButton ib, int id)
	{
		if (id >= 1000)
		{
			int cat = (id - 1000) * 16;
			ib.MainSprite.sprite = MapCreator.GetObjectSpriteFromID(cat);
			ib.AdditionalSprite.sprite = MapCreator.GetObjectSpriteFromID(cat + 8);
			ib.AdditionalSprite.gameObject.SetActive(true);
			ib.gameObject.GetComponent<Image>().color = Color.red;
		}
		else
			ib.MainSprite.sprite = MapCreator.GetObjectSpriteFromID(id);
	}
	private string getItemButtonTooltip(int id)
	{
		if (id >= 1000)
		{
			if (id == 1000)
				return "Weapons";
			else if (id == 1002)
				return "Armour";
			else if (id == 1003)
				return "Rings & shields";
			else if (id == 1008)
				return "Containers";
			else if (id == 1009)
				return "Light sources & wands";
			else if (id == 1010)
				return "Treasures";
			else if (id == 1011)
				return "Food";
			else if (id == 1019)
				return "Books & scrolls";
			else
				return "Invalid";
		}
		else
			return StaticObject.GetName(id);
	}
	private int getCategory(int index)
	{
		switch (index)
		{
			case 0:	return 1000;
			case 1:	return 1002;
			case 2:	return 1003;
			case 3:	return 1008;
			case 4:	return 1009;
			case 5:	return 1010;
			case 6:	return 1011;
			case 7:	return 1019;
			default:
				return -1;
		}
	}

	#endregion

	#region Context Menu

	public void SpawnContextMenu(Vector2 mousePosition, StaticObject so)
	{
		if (contextMenu)
			Destroy(contextMenu);

		contextMenu = Instantiate(ContextMenuPrefab, transform);
		contextMenu.transform.position = mousePosition;
		contextMenu.name = "ContextMenu";
		contextMenu.transform.Find("TopPanel/Close").gameObject.GetComponent<Button>().onClick.AddListener(() => Destroy(contextMenu));

		createButton("Copy", () => copyObject = so, getContextMenuContent(), contextMenu, 0);
		Action remove = () =>
		{
			MapCreator.RemoveObject(so, true);
			StaticObjectPanelObject.SetActive(false);
			ObjectProperties.GetComponent<PropertiesPanel>().DeselectObject();
			ObjectProperties.SetActive(false);
			OnSelectNonGO();
		};
		createButton("Remove", remove, getContextMenuContent(), contextMenu, 1);
		Action<StaticObject> addTrigger = (tr) =>
		{
			if (!MapCreator.AddTrigger(so, tr))
				SpawnPopupMessage("Failed to add trigger");
			SelectObject(tr, false);
		};
		createButton("Add trigger", () => CreateSpecifiedItemList((id) => CreateObject(id, CurrentLevel, new Vector2Int(so.XPos, so.YPos), so.Tile, addTrigger), StaticObject.GetTriggers(), "Trigger list", ""), getContextMenuContent(), contextMenu, 2);
		if(!so.IsQuantity && so.Special > 0)
		{
			Action<StaticObject> selectContainedAct = (_so) => DeselectObjectAndSetProperties(_so);
			Action getContainedAct = () => createItemList(selectContainedAct, so.GetContainedObjects(), so.Name + " inventory", "");
			createButton("Inventory", getContainedAct, getContextMenuContent(), contextMenu, 2);
		}
		if(so.IsTrigger() && MapCreator.ObjectToGO.ContainsKey(so))
		{
			createButton("Link trigger", () => StartLinking(so, MapCreator.ObjectToGO[so].transform.position), getContextMenuContent(), contextMenu, 3);
		}
		else if((so.ObjectID == 395 || so.ObjectID == 396) && MapCreator.ObjectToGO.ContainsKey(so))
		{
			createButton("Link trap", () => StartLinking(so, MapCreator.ObjectToGO[so].transform.position), getContextMenuContent(), contextMenu, 3);
		}
		else if(so.IsLever() && so.GetUseTrigger())
		{
			StaticObject use = so.GetUseTrigger();
			createButton("Link lever", () => StartLinking(use, MapCreator.ObjectToGO[so].transform.position), getContextMenuContent(), contextMenu, 3);
		}
	}

	public void SpawnContextMenu(Vector2 mousePosition, Vector2Int spawnPosition, MapTile tile)
	{
		if (contextMenu)
			Destroy(contextMenu);

		contextMenu = Instantiate(ContextMenuPrefab, transform);
		contextMenu.transform.position = mousePosition;
		contextMenu.name = "ContextMenu";
		contextMenu.transform.Find("TopPanel/Close").gameObject.GetComponent<Button>().onClick.AddListener(() => Destroy(contextMenu));

		Action<int> addObjectAct = (id) => CreateObject(id, CurrentLevel, spawnPosition, tile);
		Action createListAct = () => createFullItemList(addObjectAct, "Item list", "Pick an object to spawn. Red objects - unknown");
		createButton("Add object", createListAct, getContextMenuContent(), contextMenu, 0);
		Action<int> addSpecificObjectAct = (id) => CreateObject(id, CurrentLevel, spawnPosition, tile);

		createButton("Add move trigger", () => addSpecificObjectAct(416), getContextMenuContent(), contextMenu, 2);
		createButton("Add trap", () => CreateSpecifiedItemList(addSpecificObjectAct, StaticObject.GetIDsByType(ObjectType.Trap), "Trap list", "Pick a trap to create"), getContextMenuContent(), contextMenu, 1);

		if(copyObject)
		{
			Action copyObjectAct = () => MapCreator.AddObject(tile, spawnPosition, CurrentLevel, copyObject.ObjectID, copyObject);
			createButton("Paste object", copyObjectAct, getContextMenuContent(), contextMenu, 3);
		}
	}
	public void SpawnConfigurableContextMenu(List<Tuple<string, Action<int>>> actions)
	{
		if (contextMenu)
			Destroy(contextMenu);
		contextMenu = Instantiate(ContextMenuPrefab, transform);
		contextMenu.transform.position = Input.mousePosition;
		contextMenu.name = "ContextMenu";
		contextMenu.transform.Find("TopPanel/Close").gameObject.GetComponent<Button>().onClick.AddListener(() => Destroy(contextMenu));

		for (int i = 0; i < actions.Count; i++)
		{
			string text = actions[i].Item1;
			Action<int> act = actions[i].Item2;
			int new_i = i;
			createButton(text, () => act(new_i), getContextMenuContent(), contextMenu, i);
		}
	}

	public void SpawnYesNoMenu(string message, float textHeight, Action yesAct, Action noAct, string yesStr, string noStr)
	{
		if (contextMenu)
			Destroy(contextMenu);

		contextMenu = Instantiate(ContextMenuPrefab, transform);
		contextMenu.transform.position = new Vector3(Screen.width / 2, Screen.height / 2);
		contextMenu.name = "ContextMenu";
		contextMenu.transform.Find("TopPanel/Close").gameObject.GetComponent<Button>().onClick.AddListener(() => Destroy(contextMenu));
		createText(message, textHeight, getContextMenuContent());
		createButton(yesStr, yesAct, getContextMenuContent(), contextMenu, 0);
		createButton(noStr, noAct, getContextMenuContent(), contextMenu, 0);
	}
	private Transform getContextMenuContent()
	{
		if (contextMenu)
			return contextMenu.transform.Find("TopPanel/Main");
		else
			return null;
	}
	private GameObject createText(string message, float height, Transform parent)
	{
		GameObject textGO = Instantiate(TextPrefab, parent);
		RectTransform textRT = textGO.GetComponent<RectTransform>();
		textRT.sizeDelta = new Vector2(textRT.sizeDelta.x, height);
		textGO.name = "Text";
		textGO.GetComponent<Text>().text = message;
		return textGO;
	}
	private GameObject createButton(string message, Action onClick, Transform parent, GameObject menu, int id)
	{
		GameObject butGO = Instantiate(DefaultButtonPrefab, parent);
		butGO.name = "Button_" + id;
		butGO.GetComponentInChildren<Text>().text = message;
		Button but = butGO.GetComponent<Button>();
		if(onClick != null)
			but.onClick.AddListener(() => onClick());
		but.onClick.AddListener(() => Destroy(menu));
		return butGO;
	}

	public void CreateLink(GameObject endGO)
	{
		if (!endGO)
			return;
		StaticObjectScript sos = endGO.GetComponent<StaticObjectScript>();
		if(sos)
		{
			StaticObject end = sos.StaticObject;
			StaticObject start = linkingObject;
			Action<StaticObject> link = (newEnd) =>
			{
				if (start.Special > 0 && start.IsQuantity == false)
					SpawnYesNoMenu("Warning, this will destroy current linking objects.", 40, () => LinkObjects(start, newEnd), null, "Link", "Cancel");
				else
					LinkObjects(start, newEnd);
			};
			if (end.IsTrap() || start.IsTrap()) //Delete object & inventory traps
				link(end);
			else if (end.IsDoor())
			{
				List<StaticObject> doorTraps = end.GetDoorTraps();
				if (doorTraps == null || doorTraps.Count == 0)
					SpawnPopupMessage(string.Format("Target door has no door traps to link to."));
				else if (doorTraps.Count == 1)
					link(doorTraps[0]);
				else if(doorTraps.Count > 1)
				{
					List<Tuple<string, Action<int>>> actions = new List<Tuple<string, Action<int>>>();
					for (int i = 0; i < doorTraps.Count; i++)
					{
						actions.Add(new Tuple<string, Action<int>>(doorTraps[i].Name + " [" + doorTraps[i].CurrentAdress + "]", (but_i) => link(doorTraps[but_i])));
					}
					SpawnConfigurableContextMenu(actions);
				}
			}
			else			
				SpawnPopupMessage(string.Format("Target object ({0}) is not a trap.", end.Name));
			
		}
	}
	public void LinkObjects(StaticObject start, StaticObject end)
	{
		if(start.Special > 0 && start.IsQuantity == false)
		{
			List<StaticObject> oldList = start.GetContainedObjects();
			foreach (var old in oldList)
				MapCreator.RemoveObject(old);
		}
		start.Special = end.CurrentAdress;
		if (start.IsTrigger() || start.ObjectID == 395)	//Delete object trap
		{
			start.Quality = end.Tile.Position.x;
			start.Owner = end.Tile.Position.y;
		}
		start.IsQuantity = true;
	}

	public void StopLinking() => linkingObject = null;
	
	
	public void StartLinking(StaticObject so, Vector3 worldStartPos)
	{
		triggerLinkGO = new GameObject("TriggerLink");
		LineRenderer triggerLink = createLineRendererForLink(triggerLinkGO);
		Vector3[] points = new Vector3[2] { worldStartPos, worldStartPos };
		triggerLink.SetPositions(points);
		OnStartLink(triggerLink);
		linkingObject = so;
	}

	public LineRenderer createLineRendererForLink(GameObject go)
	{
		LineRenderer triggerLink = go.AddComponent<LineRenderer>();
		triggerLink.material = MapCreator.SpriteMaterial;
		triggerLink.sortingLayerName = "Items";
		triggerLink.startWidth = 0.03f;
		triggerLink.endWidth = 0.03f;
		return triggerLink;
	}

	#endregion

	#region Object Editors

	public void CreateCommonPropertiesEditor()
	{
		CreateSpecifiedItemList((i) => CreateBasicCommonPropertiesEditor(i), StaticObject.GetAll(), "Object list", "Pick object to edit", true);
	}
	public void CreateWallTerrainEditor()
	{
		CreateTexturePicker(GetTextures(TextureType.Wall, false), (i) => CreateTerrainEditor(i, TextureType.Wall), "Wall textures", "Select wall to edit");
	}
	public void CreateFloorTerrainEditor()
	{
		CreateTexturePicker(GetTextures(TextureType.Floor, false), (i) => CreateTerrainEditor(i, TextureType.Floor), "Floor textures", "Select floor to edit");
	}
	public void CreateCommonPropertiesEditor(int id)
	{
		Vector2 before = Vector2.zero;
		if (listMenu)
		{
			before = listMenu.GetComponent<RectTransform>().position;
			Destroy(listMenu);
		}

		listMenu = Instantiate(ObjectEditorPrefab, transform);
		ListMenu list = listMenu.GetComponent<ListMenu>();
		list.Title.text = "Common object properties";
		list.Information.text = "";
		if (before != Vector2.zero)
			listMenu.GetComponent<RectTransform>().position = before;

		CommonData com = MapCreator.ObjectData.CommonData[id];
		GameObject commonProps = Instantiate(CommonPropertiesPrefab, list.Content.transform);
		commonProps.transform.Find("Sprite").GetComponent<Image>().sprite = MapCreator.GetObjectSpriteFromID(id);
		commonProps.transform.Find("Name").GetComponent<Text>().text = StaticObject.GetName(id);
		commonProps.transform.Find("Height").GetComponent<InputField>().text = com.Height.ToString();
		commonProps.transform.Find("Radius").GetComponent<InputField>().text = com.Radius.ToString();
		commonProps.transform.Find("Type").GetComponent<InputField>().text = com.Type.ToString();
		commonProps.transform.Find("Mass").GetComponent<InputField>().text = com.Mass.ToString();

		commonProps.transform.Find("F0").GetComponent<InputField>().text = com.Flag0.ToString();
		commonProps.transform.Find("F1").GetComponent<InputField>().text = com.Flag1.ToString();
		commonProps.transform.Find("F2").GetComponent<InputField>().text = com.Flag2.ToString();
		commonProps.transform.Find("F3").GetComponent<InputField>().text = com.Flag3.ToString();
		commonProps.transform.Find("F4").GetComponent<InputField>().text = com.Flag4.ToString();
		commonProps.transform.Find("Pickable").GetComponent<InputField>().text = com.Pickable.ToString();
		commonProps.transform.Find("F6").GetComponent<InputField>().text = com.Flag6.ToString();
		commonProps.transform.Find("Container").GetComponent<InputField>().text = com.Container.ToString();

		commonProps.transform.Find("Value").GetComponent<InputField>().text = com.Value.ToString();

		commonProps.transform.Find("QualityClass").GetComponent<InputField>().text = com.QualityClass.ToString();

		commonProps.transform.Find("Ownable").GetComponent<InputField>().text = com.Ownable.ToString();
		commonProps.transform.Find("ObjectType").GetComponent<InputField>().text = com.ObjectType.ToString();

		commonProps.transform.Find("QualityType").GetComponent<InputField>().text = com.QualityType.ToString();
		commonProps.transform.Find("LookDescription").GetComponent<InputField>().text = com.LookDescription.ToString();

		commonProps.transform.Find("Raw0").GetComponent<InputField>().text = com.RawData[0].ToString();
		commonProps.transform.Find("Raw1").GetComponent<InputField>().text = com.RawData[1].ToString();
		commonProps.transform.Find("Raw2").GetComponent<InputField>().text = com.RawData[2].ToString();
		commonProps.transform.Find("Raw3").GetComponent<InputField>().text = com.RawData[3].ToString();
		commonProps.transform.Find("Raw4").GetComponent<InputField>().text = com.RawData[4].ToString();
		commonProps.transform.Find("Raw5").GetComponent<InputField>().text = com.RawData[5].ToString();
		commonProps.transform.Find("Raw6").GetComponent<InputField>().text = com.RawData[6].ToString();
		commonProps.transform.Find("Raw7").GetComponent<InputField>().text = com.RawData[7].ToString();
		commonProps.transform.Find("Raw8").GetComponent<InputField>().text = com.RawData[8].ToString();
		commonProps.transform.Find("Raw9").GetComponent<InputField>().text = com.RawData[9].ToString();
		commonProps.transform.Find("RawA").GetComponent<InputField>().text = com.RawData[10].ToString();
		if(StaticObject.IsMonster(id))
			addMonsterEditor(id, list.Content.transform);
		else if (StaticObject.IsWeapon(id) && id < 16)
		{
			GameObject weaponProps = Instantiate(WeaponEditorPrefab, list.Content.transform);
			WeaponData wd = MapCreator.ObjectData.WeaponData[id];
			setInputProperty(weaponProps.transform, "Slash", wd.Slash);
			setInputProperty(weaponProps.transform, "Bash", wd.Bash);
			setInputProperty(weaponProps.transform, "Stab", wd.Stab);
			setInputProperty(weaponProps.transform, "Unk1", wd.Unk1);
			setInputProperty(weaponProps.transform, "Unk2", wd.Unk2);
			setInputProperty(weaponProps.transform, "Unk3", wd.Unk3);
			setInputProperty(weaponProps.transform, "Durability", wd.Durability);
			setInputProperty(weaponProps.transform, "Skill", wd.Skill);
		}
	}
	private void addBasicMonsterEditor(int id, Transform parent)
	{
		GameObject monsterProps = Instantiate(MonsterEditorBasicPrefab, parent);
		MonsterData md = MapCreator.ObjectData.MonsterData[id - 64];
		setInputProperty(monsterProps.transform, "Level", md.Level, (i) => md.Level = i, 0, 255);
		setInputProperty(monsterProps.transform, "Unk0_1", md.Unk0_1, (i) => md.Unk0_1 = i, 0, 255);
		setInputProperty(monsterProps.transform, "Unk0_2", md.Unk0_2, (i) => md.Unk0_2 = i, 0, 255);
		setInputProperty(monsterProps.transform, "Unk0_3", md.Unk0_3, (i) => md.Unk0_3 = i, 0, 255);
		setInputProperty(monsterProps.transform, "Health", md.Health, (i) => md.Health = i, 0, 65535);
		setInputProperty(monsterProps.transform, "Attack", md.Attack, (i) => md.Attack = i, 0, 255);
		setInputProperty(monsterProps.transform, "Passiveness", md.Passiveness, (i) => md.Passiveness = i, 0, 255);
		setInputProperty(monsterProps.transform, "Speed", md.Speed, (i) => md.Speed = i, 0, 255);
		setInputProperty(monsterProps.transform, "Poison", md.Poison, (i) => md.Poison = i, 0, 255);
		setInputProperty(monsterProps.transform, "EquipmentDamage", md.EquipmentDamage, (i) => md.EquipmentDamage = i, 0, 255);
		setInputProperty(monsterProps.transform, "Attack1Value", md.Attack1Value, (i) => md.Attack1Value = i, 0, 255);
		setInputProperty(monsterProps.transform, "Attack1Damage", md.Attack1Damage, (i) => md.Attack1Damage = i, 0, 255);
		setInputProperty(monsterProps.transform, "Attack1Chance", md.Attack1Chance, (i) => md.Attack1Chance = i, 0, 255);
		setInputProperty(monsterProps.transform, "Attack2Value", md.Attack2Value, (i) => md.Attack2Value = i, 0, 255);
		setInputProperty(monsterProps.transform, "Attack2Damage", md.Attack2Damage, (i) => md.Attack2Damage = i, 0, 255);
		setInputProperty(monsterProps.transform, "Attack2Chance", md.Attack2Chance, (i) => md.Attack2Chance = i, 0, 255);
		setInputProperty(monsterProps.transform, "Attack3Value", md.Attack3Value, (i) => md.Attack3Value = i, 0, 255);
		setInputProperty(monsterProps.transform, "Attack3Damage", md.Attack3Damage, (i) => md.Attack3Damage = i, 0, 255);
		setInputProperty(monsterProps.transform, "Attack3Chance", md.Attack3Chance, (i) => md.Attack3Chance = i, 0, 255);
		setInputProperty(monsterProps.transform, "Experience", md.Experience, (i) => md.Experience = i, 0, 65535);
		setDropdownProperty(monsterProps.transform, "Remains", md.Remains, MonsterData.GetRemains(), (i) => md.Remains = i);
		setDropdownProperty(monsterProps.transform, "HitDecal", md.HitDecal, MonsterData.GetHitDecals(), (i) => md.HitDecal = i);
		setDropdownProperty(monsterProps.transform, "MonsterType", md.MonsterType, MonsterData.GetMonsterTypes(), (i) => md.MonsterType = i);
		setDropdownProperty(monsterProps.transform, "Species", md.OwnerType, MapCreator.StringData.GetRaces(), (i) => md.OwnerType = i);
		setDropdownProperty(monsterProps.transform, "MonsterSprite", md.SpriteID, MapCreator.ObjectData.MonsterSpriteNames, (i) => md.SpriteID = i);
		setDropdownProperty(monsterProps.transform, "Palette", md.AuxPalette, MapCreator.TextureData.GetAuxPalettes(), (i) => md.AuxPalette = i);
		setInventoryProperty(monsterProps.transform, "Inv1", md.Inventory[0], md.InventoryInfo[0], false, (i) => md.Inventory[0] = i, (i) => md.InventoryInfo[0] = i);
		setInventoryProperty(monsterProps.transform, "Inv2", md.Inventory[1], md.InventoryInfo[1], false, (i) => md.Inventory[1] = i, (i) => md.InventoryInfo[1] = i);
		setInventoryProperty(monsterProps.transform, "Inv3", md.Inventory[2], md.InventoryInfo[2], true, (i) => md.Inventory[2] = i, (i) => md.InventoryInfo[2] = i);
		setInventoryProperty(monsterProps.transform, "Inv4", md.Inventory[3], md.InventoryInfo[3], true, (i) => md.Inventory[3] = i, (i) => md.InventoryInfo[3] = i);
	}
	private void addBasicWeaponEditor(int id, Transform parent)
	{
		GameObject weaponProps = Instantiate(WeaponEditorBasicPrefab, parent);
		WeaponData wd = MapCreator.ObjectData.WeaponData[id];
		setInputProperty(weaponProps.transform, "Slash", wd.Slash, (i) => wd.Slash = i, 0, 255);
		setInputProperty(weaponProps.transform, "Bash", wd.Bash, (i) => wd.Bash = i, 0, 255);
		setInputProperty(weaponProps.transform, "Stab", wd.Stab, (i) => wd.Stab = i, 0, 255);
		setInputProperty(weaponProps.transform, "Unk1", wd.Unk1, (i) => wd.Unk1 = i, 0, 255);
		setInputProperty(weaponProps.transform, "Unk2", wd.Unk2, (i) => wd.Unk2 = i, 0, 255);
		setInputProperty(weaponProps.transform, "Unk3", wd.Unk3, (i) => wd.Unk3 = i, 0, 255);
		setInputProperty(weaponProps.transform, "Durability", wd.Durability, (i) => wd.Durability = i, 0, 255);
		setDropdownProperty(weaponProps.transform, "Skill", wd.Skill, WeaponData.GetSkills(), (i) => wd.Skill = i);
	}
	private void addProjectileEditor(int id, Transform parent)
	{
		GameObject projProps = Instantiate(ProjectileEditorPrefab, parent);
		ProjectileData pd = MapCreator.ObjectData.ProjectileData[id - 16];
		setInputProperty(projProps.transform, "Damage", pd.Damage, (i) => pd.Damage = i, 0, 127);
		setInputProperty(projProps.transform, "Unk1", pd.Unk1, (i) => pd.Unk1 = i, 0, 511);
		setInputProperty(projProps.transform, "Unk2", pd.Unk2, (i) => pd.Unk2 = i, 0, 255);
	}
	private void addRangedEditor(int id, Transform parent)
	{
		GameObject rangedProps = Instantiate(RangedEditorPrefab, parent);
		RangedData rd = MapCreator.ObjectData.RangedData[id - 24];
		setInputProperty(rangedProps.transform, "Ammo", rd.Ammo, (i) => rd.Ammo = i, 0, 255);
		setInputProperty(rangedProps.transform, "Unk1", rd.Unk1, (i) => rd.Unk1 = i, 0, 255);
	}
	private void addArmourEditor(int id, Transform parent)
	{
		GameObject armorProps = Instantiate(ArmourEditorPrefab, parent);
		ArmourData ad = MapCreator.ObjectData.ArmourData[id - 32];
		setInputProperty(armorProps.transform, "Protection", ad.Protection, (i) => ad.Protection = i, 0, 255);
		setInputProperty(armorProps.transform, "Durability", ad.Durability, (i) => ad.Durability = i, 0, 255);
		setInputProperty(armorProps.transform, "Unk1", ad.Unk1, (i) => ad.Unk1 = i, 0, 255);
		setDropdownProperty(armorProps.transform, "Type", ad.Type, ArmourData.GetArmourTypes(), (i) => ad.Type = i);
	}
	private void addContainerEditor(int id, Transform parent)
	{
		GameObject contProps = Instantiate(ContainerEditorPrefab, parent);
		ContainerData cd = MapCreator.ObjectData.ContainerData[id - 128];
		setInputProperty(contProps.transform, "Capacity", cd.Capacity, (i) => cd.Capacity = i, 0, 255);
		setInputProperty(contProps.transform, "Slots", cd.Slots, (i) => cd.Slots = i, 0, 255);
		setDropdownProperty(contProps.transform, "Type", cd.Type, ContainerData.GetContainerTypes(), (i) => cd.Type = i);
	}
	private void addLightEditor(int id, Transform parent)
	{
		GameObject lightProps = Instantiate(LightEditorPrefab, parent);
		LightData ld = MapCreator.ObjectData.LightData[id - 144];
		setInputProperty(lightProps.transform, "Brightness", ld.Brightness, (i) => ld.Brightness = i);
		setInputProperty(lightProps.transform, "Duration", ld.Duration, (i) => ld.Duration = i);
	}
	private void addMonsterEditor(int id, Transform parent)
	{
		GameObject monsterProps = Instantiate(MonsterEditorPrefab, parent);
		MonsterData md = MapCreator.ObjectData.MonsterData[id - 64];
		setInputProperty(monsterProps.transform, "Level", md.Level);
		setInputProperty(monsterProps.transform, "Unk0_1", md.Unk0_1);
		setInputProperty(monsterProps.transform, "Unk0_2", md.Unk0_2);
		setInputProperty(monsterProps.transform, "Unk0_3", md.Unk0_3);
		setInputProperty(monsterProps.transform, "Health", md.Health);
		setInputProperty(monsterProps.transform, "Attack", md.Attack);
		setInputProperty(monsterProps.transform, "Unk1", md.Unk1);
		setInputProperty(monsterProps.transform, "Remains", md.HitDecal);
		setInputProperty(monsterProps.transform, "OwnerType", md.OwnerType);
		setInputProperty(monsterProps.transform, "Passiveness", md.Passiveness);
		setInputProperty(monsterProps.transform, "Unk2", md.Unk2);
		setInputProperty(monsterProps.transform, "Speed", md.Speed);
		setInputProperty(monsterProps.transform, "Unk3", md.Unk3);
		setInputProperty(monsterProps.transform, "Poison", md.Poison);
		setInputProperty(monsterProps.transform, "MonsterType", md.MonsterType);
		setInputProperty(monsterProps.transform, "EquipmentDamage", md.EquipmentDamage);
		setInputProperty(monsterProps.transform, "Unk4", md.Unk4);
		setInputProperty(monsterProps.transform, "Attack1Value", md.Attack1Value);
		setInputProperty(monsterProps.transform, "Attack1Damage", md.Attack1Damage);
		setInputProperty(monsterProps.transform, "Attack1Chance", md.Attack1Chance);
		setInputProperty(monsterProps.transform, "Attack2Value", md.Attack2Value);
		setInputProperty(monsterProps.transform, "Attack2Damage", md.Attack2Damage);
		setInputProperty(monsterProps.transform, "Attack2Chance", md.Attack2Chance);
		setInputProperty(monsterProps.transform, "Attack3Value", md.Attack3Value);
		setInputProperty(monsterProps.transform, "Attack3Damage", md.Attack3Damage);
		setInputProperty(monsterProps.transform, "Attack3Chance", md.Attack3Chance);
		setInputProperty(monsterProps.transform, "Inventory1", md.Inventory[0]);
		setInputProperty(monsterProps.transform, "Inventory2", md.Inventory[1]);
		setInputProperty(monsterProps.transform, "Inventory3", md.Inventory[2]);
		setInputProperty(monsterProps.transform, "Inventory4", md.Inventory[3]);
		setInputProperty(monsterProps.transform, "Experience", md.Experience);
		setInputProperty(monsterProps.transform, "Unk5", md.Unk5);
		setInputProperty(monsterProps.transform, "Unk6", md.Unk6);
		setInputProperty(monsterProps.transform, "Unk7", md.Unk7);
		setInputProperty(monsterProps.transform, "Unk8", md.Unk8);
		setInputProperty(monsterProps.transform, "Unk9", md.Unk9);
		setInputProperty(monsterProps.transform, "Unk10", md.Unk10);
	}
	public void CreateBasicCommonPropertiesEditor(int id)
	{
		Vector2 before = Vector2.zero;
		if (listMenu)
		{
			before = listMenu.GetComponent<RectTransform>().position;
			Destroy(listMenu);
		}

		listMenu = Instantiate(ObjectEditorPrefab, transform);
		ListMenu list = listMenu.GetComponent<ListMenu>();
		list.Title.text = "Object properties";
		list.Information.text = "";
		if (before != Vector2.zero)
			listMenu.GetComponent<RectTransform>().position = before;

		RectTransform listRT = listMenu.GetComponent<RectTransform>();

		CommonData com = MapCreator.ObjectData.CommonData[id];
		GameObject commonProps = Instantiate(CommonPropertiesBasicPrefab, list.Content.transform);
		commonProps.transform.Find("Sprite").GetComponent<Image>().sprite = MapCreator.GetObjectSpriteFromID(id);
		Button spriteBut = commonProps.transform.Find("Sprite").GetComponent<Button>();
		Action<Texture2D> changeSprAct = (newTex) => spriteBut.GetComponent<Image>().sprite = Sprite.Create(newTex, new Rect(0, 0, newTex.width, newTex.height), new Vector2(0.5f, 0.5f));
		Action<int> loadNewTexAct = (i) => LoadTextureAction(TextureType.Object, i, id, changeSprAct);
		Action spawnTexPickAct = () => CreateTexturePicker(GetTextures(TextureType.Object, true), loadNewTexAct, "Object sprites", "Select new object sprite");
		spriteBut.onClick.AddListener(() => spawnTexPickAct());
		commonProps.transform.Find("Name").GetComponent<Text>().text = StaticObject.GetName(id);
		StringBlock sb = MapCreator.StringData.GetObjectNameBlock();
		InputField nameInput = setInputProperty(commonProps.transform, "NameInput", sb.Strings[id], (str) => sb.Strings[id] = str);

		setInputProperty(commonProps.transform, "Mass", com.Mass, (i) => com.Mass = i, 0, 2047);
		setInputProperty(commonProps.transform, "Value", com.Value, (i) => com.Value = i, 0, 65535);
		setInputProperty(commonProps.transform, "Height", com.Height, (i) => com.Height = i, 0, 255);
		setToggleProperty(commonProps.transform, "PickupFlag", com.PickupFlag == 1, (b) => com.PickupFlag = Convert.ToInt32(b));
		Toggle qualTog = setToggleProperty(commonProps.transform, "QualityToggle", com.QualityType != 15);
		Dropdown qualDrop = setDropdownProperty(commonProps.transform, "QualityDrop", 0);
		if(com.QualityType == 15)
			qualDrop.interactable = false;
		else
			qualDrop.value = com.QualityType;
		Action<bool> setQuality = (b) =>
		{
			if(b)
			{
				qualDrop.interactable = true;
				com.QualityType = 0;
				qualDrop.value = 0;
			}
			else
			{
				qualDrop.interactable = false;
				com.QualityType = 15;
			}
		};
		Action<int> changeQualAct = (i) => com.QualityType = i;
		qualDrop.onValueChanged.AddListener((i) => changeQualAct(i));
		qualTog.onValueChanged.AddListener((b) => setQuality(b));
		Toggle talTog = setToggleProperty(commonProps.transform, "TalismanToggle", com.ObjectType == 10);
		if (com.ObjectType == 0 || com.ObjectType == 10)
		{
			Action<bool> setTalismanAct = (b) =>
			{
				if (b)
					com.ObjectType = 10;
				else
					com.ObjectType = 0;
			};
			talTog.onValueChanged.AddListener((b) => setTalismanAct(b));
		}
		else
			talTog.interactable = false;

		if (StaticObject.IsMonster(id))
		{
			addBasicMonsterEditor(id, list.Content.transform);
			listRT.sizeDelta = new Vector2(listRT.sizeDelta.x, listRT.sizeDelta.y + 340.0f);
		}
		else if (StaticObject.IsMelee(id))
		{
			addBasicWeaponEditor(id, list.Content.transform);
			listRT.sizeDelta = new Vector2(listRT.sizeDelta.x, listRT.sizeDelta.y + 140.0f);
		}
		else if (StaticObject.IsRanged(id) || StaticObject.IsSpecialRanged(id))
		{
			addRangedEditor(id, list.Content.transform);
			listRT.sizeDelta = new Vector2(listRT.sizeDelta.x, listRT.sizeDelta.y + 80.0f);
		}
		else if (StaticObject.IsProjectile(id) || StaticObject.IsSpecialProjectile(id))
		{
			addProjectileEditor(id, list.Content.transform);
			listRT.sizeDelta = new Vector2(listRT.sizeDelta.x, listRT.sizeDelta.y + 80.0f);
		}
		else if (StaticObject.IsArmour(id) || StaticObject.IsBauble(id) || StaticObject.IsShield(id) || StaticObject.IsSpecialBauble(id))
		{
			addArmourEditor(id, list.Content.transform);
			listRT.sizeDelta = new Vector2(listRT.sizeDelta.x, listRT.sizeDelta.y + 80.0f);
		}
		else if (StaticObject.IsContainer(id))
		{
			addContainerEditor(id, list.Content.transform);
			listRT.sizeDelta = new Vector2(listRT.sizeDelta.x, listRT.sizeDelta.y + 40.0f);
		}
		else if (StaticObject.IsLight(id))
		{
			addLightEditor(id, list.Content.transform);
			listRT.sizeDelta = new Vector2(listRT.sizeDelta.x, listRT.sizeDelta.y + 40.0f);
		}
	}
	public void CreateTerrainEditor(int id, TextureType texType)
	{
		Vector2 before = Vector2.zero;
		if (listMenu)
		{
			before = listMenu.GetComponent<RectTransform>().position;
			Destroy(listMenu);
		}

		listMenu = Instantiate(ObjectEditorPrefab, transform);
		ListMenu list = listMenu.GetComponent<ListMenu>();
		list.Title.text = "Terrain editor";
		list.Information.text = "";
		if (before != Vector2.zero)
			listMenu.GetComponent<RectTransform>().position = before;

		GameObject terrProps = Instantiate(TerrainEditorPrefab, list.Content.transform);
		Texture2D tex = MapCreator.GetTexture(texType, id);
		terrProps.transform.Find("Name").GetComponent<Text>().text = "Texture ID : " + id;
		terrProps.transform.Find("Texture").GetComponent<Image>().sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
		StringBlock sb = MapCreator.StringData.GetWallFloorBlock();
		int descIndex = texType == TextureType.Wall ? id : sb.Strings.Count - 2 - id;
		setInputProperty(terrProps.transform, "Description", sb.Strings[descIndex], (str) => sb.Strings[descIndex] = str);
		int[] typeArray = texType == TextureType.Wall ? MapCreator.TextureData.WallTypes : MapCreator.TextureData.FloorTypes;
		setDropdownProperty(terrProps.transform, "Type", typeArray[id], MapCreator.TextureData.GetTerrainTypes(), (i) => typeArray[id] = i);
	}
	private void setInventoryProperty(Transform editor, string name, int invValue, int infoValue, bool chance, Action<int> inventoryAct, Action<int> infoAct)
	{
		Button but = editor.Find(name).GetComponent<Button>();
		Toggle tog = editor.Find(name + "Tog").GetComponent<Toggle>();
		Slider sl = null;
		if (chance)
			sl = editor.Find(name + "Chance").GetComponent<Slider>();
		Action<bool> setInvAct = (b) =>
		{
			if(b)
			{
				but.interactable = true;
				if (sl)
					sl.interactable = true;
				inventoryAct(0);
				infoAct(1);
				but.GetComponent<Image>().sprite = MapCreator.GetObjectSpriteFromID(0);
			}
			else
			{
				but.interactable = false;
				if (sl)
					sl.interactable = false;
				inventoryAct(0);
				infoAct(0);
				but.GetComponent<Image>().sprite = null;
			}
		};
		tog.onValueChanged.AddListener((b) => setInvAct(b));
		if (infoValue > 0)
		{
			tog.isOn = true;
			but.GetComponent<Image>().sprite = MapCreator.GetObjectSpriteFromID(invValue);
			if (chance)
				sl.value = infoValue;
		}
		else
		{
			tog.isOn = false;
		}
		if(chance)
		{
			sl.onValueChanged.AddListener((f) => infoAct((int)f));
		}
		Action<int> changeInvAct = (i) =>
		{
			but.GetComponent<Image>().sprite = MapCreator.GetObjectSpriteFromID(i);
			inventoryAct(i);
		};
		int max = chance ? 128 : 256;
		but.onClick.AddListener(() => CreateSpecifiedItemList(changeInvAct, StaticObject.GetIDs(max), "Object list", "Select new inventory"));
	}
	private InputField setInputProperty(Transform editor, string name, int value, Action<int> changeAct = null, int min = 0, int max = 128)
	{
		InputField input = editor.Find(name).GetComponent<InputField>();
		input.text = value.ToString();
		Action<string> clampVals = (str) =>
		{
			int val = int.Parse(str);
			val = Mathf.Clamp(val, min, max);
			input.text = val.ToString();
		};
		input.onEndEdit.AddListener((str) => clampVals(str));
		if (changeAct != null)
			input.onValueChanged.AddListener((str) => changeAct(int.Parse(str)));
		return input;
	}
	private InputField setInputProperty(Transform editor, string name, string value, Action<string> changeAct = null)
	{
		InputField input = editor.Find(name).GetComponent<InputField>();
		input.text = value.ToString();
		if (changeAct != null)
			input.onValueChanged.AddListener((str) => changeAct(str));
		return input;
	}
	private Toggle setToggleProperty(Transform editor, string name, bool value, Action<bool> changeAct = null)
	{
		Toggle toggle = editor.Find(name).GetComponent<Toggle>();
		toggle.isOn = value;
		if (changeAct != null)
			toggle.onValueChanged.AddListener((b) => changeAct(b));
		return toggle;
	}
	private Dropdown setDropdownProperty(Transform editor, string name, int value, Action<int> changeAct = null)
	{
		Dropdown dropdown = editor.Find(name).GetComponent<Dropdown>();
		dropdown.value = value;
		if (changeAct != null)
			dropdown.onValueChanged.AddListener((i) => changeAct(i));
		return dropdown;
	}
	private Dropdown setDropdownProperty(Transform editor, string name, int value, Dictionary<string, int> dict, Action<int> act = null)
	{
		Dropdown drop = setDropdownProperty(editor, name, 0);
		List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();
		foreach (var key in dict.Keys)
			options.Add(new Dropdown.OptionData(key));
		drop.options = options;
		int index = 0;
		foreach (var val in dict.Values)
		{
			if (val == value)
				break;
			index++;
		}
		drop.value = index;
		if (act != null)
			drop.onValueChanged.AddListener((i) => act(dict[drop.options[i].text]));
		return drop;
	}
	private Dropdown setDropdownProperty(Transform editor, string name, int value, List<string> list, Action<int> act = null)
	{
		Dropdown drop = setDropdownProperty(editor, name, 0);
		List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();
		foreach (var item in list)		
			options.Add(new Dropdown.OptionData(item));
		drop.options = options;
		drop.value = value;
		if (act != null)
			drop.onValueChanged.AddListener((i) => act(i));
		return drop;
	}
	private Dropdown setDropdownProperty(Transform editor, string name, int value, string[] array, Action<int> act = null)
	{
		Dropdown drop = setDropdownProperty(editor, name, 0);
		List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();
		foreach (var item in array)
			options.Add(new Dropdown.OptionData(item));
		drop.options = options;
		drop.value = value;
		if (act != null)
			drop.onValueChanged.AddListener((i) => act(i));
		return drop;
	}
	#endregion

	#region Selection

	public bool SelectObject(StaticObject so, bool centerCamera)
	{
		DeselectObjectAndSetProperties(so);
		if (centerCamera)
			SetCameraToObject(so);
		if (MapCreator.ObjectToGO.ContainsKey(so))
		{
			GameObject go = MapCreator.ObjectToGO[so];
			OnSelectObject(go);
			GameObject col = go.GetComponentInChildren<Collider>().gameObject;
			SetSpriteOutline(col, true, Color.green);
			return true;
		}
		else
			return false;
	}

	public void SetTileProperties(GameObject obj, bool activate)
	{
		//Debug.Log("Set tile properties");
		if (activate)
		{
			TilePropertiesObject.SetActive(true);
			TileProperties tp = TilePropertiesObject.GetComponent<TileProperties>();
			MapTileScript mts = obj.GetComponent<MapTileScript>();
			if (tp && mts)
			{
				tp.SetTile(mts.MapTile);
			}
		}
		else
		{
			TilePropertiesObject.SetActive(false);
		}
	}

	public void SetObjectProperties(GameObject obj, bool activate)
	{
		if (activate)
		{
			StaticObjectScript sos = obj.GetComponent<StaticObjectScript>();
			if (sos)
				SetObjectProperties(sos.StaticObject);
		}
		else
			ObjectProperties.SetActive(false);
	}
	public void DeselectObjectAndSetProperties(StaticObject so)
	{
		OnSelectNonGO();	//InputManager.SelectedObject = null
		SetObjectProperties(so);
	}
	public void SetObjectProperties(StaticObject so)
	{
		ObjectProperties.SetActive(true);
		PropertiesPanel pp = ObjectProperties.GetComponent<PropertiesPanel>();
		pp.SetObject(so);
		pp.SetBasicPanel(so);
		
	}

	public void SetTilePanel(GameObject obj, bool activate, GameObject sel)
	{
		if (TilePanelObject.activeSelf != activate)
			TilePanelObject.SetActive(activate);
		if (activate)
		{
			TilePanel tp = TilePanelObject.GetComponent<TilePanel>();
			MapTileScript mts = obj.transform.parent.GetComponent<MapTileScript>();
			if (tp && mts)
			{
				tp.SetTile(mts.MapTile, CurrentLevel);
				SetTileOutline(obj, true);
			}
		}
		else
		{
			if (obj.transform.parent && obj.transform.parent.gameObject == sel)
				SetTileOutline(obj, true, Color.green);
			else
				SetTileOutline(obj, false);
		}
	}

	public void SetTileOutline(GameObject obj, bool activate, Color? col = null)
	{
		MapTileScript mts = obj.transform.parent.GetComponent<MapTileScript>();
		if (mts)
		{
			MeshRenderer floor = mts.FloorObject.GetComponent<MeshRenderer>();
			MeshRenderer wall = null;
			if (mts.WallObject)
				wall = mts.WallObject.GetComponent<MeshRenderer>();
			if (activate)
			{
				if (floor)
				{
					floor.material.SetFloat("_OutlineSize", 4.0f);
					if (col != null)
						floor.material.SetColor("_OutlineColor", (Color)col);
					else
						floor.material.SetColor("_OutlineColor", Color.white);
				}
				if (wall)
				{
					wall.material.SetFloat("_OutlineSize", 4.0f);
					if (col != null)
						wall.material.SetColor("_OutlineColor", (Color)col);
					else
						wall.material.SetColor("_OutlineColor", Color.white);
				}
			}
			else
			{
				if (floor)
				{
					floor.material.SetFloat("_OutlineSize", 0);
				}
				if (wall)
				{
					wall.material.SetFloat("_OutlineSize", 0);
				}
			}
		}
	}
	public void SetStaticObjectPanel(GameObject obj, bool activate, GameObject sel)
	{
		StaticObjectPanelObject.SetActive(activate);
		StaticObjectPanel sop = StaticObjectPanelObject.GetComponent<StaticObjectPanel>();
		StaticObjectScript sos = obj.transform.parent.GetComponent<StaticObjectScript>();
		if (activate)
		{
			if (sop && sos)
			{
				sop.SetStaticObject(sos.StaticObject);
			}
			SetSpriteOutline(obj, true);
		}
		else
		{
			sop.ClearObjectList(sop.NextObjectChainPanel.transform);
			sop.ClearObjectList(sop.ContainerPanel.transform, true);
			//Debug.LogFormat("Moving out of object {0}, parent {1}", obj, obj.transform.parent);
			if (obj.transform.parent && obj.transform.parent.gameObject == sel)
			{
				SetSpriteOutline(obj, true, Color.green);
			}
			else
				SetSpriteOutline(obj, false);
		}
	}

	public void SetSpriteOutline(GameObject obj, bool activate, Color? col = null)
	{
		SpriteOutline outline = obj.transform.parent.GetComponent<SpriteOutline>();
		if (outline)
		{
			if (activate)
			{
				outline.OutlineObject.SetActive(true);

				if (col != null)
				{
					Material currentMat = outline.OutlineObject.GetComponent<SpriteRenderer>().material;
					currentMat.SetColor("_Color", (Color)col);
				}
				else
				{
					Material currentMat = outline.OutlineObject.GetComponent<SpriteRenderer>().material;
					currentMat.SetColor("_Color", Color.white);
				}
			}
			else
				outline.OutlineObject.SetActive(false);
		}
	}

	#endregion

	#region Create Object Menus

	public void CreateObject(int id, int level, Vector2Int tilePos, MapTile tile, Action<StaticObject> afterCreatingAct = null)
	{
		ObjectType type = StaticObject.GetObjectType(id);
		if (type == ObjectType.Monster)
			createMonster(id, level, tilePos, tile, afterCreatingAct);
		else if (type == ObjectType.Gold)
			createGold(id, level, tilePos, tile, afterCreatingAct);
		else if (type == ObjectType.Food || type == ObjectType.Light)
			createFoodLight(id, level, tilePos, tile, afterCreatingAct);
		else if (type == ObjectType.Drink)
			createDrink(id, level, tilePos, tile, afterCreatingAct);
		else if (type == ObjectType.Weapon || type == ObjectType.Melee || type == ObjectType.Ranged || type == ObjectType.SpecialRanged)
			createWeapon(id, level, tilePos, tile, afterCreatingAct);
		else if (type == ObjectType.Projectile)
			createAmmo(id, level, tilePos, tile, afterCreatingAct);
		else if (type == ObjectType.Armour || type == ObjectType.Baubles || type == ObjectType.Shield || type == ObjectType.SpecialBauble || type == ObjectType.SpecialShield)
			createWearable(id, level, tilePos, tile, afterCreatingAct);
		else if (type == ObjectType.Potion)
			createPotion(id, level, tilePos, tile, afterCreatingAct);
		else if (type == ObjectType.Treasure)
			createTreasure(id, level, tilePos, tile, afterCreatingAct);
		else if (type == ObjectType.Wand)
			createWand(id, level, tilePos, tile, afterCreatingAct);
		else if (type == ObjectType.Bones)
			createBones(id, level, tilePos, tile, afterCreatingAct);
		else if (type == ObjectType.Key)
			createKey(id, level, tilePos, tile, afterCreatingAct);
		else if (type == ObjectType.Scroll)
			createScroll(id, level, tilePos, tile, afterCreatingAct);
		else if (type == ObjectType.Book)
			createBook(id, level, tilePos, tile, afterCreatingAct);
		else if (type == ObjectType.Countable)
			createCountable(id, level, tilePos, tile, afterCreatingAct);
		else if (type == ObjectType.Uncountable)
			createUncountable(id, level, tilePos, tile, afterCreatingAct);
		else if (id == 302)
			createFountain(id, level, tilePos, tile, afterCreatingAct);
		else if (type == ObjectType.CeilingHugger)
			createCeilingHugger(id, level, tilePos, tile, afterCreatingAct);
		else if (type == ObjectType.Lever)
			createLever(id, level, tilePos, tile, afterCreatingAct);
		else if (type == ObjectType.Door)
			createDoor(id, level, tilePos, tile, afterCreatingAct);
		else if (type == ObjectType.Lock)
			createLock(id, level, tilePos, tile, afterCreatingAct);
		else if (type == ObjectType.VerticalTexture)
			createVertTexture(id, level, tilePos, tile, afterCreatingAct);
		else if (type == ObjectType.Writing)
			createWriting(id, level, tilePos, tile, afterCreatingAct);
		else if (type == ObjectType.Grave)
			createGrave(id, level, tilePos, tile, afterCreatingAct);
		else if (type == ObjectType.DialLever)
			createDialLever(id, level, tilePos, tile, afterCreatingAct);
		else if (id == 356)
			createBridge(id, level, tilePos, tile, afterCreatingAct);
		else if (id == 352)
			createPillar(id, level, tilePos, tile, afterCreatingAct);
		else if (type == ObjectType.Model)
			createModel(id, level, tilePos, tile, afterCreatingAct);
		else if (id == 384)
			createDamageTrap(id, level, tilePos, tile, afterCreatingAct);
		else if (id == 385)
			createTeleportTrap(id, level, tilePos, tile, afterCreatingAct);
		else if (id == 386)
			createArrowTrap(id, level, tilePos, tile, afterCreatingAct);
		else if (id == 388)
			createPitTrap(id, level, tilePos, tile, afterCreatingAct);
		else if (id == 389)
			createTerrainTrap(id, level, tilePos, tile, afterCreatingAct);
		else if (id == 390)
			createSpelltrap(id, level, tilePos, tile, afterCreatingAct);
		else if (id == 391)
			createCreateObjectTrap(id, level, tilePos, tile, afterCreatingAct);
		else if (id == 392)
			createDoorTrap(id, level, tilePos, tile, afterCreatingAct);
		else if (id == 395)
			createDeleteObjectTrap(id, level, tilePos, tile, afterCreatingAct);
		else if (id == 396)
			createInventoryTrap(id, level, tilePos, tile, afterCreatingAct);
		else if (id == 397)
			createVarTrap(id, level, tilePos, tile, afterCreatingAct);
		else if (id == 398)
			createCheckTrap(id, level, tilePos, tile, afterCreatingAct);
		else if (id == 400)
			createTextTrap(id, level, tilePos, tile, afterCreatingAct);
		else if (id == 416)
			createMoveTrigger(id, level, tilePos, tile, afterCreatingAct);
		else if (id == 417)
			createPickUpTrigger(id, level, tilePos, tile, afterCreatingAct);
		else if (id == 418)
			createUseTrigger(id, level, tilePos, tile, afterCreatingAct);
		else if (id == 419)
			createLookTrigger(id, level, tilePos, tile, afterCreatingAct);
		else if (id == 421)
			createOpenTrigger(id, level, tilePos, tile, afterCreatingAct);
		else if (id == 422)
			createUnlockTrigger(id, level, tilePos, tile, afterCreatingAct);
		else if (id == 465)
			createAttitudeTrap(387, level, tilePos, tile, afterCreatingAct);
		else if (id == 466)
			createPlatformTrap(387, level, tilePos, tile, afterCreatingAct);
		else if (id == 467)
			createCameraTrap(387, level, tilePos, tile, afterCreatingAct);
		else if (id == 468)
			createConversationTrap(387, level, tilePos, tile, afterCreatingAct);
		else if (id == 469)
			createEndGameTrap(387, level, tilePos, tile, afterCreatingAct);
		else
		{
			StaticObject so = MapCreator.AddObject(tile, tilePos, level, id);
			afterCreatingAct?.Invoke(so);
		}
	}
	private void createFountain(int id, int level, Vector2Int tilePos, MapTile tile, Action<StaticObject> afterCreatingAct = null)
	{
		createMenuGO = Instantiate(CreateObjectPrefab, transform);
		createMenuGO.transform.Find("TopBar/Title").gameObject.GetComponent<Text>().text = "Create " + StaticObject.GetName(id);
		GameObject mainPanelGO = createMenuGO.transform.Find("MainPanel").gameObject;

		Toggle isEnchanted = addToggleToCreateMenu("Is enchanted", mainPanelGO.transform);
		Dictionary<string, int> dict = StaticObject.GetFountainEnchants();
		Dropdown enchDrop = addDropdownToCreateMenu("Enchantment", mainPanelGO.transform, dict, 7);
		enchDrop.interactable = false;
		isEnchanted.onValueChanged.AddListener((b) => enchDrop.interactable = b);		
		Action create = () =>
		{
			StaticObject so = MapCreator.AddObject(tile, tilePos, level, id);
			int spec = isEnchanted.isOn ? dict[enchDrop.options[enchDrop.value].text] : 1;
			so.SetProperties(isEnchanted.isOn, false, false, true, 0, 40, 0, spec);
			StaticObject water = MapCreator.AddObject(tile, tilePos, level, 457);
			water.SetProperties(false, false, false, true, 0, 40, 5, 1);
			Destroy(createMenuGO);
			afterCreatingAct?.Invoke(so);
		};
		addButtonToCreateMenu("Create", mainPanelGO.transform, create);
	}
	private void createMonster(int id, int level, Vector2Int tilePos, MapTile tile, Action<StaticObject> afterCreatingAct = null)
	{
		createMenuGO = Instantiate(CreateObjectPrefab, transform);
		createMenuGO.transform.Find("TopBar/Title").gameObject.GetComponent<Text>().text = "Create " + StaticObject.GetName(id);
		GameObject mainPanelGO = createMenuGO.transform.Find("MainPanel").gameObject;

		Slider height = addSliderToCreateMenu("Height", mainPanelGO.transform, tile.FloorHeight * 8.0f, 127.0f, tile.FloorHeight * 8.0f);
		Dropdown dir = addDropdownToCreateMenu("Direction", mainPanelGO.transform, new string[] { "N", "NE", "E", "SE", "S", "SW", "W", "NW" }, 0);
		Dropdown att = addDropdownToCreateMenu("Attitude", mainPanelGO.transform, MobileObject.GetAttitudes());
		Dropdown goal = addDropdownToCreateMenu("Goal", mainPanelGO.transform, MobileObject.GetGoals(), 2, 80.0f);
		InputField hp = addInputToCreateMenu("HP", mainPanelGO.transform);
		hp.text = MobileObject.GetDefaultHP(id).ToString();
		Toggle npcTog = addToggleToCreateMenu("Is NPC", mainPanelGO.transform, false);
		Dictionary<string, int> dict = MapCreator.GetNPCIDs();
		Dropdown ids = addDropdownToCreateMenu("NPC name", mainPanelGO.transform, dict, 0, 40.0f);
		ids.interactable = false;
		npcTog.onValueChanged.AddListener((b) => ids.interactable = b);
		Action create = () =>
		{
			MobileObject mo = (MobileObject)MapCreator.AddObject(tile, tilePos, level, id);
			mo.SetProperties(false, false, false, false, 0, tile.Position.x, tile.Position.y, 0);
			mo.ZPos = (int)height.value;
			mo.Direction = dir.value;
			mo.Attitude = att.value;
			mo.Goal = goal.value;
			mo.HP = int.Parse(hp.text);
			if(npcTog.isOn)
				mo.Whoami = dict[ids.options[ids.value].text];
			MapCreator.SetGODirection(mo);
			MapCreator.SetGOHeight(mo);
			Destroy(createMenuGO);
			afterCreatingAct?.Invoke(mo);
		};
		addButtonToCreateMenu("Create", mainPanelGO.transform, create);
	}
	private void createCeilingHugger(int id, int level, Vector2Int tilePos, MapTile tile, Action<StaticObject> afterCreatingAct = null)
	{
		createMenuGO = Instantiate(CreateObjectPrefab, transform);
		createMenuGO.transform.Find("TopBar/Title").gameObject.GetComponent<Text>().text = "Create " + StaticObject.GetName(id);
		GameObject mainPanelGO = createMenuGO.transform.Find("MainPanel").gameObject;
		Toggle tog = addToggleToCreateMenu("Place at ceiling", mainPanelGO.transform, true);
		Action create = () =>
		{
			StaticObject so = MapCreator.AddObject(tile, tilePos, level, id);
			so.SetProperties(false, false, false, true, 0, 40, 0, 1);
			if (tog.isOn)
				so.ZPos = 120;
			MapCreator.SetGOHeight(so);
			Destroy(createMenuGO);
			afterCreatingAct?.Invoke(so);
		};
		addButtonToCreateMenu("Create", mainPanelGO.transform, create);
	}
	private void createAmmo(int id, int level, Vector2Int tilePos, MapTile tile, Action<StaticObject> afterCreatingAct = null)
	{
		createMenuGO = Instantiate(CreateObjectPrefab, transform);
		createMenuGO.transform.Find("TopBar/Title").gameObject.GetComponent<Text>().text = "Create " + StaticObject.GetName(id);
		GameObject mainPanelGO = createMenuGO.transform.Find("MainPanel").gameObject;
		
		Slider quantity = addSliderToCreateMenu("Quantity", mainPanelGO.transform, 1.0f, 63.0f, 1.0f);
		Action create = () =>
		{
			StaticObject so = MapCreator.AddObject(tile, tilePos, level, id);
			so.SetProperties(false, false, false, true, 0, 40, 0, (int)quantity.value);
			Destroy(createMenuGO);
			afterCreatingAct?.Invoke(so);
		};
		addButtonToCreateMenu("Create", mainPanelGO.transform, create);
	}
	private void createCountable(int id, int level, Vector2Int tilePos, MapTile tile, Action<StaticObject> afterCreatingAct = null)
	{
		createMenuGO = Instantiate(CreateObjectPrefab, transform);
		createMenuGO.transform.Find("TopBar/Title").gameObject.GetComponent<Text>().text = "Create " + StaticObject.GetName(id);
		GameObject mainPanelGO = createMenuGO.transform.Find("MainPanel").gameObject;

		Slider quantity = addSliderToCreateMenu("Quantity", mainPanelGO.transform, 1.0f, 63.0f, 1.0f);
		Toggle specTog = addToggleToCreateMenu("Special item", mainPanelGO.transform);
		InputField special = addInputToCreateMenu("Special", mainPanelGO.transform);
		Action<bool> specTogAct = (b) =>
		{
			if(b)
			{
				special.interactable = true;
				quantity.interactable = false;
			}
			else
			{
				special.interactable = false;
				quantity.interactable = true;
			}
		};
		Action create = () =>
		{
			StaticObject so = MapCreator.AddObject(tile, tilePos, level, id);
			int spec = specTog.isOn ? int.Parse(special.text) + 512 : (int)quantity.value;
			so.SetProperties(false, false, false, true, 0, 40, 0, spec);
			Destroy(createMenuGO);
			afterCreatingAct?.Invoke(so);
		};
		addButtonToCreateMenu("Create", mainPanelGO.transform, create);

	}
	private void createUncountable(int id, int level, Vector2Int tilePos, MapTile tile, Action<StaticObject> afterCreatingAct = null)
	{
		createMenuGO = Instantiate(CreateObjectPrefab, transform);
		createMenuGO.transform.Find("TopBar/Title").gameObject.GetComponent<Text>().text = "Create " + StaticObject.GetName(id);
		GameObject mainPanelGO = createMenuGO.transform.Find("MainPanel").gameObject;

		Toggle specTog = addToggleToCreateMenu("Special item", mainPanelGO.transform);
		InputField special = addInputToCreateMenu("Special", mainPanelGO.transform);
		special.interactable = false;
		specTog.onValueChanged.AddListener((b) => special.interactable = b);
		Action create = () =>
		{
			StaticObject so = MapCreator.AddObject(tile, tilePos, level, id);
			int spec = specTog.isOn ? int.Parse(special.text) + 512 : 1;
			so.SetProperties(false, false, false, true, 0, 40, 0, spec);
			Destroy(createMenuGO);
			afterCreatingAct?.Invoke(so);
		};
		addButtonToCreateMenu("Create", mainPanelGO.transform, create);
	}
	private void createDrink(int id, int level, Vector2Int tilePos, MapTile tile, Action<StaticObject> afterCreatingAct = null)
	{
		createMenuGO = Instantiate(CreateObjectPrefab, transform);
		createMenuGO.transform.Find("TopBar/Title").gameObject.GetComponent<Text>().text = "Create " + StaticObject.GetName(id);
		GameObject mainPanelGO = createMenuGO.transform.Find("MainPanel").gameObject;
		Toggle isEnchanted = addToggleToCreateMenu("Is enchanted", mainPanelGO.transform);
		Toggle isTrapped = addToggleToCreateMenu("Is a trap", mainPanelGO.transform);
		Toggle isIdentified = addToggleToCreateMenu("Is identified", mainPanelGO.transform);
		Slider quantity = addSliderToCreateMenu("Quantity", mainPanelGO.transform, 1.0f, 63.0f, 1.0f);
		Dictionary<string, int> enchantments = StaticObject.GetPotionScrollEnchants();
		Dropdown enchDrop = addDropdownToCreateMenu("Effect", mainPanelGO.transform, enchantments, 0, 60.0f);
		Action<bool> isTrappedAct = (b) => enchDrop.interactable = !b;
		isTrapped.onValueChanged.AddListener((b) => isTrappedAct(b));
		enchDrop.interactable = false;
		isTrapped.interactable = false;
		isIdentified.interactable = false;
		Action<bool> isEnchAct = (b) =>
		{
			if(b)
			{
				isTrapped.interactable = true;
				isIdentified.interactable = true;
				enchDrop.interactable = true;
				quantity.interactable = false;
			}
			else
			{
				isTrapped.interactable = false;
				isIdentified.interactable = false;
				enchDrop.interactable = false;
				quantity.interactable = true;
			}
		};
		isEnchanted.onValueChanged.AddListener((b) => isEnchAct(b));
		Action create = () =>
		{
			StaticObject drink = MapCreator.AddObject(tile, tilePos, level, id);
			if (isEnchanted.isOn)
			{
				Action<StaticObject> assignTrap = (trap) =>
				{
					if (!MapCreator.AddTrigger(drink, trap))
						SpawnPopupMessage("Failed to add trap");
					trap.Flags = 0;
				};
				Action<int> addTrap = (i) => CreateObject(i, level, tilePos, tile, assignTrap);
				if (isTrapped.isOn)
				{
					drink.SetProperties(true, false, false, false, 0, 40, 0, 0);
					CreateSpecifiedItemList(addTrap, StaticObject.GetIDsByType(ObjectType.Trap), "Trap list", "Select a trap for potion");
				}
				else
				{
					int e = enchantments[enchDrop.options[enchDrop.value].text];
					drink.SetProperties(true, false, false, true, 4, 40, 0, 512 + e);
				}
				drink.Direction = isIdentified.isOn ? 7 : 0;
			}
			else
				drink.SetProperties(false, false, false, true, 0, 40, 0, (int)quantity.value);
			Destroy(createMenuGO);
			afterCreatingAct?.Invoke(drink);
		};
		addButtonToCreateMenu("Create", mainPanelGO.transform, create);
	}
	private void createFoodLight(int id, int level, Vector2Int tilePos, MapTile tile, Action<StaticObject> afterCreatingAct = null)
	{
		createMenuGO = Instantiate(CreateObjectPrefab, transform);
		createMenuGO.transform.Find("TopBar/Title").gameObject.GetComponent<Text>().text = "Create " + StaticObject.GetName(id);
		GameObject mainPanelGO = createMenuGO.transform.Find("MainPanel").gameObject;
		Slider quality = addSliderToCreateMenu("Quality", mainPanelGO.transform, 1.0f, 63.0f, 40.0f);
		Slider quantity = addSliderToCreateMenu("Quantity", mainPanelGO.transform, 1.0f, 63.0f, 1.0f);
		Action create = () =>
		{
			StaticObject food = MapCreator.AddObject(tile, tilePos, level, id);
			food.SetProperties(false, false, false, true, 0, (int)quality.value, 0, (int)quantity.value);
			Destroy(createMenuGO);
			afterCreatingAct?.Invoke(food);
		};
		addButtonToCreateMenu("Create", mainPanelGO.transform, create);
	}
	private void createWeapon(int id, int level, Vector2Int tilePos, MapTile tile, Action<StaticObject> afterCreatingAct = null)
	{
		createMenuGO = Instantiate(CreateObjectPrefab, transform);
		createMenuGO.transform.Find("TopBar/Title").gameObject.GetComponent<Text>().text = "Create " + StaticObject.GetName(id);
		GameObject mainPanelGO = createMenuGO.transform.Find("MainPanel").gameObject;
		Slider quality = addSliderToCreateMenu("Quality", mainPanelGO.transform, 1.0f, 63.0f, 40.0f);
		Toggle isEnchanted = addToggleToCreateMenu("Is enchanted", mainPanelGO.transform);
		Dictionary<string, int> dict = StaticObject.GetWeaponEnchants();
		Dropdown enchDrop = addDropdownToCreateMenu("Enchantment", mainPanelGO.transform, dict, 0, 80.0f);
		enchDrop.interactable = false;
		Action<bool> isEnchAct = (b) => enchDrop.interactable = b;
		isEnchanted.onValueChanged.AddListener((b) => isEnchAct(b));
		Action create = () =>
		{
			StaticObject weapon = MapCreator.AddObject(tile, tilePos, level, id);
			int spec = isEnchanted.isOn ? dict[enchDrop.options[enchDrop.value].text] : 1;
			weapon.SetProperties(isEnchanted.isOn, false, false, true, 0, (int)quality.value, 0, 512 + spec);
			Destroy(createMenuGO);
			afterCreatingAct?.Invoke(weapon);
		};
		addButtonToCreateMenu("Create", mainPanelGO.transform, create);
	}
	private void createWearable(int id, int level, Vector2Int tilePos, MapTile tile, Action<StaticObject> afterCreatingAct = null)
	{
		createMenuGO = Instantiate(CreateObjectPrefab, transform);
		createMenuGO.transform.Find("TopBar/Title").gameObject.GetComponent<Text>().text = "Create " + StaticObject.GetName(id);
		GameObject mainPanelGO = createMenuGO.transform.Find("MainPanel").gameObject;
		Slider quality = addSliderToCreateMenu("Quality", mainPanelGO.transform, 1.0f, 63.0f, 40.0f);
		Toggle isEnchanted = addToggleToCreateMenu("Is enchanted", mainPanelGO.transform);
		Dictionary<string, int> dict = StaticObject.GetWearableEnchants();
		Dropdown enchDrop = addDropdownToCreateMenu("Enchantment", mainPanelGO.transform, dict, 0, 80.0f);
		enchDrop.interactable = false;
		Action<bool> isEnchAct = (b) => enchDrop.interactable = b;
		isEnchanted.onValueChanged.AddListener((b) => isEnchAct(b));
		Action create = () =>
		{
			StaticObject arm = MapCreator.AddObject(tile, tilePos, level, id);
			int spec = isEnchanted.isOn ? dict[enchDrop.options[enchDrop.value].text] : 1;
			arm.SetProperties(isEnchanted.isOn, false, false, true, 0, (int)quality.value, 0, 512 + spec);
			Destroy(createMenuGO);
			afterCreatingAct?.Invoke(arm);
		};
		addButtonToCreateMenu("Create", mainPanelGO.transform, create);
	}
	private void createGold(int id, int level, Vector2Int tilePos, MapTile tile, Action<StaticObject> afterCreatingAct = null)
	{
		createMenuGO = Instantiate(CreateObjectPrefab, transform);
		createMenuGO.transform.Find("TopBar/Title").gameObject.GetComponent<Text>().text = "Create " + StaticObject.GetName(id);
		GameObject mainPanelGO = createMenuGO.transform.Find("MainPanel").gameObject;
		Slider quantity = addSliderToCreateMenu("Quantity", mainPanelGO.transform, 1.0f, 63.0f, 1.0f);
		Action create = () =>
		{
			StaticObject gold = MapCreator.AddObject(tile, tilePos, level, id);
			gold.SetProperties(false, false, false, true, 0, 40, 0, (int)quantity.value);
			Destroy(createMenuGO);
			afterCreatingAct?.Invoke(gold);
		};
		addButtonToCreateMenu("Create", mainPanelGO.transform, create);
	}
	private void createTreasure(int id, int level, Vector2Int tilePos, MapTile tile, Action<StaticObject> afterCreatingAct = null)
	{
		createMenuGO = Instantiate(CreateObjectPrefab, transform);
		createMenuGO.transform.Find("TopBar/Title").gameObject.GetComponent<Text>().text = "Create " + StaticObject.GetName(id);
		GameObject mainPanelGO = createMenuGO.transform.Find("MainPanel").gameObject;
		Slider quality = addSliderToCreateMenu("Quality", mainPanelGO.transform, 1.0f, 63.0f, 40.0f);
		Toggle isMagicalTog = addToggleToCreateMenu("Magical", mainPanelGO.transform);
		Slider quantity = addSliderToCreateMenu("Quantity", mainPanelGO.transform, 1.0f, 63.0f, 1.0f);
		Dictionary<string, int> spells = StaticObject.GetPotionScrollEnchants();
		Dropdown magicDrop = addDropdownToCreateMenu("Spell", mainPanelGO.transform, spells, 0, 60.0f);
		Slider charges = addSliderToCreateMenu("Charges", mainPanelGO.transform, 0, 63.0f, 1.0f);
		magicDrop.interactable = false;
		charges.interactable = false;
		Action<bool> isMagicalAct = (b) =>
		{
			if(b)
			{
				quantity.interactable = false;
				magicDrop.interactable = true;
				charges.interactable = true;
			}
			else
			{
				quantity.interactable = true;
				magicDrop.interactable = false;
				charges.interactable = false;
			}
		};
		isMagicalTog.onValueChanged.AddListener((b) => isMagicalAct(b));
		Action create = () =>
		{
			StaticObject treasure = MapCreator.AddObject(tile, tilePos, level, id);
			if (isMagicalTog.isOn)
			{
				StaticObject spell = MapCreator.AddObject(tile, tilePos, level, 288, null, true);
				int e = spells[magicDrop.options[magicDrop.value].text];
				spell.SetProperties(true, false, false, true, 4, (int)charges.value, 0, 512 + e);
				spell.ZPos = 0;
				spell.XPos = 3;
				spell.YPos = 3;
				treasure.SetProperties(false, false, false, false, 0, (int)quality.value, 0, 0);
				treasure.AddToContainer(spell);
			}
			else
				treasure.SetProperties(false, false, false, true, 0, (int)quality.value, 0, (int)quantity.value);
			Destroy(createMenuGO);
			afterCreatingAct?.Invoke(treasure);
		};
		addButtonToCreateMenu("Create", mainPanelGO.transform, create);
	}
	private void createWand(int id, int level, Vector2Int tilePos, MapTile tile, Action<StaticObject> afterCreatingAct = null)
	{
		createMenuGO = Instantiate(CreateObjectPrefab, transform);
		createMenuGO.transform.Find("TopBar/Title").gameObject.GetComponent<Text>().text = "Create wand";
		GameObject mainPanelGO = createMenuGO.transform.Find("MainPanel").gameObject;
		Dictionary<string, int> spells = StaticObject.GetPotionScrollEnchants();
		Dropdown spellDrop = addDropdownToCreateMenu("Enchantment", mainPanelGO.transform, spells, 0, 60.0f);
		Slider charges = addSliderToCreateMenu("Charges", mainPanelGO.transform, 0, 63.0f, 1.0f);
		Action create = () =>
		{
			StaticObject spell = MapCreator.AddObject(tile, tilePos, level, 288, null, true);
			int e = spells[spellDrop.options[spellDrop.value].text];
			spell.SetProperties(true, false, false, true, 4, (int)charges.value, 0, 512 + e);
			spell.ZPos = 0;
			spell.XPos = 3;
			spell.YPos = 3;
			StaticObject wand = MapCreator.AddObject(tile, tilePos, level, id);
			wand.SetProperties(false, false, false, false, 0, 40, 0, 0);
			wand.AddToContainer(spell);
			Destroy(createMenuGO);
			afterCreatingAct?.Invoke(wand);
		};
		addButtonToCreateMenu("Create", mainPanelGO.transform, create);
	}
	private void createBones(int id, int level, Vector2Int tilePos, MapTile tile, Action<StaticObject> afterCreatingAct = null)
	{
		createMenuGO = Instantiate(CreateObjectPrefab, transform);
		createMenuGO.transform.Find("TopBar/Title").gameObject.GetComponent<Text>().text = "Create " + StaticObject.GetName(id);
		GameObject mainPanelGO = createMenuGO.transform.Find("MainPanel").gameObject;
		Dropdown monsterType = addDropdownToCreateMenu("Monster", mainPanelGO.transform, StaticObject.GetMonsters(), 0, 40);
		Slider quantity = addSliderToCreateMenu("Quantity", mainPanelGO.transform, 1.0f, 63.0f, 1.0f);
		Toggle specTog = addToggleToCreateMenu("Special item", mainPanelGO.transform);
		InputField special = addInputToCreateMenu("Special", mainPanelGO.transform);
		Action<bool> specTogAct = (b) =>
		{
			if(b)
			{
				quantity.interactable = false;
				special.interactable = true;
			}
			else
			{
				quantity.interactable = true;
				special.interactable = false;
			}
		};
		specTog.onValueChanged.AddListener((b) => specTogAct(b));
		special.interactable = false;
		Action create = () =>
		{
			StaticObject bones = MapCreator.AddObject(tile, tilePos, level, id);
			int spec = specTog.isOn ? 512 + int.Parse(special.text) : (int)quantity.value;
			bones.SetProperties(false, false, false, true, 0, 40, monsterType.value, spec);
			Destroy(createMenuGO);
			afterCreatingAct?.Invoke(bones);
		};
		addButtonToCreateMenu("Create", mainPanelGO.transform, create);
	}
	private void createKey(int id, int level, Vector2Int tilePos, MapTile tile, Action<StaticObject> afterCreatingAct = null)
	{
		createMenuGO = Instantiate(CreateObjectPrefab, transform);
		createMenuGO.transform.Find("TopBar/Title").gameObject.GetComponent<Text>().text = "Create key";
		GameObject mainPanelGO = createMenuGO.transform.Find("MainPanel").gameObject;
		addDescriptionToCreateMenu("Use 'Key Editor' in objects menu to create key ID's", mainPanelGO.transform);
		Slider keyIDs = addSliderToCreateMenu("Key ID", mainPanelGO.transform, 1.0f, 63.0f, 1.0f);
		Action create = () =>
		{
			StaticObject key = MapCreator.AddObject(tile, tilePos, level, id);
			key.SetProperties(false, false, false, true, 0, 40, (int)keyIDs.value, 1);
			Destroy(createMenuGO);
			afterCreatingAct?.Invoke(key);
		};
		addButtonToCreateMenu("Create", mainPanelGO.transform, create);
	}
	private void createScroll(int id, int level, Vector2Int tilePos, MapTile tile, Action<StaticObject> afterCreatingAct = null)
	{
		createMenuGO = Instantiate(CreateObjectPrefab, transform);
		createMenuGO.transform.Find("TopBar/Title").gameObject.GetComponent<Text>().text = "Create scroll";
		GameObject mainPanelGO = createMenuGO.transform.Find("MainPanel").gameObject;
		Toggle isEnchanted = addToggleToCreateMenu("Is enchanted", mainPanelGO.transform);
		Toggle isTrapped = addToggleToCreateMenu("Is a trap", mainPanelGO.transform);
		Toggle isIdentified = addToggleToCreateMenu("Is identified", mainPanelGO.transform);
		InputField largeInput = addLargeInputFieldToCreateMenu("Text", mainPanelGO.transform);
		Dictionary<string, int> enchantments = StaticObject.GetPotionScrollEnchants();
		Dropdown enchDrop = addDropdownToCreateMenu("Enchantment", mainPanelGO.transform, enchantments, 0, 60.0f);
		isTrapped.interactable = false;
		enchDrop.interactable = false;
		Action<bool> isEnchantedAct = (b) =>
		{
			if (b)
			{
				largeInput.interactable = false;
				isTrapped.interactable = true;
				enchDrop.interactable = true;
			}
			else
			{
				largeInput.interactable = true;
				isTrapped.interactable = false;
				enchDrop.interactable = false;
			}
		};
		Action<bool> isTrappedAct = (b) =>
		{
			if (b)
			{
				enchDrop.interactable = false;
			}
			else
			{
				enchDrop.interactable = true;
			}
		};
		isEnchanted.onValueChanged.AddListener((b) => isEnchantedAct(b));
		isTrapped.onValueChanged.AddListener((b) => isTrappedAct(b));
		Action create = () =>
		{
			StaticObject scroll = MapCreator.AddObject(tile, tilePos, level, id);
			Action<StaticObject> assignTrap = (trap) =>
			{
				if (!MapCreator.AddTrigger(scroll, trap))
					SpawnPopupMessage("Failed to add trap");
				trap.Flags = 0;
			};
			Action<int> addTrap = (i) => CreateObject(i, level, tilePos, tile, assignTrap);
			if (isEnchanted.isOn)	//Enchanted scroll
			{
				if (isTrapped.isOn)	
				{
					scroll.SetProperties(true, false, false, false, 0, 40, 0, 0);
					CreateSpecifiedItemList(addTrap, StaticObject.GetIDsByType(ObjectType.Trap), "Trap list", "Select a trap for scroll");
				}
				else
				{
					int e = enchantments[enchDrop.options[enchDrop.value].text];
					scroll.SetProperties(true, false, false, true, 4, 40, 0, 512 + e);
				}
			}
			else                   //Readable scroll
			{
				int index = MapCreator.StringData.GetNextEmptyScrollSlot();
				if (index == -1)
				{
					SpawnPopupMessage("Failed to find an empty string for a writing.");
					Destroy(createMenuGO);
					return;
				}
				string text = largeInput.text;
				MapCreator.StringData.GetScrollBlock().Strings[index] = text;
				scroll.SetProperties(false, false, false, true, 0, 40, 0, index + 512);
			}
			scroll.Direction = isIdentified.isOn ? 7 : 0;
			Destroy(createMenuGO);
			afterCreatingAct?.Invoke(scroll);
		};
		addButtonToCreateMenu("Create", mainPanelGO.transform, create);
	}
	private void createPotion(int id, int level, Vector2Int tilePos, MapTile tile, Action<StaticObject> afterCreatingAct = null)
	{
		createMenuGO = Instantiate(CreateObjectPrefab, transform);
		createMenuGO.transform.Find("TopBar/Title").gameObject.GetComponent<Text>().text = "Create " + StaticObject.GetName(id);
		GameObject mainPanelGO = createMenuGO.transform.Find("MainPanel").gameObject;
		Toggle isTrapped = addToggleToCreateMenu("Is a trap", mainPanelGO.transform);
		Toggle isIdentified = addToggleToCreateMenu("Is identified", mainPanelGO.transform);
		Dictionary<string, int> enchantments = StaticObject.GetPotionScrollEnchants();
		Dropdown enchDrop = addDropdownToCreateMenu("Effect", mainPanelGO.transform, enchantments, 0, 60.0f);
		Action<bool> isTrappedAct = (b) => enchDrop.interactable = !b;
		isTrapped.onValueChanged.AddListener((b) => isTrappedAct(b));
		Action create = () =>
		{
			StaticObject potion = MapCreator.AddObject(tile, tilePos, level, id);
			Action<StaticObject> assignTrap = (trap) =>
			{
				if (!MapCreator.AddTrigger(potion, trap))
					SpawnPopupMessage("Failed to add trap");
				trap.Flags = 0;
			};
			Action<int> addTrap = (i) => CreateObject(i, level, tilePos, tile, assignTrap);
			if (isTrapped.isOn)
			{
				potion.SetProperties(true, false, false, false, 0, 40, 0, 0);
				CreateSpecifiedItemList(addTrap, StaticObject.GetIDsByType(ObjectType.Trap), "Trap list", "Select a trap for potion");				
			}
			else
			{
				int e = enchantments[enchDrop.options[enchDrop.value].text];
				potion.SetProperties(true, false, false, true, 4, 40, 0, 512 + e);
			}
			potion.Direction = isIdentified.isOn ? 7 : 0;
			Destroy(createMenuGO);
			afterCreatingAct?.Invoke(potion);
		};
		addButtonToCreateMenu("Create", mainPanelGO.transform, create);
	}
	private void createBook(int id, int level, Vector2Int tilePos, MapTile tile, Action<StaticObject> afterCreatingAct = null)
	{
		createMenuGO = Instantiate(CreateObjectPrefab, transform);
		createMenuGO.transform.Find("TopBar/Title").gameObject.GetComponent<Text>().text = "Create " + StaticObject.GetName(id);
		GameObject mainPanelGO = createMenuGO.transform.Find("MainPanel").gameObject;
		InputField largeInput = addLargeInputFieldToCreateMenu("Text", mainPanelGO.transform);
		Action create = () =>
		{
			StaticObject book = MapCreator.AddObject(tile, tilePos, level, id);
			int index = MapCreator.StringData.GetNextEmptyScrollSlot();
			if (index == -1)
			{
				SpawnPopupMessage("Failed to find an empty string for a writing.");
				Destroy(createMenuGO);
				return;
			}
			string text = largeInput.text;
			MapCreator.StringData.GetScrollBlock().Strings[index] = text;
			book.SetProperties(false, false, false, true, 0, 40, 0, index + 512);
			Destroy(createMenuGO);
			afterCreatingAct?.Invoke(book);
		};
		addButtonToCreateMenu("Create", mainPanelGO.transform, create);
	}
	private void createVertTexture(int id, int level, Vector2Int tilePos, MapTile tile, Action<StaticObject> afterCreatingAct = null)
	{
		createMenuGO = Instantiate(CreateObjectPrefab, transform);
		createMenuGO.transform.Find("TopBar/Title").gameObject.GetComponent<Text>().text = "Create wall";
		GameObject mainPanelGO = createMenuGO.transform.Find("MainPanel").gameObject;
		int curSpriteID = 0;
		Sprite curSprite = Sprite.Create(MapCreator.GetWallTextureFromIndex(curSpriteID, CurrentLevel), new Rect(0, 0, 64.0f, 64.0f), Vector2.zero);
		Slider height = addSliderToCreateMenu("Height", mainPanelGO.transform, tile.FloorHeight * 8.0f, 127.0f, tile.FloorHeight * 8.0f);
		Dropdown dir = addDropdownToCreateMenu("Direction", mainPanelGO.transform, new string[] { "N", "NE", "E", "SE", "S", "SW", "W", "NW" }, 0);
		Button texButton = addImageButtonToCreateMenu("Texture", mainPanelGO.transform, curSprite);
		Action<int> setTexAct = (i) =>
		{
			curSpriteID = i;
			texButton.GetComponent<Image>().sprite = Sprite.Create(MapCreator.GetWallTextureFromIndex(curSpriteID, CurrentLevel), new Rect(0, 0, 64.0f, 64.0f), Vector2.zero);
			Destroy(TexturePicker);
		};
		Action spawnTexPicker = () => CreateTexturePicker(GetCurrentLevelTextures(TextureType.Wall, false), setTexAct, "Textures", "Select wall texture");
		texButton.onClick.AddListener(() => spawnTexPicker());
		Action createVertTexture = () =>
		{
			StaticObject vertText = MapCreator.AddObject(tile, tilePos, level, id);
			vertText.ZPos = (int)height.value;
			vertText.Owner = curSpriteID;
			vertText.Direction = dir.value;
			MapCreator.SetGOSprite(vertText);
			MapCreator.SetGOHeight(vertText);
			MapCreator.SetGODirection(vertText);
			Destroy(createMenuGO);
			afterCreatingAct?.Invoke(vertText);
		};
		addButtonToCreateMenu("Create", mainPanelGO.transform, createVertTexture);
	}
	private void createWriting(int id, int level, Vector2Int tilePos, MapTile tile, Action<StaticObject> afterCreatingAct = null)
	{
		createMenuGO = Instantiate(CreateObjectPrefab, transform);
		createMenuGO.transform.Find("TopBar/Title").gameObject.GetComponent<Text>().text = "Create " + StaticObject.GetName(id);
		GameObject mainPanelGO = createMenuGO.transform.Find("MainPanel").gameObject;
		Slider height = addSliderToCreateMenu("Height", mainPanelGO.transform, tile.FloorHeight * 8.0f, 127.0f, tile.FloorHeight * 8.0f);
		Dropdown dir = addDropdownToCreateMenu("Direction", mainPanelGO.transform, new string[] { "N", "NE", "E", "SE", "S", "SW", "W", "NW" }, 0);
		int curSpriteID = 20;
		Sprite curSprite = Sprite.Create(MapCreator.TextureData.Other.Textures[curSpriteID], new Rect(0, 0, 16.0f, 16.0f), Vector2.zero);
		Button texButton = addImageButtonToCreateMenu("Texture", mainPanelGO.transform, curSprite);
		Action<int> setTexAct = (i) =>
		{
			curSpriteID = i + 20;
			texButton.GetComponent<Image>().sprite = Sprite.Create(MapCreator.TextureData.Other.Textures[curSpriteID], new Rect(0, 0, 16.0f, 16.0f), Vector2.zero);
			Destroy(TexturePicker);
		};
		Action spawnTexPicker = () => CreateTexturePicker(GetWritingTextures(), setTexAct, "Textures", "Select writing texture");
		texButton.onClick.AddListener(() => spawnTexPicker());
		InputField largeInput = addLargeInputFieldToCreateMenu("Text", mainPanelGO.transform);
		Toggle snapToWallToggle = addToggleToCreateMenu("Snap to nearest wall", mainPanelGO.transform, true);
		Action createWritingAct = () =>
		{
			StaticObject writing = MapCreator.AddObject(tile, tilePos, level, id);
			writing.Direction = dir.value;
			if (snapToWallToggle.isOn)
			{
				Tuple<Vector2Int, int> snapPos = MapCreator.GetNearestWall(tile, tilePos, level);
				//Debug.LogFormat("Snap pos : {0}", snapPos);
				writing.XPos = snapPos.Item1.x;
				writing.YPos = snapPos.Item1.y;
				writing.Direction = snapPos.Item2;
			}
			writing.ZPos = (int)height.value;
			int index = MapCreator.StringData.GetNextEmptyWritingSlot();
			if (index == -1)
			{
				SpawnPopupMessage("Failed to find an empty string for a writing.");
				Destroy(createMenuGO);
				return;
			}
			string text = largeInput.text;
			MapCreator.StringData.GetWritingBlock().Strings[index] = text;
			writing.SetProperties(false, false, false, true, curSpriteID - 20, 40, 0, index + 512);
			MapCreator.SetGOSprite(writing);
			MapCreator.SetGOHeight(writing);
			MapCreator.SetGODirection(writing);
			MapCreator.SetGOPosition(writing);
			Destroy(createMenuGO);
			afterCreatingAct?.Invoke(writing);
		};
		addButtonToCreateMenu("Create", mainPanelGO.transform, createWritingAct);
	}
	private void createGrave(int id, int level, Vector2Int tilePos, MapTile tile, Action<StaticObject> afterCreatingAct = null)
	{
		createMenuGO = Instantiate(CreateObjectPrefab, transform);
		createMenuGO.transform.Find("TopBar/Title").gameObject.GetComponent<Text>().text = "Create grave";
		GameObject mainPanelGO = createMenuGO.transform.Find("MainPanel").gameObject;
		addDescriptionToCreateMenu("Grave pictures are not supported (yet?)", mainPanelGO.transform);
		addDescriptionToCreateMenu("Graves can't have empty description, if text is empty, it will be automatically replaced by 'something indecipherable'", mainPanelGO.transform);
		Dropdown dir = addDropdownToCreateMenu("Direction", mainPanelGO.transform, new string[] { "N", "NE", "E", "SE", "S", "SW", "W", "NW" }, 0);
		int curSpriteID = 28;
		Sprite curSprite = Sprite.Create(MapCreator.TextureData.Other.Textures[curSpriteID], new Rect(0, 0, 16.0f, 32.0f), Vector2.zero);
		Button texButton = addImageButtonToCreateMenu("Texture", mainPanelGO.transform, curSprite);
		Action<int> setTexAct = (i) =>
		{
			curSpriteID = i + 28;
			texButton.GetComponent<Image>().sprite = Sprite.Create(MapCreator.TextureData.Other.Textures[curSpriteID], new Rect(0, 0, 16.0f, 32.0f), Vector2.zero);
			Destroy(TexturePicker);
		};
		Action spawnTexPicker = () => CreateTexturePicker(GetGraveTextures(), setTexAct, "Textures", "Select grave texture");
		texButton.onClick.AddListener(() => spawnTexPicker());
		InputField largeInput = addLargeInputFieldToCreateMenu("Description", mainPanelGO.transform);
		Action createGraveAct = () =>
		{
			StaticObject grave = MapCreator.AddObject(tile, tilePos, level, id);
			grave.Direction = dir.value;
			int index = MapCreator.StringData.GetNextEmptyGraveSlot();
			if (index == -1)
			{
				SpawnPopupMessage("Failed to find an empty string for a grave.");
				Destroy(createMenuGO);
				return;
			}
			string text = largeInput.text;
			MapCreator.StringData.GetWritingBlock().Strings[index] = text;
			grave.SetProperties(false, false, false, true, curSpriteID - 28, 40, 0, index + 512);
			MapCreator.SetGODirection(grave);
			Destroy(createMenuGO);
			afterCreatingAct?.Invoke(grave);
		};
		addButtonToCreateMenu("Create", mainPanelGO.transform, createGraveAct);
	}
	private void createBridge(int id, int level, Vector2Int tilePos, MapTile tile, Action<StaticObject> afterCreatingAct = null)
	{
		createMenuGO = Instantiate(CreateObjectPrefab, transform);
		createMenuGO.transform.Find("TopBar/Title").gameObject.GetComponent<Text>().text = "Create " + StaticObject.GetName(id);
		GameObject mainPanelGO = createMenuGO.transform.Find("MainPanel").gameObject;
		Slider height = addSliderToCreateMenu("Height", mainPanelGO.transform, tile.FloorHeight * 8.0f, 127.0f, tile.FloorHeight * 8.0f);
		Dropdown dir = addDropdownToCreateMenu("Direction", mainPanelGO.transform, new string[] { "N", "NE", "E", "SE", "S", "SW", "W", "NW" }, 0);
		int curTexId = 0;
		Sprite curSprite = Sprite.Create(GetBridgeTextures(level)[curTexId], new Rect(0, 0, 32.0f, 32.0f), Vector2.zero);
		Button texButton = addImageButtonToCreateMenu("Texture", mainPanelGO.transform, curSprite);
		Action<int> setTexAct = (i) =>
		{
			curTexId = i;
			texButton.GetComponent<Image>().sprite = Sprite.Create(GetBridgeTextures(level)[curTexId], new Rect(0, 0, 32.0f, 32.0f), Vector2.zero);
			Destroy(TexturePicker);
		};
		Action spawnTexPicker = () => CreateTexturePicker(GetBridgeTextures(level), setTexAct, "Textures", "Select bridge texture");
		texButton.onClick.AddListener(() => spawnTexPicker());
		Action createBridgeAct = () =>
		{
			StaticObject bridge = MapCreator.AddObject(tile, tilePos, level, id);
			bridge.ZPos = (int)height.value;
			bridge.Direction = dir.value;
			bridge.SetProperties(false, false, false, true, curTexId, 40, 0, 0);
			MapCreator.SetGODirection(bridge);
			MapCreator.SetGOHeight(bridge);
			Destroy(createMenuGO);
			afterCreatingAct?.Invoke(bridge);
		};
		addButtonToCreateMenu("Create", mainPanelGO.transform, createBridgeAct);
	}
	private void createDialLever(int id, int level, Vector2Int tilePos, MapTile tile, Action<StaticObject> afterCreatingAct = null)
	{
		createMenuGO = Instantiate(CreateObjectPrefab, transform);
		createMenuGO.transform.Find("TopBar/Title").gameObject.GetComponent<Text>().text = "Create dial lever";
		GameObject mainPanelGO = createMenuGO.transform.Find("MainPanel").gameObject;
		Toggle addUseToggle = addToggleToCreateMenu("Add use trigger to lever", mainPanelGO.transform, true);
		Dropdown useType = addDropdownToCreateMenu("Trigger type", mainPanelGO.transform, new string[] { "Unlimited", "One time only" });
		addUseToggle.onValueChanged.AddListener((b) => useType.interactable = !b);
		Toggle snapToWallToggle = addToggleToCreateMenu("Snap to nearest wall", mainPanelGO.transform, true);
		Slider height = addSliderToCreateMenu("Height", mainPanelGO.transform, tile.FloorHeight * 8.0f, 127.0f, tile.FloorHeight * 8.0f + 16.0f);
		Slider value = addSliderToCreateMenu("Start position", mainPanelGO.transform, 0, 7.0f, 0);
		Action createLeverAct = () =>
		{
			StaticObject lever = MapCreator.AddObject(tile, tilePos, level, id);
			lever.SetProperties(false, false, false, false, 0, 40, 0, 0);
			lever.ZPos = (int)height.value;
			lever.Flags = (int)value.value;
			if (snapToWallToggle.isOn)
			{
				Tuple<Vector2Int, int> snapPos = MapCreator.GetNearestWall(tile, tilePos, level);
				//Debug.LogFormat("Snap pos : {0}", snapPos);
				lever.XPos = snapPos.Item1.x;
				lever.YPos = snapPos.Item1.y;
				lever.Direction = snapPos.Item2;
			}
			if (addUseToggle.isOn)
			{
				StaticObject use = MapCreator.AddObject(tile, new Vector2Int(3, 3), level, 418, null, true);
				int useFlags = useType.value == 0 ? 6 : 4;
				use.SetProperties(false, true, true, true, useFlags, 0, 0, 0);
				lever.Special = use.CurrentAdress;
				use.PrevAdress = lever.CurrentAdress;
			}
			MapCreator.SetGOPosition(lever);
			MapCreator.SetGOHeight(lever);
			MapCreator.SetGODirection(lever);
			MapCreator.SetGOSprite(lever);
			Destroy(createMenuGO);
			afterCreatingAct?.Invoke(lever);
		};
		addButtonToCreateMenu("Create", mainPanelGO.transform, createLeverAct);
	}
	private void createPillar(int id, int level, Vector2Int tilePos, MapTile tile, Action<StaticObject> afterCreatingAct = null) //352
	{
		createMenuGO = Instantiate(CreateObjectPrefab, transform);
		createMenuGO.transform.Find("TopBar/Title").gameObject.GetComponent<Text>().text = "Create pillar";
		GameObject mainPanelGO = createMenuGO.transform.Find("MainPanel").gameObject;
		Slider height = addSliderToCreateMenu("Height", mainPanelGO.transform, tile.FloorHeight * 8.0f, 127.0f, tile.FloorHeight * 8.0f);
		Dropdown dir = addDropdownToCreateMenu("Direction", mainPanelGO.transform, new string[] { "N", "NE", "E", "SE", "S", "SW", "W", "NW" }, 0);

		int curTexId = 0;
		List<Texture2D> pillartex = GetPillarTextures();
		Sprite curSprite = Sprite.Create(pillartex[curTexId], new Rect(0, 0, 8.0f, 32.0f), Vector2.zero);
		Button texButton = addImageButtonToCreateMenu("Texture", mainPanelGO.transform, curSprite);
		Action<int> setTexAct = (i) =>
		{
			curTexId = i;
			texButton.GetComponent<Image>().sprite = Sprite.Create(pillartex[curTexId], new Rect(0, 0, 8.0f, 32.0f), Vector2.zero);
			Destroy(TexturePicker);
		};
		Action spawnTexPicker = () => CreateTexturePicker(pillartex, setTexAct, "Textures", "Select pillar texture");
		texButton.onClick.AddListener(() => spawnTexPicker());
		Action createPillarAct = () =>
		{
			StaticObject bridge = MapCreator.AddObject(tile, tilePos, level, id);
			bridge.ZPos = (int)height.value;
			bridge.Direction = dir.value;
			bridge.SetProperties(false, false, false, true, curTexId, 40, 0, 0);
			MapCreator.SetGODirection(bridge);
			MapCreator.SetGOHeight(bridge);
			Destroy(createMenuGO);
			afterCreatingAct?.Invoke(bridge);
		};
		addButtonToCreateMenu("Create", mainPanelGO.transform, createPillarAct);
	}
	private void createModel(int id, int level, Vector2Int tilePos, MapTile tile, Action<StaticObject> afterCreatingAct = null)
	{
		createMenuGO = Instantiate(CreateObjectPrefab, transform);
		createMenuGO.transform.Find("TopBar/Title").gameObject.GetComponent<Text>().text = "Create " + StaticObject.GetName(id);
		GameObject mainPanelGO = createMenuGO.transform.Find("MainPanel").gameObject;
		Slider height = addSliderToCreateMenu("Height", mainPanelGO.transform, tile.FloorHeight * 8.0f, 127.0f, tile.FloorHeight * 8.0f);
		Dropdown dir = addDropdownToCreateMenu("Direction", mainPanelGO.transform, new string[] { "N", "NE", "E", "SE", "S", "SW", "W", "NW" }, 0);
		Action createAct = () =>
		{
			StaticObject so = MapCreator.AddObject(tile, tilePos, level, id);
			so.ZPos = (int)height.value;
			so.Direction = dir.value;
			so.SetProperties(false, false, false, true, 0, 40, 0, 0);
			MapCreator.SetGODirection(so);
			MapCreator.SetGOHeight(so);
			Destroy(createMenuGO);
			afterCreatingAct?.Invoke(so);
		};
		addButtonToCreateMenu("Create", mainPanelGO.transform, createAct);
	}
	private void createLever(int id, int level, Vector2Int tilePos, MapTile tile, Action<StaticObject> afterCreatingAct = null)
	{
		createMenuGO = Instantiate(CreateObjectPrefab, transform);
		createMenuGO.transform.Find("TopBar/Title").gameObject.GetComponent<Text>().text = "Create " + StaticObject.GetName(id);
		GameObject mainPanelGO = createMenuGO.transform.Find("MainPanel").gameObject;
		Toggle addUseToggle = addToggleToCreateMenu("Add use trigger to lever", mainPanelGO.transform, true);
		Dropdown useType = addDropdownToCreateMenu("Trigger type", mainPanelGO.transform, new string[] { "Unlimited", "One time only" });
		addUseToggle.onValueChanged.AddListener((b) => useType.interactable = !b);
		Toggle snapToWallToggle = addToggleToCreateMenu("Snap to nearest wall", mainPanelGO.transform, true);
		Slider height = addSliderToCreateMenu("Height", mainPanelGO.transform, tile.FloorHeight * 8.0f, 127.0f, tile.FloorHeight * 8.0f + 16.0f);
		Action createLeverAct = () =>
		{
			StaticObject lever = MapCreator.AddObject(tile, tilePos, level, id);
			lever.SetProperties(false, false, false, false, 0, 40, 0, 0);
			lever.ZPos = (int)height.value;
			if(snapToWallToggle.isOn)
			{
				Tuple<Vector2Int, int> snapPos = MapCreator.GetNearestWall(tile, tilePos, level);
				//Debug.LogFormat("Snap pos : {0}", snapPos);
				lever.XPos = snapPos.Item1.x;
				lever.YPos = snapPos.Item1.y;
				lever.Direction = snapPos.Item2;
			}
			if(addUseToggle.isOn)
			{
				StaticObject use = MapCreator.AddObject(tile, new Vector2Int(3, 3), level, 418, null, true);
				int useFlags = useType.value == 0 ? 6 : 4;
				use.SetProperties(false, true, true, true, useFlags, 0, 0, 0);
				lever.Special = use.CurrentAdress;
				use.PrevAdress = lever.CurrentAdress;
			}
			MapCreator.SetGOPosition(lever);
			MapCreator.SetGOHeight(lever);
			MapCreator.SetGODirection(lever);
			Destroy(createMenuGO);
			afterCreatingAct?.Invoke(lever);
		};
		addButtonToCreateMenu("Create", mainPanelGO.transform, createLeverAct);
	}
	private void createDoor(int id, int level, Vector2Int tilePos, MapTile tile, Action<StaticObject> afterCreatingAct = null)
	{
		createMenuGO = Instantiate(CreateObjectPrefab, transform);
		createMenuGO.transform.Find("TopBar/Title").gameObject.GetComponent<Text>().text = "Create " + StaticObject.GetName(id);
		GameObject mainPanelGO = createMenuGO.transform.Find("MainPanel").gameObject;
		addDescriptionToCreateMenu("Door 6 and portcullis is always indescructible", mainPanelGO.transform);
		addDescriptionToCreateMenu("Locked doors are only opened by keys or door traps.", mainPanelGO.transform);
		List<Texture2D> doorTexs = GetCurrentLevelTextures(TextureType.Door, false);
		doorTexs.Add(Resources.Load<Texture2D>("port"));
		doorTexs.Add(Resources.Load<Texture2D>("secr"));
		int doorIndex = 0;
		Button selectTexBut = addImageButtonToCreateMenu("Door type", mainPanelGO.transform, Sprite.Create(MapCreator.GetDoorTextureFromIndex(0, CurrentLevel), new Rect(0, 0, 32.0f, 64.0f), new Vector2(0, 0)));
		Toggle openTog = addToggleToCreateMenu("Door is open", mainPanelGO.transform);
		Toggle lockTog = addToggleToCreateMenu("Add lock to door", mainPanelGO.transform, true);
		Toggle lockKey = addToggleToCreateMenu("Opened by key", mainPanelGO.transform, true);
		//InputField lockId = addInputToCreateMenu("Key ID", mainPanelGO.transform);
		Slider lockId = addSliderToCreateMenu("Key ID", mainPanelGO.transform, 1.0f, 63.0f, 1.0f);
		lockKey.onValueChanged.AddListener((b) => lockId.interactable = b);
		Slider lockDiff = addSliderToCreateMenu("Lock difficulty", mainPanelGO.transform, 0, 30.0f, 1.0f);
		Slider doorQual = addSliderToCreateMenu("Door quality", mainPanelGO.transform, 1.0f, 63.0f, 40.0f);
		Toggle doorMassive = addToggleToCreateMenu("Indestructible", mainPanelGO.transform, false);
		Dropdown dir = addDropdownToCreateMenu("Direction", mainPanelGO.transform, new string[] { "N", "NE", "E", "SE", "S", "SW", "W", "NW" }, 0);
		Action<bool> togLock = (b) =>
		{
			lockKey.interactable = b;
			lockId.interactable = b;
			lockDiff.interactable = b;
		};
		lockTog.onValueChanged.AddListener((b) => togLock(b));
		Action<int> selTex = (i) =>
		{
			doorIndex = i;
			selectTexBut.GetComponent<Image>().sprite = Sprite.Create(doorTexs[i], new Rect(0, 0, 32.0f, 64.0f), new Vector2(0, 0));
			Destroy(TexturePicker);
		};
		selectTexBut.onClick.AddListener(() => CreateTexturePicker(doorTexs, selTex, "Textures", "Select door texture"));
		Action createDoorAct = () =>
		{
			int doorId = 320 + doorIndex;
			if (openTog.isOn)
				doorId += 8;
			StaticObject door = MapCreator.AddObject(tile, tilePos, level, doorId);
			door.SetProperties(false, false, doorMassive.isOn, false, 0, (int)doorQual.value, 0, 0);
			door.Direction = dir.value;
			if(lockTog.isOn)
			{
				StaticObject lockObj = MapCreator.AddObject(tile, new Vector2Int(3, 3), level, 271, null, true);
				int inputId = (int)lockId.value;
				int keyId = lockKey.isOn ? 512 + inputId : 512;
				lockObj.SetProperties(false, true, false, true, 3, 40, 0, keyId);
				lockObj.ZPos = (int)lockDiff.value;
				door.Special = lockObj.CurrentAdress;
				lockObj.PrevAdress = door.CurrentAdress;
			}
			MapCreator.SetGOPosition(door);
			MapCreator.SetGOHeight(door);
			MapCreator.SetGODirection(door);
			MapCreator.CreateDoorGO(door, MapCreator.ObjectToGO[door].transform, CurrentLevel);
			Destroy(createMenuGO);
			afterCreatingAct?.Invoke(door);
		};
		addButtonToCreateMenu("Create", mainPanelGO.transform, createDoorAct);
	}
	private void createLock(int id, int level, Vector2Int tilePos, MapTile tile, Action<StaticObject> afterCreatingAct = null)
	{
		createMenuGO = Instantiate(CreateObjectPrefab, transform);
		createMenuGO.transform.Find("TopBar/Title").gameObject.GetComponent<Text>().text = "Create lock";
		GameObject mainPanelGO = createMenuGO.transform.Find("MainPanel").gameObject;
		Toggle lockKey = addToggleToCreateMenu("Opened by key", mainPanelGO.transform, true);
		InputField lockId = addInputToCreateMenu("Key ID", mainPanelGO.transform);
		lockKey.onValueChanged.AddListener((b) => lockId.interactable = b);
		Slider lockDiff = addSliderToCreateMenu("Lock difficulty", mainPanelGO.transform, 0, 30.0f, 1.0f);
		Action createLockAct = () =>
		{
			StaticObject lockObj = MapCreator.AddObject(tile, new Vector2Int(3, 3), level, 271);
			int inputId = 0;
			if (!string.IsNullOrEmpty(lockId.text))
			{
				inputId = int.Parse(lockId.text);
				if (inputId < 0)
					inputId = 0;
				else if (inputId > 511)
					inputId = 511;
			}
			int keyId = lockKey.isOn ? 512 + inputId : 512;
			lockObj.SetProperties(false, true, false, true, 3, 40, 0, keyId);
			lockObj.ZPos = (int)lockDiff.value;
			Destroy(createMenuGO);
			afterCreatingAct?.Invoke(lockObj);
		};
		addButtonToCreateMenu("Create", mainPanelGO.transform, createLockAct);
	}
	private void createDoorTrap(int id, int level, Vector2Int tilePos, MapTile tile, Action<StaticObject> afterCreatingAct = null)
	{
		createMenuGO = Instantiate(CreateObjectPrefab, transform);
		createMenuGO.transform.Find("TopBar/Title").gameObject.GetComponent<Text>().text = "Create door trap";
		GameObject mainPanelGO = createMenuGO.transform.Find("MainPanel").gameObject;
		addDescriptionToCreateMenu("Opens, closes doors, or both.", mainPanelGO.transform);
		addDescriptionToCreateMenu("Door traps must be attached to doors. If a lock is present on a door, door trap will destroy this lock once used. If a lock is present on a trap door, it will replace the door lock once used.", mainPanelGO.transform);
		Dropdown trapType = addDropdownToCreateMenu("Action type", mainPanelGO.transform, new string[] { "Open", "Close", "Both" });
		Toggle lockTog = addToggleToCreateMenu("Add lock to trap", mainPanelGO.transform, true);
		Toggle lockKey = addToggleToCreateMenu("Opened by key", mainPanelGO.transform, true);
		InputField lockId = addInputToCreateMenu("Key ID", mainPanelGO.transform);
		lockKey.onValueChanged.AddListener((b) => lockId.interactable = b);
		Slider lockDiff = addSliderToCreateMenu("Lock difficulty", mainPanelGO.transform, 0, 30.0f, 1.0f);
		Action<bool> togLock = (b) =>
		{
			lockKey.interactable = b;
			lockId.interactable = b;
			lockDiff.interactable = b;
		};
		lockTog.onValueChanged.AddListener((b) => togLock(b));
		Action createTrapAct = () =>
		{
			StaticObject doorTrap = MapCreator.AddObject(tile, new Vector2Int(3, 3), level, id);
			doorTrap.SetProperties(false, true, true, false, 1, trapType.value + 1, 0, 0);
			if(lockTog.isOn)
			{
				StaticObject lockObj = MapCreator.AddObject(tile, new Vector2Int(3, 3), level, 271, null, true);
				int inputId = 0;
				if (!string.IsNullOrEmpty(lockId.text))
				{
					inputId = int.Parse(lockId.text);
					if (inputId < 0)
						inputId = 0;
					else if (inputId > 511)
						inputId = 511;
				}
				int keyId = lockKey.isOn ? 512 + inputId : 512;
				lockObj.SetProperties(false, true, false, true, 3, 40, 0, keyId);
				lockObj.ZPos = (int)lockDiff.value;
				doorTrap.Special = lockObj.CurrentAdress;
				lockObj.PrevAdress = doorTrap.CurrentAdress;
			}
			Destroy(createMenuGO);
			afterCreatingAct?.Invoke(doorTrap);
		};
		addButtonToCreateMenu("Create", mainPanelGO.transform, createTrapAct);
	}
	private void createDamageTrap(int id, int level, Vector2Int tilePos, MapTile tile, Action<StaticObject> afterCreatingAct = null)
	{
		createMenuGO = Instantiate(CreateObjectPrefab, transform);
		createMenuGO.transform.Find("TopBar/Title").gameObject.GetComponent<Text>().text = "Create damage trap";
		GameObject mainPanelGO = createMenuGO.transform.Find("MainPanel").gameObject;
		Dropdown owner = addDropdownToCreateMenu("Damage type", mainPanelGO.transform, new string[] { "Health", "Poison" });
		Slider quality = addSliderToCreateMenu("Damage value", mainPanelGO.transform, 0, 63.0f);
		Action createDamageTrapAct = () =>
		{
			StaticObject damTrap = MapCreator.AddObject(tile, tilePos, level, id);
			damTrap.SetProperties(false, true, true, false, 1, (int)quality.value, owner.value, 0);
			Destroy(createMenuGO);
			afterCreatingAct?.Invoke(damTrap);
		};
		addButtonToCreateMenu("Create", mainPanelGO.transform, createDamageTrapAct);
	}
	private void createTextTrap(int id, int level, Vector2Int tilePos, MapTile tile, Action<StaticObject> afterCreatingAct = null)
	{
		createMenuGO = Instantiate(CreateObjectPrefab, transform);
		createMenuGO.transform.Find("TopBar/Title").gameObject.GetComponent<Text>().text = "Create text trap";
		GameObject mainPanelGO = createMenuGO.transform.Find("MainPanel").gameObject;
		InputField largeInput = addLargeInputFieldToCreateMenu("Text printed", mainPanelGO.transform);
		Action createTextTrapAct = () =>
		{
			StaticObject textTrap = MapCreator.AddObject(tile, tilePos, level, id);
			int index = MapCreator.StringData.GetNextEmptyTextTrapString();
			if(index == -1)
			{
				SpawnPopupMessage("Failed to find an empty string for a new text trap.");
				return;
			}
			string text = largeInput.text + "\n";
			MapCreator.StringData.GetTextTrapBlock().Strings[index] = text;
			int qual = (index / 64) * 2;
			int ownr = index % 64;
			textTrap.SetProperties(false, true, true, false, 1, qual, ownr, 0);
			Destroy(createMenuGO);
			afterCreatingAct?.Invoke(textTrap);
		};
		addButtonToCreateMenu("Create", mainPanelGO.transform, createTextTrapAct);
	}
	private void createVarTrap(int id, int level, Vector2Int tilePos, MapTile tile, Action<StaticObject> afterCreatingAct = null)
	{
		createMenuGO = Instantiate(CreateObjectPrefab, transform);
		createMenuGO.transform.Find("TopBar/Title").gameObject.GetComponent<Text>().text = "Create variable trap";
		GameObject mainPanelGO = createMenuGO.transform.Find("MainPanel").gameObject;
		addDescriptionToCreateMenu("Sets or changes a 'trap' variable or a 'quest' variable", mainPanelGO.transform);
		addDescriptionToCreateMenu("Warning - do not change this object position in map - it's variable is partially saved in it's position.", mainPanelGO.transform);
		Slider slider = addSliderToCreateMenu("Variable index", mainPanelGO.transform, 1.0f, 127.0f, 1.0f);
		Toggle toggle = addToggleToCreateMenu("Set quest variables", mainPanelGO.transform, false, (b) => slider.interactable = !b);
		InputField input = addInputToCreateMenu("Value / quest", mainPanelGO.transform);
		//Slider varValue = addSliderToCreateMenu("Value / quest index", mainPanelGO.transform, 0, 63.0f);
		Dropdown dropdown = addDropdownToCreateMenu("Operation type", mainPanelGO.transform, new string[] { "Add", "Substract", "Set", "And", "Or", "XOR", "Shift left" }, 2);
		Action createVarTrapAct = () =>
		{
			StaticObject varTrap = MapCreator.AddObject(tile, tilePos, level, id);
			if (toggle.isOn)
				varTrap.ZPos = 0;
			else
				varTrap.ZPos = (int)slider.value;
			//int value = (int)varValue.value;
			if (string.IsNullOrEmpty(input.text))
				input.text = "0";
			int value = int.Parse(input.text);
			int ypos = value & 0x07;
			int ownr = (value & 0xF8) >> 3;
			int qual = (value & 0x3F00) >> 8;
			varTrap.XPos = 0;
			varTrap.YPos = ypos;
			varTrap.Direction = dropdown.value;
			varTrap.SetProperties(false, true, true, false, 1, qual, ownr, 0);
			MapCreator.SetGOHeight(varTrap);
			MapCreator.SetGOPosition(varTrap);
			Destroy(createMenuGO);
			afterCreatingAct?.Invoke(varTrap);
		};
		addButtonToCreateMenu("Create", mainPanelGO.transform, createVarTrapAct);
	}
	private void createCheckTrap(int id, int level, Vector2Int tilePos, MapTile tile, Action<StaticObject> afterCreatingAct = null)
	{
		createMenuGO = Instantiate(CreateObjectPrefab, transform);
		createMenuGO.transform.Find("TopBar/Title").gameObject.GetComponent<Text>().text = "Create check variable trap";
		GameObject mainPanelGO = createMenuGO.transform.Find("MainPanel").gameObject;
		addDescriptionToCreateMenu("Checks variables from 'variable index' , to 'variable index' + 'variable range'", mainPanelGO.transform);
		addDescriptionToCreateMenu("When range > 0, variable values are added in the range. If 'shift instead' is on, value is composed from 3 lower bits of every variable in range.", mainPanelGO.transform);
		addDescriptionToCreateMenu("If the variable is equal to value, it triggers first trigger in inventory, if is not equal, it triggers second trigger if present.", mainPanelGO.transform);
		addDescriptionToCreateMenu("Warning - do not change this object position in map - it's variable is partially saved in it's position.", mainPanelGO.transform);
		Slider varIndex = addSliderToCreateMenu("Variable index", mainPanelGO.transform, 1.0f, 127.0f, 1.0f);
		Slider varRange = addSliderToCreateMenu("Variable range", mainPanelGO.transform, 0, 7.0f, 0);
		Toggle checkType = addToggleToCreateMenu("Shift instead", mainPanelGO.transform, false);
		InputField input = addInputToCreateMenu("Value checked for", mainPanelGO.transform);
		//Slider varValue = addSliderToCreateMenu("Value checked for", mainPanelGO.transform, 0, 63.0f);
		Action createCheckTrapAct = () =>
		{
			StaticObject checkTrap = MapCreator.AddObject(tile, tilePos, level, id);
			checkTrap.ZPos = (int)varIndex.value;
			//int value = (int)varValue.value;
			if (string.IsNullOrEmpty(input.text))
				input.text = "0";
			int value = int.Parse(input.text);
			int ypos = value & 0x07;
			int ownr = (value & 0xF8) >> 3;
			int qual = (value & 0x3F00) >> 8;
			if (checkType.isOn)
				checkTrap.XPos = 0;
			else
				checkTrap.XPos = 1;
			checkTrap.YPos = ypos;
			checkTrap.Direction = (int)varRange.value;
			checkTrap.SetProperties(false, true, true, false, 1, qual, ownr, 0);
			MapCreator.SetGOHeight(checkTrap);
			MapCreator.SetGOPosition(checkTrap);
			Destroy(createMenuGO);
			afterCreatingAct?.Invoke(checkTrap);
		};
		addButtonToCreateMenu("Create", mainPanelGO.transform, createCheckTrapAct);
	}

	private void createTeleportTrap(int id, int level, Vector2Int tilePos, MapTile tile, Action<StaticObject> afterCreatingAct = null)
	{
		createMenuGO = Instantiate(CreateObjectPrefab, transform);
		createMenuGO.transform.Find("TopBar/Title").gameObject.GetComponent<Text>().text = "Create teleport trap";
		GameObject mainPanelGO = createMenuGO.transform.Find("MainPanel").gameObject;
		addDescriptionToCreateMenu("Teleports player. Used for stairs too.", mainPanelGO.transform);
		Slider targetLevel = addSliderToCreateMenu("Target level", mainPanelGO.transform, 1.0f, MapCreator.GetLevelCount(), 1.0f);
		Action<bool> curLevelAct = (b) => targetLevel.interactable = !b;
		Toggle curLevel = addToggleToCreateMenu("Teleport in this level", mainPanelGO.transform, false, curLevelAct);
		Slider targetX = addSliderToCreateMenu("Target X", mainPanelGO.transform, 0, 63.0f, 0);
		Slider targetY = addSliderToCreateMenu("Target Y", mainPanelGO.transform, 0, 63.0f, 0);
		Action createTeleAct = () =>
		{
			StaticObject teleTrap = MapCreator.AddObject(tile, tilePos, level, id);
			teleTrap.ZPos = curLevel.isOn ? 0 : (int)targetLevel.value;
			teleTrap.YPos = 0;		//Not sure if needed
			teleTrap.XPos = 0;
			teleTrap.SetProperties(false, true, true, false, 1, (int)targetX.value, (int)targetY.value, 0);
			MapCreator.SetGOHeight(teleTrap);
			MapCreator.SetGOPosition(teleTrap);
			Destroy(createMenuGO);
			afterCreatingAct?.Invoke(teleTrap);
		};
		addButtonToCreateMenu("Create", mainPanelGO.transform, createTeleAct);
	}
	private void createArrowTrap(int id, int level, Vector2Int tilePos, MapTile tile, Action<StaticObject> afterCreatingTrap = null)
	{
		createMenuGO = Instantiate(CreateObjectPrefab, transform);
		createMenuGO.transform.Find("TopBar/Title").gameObject.GetComponent<Text>().text = "Create arrow trap";
		GameObject mainPanelGO = createMenuGO.transform.Find("MainPanel").gameObject;
		addDescriptionToCreateMenu("Shoots an object into a direction.", mainPanelGO.transform);
		Slider height = addSliderToCreateMenu("Height", mainPanelGO.transform, tile.FloorHeight * 8.0f, 127.0f, tile.FloorHeight * 8.0f + 16.0f);
		int arrowId = 23;
		Button button = addButtonToCreateMenu(StaticObject.GetName(arrowId), mainPanelGO.transform);
		Action<int> selectObject = (i) =>
		{
			arrowId = i;
			button.GetComponentInChildren<Text>().text = StaticObject.GetName(arrowId);
		};
		Action createObjList = () => CreateSpecifiedItemList(selectObject, StaticObject.GetShootable(), "Object list", "Select object to shoot.");
		button.onClick.AddListener(() => createObjList());
		Dropdown dir = addDropdownToCreateMenu("Direction", mainPanelGO.transform, new string[] { "N", "NE", "E", "SE", "S", "SW", "W", "NW" }, 0);
		Action createTrapAct = () =>
		{
			StaticObject arrowTrap = MapCreator.AddObject(tile, tilePos, level, id);
			int ownr = arrowId & 0x1F;
			int qual = (arrowId & 0x1E0) >> 5;
			arrowTrap.SetProperties(false, true, true, false, 1, qual, ownr, 0);
			arrowTrap.Direction = dir.value;
			arrowTrap.ZPos = (int)height.value;
			MapCreator.SetGOHeight(arrowTrap);
			Destroy(createMenuGO);
			afterCreatingTrap?.Invoke(arrowTrap);
		};
		addButtonToCreateMenu("Create", mainPanelGO.transform, createTrapAct);
	}
	private void createAttitudeTrap(int id, int level, Vector2Int tilePos, MapTile tile, Action<StaticObject> afterCreatingAct = null)
	{
		createMenuGO = Instantiate(CreateObjectPrefab, transform);
		createMenuGO.transform.Find("TopBar/Title").gameObject.GetComponent<Text>().text = "Create attitude trap";
		GameObject mainPanelGO = createMenuGO.transform.Find("MainPanel").gameObject;
		addDescriptionToCreateMenu("Angers nearby monsters.", mainPanelGO.transform);
		Dropdown race = addDropdownToCreateMenu("Monster type", mainPanelGO.transform, MapCreator.StringData.GetRaces(), 0);
		Action createTrapAct = () =>
		{
			StaticObject attTrap = MapCreator.AddObject(tile, tilePos, level, 387);
			attTrap.SetProperties(false, true, true, false, 1, 5, race.value, 0);
			attTrap.XPos = 0;	//Not sure if needed
			attTrap.YPos = 0;
			MapCreator.SetGOPosition(attTrap);
			Destroy(createMenuGO);
			afterCreatingAct?.Invoke(attTrap);
		};
		addButtonToCreateMenu("Create", mainPanelGO.transform, createTrapAct);
	}
	private void createPlatformTrap(int id, int level, Vector2Int tilePos, MapTile tile, Action<StaticObject> afterCreatingAct = null)
	{
		createMenuGO = Instantiate(CreateObjectPrefab, transform);
		createMenuGO.transform.Find("TopBar/Title").gameObject.GetComponent<Text>().text = "Create platform trap";
		GameObject mainPanelGO = createMenuGO.transform.Find("MainPanel").gameObject;
		addDescriptionToCreateMenu("Changes floor height.", mainPanelGO.transform);
		Slider height = addSliderToCreateMenu("Target height", mainPanelGO.transform, 0, 8.0f, 0, (f) => f * 16);
		Action createTrapAct = () =>
		{
			StaticObject platformTrap = MapCreator.AddObject(tile, tilePos, level, 387);
			platformTrap.SetProperties(false, true, true, false, 1, 3, 0, 0);
			platformTrap.ZPos = (int)height.value * 16;
			platformTrap.XPos = 0;   //Not sure if needed
			platformTrap.YPos = 0;
			MapCreator.SetGOHeight(platformTrap);
			MapCreator.SetGOPosition(platformTrap);
			Destroy(createMenuGO);
			afterCreatingAct?.Invoke(platformTrap);
		};
		addButtonToCreateMenu("Create", mainPanelGO.transform, createTrapAct);
	}
	private void createCameraTrap(int id, int level, Vector2Int tilePos, MapTile tile, Action<StaticObject> afterCreatingAct = null)
	{
		createMenuGO = Instantiate(CreateObjectPrefab, transform);
		createMenuGO.transform.Find("TopBar/Title").gameObject.GetComponent<Text>().text = "Create camera trap";
		GameObject mainPanelGO = createMenuGO.transform.Find("MainPanel").gameObject;
		addDescriptionToCreateMenu("A camera.", mainPanelGO.transform);
		Dropdown dir = addDropdownToCreateMenu("Direction", mainPanelGO.transform, new string[] { "N", "NE", "E", "SE", "S", "SW", "W", "NW" }, 0);
		Slider height = addSliderToCreateMenu("Height", mainPanelGO.transform, tile.FloorHeight * 8.0f, 127.0f, tile.FloorHeight * 8.0f + 16.0f);
		Action createTrapAct = () =>
		{
			StaticObject camera = MapCreator.AddObject(tile, tilePos, level, 387);
			camera.SetProperties(false, true, true, false, 1, 2, 0, 0);
			camera.ZPos = (int)height.value;
			camera.Direction = dir.value;
			MapCreator.SetGODirection(camera);
			MapCreator.SetGOHeight(camera);
			Destroy(createMenuGO);
			afterCreatingAct?.Invoke(camera);
		};
		addButtonToCreateMenu("Create", mainPanelGO.transform, createTrapAct);
	}
	private void createConversationTrap(int id, int level, Vector2Int tilePos, MapTile tile, Action<StaticObject> afterCreatingAct = null)
	{
		createMenuGO = Instantiate(CreateObjectPrefab, transform);
		createMenuGO.transform.Find("TopBar/Title").gameObject.GetComponent<Text>().text = "Create conversation trap";
		GameObject mainPanelGO = createMenuGO.transform.Find("MainPanel").gameObject;
		addDescriptionToCreateMenu("Starts a conversation.", mainPanelGO.transform);
		addDescriptionToCreateMenu("In UW1 only uses conversation no 25 (door).", mainPanelGO.transform);
		Slider convSlot = addSliderToCreateMenu("Conversation slot", mainPanelGO.transform, 25.0f, 25.0f, 25.0f);
		Action createTrapAct = () =>
		{
			StaticObject convTrap = MapCreator.AddObject(tile, tilePos, level, 387);
			convTrap.SetProperties(false, true, true, false, 1, 42, 0, 0);
			convTrap.ZPos = (int)convSlot.value;
			MapCreator.SetGOHeight(convTrap);
			Destroy(createMenuGO);
			afterCreatingAct?.Invoke(convTrap);
		};
		addButtonToCreateMenu("Create", mainPanelGO.transform, createTrapAct);
	}
	private void createEndGameTrap(int id, int level, Vector2Int tilePos, MapTile tile, Action<StaticObject> afterCreatingAct = null)
	{
		createMenuGO = Instantiate(CreateObjectPrefab, transform);
		createMenuGO.transform.Find("TopBar/Title").gameObject.GetComponent<Text>().text = "Create end game trap";
		GameObject mainPanelGO = createMenuGO.transform.Find("MainPanel").gameObject;
		addDescriptionToCreateMenu("Finishes the game!.", mainPanelGO.transform);
		Action createEndGameTrap = () =>
		{
			StaticObject endGame = MapCreator.AddObject(tile, tilePos, level, 387);
			endGame.SetProperties(false, true, true, false, 1, 63, 0, 0);
			Destroy(createMenuGO);
			afterCreatingAct?.Invoke(endGame);
		};
		addButtonToCreateMenu("Create", mainPanelGO.transform, createEndGameTrap);
	}
	private void createPitTrap(int id, int level, Vector2Int tilePos, MapTile tile, Action<StaticObject> afterCreatingAct = null)
	{
		createMenuGO = Instantiate(CreateObjectPrefab, transform);
		createMenuGO.transform.Find("TopBar/Title").gameObject.GetComponent<Text>().text = "Create pit trap";
		GameObject mainPanelGO = createMenuGO.transform.Find("MainPanel").gameObject;
		addDescriptionToCreateMenu("It seems that it doesn't do anything in UW1. It can be used as a dummy trap for 'check variable' trap that does nothing.", mainPanelGO.transform);
		int floorIndex = 0;
		Action<int> selectTexAct = (i) =>
		{
			floorIndex = i;
			Destroy(TexturePicker);
		};
		Action spawnTexPicker = () => CreateTexturePicker(GetCurrentLevelTextures(TextureType.Floor, false), selectTexAct, "Textures", "Select floor texture");
		Button setTexBut = addButtonToCreateMenu("Floor texture", mainPanelGO.transform, spawnTexPicker);
		Action createTrapAct = () =>
		{
			StaticObject pitTrap = MapCreator.AddObject(tile, tilePos, level, id);
			pitTrap.SetProperties(false, true, true, false, 1, floorIndex, floorIndex, 0);
			pitTrap.YPos = 0;      //Not sure if needed
			pitTrap.XPos = 0;
			MapCreator.SetGOPosition(pitTrap);
			Destroy(createMenuGO);
			afterCreatingAct?.Invoke(pitTrap);
		};
		addButtonToCreateMenu("Create", mainPanelGO.transform, createTrapAct);
	}
	private void createTerrainTrap(int id, int level, Vector2Int tilePos, MapTile tile, Action<StaticObject> afterCreatingAct = null)
	{
		createMenuGO = Instantiate(CreateObjectPrefab, transform);
		createMenuGO.transform.Find("TopBar/Title").gameObject.GetComponent<Text>().text = "Create terrain trap";
		GameObject mainPanelGO = createMenuGO.transform.Find("MainPanel").gameObject;
		addDescriptionToCreateMenu("Changes terrain.", mainPanelGO.transform);
		//Slider height = addSliderToCreateMenu("Height", mainPanelGO.transform, 0, 7.0f, 0);
		Slider height = addSliderToCreateMenu("Target height", mainPanelGO.transform, 0, 7.0f, 0);
		Action<bool> useTriggerAct = (b) => height.interactable = !b;
		Toggle useTrigger = addToggleToCreateMenu("Use trigger height", mainPanelGO.transform, false, useTriggerAct);
		Slider rangeX = addSliderToCreateMenu("Size X", mainPanelGO.transform, 0, 7.0f, 0);
		Slider rangeY = addSliderToCreateMenu("Size Y", mainPanelGO.transform, 0, 7.0f, 0);
		Dropdown tileType = addDropdownToCreateMenu("Tile type", mainPanelGO.transform, new string[] { "Solid", "Open" }, 1);
		int floorIndex = 0;
		int wallIndex = 0;
		Button floorButton = addImageButtonToCreateMenu("Floor texture", mainPanelGO.transform, Sprite.Create(MapCreator.GetFloorTextureFromIndex(floorIndex, CurrentLevel), new Rect(0, 0, 32.0f, 32.0f), Vector2.zero));
		Toggle useCurrentFloor = addToggleToCreateMenu("Don't change floor texture", mainPanelGO.transform, true, (b) => floorButton.interactable = !b);
		Button wallButton = addImageButtonToCreateMenu("Wall texture", mainPanelGO.transform, Sprite.Create(MapCreator.GetWallTextureFromIndex(wallIndex, CurrentLevel), new Rect(0, 0, 64.0f, 64.0f), Vector2.zero));
		Toggle useCurrentWall = addToggleToCreateMenu("Don't change wall texture", mainPanelGO.transform, true, (b) => wallButton.interactable = !b);
		Action<int> selectFloorAct = (i) =>
		{
			floorIndex = i;
			floorButton.GetComponent<Image>().sprite = Sprite.Create(MapCreator.GetFloorTextureFromIndex(floorIndex, CurrentLevel), new Rect(0, 0, 32.0f, 32.0f), Vector2.zero);
			Destroy(TexturePicker);
		};
		Action<int> selectWallAct = (i) =>
		{
			wallIndex = i;
			wallButton.GetComponent<Image>().sprite = Sprite.Create(MapCreator.GetWallTextureFromIndex(wallIndex, CurrentLevel), new Rect(0, 0, 64.0f, 64.0f), Vector2.zero);
			Destroy(TexturePicker);
		};
		Action spawnFloorPicker = () => CreateTexturePicker(GetCurrentLevelTextures(TextureType.Floor, false), selectFloorAct, "Textures", "Select new floor texture");
		Action spawnWallPicker = () => CreateTexturePicker(GetCurrentLevelTextures(TextureType.Wall, false), selectWallAct, "Textures", "Select new wall texture");
		floorButton.onClick.AddListener(() => spawnFloorPicker());
		wallButton.onClick.AddListener(() => spawnWallPicker());
		Action createTrapAct = () =>
		{
			StaticObject terrainTrap = MapCreator.AddObject(tile, tilePos, level, id);
			terrainTrap.XPos = (int)rangeX.value;
			terrainTrap.YPos = (int)rangeY.value;
			if (useTrigger.isOn)
				terrainTrap.ZPos = 120;
			else
				terrainTrap.ZPos = (int)(height.value * 16);
			if (useCurrentFloor.isOn)
				floorIndex = 11;
			int qual = tileType.value + (floorIndex << 1);
			if (useCurrentWall.isOn)
				wallIndex = 63;
			terrainTrap.SetProperties(false, true, true, false, 1, qual, wallIndex, 0);
			MapCreator.SetGOPosition(terrainTrap);
			MapCreator.SetGOHeight(terrainTrap);
			Destroy(createMenuGO);
			afterCreatingAct?.Invoke(terrainTrap);
		};
		addButtonToCreateMenu("Create", mainPanelGO.transform, createTrapAct);
	}
	private void createSpelltrap(int id, int level, Vector2Int tilePos, MapTile tile, Action<StaticObject> afterCreatingAct = null)
	{
		createMenuGO = Instantiate(CreateObjectPrefab, transform);
		createMenuGO.transform.Find("TopBar/Title").gameObject.GetComponent<Text>().text = "Spell trap";
		GameObject mainPanelGO = createMenuGO.transform.Find("MainPanel").gameObject;
		addDescriptionToCreateMenu("Casts a spell.", mainPanelGO.transform);
		addDescriptionToCreateMenu("Only some of the spells actually work (tested - projectiles, sheet lightning).", mainPanelGO.transform);
		addDescriptionToCreateMenu("It seems that it is also used to play cutscenes (used when releasing kidnapped girl).", mainPanelGO.transform);
		Dropdown dir = addDropdownToCreateMenu("Direction", mainPanelGO.transform, new string[] { "N", "NE", "E", "SE", "S", "SW", "W", "NW" }, 0);
		Slider height = addSliderToCreateMenu("Height", mainPanelGO.transform, tile.FloorHeight * 8.0f, 127.0f, tile.FloorHeight * 8.0f + 16.0f);
		Tuple<Dictionary<string, int>, string[]> pair = StaticObject.GetSpelltrapEffects();
		Dropdown spells = addDropdownToCreateMenu("Spell", mainPanelGO.transform, pair.Item2);
		int spellIndex = 0;
		spells.onValueChanged.AddListener((i) => spellIndex = pair.Item1[spells.options[i].text]);
		Action createTrapAct = () =>
		{
			StaticObject spellTrap = MapCreator.AddObject(tile, tilePos, level, id);
			int qual = (spellIndex & 0xF0) >> 4;
			int ownr = spellIndex & 0x0F;
			spellTrap.SetProperties(false, true, true, false, 1, qual, ownr, 0);
			spellTrap.ZPos = (int)height.value;
			spellTrap.Direction = dir.value;
			MapCreator.SetGOHeight(spellTrap);
			Destroy(createMenuGO);
			afterCreatingAct?.Invoke(spellTrap);
		};
		addButtonToCreateMenu("Create", mainPanelGO.transform, createTrapAct);
	}
	private void createCreateObjectTrap(int id, int level, Vector2Int tilePos, MapTile tile, Action<StaticObject> afterCreatingAct = null)
	{
		createMenuGO = Instantiate(CreateObjectPrefab, transform);
		createMenuGO.transform.Find("TopBar/Title").gameObject.GetComponent<Text>().text = "Create object trap";
		GameObject mainPanelGO = createMenuGO.transform.Find("MainPanel").gameObject;
		addDescriptionToCreateMenu("Creates an object from a template.", mainPanelGO.transform);
		addDescriptionToCreateMenu("Drag an object from map to add template.", mainPanelGO.transform);
		Slider height = addSliderToCreateMenu("Height", mainPanelGO.transform, tile.FloorHeight * 8.0f, 127.0f, tile.FloorHeight * 8.0f + 16.0f);
		Slider chance = addSliderToCreateMenu("Spawn chance", mainPanelGO.transform, 0, 100.0f, 100.0f);
		Action createCreateTrap = () =>
		{
			StaticObject createTrap = MapCreator.AddObject(tile, tilePos, level, id);
			int qual = ((int)chance.value) * -63 / 100 + 63;
			createTrap.SetProperties(false, true, true, false, 0, qual, 0, 0);
			createTrap.XPos = 0;
			createTrap.YPos = 0;
			MapCreator.SetGOPosition(createTrap);
			Destroy(createMenuGO);
			afterCreatingAct?.Invoke(createTrap);
		};
		addButtonToCreateMenu("Create", mainPanelGO.transform, createCreateTrap);
	}
	private void createDeleteObjectTrap(int id, int level, Vector2Int tilePos, MapTile tile, Action<StaticObject> afterCreatingAct = null)
	{
		createMenuGO = Instantiate(CreateObjectPrefab, transform);
		createMenuGO.transform.Find("TopBar/Title").gameObject.GetComponent<Text>().text = "Delete object trap";
		GameObject mainPanelGO = createMenuGO.transform.Find("MainPanel").gameObject;
		addDescriptionToCreateMenu("Destroys linked object.", mainPanelGO.transform);
		addDescriptionToCreateMenu("Doesn't work on monsters.", mainPanelGO.transform);
		Action createTrapAct = () =>
		{
			StaticObject trap = MapCreator.AddObject(tile, tilePos, level, id);
			trap.SetProperties(false, true, true, false, 1, 0, 0, 0);
			trap.XPos = 0;
			trap.YPos = 0;
			MapCreator.SetGOPosition(trap);
			Destroy(createMenuGO);
			afterCreatingAct?.Invoke(trap);
		};
		addButtonToCreateMenu("Create", mainPanelGO.transform, createTrapAct);
	}
	private void createInventoryTrap(int id, int level, Vector2Int tilePos, MapTile tile, Action<StaticObject> afterCreatingAct = null)
	{
		createMenuGO = Instantiate(CreateObjectPrefab, transform);
		createMenuGO.transform.Find("TopBar/Title").gameObject.GetComponent<Text>().text = "Inventory trap";
		GameObject mainPanelGO = createMenuGO.transform.Find("MainPanel").gameObject;
		addDescriptionToCreateMenu("Searches for an item in inventory, triggers linked object when it is found.", mainPanelGO.transform);
		int objectID = 0;
		Button button = addImageButtonToCreateMenu("Object", mainPanelGO.transform, MapCreator.GetObjectSpriteFromID(objectID));
		Action<int> selectObject = (i) =>
		{
			objectID = i;
			button.GetComponent<Image>().sprite = MapCreator.GetObjectSpriteFromID(objectID);
			Destroy(itemPicker);
		};
		Action spawnPicker = () => createFullItemList(selectObject, "Objects", "");
		button.onClick.AddListener(() => spawnPicker());
		Action createTrapAct = () =>
		{
			StaticObject trap = MapCreator.AddObject(tile, tilePos, level, id);
			int qual = (objectID & 0x1E0) >> 5;
			int ownr = objectID & 0x1F;
			trap.SetProperties(false, true, true, false, 1, qual, ownr, 0);
			Destroy(createMenuGO);
			afterCreatingAct?.Invoke(trap);
		};
		addButtonToCreateMenu("Create", mainPanelGO.transform, createTrapAct);
	}
	private void createMoveTrigger(int id, int level, Vector2Int tilePos, MapTile tile, Action<StaticObject> afterCreatingAct = null)
	{
		createMenuGO = Instantiate(CreateObjectPrefab, transform);
		createMenuGO.transform.Find("TopBar/Title").gameObject.GetComponent<Text>().text = "Create move trigger";
		GameObject mainPanelGO = createMenuGO.transform.Find("MainPanel").gameObject;
		Dropdown flags = addDropdownToCreateMenu("Trigger type", mainPanelGO.transform, new string[] { "Unlimited", "One time only" });
		Action createMoveTriggerAct = () =>
		{
			StaticObject moveTrig = MapCreator.AddObject(tile, tilePos, level, id);
			moveTrig.SetProperties(false, true, true, true, flags.value == 0 ? 6 : 4, 0, 0, 0);
			Destroy(createMenuGO);
			afterCreatingAct?.Invoke(moveTrig);
		};
		addButtonToCreateMenu("Create", mainPanelGO.transform, createMoveTriggerAct);
	}
	private void createUseTrigger(int id, int level, Vector2Int tilePos, MapTile tile, Action<StaticObject> afterCreatingAct = null)
	{
		createMenuGO = Instantiate(CreateObjectPrefab, transform);
		createMenuGO.transform.Find("TopBar/Title").gameObject.GetComponent<Text>().text = "Create use trigger";
		GameObject mainPanelGO = createMenuGO.transform.Find("MainPanel").gameObject;
		Dropdown flags = addDropdownToCreateMenu("Trigger type", mainPanelGO.transform, new string[] { "Unlimited", "One time only" });
		Action createMoveTriggerAct = () =>
		{
			StaticObject moveTrig = MapCreator.AddObject(tile, tilePos, level, id);
			moveTrig.SetProperties(false, true, true, true, flags.value == 0 ? 6 : 4, 0, 0, 0);
			Destroy(createMenuGO);
			afterCreatingAct?.Invoke(moveTrig);
		};
		addButtonToCreateMenu("Create", mainPanelGO.transform, createMoveTriggerAct);
	}
	private void createLookTrigger(int id, int level, Vector2Int tilePos, MapTile tile, Action<StaticObject> afterCreatingAct = null)
	{
		createMenuGO = Instantiate(CreateObjectPrefab, transform);
		createMenuGO.transform.Find("TopBar/Title").gameObject.GetComponent<Text>().text = "Create look trigger";
		GameObject mainPanelGO = createMenuGO.transform.Find("MainPanel").gameObject;
		Dropdown flags = addDropdownToCreateMenu("Trigger type", mainPanelGO.transform, new string[] { "Unlimited", "One time only" });
		Slider difficulty = addSliderToCreateMenu("Difficulty", mainPanelGO.transform, 0, 30.0f, 0);
		Action createMoveTriggerAct = () =>
		{
			StaticObject moveTrig = MapCreator.AddObject(tile, tilePos, level, id);
			moveTrig.SetProperties(false, true, true, true, flags.value == 0 ? 6 : 4, 0, 0, 0);
			moveTrig.ZPos = (int)difficulty.value;
			Destroy(createMenuGO);
			afterCreatingAct?.Invoke(moveTrig);
		};
		addButtonToCreateMenu("Create", mainPanelGO.transform, createMoveTriggerAct);
	}
	private void createPickUpTrigger(int id, int level, Vector2Int tilePos, MapTile tile, Action<StaticObject> afterCreatingAct = null)
	{
		createMenuGO = Instantiate(CreateObjectPrefab, transform);
		createMenuGO.transform.Find("TopBar/Title").gameObject.GetComponent<Text>().text = "Create pick up trigger";
		GameObject mainPanelGO = createMenuGO.transform.Find("MainPanel").gameObject;
		Dropdown flags = addDropdownToCreateMenu("Trigger type", mainPanelGO.transform, new string[] { "Unlimited", "One time only" });
		Action createMoveTriggerAct = () =>
		{
			StaticObject moveTrig = MapCreator.AddObject(tile, tilePos, level, id);
			moveTrig.SetProperties(false, true, true, true, flags.value == 0 ? 6 : 4, 0, 0, 0);
			Destroy(createMenuGO);
			afterCreatingAct?.Invoke(moveTrig);
		};
		addButtonToCreateMenu("Create", mainPanelGO.transform, createMoveTriggerAct);
	}
	private void createOpenTrigger(int id, int level, Vector2Int tilePos, MapTile tile, Action<StaticObject> afterCreatingAct = null)
	{
		createMenuGO = Instantiate(CreateObjectPrefab, transform);
		createMenuGO.transform.Find("TopBar/Title").gameObject.GetComponent<Text>().text = "Create open trigger";
		GameObject mainPanelGO = createMenuGO.transform.Find("MainPanel").gameObject;
		Dropdown flags = addDropdownToCreateMenu("Trigger type", mainPanelGO.transform, new string[] { "Unlimited", "One time only" });
		Action createMoveTriggerAct = () =>
		{
			StaticObject moveTrig = MapCreator.AddObject(tile, tilePos, level, id);
			moveTrig.SetProperties(false, true, true, true, flags.value == 0 ? 6 : 4, 0, 0, 0);
			Destroy(createMenuGO);
			afterCreatingAct?.Invoke(moveTrig);
		};
		addButtonToCreateMenu("Create", mainPanelGO.transform, createMoveTriggerAct);
	}
	private void createUnlockTrigger(int id, int level, Vector2Int tilePos, MapTile tile, Action<StaticObject> afterCreatingAct = null)
	{
		createMenuGO = Instantiate(CreateObjectPrefab, transform);
		createMenuGO.transform.Find("TopBar/Title").gameObject.GetComponent<Text>().text = "Create unlock trigger";
		GameObject mainPanelGO = createMenuGO.transform.Find("MainPanel").gameObject;
		Dropdown flags = addDropdownToCreateMenu("Trigger type", mainPanelGO.transform, new string[] { "Unlimited", "One time only" });
		Action createMoveTriggerAct = () =>
		{
			StaticObject moveTrig = MapCreator.AddObject(tile, tilePos, level, id);
			moveTrig.SetProperties(false, true, true, true, flags.value == 0 ? 6 : 4, 0, 0, 0);
			Destroy(createMenuGO);
			afterCreatingAct?.Invoke(moveTrig);
		};
		addButtonToCreateMenu("Create", mainPanelGO.transform, createMoveTriggerAct);
	}
	private Dropdown addDropdownToCreateMenu(string text, Transform parent, Dictionary<string, int> dict, int initialVal = 0, float width = 0)
	{
		string[] options = new string[dict.Count];
		int i = 0;
		foreach (var item in dict)
		{
			options[i] = item.Key;
			i++;
		}
		return addDropdownToCreateMenu(text, parent, options, initialVal, width);
	}
	private Dropdown addDropdownToCreateMenu(string text, Transform parent, string[] options, int initialVal = 0, float width = 0)
	{		
		GameObject dropdownGO = Instantiate(CreateObjectDropdownPrefab, parent);
		dropdownGO.transform.Find("Description").GetComponent<Text>().text = text;
		Dropdown dropdown = dropdownGO.GetComponentInChildren<Dropdown>();
		dropdown.options = new List<Dropdown.OptionData>();
		foreach (var option in options)
			dropdown.options.Add(new Dropdown.OptionData(option));
		dropdown.value = initialVal;
		dropdown.template.offsetMax = new Vector2(width, 0);
		return dropdown;
	}
	private Slider addSliderToCreateMenu(string text, Transform parent, float min, float max, float initialVal = 0, Func<int, int> valAct = null)
	{
		GameObject sliderGO = Instantiate(CreateObjectSliderPrefab, parent);
		sliderGO.transform.Find("Description").GetComponent<Text>().text = text;
		Slider slider = sliderGO.GetComponentInChildren<Slider>();
		slider.minValue = min;
		slider.maxValue = max;
		slider.value = initialVal;
		Text handleText = slider.handleRect.GetComponentInChildren<Text>();
		if (valAct == null)
		{
			handleText.text = (slider.value).ToString();
			slider.onValueChanged.AddListener((f) => handleText.text = ((int)f).ToString());
		}
		else
		{
			handleText.text = valAct((int)slider.value).ToString();
			slider.onValueChanged.AddListener((f) => handleText.text = (valAct((int)f)).ToString());
		}
		return slider;
	}
	private InputField addLargeInputFieldToCreateMenu(string text, Transform parent)
	{
		GameObject inputGO = Instantiate(CreateObjectLargeTextPrefab, parent);
		inputGO.transform.Find("Description").GetComponent<Text>().text = text;
		InputField input = inputGO.GetComponentInChildren<InputField>();
		//input.lineType = InputField.LineType.SingleLine;
		StartCoroutine(setupLargeInput(input));
		return input;
	}
	private IEnumerator setupLargeInput(InputField input)
	{
		yield return new WaitForEndOfFrame();
		RectTransform inputCaretRt = input.transform.Find("InputField Input Caret").GetComponent<RectTransform>();
		inputCaretRt.anchorMax = Vector2.one;
		inputCaretRt.anchorMin = Vector2.zero;
		inputCaretRt.sizeDelta = Vector2.zero;
		inputCaretRt.anchoredPosition = Vector2.zero;
	}
	private Button addButtonToCreateMenu(string text, Transform parent, Action act = null)
	{
		GameObject buttonGO = Instantiate(CreateObjectButtonPrefab, parent);
		buttonGO.GetComponentInChildren<Text>().text = text;
		Button button = buttonGO.GetComponentInChildren<Button>();
		if(act != null)
			button.onClick.AddListener(() => act());
		return button;
	}
	private Button addImageButtonToCreateMenu(string text, Transform parent, Sprite sprite, Action act = null)
	{
		GameObject buttonGO = Instantiate(CreateObjectImageButtonPrefab, parent);
		buttonGO.transform.Find("Description").GetComponent<Text>().text = text;
		Button button = buttonGO.GetComponentInChildren<Button>();
		button.GetComponent<Image>().sprite = sprite;
		if (act != null)
			button.onClick.AddListener(() => act());
		return button;
	}
	private Toggle addToggleToCreateMenu(string text, Transform parent, bool initialVal = false, System.Action<bool> act = null)
	{
		GameObject toggleGO = Instantiate(CreateObjectTogglePrefab, parent);
		toggleGO.transform.Find("Description").GetComponent<Text>().text = text;
		Toggle toggle = toggleGO.GetComponentInChildren<Toggle>();
		toggle.isOn = initialVal;
		if (act != null)
			toggle.onValueChanged.AddListener((b) => act(b));
		return toggle;
	}
	private InputField addInputToCreateMenu(string text, Transform parent, int min = 0, int max = 512)
	{
		GameObject inputGO = Instantiate(CreateObjectInputPrefab, parent);
		inputGO.transform.Find("Description").GetComponent<Text>().text = text;
		InputField input = inputGO.GetComponentInChildren<InputField>();
		input.contentType = InputField.ContentType.IntegerNumber;
		Action<string> clampVals = (str) =>
		{
			if (string.IsNullOrEmpty(str))
				str = "0";
			int val = int.Parse(str);
			val = Mathf.Clamp(val, min, max);
			input.text = val.ToString();
		};
		input.onEndEdit.AddListener((str) => clampVals(str));
		return input;
	}
	private InputField addStringInputToCreateMenu(string text, Transform parent)
	{
		GameObject inputGO = Instantiate(CreateObjectInputPrefab, parent);
		inputGO.transform.Find("Description").GetComponent<Text>().text = text;
		InputField input = inputGO.GetComponentInChildren<InputField>();
		input.contentType = InputField.ContentType.Alphanumeric;
		return input;
	}
	private void addDescriptionToCreateMenu(string text, Transform parent)
	{
		GameObject descrGO = Instantiate(CreateObjectDescriptionPrefab, parent);
		descrGO.transform.Find("Description").GetComponent<Text>().text = text;
	}

	#endregion

	#region Levels

	public void ChangeLevelView(Slider slider)
	{
		if (!MapCreator.IsInitialized)
			return;
		InputManager.DeselectObject();
		ObjectProperties.SetActive(false);
		TilePropertiesObject.SetActive(false);
		levels[CurrentLevel - 1].SetActive(false);
		CurrentLevel = (int)slider.value;
		levels[CurrentLevel - 1].SetActive(true);
		if (CurrentLevel == 1)
			StartGO.SetActive(true);
		else
			StartGO.SetActive(false);
		changeTemplateFloorButton(MapCreator.GetFloorTextureFromIndex(0, CurrentLevel));
		changeTemplateWallButton(MapCreator.GetWallTextureFromIndex(0, CurrentLevel));
		slider.handleRect.GetComponentInChildren<Text>().text = CurrentLevel.ToString();
		updateSectorDropdown(CurrentLevel);
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
			if (clearConvsTog.isOn)
				MapCreator.ClearConversations();
			levels = MapCreator.CreateLevels();
			levels[0].SetActive(true);

			Slider levelSlider = LevelSlider.GetComponent<Slider>();
			levelSlider.maxValue = MapCreator.GetLevelCount();
			levelSlider.handleRect.GetComponentInChildren<Text>().text = "1";
			levelSlider.value = 1;

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
		Debug.Log($"updateSectorDropdown, {levelNumber}, CurrentLevel {CurrentLevel}");
		if (levelNumber == CurrentLevel)
		{
			Debug.Log($"Updating");
			SectorDropdown.ClearOptions();
			MapCreator.LevelData[CurrentLevel - 1].Sectors.ForEach(s => SectorDropdown.options.Add(new Dropdown.OptionData(s.Name)));			
			SectorDropdown.value = 0;			
		}
	}

	#endregion

	#region Other

	public void SpawnPopupMessage(string message, Sprite spr = null, bool textOnLeft = false, bool destroy = false)
	{
		if (destroy)
			Destroy(popup);
		popup = Instantiate(PopupMessagePrefab, transform);
		Text text = popup.GetComponentInChildren<Text>();
		text.text = message;
		popup.GetComponent<Button>().onClick.AddListener(() => Destroy(popup));
		Image image = null;
		for (int i = 0; i < popup.transform.childCount; i++)
		{
			image = popup.transform.GetChild(i).GetComponent<Image>();
			if (image)
				break;
		}
		if (spr)
		{
			image.sprite = spr;
		}
		else
		{
			image.gameObject.SetActive(false);
		}
		if (textOnLeft)
		{
			text.alignment = TextAnchor.MiddleLeft;
		}
		popup.transform.SetAsLastSibling();
	}

	public void CreateFileExplorer()
	{
		string path = DataReader.FilePath;
		if (string.IsNullOrEmpty(path) && !DataReader.ValidateFilePath(path))
			path = Application.dataPath;

		fileExplorer = Instantiate(FileExplorerPrefab, transform);
		fileExplorer.GetComponent<FileExplorer>().InitFileReader(FileExplorer.GetUpperPath(path));
	}

	public void Undo()
	{
		if (!MapCreator.IsInitialized)
			return;
		Command last = InputManager.PopLastCommand();
		if(last != null)
			last.Undo();
	}


	public void SetEditorMode(Dropdown dropdown)
	{
		InputManager.DeselectObject();
		ObjectProperties.SetActive(false);
		TilePropertiesObject.SetActive(false);
		CurrentMode = (EditorMode)(dropdown.value + 1);
		if (CurrentMode == EditorMode.Tile)
		{
			TemplatePanel.SetActive(true);
			TextureTemplatePanel.SetActive(false);
			SectorPanel.SetActive(false);
		}
		else if (CurrentMode == EditorMode.Texture)
		{
			TemplatePanel.SetActive(false);
			TextureTemplatePanel.SetActive(true);
			SectorPanel.SetActive(false);
		}
		else if (CurrentMode == EditorMode.Object)
		{
			TemplatePanel.SetActive(false);
			TextureTemplatePanel.SetActive(false);
			SectorPanel.SetActive(false);
		}
		else if (CurrentMode == EditorMode.Sector)
		{
			TemplatePanel.SetActive(false);
			TextureTemplatePanel.SetActive(false);
			SectorPanel.SetActive(true);
		}
	}

	public void SetCursorMode(CursorType cursorMode)
	{
		
		if (cursorMode == CurrentCursorMode)
			return;
		if (cursorMode == CursorType.Normal)
			Cursor.SetCursor(NormalCursor, new Vector2(9.0f, 6.0f), CursorMode.Auto);
		else if (cursorMode == CursorType.ArrowX)
			Cursor.SetCursor(ArrowXCursor, new Vector2(16.0f, 16.0f), CursorMode.Auto);
		else if (cursorMode == CursorType.ArrowY)
			Cursor.SetCursor(ArrowYCursor, new Vector2(16.0f, 16.0f), CursorMode.Auto);
	}


	public bool IsWindowActive()
	{
		if (listMenu || fileExplorer || textureExplorer || TexturePicker || conversationWindow || createMenuGO)
			return true;
		return false;
	}

	public void SpawnTooltip(string text)
	{
		DestroyTooltip();
		tooltip = Instantiate(TooltipPrefab, transform);
		tooltip.GetComponentInChildren<Text>().text = text;
		tooltip.transform.position = Input.mousePosition;
	}

	public void DestroyTooltip()
	{
		if (tooltip)
			Destroy(tooltip);
	}

	public void RunDocumentation()
	{
		string doc = Application.streamingAssetsPath + "/documentation.txt";
		try
		{
			System.Diagnostics.Process.Start("notepad.exe", doc);
		}
		catch(Exception e)
		{
			SpawnPopupMessage("Error while trying to run notepad.exe\n" + e.Message);
		}
	}

	public void DestroyGameObject(GameObject go)
	{
		Destroy(go);
	}

	public void DebugString(string str)
	{
		Debug.Log(str);
	}


	private string clampVals(string str, int min, int max)
	{
		int val = int.Parse(str);
		val = Mathf.Clamp(val, min, max);
		return val.ToString();
	}
	//public void DebugConversation(Conversation conv)
	//{
	//	ConversationManager convMan = new ConversationManager();
	//	StartCoroutine(convMan.DoConversation(conv));
	//}

	#endregion
}

#region Obsolete

//public void CreateConversationList()
//{
//	if (!MapCreator.IsInitialized)
//		return;
//	if (listMenu)
//		return;

//	listMenu = Instantiate(ListMenuPrefab, transform);
//	ListMenu list = listMenu.GetComponent<ListMenu>();
//	list.Title.text = "List of conversations";
//	list.Information.text = "";
//	list.OptionsPanel.SetActive(true);
//	Toggle debugTog = list.CreateToggleOption("Debug mode");
//	Toggle singleTog = list.CreateToggleOption("Single code processing", false);
//	Toggle clearTog = list.CreateToggleOption("Clean conversation (no saved progress)");
//	int[] impGlobs = ConversationManager.GetImportedGlobals();
//	System.Action createOptionsWindow = () =>
//	{
//		GameObject optWinGO = Instantiate(OptionWindowPrefab, transform);
//		OptionsWindow optWin = optWinGO.GetComponent<OptionsWindow>();
//		optWin.Title.text = "Conversation parameters";
//		optWin.AddDefaultOptions(impGlobs);
//	};
//	Button optBut = list.CreateButtonOption("Parameters", createOptionsWindow);
//	debugTog.onValueChanged.AddListener((x) => { if (x == false) singleTog.isOn = false; singleTog.interactable = x; });

//	//Max limit for convs - 256?
//	for (int i = 0; i < MapCreator.ConversationData.Conversations.Length; i++)
//	{
//		int new_i = i;
//		Conversation conv = MapCreator.ConversationData.Conversations[new_i];
//		if (!conv)
//			continue;

//		string npcName = "";
//		if (i < 256)
//			npcName = StringBlock.GetNPCName(i);
//		else
//			npcName = StaticObject.GetMonsterName(i - 256) + "(generic)";

//		GameObject stringItem = Instantiate(StringBlockItem, list.Content.transform);
//		Text text = stringItem.GetComponentInChildren<Text>();
//		text.text = new_i + ": " + npcName;
//		Button button = stringItem.GetComponentInChildren<Button>();
//		impGlobs[14] = conv.Slot;
//		//button.onClick.AddListener(() => DataWriter.SaveStringToTxt(conv.DumpConversation(), "conv_" + new_i + ".txt"));
//		System.Action act = () =>
//		{
//			PlayConversation(conv, impGlobs, debugTog.isOn, singleTog.isOn, clearTog.isOn);
//			Destroy(listMenu);
//		};
//		button.onClick.AddListener(() => act());
//		if(!conv.Converted)
//		{
//			button.interactable = false;
//			text.text += " [unconverted]";
//		}
//	}
//}


//string mobTest = "";
//for (int i = 0; i < levData.Count; i++)
//{
//	LevelData ld = levData[i];
//	mobTest += "==LEVEL " + (i + 1) + "==\n";
//	for (int j = 0; j < 256; j++)
//	{
//		StaticObject so = ld.Objects[j];
//		if (so && so is MobileObject)
//		{
//			MobileObject mo = (MobileObject)so;
//			string mobName = "";
//			for (int k = 0; k < 20; k++)
//			{
//				if (k < mo.Name.Length)
//					mobName += mo.Name[k];
//				else
//					mobName += " ";
//			}
//			mobTest += string.Format("{0} : \t{20}\t\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12}\t{13}\t{14}\t{15}\t{16}\t{17}\t\t{18}\t{19}\n", mobName, mo.B9, mo.BA, mo.BB, mo.BC, mo.BD, mo.BE, mo.BF, mo.B10, mo.B11, mo.B12, mo.B13, mo.B14, mo.B15, mo.B16, mo.B17, mo.B18, mo.B19, mo.Level, mo.MobHeight, mo.CurrentAdress);
//		}
//	}

//}
//DataWriter.SaveStringToTxt(mobTest, "mobtest.txt");
#endregion