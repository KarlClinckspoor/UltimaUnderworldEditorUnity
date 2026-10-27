using System;
using System.Collections.Generic;

namespace UltimateEditor.Games
{
	/// <summary>
	/// A .GR picture file, the same in both games (UW-Formats 3.2; uw2 gfx/LOADGR.C): a type
	/// byte (1), a word count of pictures, then count + 1 doubleword offsets. The game takes a
	/// picture's size from the next offset (_ld_gr, gronk_gr), so the pictures lie one after
	/// another in index order and the last offset is the end of the file; a picture whose
	/// offset equals the next one is empty (load_obj_ems gives it no slot).
	///
	/// A picture starts with its type, width and height. Type 4 is 8-bit: a word size
	/// (width * height) and one palette index a pixel. Type 8 is 4-bit run-length encoded: an
	/// auxiliary palette byte, a word count of nibbles and the nibbles, high one first.
	/// Type 0xA is 4-bit unpacked: the auxiliary palette, a word count of nibbles, the nibbles.
	/// Pixels run row by row from the top.
	///
	/// Rebuild keeps every picture it is not given byte for byte, so a file saved without
	/// changes comes out the same.
	/// </summary>
	public static class GrFile
	{
		public const int TypeByte = 1;

		public sealed class Picture
		{
			public int Type, Width, Height, Aux;
			/// <summary>Palette indices (type 4) or auxiliary palette indices (types 8, 0xA), top row first.</summary>
			public int[] Pixels;
		}

		public static int Count(byte[] file) { return file[1] | file[2] << 8; }

		/// <summary>The count + 1 offsets.</summary>
		public static int[] Offsets(byte[] file)
		{
			if (file == null || file.Length < 3 || file[0] != TypeByte) throw new FormatException("not a .GR file");
			int n = Count(file);
			if (3 + 4 * (n + 1) > file.Length) throw new FormatException("offset table runs past the end of the file");
			var o = new int[n + 1];
			for (int i = 0; i <= n; i++)
				o[i] = file[3 + 4 * i] | file[4 + 4 * i] << 8 | file[5 + 4 * i] << 16 | file[6 + 4 * i] << 24;
			return o;
		}

		/// <summary>True when the pictures lie in order, one after another, from the end of the table to the end of the file.</summary>
		public static bool Contiguous(byte[] file, int[] o)
		{
			int n = o.Length - 1;
			if (o[0] != 3 + 4 * (n + 1) || o[n] != file.Length) return false;
			for (int i = 0; i < n; i++) if (o[i] > o[i + 1]) return false;
			return true;
		}

		public static bool IsEmpty(int[] o, int i) { return o[i] == o[i + 1]; }

		/// <summary>Picture i, or null when it is empty.</summary>
		public static Picture Decode(byte[] file, int[] o, int i)
		{
			if (IsEmpty(o, i)) return null;
			int at = o[i];
			var p = new Picture { Type = file[at], Width = file[at + 1], Height = file[at + 2] };
			int n = p.Width * p.Height;
			p.Pixels = new int[n];
			if (p.Type == 4)
			{
				for (int k = 0; k < n; k++) p.Pixels[k] = file[at + 5 + k];
				return p;
			}
			p.Aux = file[at + 3];
			int nibbles = file[at + 4] | file[at + 5] << 8;
			var nib = new int[nibbles];
			for (int k = 0; k < nibbles; k++)
			{
				int b = file[at + 6 + k / 2];
				nib[k] = (k & 1) == 0 ? b >> 4 : b & 0xF;
			}
			if (p.Type == 0xA)
			{
				for (int k = 0; k < n && k < nibbles; k++) p.Pixels[k] = nib[k];
				return p;
			}
			if (p.Type != 8) throw new FormatException("picture " + i + " has type " + p.Type);
			DecodeRle(nib, p.Pixels);
			return p;
		}

		/// <summary>The 4-bit run-length code: repeat and run records take turns, starting with a
		/// repeat (a count, then the nibble repeated). A repeat count of 1 goes straight to the run;
		/// 2 is followed by the number of repeat records that come next. A run is a count and that
		/// many nibbles. A count is one nibble, or 0 and two nibbles, or 0, 0, 0 and three.</summary>
		public static void DecodeRle(int[] nib, int[] pixels)
		{
			int ptr = 0, px = 0, state = 0, repeats = 0;
			Func<int> next = () => ptr < nib.Length ? nib[ptr++] : 0;
			Func<int> count = () =>
			{
				int c = next();
				if (c == 0) { c = next() << 4; c |= next(); }
				if (c == 0) { c = next() << 8; c |= next() << 4; c |= next(); }
				return c;
			};
			while (px < pixels.Length && ptr < nib.Length)
			{
				if (state == 0)
				{
					int c = count();
					if (c == 1) state = 2;
					else if (c == 2) repeats = count() - 1;
					else
					{
						int v = next();
						for (int k = 0; k < c && px < pixels.Length; k++) pixels[px++] = v;
						if (repeats == 0) state = 2; else repeats--;
					}
				}
				else
				{
					int c = count();
					for (int k = 0; k < c && px < pixels.Length; k++) pixels[px++] = next();
					state = 0;
				}
			}
		}

		public static List<int> EncodeRle(int[] pixels)
		{
			var o = new List<int>();
			int n = pixels.Length, i = 0;
			bool lastWasRepeat = false;
			while (i < n)
			{
				// repeat records: every stretch of three or more equal pixels from here
				var reps = new List<int[]>();
				while (i < n)
				{
					int len = RunLength(pixels, i);
					if (len < 3) break;
					reps.Add(new[] { len, pixels[i] });
					i += len;
				}
				if (reps.Count == 0) PutCount(o, 1);
				else
				{
					if (reps.Count > 1) { PutCount(o, 2); PutCount(o, reps.Count); }
					foreach (var r in reps) { PutCount(o, r[0]); o.Add(r[1]); }
				}
				lastWasRepeat = reps.Count > 0;
				if (i >= n) break;
				// a run of single pixels, up to the next stretch of three
				int start = i;
				while (i < n && i - start < 0xFFF && RunLength(pixels, i) < 3) i++;
				PutCount(o, i - start);
				for (int k = start; k < i; k++) o.Add(pixels[k] & 0xF);
				lastWasRepeat = false;
			}
			// End the way the shipped pictures do, which the editor's own reader relies on
			// (DataReader.Read4BitGraphic drops an odd last nibble; Convert4BitToRawTex keeps
			// reading until one nibble before the end): after a final repeat, "1" (go to the
			// run) and a 0; after a final run nothing, plus a 0 when the count is odd.
			if (lastWasRepeat) { o.Add(1); o.Add(0); }
			else if ((o.Count & 1) != 0) o.Add(0);
			return o;
		}

		static int RunLength(int[] p, int i)
		{
			int j = i + 1;
			while (j < p.Length && p[j] == p[i] && j - i < 0xFFF) j++;
			return j - i;
		}

		static void PutCount(List<int> o, int c)
		{
			if (c <= 0 || c > 0xFFF) throw new ArgumentOutOfRangeException("c");
			if (c < 16) o.Add(c);
			else if (c < 256) { o.Add(0); o.Add(c >> 4); o.Add(c & 0xF); }
			else { o.Add(0); o.Add(0); o.Add(0); o.Add(c >> 8); o.Add((c >> 4) & 0xF); o.Add(c & 0xF); }
		}

		/// <summary>The bytes of a picture in its type.</summary>
		public static byte[] Encode(Picture p)
		{
			int n = p.Width * p.Height;
			if (p.Width < 1 || p.Width > 255 || p.Height < 1 || p.Height > 255) throw new ArgumentException("pictures are 1 to 255 pixels each way");
			if (p.Pixels == null || p.Pixels.Length != n) throw new ArgumentException("pixel count does not match the size");
			if (p.Type == 4)
			{
				var b = new byte[5 + n];
				b[0] = 4; b[1] = (byte)p.Width; b[2] = (byte)p.Height; b[3] = (byte)n; b[4] = (byte)(n >> 8);
				for (int k = 0; k < n; k++) b[5 + k] = (byte)p.Pixels[k];
				return b;
			}
			List<int> nib;
			if (p.Type == 8) nib = EncodeRle(p.Pixels);
			else if (p.Type == 0xA) { nib = new List<int>(n); foreach (var v in p.Pixels) nib.Add(v & 0xF); }
			else throw new ArgumentException("type " + p.Type + " cannot be written");
			if (nib.Count > 0xFFFF) throw new ArgumentException("picture too large");
			var r = new byte[6 + (nib.Count + 1) / 2];
			r[0] = (byte)p.Type; r[1] = (byte)p.Width; r[2] = (byte)p.Height; r[3] = (byte)p.Aux;
			r[4] = (byte)nib.Count; r[5] = (byte)(nib.Count >> 8);
			for (int k = 0; k < nib.Count; k++)
				r[6 + k / 2] |= (byte)((k & 1) == 0 ? nib[k] << 4 : nib[k]);
			return r;
		}

		/// <summary>The file with the given pictures replaced (by index); the others keep their
		/// bytes, empty ones stay empty. The pictures are laid out again in order.</summary>
		public static byte[] Rebuild(byte[] file, IDictionary<int, Picture> replace)
		{
			var o = Offsets(file);
			int n = o.Length - 1;
			if (!Contiguous(file, o)) throw new FormatException("pictures are not stored in order");
			var parts = new byte[n][];
			int size = 3 + 4 * (n + 1);
			for (int i = 0; i < n; i++)
			{
				Picture p;
				if (replace != null && replace.TryGetValue(i, out p) && p != null) parts[i] = Encode(p);
				else
				{
					parts[i] = new byte[o[i + 1] - o[i]];
					Buffer.BlockCopy(file, o[i], parts[i], 0, parts[i].Length);
				}
				size += parts[i].Length;
			}
			var r = new byte[size];
			r[0] = TypeByte; r[1] = (byte)n; r[2] = (byte)(n >> 8);
			int at = 3 + 4 * (n + 1);
			for (int i = 0; i <= n; i++)
			{
				PutLong(r, 3 + 4 * i, at);
				if (i < n) { Buffer.BlockCopy(parts[i], 0, r, at, parts[i].Length); at += parts[i].Length; }
			}
			return r;
		}

		static void PutLong(byte[] b, int at, int v)
		{
			b[at] = (byte)v; b[at + 1] = (byte)(v >> 8); b[at + 2] = (byte)(v >> 16); b[at + 3] = (byte)(v >> 24);
		}
	}
}
