using System;
using System.Collections.Generic;
using System.IO;

namespace UltimateEditor.Uw1
{
	/// <summary>
	/// DATA\SKILLS.DAT: the classes' starting attributes and their skill choices
	/// (game/CHARGEN.C strt_chargen, roll_stats, set_sklmnu).
	/// Layout: 8 classes of 4 bytes (strength, dexterity, intelligence, bonus points), then for
	/// each class 5 entries of a count byte and that many skill numbers (0-19). A count of 0
	/// gives no skill, 1 gives that skill at once, more asks the player to pick one.
	/// </summary>
	public sealed class SkillsDat
	{
		public const int Classes = 8, EntriesPerClass = 5, Skills = 20;

		/// <summary>Skill names in skill order (include/player.h; STRINGS.PAK block 2 from 0x1F).</summary>
		public static readonly string[] SkillNames =
		{
			"Attack", "Defense", "Unarmed", "Sword", "Axe", "Mace", "Missile", "Mana", "Lore", "Casting",
			"Traps", "Search", "Track", "Sneak", "Repair", "Charm", "Picklock", "Acrobat", "Appraise", "Swimming"
		};

		/// <summary>Class names in class order (STRINGS.PAK block 2).</summary>
		public static readonly string[] ClassNames = { "Fighter", "Mage", "Bard", "Tinker", "Druid", "Paladin", "Ranger", "Shepherd" };

		public sealed class ClassEntry
		{
			public int Strength, Dexterity, Intelligence, BonusPoints;
			public readonly List<int>[] Choices = new List<int>[EntriesPerClass];
			public ClassEntry() { for (int i = 0; i < EntriesPerClass; i++) Choices[i] = new List<int>(); }
		}

		public readonly ClassEntry[] Entries = new ClassEntry[Classes];

		/// <summary>Bytes after the 40 entries (none in the shipped file); kept when saving.</summary>
		public byte[] Trailer = new byte[0];

		public static SkillsDat Parse(byte[] data)
		{
			if (data.Length < Classes * 4 + Classes * EntriesPerClass)
				throw new InvalidDataException("SKILLS.DAT is too short");
			var s = new SkillsDat();
			for (int c = 0; c < Classes; c++)
			{
				var e = new ClassEntry
				{
					Strength = data[c * 4], Dexterity = data[c * 4 + 1], Intelligence = data[c * 4 + 2], BonusPoints = data[c * 4 + 3]
				};
				s.Entries[c] = e;
			}
			int p = Classes * 4;
			for (int c = 0; c < Classes; c++)
				for (int k = 0; k < EntriesPerClass; k++)
				{
					if (p >= data.Length) throw new InvalidDataException("SKILLS.DAT ends inside the skill entries");
					int n = data[p++];
					if (p + n > data.Length) throw new InvalidDataException("SKILLS.DAT ends inside a skill entry");
					for (int i = 0; i < n; i++)
						s.Entries[c].Choices[k].Add(data[p++]);
				}
			s.Trailer = new byte[data.Length - p];
			Array.Copy(data, p, s.Trailer, 0, s.Trailer.Length);
			return s;
		}

		public byte[] ToBytes()
		{
			var o = new List<byte>();
			foreach (var e in Entries)
			{
				o.Add(Byte(e.Strength, "strength")); o.Add(Byte(e.Dexterity, "dexterity"));
				o.Add(Byte(e.Intelligence, "intelligence")); o.Add(Byte(e.BonusPoints, "bonus points"));
			}
			foreach (var e in Entries)
				foreach (var choice in e.Choices)
				{
					o.Add(Byte(choice.Count, "count"));
					foreach (int sk in choice)
					{
						if (sk < 0 || sk >= Skills) throw new InvalidDataException("Skill number " + sk + " is not 0-19");
						o.Add((byte)sk);
					}
				}
			o.AddRange(Trailer);
			// The game reads at most 0x348 bytes into the buffer it shares with CHRGEN.DAT
			// (CHARGEN.C: intoFarBuffer(fd, stdat, 0x348)) and needs at least 0x28.
			if (o.Count > 0x348) throw new InvalidDataException("SKILLS.DAT would exceed 0x348 bytes");
			return o.ToArray();
		}

		static byte Byte(int v, string what)
		{
			if (v < 0 || v > 255) throw new InvalidDataException(what + " must be 0-255");
			return (byte)v;
		}

		/// <summary>
		/// The attributes roll_stats gives the class: the base plus the bonus points spread
		/// 1-4 at a time over random attributes, none above 30. Returns the base and the most
		/// each attribute can reach.
		/// </summary>
		public static int MaxAttribute(int baseValue, int bonus) { return Math.Min(30, baseValue + bonus); }
	}

	/// <summary>
	/// DATA\CMB.DAT: up to ten item combinations of three words, first, second and output
	/// (obj/COMBINE.C). The low nine bits are an item id; bit 15 of a source says it is used
	/// up. A rule counts only when at least one source is used up; all ten are scanned.
	/// </summary>
	public sealed class CmbDat
	{
		public const int Rules = 10;
		public const int UsedUp = 0x8000;

		public struct Rule
		{
			public int First, Second, Output;   // raw words
			public int FirstItem { get { return First & 0x1FF; } }
			public int SecondItem { get { return Second & 0x1FF; } }
			public int OutputItem { get { return Output & 0x1FF; } }
			public bool FirstUsedUp { get { return (First & UsedUp) != 0; } }
			public bool SecondUsedUp { get { return (Second & UsedUp) != 0; } }
			/// <summary>ObjsBeCombinable ignores a rule with neither source used up.</summary>
			public bool Active { get { return FirstUsedUp || SecondUsedUp; } }
			public static Rule Make(int first, bool firstUsed, int second, bool secondUsed, int output)
			{
				return new Rule
				{
					First = (first & 0x1FF) | (firstUsed ? UsedUp : 0),
					Second = (second & 0x1FF) | (secondUsed ? UsedUp : 0),
					Output = output & 0x1FF
				};
			}
		}

		public readonly Rule[] Entries = new Rule[Rules];

		public static CmbDat Parse(byte[] data)
		{
			var c = new CmbDat();
			for (int i = 0; i < Rules; i++)
			{
				int o = i * 6;
				// The game reads sizeof(ObjectCombinations) = 60 bytes; a shorter file leaves zeros.
				c.Entries[i] = new Rule
				{
					First = Word(data, o), Second = Word(data, o + 2), Output = Word(data, o + 4)
				};
			}
			return c;
		}

		static int Word(byte[] d, int o) { return o + 1 < d.Length ? d[o] | d[o + 1] << 8 : 0; }

		public byte[] ToBytes()
		{
			var b = new byte[Rules * 6];
			for (int i = 0; i < Rules; i++)
			{
				Put(b, i * 6, Entries[i].First);
				Put(b, i * 6 + 2, Entries[i].Second);
				Put(b, i * 6 + 4, Entries[i].Output);
			}
			return b;
		}

		static void Put(byte[] b, int o, int v) { b[o] = (byte)v; b[o + 1] = (byte)(v >> 8); }

		/// <summary>Problems the game would hit: a rule that never matches, or duplicates.</summary>
		public List<string> Check()
		{
			var issues = new List<string>();
			for (int i = 0; i < Rules; i++)
			{
				var r = Entries[i];
				if (r.First == 0 && r.Second == 0 && r.Output == 0) continue;
				if (!r.Active) issues.Add(string.Format("Rule {0}: neither source is used up, so the game ignores it.", i + 1));
				if (r.OutputItem == 0) issues.Add(string.Format("Rule {0}: the output is item 0 (a hand axe).", i + 1));
				for (int j = 0; j < i; j++)
				{
					var q = Entries[j];
					if (!q.Active) continue;
					if ((q.FirstItem == r.FirstItem && q.SecondItem == r.SecondItem) || (q.FirstItem == r.SecondItem && q.SecondItem == r.FirstItem))
						issues.Add(string.Format("Rule {0} combines the same items as rule {1}; only rule {1} is ever used.", i + 1, j + 1));
				}
			}
			return issues;
		}
	}

	/// <summary>
	/// DATA\SHADES.DAT: 8 rows (light level 0 darkest to 7) of six int16 (map/LIGHTING.C):
	/// smooth_div, smooth_base, smooth_lowpass, curvrad (view radius 3-7), distpoly, dist8.
	/// The same 96 bytes in both games. Edited by Tools > Light levels (SHADES.DAT).
	/// </summary>
	public sealed class ShadesDat
	{
		public static readonly string[] Columns = { "smooth_div", "smooth_base", "smooth_lowpass", "curvrad", "distpoly", "dist8" };
		public static readonly string[] ColumnHelp =
		{
			"Slope: how fast it gets darker with distance (distance * 8 * sqrt 2 * this / 64). 1 or less is read as 1.",
			"Added to every shade: how dark even the nearest square is (0 full light, 14 darkest).",
			"Added before the base and may be negative: shifts where the darkening starts (the sum is clamped at 0 first).",
			"View radius in squares: squares farther away are not drawn at all (shade 15). The game uses 3-7; UW-Formats reports crashes above about 9 in busy areas.",
			"Shade from which floors and walls are drawn flat, in their average colour, instead of textured.",
			"Shade from which the renderer switches to the smaller copy of a texture (UW1; GRIDDB.C).",
		};
		/// <summary>What gives each light level: the light spells 0-7 of block 6 by number, and the lit
		/// lights' brightness in OBJECTS.DAT (candle 1, torch 2, taper 3, lantern 4; UW2's 0x97 is 6).
		/// Level 5 swaps in MONO.DAT's grey maps; the wizard eye uses 6 (PLAYDATA.C).</summary>
		public static readonly string[] LevelNames =
		{
			"0 darkness (Darkness)", "1 candle (Burning Match)", "2 torch (Candlelight)", "3 taper (Light)",
			"4 lantern (Magic Lantern)", "5 night vision (grey, MONO.DAT)", "6 Daylight, wizard eye", "7 Sunlight",
		};
		public readonly short[,] Rows = new short[8, 6];

		public static ShadesDat Parse(byte[] d)
		{
			if (d.Length < 96) throw new InvalidDataException("SHADES.DAT must be 96 bytes");
			var s = new ShadesDat();
			for (int r = 0; r < 8; r++)
				for (int c = 0; c < 6; c++)
					s.Rows[r, c] = (short)(d[r * 12 + c * 2] | d[r * 12 + c * 2 + 1] << 8);
			return s;
		}

		/// <summary>
		/// The shade of a square at distance i (0-15) from the eye at light level row, as
		/// preset_grid (3d/VIEW3D.C) computes it in both games: 15 beyond curvrad (not drawn),
		/// else min(14, max(0, (isqrt(128 i^2) * smooth_div >> 6) + smooth_lowpass) + smooth_base).
		/// </summary>
		public int[] ShadeByDistance(int row)
		{
			int div = Math.Max(1, (int)Rows[row, 0]), bas = Rows[row, 1], low = Rows[row, 2], range = Rows[row, 3];
			var shades = new int[16];
			for (int i = 0; i < 16; i++)
			{
				if (i > range) { shades[i] = 15; continue; }
				int t = (i << 8) >> 5;
				int u = ISqrt(t * t * 2);
				t = (u * div) >> 6;
				t += low;
				if (t < 0) t = 0;
				u = t + bas;
				if (u > 14) u = 14;
				shades[i] = u;
			}
			return shades;
		}

		/// <summary>The shade of the grid cell dx squares across and dy ahead (glocs: isqrt of the distance in squares).</summary>
		public int CellShade(int row, int dx, int dy)
		{
			int d = ISqrt(dx * dx + dy * dy);
			if (d > Rows[row, 3] || d > 15) return 15;
			return ShadeByDistance(row)[d];
		}

		public static int ISqrt(int v)
		{
			if (v <= 0) return 0;
			int r = (int)Math.Sqrt(v);
			while (r * r > v) r--;
			while ((r + 1) * (r + 1) <= v) r++;
			return r;
		}

		/// <summary>Warnings for a row (none means the game reads it as given).</summary>
		public string Check(int row)
		{
			var sb = new System.Text.StringBuilder();
			if (Rows[row, 0] <= 1) sb.Append("smooth_div 1 or less is read as 1. ");
			if (Rows[row, 3] < 1) sb.Append("A view radius under 1 shows almost nothing. ");
			if (Rows[row, 3] > 7) sb.Append("A view radius above 7 is untested; UW-Formats reports crashes above about 9. ");
			if (Rows[row, 3] > 15) sb.Append("Above 15 the grid is not rebuilt at all (preset_grid needs range < 16). ");
			return sb.ToString();
		}

		public byte[] ToBytes()
		{
			var d = new byte[96];
			for (int r = 0; r < 8; r++)
				for (int c = 0; c < 6; c++)
				{
					d[r * 12 + c * 2] = (byte)Rows[r, c];
					d[r * 12 + c * 2 + 1] = (byte)(Rows[r, c] >> 8);
				}
			return d;
		}
	}
}
