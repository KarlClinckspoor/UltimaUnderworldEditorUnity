using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Linq;

/// <summary>UIManager: strings (split from UIManager.cs by region, review package 10).</summary>
public partial class UIManager
{
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
		// Key names with buttons that find each key's keys and locks (Extensions/UI/KeyEditorWindow.cs)
		listMenu = UltimateEditor.Uw1.UI.KeyEditorWindow.Show(this);
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
}
