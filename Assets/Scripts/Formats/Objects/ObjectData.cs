// Moved from Data/StaticObject.cs (Unity-free, Formats assembly). The code is unchanged.
// OBJECTS.DAT and COMOBJ.DAT as the editor holds them; ObjectDataCodec reads and writes them.
using System.Collections.Generic;

public class ObjectData
{
	public int StartValue;
	public int CommonStart;
	public CommonData[] CommonData;
	public WeaponData[] WeaponData;
	public ProjectileData[] ProjectileData;
	public RangedData[] RangedData;
	public ArmourData[] ArmourData;
	public ContainerData[] ContainerData;
	public LightData[] LightData;
	public MonsterData[] MonsterData;
	public string[] MonsterSpriteNames;
	public int[] UnknownData;
}

public class CommonData
{
	public int Height;

	public int Radius;
	public int Type;
	public int Mass;

	public int Flag0;
	public int Flag1;
	public int Flag2;
	public int Flag3;
	public int Flag4;
	public int Pickable;
	public int Flag6;
	public int Container;

	public int Value;
	public int QualityClass;

	public int ObjectType;
	public int PickupFlag;
	public int UnkFlag1;
	public int Ownable;

	public int QualityType;
	public int LookDescription;

	public int[] RawData;
}

public class WeaponData
{
	public int ObjectID;

	public int Slash;
	public int Bash;
	public int Stab;

	public int Unk1;
	public int Unk2;
	public int Unk3;

	public int Skill;
	public int Durability;

	public static Dictionary<string, int> GetSkills()
	{
		Dictionary<string, int> dict = new Dictionary<string, int>();
		dict[GetSkill(3)] = 3;
		dict[GetSkill(4)] = 4;
		dict[GetSkill(5)] = 5;
		dict[GetSkill(6)] = 6;
		return dict;
	}
	public static string GetSkill(int val)
	{
		switch (val)
		{
			case 3:
				return "Sword";
			case 4:
				return "Axe";
			case 5:
				return "Mace";
			case 6:
				return "Unarmed";
			default:
				return "INVALID";
		}
	}
}

public class ArmourData
{
	public int Protection;
	public int Durability;
	public int Unk1;
	public int Type;

	public static Dictionary<string, int> GetArmourTypes()
	{
		Dictionary<string, int> dict = new Dictionary<string, int>();
		dict[GetArmourType(0)] = 0;
		dict[GetArmourType(1)] = 1;
		dict[GetArmourType(3)] = 3;
		dict[GetArmourType(4)] = 4;
		dict[GetArmourType(5)] = 5;
		dict[GetArmourType(8)] = 8;
		dict[GetArmourType(9)] = 9;
		return dict;
	}
	public static string GetArmourType(int val)
	{
		switch (val)
		{
			case 0:
				return "Shield";
			case 1:
				return "Torso";
			case 3:
				return "Legs";
			case 4:
				return "Gloves";
			case 5:
				return "Boots";
			case 8:
				return "Head";
			case 9:
				return "Rings";
			default:
				return "INVALID";
		}
	}
}

public class ContainerData
{
	public int Capacity;
	public int Type;
	public int Slots;

	public static Dictionary<string, int> GetContainerTypes()
	{
		Dictionary<string, int> dict = new Dictionary<string, int>();
		dict[GetContainerType(0)] = 0;
		dict[GetContainerType(1)] = 1;
		dict[GetContainerType(2)] = 2;
		dict[GetContainerType(3)] = 3;
		dict[GetContainerType(255)] = 255;
		return dict;
	}
	public static string GetContainerType(int val)
	{
		switch (val)
		{
			case 0:
				return "Runes";
			case 1:
				return "Projectiles";
			case 2:
				return "Scrolls";
			case 3:
				return "Food";
			case 255:
				return "Everything";
			default:
				return "INVALID";
		}
	}
}

public class ProjectileData
{
	//OBJECTS.DAT struct MissileInfo (include/object.h in underworld-exhumed)
	public int Damage;
	public int Unk1;    //byte 1: missile type
	public int Unk2;    //byte 2: signed; for ammunition, minus its damage type (0x40 missile, 0x0B magic + fire...)
}

public class RangedData
{
	//OBJECTS.DAT struct MissileInfo for launchers (items 0x18-0x1F)
	public int Unk1;    //byte 0: damage
	public int Type;    //byte 1: missile type
	public int Ammo;    //byte 2: ammunition item - 0x10 (0 sling stone, 1 bolt, 2 arrow)
}

public class LightData
{
	public int Duration;
	public int Brightness;
}

public class MonsterData
{
	public int Level;
	public int Unk0_1;
	public int Unk0_2;
	public int Unk0_3;
	public int Health;
	public int Attack;
	public int Unk1;
	public int Remains;
	public int HitDecal;
	public int OwnerType;
	public int Passiveness;
	public int Unk2;
	public int Speed;
	public int Unk3;
	public int Poison;
	public int MonsterType;
	public int EquipmentDamage;
	public int Unk4;
	public int Attack1Value;
	public int Attack1Damage;
	public int Attack1Chance;
	public int Attack2Value;
	public int Attack2Damage;
	public int Attack2Chance;
	public int Attack3Value;
	public int Attack3Damage;
	public int Attack3Chance;

	public long Inv_Unk1;
	public int[] Inventory;
	public int[] InventoryInfo;
	public int Inv_Unk2;

	public int Experience;
	public int Unk5;
	public int Unk6;
	public int Unk7;
	public int Unk8;
	public int Unk9;
	public int Unk10;

	public int SpriteID;
	public int AuxPalette;

	public static Dictionary<string, int> GetHitDecals()
	{
		Dictionary<string, int> dict = new Dictionary<string, int>();
		dict[GetHitDecal(0)] = 0;
		dict[GetHitDecal(8)] = 8;
		return dict;
	}
	public static Dictionary<string, int> GetRemains()
	{
		Dictionary<string, int> dict = new Dictionary<string, int>();
		dict[GetRemains(0)] = 0;
		dict[GetRemains(2)] = 2;
		dict[GetRemains(4)] = 4;
		dict[GetRemains(6)] = 6;
		dict[GetRemains(8)] = 8;
		dict[GetRemains(10)] = 10;
		dict[GetRemains(12)] = 12;
		dict[GetRemains(14)] = 14;
		return dict;
	}
	public static Dictionary<string, int> GetMonsterTypes()
	{
		Dictionary<string, int> dict = new Dictionary<string, int>();
		dict[GetMonsterType(0)] = 0;
		dict[GetMonsterType(1)] = 1;
		dict[GetMonsterType(2)] = 2;
		dict[GetMonsterType(3)] = 3;
		dict[GetMonsterType(4)] = 4;
		dict[GetMonsterType(5)] = 5;
		dict[GetMonsterType(17)] = 17;
		dict[GetMonsterType(81)] = 81;
		return dict;
	}
	public static string GetHitDecal(int val)
	{
		switch (val)
		{
			case 0:
				return "Damage";
			case 8:
				return "Blood";
			default:
				return "INVALID";
		}
	}
	
	public static string GetRemains(int val)
	{
		switch (val)
		{
			case 0:
				return "None";
			case 2:
				return "Rotworm (217)";
			case 4:
				return "Rubble (218)";
			case 6:
				return "Wood chips (219)";
			case 8:
				return "Bones (220)";
			case 10:
				return "Green blood (221)";
			case 12:
				return "Red blood (222)";
			case 14:
				return "Red blood (223)";
			default:
				return "INVALID";
		}
	}
	public static string GetMonsterType(int val)
	{
		switch (val)
		{
			case 0:
				return "Ethereal";
			case 1:
				return "Humanoid";
			case 2:
				return "Flyer";
			case 3:
				return "Swimmer";
			case 4:
				return "Critter";
			case 5:
				return "Crawler";
			case 17:
				return "Earth";
			case 81:
				return "Human";
			default:
				return "INVALID";
		}
	}
}
