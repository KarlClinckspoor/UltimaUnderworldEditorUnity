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
		/// <summary>Traps whose fields mean the same as in UW1, so the UW1 editors apply.</summary>
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
				case 0x180: return "Damage trap: hits whoever set it off for quality points; heals instead when owner is set.";
				case 0x181: return "Teleport trap: moves who to square (quality, owner) of level z (0: this level). Fine x bits choose the facing and whether the player loses the level's map.";
				case 0x182: return "Arrow trap: fires the trap's missile.";
				case 0x183: return "Hack trap: a special case chosen by quality (do_trap_hack); UW2's cases differ from UW1's.";
				case 0x184: return "Special effects trap: plays an effect.";
				case 0x185: return "Change terrain trap: changes tiles from the target square over fine x by fine y: wall texture = owner, floor = quality >> 1, height = z >> 3, type = heading * 2 + (quality & 1) (15 is read as 10).";
				case 0x186: return "Spell trap: casts a spell (inanimate_spell).";
				case 0x187: return "Create object trap: copies its linked object onto the square with chance (63 - quality) in 63, not when another created object is within four squares. A linked adventurer (0x7F) is a template for a random monster scaled by the level, the castle plot and the player's level.";
				case 0x188: return "Door trap: opens (quality 1), closes (2) or toggles (3) the door on the square; with owner set, replaces its lock by a copy of the trap's linked lock.";
				case 0x189: return "Ward trap (a rune of warding): hits the critter of class quality (0x3F any) that steps on it for 3 + rand * Casting, and tells the player.";
				case 0x18A: return "Skill trap (condition): passes when the check passes. Quality 0-2 an attribute (strength, dexterity, intelligence), 3 the value 15, 4 and up skill quality - 4; owner * 3 the difficulty; heading 1 rolls a skill check, otherwise a plain comparison. On a pass the trap or trigger after its first linked object runs.";
				case 0x18B: return "Delete object trap: removes its linked object from square (quality, owner).";
				case 0x18C: return "Inventory trap (condition): runs the alternative chain when the player lacks item quality * 32 + owner (fine x: it must be worn; z: at least that many).";
				case 0x18D: return "Set variable trap: numbered variable z + (fine x << 7), operation heading, value (quality << 6) + owner; fine y set also counts the 'changed' clock.";
				case 0x18E: return "Check variable trap (condition): runs the alternative chain when the numbered variables do not match.";
				case 0x18F: return "Null trap: does nothing; passes on along its link.";
				case 0x190: return "Text string trap: prints string quality * 32 + owner of block 9 (if owner's high bit is set, only while the player's flag b62_5 is set).";
				case 0x191: return "Experience trap: gives (quality * 8 + (owner & 7) - 256) << (owner >> 3) experience, which can be negative.";
				case 0x192: return "Jump trap: set_jmp(quality, owner, heading).";
				case 0x193: return "Change from trap: with a change to trap, swaps objects of one kind for another.";
				case 0x194: return "Change to trap: the target of a change from trap.";
				case 0x195: return "Oscillator trap: steps a tile's height, floor or wall by one between quality and owner, reversing at the ends.";
				case 0x196: return "Proximity trap (condition): runs the alternative chain when who is outside the rectangle (quality, owner) from the square, or on the wrong side of the trap's height.";
				case 0x197: return "Pit trap: opens or fills a pit.";
				case 0x198: return "Bridge trap: lays or removes a row of bridges.";
				case 0x19E: return "Flam rune: a rune of fire left on the floor.";
				case 0x19F: return "Tym rune: a rune of stasis left on the floor.";
			}
			if ((id >= 0x1A0 && id <= 0x1AC) || (id >= 0x1B0 && id <= 0x1BC))
				return "Trigger: fires its linked trap at square (quality, owner) for its mode (OBJECTS.DAT's trigger table). The player sets it off with flag 11 (look triggers above the floor need a Search check against z), creatures with the enchanted bit (not for a door's trigger), other objects with flag 9. Without flag 10 the trap chain is deleted after it runs.";
			return null;
		}

		static readonly string[] skills = { "Attack", "Defense", "Unarmed", "Sword", "Axe", "Mace", "Missile", "Mana", "Lore", "Casting",
			"Traps", "Search", "Track", "Sneak", "Repair", "Charm", "Picklock", "Acrobat", "Appraise", "Swimming" };

		/// <summary>do_trap_hack's cases by quality (uw2 event/TRIGGER.C, mostly in WORLDEV.C).</summary>
		static readonly System.Collections.Generic.Dictionary<int, string> hacks = new System.Collections.Generic.Dictionary<int, string>
		{
			{ 2, "the crystal ball" }, { 3, "the eight-position switch" }, { 4, "the eight-position switch" }, { 5, "a crime against owner (owner)" },
			{ 10, "the best weapon for the arena" }, { 11, "fraznium" }, { 12, "a standing wave" }, { 14, "a cycling floor" }, { 17, "breaking ice" },
			{ 18, "flipping switches" }, { 19, "resetting arrow pillars" }, { 20, "toggling pillars" }, { 21, "raising an object" }, { 22, "lowering an object" },
			{ 23, "set the linked object's owner" }, { 24, "graffiti" }, { 25, "skup_ductosnore" }, { 26, "a force field" }, { 27, "set the linked object's quality" },
			{ 28, "set the linked object's owner" }, { 29, "the switch puzzle" }, { 30, "running from the Pits of Carnage arena" }, { 31, "cheating in the arena" },
			{ 32, "qbert" }, { 33, "redeeming bottles" }, { 34, "the courtyard" }, { 35, "recharging light bulbs" }, { 36, "the castle NPCs' moves" },
			{ 37, "bringing the schedules up to date" }, { 38, "spoiling cure potions" }, { 39, "showing or hiding the linked object" },
			{ 40, "a vending machine" }, { 41, "a vending machine" }, { 42, "a vending machine" }, { 43, "changing a critter's goal" }, { 44, "sleep" },
			{ 45, "hiding terrain" }, { 54, "the blackrock gem" }, { 55, "the blackrock gem" }, { 62, "setting the goal of the critter that set it off" },
		};

		static readonly string[] ops = { "add", "subtract", "set to", "and", "or", "xor", "shift left by", "count up while equal to" };

		/// <summary>A numbered variable (TRIGGER.C set_numbered_variable): 0-0xFF the player's
		/// vars, 0x100-0x17F quest bits, 0x180-0x18F quest bytes (quest 128 on), 0x190-0x19F X clocks.</summary>
		public static string VariableName(int n)
		{
			if (n < 0x100) return "variable " + n;
			if (n < 0x180) return string.Format("quest bit {0} (bit {1} of quest {2})", n - 0x100, (n - 0x100) & 3, (n - 0x100) >> 2);
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
					return owner != 0 ? "Heals " + quality + " hit points." : "Deals " + quality + " damage (7 in 10: of type 2).";
				case 0x181:
				{
					string where = z == 0 ? "this level" : "level " + z;
					string s = string.Format("Teleports to square ({0}, {1}) of {2}.", quality, owner, where);
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
					return string.Format("Special effect {0}, argument {1}.", quality, owner);
				case 0x185:
				{
					int type = heading * 2 + (quality & 1);
					if (type == 15) type = 10;
					return string.Format("Over {0} by {1} squares from the target: wall texture {2}, floor {3}, height {4}, tile type {5} (63 / 15 / 0x3F mean unchanged where the game allows).",
						fineX + 1, fineY + 1, owner, quality >> 1, z >> 3, type);
				}
				case 0x186:
					return string.Format("Casts spell {0} with power {1}.", quality, owner);
				case 0x187:
					return isQuantity || link == 0 ? "Creates nothing (no linked object)." :
						string.Format("Creates a copy of object {0} with chance {1} in 63.", link, Math.Max(0, 63 - quality));
				case 0x188:
				{
					string act = quality == 1 ? "Opens" : quality == 2 ? "Closes" : quality == 3 ? "Toggles" : "Leaves";
					return act + " the door on the target square" + (owner != 0 ? ", and replaces its lock by a copy of the linked lock." : ".");
				}
				case 0x189:
					return (quality == 0x3F ? "Any critter" : "A critter of class " + quality) + " setting it off takes 3 + Casting-scaled damage.";
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
					string s = string.Format("{0}: {1} {2}.", VariableName(v), ops[heading & 7], value);
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
					return string.Format("set_jmp({0}, {1}, {2}).", quality, owner, heading);
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
