using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Linq;

/// <summary>UIManager: conversations (split from UIManager.cs by region, review package 10).</summary>
public partial class UIManager
{
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
}
