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
	}
}
