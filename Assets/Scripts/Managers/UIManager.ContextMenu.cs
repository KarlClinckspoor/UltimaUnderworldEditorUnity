using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Linq;

/// <summary>UIManager: context menu (split from UIManager.cs by region, review package 10).</summary>
public partial class UIManager
{
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
			Undoable(so.CurrentLevel, "Delete " + so.Name, () => ObjectList.Delete(so));
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
		Action pickTrigger = () =>
		{
			// checked before the trigger is created, so a refusal leaves nothing behind
			if (so.IsQuantity && so.Special != 0)
			{
				SpawnPopupMessage(so.Name + " is a stack or has a special value (" + so.Special + "): its link field holds that, so it can't hold a trigger.");
				return;
			}
			CreateSpecifiedItemList((id) => CreateObject(id, CurrentLevel, new Vector2Int(so.XPos, so.YPos), so.Tile, addTrigger), StaticObject.GetTriggers(), "Trigger list", "");
		};
		createButton("Add trigger", pickTrigger, getContextMenuContent(), contextMenu, 2);
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
			Action copyObjectAct = () =>
			{
				var pasted = MapCreator.AddObject(tile, spawnPosition, CurrentLevel, copyObject.ObjectID, copyObject);
				if (pasted) GiveOwnText(copyObject, pasted);
			};
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
		StaticObject oldTarget = start.IsQuantity && start.Special > 0 ? MapCreator.LevelData[start.CurrentLevel - 1].Objects[start.Special] : null;
		start.Special = end.CurrentAdress;
		if (start.IsTrigger() || start.ObjectID == 395)	//Delete object trap
		{
			start.Quality = end.Tile.Position.x;
			start.Owner = end.Tile.Position.y;
		}
		start.IsQuantity = true;
		// A trap's flags count the triggers pointing at it (event/TRIGGER.C delete_trap; 4 bits,
		// the enchanted bit being the 8s).
		if (start.IsTrigger())
		{
			if (end.IsTrap())
				end.SetTrapTriggerCount(MapCreator.CountTriggersTo(end));
			if (oldTarget != null && oldTarget != end && oldTarget.IsTrap())
				oldTarget.SetTrapTriggerCount(MapCreator.CountTriggersTo(oldTarget));
		}
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
}
