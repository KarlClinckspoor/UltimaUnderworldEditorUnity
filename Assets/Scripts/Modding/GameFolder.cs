using System;
using System.Collections.Generic;
using System.IO;

namespace UltimateEditor.Modding
{
	/// <summary>
	/// The files of a game install that mods change: the executable (UW.EXE / UW2.EXE) and the
	/// DATA, CRIT, CUTS and SOUND folders, by relative path ('/' and upper case, as mod.json
	/// names them). Left out: saved games, the editor's .BKP backups, the sound setup (UW.CFG),
	/// scratch files and everything else in the folder (installers, GOG and DOSBox files).
	/// </summary>
	public interface IFileSource
	{
		/// <summary>Relative paths, upper case, '/' separators.</summary>
		IEnumerable<string> List();
		/// <summary>The file's bytes, or null when it is not there.</summary>
		byte[] Read(string rel);
	}

	public class GameFolder : IFileSource
	{
		public static readonly string[] Folders = { "DATA", "CRIT", "CUTS", "SOUND" };
		public readonly string Root;

		public GameFolder(string root) { Root = root; }

		public static string Normalize(string rel) { return rel.Replace('\\', '/').ToUpperInvariant(); }

		public static bool IsGameFile(string rel)
		{
			rel = Normalize(rel);
			string name = rel.Substring(rel.LastIndexOf('/') + 1);
			if (name.EndsWith(".BKP") || name.EndsWith(".TMP") || name == "UW.CFG") return false;
			if (rel.IndexOf('/') < 0) return name == "UW.EXE" || name == "UW2.EXE";
			string top = rel.Substring(0, rel.IndexOf('/'));
			return Array.IndexOf(Folders, top) >= 0;
		}

		public virtual IEnumerable<string> List()
		{
			var r = new List<string>();
			if (!Directory.Exists(Root)) return r;
			foreach (var f in Directory.GetFiles(Root))
			{
				string rel = Normalize(Path.GetFileName(f));
				if (IsGameFile(rel)) r.Add(rel);
			}
			foreach (var d in Directory.GetDirectories(Root))
			{
				string top = Normalize(Path.GetFileName(d));
				if (Array.IndexOf(Folders, top) < 0) continue;
				foreach (var f in Directory.GetFiles(d, "*", SearchOption.AllDirectories))
				{
					string rel = Normalize(f.Substring(Root.Length).TrimStart('/', '\\'));
					if (IsGameFile(rel)) r.Add(rel);
				}
			}
			r.Sort(StringComparer.Ordinal);
			return r;
		}

		public virtual byte[] Read(string rel)
		{
			string p = Resolve(rel);
			return p != null && File.Exists(p) ? File.ReadAllBytes(p) : null;
		}

		/// <summary>The real path of a relative path, matched without case one component at a time
		/// (the game names its files in any case); null when some component is not there.</summary>
		public string Resolve(string rel)
		{
			string cur = Root;
			foreach (var part in Normalize(rel).Split('/'))
			{
				string hit = null;
				if (Directory.Exists(cur))
					foreach (var e in Directory.GetFileSystemEntries(cur))
						if (string.Equals(Path.GetFileName(e), part, StringComparison.OrdinalIgnoreCase)) { hit = e; break; }
				if (hit == null) return null;
				cur = hit;
			}
			return cur;
		}

		/// <summary>Where a relative path is, or would be created (existing folders in their own case).</summary>
		public string PathFor(string rel)
		{
			string cur = Root;
			var parts = Normalize(rel).Split('/');
			for (int i = 0; i < parts.Length; i++)
			{
				string hit = null;
				if (Directory.Exists(cur))
					foreach (var e in Directory.GetFileSystemEntries(cur))
						if (string.Equals(Path.GetFileName(e), parts[i], StringComparison.OrdinalIgnoreCase)) { hit = e; break; }
				cur = hit ?? Path.Combine(cur, parts[i]);
			}
			return cur;
		}
	}

	/// <summary>
	/// The original files of a folder the editor has written to: for each file, its .BKP (the
	/// editor's one-time copy of the original, made before its first save) when there is one,
	/// else the file itself.
	/// </summary>
	public sealed class BackupBaseFolder : GameFolder
	{
		public BackupBaseFolder(string root) : base(root) { }

		public override IEnumerable<string> List()
		{
			var set = new SortedSet<string>(base.List(), StringComparer.Ordinal);
			// a file the editor deleted still has its backup
			foreach (var d in Folders)
			{
				string dir = Resolve(d);
				if (dir == null) continue;
				foreach (var f in Directory.GetFiles(dir, "*.BKP", SearchOption.AllDirectories))
				{
					string rel = Normalize(f.Substring(Root.Length).TrimStart('/', '\\'));
					set.Add(rel.Substring(0, rel.Length - 4));
				}
			}
			foreach (var f in Directory.GetFiles(Root, "*.BKP"))
			{
				string rel = Normalize(Path.GetFileName(f));
				rel = rel.Substring(0, rel.Length - 4);
				if (IsGameFile(rel)) set.Add(rel);
			}
			return set;
		}

		public override byte[] Read(string rel)
		{
			string p = Resolve(rel + ".BKP");
			if (p != null) return File.ReadAllBytes(p);
			return base.Read(rel);
		}

		/// <summary>True when the file has a backup (the editor changed it).</summary>
		public bool HasBackup(string rel) { return Resolve(rel + ".BKP") != null; }
	}
}
