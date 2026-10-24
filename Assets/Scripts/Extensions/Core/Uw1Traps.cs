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
			new KeyValuePair<int, string>(0x03, "Eight-position switch (eight_pos_switch); uses the triggering switch's flags"),
			new KeyValuePair<int, string>(0x04, "Eight-position switch (same as 3)"),
			new KeyValuePair<int, string>(0x05, "Crime against the race in Owner (player_did_bad)"),
			new KeyValuePair<int, string>(0x18, "Bullfrog puzzle (work_bullfrog_tiles, Owner passed on)"),
			new KeyValuePair<int, string>(0x28, "Emerald puzzle (emerald_trap)"),
			new KeyValuePair<int, string>(0x29, "Exploding book"),
			new KeyValuePair<int, string>(0x2A, "Talking door"),
			new KeyValuePair<int, string>(0x32, "Critters waiting with conversation 0xD8 come and talk"),
			new KeyValuePair<int, string>(0x39, "Arial's scene"),
			new KeyValuePair<int, string>(0x3C, "Earthquake type 1, strength = Owner (only when the player set it off)"),
			new KeyValuePair<int, string>(0x3D, "Earthquake type 2, strength = Owner"),
			new KeyValuePair<int, string>(0x3E, "Earthquake type 3, strength = Owner"),
			new KeyValuePair<int, string>(0x3F, "End of the game (EndGameMode = Owner + 1)"),
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
				r.Add(R("Quality, Owner", "Target square X, Y: the trap chain runs there."));
				r.Add(R("Link", "The first trap of its chain."));
				r.Add(R("Flag 11", "The player may set it off."));
				r.Add(R("Enchanted", "Creatures may set it off (never on a door, MAJOR_RECT)."));
				r.Add(R("Flag 10", "Reusable: without it the trap chain is deleted after use."));
				if (item == FirstTrigger + (int)TriggerType.Look)
					r.Add(R("Z", "Above floor level: a Search skill check against Z is needed to notice it."));
				r.Add(R("Mode", "Runs when the action matches its mode in OBJECTS.DAT's trigger table."));
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
			r.Add(R("Flags 9-12", "Number of triggers pointing at this trap; delete_trap uses it to find and remove them."));
			r.Add(R("Link", "Next trap (or trigger) in the chain, except where noted."));
			switch ((TrapType)(item - FirstTrap))
			{
				case TrapType.Damage:
					r.Add(R("Quality", "Damage dealt (physical)."));
					r.Add(R("Owner", "Non-zero: heals by Quality instead."));
					break;
				case TrapType.Teleport:
					r.Add(R("Quality, Owner", "Destination tile X, Y."));
					r.Add(R("Z", "Destination level (0 this level)."));
					break;
				case TrapType.Arrow:
					r.Add(R("Quality, Owner", "Missile item = Quality * 32 + Owner, fired from the target square."));
					break;
				case TrapType.Hack:
					r.Add(R("Quality", "Which special trap (see the list); others do nothing in UW1."));
					r.Add(R("Owner", "Parameter of the special trap."));
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
					break;
				case TrapType.CreateObject:
					r.Add(R("Quality", "Chance of NOT creating: creates when rand(63) >= Quality."));
					r.Add(R("Link", "The template object copied to the target square (ends the chain)."));
					break;
				case TrapType.Door:
					r.Add(R("Quality", "1 open, 2 close, 3 toggle the door on the target square."));
					r.Add(R("Link", "A lock copied onto the door, replacing its lock (ends the chain)."));
					break;
				case TrapType.Ward:
				case TrapType.Tell:
					r.Add(R("Quality", "Creature class it hits (0x3F any); damage 3 + rand(Casting skill). Ends the chain."));
					break;
				case TrapType.DeleteObject:
					r.Add(R("Quality, Owner", "Square X, Y where its linked object is deleted."));
					r.Add(R("Link", "The object to delete (ends the chain)."));
					break;
				case TrapType.Inventory:
					r.Add(R("Quality, Owner", "Item = Quality * 32 + Owner the player must carry, or the chain ends."));
					r.Add(R("Z", "Non-zero: the stack must hold at least Z."));
					break;
				case TrapType.SetVariable:
					r.Add(R("Z", "0: a quest bit; 1-63: game variable Z."));
					r.Add(R("Quality, Owner, Fine Y", "Value = ((Quality << 5 | Owner) << 3) | Fine Y (quest bit number for Z 0)."));
					r.Add(R("Heading", "Variables: 0 add, 1 subtract, 2 set, 3 and, 4 or, 5 xor, 6 shift left (result kept to 6 bits). Quest bits: 5 toggle, 1 clear, others set."));
					break;
				case TrapType.CheckVariable:
					r.Add(R("Z, Heading", "Checks game variables Z..Z+Heading."));
					r.Add(R("Fine X", "Non-zero: sums them; zero: packs 3 bits of each."));
					r.Add(R("Quality, Owner, Fine Y", "Value compared, as for the set variable trap."));
					r.Add(R("Link", "On a mismatch the chain continues through the trigger after the linked object (the 'else' branch)."));
					break;
				case TrapType.TextString:
					r.Add(R("Quality, Owner", "Prints string Quality * 32 + Owner of block 9."));
					break;
			}
			return r;
		}
	}
}
