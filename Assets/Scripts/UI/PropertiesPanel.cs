using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PropertiesPanel : MonoBehaviour {

	public StaticObject SelectedObject { get; private set; }

	public GameObject BasicPanel;
	public Text BasicInfo;

	public GameObject InputPropertyPrefab;
	public GameObject DropdownPropertyPrefab;
	public GameObject ImageButtonPropertyPrefab;
	public GameObject LargeInputPropertyPrefab;
	public GameObject TogglePropertyPrefab;
	public GameObject SliderPropertyPrefab;
	public GameObject ButtonPropertyPrefab;
	public GameObject InventoryPropertyPrefab;
	public GameObject InputButtonPropertyPrefab;
	public GameObject InputButton2PropertyPrefab;
	public GameObject DropdownButtonPropertyPrefab;
	public GameObject DropdownButton2PropertyPrefab;

	public Text Adresses;
	public Dropdown Types;
	public InputField XPos;
	public InputField YPos;
	public InputField Height;
	public Toggle Enchantable;
	public Toggle Invisible;
	public Toggle IsQuantity;
	public Toggle Door;
	public InputField Direction;
	public InputField Flags;
	public InputField Quality;
	public InputField Owner;
	public InputField Special;

	public Dropdown Inventory;
	public Button RemoveFromContainer;

	public InputField HomeX;
	public InputField HomeY;
	public InputField Attitude;
	public InputField Goal;
	public InputField HP;
	public InputField NPCID;

	public GameObject HomeXGO;
	public GameObject HomeYGO;
	public GameObject AttitudeGO;
	public GameObject GoalGO;
	public GameObject HPGO;
	public GameObject NPCIDGO;

	public GameObject HomeText;
	public GameObject AttitudeText;
	public GameObject GoalText;
	public GameObject HPText;
	public GameObject NPCIDText;

	public Dictionary<int, StaticObject> InventoryLinks = new Dictionary<int, StaticObject>();
	public static Dictionary<Dropdown.OptionData, int> TypeLinks = new Dictionary<Dropdown.OptionData, int>();
	public Dictionary<int, int> ObjectIDToDropdownIndex = new Dictionary<int, int>();

	public List<Dropdown.OptionData> StaticTypes = new List<Dropdown.OptionData>();
	public List<Dropdown.OptionData> MobileTypes = new List<Dropdown.OptionData>();

	private UIManager uiManager;

	private void Awake()
	{
		//Types.AddOptions(StaticObject.GetTypes());
		List<int> statics = StaticObject.GetStaticTypes();
		for (int i = 0; i < statics.Count; i++)
		{
			int staticID = statics[i];
			Dropdown.OptionData option = new Dropdown.OptionData(StaticObject.GetName(staticID));
			StaticTypes.Add(option);
			TypeLinks[option] = staticID;
			ObjectIDToDropdownIndex[staticID] = i;
		}
		List<int> mobiles = StaticObject.GetMobileTypes();
		for (int i = 0; i < mobiles.Count; i++)
		{
			int mobileID = mobiles[i];
			Dropdown.OptionData option = new Dropdown.OptionData(StaticObject.GetName(mobileID));
			MobileTypes.Add(option);
			TypeLinks[option] = mobileID;
			ObjectIDToDropdownIndex[mobileID] = i;
		}
	}

	private void Start()
	{
		uiManager = GameObject.FindGameObjectWithTag("UIManager").GetComponent<UIManager>();
	}

	private void clearBasicPanel()
	{
		BasicInfo.transform.parent.SetAsFirstSibling();
		for (int i = 1; i < BasicPanel.transform.childCount; i++)
			Destroy(BasicPanel.transform.GetChild(i).gameObject);
	}

	public void SetBasicPanel(StaticObject so)
	{
		if (so == null)
		{
			Debug.LogError("Properties Panel : Tried to set object to null (SetBasicPanel)");
			return;
		}
		clearBasicPanel();
		BasicInfo.text = so.Name + " [" + so.CurrentAdress + "]";
		List<StaticObject> contained = so.GetContainedObjects();
		if (so.IsContainer())
		{
			createInventoryProperty("Inventory", BasicPanel.transform, so);
		}
		StaticObject container = so.GetContainer();
		if (container)
		{
			createRemoveFromContainerButton(so, container);
			createSelectContainerButton(so, container);
		}
		if(so.IsModel())
		{
			createHeightSlider(so);
			createDirectionDropdown(so);
			StaticObject lockObj = so.GetLock();
			if (so.ObjectID == 349)	//Chest
			{
				if (lockObj)
					createLinkProperty(so, lockObj, "Door lock", "Select", "Destroy", (next) => destroyContainedObject(so, next));
				else
					createAddNextInvProperty(so, StaticObject.GetIDsByType(ObjectType.Lock), "", "Add lock");
			}
		}

		if (so.IsMonster())
		{
			MobileObject mo = (MobileObject)so;
			createHeightSlider(mo);
			createDirectionDropdown(mo);
			string[] atts = MobileObject.GetAttitudes();
			createDropdownProperty("Attitude", BasicPanel.transform, atts[mo.Attitude], atts, (i) => changeMobileAttitude(mo, i));
			string[] goals = MobileObject.GetGoals();
			createDropdownProperty("Goal", BasicPanel.transform, goals[mo.Goal], goals, (i) => changeMobileGoal(mo, i));
			createInputProperty("HP", BasicPanel.transform, mo.HP, (str) => changeMobileHP(mo, int.Parse(str)), 1, 512);
			Dictionary<string, int> npcIds = MapCreator.GetNPCIDs();
			GameObject npcTogGO = createToggleProperty("Is NPC", BasicPanel.transform, mo.Whoami > 0);
			Toggle npcTog = npcTogGO.GetComponentInChildren<Toggle>();
			GameObject npcDropGO = createDropdownProperty("NPC name", BasicPanel.transform, mo.GetNPCName(), npcIds);
			Dropdown npcDrop = npcDropGO.GetComponentInChildren<Dropdown>();
			if (mo.Whoami == 0)
				npcDropGO.SetActive(false);
			Action<bool> npcTogAct = (b) =>
			{
				if (b)
				{
					npcDropGO.SetActive(true);
				}
				else
				{
					npcDropGO.SetActive(false);
					changeMobileNPCID(mo, 0);
				}
			};
			npcTog.onValueChanged.AddListener((b) => npcTogAct(b));
			npcDrop.onValueChanged.AddListener((i) => changeMobileNPCID(mo, npcIds[npcDrop.options[npcDrop.value].text]));
		}
		else if (so.IsFood() || so.IsLight())
		{
			createQualitySlider(so, so.Quality, 0, 63.0f);
			createOwnerDropdown(so);
			GameObject quantityInputGO = createQuantityInput(so);
			//createQuantityToggle(so, quantityInputGO);
		}
		else if (so.IsCeilingHugger())
		{
			createHeightSlider(so);
		}
		else if (so.IsRune())
		{
			createOwnerDropdown(so);
		}
		else if (so.IsCountable() || so.IsProjectile() || so.IsGold())
		{
			createOwnerDropdown(so);
			GameObject quantityInputGO = createQuantityInput(so);
			//createQuantityToggle(so, quantityInputGO);
		}
		else if (so.IsUncountable())
		{
			createOwnerDropdown(so);
			GameObject quantityInputGO = createQuantityInput(so);
			//createQuantityToggle(so, quantityInputGO);
		}
		else if (so.ObjectID == 302)	//Fountain
		{
			Toggle isEnchanted = createToggleProperty("Enchanted", BasicPanel.transform, so.IsEnchanted).GetComponentInChildren<Toggle>();
			isEnchanted.interactable = false;
			if (so.IsEnchanted)   //Standard enchantment
				createEnchantmentProperty(so, StaticObject.GetFountainEnchants(), false, "Healing");
		}
		else if (so.IsDrink())
		{
			createOwnerDropdown(so);
			if (so.IsEnchanted)
			{
				Toggle isEnchanted = createToggleProperty("Enchanted", BasicPanel.transform, so.IsEnchanted).GetComponentInChildren<Toggle>();
				isEnchanted.interactable = false;
				if (so.IsQuantity)   //Standard enchantment
					createEnchantmentProperty(so, StaticObject.GetPotionScrollEnchants(), false, "Effect");
				else
				{
					StaticObject trap = so.GetContainedObject();
					createLinkProperty(so, trap, "Potion trap", "Select", "Destroy", (x) => destroyContainedObject(so, x));
				}
			}
			else
			{
				GameObject quantityInputGO = createQuantityInput(so);
				//createQuantityToggle(so, quantityInputGO);
			}
		}
		else if (so.IsBones())
		{
			createMonsterDropdown(so);
			GameObject quantityInputGO = createQuantityInput(so);
			//createQuantityToggle(so, quantityInputGO);
		}
		else if (so.IsKey())
		{
			createSliderProperty("Key ID", BasicPanel.transform, so.Owner, 1.0f, 63.0f, (f) => changeObjectOwner(so, (int)f));
		}
		else if (so.IsWeapon())
		{
			createOwnerDropdown(so);
			createQualitySlider(so, so.Quality, 1.0f, 63.0f);
			createIdentifiedToggle(so);
			createEnchantmentProperties(so, StaticObject.GetWeaponEnchants());
		}
		else if (so.IsArmour() || so.IsBauble() || so.IsShield())
		{
			createOwnerDropdown(so);
			createQualitySlider(so, so.Quality, 1.0f, 63.0f);
			createIdentifiedToggle(so);
			createEnchantmentProperties(so, StaticObject.GetWearableEnchants());
		}
		else if (so.IsTreasure())
		{
			createOwnerDropdown(so);
			createQualitySlider(so, so.Quality, 1.0f, 63.0f);
			StaticObject spell = so.GetContainedObject();
			if (spell && spell.ObjectID == 288)	//Is magical treasure
				createEnchantmentProperty(spell, StaticObject.GetPotionScrollEnchants(), true, "Spell");
			else//Normal - just add quantity
			{
				GameObject quantGO = createQuantityInput(so);
				//createQuantityToggle(so, quantGO);
			}
		}
		else if (so.IsWand())
		{
			createOwnerDropdown(so);
			createIdentifiedToggle(so);
			StaticObject spell = so.GetContainedObject();
			if (spell && spell.ObjectID == 288)
				createEnchantmentProperty(spell, StaticObject.GetPotionScrollEnchants(), true, "Spell");
		}
		else if (so.IsPotion())
		{
			createOwnerDropdown(so);
			createIdentifiedToggle(so);
			Toggle isEnchanted = createToggleProperty("Enchanted", BasicPanel.transform, so.IsEnchanted).GetComponentInChildren<Toggle>();
			isEnchanted.interactable = false;
			if (so.IsQuantity)   //Standard enchantment
				createEnchantmentProperty(so, StaticObject.GetPotionScrollEnchants(), false, "Effect");
			else
			{
				StaticObject trap = so.GetContainedObject();
				createLinkProperty(so, trap, "Potion trap", "Select", "Destroy", (x) => destroyContainedObject(so, x));
			}
		}
		else if (so.IsScroll())
		{
			createOwnerDropdown(so);
			Toggle isEnchanted = createToggleProperty("Enchanted", BasicPanel.transform, so.IsEnchanted).GetComponentInChildren<Toggle>();
			isEnchanted.interactable = false;
			if(so.IsEnchanted)
			{
				createIdentifiedToggle(so);
				if (so.IsQuantity)  //Standard enchantment
					createEnchantmentProperty(so, StaticObject.GetPotionScrollEnchants(), false, "Spell");
				else
				{
					StaticObject trap = so.GetContainedObject();
					createLinkProperty(so, trap, "Scroll trap", "Select", "Destroy", (x) => destroyContainedObject(so, x));
				}
			}
			else
			{
				createLargeInputProperty("Text", BasicPanel.transform, MapCreator.StringData.GetScroll(so), (str) => MapCreator.StringData.SetScroll(so, str));
			}
		}
		else if (so.IsBook())
		{
			createOwnerDropdown(so);
			createLargeInputProperty("Text", BasicPanel.transform, MapCreator.StringData.GetScroll(so), (str) => MapCreator.StringData.SetScroll(so, str));
		}
		else if (so.IsVerticalTexture())
		{
			createHeightSlider(so);
			createDirectionDropdown(so);
			Sprite curSprite = Sprite.Create(MapCreator.GetWallTextureFromIndex(so.Owner, so.CurrentLevel), new Rect(0, 0, 64.0f, 64.0f), Vector2.zero);
			Button texButton = createImageButtonProperty("Texture", BasicPanel.transform, curSprite);
			Action<int> setTexAct = (i) =>
			{
				so.Owner = i;
				Owner.text = so.Owner.ToString();
				texButton.GetComponent<Image>().sprite = Sprite.Create(MapCreator.GetWallTextureFromIndex(so.Owner, so.CurrentLevel), new Rect(0, 0, 64.0f, 64.0f), Vector2.zero);
				MapCreator.SetGOSprite(so);
				Destroy(uiManager.TexturePicker);
			};
			texButton.onClick.AddListener(() => uiManager.CreateTexturePicker(uiManager.GetCurrentLevelTextures(TextureType.Wall, false), setTexAct, "Texture", "Select wall texture"));
		}
		else if(so.IsWriting())
		{
			createHeightSlider(so);
			createDirectionDropdown(so);
			Sprite curSprite = Sprite.Create(MapCreator.TextureData.Other.Textures[so.Flags + 20], new Rect(0, 0, 16.0f, 16.0f), Vector2.zero);
			Button texButton = createImageButtonProperty("Texture", BasicPanel.transform, curSprite);
			Action<int> selTexAct = (i) =>
			{
				so.Flags = i;
				Flags.text = so.Flags.ToString();
				texButton.GetComponent<Image>().sprite = Sprite.Create(MapCreator.TextureData.Other.Textures[so.Flags + 20], new Rect(0, 0, 16.0f, 16.0f), Vector2.zero);
				MapCreator.SetGOSprite(so);
				Destroy(uiManager.TexturePicker);
			};
			texButton.onClick.AddListener(() => uiManager.CreateTexturePicker(uiManager.GetWritingTextures(), selTexAct, "Textures", "Select writing texture"));
			createLargeInputProperty("Text", BasicPanel.transform, MapCreator.StringData.GetWriting(so), (str) => MapCreator.StringData.SetWriting(so, str));
		}
		else if(so.IsGrave())
		{
			createDirectionDropdown(so);
			Sprite curSprite = Sprite.Create(MapCreator.TextureData.Other.Textures[so.Flags + 28], new Rect(0, 0, 16.0f, 32.0f), Vector2.zero);
			Button texButton = createImageButtonProperty("Texture", BasicPanel.transform, curSprite);
			Action<int> selTexAct = (i) =>
			{
				so.Flags = i;
				Flags.text = so.Flags.ToString();
				texButton.GetComponent<Image>().sprite = Sprite.Create(MapCreator.TextureData.Other.Textures[so.Flags + 28], new Rect(0, 0, 16.0f, 32.0f), Vector2.zero);
				Destroy(uiManager.TexturePicker);
			};
			texButton.onClick.AddListener(() => uiManager.CreateTexturePicker(uiManager.GetWritingTextures(), selTexAct, "Textures", "Select grave texture"));
			createLargeInputProperty("Description", BasicPanel.transform, MapCreator.StringData.GetGrave(so), (str) => MapCreator.StringData.SetGrave(so, str));
		}
		else if(so.ObjectID == 356)	//Bridge
		{
			//This as a model will have also height and direction
			Sprite curSprite = Sprite.Create(uiManager.GetBridgeTextures(so.CurrentLevel)[so.Flags], new Rect(0, 0, 32.0f, 32.0f), Vector2.zero);
			Button texButton = createImageButtonProperty("Texture", BasicPanel.transform, curSprite);
			Action<int> selTexAct = (i) =>
			{
				so.Flags = i;
				Flags.text = so.Flags.ToString();
				texButton.GetComponent<Image>().sprite = Sprite.Create(uiManager.GetBridgeTextures(so.CurrentLevel)[so.Flags], new Rect(0, 0, 32.0f, 32.0f), Vector2.zero);
				Destroy(uiManager.TexturePicker);
			};
			texButton.onClick.AddListener(() => uiManager.CreateTexturePicker(uiManager.GetBridgeTextures(so.CurrentLevel), selTexAct, "Textures", "Select bridge texture"));
		}
		else if(so.ObjectID == 352)	//Pillar
		{
			//This as a model will have also height and direction
			List<Texture2D> pillartex = uiManager.GetPillarTextures();
			Sprite curSprite = Sprite.Create(pillartex[so.Flags], new Rect(0, 0, 8.0f, 32.0f), Vector2.zero);
			Button texButton = createImageButtonProperty("Texture", BasicPanel.transform, curSprite);
			Action<int> selTexAct = (i) =>
			{
				so.Flags = i;
				Flags.text = so.Flags.ToString();
				texButton.GetComponent<Image>().sprite = Sprite.Create(pillartex[so.Flags], new Rect(0, 0, 8.0f, 32.0f), Vector2.zero);
				Destroy(uiManager.TexturePicker);
			};
			texButton.onClick.AddListener(() => uiManager.CreateTexturePicker(pillartex, selTexAct, "Textures", "Select pillar texture"));
		}
		else if(so.IsDoor())
		{
			bool isDoorOpen = so.ObjectID >= 328 ? true : false;
			Sprite curSprite = Sprite.Create(so.GetDoorTexture(), new Rect(0, 0, 32.0f, 64.0f), new Vector2(0, 0));
			Button selectTexButton = createImageButtonProperty("Door type", BasicPanel.transform, curSprite);
			createToggleProperty("Indestructible", BasicPanel.transform, so.IsDoorOpen, (b) => changeObjectIsDoorOpen(so, b));
			Action<int> selDoorTex = (i) =>
			{
				so.SetID((isDoorOpen ? 328 : 320) + i);
				Types.value = ObjectIDToDropdownIndex[so.ObjectID];
				selectTexButton.GetComponent<Image>().sprite = Sprite.Create(so.GetDoorTexture(), new Rect(0, 0, 32.0f, 64.0f), new Vector2(0, 0));
				MapCreator.UpdateGODoorTexture(so, so.CurrentLevel);
				Destroy(uiManager.TexturePicker);
			};
			Action createDoorTexPicker = () =>
			{
				List<Texture2D> doorTexs = uiManager.GetCurrentLevelTextures(TextureType.Door, false);
				doorTexs.Add(Resources.Load<Texture2D>("port"));
				doorTexs.Add(Resources.Load<Texture2D>("secr"));
				uiManager.CreateTexturePicker(doorTexs, selDoorTex, "Textures", "Select door texture");
			};
			selectTexButton.onClick.AddListener(() => createDoorTexPicker());
			createQualitySlider(so, so.Quality, 0, 63.0f);
			StaticObject lockObj = so.GetLock();
			if (lockObj)
				createLinkProperty(so, lockObj, "Door lock", "Select", "Destroy", (next) => destroyContainedObject(so, next));
			else
				createAddNextInvProperty(so, StaticObject.GetIDsByType(ObjectType.Lock), "", "Add lock");
			List<StaticObject> doorTraps = so.GetDoorTraps();
			if (doorTraps.Count > 0)
				createMultipleLinkProperty(so, doorTraps, "Door traps", "Destroy", (next) => destroyContainedObject(so, next));
			createAddNextInvProperty(so, new List<int>(new int[] { 392 }), "", "Add door trap");

		}

		else if(so.IsLock())
		{
			createSliderProperty("Lock difficulty", BasicPanel.transform, so.ZPos, 1.0f, 30.0f, (f) => { so.ZPos = (int)f; Height.text = so.ZPos.ToString(); });
			bool isKey = so.Special != 512;
			GameObject keyIDGO = createSliderProperty("Key ID", BasicPanel.transform, so.Special - 512, 1.0f, 63.0f, (f) => changeObjectSpecial(so, (int)(f + 512.0f)));
			keyIDGO.SetActive(isKey);
			Action<bool> isKeyAct = (b) =>
			{
				if (!b)
				{
					so.Special = 512;
					keyIDGO.SetActive(false);
				}
				else
				{
					keyIDGO.SetActive(true);
					string s = keyIDGO.GetComponentInChildren<InputField>().text;
					so.Special = string.IsNullOrEmpty(s) ? 512 : int.Parse(s) + 512;
				}
				Special.text = so.Special.ToString();
			};
			createToggleProperty("Opened by key", BasicPanel.transform, isKey, isKeyAct);
		}
		else if(so.IsDoorTrap())
		{
			string[] doorTrapTypes = new string[] { "Open", "Close", "Toggle" };   //quality 1, 2, 3 (TRIGGER.C TRAP_DOOR)
			Action<int> changeDoorTrapTypeAct = (i) =>
			{
				so.Quality = i + 1;
				Quality.text = so.Quality.ToString();
			};
			createDropdownProperty("Action type", BasicPanel.transform, doorTrapTypes[Mathf.Clamp(so.Quality - 1, 0, 2)], doorTrapTypes, changeDoorTrapTypeAct);
			StaticObject lockObj = so.GetLock();
			if (lockObj)
				createLinkProperty(so, lockObj, "Door lock", "Select", "Destroy", (next) => destroyContainedObject(so, next));
			else
			{
				createAddNextInvProperty(so, StaticObject.GetIDsByType(ObjectType.Lock), "", "Add lock");
				if(container)
				{
					StaticObject doorLock = container.GetLock();
					if (doorLock)
					{
						Action copyLock = () =>
						{
							StaticObject copy = MapCreator.AddObject(so.Tile, new Vector2Int(3, 3), so.CurrentLevel, 271, null, true);
							copy.SetFullProperties(doorLock);
							so.Special = copy.CurrentAdress;
							copy.PrevAdress = so.CurrentAdress;
							uiManager.SelectObject(copy, false);
						};
						createButtonProperty("Copy lock from door", BasicPanel.transform, copyLock);
					}
				}
			}
		}
		else if(so.IsLever())
		{
			createHeightSlider(so);
			createDirectionDropdown(so);
			List<Texture2D> leverTexs = uiManager.GetTextures(TextureType.Lever, false);
			Sprite curSprite = Sprite.Create(leverTexs[so.ObjectID - 368], new Rect(0, 0, 16.0f, 16.0f), Vector2.zero);
			Button texButton = createImageButtonProperty("Type", BasicPanel.transform, curSprite);
			Action<int> selTexAct = (i) =>
			{
				so.SetID(368 + i);
				Types.value = ObjectIDToDropdownIndex[so.ObjectID];
				texButton.GetComponent<Image>().sprite = Sprite.Create(leverTexs[so.ObjectID - 368], new Rect(0, 0, 16.0f, 16.0f), Vector2.zero);
				MapCreator.SetGOSprite(so);
				Destroy(uiManager.TexturePicker);
			};
			texButton.onClick.AddListener(() => uiManager.CreateTexturePicker(leverTexs, selTexAct, "Textures", "Select lever texture"));
			if (so.Special > 0)
			{
				StaticObject use = so.GetContainedObject();
				if(use.ObjectID == 418)
					createLinkingProperties(use);
			}
			else
				createAddUseTriggerProperty(so);
		}
		else if(so.IsDialLever())
		{
			createHeightSlider(so);
			createDirectionDropdown(so);
			Action<float> changePos = (f) =>
			{
				changeObjectFlags(so, (int)f);
				MapCreator.SetGOSprite(so);
			};
			createSliderProperty("Position", BasicPanel.transform, so.Flags, 0, 7.0f, changePos);
			if (so.Special > 0)
			{
				StaticObject use = so.GetContainedObject();
				if (use.ObjectID == 418)
					createLinkingProperties(use);
			}
			else
				createAddUseTriggerProperty(so);

		}
		else if (so.IsTrigger() || so.IsTrap() || so.ObjectID == 387)
		{
			// Trap and trigger semantics from underworld-exhumed, uw1/src/event/TRIGGER.C
			// (UseTrigger, UseTrap, do_trap_hack); see Extensions/Core/Uw1Traps.cs.
			createLinkingProperties(so);
			if (so.IsTrigger())
				createTriggerProperties(so);
			else
				createTrapReferenceProperty(so);
			bool uw2 = LevArk.Game.Kind == UltimateEditor.Games.GameKind.UW2;
			if (uw2)
			{
				// UW2 (uw2/src/event/TRIGGER.C): say what the fields do; the UW1 editors below are
				// used only for the traps whose fields mean the same in both games.
				string what = UltimateEditor.Games.Uw2Traps.Describe(so.ObjectID);
				if (what != null)
				{
					var t = UltimateEditor.Uw1.UI.UiKit.Paragraph(BasicPanel.transform, "UW2: " + what + "\nRaw fields: Tools > Object fields.", 11, Color.white);
					t.horizontalOverflow = HorizontalWrapMode.Wrap;
				}
			}
			if (uw2 && so.IsTrap() && !UltimateEditor.Games.Uw2Traps.SharedWithUw1(so.ObjectID))
			{
				// The fields UW2's traps read, with what they come to at their present values
				Text decoded = null;
				Action refresh = () =>
				{
					if (decoded) decoded.text = "Now: " + (Uw2TrapNow(so) ?? "(no decoding for this trap)");
				};
				decoded = UltimateEditor.Uw1.UI.UiKit.Paragraph(BasicPanel.transform, "", 11, new Color(1f, 0.92f, 0.6f));
				decoded.horizontalOverflow = HorizontalWrapMode.Wrap;
				refresh();
				createSliderProperty("Quality", BasicPanel.transform, so.Quality, 0, 63.0f, (f) => { changeObjectQuality(so, (int)f); refresh(); });
				createSliderProperty("Owner", BasicPanel.transform, so.Owner, 0, 63.0f, (f) => { changeObjectOwner(so, (int)f); refresh(); });
				createSliderProperty("Z (height)", BasicPanel.transform, so.ZPos, 0, 127.0f, (f) => { changeObjectZPos(so, (int)f); refresh(); });
				createSliderProperty("Heading", BasicPanel.transform, so.Direction, 0, 7.0f, (f) => { changeObjectDir(so, (int)f); refresh(); });
				createSliderProperty("Fine X", BasicPanel.transform, so.XPos, 0, 7.0f, (f) => { changeObjectX(so, (int)f); refresh(); });
				createSliderProperty("Fine Y", BasicPanel.transform, so.YPos, 0, 7.0f, (f) => { changeObjectY(so, (int)f); refresh(); });
			}
			else if (so.ObjectID == 384)   //Damage trap
			{
				// Quality is the damage; a non-zero owner makes it heal instead (it was shown
				// as a "health/poison" damage type).
				createSliderProperty("Damage (or healing)", BasicPanel.transform, so.Quality, 0, 63.0f, (f) => changeObjectQuality(so, (int)f));
				createToggleProperty("Heals instead of hurting", BasicPanel.transform, so.Owner != 0, (b) => changeObjectOwner(so, b ? 1 : 0));
			}
			else if (so.ObjectID == 385)    //Teleport
			{
				bool teleInThisLvl = so.ZPos == 0 ? true : false;
				// In v0.2, sometimes when this is called, uiManager wasn't initialized yet.
				// For instance, the teleport trap in Lvl3, idx 974.
				// This would lead to a null reference exception, and the UI would become wonky.
				// This fix is hacky, as this should've been called automatically, I think.
				// TODO: Figure out why this is needed.
				if (!uiManager)
					Start();
				GameObject targetLevel = createSliderProperty("Target level", BasicPanel.transform, so.ZPos, 1.0f, uiManager.MapCreator.GetLevelCount(), (f) => changeObjectZPos(so, (int)f));
				if (teleInThisLvl)
					targetLevel.SetActive(false);
				Action<bool> curLevelAct = (b) =>
				{
					if (b)
					{
						targetLevel.SetActive(false);
						changeObjectZPos(so, 0);
					}
					else
						targetLevel.SetActive(true);
				};
				createToggleProperty("Teleport in this level", BasicPanel.transform, teleInThisLvl, curLevelAct);
				createSliderProperty("Target X", BasicPanel.transform, so.Quality, 0, 63.0f, (f) => changeObjectQuality(so, (int)f));
				createSliderProperty("Target Y", BasicPanel.transform, so.Owner, 0, 63.0f, (f) => changeObjectOwner(so, (int)f));
			}
			else if (so.ObjectID == 386)    //Arrow
			{
				createHeightSlider(so);
				Button changeProjectileType = createImageButtonProperty("Projectile", BasicPanel.transform, MapCreator.GetObjectSpriteFromID(so.GetArrowTrapProjectile()));
				Action<int> changeProjectileAct = (i) =>
				{
					so.SetArrowTrapProjectile(i);
					resetAdvancedPanel(so);
					changeProjectileType.GetComponent<Image>().sprite = MapCreator.GetObjectSpriteFromID(i);
				};
				Action spawnItemPicker = () => uiManager.CreateSpecifiedItemList(changeProjectileAct, StaticObject.GetShootable(), "Object list", "Select object to shoot.");
				changeProjectileType.onClick.AddListener(() => spawnItemPicker());
				createDirectionDropdown(so);
			}
			else if (so.ObjectID == 387)    //Do (special) trap
			{
				// do_trap_hack: the quality picks a special case of the game. The old labels
				// (camera, platform, attitude) were UW2's.
				var hacks = UltimateEditor.Uw1.Uw1Traps.HackTraps;
				string[] names = new string[hacks.Length + 1];
				int current = hacks.Length;
				for (int i = 0; i < hacks.Length; i++)
				{
					names[i] = string.Format("0x{0:X2} {1}", hacks[i].Key, hacks[i].Value);
					if (hacks[i].Key == so.Quality) current = i;
				}
				names[hacks.Length] = string.Format("0x{0:X2} (no effect in UW1)", so.Quality);
				createDropdownProperty("Special trap", BasicPanel.transform, names[current], names, (i) =>
				{
					if (i < hacks.Length)
						changeObjectQuality(so, hacks[i].Key);
					SetBasicPanel(so);
				});
				if (so.Quality == 5)
				{
					string[] races = MapCreator.StringData.GetRaces();
					createDropdownProperty("Race wronged (owner)", BasicPanel.transform, races[Mathf.Clamp(so.Owner, 0, races.Length - 1)], races, (i) => changeObjectOwner(so, i));
				}
				else if (so.Quality >= 0x3C && so.Quality <= 0x3E)
					createSliderProperty("Earthquake strength (owner)", BasicPanel.transform, so.Owner, 0, 63.0f, (f) => changeObjectOwner(so, (int)f));
				else if (so.Quality == 0x3F)
					createSliderProperty("End game mode - 1 (owner)", BasicPanel.transform, so.Owner, 0, 63.0f, (f) => changeObjectOwner(so, (int)f));
				else
					createSliderProperty("Parameter (owner)", BasicPanel.transform, so.Owner, 0, 63.0f, (f) => changeObjectOwner(so, (int)f));
			}
			else if (so.ObjectID == 388 || so.ObjectID == 399)	//Pit, combination
			{
				createButtonProperty("No effect in UW1 (UseTrap has no case for it); it only passes on to its link.", BasicPanel.transform);
			}
			else if (so.ObjectID == 389)	//Terrain trap
			{
				createSliderProperty("Size X", BasicPanel.transform, so.XPos, 0, 7.0f, (f) => changeObjectX(so, (int)f));
				createSliderProperty("Size Y", BasicPanel.transform, so.YPos, 0, 7.0f, (f) => changeObjectY(so, (int)f));
				// event/WORLDEV.C change_terrain: the new height is Z >> 3 (0-13); above 13 keeps
				// the tile's height. It was shown as Z / 16, which only reached even heights.
				bool useTriggerHeight = (so.ZPos >> 3) > 13;
				GameObject heightSlider = createSliderProperty("New floor height", BasicPanel.transform, Math.Min(13, so.ZPos >> 3), 0, 13.0f, (f) => changeObjectZPos(so, (int)f << 3));
				if (useTriggerHeight)
				{
					//Debug.Log($"{heightSlider.transform.parent.gameObject.name}");
					heightSlider.SetActive(false);
				}
				Action<bool> useTriggerHeightAct = (b) =>
				{
					if (b)
						changeObjectZPos(so, 120);
					else
						changeObjectZPos(so, 0);
					heightSlider.SetActive(!b);
				};
				createToggleProperty("Keep the tile's height", BasicPanel.transform, useTriggerHeight, useTriggerHeightAct);
				// The new tile type is heading * 2 + (quality & 1): 0-9, 10 or more keeps it
				// (15 becomes 10). It was read from the owner's low bit (the wall texture).
				string[] tileTypes = new string[] { "Solid", "Open", "Diagonal SE", "Diagonal SW", "Diagonal NE", "Diagonal NW", "Slope N", "Slope S", "Slope E", "Slope W", "Keep the tile's type" };
				int curTileType = Math.Min(10, (so.Direction << 1) + (so.Quality & 1));
				Action<int> changeTypeAct = (i) =>
				{
					so.Direction = i >> 1;
					so.Quality = (so.Quality & 0x3E) + (i & 1);
					Quality.text = so.Quality.ToString();
					Direction.text = so.Direction.ToString();
				};

				int floorIndex = (so.Quality & 0x3E) >> 1;
				bool useCurrentFloor = floorIndex >= 11 ? true : false;   // the game applies 0-10 (0xB and up keep)
				bool useCurrentWall = so.Owner >= 48 ? true : false;
				createDropdownProperty("New tile type", BasicPanel.transform, tileTypes[curTileType], tileTypes, changeTypeAct);
				// The level's floor list has 10 entries (0-9); index 10 is applied by the game but
				// has no texture here to show, so show the tile's own for it.
				Texture2D curFloorTex = useCurrentFloor || floorIndex >= 10 ? MapCreator.GetFloorTextureFromIndex(so.Tile.FloorTexture, so.CurrentLevel) : MapCreator.GetFloorTextureFromIndex(floorIndex, so.CurrentLevel);
				Texture2D curWallTex = useCurrentWall ? MapCreator.GetWallTextureFromIndex(so.Tile.WallTexture, so.CurrentLevel) : MapCreator.GetWallTextureFromIndex(so.Owner, so.CurrentLevel);
				Button changeFloorTex = createImageButtonProperty("Floor texture", BasicPanel.transform, Sprite.Create(curFloorTex, new Rect(0, 0, 32.0f, 32.0f), Vector2.zero));
				if(useCurrentFloor)
					changeFloorTex.transform.parent.gameObject.SetActive(false);
				Action<bool> useCurFloorAct = (b) =>
				{
					if (b)
						so.Quality = (11 << 1) + (so.Quality & 0x01);
					else
						so.Quality = (so.Quality & 0x01);
					changeFloorTex.transform.parent.gameObject.SetActive(!b);
				};
				GameObject useCurFloor = createToggleProperty("Use current floor texture", BasicPanel.transform, useCurrentFloor, useCurFloorAct);
				Button changeWallTex = createImageButtonProperty("Wall texture", BasicPanel.transform, Sprite.Create(curWallTex, new Rect(0, 0, 64.0f, 64.0f), Vector2.zero));
				if (useCurrentWall)
					changeWallTex.transform.parent.gameObject.SetActive(false);
				Action<bool> useCurWallAct = (b) =>
				{
					if (b)
						so.Owner = 63;
					else
						so.Owner = 0;
					changeWallTex.transform.parent.gameObject.SetActive(!b);
				};
				GameObject useCurWall = createToggleProperty("Use current wall texture", BasicPanel.transform, useCurrentWall, useCurWallAct);
				Action<int> selectFloorAct = (i) =>
				{
					int curType = so.Quality & 0x01;
					so.Quality = (i << 1) + curType;
					changeFloorTex.GetComponent<Image>().sprite = Sprite.Create(MapCreator.GetFloorTextureFromIndex(i, so.CurrentLevel), new Rect(0, 0, 32.0f, 32.0f), Vector2.zero);
					Destroy(uiManager.TexturePicker);
				};
				Action<int> selectWallAct = (i) =>
				{
					so.Owner = i;
					changeWallTex.GetComponent<Image>().sprite = Sprite.Create(MapCreator.GetWallTextureFromIndex(so.Owner, so.CurrentLevel), new Rect(0, 0, 64.0f, 64.0f), Vector2.zero);
					Destroy(uiManager.TexturePicker);
				};
				Action spawnFloorPicker = () => uiManager.CreateTexturePicker(uiManager.GetCurrentLevelTextures(TextureType.Floor, false), selectFloorAct, "Textures", "Select new floor texture");
				Action spawnWallPicker = () => uiManager.CreateTexturePicker(uiManager.GetCurrentLevelTextures(TextureType.Wall, false), selectWallAct, "Textures", "Select new wall texture");
				changeFloorTex.onClick.AddListener(() => spawnFloorPicker());
				changeWallTex.onClick.AddListener(() => spawnWallPicker());
			}
			else if (so.ObjectID == 390)	//Spell trap
			{
				Tuple<Dictionary<string, int>, string[]> pair = StaticObject.GetSpelltrapEffects();
				createHeightSlider(so);
				createDirectionDropdown(so);
				int curSpell = so.GetSpellTrapSpell();
				string curSpellName = StaticObject.GetEnchantmentName(curSpell);
				string[] validSpells = pair.Item2;
				Action<int> changeSpellAct = (i) =>
				{
					int spellIndex = pair.Item1[validSpells[i]];
					so.SetSpellTrapSpell(spellIndex);
					resetAdvancedPanel(so);
				};
				
				createDropdownProperty("Spell", BasicPanel.transform, curSpellName, validSpells, changeSpellAct);
			}
			else if (so.ObjectID == 391)	//Create object
			{
				createHeightSlider(so);
				Action<float> changeChanceAct = (f) =>
				{
					so.Quality = ((int)f) * -63 / 100 + 63;
					Quality.text = so.Quality.ToString();
				};
				int curChance = ((so.Quality * -1) + 63) * 100 / 63;
				createSliderProperty("Spawn chance", BasicPanel.transform, curChance, 0, 100.0f, changeChanceAct);
				StaticObject template = so.GetContainedObject();
				if (template)
					createLinkProperty(so, template, "Template", "Select", "Destroy", (next) => destroyContainedObject(so, next));
				else				
					createAddNextInvProperty(so, StaticObject.GetAll(), "", "Add template");
			}
			else if (so.ObjectID == 393 || so.ObjectID == 394)	//Ward and tell traps
			{
				// Hits the critter that set it off when its item & 0xF equals the quality,
				// or any critter for 0x3F: 3 + rand(Casting) damage.
				string[] classes = new string[17];
				for (int i = 0; i < 16; i++) classes[i] = string.Format("Critters with item & 0xF = {0} (e.g. {1})", i, StaticObject.GetMonsterName(i));
				classes[16] = "Any critter (0x3F)";
				int cur = so.Quality == 0x3F ? 16 : Mathf.Clamp(so.Quality, 0, 15);
				createDropdownProperty("Hits", BasicPanel.transform, classes[cur], classes, (i) => changeObjectQuality(so, i == 16 ? 0x3F : i));
			}
			else if (so.ObjectID == 395)	//Delete object
			{
				// Deletes its linked object from the square (quality, owner).
				createSliderProperty("Square X (quality)", BasicPanel.transform, so.Quality, 0, 63.0f, (f) => changeObjectQuality(so, (int)f));
				createSliderProperty("Square Y (owner)", BasicPanel.transform, so.Owner, 0, 63.0f, (f) => changeObjectOwner(so, (int)f));
			}
			else if (so.ObjectID == 396)	//Inventory trap
			{
				int curInvCheck = so.GetInventoryTrapItem();
				Button selInvCheck = createImageButtonProperty("Item to check", BasicPanel.transform, MapCreator.GetObjectSpriteFromID(curInvCheck));
				Action<int> selInvCheckAct = (i) =>
				{
					so.SetInventoryTrapItem(i);
					selInvCheck.GetComponent<Image>().sprite = MapCreator.GetObjectSpriteFromID(i);
					resetAdvancedPanel(so);
				};
				Action spawnItemPicker = () => uiManager.CreateSpecifiedItemList(selInvCheckAct, StaticObject.GetAll(), "", "Select object for trap to search for.");
				selInvCheck.onClick.AddListener(() => spawnItemPicker());
				// With a non-zero Z the stack must hold at least that many.
				createSliderProperty("At least this many (Z, 0 any)", BasicPanel.transform, so.ZPos, 0, 127.0f, (f) => changeObjectZPos(so, (int)f));
			}
			else if (so.ObjectID == 397)	//Var
			{
				// Z 0: quest bit; 1-63: game variable (the player record has 64).
				bool isSetQuest = so.ZPos == 0 ? true : false;
				GameObject varIndexGO = createSliderProperty("Variable index", BasicPanel.transform, Math.Max(1, so.ZPos), 1.0f, 63.0f, (f) => changeObjectZPos(so, (int)f));
				if (isSetQuest)
					varIndexGO.SetActive(false);
				Action<bool> setQuestAct = (b) =>
				{
					if(!b)
					{
						varIndexGO.SetActive(true);
						changeObjectZPos(so, 1);
					}
					else
					{
						varIndexGO.SetActive(false);
						changeObjectZPos(so, 0);
					}
				};
				createToggleProperty("Set quest variables", BasicPanel.transform, isSetQuest, setQuestAct);
				createInputProperty("Value / quest index", BasicPanel.transform, so.GetVarTrapValue(), (str) => { int v; if (int.TryParse(str, out v)) { so.SetVarTrapValue(v); resetAdvancedPanel(so); } });
				string[] opTypes = new string[] { "Add", "Substract", "Set", "And", "Or", "XOR", "Shift left" };
				if (isSetQuest)
					opTypes = new string[] { "Set bit", "Clear bit", "Set bit", "Set bit", "Set bit", "Toggle bit", "Set bit", "Set bit" };
				createDropdownProperty("Operation type", BasicPanel.transform, opTypes[Mathf.Clamp(so.Direction, 0, opTypes.Length - 1)], opTypes, (i) => changeObjectDir(so, i));
				if (!isSetQuest)
					createButtonProperty("The result is kept to 6 bits (0-63).", BasicPanel.transform);
			}
			else if (so.ObjectID == 398) //Check var trap
			{
				createSliderProperty("Variable index", BasicPanel.transform, Math.Max(1, so.ZPos), 1.0f, 63.0f, (f) => changeObjectZPos(so, (int)f));
				bool shiftInstead = so.XPos == 0 ? true : false;
				createSliderProperty("Check range", BasicPanel.transform, (int)so.Direction, 0, 7.0f, (f) => changeObjectDir(so, (int)f));
				Action<bool> shiftInsteadAct = (b) =>
				{
					if (b)
						changeObjectX(so, 0);
					else
						changeObjectX(so, 1);
				};
				createToggleProperty("Pack 3 bits of each instead of adding", BasicPanel.transform, shiftInstead, shiftInsteadAct);
				createInputProperty("Value checked for", BasicPanel.transform, so.GetVarTrapValue(), (str) => { int v; if (int.TryParse(str, out v)) { so.SetVarTrapValue(v); resetAdvancedPanel(so); } });
				int count = so.GetContainedObjectsCount();
				if (count == 1)
					createAddNextInvProperty(so, StaticObject.GetIDsByType(StaticObject.GetTrapValidChildren(so.ObjectID)), "Traps & triggers", "Add next trap / trigger");
			}
				else if (so.ObjectID == 400)	//Text trap
			{
				createLargeInputProperty("Text", BasicPanel.transform, MapCreator.StringData.GetTextTrapString(so), (str) => MapCreator.StringData.SetTextTrapString(so, str));
			}
		}
		if (contained != null && contained.Count > 0 && !so.IsTrap() && !so.IsTrigger())
		{
			for (int i = 0; i < contained.Count; i++)
			{
				StaticObject inv = contained[i];
				if (inv.IsTrigger())
					createLinkProperty(so, inv, "Trigger list", "Select", "Destroy", (next) => destroyContainedObject(so, next));
			}
		}
	}
	private void destroyContainedObject(StaticObject parent, StaticObject target)
	{
		MapCreator.RemoveObject(target, true);
		SetBasicPanel(parent);
	}
	/// <summary>A UW2 trap's fields decoded (Uw2Traps.Decode), with block 9 for the text string trap.</summary>
	static string Uw2TrapNow(StaticObject so)
	{
		Func<int, string> text = (n) =>
		{
			var sd = MapCreator.StringData;
			if (sd == null || sd.Blocks == null) return null;
			foreach (var b in sd.Blocks)
				if (b.BlockNumber == 9) return n >= 0 && n < b.Strings.Count ? b.Strings[n] : null;
			return null;
		};
		return UltimateEditor.Games.Uw2Traps.Decode(so.ObjectID, so.Quality, so.Owner, so.ZPos, so.Direction, so.XPos, so.YPos, so.IsQuantity, so.Special, text);
	}

	private void changeObjectIsDoorOpen(StaticObject so, bool val)
	{
		so.IsDoorOpen = val;
		Door.isOn = val;
	}
	private void changeObjectOwner(StaticObject so, int val)
	{
		so.Owner = val;
		Owner.text = so.Owner.ToString();
	}
	private void changeObjectSpecial(StaticObject so, int val)
	{
		so.Special = val;
		Special.text = so.Special.ToString();
	}
	private void changeObjectQuality(StaticObject so, int val)
	{
		so.Quality = val;
		Quality.text = so.Quality.ToString();
	}
	private void changeObjectZPos(StaticObject so, int val)
	{
		so.ZPos = val;
		Height.text = so.ZPos.ToString();
	}
	private void changeObjectDir(StaticObject so, int val)
	{
		so.Direction = val;
		Direction.text = so.Direction.ToString();
	}
	private void changeObjectFlags(StaticObject so, int val)
	{
		so.Flags = val;
		Flags.text = so.Flags.ToString();
	}
	private void changeObjectXY(StaticObject so, int x, int y)
	{
		so.XPos = x;
		so.YPos = y;
		XPos.text = so.XPos.ToString();
		YPos.text = so.YPos.ToString();
	}
	private void changeObjectX(StaticObject so, int x)
	{
		so.XPos = x;
		XPos.text = so.XPos.ToString();
	}
	private void changeObjectY(StaticObject so, int y)
	{
		so.YPos = y;
		YPos.text = so.YPos.ToString();
	}
	private void changeMobileAttitude(MobileObject mo, int att)
	{
		mo.Attitude = att;
		Attitude.text = mo.Attitude.ToString();
	}
	private void changeMobileGoal(MobileObject mo, int goal)
	{
		mo.Goal = goal;
		Goal.text = mo.Goal.ToString();
	}
	private void changeMobileHP(MobileObject mo, int hp)
	{
		mo.HP = hp;
		HP.text = mo.HP.ToString();
	}
	private void changeMobileNPCID(MobileObject mo, int npc)
	{
		mo.Whoami = npc;
		NPCID.text = mo.Whoami.ToString();
	}
	private void resetAdvancedPanel(StaticObject so)
	{
		XPos.text = so.XPos.ToString();
		YPos.text = so.YPos.ToString();
		Height.text = so.ZPos.ToString();
		Enchantable.isOn = so.IsEnchanted;
		Invisible.isOn = so.IsInvisible;
		IsQuantity.isOn = so.IsQuantity;
		Door.isOn = so.IsDoorOpen;
		Direction.text = so.Direction.ToString();
		Flags.text = so.Flags.ToString();
		Quality.text = so.Quality.ToString();
		Owner.text = so.Owner.ToString();
		Special.text = so.Special.ToString();
	}
	private void createEnchantmentProperties(StaticObject so, Dictionary<string, int> dict)
	{
		Toggle isEnchanted = createToggleProperty("Enchanted", BasicPanel.transform, so.IsEnchanted).GetComponentInChildren<Toggle>();
		GameObject enchDropGO = createEnchantmentProperty(so, dict, false, "Enchantment");
		Dropdown enchDrop = enchDropGO.GetComponentInChildren<Dropdown>();
		if (!so.IsEnchanted)
			enchDropGO.SetActive(false);
		Action<bool> isEnchAct = (b) =>
		{
			if (b)
			{
				so.IsEnchanted = true;
				enchDropGO.SetActive(true);
				string lastEnch = enchDrop.options[enchDrop.value].text;
				if (!dict.ContainsKey(lastEnch))
					lastEnch = dict.First().Key;
				so.Special = dict[lastEnch] + 512;
				Special.text = so.Special.ToString();
			}
			else
			{
				so.IsEnchanted = false;
				so.Special = 0;
				Special.text = so.Special.ToString();
				enchDropGO.SetActive(false);
			}
		};
		isEnchanted.onValueChanged.AddListener((b) => isEnchAct(b));

		// What the enchantment really does (Games/EnchantmentEffects.cs): UW2's weapon names are
		// swapped (Accuracy adds damage and Damage adds to hit), and toughness on shields and
		// rings lands twice on the torso.
		var game = LevArk.Game.Kind;
		bool shieldOrRing = so.IsShield() || so.IsBauble();
		var note = UltimateEditor.Uw1.UI.UiKit.Paragraph(BasicPanel.transform, "", 10, new Color(1f, 0.85f, 0.55f));
		note.horizontalOverflow = HorizontalWrapMode.Wrap;
		Action refreshNote = () =>
		{
			string real = so.IsEnchanted ? UltimateEditor.Games.EnchantmentEffects.Describe(game, so.GetEnchantment(), shieldOrRing) : null;
			note.text = (real != null ? "In the game: " + real + "\n" : "") + UltimateEditor.Games.EnchantmentEffects.Summary(game);
		};
		refreshNote();
		enchDrop.onValueChanged.AddListener((i) => refreshNote());
		isEnchanted.onValueChanged.AddListener((b) => refreshNote());
	}
	private GameObject createEnchantmentProperty(StaticObject so, Dictionary<string, int> dict, bool addCharges, string text)
	{
		if (addCharges)
			createSliderProperty("Charges", BasicPanel.transform, so.Quality, 0, 63.0f, (f) => so.Quality = (int)f);

		GameObject dropGO = createDropdownProperty(text, BasicPanel.transform, so.GetEnchantmentName(), dict);
		Dropdown drop = dropGO.GetComponentInChildren<Dropdown>();
		Action<int> changeEnchantment = (i) =>
		{
			int val = dict[drop.options[i].text];
			changeObjectSpecial(so, val + 512);
		};
		drop.onValueChanged.AddListener((i) => changeEnchantment(i));
		return dropGO;
	}
	// Trigger options (event/TRIGGER.C UseTrigger): the target square where the trap chain
	// runs, who may set it off and whether it stays after use.
	private void createTriggerProperties(StaticObject so)
	{
		createSliderProperty("Target square X (quality)", BasicPanel.transform, so.Quality, 0, 63.0f, (f) => changeObjectQuality(so, (int)f));
		createSliderProperty("Target square Y (owner)", BasicPanel.transform, so.Owner, 0, 63.0f, (f) => changeObjectOwner(so, (int)f));
		// Flags holds id bits 9-11: bit 10 is Flags & 2, bit 11 is Flags & 4.
		createToggleProperty("The player can set it off", BasicPanel.transform, (so.Flags & 4) != 0, (b) => changeObjectFlags(so, b ? so.Flags | 4 : so.Flags & ~4));
		createToggleProperty("Creatures can set it off", BasicPanel.transform, so.IsEnchanted, (b) => { so.IsEnchanted = b; Enchantable.isOn = b; });
		createToggleProperty("Reusable (keep the traps after use)", BasicPanel.transform, (so.Flags & 2) != 0, (b) => changeObjectFlags(so, b ? so.Flags | 2 : so.Flags & ~2));
		if (so.ObjectID == UltimateEditor.Uw1.Uw1Traps.FirstTrigger + (int)UltimateEditor.Uw1.Uw1Traps.TriggerType.Look)
			createSliderProperty("Search difficulty (Z; 0 always noticed)", BasicPanel.transform, so.ZPos, 0, 127.0f, (f) => changeObjectZPos(so, (int)f));
		createButtonProperty("Use target = this tile", BasicPanel.transform, () =>
		{
			StaticObject top = so.GetFirstContainer();
			MapTile tile = top != null && top.Tile != null ? top.Tile : so.Tile;
			if (tile == null)
				return;
			changeObjectQuality(so, tile.Position.x);
			changeObjectOwner(so, tile.Position.y);
			SetBasicPanel(so);
		});
	}

	// A trap's flags count the triggers that point at it; delete_trap (TRIGGER.C) relies on
	// the count to remove them when the trap is deleted. Show it and offer to recount.
	private void createTrapReferenceProperty(StaticObject so)
	{
		int count = countTriggersTo(so);
		string text = string.Format("Triggers pointing here: {0} (flags say {1})", count, so.Flags);
		createButtonProperty(text + (count != so.Flags ? " - click to fix" : ""), BasicPanel.transform, () =>
		{
			changeObjectFlags(so, Math.Min(7, countTriggersTo(so)));
			SetBasicPanel(so);
		});
	}

	private static int countTriggersTo(StaticObject trap) => MapCreator.CountTriggersTo(trap);

	private void createLinkingProperties(StaticObject so)
	{
		if (so.Special > 0)   //Has a link to something (either inside (IsQuantity = false) or outside (IsQuantity = true)
		{
			if (!so.IsQuantity)
			{
				Action<StaticObject> popup = (target) =>
				{
					if (target.IsQuantity == false && target.Special > 0)
						uiManager.SpawnYesNoMenu("Warning, this will also destroy linked objects.", 40.0f, () => destroyContainedObject(so, target), null, "Destroy", "Cancel");
					else
						destroyContainedObject(so, target);
				};
				createMultipleLinkProperty(so, so.GetContainedObjects(), "Links to", "Destroy", popup);
			}
			else
			{
				Action<StaticObject> unlink = (target) =>
				{
					so.Special = 0;
					SetBasicPanel(so);
				};
				createMultipleLinkProperty(so, so.GetContainedObjects(), "Links to", "Unlink", unlink);
			}
		}
		else
		{
			int id = so.IsLever() ? so.GetUseTrigger().ObjectID : so.ObjectID;
			createAddNextInvProperty(so, StaticObject.GetIDsByType(StaticObject.GetTrapValidChildren(id)), "Traps & triggers", "Add next trap / trigger");
			createAddNextLinkProperty(so);
		}
	}
	private GameObject createAddNextInvProperty(StaticObject so, List<int> validIDs, string listDescription, string buttonDescription, Action<StaticObject> additionalAction = null)
	{
		Action<StaticObject> afterCreating = (next) =>
		{
			if (!uiManager.MapCreator.AddTrigger(so, next))
				uiManager.SpawnPopupMessage("Failed to add object");
			additionalAction?.Invoke(next);
			uiManager.SelectObject(next, false);
		};
		Action<int> createNextTrap = (i) => uiManager.CreateObject(i, so.CurrentLevel, new Vector2Int(so.XPos, so.YPos), so.Tile, afterCreating);
		Action createList = () => uiManager.CreateSpecifiedItemList(createNextTrap, validIDs, listDescription, "Select object type to add.");
		GameObject buttonGO = createButtonProperty(buttonDescription, BasicPanel.transform, createList);
		return buttonGO;
	}

	private GameObject createAddNextLinkProperty(StaticObject so)
	{
		Action startLinking = () =>
		{
			if (MapCreator.ObjectToGO.ContainsKey(so))
				uiManager.StartLinking(so, MapCreator.ObjectToGO[so].transform.position);
			else
			{
				StaticObject container = so.GetFirstContainer();
				if (MapCreator.ObjectToGO.ContainsKey(container))
					uiManager.StartLinking(so, MapCreator.ObjectToGO[container].transform.position);
				else
					Debug.LogErrorFormat("Failed to get GameObject for first container {0}", container);
			}
		};
		GameObject buttonGO = createButtonProperty("Create new link", BasicPanel.transform, startLinking);
		return buttonGO;
	}
	private void createAddUseTriggerProperty(StaticObject so)
	{
		Action createUseTrigger = () =>
		{
			StaticObject use = MapCreator.AddObject(so.Tile, new Vector2Int(3, 3), so.CurrentLevel, 418, null, true);
			use.SetProperties(false, true, true, true, 1, 0, 0, 0);
			so.Special = use.CurrentAdress;
			use.PrevAdress = so.CurrentAdress;
			uiManager.SelectObject(use, false);
		};
		createButtonProperty("Add trigger", BasicPanel.transform, createUseTrigger);
	}
	private GameObject createQuantityInput(StaticObject so)
	{
		Action<string> changeQuantityAct = (s) =>
		{
			so.Special = so.Special >= 512 ? int.Parse(s) + 512 : int.Parse(s);
			Special.text = so.Special.ToString();
		};
		int val = so.Special >= 512 ? so.Special - 512 : so.Special;
		return createInputProperty(so.Special >= 512 ? "Special" : "Quantity", BasicPanel.transform, val, changeQuantityAct);
	}
	private void createQuantityToggle(StaticObject so, GameObject quantityInputObject)
	{
		Action<bool> changeQuantityAct = (b) =>
		{
			if (b)
			{
				quantityInputObject.SetActive(true);
				so.Special = 1;
				Special.text = 1.ToString();
				quantityInputObject.GetComponentInChildren<InputField>().text = 1.ToString();
			}
			else
			{
				quantityInputObject.SetActive(false);
				so.Special = 0;
				Special.text = 0.ToString();
			}
			so.IsQuantity = b;
			IsQuantity.isOn = b;
		};
		createToggleProperty("Quantity", BasicPanel.transform, so.IsQuantity, changeQuantityAct);
	}

	private void createMultipleLinkProperty(StaticObject so, List<StaticObject> targets, string mainString, string altString, System.Action<StaticObject> altAction)
	{
		GameObject dbGO = Instantiate(DropdownButton2PropertyPrefab, BasicPanel.transform);
		Transform descrT = dbGO.transform.Find("Description");
		descrT.GetComponent<Text>().text = mainString;
		Dictionary<int, StaticObject> dict = new Dictionary<int, StaticObject>();
		Dropdown dropdown = dbGO.GetComponentInChildren<Dropdown>();
		dropdown.options = new List<Dropdown.OptionData>();
		for (int i = 0; i < targets.Count; i++)
		{
			dict[i] = targets[i];
			dropdown.options.Add(new Dropdown.OptionData(targets[i].Name + " [" + targets[i].CurrentAdress + "]"));
		}
		Button buttonA = dbGO.transform.Find("ButtonA").GetComponent<Button>();
		buttonA.GetComponentInChildren<Text>().text = "Select";
		System.Action select = () => uiManager.SelectObject(targets[dropdown.value], true);
		buttonA.onClick.AddListener(() => select());
		Button buttonB = dbGO.transform.Find("ButtonB").GetComponent<Button>();
		buttonB.GetComponentInChildren<Text>().text = altString;
		buttonB.onClick.AddListener(() => altAction(targets[dropdown.value]));
	}

	private void createLinkProperty(StaticObject so, StaticObject target, string text, string butTextA, string butTextB, Action<StaticObject> altAct)
	{
		GameObject ibGO = Instantiate(InputButton2PropertyPrefab, BasicPanel.transform);
		Transform descrT = ibGO.transform.Find("Description");
		descrT.GetComponent<Text>().text = text;
		InputField input = ibGO.GetComponentInChildren<InputField>();
		input.text = target.Name + " [" + target.CurrentAdress + "]";
		input.interactable = false;
		Button buttonA = ibGO.transform.Find("ButtonA").GetComponent<Button>();
		buttonA.GetComponentInChildren<Text>().text = butTextA;
		buttonA.onClick.AddListener(() => uiManager.SelectObject(target, true));
		Button buttonB = ibGO.transform.Find("ButtonB").GetComponent<Button>();
		buttonB.GetComponentInChildren<Text>().text = butTextB;
		buttonB.onClick.AddListener(() => altAct(target));
	}
	private void createQualitySlider(StaticObject so, float val, float min, float max)
	{
		System.Action<float> changeQualityAct = (f) =>
		{
			so.Quality = (int)f;
			Quality.text = so.Quality.ToString();
		};
		createSliderProperty("Quality", BasicPanel.transform, val, min, max, changeQualityAct);
	}
	private void createIdentifiedToggle(StaticObject so)
	{
		Action<bool> changeIdent = (b) =>
		{
			so.Direction = b ? 7 : 0;
			Direction.text = so.Direction.ToString();
		};
		GameObject toggleGO = createToggleProperty("Identified", BasicPanel.transform, so.Direction == 7, changeIdent);
	}
	private void createOwnerDropdown(StaticObject so)
	{
		List<Dropdown.OptionData> races = new List<Dropdown.OptionData>();
		for (int i = 0; i < 29; i++)
		{
			Dropdown.OptionData race = new Dropdown.OptionData(MapCreator.StringData.GetRace(i));
			races.Add(race);
		}
		GameObject dropdownGO = createDropdownProperty("Owner", BasicPanel.transform, MapCreator.StringData.GetRace(so.Owner), races);
		Dropdown dropdown = dropdownGO.GetComponentInChildren<Dropdown>();
		Action<int> changeOwnerAct = (i) =>
		{
			if (!MapCreator.StringData.IsRace(i))
				i = 0;
			so.Owner = i;
			Owner.text = so.Owner.ToString();
			dropdown.value = dropdown.options.FindIndex((s) => s.text == MapCreator.StringData.GetRace(so.Owner));
		};
		dropdown.onValueChanged.AddListener((i) => changeOwnerAct(i));
	}
	private void createHeightSlider(StaticObject so)
	{
		Action<float> changeHeight = (f) =>
		{
			so.ZPos = (int)f;
			Height.text = so.ZPos.ToString();
			MapCreator.SetGOHeight(so);
		};
		createSliderProperty("Height", BasicPanel.transform, so.ZPos, so.Tile.FloorHeight * 8.0f, 127.0f, changeHeight);
	}
	private void createDirectionDropdown(StaticObject so)
	{
		Action<int> changeDir = (i) =>
		{
			so.Direction = i;
			Direction.text = so.Direction.ToString();
			MapCreator.SetGODirection(so);
		};
		string[] dirs = new string[] { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
		createDropdownProperty("Direction", BasicPanel.transform, dirs[so.Direction], dirs, changeDir);
	}
	private void createMonsterDropdown(StaticObject so)
	{
		List<Dropdown.OptionData> monsters = new List<Dropdown.OptionData>();
		for (int i = 0; i < 64; i++)
		{
			if(i == 0)
			{
				monsters.Add(new Dropdown.OptionData("none"));
				continue;
			}
			monsters.Add(new Dropdown.OptionData(StaticObject.GetMonsterName(i)));
		}
		GameObject dropdownGO = createDropdownProperty("Remains", BasicPanel.transform, monsters[so.Owner].text, monsters);
		Dropdown dropdown = dropdownGO.GetComponentInChildren<Dropdown>();
		Action<int> changeOwnerAct = (i) =>
		{
			if (!StaticObject.IsMonster(i))
				i = 0;
			so.Owner = i;
			Owner.text = so.Owner.ToString();
			dropdown.value = dropdown.options.FindIndex((s) => s.text == StaticObject.GetMonsterName(so.Owner));
		};
		dropdown.onValueChanged.AddListener((i) => changeOwnerAct(i));
	}
	private void createRemoveFromContainerButton(StaticObject so, StaticObject container)
	{
		string text = "";
		if (container.IsDoor())
			text = "Remove from door";
		else
			text = "Remove from container";

		GameObject buttonGO = createButtonProperty(text, BasicPanel.transform);
		System.Action removeFromContainerAct = () =>
		{
			container.RemoveFromContainer(so);
			container.Tile.AddObjectToTile(so, container);
			GameObject go = MapCreator.SpawnGO(so, MapCreator.MapTileToGO[container.Tile]);
			uiManager.OnSelectObject(go);
			GameObject col = go.GetComponentInChildren<Collider>().gameObject;
			uiManager.SetSpriteOutline(col, true, Color.green);
			Destroy(buttonGO);
		};
		buttonGO.GetComponentInChildren<Button>().onClick.AddListener(() => removeFromContainerAct());
	}
	private void createSelectContainerButton(StaticObject so, StaticObject container)
	{
		string text = "";
		if (container.IsDoor())
			text = "Select door";
		else
			text = "Select container";

		GameObject buttonGO = createButtonProperty(text, BasicPanel.transform);
		System.Action selectContainerAct = () => uiManager.SelectObject(container, false);
		buttonGO.GetComponentInChildren<Button>().onClick.AddListener(() => selectContainerAct());
	}
	private GameObject createInventoryProperty(string text, Transform parent, StaticObject container)
	{
		List<StaticObject> inventory = container.GetContainedObjects();
		if (inventory == null || inventory.Count == 0)
			return null;
		GameObject inventoryGO = Instantiate(DropdownButton2PropertyPrefab, parent);
		Transform descrT = inventoryGO.transform.Find("Description");
		descrT.GetComponent<Text>().text = text;
		Dropdown dropdown = inventoryGO.GetComponentInChildren<Dropdown>();
		Dictionary<int, StaticObject> invDict = new Dictionary<int, StaticObject>();
		fillDropdownWithInventory(dropdown, inventory, invDict);
		Button buttonA = inventoryGO.transform.Find("ButtonA").GetComponent<Button>();
		System.Action removeAct = () =>
		{
			int val = dropdown.value;
			StaticObject so = invDict[val];

			container.RemoveFromContainer(so);
			container.Tile.AddObjectToTile(so, container);
			GameObject go = MapCreator.SpawnGO(so, MapCreator.MapTileToGO[container.Tile]);

			invDict = new Dictionary<int, StaticObject>();
			List<StaticObject> newInv = container.GetContainedObjects();
			if (newInv == null || newInv.Count == 0)
				Destroy(inventoryGO);
			fillDropdownWithInventory(dropdown, newInv, invDict);
		};
		buttonA.onClick.AddListener(() => removeAct());
		buttonA.transform.Find("Text").GetComponent<Text>().text = "Remove";
		Button buttonB = inventoryGO.transform.Find("ButtonB").GetComponent<Button>();
		System.Action selectAct = () =>
		{
			int val = dropdown.value;
			StaticObject so = invDict[val];
			uiManager.SelectObject(so, false);
		};
		buttonB.onClick.AddListener(() => selectAct());
		buttonB.transform.Find("Text").GetComponent<Text>().text = "Select";
		return inventoryGO;
	}
	private void fillDropdownWithInventory(Dropdown dropdown, List<StaticObject> inventory, Dictionary<int, StaticObject> invDict)
	{				
		dropdown.options = new List<Dropdown.OptionData>();
		for (int i = 0; i < inventory.Count; i++)
		{
			dropdown.options.Add(new Dropdown.OptionData(inventory[i].GetFullName() + " [" + inventory[i].CurrentAdress + "]"));
			invDict[i] = inventory[i];
		}
		dropdown.value = 0;
		dropdown.RefreshShownValue();
	}
	private GameObject createSliderProperty(string text, Transform parent, float val, float min, float max, System.Action<float> sliderAction, System.Func<int, int> valAct = null)
	{
		GameObject sliderGO = Instantiate(SliderPropertyPrefab, parent);
		Transform descrT = sliderGO.transform.Find("Description");
		descrT.GetComponent<Text>().text = text;
		Slider slider = sliderGO.GetComponentInChildren<Slider>();
		
		slider.minValue = min;
		slider.maxValue = max;
		slider.value = val;
		Text handleText = slider.handleRect.GetComponentInChildren<Text>();
		handleText.text = val.ToString();
		if (valAct == null)
		{
			
			slider.onValueChanged.AddListener((f) => sliderAction(f));
			slider.onValueChanged.AddListener((f) => handleText.text = ((int)f).ToString());
		}
		else
		{
			//handleText.text = (valAct((int)val)).ToString();
			slider.onValueChanged.AddListener((f) => sliderAction(valAct((int)f)));
			slider.onValueChanged.AddListener((f) => handleText.text = (valAct((int)f)).ToString());
		}
		return sliderGO;
	}
	private GameObject createToggleProperty(string text, Transform parent, bool val, System.Action<bool> toggleAction = null)
	{
		GameObject toggleGO = Instantiate(TogglePropertyPrefab, parent);
		//toggleGO.transform.SetSiblingIndex(parent.childCount - 1);
		Transform descrT = toggleGO.transform.Find("Description");
		descrT.GetComponent<Text>().text = text;
		Toggle toggle = toggleGO.GetComponentInChildren<Toggle>();
		toggle.isOn = val;
		if(toggleAction != null)
			toggle.onValueChanged.AddListener((b) => toggleAction(b));
		return toggleGO;
	}
	private GameObject createInputProperty(string text, Transform parent, int val, Action<string> inputAction, int min = 0, int max = 512)
	{
		GameObject inputGO = Instantiate(InputPropertyPrefab, parent);
		Transform descrT = inputGO.transform.Find("Description");
		descrT.GetComponent<Text>().text = text;
		InputField input = inputGO.GetComponentInChildren<InputField>();
		input.contentType = InputField.ContentType.IntegerNumber;
		input.text = val.ToString();
		Action<string> clampVals = (str) =>
		{
			int newval = int.Parse(str);
			newval = Mathf.Clamp(newval, min, max);
			input.text = newval.ToString();
		};
		input.onEndEdit.AddListener((str) => clampVals(str));
		input.onValueChanged.AddListener((s) => inputAction(s));
		return inputGO;
	}
	private GameObject createLargeInputProperty(string text, Transform parent, string val, System.Action<string> inputAction)
	{
		GameObject inputGO = Instantiate(LargeInputPropertyPrefab, parent);
		Transform descrT = inputGO.transform.Find("Description");
		descrT.GetComponent<Text>().text = text;
		InputField input = inputGO.GetComponentInChildren<InputField>();
		input.text = val;
		input.onValueChanged.AddListener((s) => inputAction(s));
		return inputGO;
	}
	private GameObject createDropdownProperty(string text, Transform parent, string val, string[] options, Action<int> dropdownAction = null)
	{
		List<Dropdown.OptionData> dropdownOptions = new List<Dropdown.OptionData>();
		for (int i = 0; i < options.Length; i++)
			dropdownOptions.Add(new Dropdown.OptionData(options[i]));
		return createDropdownProperty(text, parent, val, dropdownOptions, dropdownAction);
	}
	private GameObject createDropdownProperty(string text, Transform parent, string val, Dictionary<string, int> dict, Action<int> dropdownAction = null)
	{
		string[] options = new string[dict.Count];
		int i = 0;
		foreach (var item in dict)
		{
			options[i] = item.Key;
			i++;
		}
		return createDropdownProperty(text, parent, val, options, dropdownAction);
	}
	private GameObject createDropdownProperty(string text, Transform parent, string val, List<Dropdown.OptionData> options, Action<int> dropdownAction = null)
	{
		GameObject dropdownGO = Instantiate(DropdownPropertyPrefab, parent);
		Transform descrT = dropdownGO.transform.Find("Description");
		descrT.GetComponent<Text>().text = text;
		Dropdown dropdown = dropdownGO.GetComponentInChildren<Dropdown>();
		dropdown.options = options;
		int index = dropdown.options.FindIndex((s) => s.text == val);
		if (index == -1)
		{
			dropdown.options.Add(new Dropdown.OptionData("unknown"));
			dropdown.value = dropdown.options.Count - 1;
		}
		else
			dropdown.value = index;
		if(dropdownAction != null)
			dropdown.onValueChanged.AddListener((i) => dropdownAction(i));
		return dropdownGO;
	}
	private GameObject createButtonProperty(string text, Transform parent)
	{
		GameObject buttonGO = Instantiate(ButtonPropertyPrefab, parent);
		buttonGO.GetComponentInChildren<Text>().text = text;
		return buttonGO;
	}
	private GameObject createButtonProperty(string text, Transform parent, System.Action buttonAct)
	{
		GameObject buttonGO = createButtonProperty(text, parent);
		Button button = buttonGO.GetComponentInChildren<Button>();
		button.onClick.AddListener(() => buttonAct());
		return buttonGO;
	}
	private Button createImageButtonProperty(string text, Transform parent, Sprite curSprite, System.Action buttonAct = null)
	{
		GameObject buttonGO = Instantiate(ImageButtonPropertyPrefab, parent);
		buttonGO.GetComponentInChildren<Text>().text = text;
		Button button = buttonGO.GetComponentInChildren<Button>();
		button.GetComponent<Image>().sprite = curSprite;
		if (buttonAct != null)
			button.onClick.AddListener(() => buttonAct());
		return button;
	}

	public void DeselectObject()
	{
		SelectedObject = null;
	}

	public void SetObject(StaticObject so)
	{
		SelectedObject = so;
		if (so == null)
		{
			Debug.LogError("Properties Panel : Tried to set object to null (SetObject)");
			return;
		}
		Adresses.text = string.Format("Adr [{0}], next [{1}], prev [{2}]", so.CurrentAdress, so.NextAdress, so.PrevAdress);
		XPos.text = so.XPos.ToString();
		YPos.text = so.YPos.ToString();
		Height.text = so.ZPos.ToString();
		Enchantable.isOn = so.IsEnchanted;
		Invisible.isOn = so.IsInvisible;
		IsQuantity.isOn = so.IsQuantity;
		Door.isOn = so.IsDoorOpen;
		Direction.text = so.Direction.ToString();
		Flags.text = so.Flags.ToString();
		Quality.text = so.Quality.ToString();
		Owner.text = so.Owner.ToString();
		Special.text = so.Special.ToString();

		InventoryLinks = new Dictionary<int, StaticObject>();

		if(!so.IsQuantity)
		{
			SetInventory(so);
		}
		else
		{
			Inventory.options = new List<Dropdown.OptionData>();
			RemoveFromContainer.interactable = false;
		}

		if(so is MobileObject)
		{
			MobileObject mo = (MobileObject)so;
			SetMobileProperties(true);

			Types.options = MobileTypes;

			HomeX.text = mo.XHome.ToString();
			HomeY.text = mo.YHome.ToString();
			Attitude.text = mo.Attitude.ToString();
			Goal.text = mo.Goal.ToString();
			HP.text = mo.HP.ToString();
			NPCID.text = mo.Whoami.ToString();

			SetInventory(mo);
		}
		else
		{
			Types.options = StaticTypes;

			HomeX.text = "";
			HomeY.text = "";
			Attitude.text = "";
			Goal.text = "";
			HP.text = "";
			NPCID.text = "";

			SetMobileProperties(false);
		}
		Types.value = ObjectIDToDropdownIndex[so.ObjectID];
	}

	private void SetInventory(StaticObject so)
	{
		List<StaticObject> inventory = so.GetContainedObjects();
		List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();
		for (int i = 0; i < inventory.Count; i++)
		{
			StaticObject item = inventory[i];
			Dropdown.OptionData option = new Dropdown.OptionData(item.GetFullName());
			options.Add(option);
			InventoryLinks[i] = item;
		}
		Inventory.options = options;
		RemoveFromContainer.interactable = true;
	}

	public void SetMobileProperties(bool enable)
	{
		HomeText.SetActive(enable);
		AttitudeText.SetActive(enable);
		GoalText.SetActive(enable);
		HPText.SetActive(enable);
		NPCIDText.SetActive(enable);

		HomeXGO.SetActive(enable);
		HomeYGO.SetActive(enable);
		AttitudeGO.SetActive(enable);
		GoalGO.SetActive(enable);
		HPGO.SetActive(enable);
		NPCIDGO.SetActive(enable);
	}

}
