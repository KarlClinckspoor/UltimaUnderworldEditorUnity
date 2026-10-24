namespace UltimateEditor.Games
{
	/// <summary>
	/// What weapon and armour enchantments really do, against what their names say
	/// (STRINGS.PAK block 6, 448-479). Sources: the reference's combat/COMBAT.C DoPlayerWeapon
	/// and game/PLAYDATA.C player_affected_by (class 12) in both games; obj/LOOK.C do_of picks
	/// the name as 0x1C0 + effect (weapons) or 0x1D0 + effect (armour, rings, shields).
	///
	/// - UW1 weapons: effects 0-7 "Minor ... Unsurpassed Accuracy" add effect + 1 (1-8) to hit,
	///   8-15 "... Damage" add effect - 7 (1-8) damage. As named.
	/// - UW2 weapons: the names are swapped. 0-3 are named Minor/Major/Great/Unsurpassed
	///   Accuracy but add 2 * effect + 1 (1, 3, 5, 7) DAMAGE; 4-7 are named ... Damage but add
	///   2 * effect - 7 (1, 3, 5, 7) TO HIT. 8-15 are the special weapons (LifeStealer, Undead
	///   Bane, Fire Doom, Stone Strike, Entry...): their bonus tables are all 0, so they add
	///   nothing to hit or damage, only their effect after a hit.
	/// - Both games, armour: 0-7 "... Protection" lower a monster's to-hit at the item's body
	///   part by effect + 1 (1-8); 8-15 "... Toughness" add effect - 7 (1-8) to its armour value.
	///   For a shield or ring (body parts torso and hands) the code adds the toughness to the
	///   torso once for each of the two parts and never to the hands: double on the torso.
	///   The common belief that protection raises armour and toughness durability is not what
	///   the code does.
	/// </summary>
	public static class EnchantmentEffects
	{
		/// <summary>The real effect of the enchantment shown under block 6 name <paramref name="nameIndex"/> (448-479); null for others.</summary>
		public static string Describe(GameKind game, int nameIndex, bool shieldOrRing = false)
		{
			int e = nameIndex - 448;
			if (e >= 0 && e < 16)
			{
				if (game == GameKind.UW1)
					return e < 8
						? string.Format("Adds {0} to hit (as its name says).", e + 1)
						: string.Format("Adds {0} damage (as its name says).", e - 7);
				if (e < 4)
					return string.Format("Game bug: named Accuracy, but adds {0} DAMAGE and nothing to hit (UW2 swaps the two: COMBAT.C DoPlayerWeapon).", 2 * e + 1);
				if (e < 8)
					return string.Format("Game bug: named Damage, but adds {0} TO HIT and no damage (UW2 swaps the two).", 2 * e - 7);
				return "A special weapon: its effect fires after a hit (critical hits only, except Entry). It adds nothing to hit or damage: the game's bonus tables for special weapons are all 0.";
			}
			e = nameIndex - 464;
			if (e >= 0 && e < 16)
			{
				if (e < 8)
					return string.Format("Monsters' to-hit on the item's body part is lowered by {0}{1}. (It does not raise the armour value.)", e + 1,
						shieldOrRing ? " (a shield or ring covers torso and hands)" : "");
				return string.Format("Adds {0} to the item's armour value (not durability).{1}", e - 7,
					shieldOrRing ? " Game bug: for a shield or ring the code adds it twice to the torso and never to the hands." : "");
			}
			return null;
		}

		/// <summary>One paragraph for the editor's enchantment lists.</summary>
		public static string Summary(GameKind game)
		{
			if (game == GameKind.UW2)
				return "UW2 weapon enchantments have their names swapped: the four \"Accuracy\" ones add damage (1, 3, 5, 7) and the four \"Damage\" ones add to hit (1, 3, 5, 7). Special weapons add neither. Armour: Protection lowers monsters' to-hit, Toughness adds armour (doubled on the torso and missing on the hands for shields and rings).";
			return "UW1: Accuracy adds 1-8 to hit and Damage 1-8 damage, in the order Minor, (plain), Additional, Major, Great, Very Great, Tremendous, Unsurpassed. Armour: Protection lowers monsters' to-hit, Toughness adds armour (doubled on the torso and missing on the hands for shields and rings).";
		}
	}
}
