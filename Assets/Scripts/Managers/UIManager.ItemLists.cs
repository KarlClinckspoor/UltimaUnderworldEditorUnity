using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Linq;

/// <summary>UIManager: item lists (split from UIManager.cs by region, review package 10).</summary>
public partial class UIManager
{
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
				// Lists with the category filter combine it with the name search.
				var filter = list.GetComponent<UltimateEditor.Uw1.UI.ObjectListFilter>();
				if (filter)
				{
					filter.SetSearch(str);
					return;
				}
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
		UltimateEditor.Uw1.UI.ObjectListFilter.Attach(list, false);
		addSearch(list);

		int listed = LevArk.Game.Kind == UltimateEditor.Games.GameKind.UW2 ? StaticObject.ListedIdLimit : 459;
		for (int i = 0; i < listed; i++)
		{
			int new_i = i;
			GameObject itemGO = createItemButton(new_i, list.Content.transform, getItemButtonTooltip(new_i));
			itemGO.AddComponent<UltimateEditor.Uw1.UI.ObjectListEntry>().Item = new_i;
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
}
