using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace UltimateEditor.Games
{
	/// <summary>
	/// UW2's SCD.ARK: 16 schedules, one per X clock (block 0 runs by the time of day, 72 steps;
	/// block n by X clock n). Layout (reference docs/FORMATS.md "SCD.ARK", uw2 include/event.h
	/// struct SCDWork): word row count, byte block number, byte spare, 80 level clocks of two
	/// words (time reached, next row), then the rows of 16 bytes sorted by time. The game
	/// writes rows * 16 + 0x144 bytes, uncompressed (flags 0).
	/// </summary>
	public sealed class ScdBlock
	{
		public const int HeaderSize = 0x144;
		public byte BlockByte;
		public byte Spare;
		public byte[] Clocks = new byte[80 * 4];
		public List<ScdRow> Rows = new List<ScdRow>();

		public static ScdBlock Parse(byte[] d)
		{
			var b = new ScdBlock();
			if (d == null || d.Length < HeaderSize) return b;
			int n = d[0] | d[1] << 8;
			b.BlockByte = d[2]; b.Spare = d[3];
			Array.Copy(d, 4, b.Clocks, 0, b.Clocks.Length);
			for (int i = 0; i < n && HeaderSize + 16 * i + 16 <= d.Length; i++)
			{
				var r = new byte[16];
				Array.Copy(d, HeaderSize + 16 * i, r, 0, 16);
				b.Rows.Add(new ScdRow(r));
			}
			return b;
		}

		public byte[] ToBytes()
		{
			var d = new byte[HeaderSize + 16 * Rows.Count];
			d[0] = (byte)Rows.Count; d[1] = (byte)(Rows.Count >> 8);
			d[2] = BlockByte; d[3] = Spare;
			Array.Copy(Clocks, 0, d, 4, Clocks.Length);
			for (int i = 0; i < Rows.Count; i++) Array.Copy(Rows[i].Bytes, 0, d, HeaderSize + 16 * i, 16);
			return d;
		}

		/// <summary>
		/// Puts a row at its place by time. Rows of equal time keep their order and the new
		/// one goes after them, but a test-variables row (10) owns the disabled rows that
		/// follow it, so callers inserting into such a block should insert by index instead.
		/// </summary>
		public int InsertByTime(ScdRow row)
		{
			int i = 0;
			while (i < Rows.Count && Rows[i].Time <= row.Time) i++;
			Rows.Insert(i, row);
			return i;
		}
	}

	/// <summary>One schedule row (struct SCDRow): word time, byte level, byte once, signed byte
	/// event, 11 bytes of parameters (offsets 5-15). Offsets below are within the row.</summary>
	public sealed class ScdRow
	{
		public readonly byte[] Bytes;
		public ScdRow(byte[] b) { Bytes = b; }
		public ScdRow() { Bytes = new byte[16]; }

		public int Time { get { return Bytes[0] | Bytes[1] << 8; } set { Bytes[0] = (byte)value; Bytes[1] = (byte)(value >> 8); } }
		/// <summary>0xFF any level, 0xF6 + n every level of world n, otherwise the level (1-based).</summary>
		public int Level { get { return Bytes[2]; } set { Bytes[2] = (byte)value; } }
		public bool Once { get { return Bytes[3] != 0; } set { Bytes[3] = (byte)(value ? 1 : 0); } }
		/// <summary>Negative: disabled (a test-variables row may run it).</summary>
		public int Event { get { return (sbyte)Bytes[4]; } set { Bytes[4] = (byte)(sbyte)value; } }
		public int Word(int at) { return Bytes[at] | Bytes[at + 1] << 8; }
		public void SetWord(int at, int v) { Bytes[at] = (byte)v; Bytes[at + 1] = (byte)(v >> 8); }

		public ScdRow Clone() { return new ScdRow((byte[])Bytes.Clone()); }

		public static readonly string[] EventNames =
		{
			"nothing", "change goal", "teleport", "kill", "set quest bit", "fire scheduled triggers", "nothing",
			"special case", "set attitude", "set variable", "test variables", "remove",
		};
		static readonly string[] hackNames = { "nothing", "gargoyle (variable when on a square)", "soldier (move into a rectangle)", "freeze floors", "close doors", "nothing", "Nystul (turn)", "Gotha (move into a rectangle)" };
		static readonly string[] ops = { "add", "subtract", "set to", "and", "or", "xor", "shift left by", "count up while equal to" };

		public static string LevelText(int level)
		{
			if (level == 0xFF) return "any level";
			if (level >= 0xF6) return "world " + (level - 0xF6) + " (" + GameProfile.UW2.WorldName(level - 0xF6) + ")";
			return "level " + level;
		}

		/// <summary>The critters an event acts on (gronk_critid): low byte the mode, high byte its value.</summary>
		public static string Critters(int w, Func<int, string> npcName = null, Func<int, string> raceName = null)
		{
			int mode = w & 0xFF, v = w >> 8 & 0xFF;
			switch (mode)
			{
				case 0: { string n = npcName != null ? npcName(v) : null; return "whoami " + v + (n != null ? " (" + n + ")" : ""); }
				case 1: { string n = raceName != null ? raceName(v) : null; return "race " + v + (n != null ? " (" + n + ")" : ""); }
				case 2: return "object " + v;
				case 3: return "every critter";
			}
			return "mode " + mode + " (none)";
		}

		/// <summary>What the row does (uw2 event/SCDEVENT.C).</summary>
		public string Describe(Func<int, string> npcName = null, Func<int, string> raceName = null)
		{
			int ev = Event;
			bool disabled = ev < 0;
			if (disabled) ev = -ev;
			var sb = new StringBuilder();
			if (disabled) sb.Append("[disabled] ");
			string what;
			switch (ev)
			{
				case 1: what = string.Format("{0}: goal {1}, target {2}", Critters(Word(5), npcName, raceName), Bytes[7], Bytes[8]); break;
				case 2:
					what = string.Format("{0}: teleport to ({1}, {2}) of level {3}{4}{5}{6}", Critters(Word(7), npcName, raceName), Bytes[5], Bytes[6], Bytes[9],
						Bytes[10] != 0 ? ", even if seen" : ", only unseen", Bytes[11] != 0 ? ", there is its new home" : "", Bytes[12] != 0 ? ", retry after " + Bytes[12] + " steps" : "");
					break;
				case 3: what = string.Format("{0}: kill{1}", Critters(Word(5), npcName, raceName), Bytes[7] == 0 ? " (not the one talking)" : ""); break;
				case 4: what = string.Format("quest bit {0} of quest {1} := {2}", Bytes[5] & 3, Bytes[5] >> 2, Bytes[6]); break;
				case 5: what = string.Format("fire the scheduled triggers on square ({0}, {1})", Bytes[5], Bytes[6]); break;
				case 7:
				{
					int h = Bytes[5];
					string hn = h < hackNames.Length ? hackNames[h] : "no handler";
					switch (h)
					{
						case 1: what = string.Format("hack 1, {0}: {1} on ({2}, {3}): {4} {5} {6}", hn, Critters(Word(6), npcName, raceName), Bytes[8], Bytes[9], Uw2Traps.VariableName(Word(10)), ops[Bytes[12] & 7], Word(13)); break;
						case 2: what = string.Format("hack 2, {0}: {1}{2} to ({3}, {4})-({5}, {6})", hn, Critters(Word(8), npcName, raceName), Bytes[6] != 0 || Bytes[7] != 0 ? " at (" + Bytes[6] + ", " + Bytes[7] + ")" : "", Bytes[10], Bytes[11], Bytes[12], Bytes[13]); break;
						case 3: what = string.Format("hack 3, {0}: about half the floors {1} become {2}, height + {3}", hn, Bytes[6], Bytes[7], Word(8)); break;
						case 4: what = string.Format("hack 4, {0}: argument {1}", hn, Bytes[6]); break;
						case 6: what = string.Format("hack 6, {0}: {1} faces heading {2}", hn, Critters(Word(6), npcName, raceName), Bytes[8]); break;
						case 7: what = string.Format("hack 7, {0}: {1} to ({2}, {3})-({4}, {5})", hn, Critters(Word(6), npcName, raceName), Bytes[8], Bytes[9], Bytes[10], Bytes[11]); break;
						default: what = "hack " + h + ", " + hn; break;
					}
					break;
				}
				case 8: what = string.Format("{0}: attitude {1}", Critters(Word(5), npcName, raceName), Bytes[7]); break;
				case 9: what = string.Format("{0} {1} {2}", Uw2Traps.VariableName(Word(5)), ops[Bytes[7] & 7], Word(8)); break;
				case 10:
				{
					int count = Bytes[7];
					string vars = count <= 1 ? Uw2Traps.VariableName(Word(5)) : Uw2Traps.VariableName(Word(5)) + " and the next " + (count - 1) + " combined by " + ops[Bytes[8] & 7];
					what = string.Format("if {0} {1} {2}: run the disabled rows that follow", vars, Bytes[9] != 0 ? "is not" : "is", (short)Word(10));
					break;
				}
				case 11: what = string.Format("{0}: remove from the map", Critters(Word(5), npcName, raceName)); break;
				case 0: case 6: what = "nothing"; break;
				default: what = "unknown event " + ev + " (the game skips it)"; break;
			}
			sb.Append(what);
			return sb.ToString();
		}
	}

	public static class ScdArk
	{
		public const int Blocks = 16;

		public static Uw2Archive Open(string path)
		{
			return (Uw2Archive)Archive.Load(path, GameKind.UW2);
		}

		public static List<ScdBlock> Read(Uw2Archive a)
		{
			var list = new List<ScdBlock>();
			for (int b = 0; b < Math.Min(Blocks, a.Count); b++)
				list.Add(a.Has(b) ? ScdBlock.Parse(a.Read(b)) : new ScdBlock());
			return list;
		}

		/// <summary>Writes the blocks whose bytes changed; returns their numbers.</summary>
		public static List<int> Write(Uw2Archive a, List<ScdBlock> blocks)
		{
			var written = new List<int>();
			for (int b = 0; b < blocks.Count && b < a.Count; b++)
			{
				var now = blocks[b].ToBytes();
				var old = a.Has(b) ? a.Read(b) : null;
				if (old != null && Same(old, now)) continue;
				if (old == null && blocks[b].Rows.Count == 0) continue;
				a.Write(b, now);
				written.Add(b);
			}
			return written;
		}

		static bool Same(byte[] x, byte[] y)
		{
			if (x.Length != y.Length) return false;
			for (int i = 0; i < x.Length; i++) if (x[i] != y[i]) return false;
			return true;
		}
	}
}
