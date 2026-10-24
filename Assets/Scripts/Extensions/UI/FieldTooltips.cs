using System.Collections.Generic;
using UnityEngine;

namespace UltimateEditor.Uw1.UI
{
	/// <summary>
	/// Adds hover help to the existing OBJECTS.DAT / COMOBJ.DAT editors (prefabs), naming
	/// what each input really is in the game, since several are labelled "Unknown".
	/// Keys are the input objects' names in the prefabs; values are schema field names.
	/// </summary>
	public static class FieldTooltips
	{
		static readonly Dictionary<string, string> Weapon = new Dictionary<string, string>
		{
			{ "Slash", "Slash damage" }, { "Bash", "Bash damage" }, { "Stab", "Stab damage" },
			{ "Unk1", "Min charge" }, { "Unk2", "Speed" }, { "Unk3", "Max charge" },
			{ "Skill", "Skill" }, { "Durability", "Durability" },
		};

		static readonly Dictionary<string, string> Missile = new Dictionary<string, string>
		{
			{ "Damage", "Damage" }, { "Unk1", "Type" }, { "Unk2", "Ammo / damage type" },
		};

		static readonly Dictionary<string, string> Launcher = new Dictionary<string, string>
		{
			{ "Unk1", "Damage" }, { "Ammo", "Ammo / damage type" },
		};

		static readonly Dictionary<string, string> Armour = new Dictionary<string, string>
		{
			{ "Protection", "Protection" }, { "Durability", "Durability" }, { "Unk1", "Byte 2" }, { "Type", "Category" },
		};

		static readonly Dictionary<string, string> Container = new Dictionary<string, string>
		{
			{ "Capacity", "Capacity" }, { "Type", "Accepts" }, { "Slots", "Accepts" },
		};

		static readonly Dictionary<string, string> Light = new Dictionary<string, string>
		{
			{ "Duration", "Burn rate" }, { "Brightness", "Brightness" },
		};

		static readonly Dictionary<string, string> Creature = new Dictionary<string, string>
		{
			{ "Level", "Armour head / level" }, { "Unk0_1", "Armour zone 1" }, { "Unk0_2", "Armour zone 2" }, { "Unk0_3", "Armour zone 3" },
			{ "Health", "Average HP" }, { "Attack", "Dexterity" }, { "Unk1", "Intelligence" },
			{ "Remains", "Corpse" }, { "HitDecal", "Blood" }, { "OwnerType", "Race" }, { "Species", "Race" },
			{ "Passiveness", "Passive" }, { "Unk2", "Speed" }, { "Speed", "Run" }, { "Unk3", "Trade level" },
			{ "Poison", "Poison" }, { "MonsterType", "Sound" }, { "EquipmentDamage", "Equip" }, { "Unk4", "Defence" },
			{ "Attack1Value", "Attack 1 to-hit" }, { "Attack1Damage", "Attack 1 damage" }, { "Attack1Chance", "Attack 1 probability" },
			{ "Attack2Value", "Attack 2 to-hit" }, { "Attack2Damage", "Attack 2 damage" }, { "Attack2Chance", "Attack 2 probability" },
			{ "Attack3Value", "Attack 3 to-hit" }, { "Attack3Damage", "Attack 3 damage" }, { "Attack3Chance", "Attack 3 probability" },
			{ "Experience", "Experience" }, { "Unk5", "Spell 1" }, { "Unk6", "Spell 2" }, { "Unk7", "Spell 3" },
			{ "Unk8", "Caster level" }, { "Unk9", "Locks" }, { "Unk10", "Byte 2F" },
		};

		static readonly Dictionary<string, string> Extra = new Dictionary<string, string>
		{
			{ "Unk3", "Bytes 0x0D-0x0E: trading temper, four nibbles: wit, shrewdness, haggling, patience (BARTER.C)." },
			{ "Passiveness", "Byte 0x0A: bit 0 passive, bit 1 never summoned, bits 2-4 remains, bit 5 jumps, bit 6 swims, bit 7 flies." },
			{ "MonsterType", "Byte 0x10: bits 0-3 sound, 4-5 armour kind, 6-7 weapon kind." },
			{ "Unk8", "Byte 0x2D: bit 0 casts offensively, bits 1-7 caster power." },
			{ "Health", "Byte 0x04 average HP and byte 0x05 strength, read as one word by the editor (strength is 0 in every shipped creature)." },
			{ "Slots", "High byte of the 'accepts' word: 0x02 when Type names a kind, 0xFF for anything. Set by choosing Type." },
		};

		public enum Kind { Weapon, Missile, Launcher, Armour, Container, Light, Creature }

		public static void Attach(Transform editor, Kind kind)
		{
			if (!editor) return;
			Dictionary<string, string> map;
			RecordSchema schema;
			switch (kind)
			{
				case Kind.Weapon: map = Weapon; schema = Uw1Schemas.MeleeWeapon; break;
				case Kind.Missile: map = Missile; schema = Uw1Schemas.Missile; break;
				case Kind.Launcher: map = Launcher; schema = Uw1Schemas.Missile; break;
				case Kind.Armour: map = Armour; schema = Uw1Schemas.Armour; break;
				case Kind.Container: map = Container; schema = Uw1Schemas.Container; break;
				case Kind.Light: map = Light; schema = Uw1Schemas.Light; break;
				default: map = Creature; schema = Uw1Schemas.Creature; break;
			}
			foreach (var kv in map)
			{
				var t = editor.Find(kv.Key);
				if (!t) continue;
				string text;
				try
				{
					var f = schema[kv.Value];
					text = string.Format("{0} ({1}): {2}", f.Name, f.Location, f.Description);
					if (f.Source != null) text += "\n(" + f.Source + ")";
				}
				catch (KeyNotFoundException) { text = kv.Value; }
				string extra;
				if (kind == Kind.Creature || kind == Kind.Container)
					if (Extra.TryGetValue(kv.Key, out extra)) text += "\n" + extra;
				UiKit.Tooltip(t.gameObject, text);
			}
		}
	}
}
