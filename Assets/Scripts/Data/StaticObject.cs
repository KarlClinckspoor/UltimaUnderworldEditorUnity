using System;
using System.Collections.Generic;
using UnityEngine;

public enum ObjectType
{
	Null,
	Weapon,
	Melee,
	Ranged,
	SpecialRanged,
	Projectile,
	SpecialProjectile,
	Armour,
	Baubles,
	SpecialBauble,
	Shield,
	SpecialShield,
	Monster,
	Container,
	Light,
	Wand,
	Gold,
	Treasure,
	Food,
	Drink,
	Potion,
	Grass,
	Bones,
	Runestone,
	Key,
	Lock,
	DoorTrap,
	Book,
	Spell,
	Scroll,
	Map,
	Door,
	DialLever,
	Model,
	ContainerModel,
	Grave,
	VerticalTexture,
	MapObject,
	Lever,
	Writing,
	Trap,
	Trigger,
	Animated,
	Effect,
	Unpickable,
	Uncountable,
	Countable,
	CeilingHugger,
	Unknown
}

public class StaticObject {

	public int CurrentAdress;
	public int CurrentLevel;
	public string Name;

	public int ObjectID { get; protected set; }
	public int Flags;
	//If true, object has an enchantment (stored in QLS field)
	public bool IsEnchanted;

	public bool IsDoorOpen;
	public bool IsInvisible;
	//If true, QLS field is number of objects 
	public bool IsQuantity;

	//Position in tile
	public int XPos;
	public int YPos;
	public int ZPos;
	public int Direction;

	//For mobile objects - Quality = X of map tile
	public int Quality;
	//Next object in the same map tile
	public int NextAdress;
	public int PrevAdress;

	//For mobile objects - Quality = Y of map tile
	//for items - if is owned by mob, from 0 to 28 set the type of creature that item is owned by
	public int Owner;
	//If IsQuantity is false, this links to the "inventory" of this item
	//If IsQuantity is true & IsEnchanted is true this store enchantment type
	public int Special;

	public AnimationOverlay Animation;

	//public int FreeObjectStatus;

	public MapTile Tile;
	public Vector2Int MapPosition;

	public Action<StaticObject> OnRemove;

	public StaticObject(){}
	public StaticObject(MapTile tile) : this(tile, 0, 3, 3) { }
	public StaticObject(MapTile tile, int id, int x, int y) 
	{
		SetID(id);

		XPos = x;
		YPos = y;

		ZPos = tile.FloorHeight * 8;
		Tile = tile;
		MapPosition = tile.Position;
		Name = GetName(ObjectID);
		CurrentLevel = tile.Level;

		SetDefaults(ObjectID);
	}
	public StaticObject(MapTile tile, int x, int y, StaticObject copy) : this(copy)
	{
		XPos = x;
		YPos = y;

		Tile = tile;
		MapPosition = tile.Position;
		ZPos = tile.FloorHeight * 8;
		CurrentLevel = tile.Level;
	}

	public StaticObject(StaticObject copy)
	{
		//CurrentAdress = copy.CurrentAdress;
		//CurrentLevel = copy.CurrentLevel;
		Name = copy.Name;
		SetID(copy.ObjectID);
		Flags = copy.Flags;
		IsEnchanted = copy.IsEnchanted;
		IsDoorOpen = copy.IsDoorOpen;
		IsInvisible = copy.IsInvisible;
		IsQuantity = copy.IsQuantity;
		XPos = copy.XPos;
		YPos = copy.YPos;
		ZPos = copy.ZPos;
		Direction = copy.Direction;
		Quality = copy.Quality;
		//NextAdress = copy.NextAdress;
		//PrevAdress = copy.PrevAdress;
		Owner = copy.Owner;
		if(IsQuantity)
			Special = copy.Special;
		//FreeObjectStatus = copy.FreeObjectStatus;
		MapPosition = copy.MapPosition;
		Tile = copy.Tile;
	}

	public StaticObject(SavedStatic so)
	{
		CurrentAdress = so.CurrentAdress;
		CurrentLevel = so.CurrentLevel;
		Name = so.Name;
		ObjectID = so.ObjectID;
		Flags = so.Flags;
		IsEnchanted = so.IsEnchanted;
		IsDoorOpen = so.IsDoorOpen;
		IsInvisible = so.IsInvisible;
		IsQuantity = so.IsQuantity;
		XPos = so.XPos;
		YPos = so.YPos;
		ZPos = so.ZPos;
		Direction = so.Direction;
		Quality = so.Quality;
		NextAdress = so.NextAdress;
		PrevAdress = so.PrevAdress;
		Owner = so.Owner;
		Special = so.Special;
	}

	public void SetDefaults(int id)
	{
		if(GetObjectType(id) == ObjectType.Container)
		{
			Quality = 40;
			IsQuantity = false;
			Special = 0;
		}
		else if(GetObjectType(id) == ObjectType.Food)
		{
			Quality = 40;
			IsQuantity = true;
			Special = 1;
		}
		else
		{
			Quality = 40;
			IsQuantity = true;
			Special = 1;
		}
	}

	public void SetID(int id)
	{
		ObjectID = id;
		if (id == 400)
			OnRemove = (so) => MapCreator.StringData.ClearTextTrapString(so);
		else if (IsWriting())
			OnRemove = (so) => MapCreator.StringData.ClearWritingSlot(so);
		else if (IsGrave())
			OnRemove = (so) => MapCreator.StringData.ClearGraveSlot(so);
		else if (IsBook())
			OnRemove = (so) => MapCreator.StringData.ClearScrollSlot(so);
		else if (IsScroll())
			OnRemove = (so) => MapCreator.StringData.ClearScrollSlot(so);
			
	}

	public virtual void OnMoved(MapTile newTile)
	{
		if(Animation)
		{
			Animation.X = newTile.Position.x;
			Animation.Y = newTile.Position.y;
		}
	}

	public void SetProperties(bool enchanted, bool invisible, bool door, bool quant, int flag, int qual, int own, int spec)
	{
		IsEnchanted = enchanted;
		IsInvisible = invisible;
		IsDoorOpen = door;
		IsQuantity = quant;
		Flags = flag;
		Quality = qual;
		Owner = own;
		Special = spec;
	}
	public void SetProperties(StaticObject other)
	{
		IsEnchanted = other.IsEnchanted;
		IsInvisible = other.IsInvisible;
		IsDoorOpen = other.IsDoorOpen;
		IsQuantity = other.IsQuantity;
		Flags = other.Flags;
		Quality = other.Quality;
		Owner = other.Owner;
		Special = other.Special;
	}
	public void SetFullProperties(StaticObject other)
	{
		Direction = other.Direction;
		XPos = other.XPos;
		YPos = other.YPos;
		ZPos = other.ZPos;
		SetProperties(other);
	}

	public virtual string GetFullName()
	{
		string name = Name;
		if(IsEnchanted && IsItem())
			name += " of " + GetEnchantmentName();
		else if(IsWand())
		{
			StaticObject spell = GetContainedObject();
			if (spell)
				name += " of " + spell.GetEnchantmentName();
		}
		return name;
	}

	public static string GetMonsterName(int monsterId)
	{
		return GetName(64 + monsterId);
	}

	public bool IsItem() => ObjectID < 320 && !IsMonster();
	public bool IsMonster() => IsMonster(ObjectID);
	public static bool IsMonster(int id) => GetObjectType(id) == ObjectType.Monster;
	public bool IsDoor() => GetObjectType(ObjectID) == ObjectType.Door;
	public bool IsDoorTrap() => GetObjectType(ObjectID) == ObjectType.DoorTrap;
	public bool IsContainer() => GetObjectType(ObjectID) == ObjectType.Container || GetObjectType(ObjectID) == ObjectType.ContainerModel;
	public static bool IsContainer(int id) => GetObjectType(id) == ObjectType.Container;
	public bool IsBones() => GetObjectType(ObjectID) == ObjectType.Bones;
	public static bool IsWeapon(int id) => GetObjectType(id) == ObjectType.Weapon || GetObjectType(id) == ObjectType.Melee || GetObjectType(id) == ObjectType.Ranged;
	public bool IsWeapon() => GetObjectType(ObjectID) == ObjectType.Weapon || GetObjectType(ObjectID) == ObjectType.Melee || GetObjectType(ObjectID) == ObjectType.Ranged;
	public static bool IsMelee(int id) => GetObjectType(id) == ObjectType.Melee;
	public static bool IsRanged(int id) => GetObjectType(id) == ObjectType.Ranged;
	public static bool IsSpecialRanged(int id) => GetObjectType(id) == ObjectType.SpecialRanged;
	public bool IsArmour() => GetObjectType(ObjectID) == ObjectType.Armour;
	public static bool IsArmour(int id) => GetObjectType(id) == ObjectType.Armour;
	public bool IsBauble() => GetObjectType(ObjectID) == ObjectType.Baubles;
	public static bool IsBauble(int id) => GetObjectType(id) == ObjectType.Baubles;
	public static bool IsSpecialBauble(int id) => GetObjectType(id) == ObjectType.SpecialBauble;
	public bool IsPotion() => GetObjectType(ObjectID) == ObjectType.Potion;
	public bool IsScroll() => GetObjectType(ObjectID) == ObjectType.Scroll;
	public bool IsBook() => GetObjectType(ObjectID) == ObjectType.Book;
	public bool IsWand() => GetObjectType(ObjectID) == ObjectType.Wand;
	public bool IsGold() => GetObjectType(ObjectID) == ObjectType.Gold;
	public bool IsTreasure() => GetObjectType(ObjectID) == ObjectType.Treasure;
	public bool IsProjectile() => GetObjectType(ObjectID) == ObjectType.Projectile;
	public static bool IsProjectile(int id) => GetObjectType(id) == ObjectType.Projectile;
	public static bool IsSpecialProjectile(int id) => GetObjectType(id) == ObjectType.SpecialProjectile;
	public bool IsShield() => GetObjectType(ObjectID) == ObjectType.Shield;
	public static bool IsShield(int id) => GetObjectType(id) == ObjectType.Shield;
	public bool IsLight() => GetObjectType(ObjectID) == ObjectType.Light;
	public static bool IsLight(int id) => GetObjectType(id) == ObjectType.Light;
	public bool IsFood() => GetObjectType(ObjectID) == ObjectType.Food;
	public bool IsDrink() => GetObjectType(ObjectID) == ObjectType.Drink;
	public bool IsRune() => GetObjectType(ObjectID) == ObjectType.Runestone;
	public bool IsKey() => GetObjectType(ObjectID) == ObjectType.Key;
	public bool IsLock() => GetObjectType(ObjectID) == ObjectType.Lock;
	public bool IsGrave() => GetObjectType(ObjectID) == ObjectType.Grave;
	public bool IsWriting() => GetObjectType(ObjectID) == ObjectType.Writing;
	public bool IsVerticalTexture() => GetObjectType(ObjectID) == ObjectType.VerticalTexture;
	public bool IsLever() => GetObjectType(ObjectID) == ObjectType.Lever;
	public bool IsDialLever() => GetObjectType(ObjectID) == ObjectType.DialLever;
	public bool IsTrap() => GetObjectType(ObjectID) == ObjectType.Trap;
	public bool IsUse() => ObjectID == 418;
	public bool IsTrigger() => GetObjectType(ObjectID) == ObjectType.Trigger;
	public bool IsModel() => GetObjectType(ObjectID) == ObjectType.Model || GetObjectType(ObjectID) == ObjectType.ContainerModel;
	public bool IsSpell() => GetObjectType(ObjectID) == ObjectType.Spell;
	public bool IsCountable() => GetObjectType(ObjectID) == ObjectType.Countable;
	public bool IsUncountable() => GetObjectType(ObjectID) == ObjectType.Uncountable;
	public bool IsCeilingHugger() => GetObjectType(ObjectID) == ObjectType.CeilingHugger;
	public bool IsAnimated() => GetObjectType(ObjectID) == ObjectType.Animated;

	[Obsolete("Use GetByID")]
	public static List<int> GetTraps()
	{
		List<int> ids = new List<int>();
		for (int i = 384; i < 401; i++)
			ids.Add(i);
		return ids;
	}

	public static List<int> GetTriggers()
	{
		List<int> ids = new List<int>();
		for (int i = 417; i < 423; i++)
		{
			if(GetObjectType(i) == ObjectType.Trigger)
				ids.Add(i);
		}
		return ids;
	}

	public static List<int> GetIDsByType(params ObjectType[] types)
	{
		List<int> ids = new List<int>();
		for (int i = 0; i < 470; i++)
		{
			for (int j = 0; j < types.Length; j++)
			{
				if (GetObjectType(i) == types[j])
					ids.Add(i);
			}
		}
		return ids;
	}
	public static List<int> GetIDs(int last)
	{
		List<int> ids = new List<int>();
		for (int i = 0; i < last; i++)
			ids.Add(i);		
		return ids;
	}
	public static List<int> GetShootable()
	{
		List<int> ids = new List<int>();
		for (int i = 0; i < 460; i++)
		{
			if (i < 320 && !IsMonster(i))
				ids.Add(i);
			else if (GetObjectType(i) == ObjectType.Model)
				ids.Add(i);
		}
		return ids;
	}

	public static ObjectType[] GetTrapValidChildren(int trapID)
	{
		
		switch (trapID)
		{
			case 384: return new ObjectType[] { ObjectType.Trigger, ObjectType.Trap };	//Damage
			case 385: return new ObjectType[] { ObjectType.Trigger, ObjectType.Trap };	//Teleport
			case 386: return new ObjectType[] { ObjectType.Trigger, ObjectType.Trap };	//Arrow
			case 387: return new ObjectType[] { ObjectType.Trigger, ObjectType.Trap };	//Do
			case 388: return new ObjectType[] { ObjectType.Trigger, ObjectType.Trap };	//Pit
			case 389: return new ObjectType[] { ObjectType.Trigger, ObjectType.Trap };	//Change terrain
			case 390: return new ObjectType[] { ObjectType.Trigger, ObjectType.Trap };	//Spelltrap
			case 391: return new ObjectType[] { ObjectType.Trigger, ObjectType.Trap };	//Create object
			case 392: return new ObjectType[] { ObjectType.Trigger, ObjectType.Trap };	//Door
			case 393: return new ObjectType[] { ObjectType.Trigger, ObjectType.Trap };	//Ward
			case 394: return new ObjectType[] { ObjectType.Trigger, ObjectType.Trap };	//Tell
			case 395: return new ObjectType[] { ObjectType.Trigger, ObjectType.Trap };	//Delete object
			case 396: return new ObjectType[] { ObjectType.Trigger, ObjectType.Trap };	//Inventory
			case 397: return new ObjectType[] { ObjectType.Trigger, ObjectType.Trap };	//Var
			case 398: return new ObjectType[] { ObjectType.Trigger };	//Check
			case 399: return new ObjectType[] { ObjectType.Trigger, ObjectType.Trap };	//Combination
			case 400: return new ObjectType[] { ObjectType.Trigger, ObjectType.Trap };  //Text

			case 416: return new ObjectType[] { ObjectType.Trigger, ObjectType.Trap };	//Move
			case 417: return new ObjectType[] { ObjectType.Trigger, ObjectType.Trap };	//Pick up
			case 418: return new ObjectType[] { ObjectType.Trigger, ObjectType.Trap };	//Use
			case 419: return new ObjectType[] { ObjectType.Trigger, ObjectType.Trap };	//Look
			case 420: return new ObjectType[] { ObjectType.Trigger, ObjectType.Trap };	//Step on
			case 421: return new ObjectType[] { ObjectType.Trigger, ObjectType.Trap };	//Open
			case 422: return new ObjectType[] { ObjectType.Trigger, ObjectType.Trap };	//Unlock
			default:
				return null;
		}
	}

	public bool CanBeInserted(StaticObject other)
	{
		if (IsContainer() && other.IsItem())
			return true;
		if (IsMonster() && other.IsItem())
			return true;
		if (IsWand() && other.IsSpell())
			return true;
		if (IsDoor() && (other.IsLock() || other.IsTrap()))
			return true;
		if (IsLever() && other.IsTrigger())
			return true;
		if (IsTrap() && (other.IsItem() || other.IsMonster() || other.IsLock() || other.IsTrigger()))
			return true;
		if (IsPotion() && !IsQuantity && other.IsTrap())
			return true;
		return false;
	}

	public int GetEnchantment()
	{
		int e = Special - 512;
		if(IsWeapon())
		{
			if (e >= 192)
				return e + 256;
		}
		else if(IsArmour() || IsBauble() || IsShield())
		{
			if (e >= 192 && e < 208)
				return e + 272;
			//if (e >= 208)
			//	return e + 178;
		}
		else if(IsPotion() || IsScroll() || IsSpell() || IsDrink())
		{
			if (e < 53) //spell effects
				return e + 256;
			else if (e >= 67 && e < 70)	//Maze, frog, halluc
				return e + 144;
			else
				return -1;
		}
		else if (IsWand())
		{
			return e;
		}

		return Special;
	}

	public static Dictionary<string, int> GetPotionScrollEnchants()
	{
		Dictionary<string, int> dict = new Dictionary<string, int>();
		for (int i = 256; i < 309; i++)
			dict[GetEnchantmentName(i)] = i - 256;
		for (int i = 211; i < 214; i++)
			dict[GetEnchantmentName(i)] = i - 144;
		return dict;
	}
	public static Dictionary<string, int> GetWearableEnchants()
	{
		Dictionary<string, int> dict = new Dictionary<string, int>();
		for (int i = 0; i < 8; i++)
			dict[GetEnchantmentName(i)] = i;
		for (int i = 17; i < 22; i++)
			dict[GetEnchantmentName(i)] = i;
		dict[GetEnchantmentName(34)] = 34;
		dict[GetEnchantmentName(35)] = 35;
		dict[GetEnchantmentName(37)] = 37;
		for (int i = 49; i < 58; i++)
			dict[GetEnchantmentName(i)] = i;
		dict[GetEnchantmentName(190)] = 190;
		dict[GetEnchantmentName(191)] = 191;
		for (int i = 464; i < 480; i++)
			dict[GetEnchantmentName(i)] = i - 240;
		return dict;
	}
	public static Dictionary<string, int> GetWeaponEnchants()
	{
		Dictionary<string, int> dict = new Dictionary<string, int>();
		for (int i = 448; i < 464; i++)
			dict[GetEnchantmentName(i)] = i - 256;
		return dict;
	}
	public static Dictionary<string, int> GetFountainEnchants()
	{
		Dictionary<string, int> dict = new Dictionary<string, int>();
		for (int i = 64; i < 80; i++)
			dict[GetEnchantmentName(i)] = i;
		return dict;
	}
	public static string[] GetMonsters(bool firstNone = true)
	{
		string[] monsters = new string[64];
		for (int i = 0; i < 64; i++)
		{
			if (i == 0)
				monsters[0] = firstNone ? "none" : GetMonsterName(0);
			else
				monsters[i] = GetMonsterName(i);
		}
		return monsters;
	}

	public StaticObject GetNextObject()
	{
		if (NextAdress == 0)
			return null;

		return MapCreator.LevelData[CurrentLevel - 1].Objects[NextAdress];
	}

	public StaticObject GetPrevObject()
	{
		if (PrevAdress == 0)
			return null;

		return MapCreator.LevelData[CurrentLevel - 1].Objects[PrevAdress];
	}

	public StaticObject GetContainedObject()
	{
		if (Special == 0)
			return null;

		return MapCreator.LevelData[CurrentLevel - 1].Objects[Special];
	}

	public List<StaticObject> GetContainedObjects()
	{
		List<StaticObject> contained = new List<StaticObject>();

		StaticObject next = GetContainedObject();
		StaticObject current = null;

		int safe = 40;
		while(next)
		{
			current = next;
			contained.Add(current);
			next = next.GetNextObject();

			safe--;
			if (safe == 0)
			{
				Debug.LogErrorFormat("Failed Loop [GetContainedObjects] {0}", Name);
				return null;
			}
		}
		return contained;
	}

	public StaticObject GetContainer()
	{
		StaticObject prev = GetPrevObject();
		//StaticObject current = null;
		StaticObject current = this;
		int safe = 80;
		while (prev)
		{
			if (prev.Special == current.CurrentAdress)
				return prev;
			current = prev;
			//if (current.IsContainer() && current.GetFromInventory(this))
			//	break;
			prev = prev.GetPrevObject();

			safe--;
			if (safe == 0)
			{
				Debug.LogErrorFormat("Failed Loop [GetContainer] {0}, prev : {1}", Name, prev.Name);
				return null;
			}
		}
		return null;
		//if (current)
		//	Debug.LogFormat("This {0} container : {1}", Name, current.Name);
		//else
		//	Debug.LogFormat("This {0} has no container", Name);
		//if (current.IsContainer())
		//	return current;
		//else
		//	return null;
	}

	public StaticObject GetFirstContainer()
	{
		StaticObject cont = GetContainer();
		StaticObject current = this;
		while(cont)
		{
			current = cont;
			cont = cont.GetContainer();
		}
		return current;
	}

	public StaticObject GetLastContainedObject()
	{
		StaticObject next = GetContainedObject();
		StaticObject current = null;
		int safe = 90;
		while(next)
		{
			current = next;
			next = next.GetNextObject();

			safe--;
			if(safe == 0)
			{
				Debug.LogErrorFormat("Failed Loop [GetLastContainedObject] {0}", Name);
				return null;
			}
		}
		return current;
	}
	public int GetContainedObjectsCount()
	{
		StaticObject next = GetContainedObject();
		StaticObject current = null;
		int safe = 90;
		int count = 0;
		while (next)
		{
			current = next;
			next = next.GetNextObject();
			count++;
			safe--;
			if (safe == 0)
			{
				Debug.LogErrorFormat("Failed Loop [GetContainedObjectsCount] {0}", Name);
				return -1;
			}
		}
		return count;
	}
	public StaticObject GetUseTrigger()
	{
		List<StaticObject> objects = GetContainedObjects();
		foreach (var so in objects)
		{
			if (so.IsUse())
				return so;
		}
		return null;
	}
	public StaticObject GetLock()
	{
		List<StaticObject> objects = GetContainedObjects();
		foreach (var so in objects)
		{
			if (so.IsLock())
				return so;
		}
		return null;
	}
	
	public List<StaticObject> GetDoorTraps()
	{
		List<StaticObject> objects = GetContainedObjects();
		List<StaticObject> traps = new List<StaticObject>();
		foreach (var so in objects)
		{
			if (so.ObjectID == 392)
				traps.Add(so);
		}
		return traps;
	}

	public StaticObject GetFromInventory(StaticObject searched)
	{
		StaticObject next = GetContainedObject();

		while (next)
		{
			if (next == searched)
				return searched;
			next = next.GetContainedObject();
		}
		return null;
	}

	public bool AddToContainer(StaticObject so)
	{
		if (IsQuantity)
			return false;
		StaticObject last = GetLastContainedObject();
		if(last == null)
		{
			Special = so.CurrentAdress;
			so.PrevAdress = CurrentAdress;
		}
		else
		{
			last.NextAdress = so.CurrentAdress;
			so.PrevAdress = last.CurrentAdress;
		}
		if (so is MobileObject)
			MapCreator.DeactivateMob((MobileObject)so, so.CurrentLevel);

		return true;
	}

	public bool RemoveFromContainer(StaticObject so)
	{
		StaticObject next = GetContainedObject();
		StaticObject before = this;
		StaticObject current = null;

		while (next)
		{
			current = next;
			next = next.GetNextObject();
			//Debug.LogFormat("Before : {0}, Current : {1}, Next : {2}", before == null ? "null" : before.Name, current == null ? "null" : current.Name, next == null ? "null" : next.Name);
			if (so == current)
			{
				if(before == this)	//Item preceeding item to remove is a container
				{
					if (next == null)
						Special = 0;
					else
					{
						Special = next.CurrentAdress;
						next.PrevAdress = CurrentAdress;
					}
				}
				else
				{
					if (next == null)
						before.NextAdress = 0;
					else
					{
						before.NextAdress = next.CurrentAdress;
						next.PrevAdress = before.CurrentAdress;
					}
				}
				StaticObject lastOnTile = Tile.GetLastObject();
				if (lastOnTile)
					so.PrevAdress = lastOnTile.CurrentAdress;
				else
					so.PrevAdress = 0;
				so.NextAdress = 0;
				if (so is MobileObject)
					MapCreator.ActivateMob((MobileObject)so, so.CurrentLevel);
				return true;
			}
			before = current;
		}
		return false;
	}
	//FIXME
	public Texture2D GetDoorTexture()
	{
		if (!IsDoor())
			return null;

		bool isDoorOpen = ObjectID >= 328 ? true : false;
		int index = ObjectID - (isDoorOpen? 328 : 320);
		if (index == 6)
			return Resources.Load<Texture2D>("port");
		else if (index == 7)
			return Resources.Load<Texture2D>("secr");

		return MapCreator.GetDoorTextureFromIndex(index, CurrentLevel);
	}

	//public string GetQuantityLinkSpecial()
	//{
	//	return Special.ToString();
	//}

	public void SetVarTrapValue(int value)
	{
		YPos = value & 0x07;
		Owner = (value & 0xF8) >> 3;
		Quality = (value & 0x3F00) >> 8;
	}
	public int GetVarTrapValue()
	{
		return ((Quality & 0x3f) << 8) + (((Owner & 0x1F) << 3) + (YPos & 0x07));
	}
	public void SetArrowTrapProjectile(int value)
	{
		Owner = value & 0x1F;
		Quality = (value & 0x1E0) >> 5;
	}
	public int GetArrowTrapProjectile()
	{
		return ((Quality & 0x0F) << 5) + Owner;
	}
	public void SetSpellTrapSpell(int value)
	{
		Quality = (value & 0xF0) >> 4;
		Owner = value & 0x0F;
	}
	public int GetSpellTrapSpell()
	{
		int qual = ((Quality & 0x0F) << 4);
		return qual + (Owner & 0x0F);
	}
	public void SetInventoryTrapItem(int value)
	{
		Quality = (value & 0x1E0) >> 5;
		Owner = value & 0x1F;
	}
	public int GetInventoryTrapItem()
	{
		int qual = ((Quality & 0x0F) << 5);
		return qual + (Owner & 0x1F);
	}
	public float GetMovementStep()
	{
		return 7.0f;
	}

	public float GetMovementOffset()
	{
		return 0;
	}

	public string GetPositionString()
	{
		return "[X: " + MapPosition.x + ", Y: " + MapPosition.y + "]";
	}

	public string GetName()
	{
		return GetName(ObjectID);
	}

	public static string GetName(int id)
	{
		switch (id)
		{
			case 0: return "Hand axe";
			case 1: return "Battle axe";
			case 2: return "Axe";
			case 3: return "Dagger";
			case 4: return "Shortsword";
			case 5: return "Longsword";
			case 6: return "Broadsword";
			case 7: return "Cudgel";
			case 8: return "Light mace";
			case 9: return "Mace";
			case 10: return "Shiny sword";
			case 11: return "Jeweled axe";
			case 12: return "Black sword";
			case 13: return "Jeweled sword";
			case 14: return "Jeweled mace";
			case 15: return "Fist";
			case 16: return "Sling stone";
			case 17: return "Crossbow bolt";
			case 18: return "Arrow";
			case 19: return "Stone";
			case 20: return "Fireball";
			case 21: return "Lightning bolt";
			case 22: return "Acid";
			case 23: return "Magic missile";
			case 24: return "Sling";
			case 25: return "Bow";
			case 26: return "Crossbow";
			case 27: return "Unknown";
			case 28: return "Unknown";
			case 29: return "Unknown";
			case 30: return "Unknown";
			case 31: return "Jeweled bow";
			case 32: return "Leather vest";
			case 33: return "Mail shirt";
			case 34: return "Breastplate";
			case 35: return "Leather leggings";
			case 36: return "Mail leggings";
			case 37: return "Plate leggings";
			case 38: return "Leather gloves";
			case 39: return "Chain gauntlets";
			case 40: return "Plate gauntlets";
			case 41: return "Leather boots";
			case 42: return "Chain boots";
			case 43: return "Plate boots";
			case 44: return "Leather cap";
			case 45: return "Chain cowl";
			case 46: return "Helmet";
			case 47: return "Dragon skin boots";
			case 48: return "Crown";
			case 49: return "Crown";
			case 50: return "Crown";
			case 51: return "Unknown";
			case 52: return "Unknown";
			case 53: return "Unknown";
			case 54: return "Iron ring";
			case 55: return "Shiny shield";
			case 56: return "Gold ring";
			case 57: return "Silver ring";
			case 58: return "Red ring";
			case 59: return "Tower shield";
			case 60: return "Wooden shield";
			case 61: return "Small shield";
			case 62: return "Buckler";
			case 63: return "Jeweled shield";
			case 64: return "Rotworm";
			case 65: return "Flesh slug";
			case 66: return "Cave bat";
			case 67: return "Giant rat";
			case 68: return "Giant spider";
			case 69: return "Acid slug";
			case 70: return "Green goblin";
			case 71: return "Green goblin";
			case 72: return "Giant rat";
			case 73: return "Vampire bat";
			case 74: return "Skeleton";
			case 75: return "Imp";
			case 76: return "Grey goblin";
			case 77: return "Green goblin";
			case 78: return "Grey goblin";
			case 79: return "Ethereal Void Creatures";
			case 80: return "Grey goblin";
			case 81: return "Mongbat";
			case 82: return "Bloodworm";
			case 83: return "Wolf spider";
			case 84: return "Mountainman";
			case 85: return "Green lizardman";
			case 86: return "Mountainman";
			case 87: return "Lurker";
			case 88: return "Red lizardman";
			case 89: return "Gray lizardman";
			case 90: return "Outcast";
			case 91: return "Headless";
			case 92: return "Dread spider";
			case 93: return "Fighter";
			case 94: return "Fighter";
			case 95: return "Fighter";
			case 96: return "Troll";
			case 97: return "Ghost";
			case 98: return "Fighter";
			case 99: return "Ghoul";
			case 100: return "Ghost";
			case 101: return "Ghost";
			case 102: return "Gazer";
			case 103: return "Mage";
			case 104: return "Fighter";
			case 105: return "Dark ghoul";
			case 106: return "Mage";
			case 107: return "Mage";
			case 108: return "Mage";
			case 109: return "Mage";
			case 110: return "Feral ghoul";
			case 111: return "Feral troll";
			case 112: return "Great troll";
			case 113: return "Dire ghost";
			case 114: return "Earth golem";
			case 115: return "Mage";
			case 116: return "Deep lurker";
			case 117: return "Shadow beast";
			case 118: return "Reaper";
			case 119: return "Stone golem";
			case 120: return "Fire elemental";
			case 121: return "Metal golem";
			case 122: return "Wisp";
			case 123: return "Tybal";
			case 124: return "Slasher of veils";
			case 125: return "Unknown";
			case 126: return "Unknown";
			case 127: return "Adventurer";
			case 128: return "Sack";
			case 129: return "Open sack";
			case 130: return "Pack";
			case 131: return "Open pack";
			case 132: return "Box";
			case 133: return "Open box";
			case 134: return "Pouch";
			case 135: return "Open pouch";
			case 136: return "Map case";
			case 137: return "Open map case";
			case 138: return "Gold coffer";
			case 139: return "Open gold coffer";
			case 140: return "Urn";
			case 141: return "Quiver";
			case 142: return "Bowl";
			case 143: return "Rune bag";
			case 144: return "Lantern";
			case 145: return "Torch";
			case 146: return "Candle";
			case 147: return "Taper";
			case 148: return "Lit lantern";
			case 149: return "Lit torch";
			case 150: return "Lit candle";
			case 151: return "Lit taper";
			case 152: return "Wand";
			case 153: return "Wand";
			case 154: return "Wand";
			case 155: return "Wand";
			case 156: return "Broken wand";
			case 157: return "Broken wand";
			case 158: return "Broken wand";
			case 159: return "Broken wand";
			case 160: return "Coin";
			case 161: return "Gold coin";
			case 162: return "Ruby";
			case 163: return "Red gem";
			case 164: return "Small blue gem";
			case 165: return "Large blue gem";
			case 166: return "Sapphire";
			case 167: return "Emerald";
			case 168: return "Amulet";
			case 169: return "Goblet";
			case 170: return "Sceptre";
			case 171: return "Gold chain";
			case 172: return "Gold plate";
			case 173: return "Ankh pendant";
			case 174: return "Cup";
			case 175: return "Gold nugget";
			case 176: return "Meat";
			case 177: return "Bread";
			case 178: return "Cheese";
			case 179: return "Apple";
			case 180: return "Corn";
			case 181: return "Bread";
			case 182: return "Fish";
			case 183: return "Popcorn";
			case 184: return "Mushroom";
			case 185: return "Toadstool";
			case 186: return "Bottle of ale";
			case 187: return "Red potion";
			case 188: return "Green potion";
			case 189: return "Bottle of water";
			case 190: return "Flask of port";
			case 191: return "Bottle of wine";
			case 192: return "Plant";
			case 193: return "Grass";
			case 194: return "Skull";
			case 195: return "Skull";
			case 196: return "Bone";
			case 197: return "Bone";
			case 198: return "Pile of bones";
			case 199: return "Vines";
			case 200: return "Broken axe";
			case 201: return "Broken sword";
			case 202: return "Broken mace";
			case 203: return "Broken shield";
			case 204: return "Piece of wood";
			case 205: return "Piece of wood";
			case 206: return "Plant";
			case 207: return "Plant";
			case 208: return "Pile of debris";
			case 209: return "Pile of debris";
			case 210: return "Pile of debris";
			case 211: return "Stalactite";
			case 212: return "Plant";
			case 213: return "Pile of debris";
			case 214: return "Pile of debris";
			case 215: return "Anvil";
			case 216: return "Pole";
			case 217: return "Dead rotworm";
			case 218: return "Rubble";
			case 219: return "Pile of wood chips";
			case 220: return "Pile of bones";
			case 221: return "Blood stain";
			case 222: return "Blood stain";
			case 223: return "Blood stain";
			case 224: return "Runestone";
			case 225: return "The key of truth";
			case 226: return "The key of love";
			case 227: return "The key of courage";
			case 228: return "Two part key";
			case 229: return "Two part key";
			case 230: return "Two part key";
			case 231: return "Key of infinity";
			case 232: return "An rune";
			case 233: return "Bet rune";
			case 234: return "Corp rune";
			case 235: return "Des rune";
			case 236: return "Ex rune";
			case 237: return "Flam rune";
			case 238: return "Grav rune";
			case 239: return "Hur rune";
			case 240: return "In rune";
			case 241: return "Jux rune";
			case 242: return "Kal rune";
			case 243: return "Lor rune";
			case 244: return "Mani rune";
			case 245: return "Nox rune";
			case 246: return "Ort rune";
			case 247: return "Por rune";
			case 248: return "Quas rune";
			case 249: return "Rel rune";
			case 250: return "Sanct rune";
			case 251: return "Tym rune";
			case 252: return "Uus rune";
			case 253: return "Vas rune";
			case 254: return "Wis rune";
			case 255: return "Ylem rune";
			case 256: return "Key";
			case 257: return "Lockpick";
			case 258: return "Key";
			case 259: return "Key";
			case 260: return "Key";
			case 261: return "Key";
			case 262: return "Key";
			case 263: return "Key";
			case 264: return "Key";
			case 265: return "Key";
			case 266: return "Key";
			case 267: return "Key";
			case 268: return "Key";
			case 269: return "Key";
			case 270: return "Key";
			case 271: return "Lock";
			case 272: return "Picture of tom";
			case 273: return "Crystal splinter";
			case 274: return "Orb rock";
			case 275: return "The gem cutter of coulnes";
			case 276: return "Exploding book";
			case 277: return "Block of burning incense";
			case 278: return "Block of incense";
			case 279: return "Orb";
			case 280: return "Broken blade";
			case 281: return "Broken hilt";
			case 282: return "Figurine";
			case 283: return "Rotworm stew";
			case 284: return "Strong thread";
			case 285: return "Dragon scales";
			case 286: return "Resilient sphere";
			case 287: return "Standard";
			case 288: return "Spell";
			case 289: return "Bedroll";
			case 290: return "Silver seed";
			case 291: return "Mandolin";
			case 292: return "Flute";
			case 293: return "Leeches";
			case 294: return "Moonstone";
			case 295: return "Spike";
			case 296: return "Rock hammer";
			case 297: return "Glowing rock";
			case 298: return "Campfire";
			case 299: return "Fishing pole";
			case 300: return "Medallion";
			case 301: return "Oil flask";
			case 302: return "Fountain";
			case 303: return "Cauldron";
			case 304: return "Book";
			case 305: return "Book";
			case 306: return "Book";
			case 307: return "Book";
			case 308: return "Book";
			case 309: return "Book";
			case 310: return "Book";
			case 311: return "Book";
			case 312: return "Scroll";
			case 313: return "Scroll";
			case 314: return "Scroll";
			case 315: return "Map";
			case 316: return "Scroll";
			case 317: return "Scroll";
			case 318: return "Scroll";
			case 319: return "Scroll";
			case 320: return "Door";
			case 321: return "Door";
			case 322: return "Door";
			case 323: return "Door";
			case 324: return "Door";
			case 325: return "Massive Door";
			case 326: return "Portcullis";
			case 327: return "Secret door";
			case 328: return "Open door";
			case 329: return "Open door";
			case 330: return "Open door";
			case 331: return "Open door";
			case 332: return "Open door";
			case 333: return "Open massive door";
			case 334: return "Open portcullis";
			case 335: return "Secret door";
			case 336: return "Bench";
			case 337: return "Arrow";
			case 338: return "Crossbow bolt";
			case 339: return "Large boulder";
			case 340: return "Large boulder";
			case 341: return "Boulder";
			case 342: return "Small boulder";
			case 343: return "Shrine";
			case 344: return "Table";
			case 345: return "Beam";
			case 346: return "Moongate";
			case 347: return "Barrel";
			case 348: return "Chair";
			case 349: return "Chest";
			case 350: return "Nightstand";
			case 351: return "Lotus turbo esprit";
			case 352: return "Pillar";
			case 353: return "Dial lever";
			case 354: return "Dial lever";
			case 355: return "Unknown";
			case 356: return "Bridge";
			case 357: return "Grave";
			case 358: return "Writing";
			case 359: return "Unknown";
			case 360: return "Unknown";
			case 361: return "Unknown";
			case 362: return "Unknown";
			case 363: return "Unknown";
			case 364: return "Unknown";
			case 365: return "Unknown (force field?)";
			case 366: return "Passable wall";
			case 367: return "Impassable wall";
			case 368: return "Button";
			case 369: return "Button";
			case 370: return "Button";
			case 371: return "Switch";
			case 372: return "Switch";
			case 373: return "Lever";
			case 374: return "Pull chain";
			case 375: return "Pull chain";
			case 376: return "Button";
			case 377: return "Button";
			case 378: return "Button";
			case 379: return "Switch";
			case 380: return "Switch";
			case 381: return "Lever";
			case 382: return "Pull chain";
			case 383: return "Pull chain";
			case 384: return "Damage trap";
			case 385: return "Teleport trap";
			case 386: return "Arrow trap";
			case 387: return "Do trap";
			case 388: return "Pit trap";
			case 389: return "Change terrain trap";
			case 390: return "Spelltrap";
			case 391: return "Create object trap";
			case 392: return "Door trap";
			case 393: return "Ward trap";
			case 394: return "Tell trap";
			case 395: return "Delete object trap";
			case 396: return "Inventory trap";
			case 397: return "Set variable trap";
			case 398: return "Check variable trap";
			case 399: return "Combination trap";
			case 400: return "Text string trap";
			case 401: return "Unknown";
			case 402: return "Unknown";
			case 403: return "Unknown";
			case 404: return "Unknown";
			case 405: return "Unknown";
			case 406: return "Unknown";
			case 407: return "Unknown";
			case 408: return "Unknown";
			case 409: return "Unknown";
			case 410: return "Unknown";
			case 411: return "Unknown";
			case 412: return "Unknown";
			case 413: return "Unknown";
			case 414: return "Unknown";
			case 415: return "Unknown";
			case 416: return "Move trigger";
			case 417: return "Pick up trigger";
			case 418: return "Use trigger";
			case 419: return "Look trigger";
			case 420: return "Step on trigger";
			case 421: return "Open trigger";
			case 422: return "Unlock trigger";
			case 423: return "Unknown";
			case 424: return "Unknown";
			case 425: return "Unknown";
			case 426: return "Unknown";
			case 427: return "Unknown";
			case 428: return "Unknown";
			case 429: return "Unknown";
			case 430: return "Unknown";
			case 431: return "Unknown";
			case 432: return "Unknown";
			case 433: return "Unknown";
			case 434: return "Unknown";
			case 435: return "Unknown";
			case 436: return "Unknown";
			case 437: return "Unknown";
			case 438: return "Unknown";
			case 439: return "Unknown";
			case 440: return "Unknown";
			case 441: return "Unknown";
			case 442: return "Unknown";
			case 443: return "Unknown";
			case 444: return "Unknown";
			case 445: return "Unknown";
			case 446: return "Unknown";
			case 447: return "Unknown";
			case 448: return "Blood splatter";
			case 449: return "Mist cloud";
			case 450: return "Explosion";
			case 451: return "Explosion";
			case 452: return "Explosion";
			case 453: return "Splash";
			case 454: return "Water splash";
			case 455: return "Spell effect";
			case 456: return "Smoke";
			case 457: return "Fountain";
			case 458: return "Silver tree";
			case 459: return "Damage";
			case 460: return "Zap";
			case 461: return "Sound source";
			case 462: return "Changing terrain";
			case 463: return "Moving door";
			case 464: return "Outofrange";

			case 465: return "Attitude trap";//465 : attitude trap
			case 466: return "Platform trap";//466 : platform trap
			case 467: return "Camera trap";//467 : camera trap
			case 468: return "Conversation trap";//468 : conv trap
			case 469: return "End game trap";//469 : end game trap

			default:	return "Invalid object";
		}
	}

	public static ObjectType GetObjectType(int id)
	{
		switch (id)
		{
			case 0: return ObjectType.Melee;	//"Hand axe";
			case 1: return ObjectType.Melee; //"Battle axe";
			case 2: return ObjectType.Melee; //"Axe";
			case 3: return ObjectType.Melee; //"Dagger";
			case 4: return ObjectType.Melee; //"Shortsword";
			case 5: return ObjectType.Melee; //"Longsword";
			case 6: return ObjectType.Melee; //"Broadsword";
			case 7: return ObjectType.Melee; //"Cudgel";
			case 8: return ObjectType.Melee; //"Light mace";
			case 9: return ObjectType.Melee; //"Mace";
			case 10: return ObjectType.Melee; //"Shiny sword";
			case 11: return ObjectType.Melee; //"Jeweled axe";
			case 12: return ObjectType.Melee; //"Black sword";
			case 13: return ObjectType.Melee; //"Jeweled sword";
			case 14: return ObjectType.Melee; //"Jeweled mace";
			case 15: return ObjectType.Unknown; //"Fist";
			case 16: return ObjectType.Projectile; //"Sling stone";
			case 17: return ObjectType.Projectile; //"Crossbow bolt";
			case 18: return ObjectType.Projectile; //"Arrow";
			case 19: return ObjectType.SpecialProjectile; //"Stone";
			case 20: return ObjectType.SpecialProjectile; //"Fireball";
			case 21: return ObjectType.SpecialProjectile; //"Lightning bolt";
			case 22: return ObjectType.SpecialProjectile; //"Acid";
			case 23: return ObjectType.SpecialProjectile; //"Magic missile";
			case 24: return ObjectType.Ranged; //"Sling";
			case 25: return ObjectType.Ranged; //"Bow";
			case 26: return ObjectType.Ranged; //"Crossbow";
			case 27: return ObjectType.SpecialRanged; //"Unknown";
			case 28: return ObjectType.SpecialRanged; //"Unknown";
			case 29: return ObjectType.SpecialRanged; //"Unknown";
			case 30: return ObjectType.SpecialRanged; //"Unknown";
			case 31: return ObjectType.Ranged; //"Jeweled bow";
			case 32: return ObjectType.Armour;// "Leather vest";
			case 33: return ObjectType.Armour; //"Mail shirt";
			case 34: return ObjectType.Armour; //"Breastplate";
			case 35: return ObjectType.Armour; //"Leather leggings";
			case 36: return ObjectType.Armour; //"Mail leggings";
			case 37: return ObjectType.Armour; //"Plate leggings";
			case 38: return ObjectType.Armour; //"Leather gloves";
			case 39: return ObjectType.Armour; //"Chain gauntlets";
			case 40: return ObjectType.Armour; //"Plate gauntlets";
			case 41: return ObjectType.Armour; //"Leather boots";
			case 42: return ObjectType.Armour; //"Chain boots";
			case 43: return ObjectType.Armour; //"Plate boots";
			case 44: return ObjectType.Armour; //"Leather cap";
			case 45: return ObjectType.Armour; //"Chain cowl";
			case 46: return ObjectType.Armour; //"Helmet";
			case 47: return ObjectType.Armour; //"Dragon skin boots";
			case 48: return ObjectType.Baubles; //"Crown";
			case 49: return ObjectType.Baubles; //"Crown";
			case 50: return ObjectType.Baubles; //"Crown";
			case 51: return ObjectType.SpecialBauble; //"Unknown ring 1";
			case 52: return ObjectType.SpecialBauble; //"Unknown ring 2";
			case 53: return ObjectType.SpecialBauble; //"Unknown ring 3";
			case 54: return ObjectType.Baubles; //"Iron ring";
			case 55: return ObjectType.Shield; //"Shiny shield";
			case 56: return ObjectType.Baubles; //"Gold ring";
			case 57: return ObjectType.Baubles; //"Silver ring";
			case 58: return ObjectType.Baubles; //"Red ring";
			case 59: return ObjectType.Shield; //"Tower shield";
			case 60: return ObjectType.Shield; //"Wooden shield";
			case 61: return ObjectType.Shield; //"Small shield";
			case 62: return ObjectType.Shield; //"Buckler";
			case 63: return ObjectType.Shield; //"Jeweled shield";
			case 64: return ObjectType.Monster; //"Rotworm";
			case 65: return ObjectType.Monster; //"Flesh slug";
			case 66: return ObjectType.Monster; //"Cave bat";
			case 67: return ObjectType.Monster; //"Giant rat";
			case 68: return ObjectType.Monster; //"Giant spider";
			case 69: return ObjectType.Monster; //"Acid slug";
			case 70: return ObjectType.Monster; //"Green goblin";
			case 71: return ObjectType.Monster; //"Green goblin";
			case 72: return ObjectType.Monster; //"Giant rat";
			case 73: return ObjectType.Monster; //"Vampire bat";
			case 74: return ObjectType.Monster; //"Skeleton";
			case 75: return ObjectType.Monster; //"Imp";
			case 76: return ObjectType.Monster; //"Grey goblin";
			case 77: return ObjectType.Monster; //"Green goblin";
			case 78: return ObjectType.Monster; //"Grey goblin";
			case 79: return ObjectType.Monster; //"Ethereal Void Creatures";
			case 80: return ObjectType.Monster; //"Grey goblin";
			case 81: return ObjectType.Monster; //"Mongbat";
			case 82: return ObjectType.Monster; //"Bloodworm";
			case 83: return ObjectType.Monster; //"Wolf spider";
			case 84: return ObjectType.Monster; //"Mountainman";
			case 85: return ObjectType.Monster; //"Green lizardman";
			case 86: return ObjectType.Monster; //"Mountainman";
			case 87: return ObjectType.Monster; //"Lurker";
			case 88: return ObjectType.Monster; //"Red lizardman";
			case 89: return ObjectType.Monster; //"Gray lizardman";
			case 90: return ObjectType.Monster; //"Outcast";
			case 91: return ObjectType.Monster; //"Headless";
			case 92: return ObjectType.Monster; //"Dread spider";
			case 93: return ObjectType.Monster; //"Fighter";
			case 94: return ObjectType.Monster; //"Fighter";
			case 95: return ObjectType.Monster; //"Fighter";
			case 96: return ObjectType.Monster; //"Troll";
			case 97: return ObjectType.Monster; //"Ghost";
			case 98: return ObjectType.Monster; //"Fighter";
			case 99: return ObjectType.Monster; //"Ghoul";
			case 100: return ObjectType.Monster; //"Ghost";
			case 101: return ObjectType.Monster; //"Ghost";
			case 102: return ObjectType.Monster; //"Gazer";
			case 103: return ObjectType.Monster; //"Mage";
			case 104: return ObjectType.Monster; //"Fighter";
			case 105: return ObjectType.Monster; //"Dark ghoul";
			case 106: return ObjectType.Monster; //"Mage";
			case 107: return ObjectType.Monster; //"Mage";
			case 108: return ObjectType.Monster; //"Mage";
			case 109: return ObjectType.Monster; //"Mage";
			case 110: return ObjectType.Monster; //"Ghoul";
			case 111: return ObjectType.Monster; //"Feral troll";
			case 112: return ObjectType.Monster; //"Great troll";
			case 113: return ObjectType.Monster; //"Dire ghost";
			case 114: return ObjectType.Monster; //"Earth golem";
			case 115: return ObjectType.Monster; //"Mage";
			case 116: return ObjectType.Monster; //"Deep lurker";
			case 117: return ObjectType.Monster; //"Shadow beast";
			case 118: return ObjectType.Monster; //"Reaper";
			case 119: return ObjectType.Monster; //"Stone golem";
			case 120: return ObjectType.Monster; //"Fire elemental";
			case 121: return ObjectType.Monster; //"Metal golem";
			case 122: return ObjectType.Monster; //"Wisp";
			case 123: return ObjectType.Monster; //"Tybal";
			case 124: return ObjectType.Monster; //"Slasher of veils";
			case 125: return ObjectType.Unknown; //"Unknown";
			case 126: return ObjectType.Unknown; //"Unknown";
			case 127: return ObjectType.Unknown; //"Adventurer";
			case 128: return ObjectType.Container; //"Sack";
			case 129: return ObjectType.Container; //"Open sack";
			case 130: return ObjectType.Container; //"Pack";
			case 131: return ObjectType.Container; //"Open pack";
			case 132: return ObjectType.Container; //"Box";
			case 133: return ObjectType.Container; //"Open box";
			case 134: return ObjectType.Container; //"Pouch";
			case 135: return ObjectType.Container; //"Open pouch";
			case 136: return ObjectType.Container; //"Map case";
			case 137: return ObjectType.Container; //"Open map case";
			case 138: return ObjectType.Container; //"Gold coffer";
			case 139: return ObjectType.Container; //"Open gold coffer";
			case 140: return ObjectType.Container; //"Urn";
			case 141: return ObjectType.Container; //"Quiver";
			case 142: return ObjectType.Container; //"Bowl";
			case 143: return ObjectType.Container; //"Rune bag";
			case 144: return ObjectType.Light; //"Lantern";
			case 145: return ObjectType.Light; //"Torch";
			case 146: return ObjectType.Light; //"Candle";
			case 147: return ObjectType.Light; //"Taper";
			case 148: return ObjectType.Light; //"Lit lantern";
			case 149: return ObjectType.Light; //"Lit torch";
			case 150: return ObjectType.Light; //"Lit candle";
			case 151: return ObjectType.Light; //"Lit taper";
			case 152: return ObjectType.Wand; //"Wand";
			case 153: return ObjectType.Wand; //"Wand";
			case 154: return ObjectType.Wand; //"Wand";
			case 155: return ObjectType.Wand; //"Wand";
			case 156: return ObjectType.Countable; //"Broken wand";
			case 157: return ObjectType.Countable; //"Broken wand";
			case 158: return ObjectType.Countable; //"Broken wand";
			case 159: return ObjectType.Countable; //"Broken wand";
			case 160: return ObjectType.Gold; //"Coin";
			case 161: return ObjectType.Gold; //"Gold coin";
			case 162: return ObjectType.Treasure; //"Ruby";
			case 163: return ObjectType.Treasure; //"Red gem";
			case 164: return ObjectType.Treasure; //"Small blue gem";
			case 165: return ObjectType.Treasure; //"Large blue gem";
			case 166: return ObjectType.Treasure; //"Sapphire";
			case 167: return ObjectType.Treasure; //"Emerald";
			case 168: return ObjectType.Treasure; //"Amulet";
			case 169: return ObjectType.Treasure; //"Goblet";
			case 170: return ObjectType.Treasure; //"Sceptre";
			case 171: return ObjectType.Treasure; //"Gold chain";
			case 172: return ObjectType.Treasure; //"Gold plate";
			case 173: return ObjectType.Treasure; //"Ankh pendant";
			case 174: return ObjectType.Treasure; //"Cup";
			case 175: return ObjectType.Treasure; //"Gold nugget";
			case 176: return ObjectType.Food; //"Meat";
			case 177: return ObjectType.Food; //"Bread";
			case 178: return ObjectType.Food; //"Cheese";
			case 179: return ObjectType.Food; //"Apple";
			case 180: return ObjectType.Food; //"Corn";
			case 181: return ObjectType.Food; //"Bread";
			case 182: return ObjectType.Food; //"Fish";
			case 183: return ObjectType.Food; //"Popcorn";
			case 184: return ObjectType.Food; //"Mushroom";
			case 185: return ObjectType.Food; //"Toadstool";
			case 186: return ObjectType.Drink; //"Bottle of ale";
			case 187: return ObjectType.Potion; //"Red potion";
			case 188: return ObjectType.Potion; //"Green potion";
			case 189: return ObjectType.Drink; //"Bottle of water";
			case 190: return ObjectType.Drink; //"Flask of port";
			case 191: return ObjectType.Drink; //"Bottle of wine";
			case 192: return ObjectType.Unpickable; //"Plant";
			case 193: return ObjectType.Unpickable; //"Grass";
			case 194: return ObjectType.Bones; //"Skull";
			case 195: return ObjectType.Bones; //"Skull";
			case 196: return ObjectType.Bones; //"Bone";
			case 197: return ObjectType.Bones; //"Bone";
			case 198: return ObjectType.Bones; //"Pile of bones";
			case 199: return ObjectType.Unpickable; //"Vines";
			case 200: return ObjectType.Countable; //"Broken axe";
			case 201: return ObjectType.Countable; //"Broken sword";
			case 202: return ObjectType.Countable; //"Broken mace";
			case 203: return ObjectType.Countable; //"Broken shield";
			case 204: return ObjectType.Countable; //"Piece of wood";
			case 205: return ObjectType.Countable; //"Piece of wood";
			case 206: return ObjectType.Countable; //"Plant";
			case 207: return ObjectType.Countable; //"Plant";
			case 208: return ObjectType.Unpickable; //"Pile of debris";
			case 209: return ObjectType.Unpickable; // "Pile of debris";
			case 210: return ObjectType.Unpickable; // "Pile of debris";
			case 211: return ObjectType.CeilingHugger; // "Stalactite";
			case 212: return ObjectType.CeilingHugger; //"Plant";
			case 213: return ObjectType.Unpickable; // "Pile of debris";
			case 214: return ObjectType.Unpickable; // "Pile of debris";
			case 215: return ObjectType.Unpickable; // "Anvil";
			case 216: return ObjectType.Countable; //"Pole";
			case 217: return ObjectType.Countable; //"Dead rotworm";
			case 218: return ObjectType.Unpickable; //"Rubble";
			case 219: return ObjectType.Unpickable; //"Pile of wood chips";
			case 220: return ObjectType.Unpickable; //"Pile of bones";
			case 221: return ObjectType.Unpickable; //"Blood stain";
			case 222: return ObjectType.Unpickable; //"Blood stain";
			case 223: return ObjectType.Unpickable; //"Blood stain";
			case 224: return ObjectType.Unknown; //"Runestone";
			case 225: return ObjectType.Uncountable; //"The key of truth";
			case 226: return ObjectType.Uncountable; //"The key of love";
			case 227: return ObjectType.Uncountable; //"The key of courage";
			case 228: return ObjectType.Uncountable; //"Two part key";
			case 229: return ObjectType.Uncountable; //"Two part key";
			case 230: return ObjectType.Uncountable; //"Two part key";
			case 231: return ObjectType.Uncountable; //"Key of infinity";
			case 232: return ObjectType.Runestone; //"An rune";
			case 233: return ObjectType.Runestone; //"Bet rune";
			case 234: return ObjectType.Runestone; //"Corp rune";
			case 235: return ObjectType.Runestone; //"Des rune";
			case 236: return ObjectType.Runestone; //"Ex rune";
			case 237: return ObjectType.Runestone; //"Flam rune";
			case 238: return ObjectType.Runestone; //"Grav rune";
			case 239: return ObjectType.Runestone; //"Hur rune";
			case 240: return ObjectType.Runestone; //"In rune";
			case 241: return ObjectType.Runestone; //"Jux rune";
			case 242: return ObjectType.Runestone; //"Kal rune";
			case 243: return ObjectType.Runestone; //"Lor rune";
			case 244: return ObjectType.Runestone; //"Mani rune";
			case 245: return ObjectType.Runestone; //"Nox rune";
			case 246: return ObjectType.Runestone; //"Ort rune";
			case 247: return ObjectType.Runestone; //"Por rune";
			case 248: return ObjectType.Runestone; //"Quas rune";
			case 249: return ObjectType.Runestone; //"Rel rune";
			case 250: return ObjectType.Runestone; //"Sanct rune";
			case 251: return ObjectType.Runestone; //"Tym rune";
			case 252: return ObjectType.Runestone; //"Uus rune";
			case 253: return ObjectType.Runestone; //"Vas rune";
			case 254: return ObjectType.Runestone; //"Wis rune";
			case 255: return ObjectType.Runestone; //"Ylem rune";
			case 256: return ObjectType.Key; //"Key";
			case 257: return ObjectType.Countable; //"Lockpick";
			case 258: return ObjectType.Runestone; //"Key";
			case 259: return ObjectType.Key; //"Key";
			case 260: return ObjectType.Key; //"Key";
			case 261: return ObjectType.Key; //"Key";
			case 262: return ObjectType.Key; //"Key";
			case 263: return ObjectType.Key; //"Key";
			case 264: return ObjectType.Key; //"Key";
			case 265: return ObjectType.Key; //"Key";
			case 266: return ObjectType.Key; //"Key";
			case 267: return ObjectType.Key; //"Key";
			case 268: return ObjectType.Key; //"Key";
			case 269: return ObjectType.Key; //"Key";
			case 270: return ObjectType.Key; //"Key";
			case 271: return ObjectType.Lock; //"Lock";
			case 272: return ObjectType.Uncountable; //"Picture of tom";
			case 273: return ObjectType.Uncountable; //"Crystal splinter";
			case 274: return ObjectType.Countable; //"Orb rock";
			case 275: return ObjectType.Uncountable; //"The gem cutter of coulnes";
			case 276: return ObjectType.Uncountable; //"Exploding book";
			case 277: return ObjectType.Countable; //"Block of burning incense";
			case 278: return ObjectType.Countable; //"Block of incense";
			case 279: return ObjectType.Unpickable; //"Orb";
			case 280: return ObjectType.Uncountable; //"Broken blade";
			case 281: return ObjectType.Uncountable; //"Broken hilt";
			case 282: return ObjectType.Uncountable; //"Figurine";
			case 283: return ObjectType.Uncountable; //"Rotworm stew";
			case 284: return ObjectType.Countable; //"Strong thread";
			case 285: return ObjectType.Uncountable; //"Dragon scales";
			case 286: return ObjectType.Countable; //"Resilient sphere";
			case 287: return ObjectType.Uncountable; //"Standard";
			case 288: return ObjectType.Spell; //"Spell";
			case 289: return ObjectType.Countable; //"Bedroll";
			case 290: return ObjectType.Uncountable; //"Silver seed";
			case 291: return ObjectType.Countable; //"Mandolin";
			case 292: return ObjectType.Countable; //"Flute";
			case 293: return ObjectType.Countable; //"Leeches";
			case 294: return ObjectType.Uncountable; //"Moonstone";
			case 295: return ObjectType.Countable; //"Spike";
			case 296: return ObjectType.Countable; //"Rock hammer";
			case 297: return ObjectType.Countable; //"Glowing rock";
			case 298: return ObjectType.Unpickable; //"Campfire";
			case 299: return ObjectType.Countable; //"Fishing pole";
			case 300: return ObjectType.Countable; //"Medallion";
			case 301: return ObjectType.Countable; //"Oil flask";
			case 302: return ObjectType.Unpickable; //"Fountain";
			case 303: return ObjectType.Unpickable; //"Cauldron";
			case 304: return ObjectType.Book; //"Book";
			case 305: return ObjectType.Book; //"Book";
			case 306: return ObjectType.Book; //"Book";
			case 307: return ObjectType.Book; //"Book";
			case 308: return ObjectType.Book; //"Book";
			case 309: return ObjectType.Book; //"Book";
			case 310: return ObjectType.Book; //"Book";
			case 311: return ObjectType.Book; //"Book";
			case 312: return ObjectType.Scroll; //"Scroll";
			case 313: return ObjectType.Scroll; //"Scroll";
			case 314: return ObjectType.Scroll; //"Scroll";
			case 315: return ObjectType.Map; //"Map";
			case 316: return ObjectType.Scroll; //"Scroll";
			case 317: return ObjectType.Scroll; //"Scroll";
			case 318: return ObjectType.Scroll; //"Scroll";
			case 319: return ObjectType.Scroll; //"Scroll";
			case 320: return ObjectType.Door; //"Door";
			case 321: return ObjectType.Door; //"Door";
			case 322: return ObjectType.Door; //"Door";
			case 323: return ObjectType.Door; //"Door";
			case 324: return ObjectType.Door; //"Door";
			case 325: return ObjectType.Door; //"Door";
			case 326: return ObjectType.Door; //"Portcullis";
			case 327: return ObjectType.Door; //"Secret door";
			case 328: return ObjectType.Door; //"Open door";
			case 329: return ObjectType.Door; //"Open door";
			case 330: return ObjectType.Door; //"Open door";
			case 331: return ObjectType.Door; //"Open door";
			case 332: return ObjectType.Door; //"Open door";
			case 333: return ObjectType.Door; //"Open door";
			case 334: return ObjectType.Door; //"Open portcullis";
			case 335: return ObjectType.Door; //"Secret door";
			case 336: return ObjectType.Model; //"Bench";
			case 337: return ObjectType.Model; //"Arrow";
			case 338: return ObjectType.Model; //"Crossbow bolt";
			case 339: return ObjectType.Model; //"Large boulder";
			case 340: return ObjectType.Model; //"Large boulder";
			case 341: return ObjectType.Model; //"Boulder";
			case 342: return ObjectType.Model; //"Small boulder";
			case 343: return ObjectType.Model; //"Shrine";
			case 344: return ObjectType.Model; //"Table";
			case 345: return ObjectType.Model; //"Beam";
			case 346: return ObjectType.Model; //"Moongate";
			case 347: return ObjectType.ContainerModel; //"Barrel";
			case 348: return ObjectType.Model; //"Chair";
			case 349: return ObjectType.ContainerModel; //"Chest";
			case 350: return ObjectType.Model; //"Nightstand";
			case 351: return ObjectType.Model; //"Lotus turbo esprit";
			case 352: return ObjectType.Model; //"Pillar";
			case 353: return ObjectType.DialLever; //"Lever";
			case 354: return ObjectType.DialLever; //"Switch";
			case 355: return ObjectType.Model; //"Unknown";
			case 356: return ObjectType.Model; //"Bridge";
			case 357: return ObjectType.Grave; //"Gravestone";
			case 358: return ObjectType.Writing; //"Writing";
			case 359: return ObjectType.Unknown; //"Unknown";
			case 360: return ObjectType.Unknown; //"Unknown";
			case 361: return ObjectType.Unknown; //"Unknown";
			case 362: return ObjectType.Unknown; //"Unknown";
			case 363: return ObjectType.Unknown; //"Unknown";
			case 364: return ObjectType.Unknown; //"Unknown";
			case 365: return ObjectType.Unknown; //"Force field";
			case 366: return ObjectType.VerticalTexture; //"Vertical texture";
			case 367: return ObjectType.VerticalTexture; //"Special tmap obj";
			case 368: return ObjectType.Lever; //"Button";
			case 369: return ObjectType.Lever; //"Button";
			case 370: return ObjectType.Lever; //"Button";
			case 371: return ObjectType.Lever; //"Switch";
			case 372: return ObjectType.Lever; //"Switch";
			case 373: return ObjectType.Lever; //"Lever";
			case 374: return ObjectType.Lever; //"Pull chain";
			case 375: return ObjectType.Lever; //"Pull chain";
			case 376: return ObjectType.Lever; //"Button";
			case 377: return ObjectType.Lever; //"Button";
			case 378: return ObjectType.Lever; //"Button";
			case 379: return ObjectType.Lever; //"Switch";
			case 380: return ObjectType.Lever; //"Switch";
			case 381: return ObjectType.Lever; //"Lever";
			case 382: return ObjectType.Lever; //"Pull chain";
			case 383: return ObjectType.Lever; //"Pull chain";
			case 384: return ObjectType.Trap; //"Damage trap";
			case 385: return ObjectType.Trap; //"Teleport trap";
			case 386: return ObjectType.Trap; //"Arrow trap";
			case 387: return ObjectType.Unknown; //"Do trap";
			case 388: return ObjectType.Trap; //"Pit trap";
			case 389: return ObjectType.Trap; //"Change terrain trap";
			case 390: return ObjectType.Trap; //"Spelltrap";
			case 391: return ObjectType.Trap; //"Create object trap";
			case 392: return ObjectType.DoorTrap; //"Door trap";
			case 393: return ObjectType.Unknown; //"Ward trap";
			case 394: return ObjectType.Unknown; //"Tell trap";
			case 395: return ObjectType.Trap; //"Delete object trap";
			case 396: return ObjectType.Trap; //"Inventory trap";
			case 397: return ObjectType.Trap; //"Set variable trap";
			case 398: return ObjectType.Trap; //"Check variable trap";
			case 399: return ObjectType.Unknown; //"Combination trap";
			case 400: return ObjectType.Trap; //"Text string trap";
			case 401: return ObjectType.Unknown; //"Unknown";
			case 402: return ObjectType.Unknown; //"Unknown";
			case 403: return ObjectType.Unknown; //"Unknown";
			case 404: return ObjectType.Unknown; //"Unknown";
			case 405: return ObjectType.Unknown; //"Unknown";
			case 406: return ObjectType.Unknown; //"Unknown";
			case 407: return ObjectType.Unknown; //"Unknown";
			case 408: return ObjectType.Unknown; //"Unknown";
			case 409: return ObjectType.Unknown; //"Unknown";
			case 410: return ObjectType.Unknown; //"Unknown";
			case 411: return ObjectType.Unknown; //"Unknown";
			case 412: return ObjectType.Unknown; //"Unknown";
			case 413: return ObjectType.Unknown; //"Unknown";
			case 414: return ObjectType.Unknown; //"Unknown";
			case 415: return ObjectType.Unknown; //"Unknown";
			case 416: return ObjectType.Trigger; //"Move trigger";
			case 417: return ObjectType.Trigger; //"Pick up trigger";
			case 418: return ObjectType.Trigger; //"Use trigger";
			case 419: return ObjectType.Trigger; //"Look trigger";
			case 420: return ObjectType.Unknown; //"Step on trigger";
			case 421: return ObjectType.Trigger; //"Open trigger";
			case 422: return ObjectType.Trigger; //"Unlock trigger";
			case 423: return ObjectType.Unknown; //"Unknown";
			case 424: return ObjectType.Unknown; //"Unknown";
			case 425: return ObjectType.Unknown; //"Unknown";
			case 426: return ObjectType.Unknown; //"Unknown";
			case 427: return ObjectType.Unknown; //"Unknown";
			case 428: return ObjectType.Unknown; //"Unknown";
			case 429: return ObjectType.Unknown; //"Unknown";
			case 430: return ObjectType.Unknown; //"Unknown";
			case 431: return ObjectType.Unknown; //"Unknown";
			case 432: return ObjectType.Unknown; //"Unknown";
			case 433: return ObjectType.Unknown; //"Unknown";
			case 434: return ObjectType.Unknown; //"Unknown";
			case 435: return ObjectType.Unknown; //"Unknown";
			case 436: return ObjectType.Unknown; //"Unknown";
			case 437: return ObjectType.Unknown; //"Unknown";
			case 438: return ObjectType.Unknown; //"Unknown";
			case 439: return ObjectType.Unknown; //"Unknown";
			case 440: return ObjectType.Unknown; //"Unknown";
			case 441: return ObjectType.Unknown; //"Unknown";
			case 442: return ObjectType.Unknown; //"Unknown";
			case 443: return ObjectType.Unknown; //"Unknown";
			case 444: return ObjectType.Unknown; //"Unknown";
			case 445: return ObjectType.Unknown; //"Unknown";
			case 446: return ObjectType.Unknown; //"Unknown";
			case 447: return ObjectType.Unknown; //"Unknown";
			case 448: return ObjectType.Animated; //"Some blood";
			case 449: return ObjectType.Animated; //"A mist cloud";
			case 450: return ObjectType.Animated; //"An explosion";
			case 451: return ObjectType.Animated; //"An explosion";
			case 452: return ObjectType.Animated; //"An explosion";
			case 453: return ObjectType.Animated; //"A splash";
			case 454: return ObjectType.Animated; //"A splash";
			case 455: return ObjectType.Animated; //"A spell effect";
			case 456: return ObjectType.Animated; //"Some smoke";
			case 457: return ObjectType.Animated; //"Fountain";
			case 458: return ObjectType.Animated; //"Silver tree";
			case 459: return ObjectType.Animated; //"Some damage";
			case 460: return ObjectType.Unknown; //"Unknown";
			case 461: return ObjectType.Unknown; //"A sound source";
			case 462: return ObjectType.Unknown; //"Some changing terrain";
			case 463: return ObjectType.Unknown; //"A moving door";
			case 464: return ObjectType.Unknown; //"Outofrange";
			case 465: return ObjectType.Trap;// "Attitude trap";//465 : attitude trap
			case 466: return ObjectType.Trap;// "Platform trap";//466 : platform trap
			case 467: return ObjectType.Trap;// "Camera trap";//467 : camera trap
			case 468: return ObjectType.Trap;// "Conversation trap";//468 : conv trap
			case 469: return ObjectType.Trap;// "End game trap";//469 : end game trap

			default: return ObjectType.Unknown; 
		}
	}


	public string GetEnchantmentName()
	{
		return GetEnchantmentName(GetEnchantment());
	}

	public static string GetEnchantmentName(int id)
	{
		if (id >= 512)
			id -= 512;

		switch (id)
		{
			case 0: return "Darkness";
			case 1: return "Burning Match";
			case 2: return "Candlelight";
			case 3: return "Light";
			case 4: return "Magic Lantern";
			case 5: return "Night Vision";
			case 6: return "Daylight";
			case 7: return "Sunlight";
			case 17: return "Leap";
			case 18: return "Slow Fall";
			case 19: return "Levitate";
			case 20: return "Water Walk";
			case 21: return "Fly";
			case 34: return "Resist Blows";
			case 35: return "Thick Skin";
			case 37: return "Iron Flesh";
			case 49: return "Curse";
			case 50: return "Stealth";
			case 51: return "Conceal";
			case 52: return "Invisibilty";
			case 53: return "Missile Protection";
			case 54: return "Flameproof";
			case 55: return "Poison Resistance";
			case 56: return "Magic Protection";
			case 57: return "Greater Magic Protection";
			case 64: return "Lesser Heal 1";
			case 65: return "Lesser Heal 2";
			case 66: return "Lesser Heal 3";
			case 67: return "Lesser Heal 4";
			case 68: return "Heal 1";
			case 69: return "Heal 2";
			case 70: return "Heal 3";
			case 71: return "Heal 4";
			case 72: return "Enhanced Heal 1";
			case 73: return "Enhanced Heal 2";
			case 74: return "Enhanced Heal 3";
			case 75: return "Enhanced Heal 4";
			case 76: return "Greater Heal 1";
			case 77: return "Greater Heal 2";
			case 78: return "Greater Heal 3";
			case 79: return "Greater Heal 4";
			case 81: return "Magic Arrow";
			case 82: return "Electrical Bolt";
			case 83: return "Fireball";
			case 97: return "Reveal";
			case 98: return "Sheet Lightning";
			case 99: return "Confusion";
			case 100: return "Flame Wind";
			case 113: return "Cause Fear";
			case 114: return "Smite Undead";
			case 115: return "Ally";
			case 116: return "Poison";
			case 117: return "Paralyze";
			case 129: return "Create Food";
			case 130: return "Set Guard";
			case 131: return "Rune of Warding";
			case 132: return "Summon Monster";
			case 144: return "Cursed";
			case 145: return "Cursed";
			case 146: return "Cursed";
			case 147: return "Cursed";
			case 148: return "Cursed";
			case 149: return "Cursed";
			case 150: return "Cursed";
			case 151: return "Cursed";
			case 152: return "Cursed";
			case 153: return "Cursed";
			case 154: return "Cursed";
			case 155: return "Cursed";
			case 156: return "Cursed";
			case 157: return "Cursed";
			case 158: return "Cursed";
			case 159: return "Cursed";
			case 160: return "Increase Mana";
			case 161: return "Increase Mana";
			case 162: return "Increase Mana";
			case 163: return "Increase Mana";
			case 164: return "Mana Boost";
			case 165: return "Mana Boost";
			case 166: return "Mana Boost";
			case 167: return "Mana Boost";
			case 168: return "Regain Mana";
			case 169: return "Regain Mana";
			case 170: return "Regain Mana";
			case 171: return "Regain Mana";
			case 172: return "Restore Mana";
			case 173: return "Restore Mana";
			case 174: return "Restore Mana";
			case 175: return "Restore Mana";
			case 176: return "Speed";
			case 177: return "Detect Monster";
			case 178: return "Strengthen Door";
			case 179: return "Remove Trap";
			case 180: return "Name Enchantment";
			case 181: return "Open";
			case 182: return "Cure Poison";
			case 183: return "Roaming Sight";
			case 184: return "Telekinesis";
			case 185: return "Tremor";
			case 186: return "Gate Travel";
			case 187: return "Freeze Time";
			case 188: return "Armageddon";
			case 190: return "Regeneration";
			case 191: return "Mana Regeneration";
			case 211: return "the Frog";
			case 212: return "Maze Navigation";
			case 213: return "Hallucination";
			case 256: return "Light";
			case 257: return "Resist Blows";
			case 258: return "Magic Arrow";
			case 259: return "Create Food";
			case 260: return "Stealth";
			case 261: return "Leap";
			case 262: return "Curse";
			case 263: return "Slow Fall";
			case 264: return "Lesser Heal";
			case 265: return "Detect Monster";
			case 266: return "Cause Fear";
			case 267: return "Rune of Warding";
			case 268: return "Speed";
			case 269: return "Conceal";
			case 270: return "Night Vision";
			case 271: return "Electrical Bolt";
			case 272: return "Strengthen Door";
			case 273: return "Thick Skin";
			case 274: return "Water Walk";
			case 275: return "Heal";
			case 276: return "Levitate";
			case 277: return "Poison";
			case 278: return "Flameproof";
			case 279: return "Remove Trap";
			case 280: return "Fireball";
			case 281: return "Smite Undead";
			case 282: return "Name Enchantment";
			case 283: return "Missile Protection";
			case 284: return "Open";
			case 285: return "Cure Poison";
			case 286: return "Greater Heal";
			case 287: return "Sheet Lightning";
			case 288: return "Gate Travel";
			case 289: return "Paralyze";
			case 290: return "Daylight";
			case 291: return "Telekinesis";
			case 292: return "Fly";
			case 293: return "Ally";
			case 294: return "Summon Monster";
			case 295: return "Invisibility";
			case 296: return "Confusion";
			case 297: return "Reveal";
			case 298: return "Iron Flesh";
			case 299: return "Tremor";
			case 300: return "Roaming Sight";
			case 301: return "Flame Wind";
			case 302: return "Freeze Time";
			case 303: return "Armageddon";
			case 304: return "Mass Paralyze";
			case 305: return "Acid";
			case 306: return "Local Teleport";
			case 307: return "Mana Boost";
			case 308: return "Restore Mana";
			case 384: return "Leap";
			case 385: return "Slow Fall";
			case 386: return "Levitate";
			case 387: return "Water Walk";
			case 388: return "Fly";
			case 389: return "Curse";
			case 390: return "Stealth";
			case 391: return "Conceal";
			case 392: return "Invisibility";
			case 393: return "Missile Protection";
			case 394: return "Flameproof";
			case 395: return "Freeze Time";
			case 396: return "Roaming Sight";
			case 397: return "Haste";
			case 398: return "Telekinesis";
			case 399: return "Resist Blows";
			case 400: return "Thick Skin";
			case 401: return "Light";
			case 402: return "Iron Flesh";
			case 403: return "Night Vision";
			case 404: return "Daylight";
			case 448: return "Minor Accuracy";
			case 449: return "Accuracy";
			case 450: return "Additional Accuracy";
			case 451: return "Major Accuracy";
			case 452: return "Great Accuracy";
			case 453: return "Very Great Accuracy";
			case 454: return "Tremendous Accuracy";
			case 455: return "Unsurpassed Accuracy";
			case 456: return "Minor Damage";
			case 457: return "Damage";
			case 458: return "Additional Damage";
			case 459: return "Major Damage";
			case 460: return "Great Damage";
			case 461: return "Very Great Damage";
			case 462: return "Tremendous Damage";
			case 463: return "Unsurpassed Damage";
			case 464: return "Minor Protection";
			case 465: return "Protection";
			case 466: return "Additional Protection";
			case 467: return "Major Protection";
			case 468: return "Great Protection";
			case 469: return "Very Great Protection";
			case 470: return "Tremendous Protection";
			case 471: return "Unsurpassed Protection";
			case 472: return "Minor Toughness";
			case 473: return "Toughness";
			case 474: return "Additional Toughness";
			case 475: return "Major Toughness";
			case 476: return "Great Toughness";
			case 477: return "Very Great Toughness";
			case 478: return "Tremendous Toughness";
			case 479: return "Unsurpassed Toughness";

			default: return "INVALID";
		}
	}

	public static Tuple<Dictionary<string, int>, string[]> GetSpelltrapEffects()
	{
		List<string> list = new List<string>();
		Dictionary<string, int> dict = new Dictionary<string, int>();
		for (int i = 0; i < 214; i++)
		{
			string effect = GetEnchantmentName(i);
			if (effect != "INVALID")
			{
				dict[effect] = i;
				list.Add(effect);
			}
		}
		return new Tuple<Dictionary<string, int>, string[]>(dict, list.ToArray());
	}

	public virtual int GetFileIndex()
	{
		return DataReader.LevelOffsets[CurrentLevel - 1] + 23296 + (CurrentAdress - 256) * 8;
	}

	public static List<UnityEngine.UI.Dropdown.OptionData> GetTypes()
	{
		List<UnityEngine.UI.Dropdown.OptionData> options = new List<UnityEngine.UI.Dropdown.OptionData>();
		for (int i = 0; i < 465; i++)
		{
			UnityEngine.UI.Dropdown.OptionData option = new UnityEngine.UI.Dropdown.OptionData(GetName(i));
			options.Add(option);
		}
		return options;
	}

	public static List<int> GetStaticTypes()
	{
		List<int> statics = new List<int>();
		for (int i = 0; i < 461; i++)
		{
			if (i >= 64 && i <= 127)
				continue;
			statics.Add(i);
		}
		return statics;
	}

	public static List<int> GetMobileTypes()
	{
		List<int> mobiles = new List<int>();
		for (int i = 64; i < 125; i++)
			mobiles.Add(i);
		return mobiles;
	}

	public static List<int> GetAll()
	{
		List<int> all = new List<int>();
		for (int i = 0; i < 461; i++)
			all.Add(i);
		return all;
	}

	public SavedStatic SaveObject()
	{
		SavedStatic so = new SavedStatic(this);
		return so;
	}

	public static implicit operator bool(StaticObject t)
	{
		if (t == null)
			return false;
		return true;
	}
}

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
	public int Damage;
	public int Unk1;
	public int Unk2;
}

public class RangedData
{
	public int Ammo;
	public int Unk1;
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