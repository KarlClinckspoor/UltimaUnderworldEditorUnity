using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UltimateEditor.Games;

namespace UltimateEditor.Modding
{
	/// <summary>Puts a package together: the components of a diff (or a save), the author's
	/// details, the classification, and a check that the package applies back to the files.</summary>
	public static class ModBuilder
	{
		public static ModPackage FromDiff(ModDiffResult diff, ModManifest details, IFileSource original)
		{
			var pkg = new ModPackage { Manifest = details };
			var m = details;
			m.Components.Clear();
			m.Components.AddRange(diff.Components);
			m.BaseFiles = new SortedDictionary<string, string>(diff.BaseFiles, StringComparer.Ordinal);
			foreach (var kv in diff.Payload) pkg.Files[kv.Key] = kv.Value;
			if (string.IsNullOrEmpty(m.Created)) m.Created = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
			m.BaseSet = KnownFiles.SetOf(m.Game == "uw2" ? GameKind.UW2 : GameKind.UW1, m.BaseFiles);
			ModClassifier.Classify(m, original == null ? (Func<string, byte[]>)null : original.Read, pkg);
			return pkg;
		}

		/// <summary>
		/// Applies the package to the original files and compares the result with the modded files.
		/// Returns the files that come out different (none: the package reproduces the mod).
		/// </summary>
		public static List<string> Verify(ModPackage pkg, IFileSource original, IFileSource modded)
		{
			var r = new List<string>();
			Dictionary<string, byte[]> files;
			try { files = ModApply.Files(pkg, original.Read); }
			catch (ModApplyException e) { r.Add("the package does not apply to the original files: " + e.Message); return r; }
			foreach (var kv in files)
			{
				byte[] want = modded.Read(kv.Key);
				if (want == null && kv.Value == null) continue;
				if (want == null || kv.Value == null || !ModDiff.Equal(want, kv.Value))
				{
					// STRINGS.PAK may legitimately differ in bytes when the editor re-encoded every block
					if (kv.Key.EndsWith("STRINGS.PAK") && want != null && kv.Value != null && SameStrings(want, kv.Value)) continue;
					r.Add(kv.Key + " comes out different");
				}
			}
			if (ModApply.ChangesExe(pkg.Manifest))
			{
				string exeName = Exe.ExeIdentity.ExeName(pkg.Manifest.Game == "uw2" ? GameKind.UW2 : GameKind.UW1);
				byte[] exe = original.Read(exeName), want = modded.Read(exeName);
				if (exe != null && want != null)
				{
					exe = (byte[])exe.Clone();
					try { ModApply.ApplyExe(pkg, exe); if (!ModDiff.Equal(exe, want)) r.Add(exeName + " comes out different"); }
					catch (ModApplyException e) { r.Add(exeName + ": " + e.Message); }
				}
			}
			return r;
		}

		static bool SameStrings(byte[] a, byte[] b)
		{
			var x = StringsPakFile.Parse(a);
			var y = StringsPakFile.Parse(b);
			if (x.Blocks.Count != y.Blocks.Count) return false;
			foreach (var xb in x.Blocks)
			{
				var yb = y.Find(xb.Number);
				if (yb == null || yb.Strings.Count != xb.Strings.Count) return false;
				for (int i = 0; i < xb.Strings.Count; i++) if (xb.Strings[i] != yb.Strings[i]) return false;
			}
			return true;
		}

		/// <summary>An id from a name: lower case, letters, digits and dashes.</summary>
		public static string IdFrom(string author, string name)
		{
			var sb = new StringBuilder();
			foreach (var part in new[] { author, name })
			{
				if (string.IsNullOrEmpty(part)) continue;
				if (sb.Length > 0) sb.Append('.');
				bool dash = false;
				foreach (char ch in part.ToLowerInvariant())
				{
					if ((ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9')) { sb.Append(ch); dash = false; }
					else if (!dash && sb.Length > 0 && sb[sb.Length - 1] != '.') { sb.Append('-'); dash = true; }
				}
				while (sb.Length > 0 && sb[sb.Length - 1] == '-') sb.Length--;
			}
			return sb.Length == 0 ? "mod" : sb.ToString();
		}
	}
}
