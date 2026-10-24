using System;
using System.Collections.Generic;

namespace UltimateEditor.Uw1
{
	/// <summary>Item categories by item id, following the class ranges of include/items.h.</summary>
	[Flags]
	public enum ItemCategory : long
	{
		None = 0,
		MeleeWeapon = 1L << 0,
		Ammunition = 1L << 1,
		Launcher = 1L << 2,
		Armour = 1L << 3,
		Shield = 1L << 4,
		Ring = 1L << 5,
		Crown = 1L << 6,
		Creature = 1L << 7,
		Container = 1L << 8,
		Light = 1L << 9,
		Wand = 1L << 10,
		Treasure = 1L << 11,
		Food = 1L << 12,
		Potion = 1L << 13,
		Scenery = 1L << 14,
		QuestKey = 1L << 15,
		Runestone = 1L << 16,
		Key = 1L << 17,
		Lock = 1L << 18,
		Quest = 1L << 19,
		MagicItem = 1L << 20,
		SpellObject = 1L << 21,
		Book = 1L << 22,
		Scroll = 1L << 23,
		Door = 1L << 24,
		Furniture = 1L << 25,
		Decal = 1L << 26,
		Switch = 1L << 27,
		Trap = 1L << 28,
		Trigger = 1L << 29,
		Animated = 1L << 30,

		Weapon = MeleeWeapon | Ammunition | Launcher,
		Wearable = Armour | Shield | Ring | Crown,
		Usable = Light | Wand | Food | Potion | Key | Book | Scroll | MagicItem,
		Model3D = Door | Furniture | Decal | Switch,
		Event = Trap | Trigger,
	}

	public static class Uw1ItemCategories
	{
		public static ItemCategory Of(int item)
		{
			if (LevArk.Game.Kind == UltimateEditor.Games.GameKind.UW2) return OfUw2(item);
			if (item < 0x10) return ItemCategory.MeleeWeapon;
			if (item < 0x18) return ItemCategory.Ammunition;
			if (item < 0x20) return ItemCategory.Launcher;
			if (item < 0x30) return ItemCategory.Armour;
			if (item < 0x33) return ItemCategory.Crown;
			if (item < 0x36) return ItemCategory.Armour;      // unused armour slots
			if (item == 0x36 || (item >= 0x38 && item <= 0x3A)) return ItemCategory.Ring;
			if (item < 0x40) return ItemCategory.Shield;
			if (item < 0x80) return ItemCategory.Creature;
			if (item < 0x90) return ItemCategory.Container;
			if (item < 0x98) return ItemCategory.Light;
			if (item < 0xA0) return ItemCategory.Wand;
			if (item < 0xB0) return ItemCategory.Treasure;
			if (item == 0xBB || item == 0xBC) return ItemCategory.Potion;   // the red and green potions
			if (item < 0xC0) return ItemCategory.Food;
			if (item < 0xE1) return ItemCategory.Scenery;
			if (item < 0xE8) return ItemCategory.QuestKey;    // keys of Truth, Love, Courage, the key parts, Key of Infinity
			if (item < 0x100) return ItemCategory.Runestone;
			if (item == 0x10F) return ItemCategory.Lock;
			if (item < 0x110) return ItemCategory.Key;
			if (item < 0x120) return ItemCategory.Quest;
			if (item < 0x130) return item == 0x120 ? ItemCategory.SpellObject : ItemCategory.MagicItem;   // class 0x12: the spell object, then tools (bedroll, mandolin, spike...)
			if (item < 0x138) return ItemCategory.Book;
			if (item < 0x140) return ItemCategory.Scroll;
			if (item < 0x150) return ItemCategory.Door;
			if (item < 0x160) return ItemCategory.Furniture;
			if (item < 0x170) return ItemCategory.Decal;
			if (item < 0x180) return ItemCategory.Switch;
			if (item < 0x1A0) return ItemCategory.Trap;
			if (item < 0x1C0) return ItemCategory.Trigger;
			return ItemCategory.Animated;
		}

		/// <summary>UW2's classes (uw2 include/items.h; see Games/Uw2Items.cs for the types).</summary>
		public static ItemCategory OfUw2(int item)
		{
			if (item < 0x10) return ItemCategory.MeleeWeapon;
			if (item < 0x18) return ItemCategory.Ammunition;   // stones, bolts, arrows, then the special missiles
			if (item < 0x1B || item == 0x1F) return ItemCategory.Launcher;
			if (item < 0x20) return ItemCategory.Ammunition;   // homing dart, snowball, fireball, satellite
			if (item < 0x30 || item == 0x33) return ItemCategory.Armour;
			if (item < 0x33 || item == 0x34) return ItemCategory.Crown;   // crowns, the fraznium circlet
			if (item == 0x36) return ItemCategory.Quest;      // strange artifact
			if (item < 0x3B) return ItemCategory.Ring;        // Guardian signet ring and the rings
			if (item < 0x40) return ItemCategory.Shield;
			if (item < 0x80) return ItemCategory.Creature;
			if (item < 0x90) return ItemCategory.Container;
			if (item < 0x98) return ItemCategory.Light;
			if (item < 0xA0) return ItemCategory.Wand;
			if (item < 0xB0) return ItemCategory.Treasure;
			if (item < 0xC0) return ItemCategory.Food;
			if (item < 0xE0) return ItemCategory.Scenery;
			if (item >= 0xE1 && item <= 0xE7) return ItemCategory.Potion;
			if (item < 0x100) return ItemCategory.Runestone;
			if (item < 0x102) return ItemCategory.MagicItem;  // curious implement, lockpick
			if (item == 0x10F) return ItemCategory.Lock;
			if (item < 0x110) return ItemCategory.Key;
			if (item < 0x120) return ItemCategory.Quest;
			if (item < 0x130) return item == 0x120 ? ItemCategory.SpellObject : ItemCategory.MagicItem;
			if (item < 0x134 || item == 0x138) return ItemCategory.Book;
			if (item < 0x13B) return ItemCategory.Scroll;     // scrolls and maps
			if (item < 0x140) return ItemCategory.Scenery;
			if (item < 0x150) return ItemCategory.Door;
			if (item < 0x160) return ItemCategory.Furniture;
			if (item < 0x170) return ItemCategory.Decal;
			if (item < 0x180) return ItemCategory.Switch;
			if (item < 0x1A0) return ItemCategory.Trap;
			if (item < 0x1C0) return ItemCategory.Trigger;
			return ItemCategory.Animated;
		}

		/// <summary>The categories offered in the filter, with labels.</summary>
		public static readonly KeyValuePair<ItemCategory, string>[] Choices =
		{
			new KeyValuePair<ItemCategory, string>(ItemCategory.Weapon, "Weapons (all)"),
			new KeyValuePair<ItemCategory, string>(ItemCategory.MeleeWeapon, "Melee weapons"),
			new KeyValuePair<ItemCategory, string>(ItemCategory.Ammunition | ItemCategory.Launcher, "Missiles and launchers"),
			new KeyValuePair<ItemCategory, string>(ItemCategory.Wearable, "Armour, shields, rings, crowns"),
			new KeyValuePair<ItemCategory, string>(ItemCategory.Creature, "Creatures"),
			new KeyValuePair<ItemCategory, string>(ItemCategory.Container, "Containers"),
			new KeyValuePair<ItemCategory, string>(ItemCategory.Light | ItemCategory.Wand, "Lights and wands"),
			new KeyValuePair<ItemCategory, string>(ItemCategory.Treasure, "Treasure"),
			new KeyValuePair<ItemCategory, string>(ItemCategory.Food | ItemCategory.Potion, "Food and potions"),
			new KeyValuePair<ItemCategory, string>(ItemCategory.Scenery, "Scenery and junk"),
			new KeyValuePair<ItemCategory, string>(ItemCategory.Runestone, "Runestones"),
			new KeyValuePair<ItemCategory, string>(ItemCategory.Key | ItemCategory.Lock | ItemCategory.QuestKey, "Keys and locks"),
			new KeyValuePair<ItemCategory, string>(ItemCategory.Quest, "Unique and quest items (0x110-0x11F)"),
			new KeyValuePair<ItemCategory, string>(ItemCategory.MagicItem | ItemCategory.SpellObject, "Tools and spell objects (0x120-0x12F)"),
			new KeyValuePair<ItemCategory, string>(ItemCategory.Book | ItemCategory.Scroll, "Books and scrolls"),
			new KeyValuePair<ItemCategory, string>(ItemCategory.Door, "Doors"),
			new KeyValuePair<ItemCategory, string>(ItemCategory.Furniture | ItemCategory.Decal | ItemCategory.Switch, "Furniture, decals, switches"),
			new KeyValuePair<ItemCategory, string>(ItemCategory.Trap, "Traps"),
			new KeyValuePair<ItemCategory, string>(ItemCategory.Trigger, "Triggers"),
			new KeyValuePair<ItemCategory, string>(ItemCategory.Animated, "Animated objects"),
		};
	}

	/// <summary>A neutral view of one placed object, so the filter does not depend on Unity.</summary>
	public struct ObjectView
	{
		public int Level;
		public int Index;
		public int Item;
		public string Name;
		public bool Enchanted, Invisible, IsQuantity;
		public int Quality, Owner, Link;
		public int TileX, TileY;          // -1 when inside a container or not on the map
		public bool InContainer;
	}

	/// <summary>Tri-state condition: ignore, must be true, must be false.</summary>
	public enum Want { Any, Yes, No }

	/// <summary>The item filter (category, flags, ranges, name); every unset part passes.</summary>
	public sealed class ItemFilter
	{
		public ItemCategory Categories = ItemCategory.None;   // None = any
		public string NameContains = "";
		public Want Enchanted, Invisible, Quantity, Owned, InContainer;
		public int MinQuality = 0, MaxQuality = 63;
		public int ItemId = -1;                                // exact item id, -1 any
		public int Level = 0;                                  // 0 all levels

		public bool Matches(ObjectView o)
		{
			if (Level > 0 && o.Level != Level) return false;
			if (ItemId >= 0 && o.Item != ItemId) return false;
			if (Categories != ItemCategory.None && (Uw1ItemCategories.Of(o.Item) & Categories) == 0) return false;
			if (!Check(Enchanted, o.Enchanted)) return false;
			if (!Check(Invisible, o.Invisible)) return false;
			if (!Check(Quantity, o.IsQuantity)) return false;
			if (!Check(Owned, o.Owner != 0)) return false;
			if (!Check(InContainer, o.InContainer)) return false;
			if (o.Quality < MinQuality || o.Quality > MaxQuality) return false;
			if (!string.IsNullOrEmpty(NameContains))
			{
				string n = o.Name ?? "";
				if (n.IndexOf(NameContains, StringComparison.OrdinalIgnoreCase) < 0
					&& !string.Format("0x{0:X3}", o.Item).Equals(NameContains, StringComparison.OrdinalIgnoreCase))
					return false;
			}
			return true;
		}

		static bool Check(Want w, bool v) { return w == Want.Any || (w == Want.Yes) == v; }

		public List<ObjectView> Apply(IEnumerable<ObjectView> all)
		{
			var r = new List<ObjectView>();
			foreach (var o in all) if (Matches(o)) r.Add(o);
			return r;
		}
	}
}
