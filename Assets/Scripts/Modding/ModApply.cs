using System;
using System.Collections.Generic;
using System.Text;
using UltimateEditor.Games;
using UltimateEditor.Modding.Exe;

namespace UltimateEditor.Modding
{
	/// <summary>
	/// Applies a mod's components to the original files. Every element (block, string, picture,
	/// byte range, whole file) carries the hash or bytes of the original it was made from, and
	/// applying refuses when the file in hand is not that original: a mod never writes over
	/// something it did not expect.
	/// </summary>
	public static class ModApply
	{
		/// <summary>
		/// The new contents of every file the mod changes: path to bytes, or to null for a file the
		/// mod removes. <paramref name="original"/> gives the original files. The executable is not
		/// included (ApplyExe builds it for a target); saves are not either (SaveMods).
		/// </summary>
		public static Dictionary<string, byte[]> Files(ModPackage pkg, Func<string, byte[]> original)
		{
			var m = pkg.Manifest;
			var game = m.Game == "uw2" ? GameKind.UW2 : GameKind.UW1;
			var outp = new Dictionary<string, byte[]>(StringComparer.Ordinal);
			var errors = new List<string>();
			foreach (var c in m.Components)
			{
				if (c is ExeParamComponent || c is ExeBytesComponent || c is SaveComponent) continue;
				string rel = GameFolder.Normalize(c.File);
				byte[] cur = outp.ContainsKey(rel) ? outp[rel] : original(rel);
				try { outp[rel] = ApplyOne(c, cur, pkg, game); }
				catch (ModApplyException e) { errors.Add(rel + ": " + e.Message); }
			}
			if (errors.Count > 0) throw new ModApplyException(string.Join("\n", errors));
			return outp;
		}

		/// <summary>The components that change the executable.</summary>
		public static bool ChangesExe(ModManifest m)
		{
			foreach (var c in m.Components) if (c is ExeParamComponent || c is ExeBytesComponent) return true;
			return false;
		}

		/// <summary>
		/// The mod's executable changes applied to <paramref name="exe"/> (the original, or the
		/// original after UltimaHacks; in place). <paramref name="hackWrites"/> are the ranges the
		/// hack wrote, which byte patches may not touch.
		/// </summary>
		public static void ApplyExe(ModPackage pkg, byte[] exe, IList<KeyValuePair<int, int>> hackWrites = null)
		{
			var game = pkg.Manifest.Game == "uw2" ? GameKind.UW2 : GameKind.UW1;
			foreach (var c in pkg.Manifest.Components)
			{
				var p = c as ExeParamComponent;
				if (p != null) ExeParams.Apply(exe, game, p);
				var b = c as ExeBytesComponent;
				if (b != null) ExePatcher.Apply(exe, b, hackWrites);
			}
		}

		public static byte[] ApplyOne(ModComponent c, byte[] cur, ModPackage pkg, GameKind game)
		{
			var f = c as FileComponent;
			if (f != null)
			{
				if (Hashing.Sha256(cur) != f.Base) throw new ModApplyException(cur == null ? "the file is missing" : "the file is not the one the mod was made from");
				return f.Delete ? null : pkg.Payload(f.Data);
			}
			if (cur == null) throw new ModApplyException("the file is missing");
			var a = c as ArkBlocksComponent;
			if (a != null) return ApplyArk(a, cur, pkg, game);
			var s = c as StringsComponent;
			if (s != null) return ApplyStrings(s, cur, pkg);
			var im = c as ImagesComponent;
			if (im != null) return ApplyImages(im, cur, pkg);
			var by = c as BytesComponent;
			if (by != null) return ApplyBytes(by, cur);
			throw new ModApplyException("a " + c.Kind + " component cannot be applied to a file");
		}

		static byte[] ApplyArk(ArkBlocksComponent c, byte[] cur, ModPackage pkg, GameKind game)
		{
			var kind = c.Archive == "uw2" ? GameKind.UW2 : GameKind.UW1;
			var ar = Archive.Open(cur, kind);
			var bad = new List<string>();
			foreach (var b in c.Blocks)
			{
				byte[] old = ar.Has(b.Index) ? ar.Read(b.Index) : null;
				if (Hashing.Sha256(old) != b.Base) bad.Add(b.Index.ToString());
			}
			if (bad.Count > 0) throw new ModApplyException("block" + (bad.Count == 1 ? " " : "s ") + string.Join(", ", bad) + " are not the ones the mod was made from");
			foreach (var b in c.Blocks)
			{
				if (b.Index < 0 || b.Index >= ar.Count) throw new ModApplyException("block " + b.Index + " is past the archive's " + ar.Count + " blocks");
				if (b.Delete) ar.Delete(b.Index);
				else ar.Write(b.Index, pkg.Payload(b.Data));
			}
			return ar.ToArray();
		}

		static byte[] ApplyStrings(StringsComponent c, byte[] cur, ModPackage pkg)
		{
			var file = StringsPakFile.Parse(cur);
			var ch = StringChanges.Parse(Encoding.UTF8.GetString(pkg.Payload(c.Data)));
			var bad = new List<string>();
			var changed = new HashSet<int>();
			foreach (var bc in ch.Blocks)
			{
				var blk = file.Find(bc.Block);
				if (bc.Removed)
				{
					if (blk == null || blk.Strings.Count != bc.BaseCount) { bad.Add("block " + bc.Block); continue; }
					file.Blocks.Remove(blk);
					continue;
				}
				if (bc.Added)
				{
					if (blk != null) { bad.Add("block " + bc.Block + " (already there)"); continue; }
					blk = new StringsPakFile.Block { Number = bc.Block, Strings = new List<string>() };
					file.Blocks.Add(blk);
				}
				else if (blk == null || blk.Strings.Count != bc.BaseCount) { bad.Add("block " + bc.Block); continue; }
				foreach (var st in bc.Set)
				{
					string old = st.Index < blk.Strings.Count ? blk.Strings[st.Index] : null;
					if (Hashing.Sha256(old) != st.Base) { bad.Add("string " + st.Index + " of block " + bc.Block); continue; }
				}
				while (blk.Strings.Count < bc.Count) blk.Strings.Add("");
				if (blk.Strings.Count > bc.Count) blk.Strings.RemoveRange(bc.Count, blk.Strings.Count - bc.Count);
				foreach (var st in bc.Set) if (st.Index < blk.Strings.Count) blk.Strings[st.Index] = st.Text;
				changed.Add(bc.Block);
			}
			if (bad.Count > 0) throw new ModApplyException(Limit(bad) + " not the ones the mod was made from");
			return file.Build(changed);
		}

		static byte[] ApplyImages(ImagesComponent c, byte[] cur, ModPackage pkg)
		{
			var file = PictureFile.TryParse(cur);
			if (file == null) throw new ModApplyException("not a picture file this program can rebuild");
			if (file.Records.Count != c.BaseCount) throw new ModApplyException(string.Format("the file has {0} pictures, the mod was made from one with {1}", file.Records.Count, c.BaseCount));
			var bad = new List<string>();
			foreach (var p in c.Pictures)
			{
				byte[] old = p.Index < file.Records.Count ? file.Records[p.Index] : null;
				if (Hashing.Sha256(old) != p.Base) bad.Add("picture " + p.Index);
			}
			if (bad.Count > 0) throw new ModApplyException(Limit(bad) + " not the ones the mod was made from");
			while (file.Records.Count < c.Count) file.Records.Add(new byte[0]);
			if (file.Records.Count > c.Count) file.Records.RemoveRange(c.Count, file.Records.Count - c.Count);
			foreach (var p in c.Pictures) file.Records[p.Index] = pkg.Payload(p.Data);
			return file.Build();
		}

		static byte[] ApplyBytes(BytesComponent c, byte[] cur)
		{
			if (cur.Length != c.Length) throw new ModApplyException(string.Format("the file is {0} bytes, the mod was made from one of {1}", cur.Length, c.Length));
			var r = (byte[])cur.Clone();
			var bad = new List<string>();
			foreach (var g in c.Ranges)
			{
				byte[] old = Hashing.FromHex(g.Base), neu = Hashing.FromHex(g.Data);
				if (old.Length != neu.Length || g.Offset < 0 || g.Offset + old.Length > r.Length) { bad.Add("range at " + g.Offset); continue; }
				for (int i = 0; i < old.Length; i++) if (r[g.Offset + i] != old[i]) { bad.Add("bytes at 0x" + g.Offset.ToString("X")); break; }
			}
			if (bad.Count > 0) throw new ModApplyException(Limit(bad) + " not the ones the mod was made from");
			foreach (var g in c.Ranges)
			{
				byte[] neu = Hashing.FromHex(g.Data);
				Buffer.BlockCopy(neu, 0, r, g.Offset, neu.Length);
			}
			return r;
		}

		static string Limit(List<string> l)
		{
			if (l.Count <= 5) return string.Join(", ", l) + (l.Count == 1 ? " is" : " are");
			return string.Join(", ", l.GetRange(0, 5)) + " and " + (l.Count - 5) + " more are";
		}
	}
}
