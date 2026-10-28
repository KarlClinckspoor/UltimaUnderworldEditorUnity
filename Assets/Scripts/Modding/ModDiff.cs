using System;
using System.Collections.Generic;
using UltimateEditor.Games;
using UltimateEditor.Modding.Exe;

namespace UltimateEditor.Modding
{
	/// <summary>What ModDiff found: the components, their payload, and what stops an export.</summary>
	public sealed class ModDiffResult
	{
		public readonly List<ModComponent> Components = new List<ModComponent>();
		public readonly Dictionary<string, byte[]> Payload = new Dictionary<string, byte[]>(StringComparer.Ordinal);
		/// <summary>Hash of the original of every file a component changes.</summary>
		public readonly SortedDictionary<string, string> BaseFiles = new SortedDictionary<string, string>(StringComparer.Ordinal);
		/// <summary>Reasons the mod cannot be exported as it is.</summary>
		public readonly List<string> Problems = new List<string>();
		/// <summary>Things worth knowing that do not stop the export.</summary>
		public readonly List<string> Notes = new List<string>();
	}

	/// <summary>
	/// Compares a game folder with its original files and describes the difference as mod
	/// components, each at the finest level its file allows: archive blocks, strings, pictures,
	/// byte ranges, executable settings and patches; whole files only for new files and files
	/// with no finer kind whose length changed.
	/// </summary>
	public static class ModDiff
	{
		public static ModDiffResult Compare(IFileSource original, IFileSource modded, GameKind game)
		{
			var r = new ModDiffResult();
			var paths = new SortedSet<string>(StringComparer.Ordinal);
			foreach (var p in original.List()) paths.Add(GameFolder.Normalize(p));
			foreach (var p in modded.List()) paths.Add(GameFolder.Normalize(p));
			foreach (var rel in paths)
			{
				byte[] a = original.Read(rel), b = modded.Read(rel);
				if (a != null && b != null && Equal(a, b)) continue;
				if (a == null && b == null) continue;
				int before = r.Components.Count;
				try { CompareFile(rel, a, b, game, r); }
				catch (Exception e) { r.Problems.Add(rel + ": " + e.Message); }
				if (r.Components.Count > before || a == null) r.BaseFiles[rel] = Hashing.Sha256(a);
			}
			return r;
		}

		static void CompareFile(string rel, byte[] a, byte[] b, GameKind game, ModDiffResult r)
		{
			string name = rel.Substring(rel.LastIndexOf('/') + 1);
			if (a == null)
			{
				AddFile(rel, null, b, r);
				return;
			}
			if (b == null)
			{
				r.Components.Add(new FileComponent { File = rel, Base = Hashing.Sha256(a), Delete = true });
				r.Notes.Add(rel + " is removed by the mod");
				return;
			}
			if (rel == ExeIdentity.ExeName(game)) { CompareExe(rel, a, b, game, r); return; }
			if (name.EndsWith(".ARK")) { if (CompareArk(rel, a, b, game, r)) return; }
			else if (name == "STRINGS.PAK") { if (CompareStrings(rel, a, b, r)) return; }
			else if (name.EndsWith(".GR") || name.EndsWith(".TR")) { if (ComparePictures(rel, a, b, r)) return; }
			if (a.Length == b.Length) { AddBytes(rel, a, b, r); return; }
			AddFile(rel, a, b, r);
		}

		static void AddFile(string rel, byte[] a, byte[] b, ModDiffResult r)
		{
			string data = "payload/files/" + rel;
			r.Payload[data] = b;
			r.Components.Add(new FileComponent { File = rel, Base = Hashing.Sha256(a), Data = data });
		}

		/// <summary>Ranges of changed bytes, joining changes up to 8 bytes apart.</summary>
		static void AddBytes(string rel, byte[] a, byte[] b, ModDiffResult r)
		{
			var c = new BytesComponent { File = rel, Length = a.Length };
			int i = 0;
			while (i < a.Length)
			{
				if (a[i] == b[i]) { i++; continue; }
				int start = i, last = i;
				for (int j = i + 1; j < a.Length && j - last <= 8; j++) if (a[j] != b[j]) last = j;
				int n = last - start + 1;
				c.Ranges.Add(new BytesComponent.Range { Offset = start, Base = Hashing.Hex(a, start, n), Data = Hashing.Hex(b, start, n) });
				i = last + 1;
			}
			r.Components.Add(c);
		}

		static bool CompareArk(string rel, byte[] a, byte[] b, GameKind game, ModDiffResult r)
		{
			Archive x, y;
			try { x = Archive.Open(a, game); y = Archive.Open(b, game); }
			catch (Exception e) { r.Notes.Add(rel + " could not be read as an archive (" + e.Message + "); it is compared byte by byte"); return false; }
			if (x.Count != y.Count) { r.Notes.Add(rel + ": the archive's block count changed; it is kept as a whole file"); return false; }
			var c = new ArkBlocksComponent { File = rel, Archive = game == GameKind.UW2 ? "uw2" : "uw1" };
			string dir = "payload/ark/" + rel.Replace('/', '_') + "/";
			for (int i = 0; i < x.Count; i++)
			{
				byte[] p = x.Has(i) ? x.Read(i) : null, q = y.Has(i) ? y.Read(i) : null;
				if (p == null && q == null) continue;
				if (p != null && q != null && Equal(p, q)) continue;
				var blk = new ArkBlocksComponent.Block { Index = i, Base = Hashing.Sha256(p) };
				if (q == null) blk.Delete = true;
				else { blk.Data = dir + i.ToString("D4") + ".bin"; r.Payload[blk.Data] = q; }
				c.Blocks.Add(blk);
			}
			if (c.Blocks.Count > 0) r.Components.Add(c);
			else r.Notes.Add(rel + ": the archive was rewritten but no block changed");
			return true;
		}

		static bool CompareStrings(string rel, byte[] a, byte[] b, ModDiffResult r)
		{
			StringsPakFile x, y;
			try { x = StringsPakFile.Parse(a); y = StringsPakFile.Parse(b); }
			catch (Exception e) { r.Notes.Add(rel + " could not be decoded (" + e.Message + ")"); return false; }
			if (!x.SameTree(y)) { r.Notes.Add(rel + ": the Huffman tree changed (new characters); it is kept as a whole file"); return false; }
			var ch = new StringChanges();
			foreach (var yb in y.Blocks)
			{
				var xb = x.Find(yb.Number);
				var bc = new StringChanges.BlockChange { Block = yb.Number, Added = xb == null, BaseCount = xb == null ? 0 : xb.Strings.Count, Count = yb.Strings.Count };
				for (int i = 0; i < yb.Strings.Count; i++)
				{
					string old = xb != null && i < xb.Strings.Count ? xb.Strings[i] : null;
					if (old != null && old == yb.Strings[i]) continue;
					bc.Set.Add(new StringChanges.StringSet { Index = i, Base = Hashing.Sha256(old), Text = yb.Strings[i] });
				}
				if (bc.Added || bc.Set.Count > 0 || bc.Count != bc.BaseCount) ch.Blocks.Add(bc);
			}
			foreach (var xb in x.Blocks)
				if (y.Find(xb.Number) == null)
					ch.Blocks.Add(new StringChanges.BlockChange { Block = xb.Number, Removed = true, BaseCount = xb.Strings.Count, Count = 0 });
			if (ch.Blocks.Count == 0) { r.Notes.Add(rel + ": rewritten with the same strings"); return true; }
			string data = "payload/strings/" + rel.Replace('/', '_') + ".json";
			r.Payload[data] = System.Text.Encoding.UTF8.GetBytes(ch.ToJson());
			r.Components.Add(new StringsComponent { File = rel, Data = data, Count = ch.StringCount });
			return true;
		}

		static bool ComparePictures(string rel, byte[] a, byte[] b, ModDiffResult r)
		{
			var x = PictureFile.TryParse(a);
			var y = PictureFile.TryParse(b);
			if (x == null || y == null || x.Type != y.Type || x.SizeByte != y.SizeByte) return false;
			var c = new ImagesComponent { File = rel, BaseCount = x.Records.Count, Count = y.Records.Count };
			string dir = "payload/images/" + rel.Replace('/', '_') + "/";
			for (int i = 0; i < y.Records.Count; i++)
			{
				byte[] p = i < x.Records.Count ? x.Records[i] : null, q = y.Records[i];
				if (p != null && Equal(p, q)) continue;
				string data = dir + i.ToString("D4") + ".bin";
				r.Payload[data] = q;
				c.Pictures.Add(new ImagesComponent.Picture { Index = i, Base = Hashing.Sha256(p), Data = data });
			}
			if (c.Pictures.Count == 0 && c.Count == c.BaseCount) { r.Notes.Add(rel + ": rewritten with the same pictures"); return true; }
			r.Components.Add(c);
			return true;
		}

		static void CompareExe(string rel, byte[] a, byte[] b, GameKind game, ModDiffResult r)
		{
			var skip = new HashSet<int>();
			foreach (var p in ExeParams.Diff(a, b, game))
			{
				r.Components.Add(p);
				foreach (int at in ExeParams.Bytes(a, game, p.Param)) skip.Add(at);
			}
			// a parameter changed in both: its bytes in either executable are the parameter's
			foreach (var pc in new[] { ExeParams.StartSquare }) foreach (int at in ExeParams.Bytes(b, game, pc)) if (skip.Count > 0) skip.Add(at);
			var bytes = ExePatcher.Diff(a, b, rel, skip, r.Problems);
			if (bytes != null)
			{
				r.Components.Add(bytes);
				r.Notes.Add(string.Format("{0}: {1} byte patch(es); Exhumed plays them only where they change the executable's data (fork feature exe-patch)", rel, bytes.Patches.Count));
			}
		}

		public static bool Equal(byte[] a, byte[] b)
		{
			if (a.Length != b.Length) return false;
			for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
			return true;
		}
	}
}
