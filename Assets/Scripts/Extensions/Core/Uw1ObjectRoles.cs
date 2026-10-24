using System.Collections.Generic;

namespace UltimateEditor.Uw1
{
	/// <summary>
	/// What the generic object fields (quality, owner, link, z, heading, fine x/y and the
	/// flag bits) mean for a given item, as the reference code uses them. Each entry names
	/// the routine that reads the field (paths relative to uw1/src in underworld-exhumed).
	/// </summary>
	public static class Uw1ObjectRoles
	{
		public struct Role
		{
			public readonly string Field;
			public readonly string Meaning;
			public readonly string Source;
			public Role(string field, string meaning, string source) { Field = field; Meaning = meaning; Source = source; }
			public override string ToString() { return Field + ": " + Meaning; }
		}

		static Role R(string field, string meaning, string source) { return new Role(field, meaning, source); }

		/// <summary>The roles of the fields of an object of item <paramref name="item"/>.</summary>
		public static List<Role> For(int item, bool isQuantity)
		{
			var roles = new List<Role>();
			if (LevArk.Game.Kind == UltimateEditor.Games.GameKind.UW2)
			{
				// The roles below are UW1's. For UW2 only the traps and triggers are described
				// (uw2 event/TRIGGER.C); other items' fields are not mapped yet.
				var d = UltimateEditor.Games.Uw2Traps.Describe(item);
				if (d != null) roles.Add(R("Fields", d, "uw2 event/TRIGGER.C"));
				return roles;
			}
			int cls = item >> 4;
			if (item >= 0x180 && item < 0x1C0)
			{
				roles.AddRange(Uw1Traps.Roles(item));
				return roles;
			}
			switch (cls)
			{
				case 0x0: case 0x1: case 0x2: case 0x3:
					roles.Add(R("Quality", "Condition (durability left); 0 broken.", "combat/DAMAGE.C"));
					roles.Add(R("Owner", "Race that owns it; taking it is a crime against that race.", "obj/OBJECTS.C, include/object.h can_own"));
					if (isQuantity)
						roles.Add(R("Link", "Below 0x200 a quantity (missiles stack); from 0x200 with Enchanted, the enchantment (link - 0x200).", "include/object.h LINK_SPECIAL"));
					break;
				case 0x4: case 0x5: case 0x6: case 0x7:
					roles.Add(R("Quality", "Home tile X seen by conversations (npc_xhome); the AI's home is at 0x16.", "conv/CONVVARS.C"));
					roles.Add(R("Owner", "Home tile Y seen by conversations (npc_yhome).", "conv/CONVVARS.C"));
					roles.Add(R("Link", "Inventory (first carried object).", "obj/TREASURE.C"));
					roles.Add(R("Whoami", "Conversation slot; 0 uses generic conversation 0x100 + creature index.", "conv/CONVERSE.C"));
					break;
				case 0x8:
					roles.Add(R("Link", "Contents (first object inside). A lock object (0x10F) inside locks it.", "obj/USEITEMS.C checkLock"));
					roles.Add(R("Owner", "Race that owns it.", "include/object.h"));
					break;
				case 0x9:
					if (item < 0x98)
						roles.Add(R("Quality", "Fuel left; 0 burnt out (the light says so and will not light).", "obj/USEITEMS.C UseLight"));
					else
					{
						roles.Add(R("Link", "The spell object (0x120-0x12F) the wand casts; without one the wand turns into the broken wand.", "obj/USEITEMS.C UseWand"));
					}
					break;
				case 0xA:
					if (isQuantity) roles.Add(R("Link", "Quantity (coins, gems).", "include/object.h"));
					break;
				case 0xB:
					roles.Add(R("Quality", "Freshness / condition.", "obj/USEITEMS.C UseFood"));
					if (isQuantity) roles.Add(R("Link", "Quantity, or with Enchanted a spell (potions).", "obj/USEITEMS.C decode_obj_spell"));
					break;
				case 0x10:
					if (item == 0x10F)
					{
						roles.Add(R("Link", "The key number that opens it (link & 0x1FF), compared with a key's Owner.", "obj/USEITEMS.C checkLock"));
						roles.Add(R("Flag 9", "Locked.", "obj/USEITEMS.C checkLock"));
						roles.Add(R("Flag 10", "Stays after being unlocked (otherwise removed).", "obj/USEITEMS.C checkLock"));
						roles.Add(R("Z", "Pick difficulty: skill check against Z * 3; 14 needs Pick Lock 31, 15 cannot be picked.", "obj/USEITEMS.C checkLock"));
					}
					else if (item == 0x10E)
						roles.Add(R("Use", "The lockpick: Pick Lock skill against the lock's Z.", "obj/USEITEMS.C"));
					else
						roles.Add(R("Owner", "Number of the lock it opens (matches the lock's link).", "obj/USEITEMS.C UseKey"));
					break;
				case 0x12:
					roles.Add(R("Link", "Spell: with Flag 11 (charged) class = (link & 0x1FF) >> 6 + 12 and effect = link & 0x3F; otherwise class = (link & 0x1FF) >> 4 and effect = link & 0xF.", "obj/USEITEMS.C decode_obj_spell"));
					roles.Add(R("Quality", "Charges; a spell object of quality 0 fails 4 times in 10.", "obj/USEITEMS.C decode_obj_spell"));
					break;
				case 0x13:
					roles.Add(R("Link", "Text number - 0x200 in string block 3 (below 0x100); 0x100 and up is the stew recipe. The map (0x13B) opens the map.", "obj/USEITEMS.C UseBook"));
					roles.Add(R("Flag 10", "Plays cutscene 0x100 + text number.", "obj/USEITEMS.C UseBook"));
					roles.Add(R("Enchanted", "An enchanted scroll casts its spell and is used up.", "obj/USEITEMS.C UseBook"));
					break;
				case 0x14:
					roles.Add(R("Link", "Its lock (0x10F) and its traps/triggers.", "obj/USEITEMS.C checkLock"));
					roles.Add(R("Owner", "Bit 0: spiked (critters cannot open it).", "obj/USEITEMS.C spike"));
					roles.Add(R("Flags 9-12", "Door state.", "include/object.h OBJ_FLAGS"));
					roles.Add(R("Quality", "Door strength (damage it takes before breaking).", "combat/DAMAGE.C"));
					break;
				case 0x15:
					roles.Add(R("Link", "Chest/barrel that is not a quantity: its contents.", "obj/USEITEMS.C"));
					break;
				case 0x16:
					if (item == 0x161 || item == 0x162)
						roles.Add(R("Flags 9-12", "Current position of the 8-state lever/switch.", "obj/USEITEMS.C UseRect"));
					else if (item == 0x164)
						roles.Add(R("Flags 9-12", "Bridge texture (2 and up: texture description).", "obj/LOOK.C RectLook"));
					else if (item == 0x165 || item == 0x166)
					{
						roles.Add(R("Link / Owner", "Text number in string block 8: link & 0x1FF when Is quantity, else Owner.", "obj/LOOK.C RectLook"));
						if (item == 0x165) roles.Add(R("Picture", "GRAVE.DAT byte at the text number: the gravestone picture (0 none).", "obj/LOOK.C RectLook"));
					}
					else if (item >= 0x16E)
						roles.Add(R("Owner", "Wall texture shown (index into the level's wall textures).", "obj/LOOK.C RectLook"));
					break;
				case 0x17:
					roles.Add(R("Use", "Flips between the item and item +/- 8 and sets off its triggers (link).", "obj/USEITEMS.C UseRect"));
					roles.Add(R("Link", "Its triggers.", "event/TRIGGER.C"));
					break;
				case 0x1C: case 0x1D: case 0x1E: case 0x1F:
					if (item == 0x1CF)
						roles.Add(R("Owner", "Moving door: the door's own item.", "obj/USEITEMS.C"));
					roles.Add(R("Note", "Animated object; the LEV.ARK overlay block holds its frames left.", "obj/EFFECT.C"));
					break;
			}
			return roles;
		}
	}
}
