using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Linq;

/// <summary>UIManager: create object menus (split from UIManager.cs by region, review package 10).</summary>
public partial class UIManager
{
	#region Create Object Menus

	/// <summary>A pasted book, scroll, writing or grave gets a text slot of its own with a copy
	/// of the text: sharing the original's slot, an edit to either changed both.</summary>
	void GiveOwnText(StaticObject original, StaticObject pasted)
	{
		var sd = MapCreator.StringData;
		if (sd == null)
			return;
		if (pasted.ObjectID == 400)   // text string trap: its string is quality * 32 + owner of block 9
		{
			int free = sd.GetNextEmptyTextTrapString();
			if (free < 0) { SpawnPopupMessage("No free text slot: the pasted trap shares the original's text."); return; }
			string str = sd.GetTextTrapString(original);
			if (str == "INVALID") return;
			pasted.SetTextTrapIndex(free);
			sd.SetTextTrapString(pasted, str != null && str.EndsWith("\n") ? str.Substring(0, str.Length - 1) : str);
			return;
		}
		if (!pasted.IsQuantity || pasted.Special < 512 || pasted.IsEnchanted)
			return;
		int slot = -1;
		string text = null;
		if (pasted.IsWriting()) { text = sd.GetWriting(original); slot = sd.GetNextEmptyWritingSlot(); }
		else if (pasted.IsGrave()) { text = sd.GetGrave(original); slot = sd.GetNextEmptyGraveSlot(); }
		else if (pasted.IsBook() || pasted.IsScroll()) { text = sd.GetScroll(original); slot = sd.GetNextEmptyScrollSlot(); }
		else return;
		if (slot < 0)
		{
			SpawnPopupMessage("No free text slot: the pasted copy shares the original's text.");
			return;
		}
		pasted.Special = 512 + slot;
		if (pasted.IsWriting()) sd.SetWriting(pasted, text);
		else if (pasted.IsGrave()) sd.SetGrave(pasted, text);
		else sd.SetScroll(pasted, text);
	}

	public void CreateObject(int id, int level, Vector2Int tilePos, MapTile tile, Action<StaticObject> afterCreatingAct = null)
	{
		// the create menus take a slot when "Create" is pressed: refuse at once when there is none
		var ld = MapCreator.LevelData[level - 1];
		bool mobile = StaticObject.GetObjectType(id) == ObjectType.Monster;
		if (mobile ? ld.MobileListStart < 0 : ld.StaticListStart < 0)
		{
			SpawnPopupMessage("This level has no free " + (mobile ? "critter" : "object") + " slot left. Delete something first.");
			return;
		}
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
		else if (LevArk.Game.Kind == UltimateEditor.Games.GameKind.UW2 && id >= 384 && !UltimateEditor.Games.Uw2Traps.HasUw1CreateMenu(id))
		{
			// UW2 traps and triggers whose fields differ from UW1's: created plain, then set
			// up in the properties panel, which describes the UW2 meaning
			StaticObject so = MapCreator.AddObject(tile, tilePos, level, id);
			afterCreatingAct?.Invoke(so);
		}
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
			int hpValue;
			mo.HP = int.TryParse(hp.text, out hpValue) ? Mathf.Clamp(hpValue, 0, 255) : MobileObject.GetDefaultHP(id);	// one byte
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
		// UW2 swaps the weapon enchantments' names (Games/EnchantmentEffects.cs)
		enchDrop.onValueChanged.AddListener(i =>
		{
			string real = UltimateEditor.Games.EnchantmentEffects.Describe(LevArk.Game.Kind, dict[enchDrop.options[i].text] + 256, StaticObject.GetObjectType(id) == ObjectType.Shield || StaticObject.GetObjectType(id) == ObjectType.Baubles);
			if (real != null) SpawnPopupMessage("In the game: " + real);
		});
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
		// UW2 swaps the weapon enchantments' names (Games/EnchantmentEffects.cs)
		enchDrop.onValueChanged.AddListener(i =>
		{
			string real = UltimateEditor.Games.EnchantmentEffects.Describe(LevArk.Game.Kind, dict[enchDrop.options[i].text] + 272, StaticObject.GetObjectType(id) == ObjectType.Shield || StaticObject.GetObjectType(id) == ObjectType.Baubles);
			if (real != null) SpawnPopupMessage("In the game: " + real);
		});
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
		// TRIGGER.C TRAP_DAMAGE, WORLDEV.C whack_thing: quality is the damage; a non-zero owner
		// poisons the player at strength quality instead (4-bit poison: 1-15), others still
		// take plain damage. It never heals, in either game.
		Slider quality = addSliderToCreateMenu("Damage (or poison strength)", mainPanelGO.transform, 0, 63.0f);
		Toggle poisons = addToggleToCreateMenu("Poisons the player (strength 1-15) instead of damaging", mainPanelGO.transform, false);
		Action createDamageTrapAct = () =>
		{
			StaticObject damTrap = MapCreator.AddObject(tile, tilePos, level, id);
			damTrap.SetProperties(false, true, true, false, 1, (int)quality.value, 0, 0);
			damTrap.SetDamageTrapPoisons(poisons.isOn);
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
			textTrap.SetProperties(false, true, true, false, 1, 0, 0, 0);
			textTrap.SetTextTrapIndex(index);
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
		Slider slider = addSliderToCreateMenu("Variable index", mainPanelGO.transform, 1.0f, StaticObject.MaxTrapVariable, 1.0f);
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
			int value;
			int.TryParse(input.text, out value);
			varTrap.XPos = 0;
			varTrap.Direction = dropdown.value;
			varTrap.SetProperties(false, true, true, false, 1, 0, 0, 0);
			varTrap.SetVarTrapValue(value);
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
		Slider varIndex = addSliderToCreateMenu("Variable index", mainPanelGO.transform, 1.0f, StaticObject.MaxTrapVariable, 1.0f);
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
			int value;
			int.TryParse(input.text, out value);
			if (checkType.isOn)
				checkTrap.XPos = 0;
			else
				checkTrap.XPos = 1;
			checkTrap.Direction = (int)varRange.value;
			checkTrap.SetProperties(false, true, true, false, 1, 0, 0, 0);
			checkTrap.SetVarTrapValue(value);
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
		NameLevelSlider(targetLevel, "Target level");
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
		addDescriptionToCreateMenu("Special trap 0x05: a crime against the chosen race (player_did_bad), which angers it.", mainPanelGO.transform);
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
		addDescriptionToCreateMenu("Special trap 0x03: the eight-position switch (eight_pos_switch), driven by the flags of the switch that set it off.", mainPanelGO.transform);
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
		addDescriptionToCreateMenu("Special trap 0x02: the crystal ball view (crystal_ball), from this position and direction.", mainPanelGO.transform);
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
		// change_terrain (event/WORLDEV.C): the new height is Z >> 3, 0 to 13
		Slider height = addSliderToCreateMenu("New floor height", mainPanelGO.transform, 0, StaticObject.MaxTerrainHeight, 0);
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
			terrainTrap.SetTerrainTrapHeight(useTrigger.isOn ? StaticObject.KeepTerrainHeight : (int)height.value);
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
}
