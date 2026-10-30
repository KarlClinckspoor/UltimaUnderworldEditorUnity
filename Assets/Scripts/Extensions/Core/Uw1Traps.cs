using System.Collections.Generic;

namespace UltimateEditor.Uw1
{
	/// <summary>
	/// UW1 traps (items 0x180-0x190) and triggers (0x1A0-0x1A6) as event/TRIGGER.C runs them
	/// (UseTrigger, UseTrap, do_trap_hack) in underworld-exhumed.
	/// </summary>
	public static class Uw1Traps
	{
		public const int FirstTrap = 0x180, LastTrap = 0x190, FirstTrigger = 0x1A0, LastTrigger = 0x1A6;

		public enum TrapType
		{
			Damage, Teleport, Arrow, Hack, Pit, ChangeTerrain, Spell, CreateObject, Door,
			Ward, Tell, DeleteObject, Inventory, SetVariable, CheckVariable, Combination, TextString
		}

		public enum TriggerType { Move, PickUp, Use, Look, StepOn, Open, Unlock }

		public static readonly string[] TrapNames =
		{
			"Damage trap", "Teleport trap", "Arrow trap", "Do (special) trap", "Pit trap", "Change terrain trap",
			"Spell trap", "Create object trap", "Door trap", "Ward trap", "Tell trap", "Delete object trap",
			"Inventory trap", "Set variable trap", "Check variable trap", "Combination trap", "Text string trap"
		};

		public static readonly string[] TriggerNames =
		{
			"Move trigger", "Pick up trigger", "Use trigger", "Look trigger", "Step on trigger", "Open trigger", "Unlock trigger"
		};

		public static bool IsTrap(int item) { return item >= FirstTrap && item <= LastTrap; }
		public static bool IsTrigger(int item) { return item >= FirstTrigger && item <= LastTrigger; }

		/// <summary>The special ("hack") traps, item 0x183, chosen by quality (do_trap_hack).</summary>
		public static readonly KeyValuePair<int, string>[] HackTraps =
		{
			new KeyValuePair<int, string>(0x02, "Crystal ball (crystal_ball)"),
			new KeyValuePair<int, string>(0x03, "Eight-position switch (eight_pos_switch): target tile height from Z + 8 * the dial lever's position"),
			new KeyValuePair<int, string>(0x04, "Eight-position switch: the linked object's z = Z + 8 * the dial lever's position"),
			new KeyValuePair<int, string>(0x05, "Crime against the race in Owner (player_did_bad)"),
			new KeyValuePair<int, string>(0x18, "Bullfrog puzzle (work_bullfrog_tiles; Owner = mode 0-4, level 4 only)"),
			new KeyValuePair<int, string>(0x28, "Emerald puzzle (emerald_trap)"),
			new KeyValuePair<int, string>(0x29, "Exploding book (on the player's tile)"),
			new KeyValuePair<int, string>(0x2A, "Talking door"),
			new KeyValuePair<int, string>(0x32, "The first critter with conversation 0xD8 talks to the player"),
			new KeyValuePair<int, string>(0x39, "Arial's scene"),
			new KeyValuePair<int, string>(0x3C, "Earthquake: screen shake, strength = Owner (only when the player set it off)"),
			new KeyValuePair<int, string>(0x3D, "Earthquake: the player bounces, strength = Owner"),
			new KeyValuePair<int, string>(0x3E, "Earthquake: shake and bounce, strength = Owner"),
			new KeyValuePair<int, string>(0x3F, "End of the game (EndGameMode = Owner + 1; any Owner)"),
		};

		public static string HackTrapName(int quality)
		{
			foreach (var kv in HackTraps)
				if (kv.Key == quality)
					return kv.Value;
			return "No effect in UW1";
		}

		static Uw1ObjectRoles.Role R(string f, string m) { return new Uw1ObjectRoles.Role(f, m, "event/TRIGGER.C"); }

		/// <summary>Field roles of a trap or trigger item.</summary>
		public static List<Uw1ObjectRoles.Role> Roles(int item)
		{
			var r = new List<Uw1ObjectRoles.Role>();
			if (IsTrigger(item))
			{
				r.Add(R("Quality, Owner", "Target square X, Y on this level: the whole trap chain runs there, not where the trigger lies. The first trap should lie in that square's object list (or inside an object there): a used-up trigger finds it there to delete it."));
				r.Add(R("Link", "The first trap of its chain (followed whatever is_quant says). Shipped triggers are all is_quant and invisible."));
				r.Add(R("Flag 11", "The player may set it off."));
				r.Add(R("Enchanted", "Creatures may set it off. An enchanted trigger without Flag 11 is not set off by objects (thrown items, missiles); every other trigger is. (The MAJOR_RECT test in UseTrigger looks at the trigger itself, so it never applies: triggers in doors work for creatures too.)"));
				r.Add(R("Flag 10", "Reusable. Without it, after running, the trap chain is deleted together with every trigger linking to its first trap (delete_trap uses the trap's trigger count; kill_triggers stops at the first match in each list, so keep one used-up trigger per trap per list)."));
				r.Add(R("Flag 9", "Unused on a UW1 trigger."));
				if (item == FirstTrigger + (int)TriggerType.Look)
					r.Add(R("Z", "Search difficulty, not a height: when Z > 0 the player needs skill_check(Search, Z), a chance of (15 + Search - Z) / 31. Reveal checks at Search 45."));
				if (item == FirstTrigger + (int)TriggerType.Move)
					r.Add(R("Z", "Height: it is walked into (height 32, radius 2), so put it at the floor height * 8."));
				r.Add(R("Mode", "Fires when the action's mode matches OBJECTS.DAT's trigger table: move 0 (on the tile; walking, critters, thrown objects), pick up 2, use 4, look 5 (these inside the object), step on 1 (nothing reports it: it never fires), open 7 (a door at rest starting to open), unlock 6 (a lock picked or opened with its key). Inside an object only the first trap-class object is tried. An \"on\" switch fires the last trigger in its list, an \"off\" one the first."));
				return r;
			}
			if (item >= 0x1A7)
			{
				r.Add(R("Note", "No such trigger in UW1 (UW2 has more trigger types)."));
				return r;
			}
			if (item > LastTrap)
			{
				r.Add(R("Note", "No such trap in UW1."));
				return r;
			}
			r.Add(R("Flags 9-12", "Number of triggers pointing at this trap, 0-15 (bit 12, the editor's Enchanted, is the 8s; a trap with 15 triggers ships on level 7). delete_trap uses it to find and remove them; too low leaves triggers pointing at a freed slot."));
			r.Add(R("Link", "Next trap (or trigger) in the chain, except where noted. A trigger reached this way runs its trap at its own target square, with no who checks."));
			switch ((TrapType)(item - FirstTrap))
			{
				case TrapType.Damage:
					r.Add(R("Quality", "Damage dealt (physical, type 4) to whoever set the chain off."));
					r.Add(R("Owner", "Non-zero: poisons the player at strength Quality instead (only if stronger than his present poison and he does not resist poison; no damage). Poison is 4 bits, so 1-15 is useful (16 stores 0). Anyone else takes plain damage. It never heals."));
					break;
				case TrapType.Teleport:
					r.Add(R("Quality, Owner", "Destination tile X, Y. X = 0 is ignored; 0x3F skips the search for a free square."));
					r.Add(R("Z", "Destination level (0 this level)."));
					r.Add(R("Note", "Only the player moves. On this level the 9x9 box round the destination is searched and the teleport fails if all is full; on another level, objects near the destination may be pushed away and the player dies if there is no room. Fine X/Y and heading are unused."));
					break;
				case TrapType.Arrow:
					r.Add(R("Quality, Owner", "Missile item = Quality * 32 + Owner (Owner bit 5 overlaps Quality bit 0: keep Owner below 32)."));
					r.Add(R("Fine X, Fine Y, Z, Heading", "Launch point within the target square, and direction (heading * 32 + 2 in 1/256 turns); fired flat."));
					break;
				case TrapType.Hack:
					r.Add(R("Quality", "Which special trap (see the list); others do nothing in UW1. The chain continues."));
					r.Add(R("Other fields", "2: camera from the target square at Fine X/Y, Z and Heading. 3, 4: base height Z plus 8 * the position of the dial lever whose use trigger started the chain; 3 sets the target tile's height, 4 the linked object's z (object index below 512). 5, 0x3C-0x3E: Owner = race, or quake strength. 0x18: Owner = mode 0-4. 0x3F: any Owner."));
					break;
				case TrapType.Pit:
				case TrapType.Combination:
					r.Add(R("Note", "UseTrap has no case for it in UW1: it does nothing except pass on to its link."));
					break;
				case TrapType.ChangeTerrain:
					r.Add(R("Fine X, Fine Y", "Size: changes tiles x..x+FineX, y..y+FineY from the target square."));
					r.Add(R("Owner", "New wall texture; 0x30 or more keeps it."));
					r.Add(R("Quality >> 1", "New floor texture; 0xB or more keeps it."));
					r.Add(R("Heading * 2 + (Quality & 1)", "New tile type; 10 or more (15 becomes 10) keeps it."));
					r.Add(R("Z >> 3", "New floor height; above 13 keeps it."));
					break;
				case TrapType.Spell:
					r.Add(R("Quality", "Spell class (do_spell's major)."));
					r.Add(R("Owner", "Spell effect within the class (the editor shows class * 16 + effect)."));
					r.Add(R("Note", "The trap is the caster and whoever set the chain off the target. Classes 0-3, 7, 9, 10 and 11 do nothing from a trap; heal (4), missile (5, effect 1-4), area (6), special (13) and cutscene (14) work. Missiles start from the trap's Fine X/Y and Z in its Heading; area spells centre along its Heading."));
					break;
				case TrapType.CreateObject:
					r.Add(R("Quality", "Chance of NOT creating: creates when rand(63) >= Quality."));
					r.Add(R("Link", "The template object copied to the target square, at its own fine position and z; only its first content is copied. Nothing when the trap is is_quant. The chain always ends."));
					r.Add(R("Note", "A creature is not made while a temporary creature is within 4 squares of the template's home. With trigger count 0 and a creature template the trap is also a wandering-monster spawner."));
					break;
				case TrapType.Door:
					r.Add(R("Quality", "1 open, 2 close, 3 toggle the door at rest on the target square (others only swap the lock); a moving door is reversed instead."));
					r.Add(R("Link", "For a door at rest the door's lock is always removed, then a copy of this lock put on (no link: unlocked). The chain always ends."));
					break;
				case TrapType.Ward:
				case TrapType.Tell:
					r.Add(R("Quality", "Hits whoever set the chain off (objects too) when its item & 0xF equals Quality (15 includes the player), or anyone for 0x3F: 3 + rand(player's Casting) damage, and tells the player. Ends the chain."));
					break;
				case TrapType.DeleteObject:
					r.Add(R("Quality, Owner", "Square X, Y where its linked object is deleted; the object must be top level in that square's list."));
					r.Add(R("Link", "The object to delete (ends the chain). Shipped delete traps are all is_quant, so the target is not taken for the trap's contents."));
					break;
				case TrapType.Inventory:
					r.Add(R("Quality, Owner", "Item = Quality * 32 + Owner the player must carry (the first found), or the chain ends."));
					r.Add(R("Z", "Non-zero: if that item is a plain stack it must hold at least Z."));
					break;
				case TrapType.SetVariable:
					r.Add(R("Z", "0: a quest bit (0-31); 1-63: game variable Z (64 or more writes past the variables). Variable 0 cannot be set."));
					r.Add(R("Quality, Owner, Fine Y", "Value = ((Quality << 5 | Owner) << 3) | Fine Y (quest bit number for Z 0)."));
					r.Add(R("Heading", "Variables: 0 add, 1 subtract, 2 set, 3 and, 4 or, 5 xor, 6 shift left, 7 nothing; the result is kept to 6 bits (so set stores value & 63). Quest bits: 5 toggle, 1 clear, others set."));
					break;
				case TrapType.CheckVariable:
					r.Add(R("Z, Heading", "Checks game variables Z..Z+Heading (Z 0 is variable 0, not the quests)."));
					r.Add(R("Fine X", "Non-zero: sums them; zero: packs 3 bits of each."));
					r.Add(R("Quality, Owner, Fine Y", "Value compared, as for the set variable trap (not masked)."));
					r.Add(R("Link", "On a match the chain continues through the link (a trap or a trigger). On a mismatch it continues through the trigger after the linked object (the 'else' branch, which must be a trigger), or ends."));
					break;
				case TrapType.TextString:
					r.Add(R("Quality, Owner", "Prints string Quality * 32 + Owner of block 9."));
					break;
			}
			return r;
		}
	}
}
