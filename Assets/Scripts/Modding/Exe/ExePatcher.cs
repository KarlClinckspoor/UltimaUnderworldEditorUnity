using System;
using System.Collections.Generic;

namespace UltimateEditor.Modding.Exe
{
	/// <summary>Makes and applies exe-bytes patches.</summary>
	public static class ExePatcher
	{
		public const int Context = 16;

		/// <summary>
		/// The byte patches turning baseExe into modExe, leaving out the bytes in <paramref name="skip"/>
		/// (those of exe parameters). Fails (with the reasons) when the executables' lengths differ or
		/// a changed byte is part of a word DOS or the overlay manager relocates.
		/// </summary>
		public static ExeBytesComponent Diff(byte[] baseExe, byte[] modExe, string file, ICollection<int> skip, List<string> problems)
		{
			if (baseExe.Length != modExe.Length)
			{
				problems.Add(string.Format("{0} changed length ({1} to {2} bytes); only bytes of the same length can be patched", file, baseExe.Length, modExe.Length));
				return null;
			}
			var exe = new MzExe(baseExe);
			var reloc = exe.RelocatedWords();
			var c = new ExeBytesComponent { File = file };
			int i = 0;
			while (i < baseExe.Length)
			{
				if (baseExe[i] == modExe[i] || (skip != null && skip.Contains(i))) { i++; continue; }
				int start = i, last = i;
				// one patch for changes up to 4 bytes apart, within one region
				var loc = exe.Locate(start);
				int j = i + 1;
				while (j < baseExe.Length && j - last <= 4)
				{
					if (baseExe[j] != modExe[j] && (skip == null || !skip.Contains(j)))
					{
						var l2 = exe.Locate(j);
						if (l2.Segment != loc.Segment) break;
						last = j;
					}
					j++;
				}
				int n = last - start + 1;
				if (loc.Segment == -2)
					problems.Add(string.Format("{0} 0x{1:X}: the change is in the executable's headers or tables, which cannot be patched", file, start));
				else if (exe.TouchesRelocation(start, n, reloc))
					problems.Add(string.Format("{0} 0x{1:X} ({2}): the change touches a relocated segment word (a far address DOS fixes up at load)", file, start, loc));
				var p = new ExeBytesComponent.Patch
				{
					Segment = loc.Segment,
					Offset = loc.Offset,
					FileOffset = start,
					Base = Hashing.Hex(baseExe, start, n),
					Data = Hashing.Hex(modExe, start, n),
					Before = Hashing.Hex(baseExe, Math.Max(0, start - Context), start - Math.Max(0, start - Context)),
					After = Hashing.Hex(baseExe, start + n, Math.Min(Context, baseExe.Length - start - n)),
				};
				c.Patches.Add(p);
				i = last + 1;
			}
			return c.Patches.Count == 0 ? null : c;
		}

		/// <summary>
		/// Applies byte patches to an executable, the original or one changed by UltimaHacks. Each
		/// patch is put at its location (segment and offset) in this executable; if the bytes there
		/// are not the original ones, the patch's context is searched for in the same code region,
		/// and it must be found exactly once. <paramref name="forbidden"/>, if given, are file ranges
		/// a patch may not touch (what UltimaHacks wrote).
		/// </summary>
		public static void Apply(byte[] exe, ExeBytesComponent c, IList<KeyValuePair<int, int>> forbidden = null)
		{
			var mz = new MzExe(exe);
			var writes = new List<KeyValuePair<int, byte[]>>();
			foreach (var p in c.Patches)
			{
				byte[] b = Hashing.FromHex(p.Base), d = Hashing.FromHex(p.Data);
				var loc = new MzExe.Location { Segment = p.Segment, Offset = p.Offset };
				int at = mz.FileOffset(loc);
				if (at < 0 || !Matches(exe, at, b))
				{
					byte[] before = Hashing.FromHex(p.Before), after = Hashing.FromHex(p.After);
					var pattern = new byte[before.Length + b.Length + after.Length];
					Buffer.BlockCopy(before, 0, pattern, 0, before.Length);
					Buffer.BlockCopy(b, 0, pattern, before.Length, b.Length);
					Buffer.BlockCopy(after, 0, pattern, before.Length + b.Length, after.Length);
					int rs, re;
					mz.Region(loc, out rs, out re);
					int found = -1, hits = 0;
					for (int k = rs; k + pattern.Length <= re; k++)
						if (Matches(exe, k, pattern)) { found = k; if (++hits > 1) break; }
					if (hits != 1)
						throw new ModApplyException(string.Format("{0}: the original bytes at {1} are not in this executable{2}", c.File, loc, hits > 1 ? " (they occur more than once)" : ""));
					at = found + before.Length;
				}
				if (forbidden != null)
					foreach (var r in forbidden)
						if (at < r.Value && at + b.Length > r.Key)
							throw new ModApplyException(string.Format("{0}: the patch at {1} overlaps a change UltimaHacks makes", c.File, loc));
				writes.Add(new KeyValuePair<int, byte[]>(at, d));
			}
			foreach (var w in writes) Buffer.BlockCopy(w.Value, 0, exe, w.Key, w.Value.Length);
		}

		static bool Matches(byte[] a, int at, byte[] b)
		{
			if (at < 0 || at + b.Length > a.Length) return false;
			for (int i = 0; i < b.Length; i++) if (a[at + i] != b[i]) return false;
			return true;
		}
	}
}
