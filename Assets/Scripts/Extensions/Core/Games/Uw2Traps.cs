using System;
namespace UltimateEditor.Games
{
	/// <summary>
	/// What UW2's traps and triggers do with their fields (reference uw2/src/event/TRIGGER.C,
	/// UseTrap and UseTrigger). Used by the properties panel to describe a UW2 trap instead of
	/// showing UW1's editors where the meaning differs.
	/// </summary>
	public static class Uw2Traps
	{
		/// <summary>
		/// Traps whose fields mean the same as in UW1, so the UW1 editors apply. Differences the
		/// UW1 panels do not show (Describe says them): the teleport (0x181) also reads fine x
		/// bits 0-2 and the heading; the door trap (0x188) swaps the lock only when owner is not
		/// 0, and only then ends the chain (UW1 always swaps it and always ends the chain); the
		/// damage trap (0x180) poisons when owner is set, in both games (WORLDEV.C whack_thing).
		/// </summary>
		public static bool SharedWithUw1(int id)
		{
			return id == 0x180 || id == 0x181 || id == 0x188 || id == 0x18B;
		}

		/// <summary>
		/// Ids whose UW1 create menu sets the same fields UW2 reads: the shared traps and the
		/// move, pick up, use and look triggers (0x1A0-0x1A3: target square in quality and
		/// owner, then the linked trap). UW1's 0x1A4-0x1A6 are step on, open and unlock; UW2's
		/// are pressure, pressure release and enter.
		/// </summary>
		public static bool HasUw1CreateMenu(int id)
		{
			return SharedWithUw1(id) || (id >= 0x1A0 && id <= 0x1A3);
		}

		public static string Describe(int id)
		{
			switch (id)
			{
				case 0x180: return "Damage trap: whoever set it off takes quality damage (type 4). With owner set it poisons the player at strength quality instead (4 bits, so 1-15; no effect if he resists poison or is already more poisoned); anything else still takes plain damage. It never heals.";
				case 0x181: return "Teleport trap: moves the player (only the player) to square (quality, owner) of level z (0: this level); x or y 0x3F skips the search for a free square, and on this level a full 9x9 box makes it fail. Fine x bit 0: arrive near the ceiling and fall; bit 1: face the trap's heading; bit 2: the player loses this level's map. The UW1 panel below cannot set fine x or the heading (Tools > Object fields). Also: leaving the jail cell is an escape (quests 112, 124), the Void allows only world 8, and leaving the arena runs from the fight.";
				case 0x182: return "Arrow trap: fires missile item quality * 32 + owner (owner bit 5 overlaps quality bit 0) from the target square, at the trap's fine position and z, along its heading.";
				case 0x183: return "Hack trap: a special case chosen by quality (do_trap_hack); UW2's cases differ from UW1's.";
				case 0x184: return "Special effects trap: quality 2 plays sound owner + 0x64, 4 shakes the screen at strength owner * 2, 5-8 flash the screen in palette colour owner + 0x40 * (8 - quality); other qualities do nothing.";
				case 0x185: return "Change terrain trap: changes the (fine x + 1) by (fine y + 1) tiles from the target square: wall texture = owner (63 keeps), floor = quality >> 1 (15 or more keeps), height = z >> 3 (15 keeps), type = heading * 2 + (quality & 1) (15 is read as 10; 10 or more keeps). Objects rise and fall with the floor; a water floor, a solid tile or (half the time) lava removes static objects there.";
				case 0x186: return "Spell trap: casts spell class quality, minor owner (do_spell), with the trap as the caster and whoever set the chain off as the target; anti-magic is tested at the target square.";
				case 0x187: return "Create object trap: copies its linked object (the template, at its own fine position and z) onto the square with chance (63 - quality) in 63; only the template's first content is copied, and the chain always ends. A creature is not made while a temporary object is within four squares of the template's home, nor in world 6 once quest 7 is set. A linked adventurer (0x7F) is a template for a random hostile, temporary monster scaled by the level, the castle plot and the player's level. With trigger count 0 and a creature template it is also a wandering-monster spawner.";
				case 0x188: return "Door trap: opens (quality 1), closes (2) or toggles (3) the door on the square the chain runs at (a moving door is turned round), and the door's own open and close triggers fire. With owner set and a door found, its lock is removed and a copy of the trap's linked lock put on (no link: unlocked), and the chain ends; with owner 0 the lock is untouched and the chain goes on.";
				case 0x189: return "Ward trap (a rune of warding): hits whoever set the chain off (the player too, whose item & 0xF is 15) when its item & 0xF equals quality, or anyone for 0x3F, for 3 + rand * Casting (the player's), and tells the player. The chain always ends. Nothing in UW2 lays wards.";
				case 0x18A: return "Skill trap (condition): passes when the check passes. Quality 0-2 an attribute (strength, dexterity, intelligence), 3 the value 15, 4 and up skill quality - 4; owner * 3 the difficulty; heading 1 rolls a skill check, otherwise a plain comparison. On a pass the trap or trigger after its first linked object runs.";
				case 0x18B: return "Delete object trap: removes its linked object from square (quality, owner); a trap or trigger goes through trap_obj_del, which keeps trigger counts right. The chain ends.";
				case 0x18C: return "Inventory trap (condition): runs the alternative chain when the player lacks item quality * 32 + owner (fine x: it must be worn; z: at least that many).";
				case 0x18D: return "Set variable trap: numbered variable z + (fine x << 7), operation heading, value (quality << 6) + owner; fine y set also counts the 'changed' clock. Variables 0-0xFF keep only 8 bits. For quest flags (0x100-0x17F) operation 5 toggles, 1 leaves the flag, and any other sets it to (value > 0). Changing an X clock does not run the schedules by itself (hack 37 does).";
				case 0x18E: return "Check variable trap (condition): reads heading + 1 numbered variables from z | (fine x & 3) << 7; with fine x bit 2 it sums them, otherwise it packs their low 3 bits (so a single variable is compared by its low 3 bits). Compared with ((quality << 5 | owner) << 3) | fine y: equal continues on the link, different runs the alternative chain (only if the trap has a link).";
				case 0x18F: return "Null trap: does nothing; passes on along its link.";
				case 0x190: return "Text string trap: prints string quality * 32 + (owner & 31) of block 9. With owner bit 5 set it prints only while the player's flag b62_5 is set, which no code sets, so it never prints.";
				case 0x191: return "Experience trap: gives (quality * 8 + (owner & 7) - 256) << (owner >> 3) experience, which can be negative (quality 32 or more for a gain). Gains are halved, and halved again when the player is over-levelled for the world; a loss stops at 0.";
				case 0x192: return "Jump trap: acts on the moving object (put it under a move trigger). If its horizontal speed is at least heading * 0x2F: vertical speed quality * 0x2F / 2 for the player (quality * 0x8D / 4 for anything else); owner set stops the horizontal motion, otherwise it is halved.";
				case 0x193: return "Change from trap: changes tiles, not objects. Over fine x by fine y squares from the target square (fine y 0: the same as fine x; fine x 0: the whole map), each tile whose wall (quality), floor (heading + 8 * z bit 4), type (owner) and height (z & 15) match these values gets the linked change to trap's values. A value at its limit (wall 63, floor 15, type 10, height 15) matches anything here and leaves the field in the change to trap. Its link must be the change to trap.";
				case 0x194: return "Change to trap: the values a change from trap writes (same fields). It does nothing itself; the chain continues through its link.";
				case 0x195: return "Oscillator trap: steps the target square's height, floor or wall (fine x bits 1-2) by one, up or down (fine x bit 0), between quality and owner, turning at the ends; the direction is stored back in the trap's fine x. On level 0x44 it also checks for the sunken moongate.";
				case 0x196: return "Proximity trap (condition): continues when who is within x..x+quality, y..y+owner of the target square (towards +x and +y only) and on the right side of the trap's height; otherwise runs the alternative chain.";
				case 0x197: return "Pit trap: a tile at height 0 fills to height fine x + 8 * fine y with floor owner (above 15 makes it solid); any other tile drops to height 0 with floor quality.";
				case 0x198: return "Bridge trap: owner >> 4 = 1 lays bridges (0x164, at the trap's z and heading, flags owner & 15), 2 removes them, over quality squares from the target square; heading 0 +y, 2 +x, 4 -y, 6 -x (rounded down to even and written back into the trap).";
				case 0x19E: return "Flam rune: a rune of fire left on the floor.";
				case 0x19F: return "Tym rune: a rune of stasis left on the floor.";
			}
			if ((id >= 0x1A0 && id <= 0x1AC) || (id >= 0x1B0 && id <= 0x1BC))
				return "Trigger: fires its linked trap at square (quality, owner), where the trap chain runs (not where the trigger lies), for its mode (OBJECTS.DAT's trigger table; 0x1AD-0x1AF and 0x1BD-0x1BF are move triggers). The player sets it off with flag 11 (a look trigger with z above 0 needs a Search check against z); creatures with the enchanted bit (the MAJOR_RECT test looks at the trigger itself and never applies); objects and nobody (scheduled triggers, conversations' gronk_door, auto-closing doors, hack 18) with flag 9. Pressure and timer triggers always act as the player. Only 0x1Bx triggers can be found and disarmed. Without flag 10 the trap chain is deleted after it runs, even if it did nothing.";
			return null;
		}

		static readonly string[] skills = { "Attack", "Defense", "Unarmed", "Sword", "Axe", "Mace", "Missile", "Mana", "Lore", "Casting",
			"Traps", "Search", "Track", "Sneak", "Repair", "Charm", "Picklock", "Acrobat", "Appraise", "Swimming" };

		/// <summary>do_trap_hack's cases by quality (uw2 event/TRIGGER.C, mostly in WORLDEV.C).</summary>
		static readonly System.Collections.Generic.Dictionary<int, string> hacks = new System.Collections.Generic.Dictionary<int, string>
		{
			{ 2, "the crystal ball (a view from the target square at the trap's fine x/y, z and heading)" },
			{ 3, "the eight-position switch: the target tile's height from z + 8 * the switch position" },
			{ 4, "the eight-position switch: the linked object's z = z + 8 * the switch position" },
			{ 5, "a crime against race owner (0 nothing)" },
			{ 10, "the best weapon for the arena" }, { 11, "fraznium" }, { 12, "a standing wave (changes its own owner)" }, { 14, "a cycling floor" },
			{ 17, "breaking ice on the player's tile" },
			{ 18, "flipping the switch on the square to position owner and firing its use triggers (as nobody: they need flag 9)" },
			{ 19, "resetting arrow pillars" }, { 20, "toggling pillars" },
			{ 21, "toggling the linked object's height (z or below: z + owner; else z), and a second object of its group of five" },
			{ 22, "toggling the linked object's height (z or below: z + owner; else z)" },
			{ 23, "set the linked object's owner" }, { 24, "graffiti" }, { 25, "skup_ductosnore" }, { 26, "a force field" }, { 27, "set the linked object's quality" },
			{ 28, "set the linked object's owner" }, { 29, "random switch flips over 5 by 2 squares (no triggers fire)" },
			{ 30, "running from the Pits of Carnage arena" }, { 31, "rescuing pit fighters from lava (maybe_cheat_arena_fire)" },
			{ 32, "qbert" }, { 33, "redeeming bottles" }, { 34, "the courtyard" }, { 35, "recharging light bulbs" }, { 36, "the castle NPCs' moves" },
			{ 37, "bringing the schedules up to date" }, { 38, "spoiling cure potions" }, { 39, "showing or hiding the linked object" },
			{ 40, "a vending machine" }, { 41, "a vending machine" }, { 42, "a vending machine" }, { 43, "changing a critter's goal" }, { 44, "sleep" },
			{ 45, "marking owner by owner squares unseen on the automap" }, { 54, "the blackrock gem" }, { 55, "the blackrock gem" },
			{ 62, "setting the goal of the critter that set it off" },
		};

		static readonly string[] ops = { "add", "subtract", "set to", "and", "or", "xor", "shift left by", "count up while equal to" };

		/// <summary>
		/// Operation op with value on numbered variable n, as set_numbered_variable (TRIGGER.C)
		/// does it: a quest flag (0x100-0x17F) is not changed arithmetically: op 5 toggles it,
		/// op 1 leaves it, and any other op sets it to (value > 0), so "add 0" clears it.
		/// </summary>
		public static string Operation(int n, int op, int value)
		{
			op &= 7;
			if (n >= 0x100 && n < 0x180)
				return op == 5 ? "toggle" : op == 1 ? "leave unchanged (op 1)" : string.Format("set to {0} (op {1}, value {2})", value > 0 ? 1 : 0, op, value);
			return ops[op] + " " + value;
		}

		/// <summary>A numbered variable (TRIGGER.C set_numbered_variable): 0-0xFF the player's
		/// vars, 0x100-0x17F quest flags 0-127, 0x180-0x18F quest bytes (quest 128 on), 0x190-0x19F X clocks.</summary>
		public static string VariableName(int n)
		{
			if (n < 0x100) return "variable " + n;
			if (n < 0x180) return string.Format("quest flag {0} (get_quest/set_quest {0}), stored as bit {1} of quests[{2}]", n - 0x100, (n - 0x100) & 3, (n - 0x100) >> 2);
			if (n < 0x190) return "quest " + (128 + n - 0x180);
			if (n < 0x1A0) return "X clock " + (n - 0x190);
			return "nothing (reads 0)";
		}

		/// <summary>
		/// What this trap will do with its present fields, in numbers (uw2 event/TRIGGER.C
		/// UseTrap). Null for ids it does not cover. <paramref name="text"/> looks up STRINGS.PAK
		/// block 9 for the text string trap; it may be null.
		/// </summary>
		public static string Decode(int id, int quality, int owner, int z, int heading, int fineX, int fineY, bool isQuantity, int link, Func<int, string> text = null)
		{
			switch (id)
			{
				case 0x180:
				{
					// WORLDEV.C whack_thing: a negative damage (owner set) poisons the player; the
					// 7-in-10 roll UseTrap makes is passed on but unused, and the type is always 4
					if (owner == 0)
						return quality == 0 ? "Does nothing (0 damage)." : "Deals " + quality + " damage (type 4) to whoever set it off.";
					int strength = quality & 0xF;
					if (quality == 0)
						return "Does nothing (strength 0).";
					return (strength == 0 ? "Poisons the player at strength " + quality + ", stored in 4 bits as 0: no poison." :
						"Poisons the player at strength " + strength + (quality > 15 ? " (" + quality + " in 4 bits)" : "") + ", if he is not already more poisoned or resisting.") +
						" Anything else takes " + quality + " damage.";
				}
				case 0x181:
				{
					string where = z == 0 ? "this level" : "level " + z;
					string s = string.Format("Teleports the player (no one else) to square ({0}, {1}) of {2}.", quality, owner, where);
					if (quality == 0x3F || owner == 0x3F) s += " No search for a free square.";
					if ((fineX & 1) != 0) s += " Arrives near the ceiling and falls.";
					if ((fineX & 2) != 0) s += " Faces heading " + heading + ".";
					if ((fineX & 4) != 0) s += " The player loses this level's map.";
					return s;
				}
				case 0x182:
				{
					int item = quality << 5 | owner;
					return string.Format("Fires item 0x{0:X3} ({1}) in the trap's heading.", item, Uw2Items.Name(item & 0x1FF));
				}
				case 0x183:
				{
					string h;
					return hacks.TryGetValue(quality, out h) ? "Hack " + quality + ": " + h + "." : "Hack " + quality + ": no case (does nothing).";
				}
				case 0x184:
					// WORLDEV.C do_sfx
					if (quality == 2) return "Plays sound " + (owner + 0x64) + ".";
					if (quality == 4) return "Shakes the screen at strength " + (owner * 2) + ".";
					if (quality >= 5 && quality <= 8) return "Flashes the screen in palette colour " + (owner + 0x40 * (8 - quality)) + ".";
					return "Effect " + quality + ": does nothing.";
				case 0x185:
				{
					int type = heading * 2 + (quality & 1);
					if (type == 15) type = 10;
					return string.Format("Over {0} by {1} squares from the target: wall texture {2}, floor {3}, height {4}, tile type {5} (wall 63, floor 15 or more, height 15 and type 10 or more mean unchanged).",
						fineX + 1, fineY + 1, owner, quality >> 1, z >> 3, type);
				}
				case 0x186:
					return string.Format("Casts spell class {0}, minor {1} (do_spell), at whoever set it off.", quality, owner);
				case 0x187:
					return isQuantity || link == 0 ? "Creates nothing (no linked object)." :
						string.Format("Creates a copy of object {0} with chance {1} in 63; the chain ends.", link, Math.Max(0, 63 - quality));
				case 0x188:
				{
					string act = quality == 1 ? "Opens" : quality == 2 ? "Closes" : quality == 3 ? "Toggles" : "Leaves";
					return act + " the door on the target square" + (owner == 0 ? "; the chain goes on." :
						isQuantity || link == 0 ? ", removes its lock, and ends the chain." : ", replaces its lock by a copy of the linked lock, and ends the chain.");
				}
				case 0x189:
					return (quality == 0x3F ? "Whoever" : "Whoever with item & 0xF = " + quality + (quality == 15 ? " (the player too)" : "")) + " set it off takes 3 + Casting-scaled damage; the chain ends.";
				case 0x18A:
				{
					string what = quality < 3 ? new[] { "Strength", "Dexterity", "Intelligence" }[quality] : quality == 3 ? "the value 15" :
						quality - 4 < skills.Length ? skills[quality - 4] : "skill " + (quality - 4);
					return string.Format("{0} {1} against {2}; on a pass the alternative chain runs, otherwise the link.", heading == 1 ? "Skill check of" : "Compares", what, owner * 3);
				}
				case 0x18B:
					return string.Format("Removes linked object {0} from square ({1}, {2}).", link, quality, owner);
				case 0x18C:
				{
					int item = quality << 5 | owner;
					string s = string.Format("Unless the player has item 0x{0:X3} ({1})", item, Uw2Items.Name(item));
					if (fineX != 0) s += " worn";
					if (z > 0) s += ", at least " + z;
					return s + ", the alternative chain runs.";
				}
				case 0x18D:
				{
					int v = z + (fineX << 7);
					int value = (quality << 6) + owner;
					string s = string.Format("{0}: {1}.", VariableName(v), Operation(v, heading, value));
					if (fineY != 0) s += " Counts X clock 15 (changed) up.";
					return s;
				}
				case 0x18E:
				{
					int first = z | (fineX & 3) << 7, last = first + heading;
					int want = (quality << 5 | owner) << 3 | fineY;
					string vars = first == last ? VariableName(first) : VariableName(first) + " to " + VariableName(last);
					string how = (fineX >> 2) != 0 ? "the sum of " : first == last ? "the low 3 bits of " : "the low 3 bits, packed, of ";
					return string.Format("Unless {0}{1} equals {2}, the alternative chain runs.", how, vars, want);
				}
				case 0x18F:
					return "Does nothing; the link runs.";
				case 0x190:
				{
					int n = quality << 5 | owner & 0x1F;
					string str = text != null ? text(n) : null;
					string s = string.Format("Prints string {0} of block 9", n);
					if ((owner >> 5) != 0) s += " (only while player flag b62_5 is set)";
					return s + (str != null ? ": \"" + str + "\"" : ".");
				}
				case 0x191:
				{
					int exp = ((quality << 3) + (owner & 7) - 0x100) * (1 << (owner >> 3));
					return "Gives " + exp + " experience.";
				}
				case 0x192:
					return string.Format("If the moving object's speed is at least {0}: jumps with vertical speed {1} (the player) or {2} (anything else); horizontal motion {3}.",
						heading * 0x2F, quality * 0x2F / 2, quality * 0x8D / 4, owner != 0 ? "stops" : "is halved");
				case 0x195:
				{
					int mode = (fineX & 6) >> 1;
					string what = mode == 0 ? "height" : mode == 1 ? "floor" : mode == 2 ? "wall texture" : "nothing";
					return string.Format("Steps the target square's {0} {1} by one between {2} and {3}.", what, (fineX & 1) != 0 ? "up" : "down", quality, owner);
				}
				case 0x196:
					return string.Format("Unless who set it off is within {0} by {1} squares from the target{2}, the alternative chain runs.",
						quality + 1, owner + 1, fineX != 0 && fineY != 0 ? "" : fineX == 0 && fineY == 0 ? " (no height passes)" : fineX == 0 ? " at or below height " + z : " above height " + z);
				case 0x197:
					return string.Format("An open pit fills to height {0} with floor {1}; a filled one opens with floor {2}.", fineX + ((fineY & 7) << 3), owner, quality);
				case 0x198:
				{
					string act = (owner >> 4) == 1 ? "Lays" : (owner >> 4) == 2 ? "Removes" : "Does nothing to";
					return string.Format("{0} a row of {1} bridges at height {2}, heading {3}.", act, quality, z, heading & 6);
				}
			}
			return null;
		}
	}
}
