using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Linq;

/// <summary>UIManager: object editors (split from UIManager.cs by region, review package 10).</summary>
public partial class UIManager
{
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
			UltimateEditor.Uw1.UI.FieldTooltips.Attach(weaponProps.transform, UltimateEditor.Uw1.UI.FieldTooltips.Kind.Weapon);
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
		UltimateEditor.Uw1.UI.FieldTooltips.Attach(monsterProps.transform, UltimateEditor.Uw1.UI.FieldTooltips.Kind.Creature);
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
		UltimateEditor.Uw1.UI.FieldTooltips.Attach(weaponProps.transform, UltimateEditor.Uw1.UI.FieldTooltips.Kind.Weapon);
	}
	private void addProjectileEditor(int id, Transform parent)
	{
		GameObject projProps = Instantiate(ProjectileEditorPrefab, parent);
		ProjectileData pd = MapCreator.ObjectData.ProjectileData[id - 16];
		setInputProperty(projProps.transform, "Damage", pd.Damage, (i) => pd.Damage = i, 0, 255);
		setInputProperty(projProps.transform, "Unk1", pd.Unk1, (i) => pd.Unk1 = i, 0, 255);
		setInputProperty(projProps.transform, "Unk2", pd.Unk2, (i) => pd.Unk2 = i, 0, 255);
		UltimateEditor.Uw1.UI.FieldTooltips.Attach(projProps.transform, UltimateEditor.Uw1.UI.FieldTooltips.Kind.Missile);
	}
	private void addRangedEditor(int id, Transform parent)
	{
		GameObject rangedProps = Instantiate(RangedEditorPrefab, parent);
		RangedData rd = MapCreator.ObjectData.RangedData[id - 24];
		setInputProperty(rangedProps.transform, "Ammo", rd.Ammo, (i) => rd.Ammo = i, 0, 255);
		setInputProperty(rangedProps.transform, "Unk1", rd.Unk1, (i) => rd.Unk1 = i, 0, 255);
		UltimateEditor.Uw1.UI.FieldTooltips.Attach(rangedProps.transform, UltimateEditor.Uw1.UI.FieldTooltips.Kind.Launcher);
	}
	private void addArmourEditor(int id, Transform parent)
	{
		GameObject armorProps = Instantiate(ArmourEditorPrefab, parent);
		ArmourData ad = MapCreator.ObjectData.ArmourData[id - 32];
		setInputProperty(armorProps.transform, "Protection", ad.Protection, (i) => ad.Protection = i, 0, 255);
		setInputProperty(armorProps.transform, "Durability", ad.Durability, (i) => ad.Durability = i, 0, 255);
		setInputProperty(armorProps.transform, "Unk1", ad.Unk1, (i) => ad.Unk1 = i, 0, 255);
		setDropdownProperty(armorProps.transform, "Type", ad.Type, ArmourData.GetArmourTypes(), (i) => ad.Type = i);
		UltimateEditor.Uw1.UI.FieldTooltips.Attach(armorProps.transform, UltimateEditor.Uw1.UI.FieldTooltips.Kind.Armour);
	}
	private void addContainerEditor(int id, Transform parent)
	{
		GameObject contProps = Instantiate(ContainerEditorPrefab, parent);
		ContainerData cd = MapCreator.ObjectData.ContainerData[id - 128];
		setInputProperty(contProps.transform, "Capacity", cd.Capacity, (i) => cd.Capacity = i, 0, 255);
		setInputProperty(contProps.transform, "Slots", cd.Slots, (i) => cd.Slots = i, 0, 255);
		// Type and Slots are the low and high bytes of one int16 (include/object.h struct
		// Container): -1 for anything, 0x200 + kind for a kind. Keep them consistent.
		setDropdownProperty(contProps.transform, "Type", cd.Type, ContainerData.GetContainerTypes(), (i) => { cd.Type = i; cd.Slots = i == 255 ? 255 : 2; });
		UltimateEditor.Uw1.UI.FieldTooltips.Attach(contProps.transform, UltimateEditor.Uw1.UI.FieldTooltips.Kind.Container);
	}
	private void addLightEditor(int id, Transform parent)
	{
		GameObject lightProps = Instantiate(LightEditorPrefab, parent);
		LightData ld = MapCreator.ObjectData.LightData[id - 144];
		setInputProperty(lightProps.transform, "Brightness", ld.Brightness, (i) => ld.Brightness = i);
		setInputProperty(lightProps.transform, "Duration", ld.Duration, (i) => ld.Duration = i);
		UltimateEditor.Uw1.UI.FieldTooltips.Attach(lightProps.transform, UltimateEditor.Uw1.UI.FieldTooltips.Kind.Light);
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
		UltimateEditor.Uw1.UI.FieldTooltips.Attach(monsterProps.transform, UltimateEditor.Uw1.UI.FieldTooltips.Kind.Creature);
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
}
