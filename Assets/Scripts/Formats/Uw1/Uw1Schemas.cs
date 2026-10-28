namespace UltimateEditor.Uw1
{
	/// <summary>
	/// The UW1 record layouts with the meaning of every field, taken from the
	/// underworld-exhumed reference (uw1/src/include/*.h and docs/FORMATS.md). Source paths
	/// are relative to uw1/src in that repository.
	/// </summary>
	public static class Uw1Schemas
	{
		const Certainty C = Certainty.Certain;
		const Certainty I = Certainty.Inferred;
		const Certainty U = Certainty.Unknown;

		static FieldDef F(string name, int offset, int bit, int bits, string desc, string src = null, Certainty c = C, bool signed = false)
		{
			return new FieldDef(name, offset, bit, bits, desc, src, c, signed);
		}

		/// <summary>The first 8 bytes of every object (struct StaticObj, include/object.h).</summary>
		public static readonly RecordSchema StaticObject = new RecordSchema("Object (static part)", 8, "include/object.h struct Object, StaticObj",
			F("Item id", 0, 0, 9, "Item id: major class bits 6-8, minor class 4-5, index 0-3 (include/items.h).", "include/object.h ID_ITEM"),
			F("Flag 9", 0, 9, 1, "Lock (0x10F): locked. Doors: part of the door state (bits 9-12).", "obj/USEITEMS.C checkLock"),
			F("Flag 10", 0, 10, 1, "Book/scroll: plays cutscene 0x100 + text number. Trigger: keeps its trap chain after use (otherwise the chain is deleted). Lock: stays after unlocking (otherwise removed).", "event/TRIGGER.C UseTrigger; obj/USEITEMS.C"),
			F("Flag 11", 0, 11, 1, "Trigger: the player may set it off. Spell object: charged spell (charges shown).", "event/TRIGGER.C UseTrigger; obj/USEITEMS.C decode_obj_spell"),
			F("Enchanted", 0, 12, 1, "Enchanted: for an is_quant object the link holds the enchantment. Trigger: creatures may set it off.", "include/object.h ID_ENCHANT; event/TRIGGER.C"),
			F("Tenacious", 0, 13, 1, "Never culled when far from the player (UW-Formats calls it doordir).", "include/object.h ID_DOORDIR, chkTenacious"),
			F("Invisible", 0, 14, 1, "Not drawn.", "include/object.h ID_INVIS"),
			F("Is quantity", 0, 15, 1, "The link field is a quantity (below 0x200) or a special property (0x200 and up), not an object.", "include/object.h ID_ISQUANT"),
			F("Z", 2, 0, 7, "Height in the tile, 0-127 (8 units per floor step).", "include/object.h POS_Z"),
			F("Heading", 2, 7, 3, "Facing, in eighths of a turn.", "include/object.h POS_HEADING"),
			F("Fine Y", 2, 10, 3, "Y inside the tile, 0-7.", "include/object.h POS_YFINE"),
			F("Fine X", 2, 13, 3, "X inside the tile, 0-7.", "include/object.h POS_XFINE"),
			F("Quality", 4, 0, 6, "Quality 0-63; its meaning depends on the item (see the role list).", "include/object.h qn.f.quality"),
			F("Next", 4, 6, 10, "Next object in the same list (tile or container), 0 for the end.", "include/object.h qn.f.next"),
			F("Owner", 6, 0, 6, "Owner 0-63; its meaning depends on the item (see the role list).", "include/object.h ol.f.owner"),
			F("Link", 6, 6, 10, "Contents (first contained object) or, with Is quantity, a quantity / special property.", "include/object.h ol.f.link"));

		/// <summary>A mobile object: the 8 static bytes plus 19 (include/object.h).</summary>
		public static readonly RecordSchema MobileObject = new RecordSchema("Object (mobile)", 0x1B, "include/object.h struct Object",
			F("Item id", 0, 0, 9, "Item id (include/items.h).", "include/object.h"),
			F("Flags 9-12", 0, 9, 4, "Flags; see the static object.", "include/object.h"),
			F("Tenacious", 0, 13, 1, "Never culled.", "include/object.h"),
			F("Invisible", 0, 14, 1, "Not drawn.", "include/object.h"),
			F("Is quantity", 0, 15, 1, "Link is a quantity / property.", "include/object.h"),
			F("Z", 2, 0, 7, "Height in the tile.", "include/object.h"),
			F("Heading", 2, 7, 3, "Facing in eighths.", "include/object.h"),
			F("Fine Y", 2, 10, 3, "Y in the tile.", "include/object.h"),
			F("Fine X", 2, 13, 3, "X in the tile.", "include/object.h"),
			F("Quality", 4, 0, 6, "Critter: its home tile X as the conversations see it (npc_xhome).", "conv/CONVVARS.C"),
			F("Next", 4, 6, 10, "Next object in the tile list.", "include/object.h"),
			F("Owner", 6, 0, 6, "Critter: its home tile Y as the conversations see it (npc_yhome).", "conv/CONVVARS.C"),
			F("Link", 6, 6, 10, "Critter: its inventory (first object).", "include/object.h"),
			F("HP", 8, 0, 8, "Current hit points.", "include/object.h hp"),
			F("Heading byte", 9, 0, 8, "Heading (full byte).", "include/object.h heading"),
			F("Frame counter", 0x0A, 0, 4, "Animation frame counter.", "include/object.h OBJ_BIN", I),
			F("Terrain", 0x0A, 4, 3, "Terrain the critter stands on.", "include/object.h OBJ_TERRAIN", I),
			F("Loner", 0x0A, 7, 1, "Keeps to itself.", "include/object.h OBJ_LONER", I),
			F("Goal", 0x0B, 0, 4, "AI goal: 0 stand, 1 go home, 2 wander, 3 follow, 4 guard, 5 attack, 6 flee, 7 stand, 8 mill about, 9 cornered, 10 talk to player, 11 flutter, 12 hover.", "include/critter.h GOAL_*"),
			F("Goal target", 0x0B, 4, 8, "Goal target: a mobile index, 1 the player.", "include/object.h OBJ_GTARG"),
			F("Anim frame", 0x0B, 12, 4, "Animation frame. A missile's fine X instead.", "include/object.h OBJ_FRAME", I),
			F("Saved goal", 0x0D, 0, 4, "Goal saved while another runs (UW-Formats 'npc_level').", "include/object.h OBJ_OLDGOAL"),
			F("Target height", 0x0D, 4, 4, "Target height.", "include/object.h OBJ_TARGETZ", I),
			F("Summoned", 0x0D, 8, 1, "Summoned, temporary critter.", "include/object.h OBJ_TEMP", I),
			F("No healing", 0x0D, 9, 1, "Does not regenerate hit points.", "include/object.h OBJ_NOHEAL", I),
			F("Powerful", 0x0D, 10, 1, "Powerful: +7..12 to hit and +4..15 damage.", "combat/COMBAT.C critter_attack"),
			F("Bit 0D.11", 0x0D, 11, 1, "Unknown.", "include/object.h OBJ_B0D_11", U),
			F("Inventory made", 0x0D, 12, 1, "Its loot has been generated (generate_inventory).", "include/object.h OBJ_HAS_INV", I),
			F("Talked to", 0x0D, 13, 1, "The player has talked to it.", "include/object.h OBJ_TALKEDTO"),
			F("Attitude", 0x0D, 14, 2, "0 hostile, 1 upset, 2 mellow, 3 friendly.", "include/object.h OBJ_ATTITUDE"),
			F("Destination X", 0x0F, 0, 6, "Destination tile X.", "include/object.h OBJ_DESTX", I),
			F("Destination Y", 0x0F, 6, 6, "Destination tile Y.", "include/object.h OBJ_DESTY", I),
			F("Attack frame", 0x0F, 12, 4, "Attack (swing charge) frame.", "include/object.h OBJ_ATKFRAME", I),
			F("Damage taken", 0x11, 0, 8, "Accumulated damage.", "include/object.h OBJ_DAMAGE", I),
			F("Last hit", 0x12, 0, 8, "Who hit it last (mobile index).", "include/object.h last_hit", I),
			F("Speed", 0x13, 0, 7, "Speed.", "include/object.h OBJ_SPEED", I),
			F("Gravity", 0x13, 7, 1, "Affected by gravity.", "include/object.h SET_GRAVITY", I),
			F("Rate", 0x14, 0, 3, "Projectile rate.", "include/object.h OBJ_RATE", I),
			F("Pitch", 0x14, 3, 5, "Projectile pitch.", "include/object.h OBJ_PITCH", I),
			F("Sequence", 0x15, 0, 6, "Animation sequence (npc_animation).", "include/object.h OBJ_SEQ", I),
			F("Bit 15.6", 0x15, 6, 1, "Unknown.", "include/object.h OBJ_B15_6", U),
			F("Bit 15.7", 0x15, 7, 1, "Unknown.", "include/object.h OBJ_B15_7", U),
			F("Path index", 0x16, 0, 4, "Path-finding index.", "include/object.h OBJ_PATH", I),
			F("Home Y", 0x16, 4, 6, "Home tile Y (where it goes back to).", "include/object.h OBJ_HOMEY"),
			F("Home X", 0x16, 10, 6, "Home tile X.", "include/object.h OBJ_HOMEX"),
			F("Fine heading", 0x18, 0, 5, "Fine heading, 0-31.", "include/object.h OBJ_FINEHEAD", I),
			F("Bit 18.5", 0x18, 5, 1, "Unknown.", "include/object.h", U),
			F("Bit 18.6", 0x18, 6, 1, "Unknown.", "include/object.h", U),
			F("Bit 18.7", 0x18, 7, 1, "Unknown.", "include/object.h", U),
			F("Bit 19.0", 0x19, 0, 1, "Unknown flag.", "include/object.h", U),
			F("Bit 19.1", 0x19, 1, 1, "Unknown flag.", "include/object.h", U),
			F("Casting", 0x19, 2, 2, "Spell being cast (npc_spellindex).", "include/object.h OBJ_CAST", I),
			F("Bit 19.4", 0x19, 4, 1, "Unknown flag.", "include/object.h", U),
			F("Bit 19.5", 0x19, 5, 1, "Unknown flag.", "include/object.h", U),
			F("Ally", 0x19, 6, 1, "On the player's side.", "include/object.h OBJ_ALLY"),
			F("Fed", 0x19, 7, 1, "Fed (the conversation variable npc_hunger is 0x10 when set, 0xC0 when not).", "conv/CONVVARS.C"),
			F("Whoami", 0x1A, 0, 8, "Conversation slot to run; 0 uses the generic conversation 0x100 + creature index.", "conv/CONVERSE.C"));

		/// <summary>A tile of the level map (include/map.h struct Tile).</summary>
		public static readonly RecordSchema Tile = new RecordSchema("Tile", 4, "include/map.h struct Tile",
			F("Type", 0, 0, 4, "0 solid, 1 open, 2-5 diagonal open to SE, SW, NE, NW, 6-9 slope rising N, S, E, W.", "include/map.h enum TileType"),
			F("Floor height", 0, 4, 4, "Floor height, 0-15 (0x40 z units a step).", "map/MAP.C hgt_val"),
			F("Light", 0, 8, 2, "Tile light flag (bit 8 tested when the player moves); bit 9 unknown.", "include/map.h; motion/PHYSICS.C"),
			F("Floor texture", 0, 10, 4, "Index into the level's 10 floor textures.", "include/map.h"),
			F("No magic", 0, 14, 1, "Magic does not work here.", "include/map.h anti_magic_p"),
			F("Door", 0, 15, 1, "Keeps an open door open: DoClosingDoors (after sleeping, some events) never closes a door on this tile. Many shipped door tiles have it clear; it does not mean a door is here.", "event/TRIGGER.C DoClosingDoors"),
			F("Wall texture", 2, 0, 6, "Index into the level's 48 wall textures.", "include/map.h TILE_WALL"),
			F("First object", 2, 6, 10, "First object in the tile.", "include/map.h"));

		/// <summary>An animation overlay entry (include/object.h struct Anim, LEV.ARK block level + 8).</summary>
		public static readonly RecordSchema AnimOverlay = new RecordSchema("Animation overlay", 6, "include/object.h struct Anim; obj/EFFECT.C",
			F("Link low bits", 0, 0, 6, "Low six bits of the link word (kept as they are).", "include/object.h union Link", I),
			F("Object", 0, 6, 10, "The animated object; 0 ends the list.", "obj/EFFECT.C Anim_Load"),
			F("Frames left", 2, 0, 16, "Frames left, -1 for ever.", "include/object.h struct Anim", C, true),
			F("Tile X", 4, 0, 8, "Tile X.", "include/object.h"),
			F("Tile Y", 5, 0, 8, "Tile Y.", "include/object.h"));

		/// <summary>OBJECTS.DAT melee weapon table entry (include/object.h struct Weapon).</summary>
		public static readonly RecordSchema MeleeWeapon = new RecordSchema("Melee weapon", 8, "include/object.h struct Weapon; obj/HACK.C",
			F("Slash damage", 0, 0, 8, "Damage of a slash.", "include/object.h damage[0]"),
			F("Bash damage", 1, 0, 8, "Damage of a bash.", "include/object.h damage[1]"),
			F("Stab damage", 2, 0, 8, "Damage of a stab.", "include/object.h damage[2]"),
			F("Min charge", 3, 0, 8, "Minimum swing charge (editor: Unk1).", "include/object.h min_charge"),
			F("Speed", 4, 0, 8, "Swing speed (editor: Unk2).", "include/object.h speed"),
			F("Max charge", 5, 0, 8, "Maximum swing charge (editor: Unk3).", "include/object.h max_charge"),
			F("Skill", 6, 0, 8, "Skill used: 3 sword, 4 axe, 5 mace, 6 unarmed (the fist).", "include/object.h skill"),
			F("Durability", 7, 0, 8, "Durability, 255 unbreakable.", "include/object.h durability"));

		/// <summary>OBJECTS.DAT missile / launcher entry (include/object.h struct MissileInfo).</summary>
		public static readonly RecordSchema Missile = new RecordSchema("Missile or launcher", 3, "include/object.h struct MissileInfo",
			F("Damage", 0, 0, 8, "Damage.", "include/object.h damage"),
			F("Type", 1, 0, 8, "Missile type.", "include/object.h type"),
			F("Ammo / damage type", 2, 0, 8, "Launcher (0x18-0x1F): ammunition item - 0x10 (0 sling stone, 1 bolt, 2 arrow). Ammunition (0x10-0x17): minus its damage type (0x40 missile, 0x0B magic+fire, ...).", "combat/COMBAT.C:555", C, true));

		/// <summary>OBJECTS.DAT armour entry (include/object.h struct Armour).</summary>
		public static readonly RecordSchema Armour = new RecordSchema("Armour", 4, "include/object.h struct Armour",
			F("Protection", 0, 0, 8, "Protection.", "include/object.h"),
			F("Durability", 1, 0, 8, "Durability, 255 unbreakable.", "include/object.h"),
			F("Byte 2", 2, 0, 8, "Unknown: 4 body armour, Dragon Skin Boots and crowns; 5 leggings; 6 gloves; 7 other boots and hats; 0 shields and rings (measured). No reference code reads it.", "include/object.h b2", U),
			F("Category", 3, 0, 8, "0 shield, 1 body armour, 3 leggings, 4 gloves, 5 boots, 8 hat, 9 ring (paperdoll slot).", "inv/INVPANEL.C ItemFitsSlot"));

		/// <summary>OBJECTS.DAT container entry (include/object.h struct Container).</summary>
		public static readonly RecordSchema Container = new RecordSchema("Container", 3, "include/object.h struct Container",
			F("Capacity", 0, 0, 8, "Capacity in stones, 0 no limit.", "include/object.h"),
			F("Accepts", 1, 0, 16, "-1 anything; below 0x200 one item id; 0x200 runes, 0x201 missiles, 0x202 scrolls and the map, 0x203 food, 0x204 keys (UW2 only).", "include/object.h CONT_*", C, true));

		/// <summary>OBJECTS.DAT light entry (16 of them; lit lights 0x94-0x97 use entries 4-7).</summary>
		public static readonly RecordSchema Light = new RecordSchema("Light", 2, "include/object.h struct Light; game/PLAYTIME.C",
			F("Burn rate", 0, 0, 8, "How fast it burns down (editor: Duration); 0 never burns out.", "game/PLAYTIME.C:192"),
			F("Brightness", 1, 0, 8, "Light level it gives (SHADES.DAT row).", "game/PLAYDATA.C:395"));

		/// <summary>OBJECTS.DAT creature record, 48 bytes (include/critter.h struct Creature).</summary>
		public static readonly RecordSchema Creature = new RecordSchema("Creature", 0x30, "include/critter.h struct Creature",
			F("Armour head / level", 0, 0, 8, "Armour of hit zone 0; also read as the creature's level (ovr157). Editor: Level.", "include/critter.h armour[0]"),
			F("Armour zone 1", 1, 0, 8, "Armour of hit zone 1, 255 = use zone 0. Editor: Unk0_1.", "include/critter.h armour[1]"),
			F("Armour zone 2", 2, 0, 8, "Armour of hit zone 2. Editor: Unk0_2.", "include/critter.h armour[2]"),
			F("Armour zone 3", 3, 0, 8, "Armour of hit zone 3. Editor: Unk0_3.", "include/critter.h armour[3]"),
			F("Average HP", 4, 0, 8, "Average hit points (the player: maximum vitality). Editor: low byte of Health.", "include/critter.h avghit"),
			F("Strength", 5, 0, 8, "Strength; damage + strength / 5. Editor: high byte of Health (0 in every shipped creature).", "combat/COMBAT.C critter_attack"),
			F("Dexterity", 6, 0, 8, "Dexterity. Editor: Attack.", "include/critter.h attr[1]"),
			F("Intelligence", 7, 0, 8, "Intelligence. Editor: Unk1.", "include/critter.h attr[2]"),
			F("Death", 8, 0, 3, "Death effect.", "include/critter.h death", I),
			F("Blood", 8, 3, 2, "Blood on hit (editor: HitDecal 8 = blood 1).", "include/critter.h blood", I),
			F("Corpse", 8, 5, 3, "Corpse left (editor: high nibble shown as Remains).", "include/critter.h corpse", I),
			F("Race", 9, 0, 8, "Race, used for ownership and attitudes (editor: Species / OwnerType).", "include/critter.h race"),
			F("Passive", 0x0A, 0, 1, "Passive (editor: part of Passiveness).", "include/critter.h passive", I),
			F("Bit 0A.1", 0x0A, 1, 1, "Never summoned by Summon Monster.", "combat/SPELLS.C creat_spell"),
			F("Remains", 0x0A, 2, 3, "What it leaves when killed.", "include/critter.h remains", I),
			F("Jumps", 0x0A, 5, 1, "Can jump while path-finding.", "critter/PATHFIND.C hyp_move"),
			F("Swims", 0x0A, 6, 1, "Swims.", "include/critter.h swims"),
			F("Flies", 0x0A, 7, 1, "Flies.", "include/critter.h flier"),
			F("Speed", 0x0B, 0, 8, "Walking speed. Editor: Unk2.", "include/critter.h speed"),
			F("Run", 0x0C, 0, 8, "Running speed. Editor: Speed.", "include/critter.h run"),
			F("Trade level", 0x0D, 0, 4, "Trading temper: wit (BARTER.C).", "include/critter.h level"),
			F("Shrewd", 0x0D, 4, 4, "Trading temper: shrewdness.", "include/critter.h shrewd"),
			F("Haggle", 0x0D, 8, 4, "Trading temper: haggling.", "include/critter.h haggle"),
			F("Patience", 0x0D, 12, 4, "Trading temper: patience.", "include/critter.h patience"),
			F("Poison", 0x0F, 0, 8, "Poison level its blows give the player.", "critter/AI.C:1155 critter_attack(..., mycst->b0F)"),
			F("Sound", 0x10, 0, 4, "Sound set (editor: part of MonsterType).", "include/critter.h sound", I),
			F("Armour kind", 0x10, 4, 2, "Kind of armour it wears.", "include/critter.h armour_kind", I),
			F("Weapon kind", 0x10, 6, 2, "Kind of weapon it uses.", "include/critter.h weapon_kind", I),
			F("Equip", 0x11, 0, 8, "Equipment value: half of it is added to hit. Editor: EquipmentDamage.", "combat/COMBAT.C critter_attack", C, true),
			F("Defence", 0x12, 0, 8, "Defence: the target of the player's to-hit roll. Editor: Unk4.", "combat/COMBAT.C:301", C, true),
			F("Attack 1 to-hit", 0x13, 0, 8, "Attack 1 chance to hit. Editor: Attack1Value.", "include/critter.h attacks[0].chance", C, true),
			F("Attack 1 damage", 0x14, 0, 8, "Attack 1 damage (also the damage done to doors).", "include/critter.h attacks[0].damage"),
			F("Attack 1 probability", 0x15, 0, 8, "Chance this attack is chosen, percent.", "include/critter.h attacks[0].prob"),
			F("Attack 2 to-hit", 0x16, 0, 8, "Attack 2 chance to hit.", "include/critter.h", C, true),
			F("Attack 2 damage", 0x17, 0, 8, "Attack 2 damage.", "include/critter.h"),
			F("Attack 2 probability", 0x18, 0, 8, "Attack 2 probability.", "include/critter.h"),
			F("Attack 3 to-hit", 0x19, 0, 8, "Attack 3 chance to hit.", "include/critter.h", C, true),
			F("Attack 3 damage", 0x1A, 0, 8, "Attack 3 damage.", "include/critter.h"),
			F("Attack 3 probability", 0x1B, 0, 8, "Attack 3 probability.", "include/critter.h"),
			F("Nerve", 0x1C, 0, 4, "Nerve: keeps fighting when 8 or more (1 in 256).", "critter/AI.C:604"),
			F("Range", 0x1C, 4, 4, "Range.", "include/critter.h range", I),
			F("Noise", 0x1D, 0, 4, "Noise it makes.", "include/critter.h noise", I),
			F("Visibility", 0x1D, 4, 4, "How visible it is.", "include/critter.h visibility", I),
			F("Hearing", 0x1E, 0, 4, "Hearing.", "include/critter.h hearing", I),
			F("Sight", 0x1E, 4, 4, "Sight.", "include/critter.h sight", I),
			F("Laziness", 0x1F, 0, 4, "Laziness.", "include/critter.h lazy", I),
			F("Alertness", 0x1F, 4, 4, "Alertness.", "include/critter.h alert", I),
			F("Arms 1 present", 0x20, 0, 1, "First arm item present.", "include/critter.h arms[0]"),
			F("Arms 1 item", 0x20, 1, 7, "First arm item (also the missile it fires).", "include/critter.h arms[0].item"),
			F("Arms 2 present", 0x21, 0, 1, "Second arm item present.", "include/critter.h arms[1]"),
			F("Arms 2 item", 0x21, 1, 7, "Second arm item.", "include/critter.h arms[1].item"),
			F("Loot 1 chance", 0x22, 0, 4, "Chance of loot item 1.", "include/critter.h other[0].prob"),
			F("Loot 1 item", 0x22, 4, 12, "Loot item 1.", "include/critter.h other[0].item"),
			F("Loot 2 chance", 0x24, 0, 4, "Chance of loot item 2.", "include/critter.h other[1].prob"),
			F("Loot 2 item", 0x24, 4, 12, "Loot item 2.", "include/critter.h other[1].item"),
			F("Treasure chance", 0x26, 0, 4, "Chance of treasure.", "include/critter.h treasure_prob"),
			F("Treasure rate", 0x26, 4, 4, "How much treasure.", "include/critter.h treasure_rate"),
			F("Food chance", 0x26, 8, 4, "Chance of food.", "include/critter.h food_prob"),
			F("Food item", 0x26, 12, 4, "Food item (class 0x0B index).", "include/critter.h food_item"),
			F("Experience", 0x28, 0, 16, "Experience for the kill.", "include/critter.h exp", C, true),
			F("Spell 1", 0x2A, 0, 8, "First spell (spell table index, 255 none). Editor: Unk5.", "include/critter.h spells[0]"),
			F("Spell 2", 0x2B, 0, 8, "Second spell. Editor: Unk6.", "include/critter.h spells[1]"),
			F("Spell 3", 0x2C, 0, 8, "Third spell. Editor: Unk7.", "include/critter.h spells[2]"),
			F("Offensive caster", 0x2D, 0, 1, "Casts offensively; looks for targets 4 tiles around. Editor: low bit of Unk8.", "critter/AI.C:365"),
			F("Caster level", 0x2D, 1, 7, "Caster power. Editor: Unk8 >> 1.", "include/critter.h caster"),
			F("Locks", 0x2E, 0, 8, "Lock-picking skill. Editor: Unk9.", "include/critter.h locks", I),
			F("Byte 2F", 0x2F, 0, 8, "Unknown (0x49 in every shipped creature). Editor: Unk10.", "include/critter.h b2F", U));

		/// <summary>COMOBJ.DAT entry, 11 bytes (include/object.h struct ComObj, docs/FORMATS.md).</summary>
		public static readonly RecordSchema ComObj = new RecordSchema("Common object properties", 11, "include/object.h struct ComObj; docs/FORMATS.md COMOBJ.DAT",
			F("Height", 0, 0, 8, "Height.", "include/object.h"),
			F("Radius", 1, 0, 3, "Radius.", "include/object.h"),
			F("Animated", 1, 3, 1, "Animated (3d/GAMESORT.C). Editor: Type.", "3d/GAMESORT.C:266"),
			F("Mass", 1, 4, 12, "Mass in tenths of a stone.", "include/object.h"),
			F("Bit 3.0", 3, 0, 1, "Unnamed.", "include/object.h b3_0", U),
			F("Solid", 3, 1, 1, "Solid: blocks movement.", "motion/MOTION.C:571"),
			F("Bit 3.2", 3, 2, 1, "Unnamed.", "include/object.h c3_2", U),
			F("No hit", 3, 3, 1, "Not hit by missiles; also turns gravity off for a thrown object.", "motion/OBJPHYS.C:389"),
			F("Bit 3.4", 3, 4, 1, "Unnamed.", "include/object.h b3_4", U),
			F("Pick up", 3, 5, 1, "Can be picked up.", "include/object.h pickup"),
			F("Stacking", 3, 6, 2, "Stacking rule; 1 and 3 never merge. Editor: Flag6 + 'Container' (bit 7 is not a container flag).", "inv/INVPANEL.C:698"),
			F("Value", 4, 0, 16, "Value in gold.", "include/object.h"),
			F("Touch", 6, 0, 1, "Touch action.", "motion/OBJPHYS.C:163"),
			F("Usable", 6, 1, 1, "Usable.", "motion/OBJPHYS.C:163"),
			F("Quality class", 6, 2, 2, "Quality class: class * 6 + quality picks the quality word in string block 5.", "include/object.h qualclass"),
			F("Light", 6, 4, 1, "Gives light.", "include/object.h light"),
			F("Bounce", 6, 5, 4, "Bounce.", "include/object.h bounce", I),
			F("Fate", 7, 1, 4, "What happens when it lands after a throw. Editor: ObjectType.", "motion/OBJPHYS.C:440"),
			F("Pickable", 7, 5, 1, "Pickable. Editor: PickupFlag.", "include/object.h pickable"),
			F("Bit 7.6", 7, 6, 1, "Unnamed. Editor: UnkFlag1.", "include/object.h b7_6", U),
			F("Can be owned", 7, 7, 1, "Can have an owner (stealing it is a crime). Editor: Ownable.", "include/object.h can_own"),
			F("Resist magic", 8, 0, 2, "Magic resistance: resists when rand() % 3 < this.", "include/object.h DMG_MAGIC"),
			F("Resist physical", 8, 2, 1, "Resists blows.", "include/object.h DMG_PHYSICAL"),
			F("Resist fire", 8, 3, 1, "Resists fire.", "include/object.h DMG_FIRE"),
			F("Resist poison", 8, 4, 1, "Resists poison.", "include/object.h DMG_POISON"),
			F("Resist cold", 8, 5, 1, "Resists cold.", "include/object.h DMG_COLD"),
			F("Resist missiles", 8, 6, 1, "Resists missiles.", "include/object.h DMG_MISSILE"),
			F("Undead", 8, 7, 1, "Undead (Smite Undead).", "include/object.h RES_UNDEAD"),
			F("Render", 9, 0, 2, "0 sprite, 1 critter, 2 3D model or door, 3 texture-mapped model.", "3d/DRAWOBJ.C:7"),
			F("Tenacity", 9, 2, 4, "How far away it may be culled.", "obj/OBJECTS.C:140"),
			F("Bits 9.6-7", 9, 6, 2, "Unnamed.", "include/object.h b9_6", U),
			F("Quality type", 10, 0, 4, "Quality type: a group of 6 strings in block 4.", "include/object.h qualtype"),
			F("Lookable", 10, 4, 1, "Has a printable 'look at' description.", "include/object.h lookable"),
			F("Bits A.5-7", 10, 5, 3, "Unnamed.", "include/object.h", U));

		public static readonly RecordSchema[] All =
		{
			StaticObject, MobileObject, Tile, AnimOverlay, MeleeWeapon, Missile, Armour, Container, Light, Creature, ComObj
		};
	}
}
