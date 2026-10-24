using System;

namespace UltimateEditor.Games
{
	/// <summary>
	/// UW2's items: names and the editor's object types. Names are the reference's
	/// uw2/src/include/items.h, which are the game's own (STRINGS.PAK block 4) with the article
	/// dropped; ids with no name there are null. Types follow the item classes (items.h),
	/// starting from what UW1 has at the same id and changing what UW2 puts elsewhere.
	/// </summary>
	public static class Uw2Items
	{
		static readonly string[] names =
		{
			"Hand axe", "Battle axe", "Axe", "Dagger", "Shortsword", "Longsword", "Broadsword", "Cudgel",
			"Light mace", "Mace", "Jewelled dagger", "Jeweled axe", "Black sword", "Jeweled sword", "Jeweled mace", "Fist",
			"Sling stone", "Crossbow bolt", "Arrow", "Skull", "Fireball", "Lightning bolt", "Acid", "Magic arrow",
			"Sling", "Bow", "Crossbow", "Homing dart", "Snowball", "Fireball", "Satellite", "Jeweled bow",
			"Leather vest", "Mail shirt", "Breastplate", "Leather leggings", "Mail leggings", "Plate leggings", "Leather gloves", "Chain gauntlets",
			"Plate gauntlets", "Leather boots", "Chain boots", "Plate boots", "Leather cap", "Chain cowl", "Helmet", "Pair of swamp boots",
			"Crown", "Crown", "Crown", "Fraznium gauntlets", "Fraznium circlet", "Guardian signet ring", "Strange artifact", "Copper ring",
			"Gold ring", "Silver ring", "Red ring", "Tower shield", "Wooden shield", "Small shield", "Buckler", "Jeweled shield",
			"Rotworm", "Cave bat", "Vampire bat", "Giant tan rat", "Giant grey rat", "Flesh slug", "Acid slug", "Mongbat",
			"Skeleton", "Goblin", "Goblin", "Imp", "Giant spider", "Lurker", "Bloodworm", "Stickman",
			"White worm", "Snow cat", "Yeti", "Headless", "Talorid", "Ghost", "Wolf spider", "Trilkhun",
			"Brain creature", "Deep lurker", "Dread spider", "Human", "Great troll", "Spectre", "Hordling", "Earth golem",
			"Fire elemental", "Ice golem", "Dire ghost", "Reaper", "Despoiler", "Metal golem", "Haunt", "Dire reaper",
			"Destroyer", "Liche", "Liche", "Liche", "Human", "Vorz", "Fighter", "Gazer",
			"Human", "Human", "Human", "Human", "Human", "Human", "Human", "Human",
			"Human", "Human", "Human", "Human", null, null, "Human", "Adventurer",
			"Sack", "Open sack", "Pack", "Open pack", "Box", "Open box", "Pouch", "Open pouch",
			"Map case", "Open map case", "Gold coffer", "Open gold coffer", "Key ring", "Quiver", "Bowl", "Rune bag",
			"Lantern", "Torch", "Candle", "Light-sphere", "Lit lantern", "Lit torch", "Lit candle", "Lit light-sphere",
			"Wand", "Wand", "Wand", "Wand", "Broken wand", "Broken wand", "Broken wand", "Broken wand",
			"Coin", "Storage crystal", "Ruby", "Red gem", "Small blue gem", "Large blue gem", "Sapphire", "Emerald",
			"Black pearl", "Goblet", "Sceptre", "Black stone", "White stone", "Grey stone", "Delgnigzator", "Pocketwatch",
			"Piece of meat", "Piece of meat", "Piece of cheese", "Apple", "Ear of corn", "Loaf of bread", "Fish", "Popcorn",
			"Pastry", "Mushroom", "Honeycomb", "Bottle of ale", "Bottle of water", "Bottle of wine", "Meat-on-a-stick", "Nutritious wafer",
			"Plant", "Grass", "Skull", "Skull", "Bone", "Bone", "Pile of bones", "Broken dagger",
			"Broken sword", "Broken axe", "Broken mace", "Broken shield", "Piece of wood", "Piece of wood", "Plant", "Plant",
			"Pile of debris", "Pile of debris", "Lump of wax", "Stalactite", "Plant", "Icicle", "Pile of debris", "Anvil",
			"Pole", "Plant", "Plant", "Rubble", "Pile of wood chips", "Pile of bones", "Blood stain", "Blood stain",
			"Runestone", "Black potion", "Purple potion", "Yellow potion", "Green potion", "Red potion", "Colorless potion", "Brown potion",
			"An stone", "Bet stone", "Corp stone", "Des stone", "Ex stone", "Flam stone", "Grav stone", "Hur stone",
			"In stone", "Jux stone", "Kal stone", "Lor stone", "Mani stone", "Nox stone", "Ort stone", "Por stone",
			"Quas stone", "Rel stone", "Sanct stone", "Tym stone", "Uus stone", "Vas stone", "Wis stone", "Ylem stone",
			"Curious implement", "Lockpick", "Key", "Key", "Key", "Key", "Key", "Key",
			"Key", "Key", "Key", "Key", "Key", "Key", "Key", "Lock",
			"Eyeball", "Horn", "Pearl-tipped rod", "Black eggshell", "Plant", "Serpent statue", "Bottle", "Amethyst rod",
			"Blackrock gem", "Blackrock gem", "Blackrock gem", "Blackrock gem", "Blackrock gem", "Blackrock gem", "Blackrock gem", "Blackrock gem",
			"Spell", "Bedroll", "Orb", "Mandolin", "Flute", "Leeches", "Moonstone", "Force field",
			"Rock hammer", "Resilient sphere", "Campfire", "Fishing pole", "Thread", "Oil flask", "Fountain", "Banner",
			"Book", "Book", "Book", "Book", "Scroll", "Scroll", "Scroll", "Scroll",
			"Book", "Bit of a map", "Map", "Dead plant", "Dead plant", "Bottle", "Stick", "Resilient sphere",
			"Door", "Door", "Door", "Door", "Door", "Door", "Portcullis", "Secret door",
			"Open door", "Open door", "Open door", "Open door", "Open door", "Open door", "Open portcullis", "Secret door",
			"Bench", "Arrow", "Crossbow bolt", "Large boulder", "Large boulder", "Boulder", "Small boulder", "Shrine",
			"Table", "Beam", "Moongate", "Barrel", "Chair", "Chest", "Nightstand", "Lotus turbo esprit",
			"Pillar", "Lever", "Switch", "Painting", "Bridge", "Gravestone", "Writing", "Bed",
			"Large blackrock gem", "Shelf", null, null, null, "Force field", "Special tmap obj", "Special tmap obj",
			"Button", "Button", "Button", "Switch", "Switch", "Lever", "Pull chain", "Pull chain",
			"Button", "Button", "Button", "Switch", "Switch", "Lever", "Pull chain", "Pull chain",
			"Damage trap", "Teleport trap", "Arrow trap", "Hack trap", "Special effects trap", "Change terrain trap", "Spelltrap", "Create object trap",
			"Door trap", "Ward trap", "Skill trap", "Delete object trap", "Inventory trap", "Set variable trap", "Check variable trap", "Null trap",
			"Text string trap", "Experience trap", "Jump trap", "Change from trap", "Change to trap", "Oscillator trap", "Proximity trap", "Pit trap",
			"Bridge trap", null, null, null, null, null, "Flam rune", "Tym rune",
			"Move trigger", "Pick up trigger", "Use trigger", "Look trigger", "Pressure trigger", "Pressure release trigger", "Enter trigger", "Exit trigger",
			"Unlock trigger", "Timer trigger", "Open trigger", "Close trigger", "Scheduled trigger", null, null, null,
			"Move trigger", "Pick up trigger", "Use trigger", "Look trigger", "Pressure trigger", "Pressure release trigger", "Enter trigger", "Exit trigger",
			"Unlock trigger", "Timer trigger", "Open trigger", "Close trigger", "Scheduled trigger", null, null, null,
			"Blood", "Mist cloud", "Explosion", "Explosion", "Explosion", "Lightning", "Splash", "Spacey twinkles",
			"Smoke", "Fountain", "Frost", "Flash", "Lightning", "Wisp", "Vapor trail", "Moving door",
			null, null, null, null, null, null, null, null,
			null, null, null, null, null, null, null, null,
			null, null, null, null, null, null, null, null,
			null, null, null, null, null, null, null, null,
			null, null, null, null, null, null, null, null,
			null, null, null, null, null, null, null, null,
		};

		public static string Name(int id)
		{
			if (id < 0 || id >= names.Length) return "Unknown";
			return names[id] ?? string.Format("Unknown 0x{0:X3}", id);
		}

		public static ObjectType Type(int id)
		{
			if (id < 0x0F) return ObjectType.Melee;
			if (id == 0x0F) return ObjectType.Unknown;                 // fist
			if (id <= 0x12) return ObjectType.Projectile;
			if (id <= 0x17) return ObjectType.SpecialProjectile;        // skull, fireball, lightning, acid, magic arrow
			if (id <= 0x1A) return ObjectType.Ranged;
			if (id <= 0x1E) return ObjectType.SpecialProjectile;        // homing dart, snowball, fireball, satellite
			if (id == 0x1F) return ObjectType.Ranged;
			if (id <= 0x2F) return ObjectType.Armour;
			if (id <= 0x32) return ObjectType.Baubles;                  // crowns
			if (id == 0x33) return ObjectType.Armour;                   // fraznium gauntlets
			if (id <= 0x35) return ObjectType.Baubles;                  // circlet, Guardian signet ring
			if (id == 0x36) return ObjectType.SpecialBauble;            // strange artifact
			if (id <= 0x3A) return ObjectType.Baubles;                  // rings
			if (id <= 0x3F) return ObjectType.Shield;
			if (id <= 0x7E) return names[id] != null ? ObjectType.Monster : ObjectType.Unknown;
			if (id == 0x7F) return ObjectType.Unknown;                  // the adventurer
			if (id <= 0x8F) return ObjectType.Container;
			if (id <= 0x97) return ObjectType.Light;
			if (id <= 0x9B) return ObjectType.Wand;
			if (id <= 0x9F) return ObjectType.Countable;                // broken wands
			if (id == 0xA0) return ObjectType.Gold;
			if (id <= 0xAF) return ObjectType.Treasure;
			if (id <= 0xBA) return ObjectType.Food;
			if (id <= 0xBD) return ObjectType.Drink;                    // ale, water, wine
			if (id <= 0xBF) return ObjectType.Food;
			switch (id)
			{
				case 0xC0: case 0xC1: return ObjectType.Unpickable;
				case 0xC2: case 0xC3: case 0xC4: case 0xC5: case 0xC6: return ObjectType.Bones;
				case 0xD0: case 0xD1: case 0xD6: case 0xD7: case 0xD9: case 0xDA: case 0xDB: case 0xDC: case 0xDD: case 0xDE: case 0xDF: return ObjectType.Unpickable;
				case 0xD3: case 0xD4: case 0xD5: return ObjectType.CeilingHugger;   // stalactite, hanging plant, icicle
			}
			if (id <= 0xDF) return ObjectType.Countable;                // broken weapons, wood, wax, pole
			if (id == 0xE0) return ObjectType.Runestone;
			if (id <= 0xE7) return ObjectType.Potion;
			if (id <= 0xFF) return ObjectType.Runestone;
			if (id == 0x100) return ObjectType.Uncountable;             // curious implement
			if (id == 0x101) return ObjectType.Countable;               // lockpick
			if (id <= 0x10E) return ObjectType.Key;
			if (id == 0x10F) return ObjectType.Lock;
			if (id <= 0x11F) return ObjectType.Uncountable;             // quest items, blackrock gems
			switch (id)
			{
				case 0x120: return ObjectType.Spell;
				case 0x122: case 0x127: case 0x12A: case 0x12E: case 0x12F: return ObjectType.Unpickable;   // orb, force field, campfire, fountain, banner
				case 0x126: return ObjectType.Uncountable;                      // moonstone
			}
			if (id <= 0x12F) return ObjectType.Countable;
			if (id <= 0x133 || id == 0x138) return ObjectType.Book;
			if (id <= 0x137) return ObjectType.Scroll;
			if (id <= 0x13A) return ObjectType.Map;
			if (id <= 0x13F) return ObjectType.Countable;               // dead plants, bottle, stick, sphere
			if (id <= 0x14F) return ObjectType.Door;
			if (id == 0x15B || id == 0x15D) return ObjectType.ContainerModel;
			if (id <= 0x160) return ObjectType.Model;
			if (id <= 0x162) return ObjectType.DialLever;
			if (id <= 0x164) return ObjectType.Model;
			if (id == 0x165) return ObjectType.Grave;
			if (id == 0x166) return ObjectType.Writing;
			if (id <= 0x169) return ObjectType.Model;
			if (id == 0x16E || id == 0x16F) return ObjectType.VerticalTexture;
			if (id <= 0x16F) return ObjectType.Unknown;
			if (id <= 0x17F) return ObjectType.Lever;
			if (id == 0x188) return ObjectType.DoorTrap;
			if (id <= 0x198 || id == 0x19E || id == 0x19F) return ObjectType.Trap;
			if (id <= 0x19F) return ObjectType.Unknown;
			if ((id >= 0x1A0 && id <= 0x1AC) || (id >= 0x1B0 && id <= 0x1BC)) return ObjectType.Trigger;
			if (id <= 0x1BF) return ObjectType.Unknown;
			if (id <= 0x1CF) return ObjectType.Animated;
			return ObjectType.Unknown;
		}
	}
}
