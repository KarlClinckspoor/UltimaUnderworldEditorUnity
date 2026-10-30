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

/// <summary>
/// The editor's UI: menus, panels and the edits they start. Split by region into
/// UIManager.*.cs (partial classes, review package 10); this file keeps the fields, start-up and
/// the rest.
/// </summary>
public partial class UIManager : MonoBehaviour {

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
		// After File > Open with data loaded, the scene was reloaded to open another game
		string pendingOpen = UltimateEditor.Uw1.UI.GameOpener.TakePending();
		// the previous game's static state is cleared here, once its scene is gone
		if (pendingOpen != null)
			UltimateEditor.Uw1.UI.GameOpener.ResetSession();
		string path = pendingOpen ?? DataReader.GetPathFromFile();
		bool valid = !string.IsNullOrEmpty(path) && DataReader.ValidateFilePath(path) && DataReader.ValidateFile(path);
		if (valid)
			DataReader.FilePath = UltimateEditor.Uw1.UI.GameOpener.Slashes(GameSession.Choose(path));
		else
			DataReader.FilePath = Application.dataPath;
		SetCursorMode(CursorType.Normal);
		if (pendingOpen != null && valid)
			StartCoroutine(loadNextFrame());
	}

	IEnumerator loadNextFrame()
	{
		yield return null;
		LoadData();
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

	bool loading;
	/// <summary>True while Load runs (several frames, before MapCreator.IsInitialized).</summary>
	public bool IsLoading { get { return loading; } }
	/// <summary>A load was started in this scene; if it failed, its leftovers need a fresh scene.</summary>
	public bool LoadAttempted { get; private set; }

	public void LoadData()
	{
		if (MapCreator.IsInitialized)
		{
			SpawnPopupMessage("Data is already loaded.");
			return;
		}
		if (loading)
			return;
		if (string.IsNullOrEmpty(DataReader.FilePath) || !DataReader.ValidateFilePath(DataReader.FilePath))
		{
			SpawnPopupMessage("No game chosen yet: use File > Open game... and choose the game's UW.EXE or UW2.EXE.");
			return;
		}
		if(!DataReader.ValidateFile(DataReader.FilePath))
		{
			SpawnPopupMessage("Invalid file, should be LEV.ARK from DATA folder.");
			return;
		}

		CurrentLevel = 1;
		CurrentMode = EditorMode.Object;
		LoadAttempted = true;
		StartCoroutine(Load());
	}

	/// <summary>Runs loadSteps with the loading flag set, and reports an unexpected exception.</summary>
	private IEnumerator Load()
	{
		loading = true;
		try
		{
			IEnumerator steps = loadSteps();
			while (true)
			{
				bool more;
				Exception failure = null;
				try { more = steps.MoveNext(); }
				catch (Exception e) { more = false; failure = e; }
				if (failure != null)
				{
					Debug.LogException(failure);
					if (popup) Destroy(popup);
					SpawnPopupMessage("Failed to load the game data\n" + failure.Message);
				}
				if (!more) break;
				yield return steps.Current;
			}
		}
		finally { loading = false; }
	}

	private IEnumerator loadSteps()
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
		ModePanel.SetActive(true);
		BuildLevelPicker();
		syncLevelPicker(1);
		ShowLevelName();
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

	/// <summary>File > Open: the game's UW.EXE or UW2.EXE, through the system's file dialog
	/// where there is one (Extensions/UI/GameOpener.cs).</summary>
	public void CreateFileExplorer()
	{
		UltimateEditor.Uw1.UI.GameOpener.Browse(this);
	}

	/// <summary>The editor's own file explorer, starting in folder.</summary>
	public void CreateInAppFileExplorer(string folder)
	{
		if (fileExplorer)
			Destroy(fileExplorer);
		if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
			folder = FileExplorer.GetUpperPath(Application.dataPath);
		fileExplorer = Instantiate(FileExplorerPrefab, transform);
		fileExplorer.GetComponent<FileExplorer>().InitFileReader(UltimateEditor.Uw1.UI.GameOpener.Slashes(folder));
	}

	/// <summary>Runs an edit of a level's objects as an undoable step (UI/Command.cs,
	/// SnapshotCommand). Returns what the edit returned; a refused edit (false) is not kept.</summary>
	public bool Undoable(int level, string description, Func<bool> edit)
	{
		bool ok = false;
		var c = new SnapshotCommand(level, description, () => ok = edit());
		c.Do();
		if (ok)
			InputManager.Commands.Add(c);
		return ok;
	}

	public void Undo()
	{
		if (!MapCreator.IsInitialized)
			return;
		Command last = InputManager.PopLastCommand();
		if (last == null)
		{
			SpawnPopupMessage("Nothing to undo.");
			return;
		}
		// the level is read back afresh: what was selected on it is gone
		InputManager.DeselectObject();
		ObjectProperties.SetActive(false);
		last.Undo();
		SpawnPopupMessage("Undone: " + last.Description);
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
