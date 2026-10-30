using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Linq;

/// <summary>UIManager: objects (split from UIManager.cs by region, review package 10).</summary>
public partial class UIManager
{
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
			listObject.AddComponent<UltimateEditor.Uw1.UI.ObjectListEntry>().Object = so;
			totalCount++;
		}
		UltimateEditor.Uw1.UI.ObjectListFilter.Attach(list, true);
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
				Undoable(toRemove.CurrentLevel, "Take " + toRemove.Name + " out of " + container.Name, () => ObjectList.TakeOut(toRemove));
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
		if (!so)
			return;
		Undoable(so.CurrentLevel, "Delete " + so.Name, () => ObjectList.Delete(so));
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

	/// <summary>The range of each field the advanced panel edits: the width it has in the level
	/// file (uw1/src/include/object.h), so a value can't spill into the next field when saved.</summary>
	static bool FieldRange(string field, bool mobile, out int min, out int max)
	{
		return ObjectRecord.Range(field, mobile, out min, out max);
	}

	public void ChangeObjectProperty(InputField input)
	{
		StaticObject so = ObjectProperties.GetComponent<PropertiesPanel>().SelectedObject;
		if (!so)
			return;
		string field = input.gameObject.name;
		int value = 0;
		if (!int.TryParse(input.text, out value))
		{
			SpawnPopupMessage("Invalid value at " + field);
			return;
		}
		int min, max;
		if (FieldRange(field, so is MobileObject, out min, out max) && (value < min || value > max))
		{
			value = Mathf.Clamp(value, min, max);
			SpawnPopupMessage(field + " goes from " + min + " to " + max + "; set to " + value + ".");
		}
		input.text = value.ToString();

		if (field == "XPos")
		{
			so.XPos = value;
			MapCreator.SetGOPosition(so);
		}
		else if (field == "YPos")
		{
			so.YPos = value;
			MapCreator.SetGOPosition(so);
		}
		else if (field == "Height")
		{
			so.ZPos = value;
			MapCreator.SetGOHeight(so);
		}
		else if (field == "Direction")
		{
			so.Direction = value;
			MapCreator.SetGODirection(so);
		}
		else if (field == "Flags")
			so.Flags = value;
		else if (field == "Quality")
			so.Quality = value;
		else if (field == "OwnerSpecial")
			so.Owner = value;
		else if (field == "QuantityLink")
			so.Special = value;

		if (so is MobileObject)
		{
			MobileObject mo = (MobileObject)so;
			if (field == "Attitude")
				mo.Attitude = value;
			else if (field == "Goal")
				mo.Goal = value;
			else if (field == "HomeX")
				mo.XHome = value;
			else if (field == "HomeY")
				mo.YHome = value;
			else if (field == "HP")
				mo.HP = value;
			else if (field == "NPC")
				mo.Whoami = value;
		}
	}

	#endregion
}
