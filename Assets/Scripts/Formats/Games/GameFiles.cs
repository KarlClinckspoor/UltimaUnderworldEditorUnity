using System;
using System.IO;

namespace UltimateEditor.Games
{
	/// <summary>
	/// The games name their files in upper case (DOS), but copies differ: GOG's UW1 has
	/// "pals.dat", some UW2 copies "ALLPALS.dat", unpacked ones are often all lower case.
	/// On Windows any spelling opens the file; on a case-sensitive file system it does not,
	/// so every data-file path the editor builds goes through Resolve, which keeps the
	/// path when it exists and otherwise matches each component case-insensitively.
	/// </summary>
	public static class GameFiles
	{
		/// <summary>
		/// The path with each component replaced by the existing entry of the same name in
		/// any case. Components that match nothing are kept as given, so a file about to be
		/// created keeps the requested name; an existing file is overwritten under its own name.
		/// </summary>
		public static string Resolve(string path)
		{
			if (string.IsNullOrEmpty(path) || File.Exists(path) || Directory.Exists(path)) return path;
			string full;
			try { full = Path.GetFullPath(path); } catch { return path; }
			string root = Path.GetPathRoot(full);
			string rest = full.Substring(root.Length);
			string cur = root;
			var parts = rest.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
			for (int i = 0; i < parts.Length; i++)
			{
				string next = Path.Combine(cur, parts[i]);
				if (!File.Exists(next) && !Directory.Exists(next) && Directory.Exists(cur))
				{
					string match = Match(cur, parts[i]);
					if (match != null) next = match;
				}
				cur = next;
			}
			return cur;
		}

		/// <summary>The file in the game's data folder (the folder of LEV.ARK), in any case.</summary>
		public static string InData(string dataFolder, string name)
		{
			return Resolve(dataFolder + "/" + name);
		}

		static string Match(string folder, string name)
		{
			try
			{
				foreach (var e in Directory.GetFileSystemEntries(folder))
					if (string.Equals(Path.GetFileName(e), name, StringComparison.OrdinalIgnoreCase)) return e;
			}
			catch { }
			return null;
		}
	}
}
