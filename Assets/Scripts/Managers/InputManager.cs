using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum DataType
{
	Null,
	Tile,
	Object,
	Invalid
}

public class InputManager : MonoBehaviour {

	public CameraManager CameraManager;
	public MapCreator MapCreator;
	public UIManager UIManager;
	public GameObject TilePanelObject;
	public GameObject StaticObjectPanelObject;
	//public GameObject PropertiesPanelObject;
	public GameObject TilePropertiesObject;

	private Vector3 mousePosition;
	private Vector3 lastMousePosition;

	private GameObject currentObject;
	public GameObject SelectedObject { get; private set; }

	private MapTile draggedFromTile;
	private GameObject clickedObject;
	private GameObject shadowObject;

	private Vector3 dragStart;
	private Vector3 dragStartScreen;
	/// <summary>How far (screen pixels) the mouse must move with the button held before a click
	/// becomes a drag: a click whose mouse twitches must not move the object.</summary>
	const float DragThreshold = 5f;
	private bool isDragging;
	private float step;
	private float offset;

	public LineRenderer triggerLink { get; private set; }
	//private GameObject[] triggerArrows;

	public CommandList<Command> Commands = new CommandList<Command>(30);

	private void Start()
	{
		UIManager.OnSelectNonGO += DeselectObject;
		UIManager.OnSelectObject += (go) => SelectedObject = go;
		UIManager.OnStartLink += (lr) => createTriggerLink(lr);
	}

	/// <summary>While another window has the focus the editor ignores the mouse (it could
	/// select objects under another window) and draws only a few frames a second. It keeps
	/// running, so autosave still works.</summary>
	const int BackgroundFrameRate = 5;
	int focusedVSync = -1, focusedFrameRate = -1;

	private void OnApplicationFocus(bool focused)
	{
		if (!focused)
		{
			focusedVSync = QualitySettings.vSyncCount;
			focusedFrameRate = Application.targetFrameRate;
			QualitySettings.vSyncCount = 0;	// targetFrameRate is ignored while vSync is on
			Application.targetFrameRate = BackgroundFrameRate;
			// a drag in progress is dropped back where it was
			if (clickedObject && isDragging)
			{
				StaticObjectScript sos = clickedObject.GetComponent<StaticObjectScript>();
				if (sos && sos.StaticObject) MapCreator.SetGOPosition(sos.StaticObject);
				BoxCollider col = clickedObject.GetComponentInChildren<BoxCollider>();
				if (col) col.enabled = true;
			}
			if (shadowObject)
				Destroy(shadowObject);
			shadowObject = null;
			dragStart = Vector3.zero;
			clickedObject = null;
			isDragging = false;
		}
		else if (focusedFrameRate != -1 || focusedVSync != -1)
		{
			QualitySettings.vSyncCount = Mathf.Max(0, focusedVSync);
			Application.targetFrameRate = focusedFrameRate;
		}
	}

	private void Update()
	{
		if (!Application.isFocused)
			return;
		//Debug.Log(MapCreator.GetPositionUnderMouse(true, true));
		Rect screenRect = new Rect(0, 0, Screen.width, Screen.height);
		if (!screenRect.Contains(Input.mousePosition))
			return;
		mousePosition = MapCreator.GetWorldPosition(Input.mousePosition, true);
		if (mousePosition != lastMousePosition)
			UIManager.DestroyTooltip();
		if(!UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
		{
			GameObject newObject = MapCreator.GetObjectUnderMouse(UIManager.CurrentMode);
			//Debug.Log($"newObject : {newObject}, mode : {UIManager.CurrentMode}");
			if (currentObject != newObject && newObject != null)
			{
				SetObject(currentObject, false);
				currentObject = newObject;
				SetObject(currentObject, true);
			}
			else if(newObject == null)
			{
				SetObject(currentObject, false);
				currentObject = null;
			}
			if (UIManager.CurrentMode == EditorMode.Object)
				HandleObjectMode();
			else if (UIManager.CurrentMode == EditorMode.Tile)
				HandleTileMode();
			else if (UIManager.CurrentMode == EditorMode.Texture)
				HandleTextureMode();
			else if (UIManager.CurrentMode == EditorMode.Sector)
				HandleSectorMode();

			if(triggerLink)
			{
				triggerLink.SetPosition(1, mousePosition);
			}
		}
		lastMousePosition = mousePosition;
	}
	private Tuple<Vector2Int, MapTile> getClickedPos()
	{
		Vector3 basePos = MapCreator.GetWorldPosition(Input.mousePosition, false, true);
		Vector3 mousePos = MapCreator.GetWorldPosition(Input.mousePosition, false);
		MapTile tile = MapCreator.GetTile(basePos, UIManager.CurrentLevel);		
		float sx = (mousePos.x / (MapCreator.TileSize * 2.0f) % basePos.x) * 7.0f;
		float sy = (mousePos.y / (MapCreator.TileSize * 2.0f) % basePos.y) * 7.0f;
		Vector2Int pos = new Vector2Int((int)sx, (int)sy);
		return new Tuple<Vector2Int, MapTile>(pos, tile);
	}
	private void HandleObjectMode()
	{
		if(MapCreator.IsInitialized)
			getClickedPos();
		if (Input.GetMouseButtonDown(0))
		{
			ObjectModeClickDown();
		}
		if (Input.GetMouseButton(0))
		{
			if (dragStart != Vector3.zero && !isDragging && clickedObject && (Input.mousePosition - dragStartScreen).magnitude >= DragThreshold)
			{
				isDragging = true;
				clickedObject.GetComponentInChildren<BoxCollider>().enabled = false;
			}
			if (clickedObject && isDragging)
			{
				Vector3 basePos = MapCreator.GetWorldPosition(Input.mousePosition, false, true);
				Vector3 mousePos = MapCreator.GetWorldPosition(Input.mousePosition, false);
				clickedObject.transform.position = mousePos;
				MapTile tile = MapCreator.GetTile(basePos, UIManager.CurrentLevel);
				if (tile && shadowObject)
				{
					StaticObject dragged = clickedObject.GetComponent<StaticObjectScript>().StaticObject;
					if (MapCreator.FinePositionIsData(dragged))
					{
						// only the square changes: show it at its own fine position there
						float ts = MapCreator.TileSize * 2.0f;
						shadowObject.transform.position = new Vector3(tile.Position.x * ts + dragged.XPos / 7.0f * ts, tile.Position.y * ts + dragged.YPos / 7.0f * ts, MapCreator.GetTileHeight(tile));
					}
					else
					{
						float step_x = (mousePos.x % step) < (step / 2) ? (mousePos.x % step) : (mousePos.x % step) - step;
						float step_y = (mousePos.y % step) < (step / 2) ? (mousePos.y % step) : (mousePos.y % step) - step;
						shadowObject.transform.position = new Vector3(mousePos.x, mousePos.y, MapCreator.GetTileHeight(tile)) - new Vector3(step_x + offset, step_y + offset, 0);
					}
				}
			}

		}
		if (Input.GetMouseButtonUp(0))
		{
			// The drag state is always cleared, even when dropping or selecting fails: left set,
			// the next drag on empty floor dragged the last object again, without its shadow.
			try
			{
				ObjectModeClickUp();
				SelectObject();
			}
			finally
			{
				if (shadowObject)
					Destroy(shadowObject);
				shadowObject = null;
				dragStart = Vector3.zero;
				clickedObject = null;
				draggedFromTile = null;
				isDragging = false;
				if(triggerLink)
					Destroy(triggerLink.gameObject);
				UIManager.StopLinking();
				triggerLink = null;
			}
		}
		if(Input.GetMouseButtonDown(1))
		{
			if (isDragging || clickedObject)
				return;
			if (currentObject)
			{
				StaticObject so = currentObject.transform.parent.GetComponent<StaticObjectScript>().StaticObject;
				UIManager.SpawnContextMenu(Input.mousePosition, so);
				//Debug.LogFormat("Current object : {0}", so.Name);
			}
			else
			{
				Tuple<Vector2Int, MapTile> data = getClickedPos();
				UIManager.SpawnContextMenu(Input.mousePosition, data.Item1, data.Item2);
				//Debug.LogFormat("No current object");
			}
		}
	}

	private void HandleTileMode()
	{
		if (Input.GetMouseButton(1))
		{
			if (currentObject && GetDataType(currentObject) == DataType.Tile)
			{
				//currentObject.GetComponent<BoxCollider>().enabled = false;
				clickedObject = currentObject.transform.parent.gameObject;
				UIManager.ChangeTileType(clickedObject, TileType.Open);
			}
		}
		if(Input.GetMouseButtonDown(0))
		{
			if (currentObject && GetDataType(currentObject) == DataType.Tile)
			{
				clickedObject = currentObject.transform.parent.gameObject;
				SelectTile();
			}
		}
		float scroll = Input.GetAxis("Mouse ScrollWheel");
		if(scroll != 0 && currentObject && GetDataType(currentObject) == DataType.Tile)
		{
			clickedObject = currentObject.transform.parent.gameObject;
			UIManager.ChangeTileHeight(clickedObject, scroll);
		}
		if (Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(1))
		{
			clickedObject = null;
		}
	}
	private void HandleTextureMode()
	{
		if (Input.GetMouseButton(0) || Input.GetMouseButton(1))
		{
			if (currentObject && GetDataType(currentObject) == DataType.Tile)
			{
				clickedObject = currentObject.transform.parent.gameObject;
				UIManager.ChangeTileTexture(clickedObject);
			}
		}
	}
	private void HandleSectorMode()
	{		
		if (Input.GetMouseButton(0) || Input.GetMouseButton(1))
		{
			if (currentObject && GetDataType(currentObject) == DataType.Tile)
			{
				clickedObject = currentObject.transform.parent.gameObject;
				UIManager.ChangeTileSector(clickedObject, Input.GetMouseButton(1) && !Input.GetMouseButton(0));
			}
		}
	}


	private void ObjectModeClickDown()
	{
		if (currentObject && GetDataType(currentObject) == DataType.Object)
		{
			//currentObject.GetComponent<BoxCollider>().enabled = false;
			clickedObject = currentObject.transform.parent.gameObject;
			StaticObject so = clickedObject.GetComponent<StaticObjectScript>().StaticObject;
			draggedFromTile = MapCreator.GetTile(so.MapPosition.x, so.MapPosition.y, UIManager.CurrentLevel);
			dragStart = MapCreator.GetWorldPosition(Input.mousePosition, true);
			dragStartScreen = Input.mousePosition;

			shadowObject = new GameObject("Shadow of " + clickedObject.name);
			SpriteRenderer sr = shadowObject.AddComponent<SpriteRenderer>();
			SpriteRenderer origSr = clickedObject.GetComponent<SpriteRenderer>();
			sr.sprite = MapCreator.GetObjectSpriteFromID(so);
			sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 0.9f);
			sr.material = MapCreator.SpriteMaterial;
			sr.sortingOrder = 1;

			step = MapCreator.TileSize * 2.0f / so.GetMovementStep();
			offset = so.GetMovementOffset();
		}
		
	}


	private void ObjectModeClickUp()
	{
		if (clickedObject && isDragging)
		{
			Vector3 mousePos = MapCreator.GetWorldPosition(Input.mousePosition, false);
			clickedObject.transform.position = new Vector3(mousePos.x, mousePos.y, mousePos.z);
			GameObject hoveredOver = MapCreator.GetObjectUnderMouse();
			MapTile tile = null;
			StaticObject so = clickedObject.GetComponent<StaticObjectScript>().StaticObject;
			Vector2Int oldOffsets = new Vector2Int(so.XPos, so.YPos);
			if (hoveredOver && GetDataType(hoveredOver) == DataType.Tile)
			{
				tile = hoveredOver.transform.parent.GetComponent<MapTileScript>().MapTile;
			}
			else if(hoveredOver && GetDataType(hoveredOver) == DataType.Object)
			{
				StaticObject other = hoveredOver.transform.parent.GetComponent<StaticObjectScript>().StaticObject;
				// Dropping onto a container, a critter, a door or a lever puts the object in it.
				// Not onto traps and triggers: they lie on the floor under other objects, and what
				// they hold is data (a create-object trap's template); use their panel for that.
				if(!MapCreator.IsTrapOrTrigger(other) && other.CanBeInserted(so) && UIManager.Undoable(so.CurrentLevel, "Put " + so.Name + " in " + other.Name, () => ObjectList.PutIn(so, other)))
				{
					if (shadowObject)
						Destroy(shadowObject);
					return;
				}
				else
					tile = other.Tile;	//Needed????
			}
			else
			{
				Vector3 basePos = MapCreator.GetWorldPosition(Input.mousePosition, true, true);
				tile = MapCreator.GetTile(basePos, UIManager.CurrentLevel);
			}
			if (!tile)
			{
				// dropped off the map: the object stays where it was
				MapCreator.SetGOPosition(so);
				clickedObject.GetComponentInChildren<BoxCollider>().enabled = true;
				if (shadowObject)
					Destroy(shadowObject);
				return;
			}
			//Debug.LogFormat("New Tile : {0}", tile.Position);
			Vector2Int newOffsets = new Vector2Int(-1, -1);
			float tileSize = MapCreator.TileSize * 2.0f;
			//Debug.LogFormat("X : {0}, X%T : {1}, X%T/s : {2}", shadowObject.transform.position.x, shadowObject.transform.position.x % tileSize, (shadowObject.transform.position.x % tileSize) / step);
			//Debug.LogFormat("Y : {0}, Y%T : {1}, Y%T/s : {2}", shadowObject.transform.position.y, shadowObject.transform.position.y % tileSize, (shadowObject.transform.position.y % tileSize) / step);
			//Debug.LogFormat("shadowObject.transform.position.x : {0}, tilePosition.x + 1 : {1}", shadowObject.transform.position.x / tileSize, tile.Position.x + 1);
			//Debug.LogFormat("shadowObject.transform.position.y : {0}, tilePosition.y + 1 : {1}", shadowObject.transform.position.y / tileSize, tile.Position.y + 1);

			// where the shadow showed it, or the mouse if there is no shadow
			Vector3 drop = shadowObject ? shadowObject.transform.position : mousePos;
			newOffsets.x = Mathf.Clamp(Convert.ToInt32((drop.x - tile.Position.x * tileSize) / step), 0, 7);
			newOffsets.y = Mathf.Clamp(Convert.ToInt32((drop.y - tile.Position.y * tileSize) / step), 0, 7);
			//Debug.LogFormat("newOffsets : {0}", newOffsets);
			MapTile from = draggedFromTile;
			UIManager.Undoable(so.CurrentLevel, "Move " + so.Name, () => ObjectList.Move(so, from, tile, newOffsets));
			
			
			//MapCreator.SetNewPosition(so, draggedFromTile, tile, MapCreator.GetOffsetsFromWorldPos(mousePos));
			clickedObject.GetComponentInChildren<BoxCollider>().enabled = true;
		}
		if (shadowObject)
			Destroy(shadowObject);
	}


	private void SelectTile()
	{
		//Debug.Log("Select tile");
		UIManager.SetTileProperties(null, false);
		DeselectObject();
		if(clickedObject)
		{
			SelectedObject = clickedObject;
			UIManager.SetTileProperties(SelectedObject, true);
		}
	}

	public void SelectObject()
	{
		UIManager.SetObjectProperties(null, false);
		DeselectObject();
		if(triggerLink)
		{
			UIManager.CreateLink(clickedObject);
			return;
		}
		if (clickedObject && !isDragging)
		{
			SelectedObject = clickedObject;
			UIManager.SetObjectProperties(SelectedObject, true);
			//Debug.Log("Selected object : " + clickedObject);
		}
	}
	public void DeselectObject()
	{
		if (SelectedObject)
		{
			SpriteOutline outline = SelectedObject.GetComponent<SpriteOutline>();
			if(outline)
				outline.OutlineObject.SetActive(false);
			MapTileScript mts = SelectedObject.GetComponent<MapTileScript>();
			if (mts)
			{
				MeshRenderer floor = mts.FloorObject.GetComponent<MeshRenderer>();
				MeshRenderer wall = null;
				if (mts.WallObject)
					wall = mts.WallObject.GetComponent<MeshRenderer>();

				if (floor)
					floor.material.SetFloat("_OutlineSize", 0);
				if (wall)
					wall.material.SetFloat("_OutlineSize", 0);
			}
		}
		SelectedObject = null;
		if(currentObject)
		{
			SetObject(currentObject, false);
			currentObject = null;
		}
	}

	public static DataType GetDataType(GameObject obj)
	{
		//if(obj)
		//	Debug.LogFormat("Get obj : {0}", obj.name);
		if (!obj)
			return DataType.Null;
		if (obj.name.Contains("Floor"))
			return DataType.Tile;
		else if (obj.transform.parent && obj.transform.parent.GetComponent<SpriteOutline>())
			return DataType.Object;

		//Debug.LogError("Invalid object type : " + obj);
		return DataType.Invalid;
	}

	private void SetObject(GameObject obj, bool activate)
	{
		DataType type = GetDataType(obj);
		if (type == DataType.Null || type == DataType.Invalid)
			return;
		else if (type == DataType.Tile)
		{
			UIManager.SetTilePanel(obj, activate, SelectedObject);
		}
		else if(type == DataType.Object)
		{
			UIManager.SetStaticObjectPanel(obj, activate, SelectedObject);
		}
	}

	private void createTriggerLink(LineRenderer lr)
	{
		triggerLink = lr;
	}

	public void StartTriggerLink(LineRenderer lr)
	{
		triggerLink = lr;
	}

	public Command PopLastCommand()
	{
		return Commands.Pop();
	}
}

//public void DeselectTile()
//{
//	if(SelectedObject)
//	{
//		MapTileScript mts = SelectedObject.GetComponent<MapTileScript>();
//		if (mts)
//		{
//			MeshRenderer floor = mts.FloorObject.GetComponent<MeshRenderer>();
//			MeshRenderer wall = null;
//			if (mts.WallObject)
//				wall = mts.WallObject.GetComponent<MeshRenderer>();

//			if (floor)
//				floor.material.SetFloat("_OutlineSize", 0);
//			if (wall)
//				wall.material.SetFloat("_OutlineSize", 0);
//		}
//	}
//	SelectedObject = null;
//	if(currentObject)
//	{
//		SetObject(currentObject, false);
//		currentObject = null;
//	}
//}
