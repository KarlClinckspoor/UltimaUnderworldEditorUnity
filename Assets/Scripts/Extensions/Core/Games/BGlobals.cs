using System;
using System.Collections.Generic;

namespace UltimateEditor.Games
{
	/// <summary>
	/// A conversation's private globals as a save keeps them: BGLOBALS.DAT is word pairs
	/// (conversation slot, number of words), each followed by that many words; DATA's
	/// BABGLOBS.DAT is the same pairs without the words, and a new game copies it with the
	/// words zeroed. Records are in slot order; the game reads at most the script's babl_nvars
	/// words of a record (uw1/uw2 conv/BABL.C init_babl, bab_get_globals, bab_put_globals).
	/// Both games.
	/// </summary>
	public sealed class BGlobals
	{
		public sealed class Record
		{
			public int Slot;
			public int[] Words;
		}

		public readonly List<Record> Records = new List<Record>();
		/// <summary>Bytes after the last whole record, kept as they were.</summary>
		public byte[] Trailer = new byte[0];

		/// <summary>withWords: BGLOBALS.DAT (a save); false for BABGLOBS.DAT (DATA).</summary>
		public static BGlobals Parse(byte[] d, bool withWords)
		{
			var g = new BGlobals();
			int p = 0;
			while (p + 4 <= d.Length)
			{
				int slot = d[p] | d[p + 1] << 8, n = d[p + 2] | d[p + 3] << 8;
				int size = 4 + (withWords ? 2 * n : 0);
				if (p + size > d.Length) break;
				var r = new Record { Slot = slot, Words = new int[n] };
				if (withWords)
					for (int i = 0; i < n; i++) r.Words[i] = (short)(d[p + 4 + 2 * i] | d[p + 5 + 2 * i] << 8);
				g.Records.Add(r);
				p += size;
			}
			g.Trailer = new byte[d.Length - p];
			Array.Copy(d, p, g.Trailer, 0, g.Trailer.Length);
			return g;
		}

		public byte[] ToBytes(bool withWords)
		{
			var o = new List<byte>();
			foreach (var r in Records)
			{
				o.Add((byte)r.Slot); o.Add((byte)(r.Slot >> 8));
				o.Add((byte)r.Words.Length); o.Add((byte)(r.Words.Length >> 8));
				if (withWords) foreach (int w in r.Words) { o.Add((byte)w); o.Add((byte)(w >> 8)); }
			}
			o.AddRange(Trailer);
			return o.ToArray();
		}

		public Record Find(int slot) { return Records.Find(r => r.Slot == slot); }

		/// <summary>Resizes a record to n words (new words 0), as a changed babl_nvars in DATA needs.</summary>
		public void Resize(int slot, int n)
		{
			var r = Find(slot);
			if (r == null)
			{
				r = new Record { Slot = slot, Words = new int[0] };
				int at = Records.FindIndex(x => x.Slot > slot);
				if (at < 0) Records.Add(r); else Records.Insert(at, r);
			}
			var w = new int[n];
			Array.Copy(r.Words, w, Math.Min(n, r.Words.Length));
			r.Words = w;
		}
	}
}
